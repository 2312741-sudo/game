using System;

namespace TramChanh.Core
{
    /// <summary>Where settings are kept between runs. PlayerPrefs in the game, a dictionary in tests.</summary>
    public interface ISettingsStore
    {
        bool TryGetFloat(string key, out float value);
        void SetFloat(string key, float value);
    }

    /// <summary>Player-facing options: master volume and look speed. Values are always kept inside their ranges.</summary>
    public sealed class GameSettings
    {
        public const float MinVolume = 0f;
        public const float MaxVolume = 1f;
        public const float DefaultVolume = 0.8f;
        public const float MinLookScale = 0.5f;
        public const float MaxLookScale = 2f;
        public const float DefaultLookScale = 1f;
        public const string VolumeKey = "settings.master_volume";
        public const string LookScaleKey = "settings.look_scale";

        private float _volume = DefaultVolume;
        private float _lookScale = DefaultLookScale;

        public float MasterVolume { get => _volume; set => _volume = Clamp(value, MinVolume, MaxVolume, DefaultVolume); }
        public float LookScale { get => _lookScale; set => _lookScale = Clamp(value, MinLookScale, MaxLookScale, DefaultLookScale); }

        public void ResetToDefaults()
        {
            _volume = DefaultVolume;
            _lookScale = DefaultLookScale;
        }

        public void Load(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            MasterVolume = store.TryGetFloat(VolumeKey, out float v) ? v : DefaultVolume;
            LookScale = store.TryGetFloat(LookScaleKey, out float l) ? l : DefaultLookScale;
        }

        public void Save(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            store.SetFloat(VolumeKey, _volume);
            store.SetFloat(LookScaleKey, _lookScale);
        }

        private static float Clamp(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) { return fallback; }
            return value < min ? min : (value > max ? max : value);
        }
    }
}
