using System;
using TramChanh.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace TramChanh.Audio
{
    /// <summary>
    /// Thin Unity layer: owns a pool of AudioSources, asks AudioMixModel whether a cue may play, and plays it.
    /// Entry point: AudioManager.Create(events, library). Cues without an event (day start/end, UI click, ambient)
    /// are triggered with Play(AudioCue) / Stop(AudioCue).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour, IAudioCueSink
    {
        public const int DefaultVoices = 8;

        [SerializeField] private AudioCueLibrary _library;
        [SerializeField, Range(1, 32)] private int _voices = DefaultVoices;

        private AudioMixModel _model;
        private AudioSource[] _sources;
        private AudioEventRouter _router;
        private AudioMixerBridge _mixerBridge;
        private AudioMixerController _mixerControl;
        private AudioMixer _mixerOverride;
        private bool _mixerRouted;

        public AudioMixModel Model => _model;

        /// <summary>True once ApplySettings sent every volume to the AudioMixer; false means the old AudioListener.volume path is in use.</summary>
        public bool UsesMixer => _mixerRouted;

        /// <summary>Creates the manager on a new GameObject and hooks it to the event bus. Library may be null (silent until set).</summary>
        public static AudioManager Create(IEventBus events, AudioCueLibrary library, AudioMixer mixer = null)
        {
            var go = new GameObject("AudioManager");
            var manager = go.AddComponent<AudioManager>();
            manager._library = library;
            manager._mixerOverride = mixer;
            manager.Initialize();
            if (events != null) { manager.Connect(events); }
            return manager;
        }

        /// <summary>Starts routing existing game events to sounds. Calling it again replaces the previous connection.</summary>
        public void Connect(IEventBus events)
        {
            Initialize();
            if (_router != null) { _router.Dispose(); }
            _router = new AudioEventRouter(events, this);
        }

        /// <summary>
        /// Applies the three player volumes (0..1). With the AudioMixer asset present they go to the mixer in dB and
        /// AudioListener.volume stays at 1; without it (or when a parameter is not exposed) the previous behaviour is kept:
        /// master on AudioListener.volume, bus volumes on the sources.
        /// </summary>
        public void ApplySettings(float master, float sfx, float ambient)
        {
            Initialize();
            _mixerRouted = _mixerControl.Apply(master, sfx, ambient);
            if (_mixerRouted)
            {
                AudioListener.volume = 1f;
                _model.MasterVolume = 1f;
                _model.SetBusVolume(AudioBus.Sfx, 1f);
                _model.SetBusVolume(AudioBus.Ambient, 1f);
            }
            else
            {
                AudioListener.volume = Mathf.Clamp01(master);
                _model.MasterVolume = 1f;
                _model.SetBusVolume(AudioBus.Sfx, sfx);
                _model.SetBusVolume(AudioBus.Ambient, ambient);
            }
            RefreshPlayingVolumes();
        }

        /// <summary>Master volume from GameSettings. Leave at 1 if AudioListener.volume already carries the master volume.</summary>
        public void SetMasterVolume(float volume)
        {
            Initialize();
            _model.MasterVolume = volume;
            RefreshPlayingVolumes();
        }

        public void SetBusVolume(AudioBus bus, float volume)
        {
            Initialize();
            _model.SetBusVolume(bus, volume);
            RefreshPlayingVolumes();
        }

        public void Play(AudioCue cue)
        {
            Initialize();
            AudioClip clip = _library == null ? null : _library.GetClip(cue);
            if (clip == null) { return; }
            AudioPlayDecision d = _model.Request(cue, Now(), clip.length);
            if (!d.Play) { return; }
            AudioSource source = _sources[d.Slot];
            if (d.StoleSlot) { source.Stop(); }
            source.clip = clip;
            AudioCueProfile profile = _model.GetProfile(cue);
            source.outputAudioMixerGroup = _mixerBridge.GroupFor(profile.Bus);
            source.loop = profile.Loop;
            source.volume = d.Volume;
            source.Play();
        }

        public void Stop(AudioCue cue)
        {
            Initialize();
            if (_model.StopLoop(cue, out int slot)) { _sources[slot].Stop(); }
        }

        private void Initialize()
        {
            if (_model != null) { return; }
            _mixerBridge = gameObject.AddComponent<AudioMixerBridge>();
            _mixerBridge.SetMixer(_mixerOverride != null ? _mixerOverride : AudioMixerBridge.LoadDefaultMixer());
            _mixerControl = new AudioMixerController(_mixerBridge.HasMixer ? _mixerBridge : null);
            int count = Mathf.Max(1, _voices);
            _model = new AudioMixModel(count);
            _sources = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }
            if (_library != null)
            {
                foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
                {
                    if (cue != AudioCue.None) { _model.SetProfile(cue, _library.GetProfile(cue)); }
                }
            }
        }

        private void RefreshPlayingVolumes()
        {
            double now = Now();
            foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
            {
                if (cue == AudioCue.None || !_model.IsActive(cue, now)) { continue; }
                for (int i = 0; i < _sources.Length; i++)
                {
                    if (_sources[i].isPlaying && _sources[i].clip == (_library == null ? null : _library.GetClip(cue)))
                    {
                        _sources[i].volume = _model.EffectiveVolume(cue);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_router != null) { _router.Dispose(); _router = null; }
        }

        private static double Now() => Time.realtimeSinceStartupAsDouble;
    }
}
