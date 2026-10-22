using System;

namespace TramChanh.Core
{
    /// <summary>Where settings are kept between runs. PlayerPrefs in the game, a dictionary in tests.</summary>
    public interface ISettingsStore
    {
        bool TryGetFloat(string key, out float value);
        void SetFloat(string key, float value);
    }

    /// <summary>
    /// Player-facing options: master, effects and ambient volume, look speed, key hints and graphics quality.
    /// Values are always kept inside their ranges. Every value is stored as a float so older ISettingsStore
    /// implementations keep working (the toggle is 0 or 1, the quality level is a whole number).
    /// </summary>
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
        public const float DefaultSfxVolume = 1f;
        public const float DefaultAmbientVolume = 1f;
        public const bool DefaultShowKeyHints = true;
        /// <summary>-1 means "use the project default quality level"; 0 and up is an index into QualitySettings.names.</summary>
        public const int DefaultQualityLevel = -1;
        public const int MaxQualityLevel = 63;
        public const string SfxVolumeKey = "settings.sfx_volume";
        public const string AmbientVolumeKey = "settings.ambient_volume";
        public const string ShowKeyHintsKey = "settings.show_key_hints_flag";
        public const string QualityLevelKey = "settings.quality_level_index";

        private float _volume = DefaultVolume;
        private float _lookScale = DefaultLookScale;
        private float _sfxVolume = DefaultSfxVolume;
        private float _ambientVolume = DefaultAmbientVolume;
        private bool _showKeyHints = DefaultShowKeyHints;
        private int _qualityLevel = DefaultQualityLevel;

        public float MasterVolume { get => _volume; set => _volume = Clamp(value, MinVolume, MaxVolume, DefaultVolume); }
        public float LookScale { get => _lookScale; set => _lookScale = Clamp(value, MinLookScale, MaxLookScale, DefaultLookScale); }

        public float SfxVolume { get => _sfxVolume; set => _sfxVolume = Clamp(value, MinVolume, MaxVolume, DefaultSfxVolume); }
        public float AmbientVolume { get => _ambientVolume; set => _ambientVolume = Clamp(value, MinVolume, MaxVolume, DefaultAmbientVolume); }
        public bool ShowKeyHints { get => _showKeyHints; set => _showKeyHints = value; }
        public int QualityLevel { get => _qualityLevel; set => _qualityLevel = value < DefaultQualityLevel ? DefaultQualityLevel : (value > MaxQualityLevel ? MaxQualityLevel : value); }

        public void ResetToDefaults()
        {
            _volume = DefaultVolume;
            _lookScale = DefaultLookScale;
            _sfxVolume = DefaultSfxVolume;
            _ambientVolume = DefaultAmbientVolume;
            _showKeyHints = DefaultShowKeyHints;
            _qualityLevel = DefaultQualityLevel;
        }

        public void Load(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            MasterVolume = store.TryGetFloat(VolumeKey, out float v) ? v : DefaultVolume;
            LookScale = store.TryGetFloat(LookScaleKey, out float l) ? l : DefaultLookScale;
            SfxVolume = store.TryGetFloat(SfxVolumeKey, out float sfx) ? sfx : DefaultSfxVolume;
            AmbientVolume = store.TryGetFloat(AmbientVolumeKey, out float amb) ? amb : DefaultAmbientVolume;
            ShowKeyHints = store.TryGetFloat(ShowKeyHintsKey, out float hints) ? hints >= 0.5f : DefaultShowKeyHints;
            QualityLevel = store.TryGetFloat(QualityLevelKey, out float q) && !float.IsNaN(q) && !float.IsInfinity(q)
                ? (int)Math.Round(q) : DefaultQualityLevel;
        }

        public void Save(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            store.SetFloat(VolumeKey, _volume);
            store.SetFloat(LookScaleKey, _lookScale);
            store.SetFloat(SfxVolumeKey, _sfxVolume);
            store.SetFloat(AmbientVolumeKey, _ambientVolume);
            store.SetFloat(ShowKeyHintsKey, _showKeyHints ? 1f : 0f);
            store.SetFloat(QualityLevelKey, _qualityLevel);
        }

        private static float Clamp(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) { return fallback; }
            return value < min ? min : (value > max ? max : value);
        }
    }
}
