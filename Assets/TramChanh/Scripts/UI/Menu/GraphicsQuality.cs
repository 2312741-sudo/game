using UnityEngine;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Applies the saved graphics quality level. Level -1 means "the project default", remembered before any scene loads so
    /// the Defaults button can go back to it. Safe when the project has a single quality level.
    /// </summary>
    public static class GraphicsQuality
    {
        private static int _projectDefault = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _projectDefault = -1; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Capture() { _projectDefault = QualitySettings.GetQualityLevel(); }

        public static int LevelCount => QualitySettings.names == null ? 0 : QualitySettings.names.Length;

        /// <summary>Index to show in a dropdown: the saved level, or the level in use now when nothing is saved.</summary>
        public static int Resolve(int saved)
        {
            int count = LevelCount;
            if (count <= 0) { return 0; }
            int level = saved >= 0 ? saved : QualitySettings.GetQualityLevel();
            return level >= count ? count - 1 : (level < 0 ? 0 : level);
        }

        /// <summary>Sets the level (clamped). -1 restores the project default. Does nothing when there is only one level.</summary>
        public static void Apply(int saved)
        {
            int count = LevelCount;
            if (count <= 1) { return; }
            int level = saved >= 0 ? saved : (_projectDefault >= 0 ? _projectDefault : QualitySettings.GetQualityLevel());
            level = level >= count ? count - 1 : (level < 0 ? 0 : level);
            if (level != QualitySettings.GetQualityLevel()) { QualitySettings.SetQualityLevel(level, true); }
        }
    }
}
