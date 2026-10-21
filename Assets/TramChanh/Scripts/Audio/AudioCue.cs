namespace TramChanh.Audio
{
    /// <summary>Every sound the game can ask for. Values are stable so saved libraries keep working: only append.</summary>
    public enum AudioCue
    {
        None = 0,
        OrderReceived = 1,
        OrderServed = 2,
        GrillSizzleLoop = 3,
        CakeFlip = 4,
        Pour = 5,
        Ice = 6,
        TicketBell = 7,
        WrongAction = 8,
        DayStart = 9,
        DayEnd = 10,
        UiClick = 11,
        StreetAmbientLoop = 12,
        OrderFailed = 13
    }

    public enum AudioBus { Sfx = 0, Ambient = 1 }

    /// <summary>How one cue behaves. Higher Priority wins when every voice is busy.</summary>
    [System.Serializable]
    public struct AudioCueProfile
    {
        public float Volume;
        public float CooldownSeconds;
        public int Priority;
        public bool Loop;
        public AudioBus Bus;

        public AudioCueProfile(float volume, float cooldownSeconds, int priority, bool loop, AudioBus bus)
        {
            Volume = volume;
            CooldownSeconds = cooldownSeconds;
            Priority = priority;
            Loop = loop;
            Bus = bus;
        }

        /// <summary>Built-in defaults, used for any cue the library does not override.</summary>
        public static AudioCueProfile Default(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.OrderReceived: return new AudioCueProfile(0.8f, 0.30f, 6, false, AudioBus.Sfx);
                case AudioCue.OrderServed: return new AudioCueProfile(0.9f, 0.20f, 7, false, AudioBus.Sfx);
                case AudioCue.OrderFailed: return new AudioCueProfile(0.9f, 0.50f, 8, false, AudioBus.Sfx);
                case AudioCue.GrillSizzleLoop: return new AudioCueProfile(0.5f, 0f, 3, true, AudioBus.Sfx);
                case AudioCue.CakeFlip: return new AudioCueProfile(0.8f, 0.15f, 4, false, AudioBus.Sfx);
                case AudioCue.Pour: return new AudioCueProfile(0.7f, 0.15f, 4, false, AudioBus.Sfx);
                case AudioCue.Ice: return new AudioCueProfile(0.7f, 0.15f, 4, false, AudioBus.Sfx);
                case AudioCue.TicketBell: return new AudioCueProfile(0.9f, 0.40f, 9, false, AudioBus.Sfx);
                case AudioCue.WrongAction: return new AudioCueProfile(0.7f, 0.35f, 5, false, AudioBus.Sfx);
                case AudioCue.DayStart: return new AudioCueProfile(0.9f, 1.00f, 10, false, AudioBus.Sfx);
                case AudioCue.DayEnd: return new AudioCueProfile(0.9f, 1.00f, 10, false, AudioBus.Sfx);
                case AudioCue.UiClick: return new AudioCueProfile(0.6f, 0.05f, 2, false, AudioBus.Sfx);
                case AudioCue.StreetAmbientLoop: return new AudioCueProfile(0.35f, 0f, 1, true, AudioBus.Ambient);
                default: return new AudioCueProfile(1f, 0f, 0, false, AudioBus.Sfx);
            }
        }
    }
}
