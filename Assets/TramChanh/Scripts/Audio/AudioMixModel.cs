using System;

namespace TramChanh.Audio
{
    public enum AudioRejectReason { None, UnknownCue, Cooldown, AlreadyLooping, VoicesFull }

    /// <summary>Answer to a play request. Slot is the voice index (0..MaxVoices-1) the caller must use when Play is true.</summary>
    public readonly struct AudioPlayDecision
    {
        public bool Play { get; }
        public int Slot { get; }
        public float Volume { get; }
        /// <summary>True when the slot was taken from a lower priority sound that must be stopped first.</summary>
        public bool StoleSlot { get; }
        public AudioRejectReason Reason { get; }

        public AudioPlayDecision(bool play, int slot, float volume, bool stoleSlot, AudioRejectReason reason)
        {
            Play = play;
            Slot = slot;
            Volume = volume;
            StoleSlot = stoleSlot;
            Reason = reason;
        }

        public static AudioPlayDecision Reject(AudioRejectReason reason) => new AudioPlayDecision(false, -1, 0f, false, reason);
    }

    /// <summary>
    /// Pure volume, cooldown, priority and voice-limit rules. No Unity types: time is passed in by the caller.
    /// </summary>
    public sealed class AudioMixModel
    {
        private struct Voice
        {
            public bool Active;
            public AudioCue Cue;
            public int Priority;
            public double Start;
            public double End;
        }

        private readonly AudioCueProfile[] _profiles;
        private readonly bool[] _hasProfile;
        private readonly double[] _lastStart;
        private readonly Voice[] _voices;
        private readonly float[] _busVolume = { 1f, 1f };
        private float _master = 1f;

        public AudioMixModel(int maxVoices)
        {
            if (maxVoices < 1) { throw new ArgumentOutOfRangeException(nameof(maxVoices)); }
            int size = 0;
            foreach (AudioCue c in Enum.GetValues(typeof(AudioCue))) { if ((int)c + 1 > size) { size = (int)c + 1; } }
            _profiles = new AudioCueProfile[size];
            _hasProfile = new bool[size];
            _lastStart = new double[size];
            for (int i = 0; i < size; i++) { _lastStart[i] = double.NegativeInfinity; }
            foreach (AudioCue c in Enum.GetValues(typeof(AudioCue)))
            {
                if (c == AudioCue.None) { continue; }
                SetProfile(c, AudioCueProfile.Default(c));
            }
            _voices = new Voice[maxVoices];
        }

        public int MaxVoices => _voices.Length;

        public float MasterVolume { get => _master; set => _master = Clamp01(value, 1f); }

        public float GetBusVolume(AudioBus bus) => _busVolume[(int)bus];

        public void SetBusVolume(AudioBus bus, float volume) { _busVolume[(int)bus] = Clamp01(volume, 1f); }

        public void SetProfile(AudioCue cue, AudioCueProfile profile)
        {
            int i = (int)cue;
            if (i <= 0 || i >= _profiles.Length) { throw new ArgumentOutOfRangeException(nameof(cue)); }
            profile.Volume = Clamp01(profile.Volume, 1f);
            if (float.IsNaN(profile.CooldownSeconds) || profile.CooldownSeconds < 0f) { profile.CooldownSeconds = 0f; }
            _profiles[i] = profile;
            _hasProfile[i] = true;
        }

        public AudioCueProfile GetProfile(AudioCue cue) => _profiles[(int)cue];

        /// <summary>Master x bus x cue volume, always in 0..1.</summary>
        public float EffectiveVolume(AudioCue cue)
        {
            int i = (int)cue;
            if (i <= 0 || i >= _profiles.Length || !_hasProfile[i]) { return 0f; }
            AudioCueProfile p = _profiles[i];
            return Clamp01(_master * _busVolume[(int)p.Bus] * p.Volume, 0f);
        }

        public bool IsActive(AudioCue cue, double now)
        {
            Expire(now);
            for (int s = 0; s < _voices.Length; s++) { if (_voices[s].Active && _voices[s].Cue == cue) { return true; } }
            return false;
        }

        public int ActiveCount(double now)
        {
            Expire(now);
            int n = 0;
            for (int s = 0; s < _voices.Length; s++) { if (_voices[s].Active) { n++; } }
            return n;
        }

        /// <param name="clipLength">Seconds; ignored for loops, which stay until StopLoop.</param>
        public AudioPlayDecision Request(AudioCue cue, double now, double clipLength)
        {
            int i = (int)cue;
            if (i <= 0 || i >= _profiles.Length || !_hasProfile[i]) { return AudioPlayDecision.Reject(AudioRejectReason.UnknownCue); }
            AudioCueProfile p = _profiles[i];
            Expire(now);

            if (p.Loop)
            {
                for (int s = 0; s < _voices.Length; s++)
                {
                    if (_voices[s].Active && _voices[s].Cue == cue) { return AudioPlayDecision.Reject(AudioRejectReason.AlreadyLooping); }
                }
            }
            if (p.CooldownSeconds > 0f && now - _lastStart[i] < p.CooldownSeconds)
            {
                return AudioPlayDecision.Reject(AudioRejectReason.Cooldown);
            }

            int slot = -1;
            bool stole = false;
            for (int s = 0; s < _voices.Length; s++)
            {
                if (!_voices[s].Active) { slot = s; break; }
            }
            if (slot < 0)
            {
                // Steal the lowest priority voice (oldest first), but only from a strictly lower priority.
                int victim = 0;
                for (int s = 1; s < _voices.Length; s++)
                {
                    if (_voices[s].Priority < _voices[victim].Priority
                        || (_voices[s].Priority == _voices[victim].Priority && _voices[s].Start < _voices[victim].Start))
                    {
                        victim = s;
                    }
                }
                if (_voices[victim].Priority >= p.Priority) { return AudioPlayDecision.Reject(AudioRejectReason.VoicesFull); }
                slot = victim;
                stole = true;
            }

            double length = double.IsNaN(clipLength) || clipLength < 0d ? 0d : clipLength;
            _voices[slot] = new Voice
            {
                Active = true,
                Cue = cue,
                Priority = p.Priority,
                Start = now,
                End = p.Loop ? double.PositiveInfinity : now + length
            };
            _lastStart[i] = now;
            return new AudioPlayDecision(true, slot, EffectiveVolume(cue), stole, AudioRejectReason.None);
        }

        /// <summary>Frees the voice of a running loop. Returns false when that cue is not playing.</summary>
        public bool StopLoop(AudioCue cue, out int slot)
        {
            for (int s = 0; s < _voices.Length; s++)
            {
                if (_voices[s].Active && _voices[s].Cue == cue)
                {
                    _voices[s].Active = false;
                    slot = s;
                    return true;
                }
            }
            slot = -1;
            return false;
        }

        private void Expire(double now)
        {
            for (int s = 0; s < _voices.Length; s++)
            {
                if (_voices[s].Active && _voices[s].End <= now) { _voices[s].Active = false; }
            }
        }

        private static float Clamp01(float v, float fallback)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) { return fallback; }
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }
    }
}
