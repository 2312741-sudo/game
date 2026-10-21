using System.Reflection;
using TramChanh.App;
using TramChanh.Core;
using UnityEditor;
using UnityEngine;

namespace TramChanh.Editor.QA
{
    /// <summary>
    /// Dev shortcut to try the stall helper without earning the stars and money first (menu Tram Chanh > Dev > Grant Stall Helper).
    /// Play Mode only. It grants the upgrade for this session in memory and does not save, so the next launch is unchanged.
    /// </summary>
    public static class DevGrantHelper
    {
        private const string MenuPath = "Tram Chanh/Dev/Grant Stall Helper (Play Mode)";

        [MenuItem(MenuPath)]
        public static void Grant()
        {
            var bootstrap = Object.FindFirstObjectByType<TramChanhMainBootstrap>();
            FieldInfo field = typeof(TramChanhMainBootstrap).GetField("_upgrades", BindingFlags.NonPublic | BindingFlags.Instance);
            var upgrades = bootstrap != null && field != null ? field.GetValue(bootstrap) as UpgradeBook : null;
            if (upgrades == null) { Debug.LogWarning("Grant Stall Helper: TramChanhMainBootstrap not found."); return; }
            upgrades.Grant(UpgradeId.HelperWorker);
            Debug.Log("Grant Stall Helper: the helper is now owned for this session (not saved).");
        }

        [MenuItem(MenuPath, true)]
        public static bool CanGrant() => Application.isPlaying;
    }
}
