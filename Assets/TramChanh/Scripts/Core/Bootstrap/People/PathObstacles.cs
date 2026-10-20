using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.App.People
{
    /// <summary>
    /// Footprints of solid street props (trunks, lamp/utility poles, parked vehicles, sidewalk decor) and a detour pass that bends an
    /// ambient lane around them, so pedestrians and motorbikes no longer walk or ride through poles, trees and parked vehicles.
    /// Footprints are XZ rectangles; lanes run mostly along X, so detours step sideways (Z) around a footprint and rejoin the lane.
    /// </summary>
    public sealed class PathObstacles
    {
        private const string PropsRootName = "StreetProps";
        private const float BodyTop = 1.6f;
        private const float BodyBottom = 0.15f;
        private const float MaxFootprint = 6.5f;
        private const float Clearance = 0.1f;
        private const float TrunkRadius = 0.3f;
        private const float PoleRadius = 0.35f;

        private readonly List<Rect> _rects = new List<Rect>();

        public int Count => _rects.Count;

        public static PathObstacles Collect(Transform environmentRoot)
        {
            var result = new PathObstacles();
            if (environmentRoot == null) { return result; }
            foreach (Renderer renderer in environmentRoot.GetComponentsInChildren<Renderer>(false))
            {
                Transform top = PropRoot(renderer.transform);
                if (top == null || IsDecorative(top.name)) { continue; }

                if (top.name.StartsWith("PF_StreetTree") || top.name.StartsWith("PF_StreetLamp") || top.name.Contains("UtilityPole"))
                {
                    // Bounds of these include the canopy / lamp arm, so only the base counts as solid.
                    float radius = top.name.StartsWith("PF_StreetTree") ? TrunkRadius : PoleRadius;
                    Vector3 p = top.position;
                    result.AddUnique(new Rect(p.x - radius, p.z - radius, radius * 2f, radius * 2f));
                    continue;
                }

                Bounds b = renderer.bounds;
                if (b.min.y > BodyTop || b.max.y < BodyBottom || b.size.x > MaxFootprint || b.size.z > MaxFootprint) { continue; }
                result.AddUnique(new Rect(b.min.x, b.min.z, b.size.x, b.size.z));
            }
            return result;
        }

        /// <summary>
        /// Bends <paramref name="path"/> (world space) around footprints within <paramref name="radius"/>. Footprints that touch each other
        /// (a truck's body, cab and cargo) form one obstacle that is detoured once, sideways across the segment's dominant axis.
        /// Footprints that already contain the path's start or end (a seat, a table) are ignored.
        /// </summary>
        public void Detour(List<Vector3> path, float radius, float maxShift = 2.5f)
        {
            if (path.Count < 2 || _rects.Count == 0) { return; }
            Vector3 first = path[0], last = path[path.Count - 1];
            List<Rect> clusters = BuildClusters(path, radius, first, last);
            if (clusters.Count == 0) { return; }

            var handled = new bool[clusters.Count];
            var output = new List<Vector3>(path.Count + 8) { first };
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 b = path[i];
                bool isLast = i == path.Count - 1;
                Vector3 a = output[output.Count - 1];
                if (!isLast && InsideHandled(b, clusters, handled)) { continue; }   // waypoint buried in an obstacle we already walked around

                bool alongZ = Mathf.Abs(b.z - a.z) > Mathf.Abs(b.x - a.x);
                bool forward = (alongZ ? b.z - a.z : b.x - a.x) >= 0f;
                var order = new List<int>();
                for (int c = 0; c < clusters.Count; c++) { if (!handled[c] && Intersects(a, b, clusters[c])) { order.Add(c); } }
                order.Sort((l, r) => { Rect lo = Orient(clusters[l], alongZ), ro = Orient(clusters[r], alongZ); return forward ? lo.xMin.CompareTo(ro.xMin) : ro.xMax.CompareTo(lo.xMax); });

                float y = a.y;
                float lane = alongZ ? a.x : a.z;
                foreach (int c in order)
                {
                    handled[c] = true;
                    Rect grown = Orient(clusters[c], alongZ);
                    float below = grown.yMin - Clearance, above = grown.yMax + Clearance;
                    bool belowFree = !Blocked(grown.xMin, below, clusters, c, alongZ) && !Blocked(grown.xMax, below, clusters, c, alongZ);
                    bool aboveFree = !Blocked(grown.xMin, above, clusters, c, alongZ) && !Blocked(grown.xMax, above, clusters, c, alongZ);
                    float side;
                    if (belowFree && aboveFree) { side = Mathf.Abs(below - lane) <= Mathf.Abs(above - lane) ? below : above; }
                    else if (belowFree) { side = below; }
                    else if (aboveFree) { side = above; }
                    else { continue; }
                    if (Mathf.Abs(side - lane) > maxShift) { continue; }
                    float xIn = forward ? grown.xMin - Clearance : grown.xMax + Clearance;
                    float xOut = forward ? grown.xMax + Clearance : grown.xMin - Clearance;
                    output.Add(World(xIn, lane, y, alongZ));
                    output.Add(World(xIn, side, y, alongZ));
                    output.Add(World(xOut, side, y, alongZ));
                    output.Add(World(xOut, lane, y, alongZ));
                }
                if (isLast || !InsideHandled(b, clusters, handled)) { output.Add(b); }   // a waypoint inside the obstacle just walked around is dropped
            }
            path.Clear();
            path.AddRange(output);
        }

        // Grown footprints that any segment crosses, joined with every footprint touching them, merged into one rect per obstacle.
        private List<Rect> BuildClusters(List<Vector3> path, float radius, Vector3 first, Vector3 last)
        {
            var grown = new List<Rect>(_rects.Count);
            foreach (Rect rect in _rects)
            {
                Rect g = Grow(rect, radius);
                if (g.Contains(new Vector2(first.x, first.z)) || g.Contains(new Vector2(last.x, last.z))) { continue; }
                grown.Add(g);
            }
            var clusters = new List<Rect>();
            var used = new bool[grown.Count];
            for (int i = 0; i < grown.Count; i++)
            {
                if (used[i] || !CrossesPath(path, grown[i])) { continue; }
                Rect union = grown[i];
                used[i] = true;
                for (bool grew = true; grew; )
                {
                    grew = false;
                    for (int j = 0; j < grown.Count; j++)
                    {
                        if (used[j] || !Touches(union, grown[j])) { continue; }
                        union = Rect.MinMaxRect(Mathf.Min(union.xMin, grown[j].xMin), Mathf.Min(union.yMin, grown[j].yMin), Mathf.Max(union.xMax, grown[j].xMax), Mathf.Max(union.yMax, grown[j].yMax));
                        used[j] = true; grew = true;
                    }
                }
                clusters.Add(union);
            }
            return clusters;
        }

        private static bool CrossesPath(List<Vector3> path, Rect rect)
        {
            for (int i = 1; i < path.Count; i++) { if (Intersects(path[i - 1], path[i], rect)) { return true; } }
            return false;
        }

        private static bool Touches(Rect a, Rect b) => a.xMin <= b.xMax + Clearance && b.xMin <= a.xMax + Clearance && a.yMin <= b.yMax + Clearance && b.yMin <= a.yMax + Clearance;

        private static bool InsideHandled(Vector3 p, List<Rect> clusters, bool[] handled)
        {
            for (int c = 0; c < clusters.Count; c++) { if (handled[c] && clusters[c].Contains(new Vector2(p.x, p.z))) { return true; } }
            return false;
        }

        private static bool Blocked(float along, float across, List<Rect> clusters, int ignore, bool alongZ)
        {
            var p = new Vector2(along, across);
            for (int c = 0; c < clusters.Count; c++) { if (c != ignore && Orient(clusters[c], alongZ).Contains(p)) { return true; } }
            return false;
        }

        // Rect fields in "travel space": x = along the segment, y = across it. For Z-dominant segments the world axes are swapped.
        private static Rect Orient(Rect world, bool alongZ) => alongZ ? new Rect(world.yMin, world.xMin, world.height, world.width) : world;
        private static Vector3 World(float along, float across, float y, bool alongZ) => alongZ ? new Vector3(across, y, along) : new Vector3(along, y, across);

        private static Rect Grow(Rect rect, float radius)
        {
            return new Rect(rect.xMin - radius, rect.yMin - radius, rect.width + radius * 2f, rect.height + radius * 2f);
        }

        // Liang-Barsky clip of the XZ segment against the rectangle.
        private static bool Intersects(Vector3 a, Vector3 b, Rect rect)
        {
            float t0 = 0f, t1 = 1f;
            float dx = b.x - a.x, dz = b.z - a.z;
            return Clip(-dx, a.x - rect.xMin, ref t0, ref t1) && Clip(dx, rect.xMax - a.x, ref t0, ref t1)
                && Clip(-dz, a.z - rect.yMin, ref t0, ref t1) && Clip(dz, rect.yMax - a.z, ref t0, ref t1);
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(p, 0f)) { return q >= 0f; }
            float t = q / p;
            if (p < 0f) { if (t > t1) { return false; } if (t > t0) { t0 = t; } }
            else { if (t < t0) { return false; } if (t < t1) { t1 = t; } }
            return true;
        }

        private void AddUnique(Rect rect)
        {
            foreach (Rect existing in _rects) { if (existing.Overlaps(rect) && existing.Contains(rect.center)) { return; } }
            _rects.Add(rect);
        }

        private static Transform PropRoot(Transform leaf)
        {
            for (Transform t = leaf; t != null && t.parent != null; t = t.parent)
            {
                if (t.parent.name == PropsRootName) { return t; }
            }
            return null;
        }

        private static bool IsDecorative(string name)
        {
            return name == "Animals" || name.Contains("Light") || name.Contains("Banner") || name.Contains("Sign");
        }
    }
}
