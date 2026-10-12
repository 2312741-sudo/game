using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TramChanh.Core.GroundTruth;
using TramChanh.Stall.Layout;
using UnityEditor;
using UnityEngine;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// Read-only QA check: do the counter props of the stall overlap each other on the XZ plane?
    /// Props are the enabled colliders on the Interactable layer under the stall root; their world-space
    /// bounds are fed to the pure rule in <see cref="StallCounterLayoutRule"/>. The check never moves
    /// anything: it only reports. The prefab is opened with <see cref="PrefabUtility.LoadPrefabContents"/>
    /// and discarded without saving.
    /// </summary>
    public static class StallPropOverlapCheck
    {
        public const string StallPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab";
        public const string CounterRendererName = "SM_Stall_Counter";
        public const string RedTeaRackName = "RedTeaRack";

        /// <summary>Menu entry under Tram Chanh/QA/ (one attribute, same root as the other QA commands).</summary>
        [MenuItem(TramChanhValidationMenu.MenuRoot + "Check Stall Prop Overlaps", false, 25)]
        public static void CheckStallPrefabMenu()
        {
            string text = CheckPrefab(StallPrefabPath, StallCounterLayoutRule.DefaultToleranceMetres, out int problems);
            if (problems == 0) { Debug.Log(text); }
            else { Debug.LogWarning(text); }
        }

        /// <summary>
        /// Opens the prefab asset read-only, runs <see cref="Analyze"/> and returns the report text.
        /// <paramref name="problems"/> counts overlaps, props outside the counter and a misplaced red tea rack.
        /// </summary>
        public static string CheckPrefab(string assetPath, float tolerance, out int problems)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
            if (contents == null)
            {
                problems = 1;
                return "[StallPropOverlap] ERROR: could not load prefab at " + assetPath;
            }

            try
            {
                return Analyze(contents, assetPath, tolerance, out problems);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>Analyses a live or prefab-contents root. Does not modify it.</summary>
        public static string Analyze(GameObject root, string label, float tolerance, out int problems)
        {
            var sb = new StringBuilder();
            problems = 0;
            sb.AppendLine("Stall prop overlap check: " + label);
            sb.AppendLine("XZ footprints, tolerance " + tolerance.ToString("0.00", CultureInfo.InvariantCulture) + " m. Read-only; nothing was moved.");

            FootprintRect? counter = ReadCounterFootprint(root, sb, ref problems);

            var props = new List<FootprintRect>();
            int skipped = 0;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled) { continue; }
                if (collider.gameObject.layer != TramChanhLayers.InteractableIndex)
                {
                    skipped++;
                    continue;
                }

                Bounds b = collider.bounds;
                props.Add(new FootprintRect(RelativePath(root.transform, collider.transform),
                    b.min.x, b.min.z, b.max.x, b.max.z));
            }

            sb.AppendLine();
            sb.AppendLine("Props (Interactable-layer colliders): " + props.Count
                + ". Other enabled colliders, not checked as props: " + skipped + ".");
            foreach (FootprintRect prop in props)
            {
                sb.AppendLine("  - " + prop.Name + "  X[" + Fmt(prop.MinX) + ", " + Fmt(prop.MaxX)
                    + "]  Z[" + Fmt(prop.MinZ) + ", " + Fmt(prop.MaxZ) + "]  " + Fmt(prop.Width) + " x " + Fmt(prop.Depth) + " m");
            }

            if (props.Count == 0)
            {
                sb.AppendLine("  No interactable props under this root, so there is nothing to overlap. Counter props are placed at runtime (e.g. PF_DrinkStation), not in this prefab.");
            }

            IReadOnlyList<PropOverlap> overlaps = StallCounterLayoutRule.FindPropOverlaps(props, tolerance);
            sb.AppendLine();
            sb.AppendLine("Overlapping pairs: " + overlaps.Count);
            foreach (PropOverlap overlap in overlaps)
            {
                sb.AppendLine("  OVERLAP: " + overlap.First + " <-> " + overlap.Second
                    + "  overlap " + Fmt(overlap.OverlapX) + " x " + Fmt(overlap.OverlapZ) + " m, area "
                    + overlap.AreaSquareMetres.ToString("0.000", CultureInfo.InvariantCulture) + " m2");
            }
            problems += overlaps.Count;

            if (counter.HasValue)
            {
                IReadOnlyList<string> outside = StallCounterLayoutRule.FindPropsOutsideCounter(counter.Value, props, tolerance);
                sb.AppendLine("Props outside the counter footprint: " + outside.Count);
                foreach (string name in outside) { sb.AppendLine("  OUTSIDE: " + name); }
                problems += outside.Count;
            }

            if (counter.HasValue)
            {
                bool anyRack = false;
                foreach (FootprintRect prop in props)
                {
                    if (prop.Name.IndexOf(RedTeaRackName, System.StringComparison.OrdinalIgnoreCase) < 0) { continue; }
                    anyRack = true;
                    bool behind = StallCounterLayoutRule.IsBehindCounter(counter.Value, prop, tolerance);
                    sb.AppendLine("Red tea rack behind counter (" + prop.Name + "): " + (behind ? "yes" : "NO"));
                    if (!behind) { problems++; }
                }

                if (!anyRack)
                {
                    sb.AppendLine("Red tea rack: no interactable collider named *" + RedTeaRackName + "* under this root; behind-counter rule not evaluated.");
                }
            }

            sb.AppendLine();
            sb.AppendLine("SUMMARY: " + (problems == 0 ? "PASS" : "FAIL") + " - problems " + problems);
            return sb.ToString();
        }

        private static FootprintRect? ReadCounterFootprint(GameObject root, StringBuilder sb, ref int problems)
        {
            Renderer counter = null;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == CounterRendererName) { counter = renderer; break; }
            }

            if (counter == null)
            {
                sb.AppendLine("ERROR: counter renderer '" + CounterRendererName + "' not found; counter footprint checks skipped.");
                problems++;
                return null;
            }

            Bounds b = counter.bounds;
            float widthError = Mathf.Abs(b.size.x - StallDimensions.Width);
            float depthError = Mathf.Abs(b.size.z - StallDimensions.Depth);
            bool ok = widthError <= StallDimensions.FootprintTolerance && depthError <= StallDimensions.FootprintTolerance;
            sb.AppendLine("Counter footprint " + Fmt(b.size.x) + " x " + Fmt(b.size.z) + " m (GT-001 "
                + Fmt(StallDimensions.Width) + " x " + Fmt(StallDimensions.Depth) + " m): " + (ok ? "OK" : "MISMATCH"));
            if (!ok) { problems++; }

            return new FootprintRect("Counter", b.min.x, b.min.z, b.max.x, b.max.z);
        }

        private static string RelativePath(Transform root, Transform target)
        {
            var parts = new List<string>();
            for (Transform t = target; t != null && t != root; t = t.parent)
            {
                parts.Add(t.name);
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string Fmt(float value) => value.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
