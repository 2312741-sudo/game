using System;

namespace TramChanh.Audio
{
    /// <summary>
    /// Pure conversion between a 0..1 slider value and decibels. The mixer measures loudness in dB (a logarithmic scale),
    /// so a slider must go through 20 * log10(v): half of the slider is -6 dB, not "half as loud".
    /// </summary>
    public static class AudioDecibel
    {
        /// <summary>The quietest value an AudioMixer fader can hold; used instead of minus infinity for silence.</summary>
        public const float SilenceDb = -80f;

        /// <summary>0..1 to dB. 1 gives 0 dB. Zero, negatives, NaN and anything below -80 dB give -80 dB. Above 1 is clamped to 0 dB.</summary>
        public static float LinearToDb(float linear)
        {
            if (float.IsNaN(linear) || linear <= 0f) { return SilenceDb; }
            if (linear >= 1f) { return 0f; }
            float db = 20f * (float)Math.Log10(linear);
            return db < SilenceDb ? SilenceDb : db;
        }

        /// <summary>dB back to 0..1. -80 dB and lower give exactly 0; 0 dB and higher give 1.</summary>
        public static float DbToLinear(float db)
        {
            if (float.IsNaN(db) || db <= SilenceDb) { return 0f; }
            if (db >= 0f) { return 1f; }
            return (float)Math.Pow(10.0, db / 20.0);
        }
    }
}
