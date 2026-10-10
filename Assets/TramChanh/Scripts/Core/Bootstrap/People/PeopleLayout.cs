using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.App.People
{
    /// <summary>
    /// Collects the walkway anchors the people simulation needs (Docs/Design/NPC_AND_MAP_EXPANSION.md §5).
    /// Antigravity's environment provides them as WP_lane_nn, StreetEntryWest/East, TableApproach_nn. When an environment has none,
    /// a provisional development layout around the current service zone is generated (DEC-010) so the game still has walking people.
    /// </summary>
    public static class PeopleLayout
    {
        public const string WaypointPrefix = "WP_";
        public const string TableApproachPrefix = "TableApproach_";
        public const string StreetEntryWest = "StreetEntryWest";
        public const string StreetEntryEast = "StreetEntryEast";
        public const string ServicePoint = "ServicePoint";

        public static bool IsWalkwayAnchor(string name)
        {
            return name.StartsWith(WaypointPrefix, System.StringComparison.Ordinal)
                || name.StartsWith(TableApproachPrefix, System.StringComparison.Ordinal)
                || name.StartsWith("BikeParking_", System.StringComparison.Ordinal)
                || name == StreetEntryWest || name == StreetEntryEast;
        }

        /// <summary>Returns world-space anchors keyed by name; <paramref name="usedFallback"/> tells whether the dev layout was generated.</summary>
        public static Dictionary<string, Vector3> Collect(Transform environmentRoot, IReadOnlyList<Transform> tableAnchors, out bool usedFallback)
        {
            var anchors = new Dictionary<string, Vector3>();
            bool hasLane = false;
            foreach (Transform child in environmentRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!IsWalkwayAnchor(child.name) || anchors.ContainsKey(child.name)) { continue; }
                anchors.Add(child.name, child.position);
                if (child.name.StartsWith(WaypointPrefix, System.StringComparison.Ordinal)) { hasLane = true; }
            }
            usedFallback = !hasLane;
            if (usedFallback) { AddFallback(environmentRoot, anchors); }
            for (int i = 0; i < tableAnchors.Count; i++)
            {
                string name = TableApproachPrefix + (i + 1).ToString("00");
                if (anchors.ContainsKey(name)) { continue; }
                Transform table = tableAnchors[i];
                Transform service = FindChildDeep(table, ServicePoint);
                anchors.Add(name, service != null ? service.position : table.TransformPoint(new Vector3(0f, 0f, 0.8f)));
            }
            return anchors;
        }

        private static void AddFallback(Transform root, Dictionary<string, Vector3> anchors)
        {
            // Environment-root-local positions around the current service zone (stall at the origin, street at +Z, buildings at -Z).
            float[] front = { -10f, -8f, -6f, -4f, -2f, -0.6f };
            for (int i = 0; i < front.Length; i++) { Put(root, anchors, "WP_SidewalkNorth_" + (i + 1).ToString("00"), front[i], 2.9f); }
            for (int i = 0; i <= 10; i++) { Put(root, anchors, "WP_SidewalkSouth_" + (i + 1).ToString("00"), -10f + 2f * i, -3.8f); }
            Vector2[] alley =
            {
                new Vector2(-9f, 2.4f), new Vector2(-9f, -0.8f), new Vector2(-7.1f, -0.8f), new Vector2(-4.6f, -0.8f), new Vector2(-2.4f, -0.8f),
                new Vector2(-1.6f, -1.6f), new Vector2(1.6f, -1.6f), new Vector2(3f, -1.4f), new Vector2(4.9f, -1.3f), new Vector2(7.6f, -1.3f),
                new Vector2(7.6f, -3.2f),
            };
            for (int i = 0; i < alley.Length; i++) { Put(root, anchors, "WP_Alley_" + (i + 1).ToString("00"), alley[i].x, alley[i].y); }
            for (int i = 0; i <= 8; i++)
            {
                Put(root, anchors, "WP_RoadEast_" + (i + 1).ToString("00"), -16f + 4f * i, 5.6f);
                Put(root, anchors, "WP_RoadWest_" + (i + 1).ToString("00"), 16f - 4f * i, 7.4f);
            }
            Put(root, anchors, StreetEntryWest, -11f, 2.9f);
            Put(root, anchors, StreetEntryEast, 10.5f, -3.8f);
        }

        private static void Put(Transform root, Dictionary<string, Vector3> anchors, string name, float x, float z)
        {
            if (!anchors.ContainsKey(name)) { anchors.Add(name, root.TransformPoint(new Vector3(x, 0f, z))); }
        }

        private static Transform FindChildDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) { if (child.name == name) { return child; } }
            return null;
        }
    }
}
