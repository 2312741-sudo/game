using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// Pure functions behind the street-end terrains (heights, splat weights, trees, grass).
    /// No scene, no asset database: everything here is testable in EditMode.
    ///
    /// Local frame of one terrain: u = metres measured OUTWARD from the street (u = 0 is the
    /// inner edge that touches the road slab at x = +-256), v = metres along z from the
    /// terrain's own z origin. Array indices follow Unity: heights[zIndex, xIndex].
    /// For the west terrain the x axis points away from the street, so xIndex is mirrored.
    /// </summary>
    public static class TerrainBuilderMath
    {
        // ---- Layout (world, metres) -------------------------------------------------
        public const float StreetEdgeX = 256f;          // road slab of the boulevard gate ends here
        public const float TerrainLength = 96f;         // outward size (terrain x size)
        public const float TerrainWidth = 100f;         // terrain z size
        public const float TerrainOriginZ = -40f;       // covers z in [-40, 60], gate spans [-11.9, 28.1]
        public const float RoadCenterZ = 8.1f;          // middle of the 2-lane roadway (z 3.6 .. 12.6)
        public const float TerrainSizeY = 16f;
        public const float TerrainOriginY = -8f;        // so that normalized 0.5 == world y 0 exactly

        public const int HeightmapResolution = 129;     // 2^7 + 1, cell about 0.75 m
        public const int AlphamapResolution = 128;
        public const int DetailResolution = 128;
        public const int DetailPerPatch = 16;

        // 16-bit storage: 0.5 * 32766 = 16383 exactly, so the joint height has no quantization error.
        public const float FlatNormalized = 0.5f;

        // ---- Shape controls ---------------------------------------------------------
        public const float FlatBandMeters = 6f;         // strictly flat strip next to the road slab
        public const float RampMeters = 22f;            // distance over which hills fade in
        public const float BorderFadeMeters = 14f;      // far/side edges return to y = 0
        public const float AmplitudeMeters = 5f;
        public const float NoiseBias = 0.30f;           // h = A * (noise01 - bias): mostly up, small dips
        public const float CorridorHalfWidth = 5f;      // keep the view along the road calm
        public const float CorridorFade = 10f;
        public const float CorridorMinAmplitude = 0.15f;
        public const float NoiseWavelength = 42f;

        // ---- Layers ------------------------------------------------------------------
        public const int LayerDirt = 0;
        public const int LayerGrass = 1;
        public const int LayerGravel = 2;
        public const int LayerCount = 3;

        // ---- Trees / details ---------------------------------------------------------
        public const float TreeCellMeters = 12f;
        public const float TreeMinOutward = 14f;        // nothing right behind the gate
        public const float TreeCorridorHalfWidth = 8f;
        public const float TreeCorridorUntil = 70f;
        public const float TreeMaxSlopeDegrees = 22f;
        public const int MaxTreesPerTerrain = 40;
        public const int MaxGrassDensity = 2;
        public const int MaxFlowerDensity = 1;

        // ---- Basic maths ----------------------------------------------------------------

        public static float Smoothstep(float a, float b, float x)
        {
            if (b <= a) return x < a ? 0f : 1f;
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) + (uint)(y * 668265263) + (uint)(seed * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>Smooth value noise in [0,1].</summary>
        public static float ValueNoise(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash01(x0, y0, seed), b = Hash01(x0 + 1, y0, seed);
            float c = Hash01(x0, y0 + 1, seed), d = Hash01(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Fractal noise in [0,1] (4 octaves).</summary>
        public static float Fbm01(float uMeters, float vMeters, int seed)
        {
            float sum = 0f, amp = 1f, norm = 0f, freq = 1f / NoiseWavelength;
            for (int o = 0; o < 4; o++)
            {
                sum += amp * ValueNoise(uMeters * freq, vMeters * freq, seed + o * 31);
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return sum / norm;
        }

        // ---- Coordinates -----------------------------------------------------------------

        /// <summary>Outward distance (m) for a normalized x in [0,1] across the terrain.</summary>
        public static float OutwardFromNormalizedX(float xNorm, bool west)
        {
            return (west ? 1f - xNorm : xNorm) * TerrainLength;
        }

        public static float ZFromNormalized(float zNorm)
        {
            return zNorm * TerrainWidth;
        }

        // ---- Height ------------------------------------------------------------------------

        /// <summary>0 at the joint with the road and on the outer borders, 1 where hills are free.</summary>
        public static float HillMask(float uMeters, float vMeters)
        {
            float inner = Smoothstep(FlatBandMeters, FlatBandMeters + RampMeters, uMeters);
            float outer = Smoothstep(0f, BorderFadeMeters, TerrainLength - uMeters);
            float sideLo = Smoothstep(0f, BorderFadeMeters, vMeters);
            float sideHi = Smoothstep(0f, BorderFadeMeters, TerrainWidth - vMeters);
            float dz = Mathf.Abs(vMeters + TerrainOriginZ - RoadCenterZ);
            float corridor = Mathf.Lerp(CorridorMinAmplitude, 1f,
                Smoothstep(CorridorHalfWidth, CorridorHalfWidth + CorridorFade, dz));
            return inner * outer * sideLo * sideHi * corridor;
        }

        /// <summary>Height above world y = 0 in metres (may be slightly negative). Exactly 0 when the mask is 0.</summary>
        public static float HeightMeters(float uMeters, float vMeters, int seed)
        {
            float mask = HillMask(uMeters, vMeters);
            if (mask <= 0f) return 0f;
            return AmplitudeMeters * (Fbm01(uMeters, vMeters, seed) - NoiseBias) * mask;
        }

        public static float MetersToNormalized(float meters)
        {
            return FlatNormalized + meters / TerrainSizeY;
        }

        public static float NormalizedToMeters(float normalized)
        {
            return (normalized - FlatNormalized) * TerrainSizeY;
        }

        /// <summary>Heights for TerrainData.SetHeights, indexed [zIndex, xIndex].</summary>
        public static float[,] BuildHeights(int resolution, int seed, bool west)
        {
            var h = new float[resolution, resolution];
            float step = 1f / (resolution - 1);
            for (int zi = 0; zi < resolution; zi++)
            {
                float v = ZFromNormalized(zi * step);
                for (int xi = 0; xi < resolution; xi++)
                {
                    float u = OutwardFromNormalizedX(xi * step, west);
                    h[zi, xi] = MetersToNormalized(HeightMeters(u, v, seed));
                }
            }
            return h;
        }

        /// <summary>Bilinear sample of a heightmap at normalized coordinates (result is normalized height).</summary>
        public static float SampleHeight01(float[,] heights, float xNorm, float zNorm)
        {
            int n = heights.GetLength(0);
            int m = heights.GetLength(1);
            float fx = Mathf.Clamp01(xNorm) * (m - 1);
            float fz = Mathf.Clamp01(zNorm) * (n - 1);
            int x0 = Mathf.Min((int)fx, m - 2), z0 = Mathf.Min((int)fz, n - 2);
            float tx = fx - x0, tz = fz - z0;
            float a = Mathf.Lerp(heights[z0, x0], heights[z0, x0 + 1], tx);
            float b = Mathf.Lerp(heights[z0 + 1, x0], heights[z0 + 1, x0 + 1], tx);
            return Mathf.Lerp(a, b, tz);
        }

        /// <summary>Terrain steepness in degrees at normalized coordinates (central differences).</summary>
        public static float SlopeDegrees(float[,] heights, float xNorm, float zNorm)
        {
            int n = heights.GetLength(0);
            int m = heights.GetLength(1);
            float dx = 1f / (m - 1), dz = 1f / (n - 1);
            float hx1 = SampleHeight01(heights, xNorm + dx, zNorm);
            float hx0 = SampleHeight01(heights, xNorm - dx, zNorm);
            float hz1 = SampleHeight01(heights, xNorm, zNorm + dz);
            float hz0 = SampleHeight01(heights, xNorm, zNorm - dz);
            float spanX = (Mathf.Min(xNorm + dx, 1f) - Mathf.Max(xNorm - dx, 0f)) * TerrainLength;
            float spanZ = (Mathf.Min(zNorm + dz, 1f) - Mathf.Max(zNorm - dz, 0f)) * TerrainWidth;
            float gx = (hx1 - hx0) * TerrainSizeY / Mathf.Max(spanX, 1e-4f);
            float gz = (hz1 - hz0) * TerrainSizeY / Mathf.Max(spanZ, 1e-4f);
            return Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
        }

        // ---- Splat map -----------------------------------------------------------------------

        /// <summary>
        /// Writes weights (dirt, grass, gravel) that sum to 1 into <paramref name="w"/>.
        /// Rules: steep slope = dirt; the road bed just outside the gate and high ground = gravel;
        /// the rest (low, flat) = grass. noise01 breaks the borders up.
        /// </summary>
        public static void SplatWeights(float slopeDeg, float heightMeters, float uMeters, float distFromRoadCenter,
            float noise01, float[] w)
        {
            float dirt = Smoothstep(4f, 11f, slopeDeg + (noise01 - 0.5f) * 6f);

            float roadBed = (1f - Smoothstep(CorridorHalfWidth, CorridorHalfWidth + 3f, distFromRoadCenter))
                            * (1f - Smoothstep(10f, 24f, uMeters));
            float high = Smoothstep(0.9f, 1.8f, heightMeters + (noise01 - 0.5f) * 1.2f);
            float gravel = Mathf.Clamp01(Mathf.Max(roadBed, high * 0.85f));

            float grass = Mathf.Max(0.05f, 1f - Mathf.Max(dirt, gravel));
            // gravel and dirt compete: the road bed stays gravel even on a slope
            float dirtW = dirt * (1f - roadBed);
            float gravelW = gravel;
            float sum = dirtW + grass + gravelW;
            if (sum <= 1e-6f) { w[LayerDirt] = 0f; w[LayerGrass] = 1f; w[LayerGravel] = 0f; return; }
            w[LayerDirt] = dirtW / sum;
            w[LayerGrass] = grass / sum;
            w[LayerGravel] = gravelW / sum;
        }

        /// <summary>Alphamap for SetAlphamaps, indexed [zIndex, xIndex, layer].</summary>
        public static float[,,] BuildSplat(float[,] heights, int resolution, int seed, bool west)
        {
            var map = new float[resolution, resolution, LayerCount];
            var w = new float[LayerCount];
            for (int zi = 0; zi < resolution; zi++)
            {
                float zn = (zi + 0.5f) / resolution;
                float v = ZFromNormalized(zn);
                float dz = Mathf.Abs(v + TerrainOriginZ - RoadCenterZ);
                for (int xi = 0; xi < resolution; xi++)
                {
                    float xn = (xi + 0.5f) / resolution;
                    float u = OutwardFromNormalizedX(xn, west);
                    float hm = NormalizedToMeters(SampleHeight01(heights, xn, zn));
                    float slope = SlopeDegrees(heights, xn, zn);
                    float noise = ValueNoise(u * 0.11f, v * 0.11f, seed + 977);
                    SplatWeights(slope, hm, u, dz, noise, w);
                    for (int l = 0; l < LayerCount; l++) map[zi, xi, l] = w[l];
                }
            }
            return map;
        }

        // ---- Trees -----------------------------------------------------------------------------

        public struct TreeSpot
        {
            public float XNorm, ZNorm, YNorm;   // TreeInstance.position (all normalized to terrain size)
            public float RotationRad;
            public float Scale;
        }

        /// <summary>
        /// Jittered grid: at most one tree per cell, kept with probability <paramref name="density"/> (0..1).
        /// Rejected: too close to the gate, in the view corridor along the road, on steep slopes.
        /// Capped at MaxTreesPerTerrain.
        /// </summary>
        public static List<TreeSpot> PlaceTrees(float[,] heights, int seed, bool west, float density)
        {
            var result = new List<TreeSpot>();
            if (density <= 0f) return result;
            int cellsU = Mathf.FloorToInt(TerrainLength / TreeCellMeters);
            int cellsV = Mathf.FloorToInt(TerrainWidth / TreeCellMeters);
            for (int cv = 0; cv < cellsV; cv++)
            {
                for (int cu = 0; cu < cellsU; cu++)
                {
                    if (Hash01(cu, cv, seed + 5) > density) continue;
                    float u = (cu + 0.15f + 0.7f * Hash01(cu, cv, seed + 6)) * TreeCellMeters;
                    float v = (cv + 0.15f + 0.7f * Hash01(cu, cv, seed + 7)) * TreeCellMeters;
                    if (u < TreeMinOutward || u > TerrainLength - 6f) continue;
                    if (v < 6f || v > TerrainWidth - 6f) continue;
                    float dz = Mathf.Abs(v + TerrainOriginZ - RoadCenterZ);
                    if (u < TreeCorridorUntil && dz < TreeCorridorHalfWidth) continue;

                    float xn = west ? 1f - u / TerrainLength : u / TerrainLength;
                    float zn = v / TerrainWidth;
                    if (SlopeDegrees(heights, xn, zn) > TreeMaxSlopeDegrees) continue;

                    result.Add(new TreeSpot
                    {
                        XNorm = xn,
                        ZNorm = zn,
                        YNorm = SampleHeight01(heights, xn, zn) ,
                        RotationRad = Hash01(cu, cv, seed + 8) * Mathf.PI * 2f,
                        Scale = 0.85f + 0.45f * Hash01(cu, cv, seed + 9)
                    });
                    if (result.Count >= MaxTreesPerTerrain) return result;
                }
            }
            return result;
        }

        // ---- Details (grass, flowers) ---------------------------------------------------------------

        /// <summary>Grass blades per detail cell, 0..MaxGrassDensity: only on grass, not on steep ground, sparse.</summary>
        public static int GrassDensity(float grassWeight, float slopeDeg, float uMeters, float noise01)
        {
            if (uMeters < 3f) return 0;                       // nothing on the joint strip
            if (slopeDeg > 30f || grassWeight < 0.35f) return 0;
            float f = (grassWeight - 0.35f) / 0.65f * (0.3f + 0.7f * noise01);
            return Mathf.Clamp(Mathf.RoundToInt(f * MaxGrassDensity), 0, MaxGrassDensity);
        }

        /// <summary>Wild flowers: rare, only where grass is thick (0 or 1).</summary>
        public static int FlowerDensity(float grassWeight, float slopeDeg, float uMeters, float noise01)
        {
            if (uMeters < 6f || slopeDeg > 20f || grassWeight < 0.6f) return 0;
            return noise01 > 0.94f ? MaxFlowerDensity : 0;
        }

        /// <summary>Detail layer for SetDetailLayer, indexed [zIndex, xIndex].</summary>
        public static int[,] BuildDetailLayer(float[,] heights, float[,,] splat, int detailRes, int seed, bool west, bool flowers)
        {
            var map = new int[detailRes, detailRes];
            int splatRes = splat.GetLength(0);
            for (int zi = 0; zi < detailRes; zi++)
            {
                float zn = (zi + 0.5f) / detailRes;
                float v = ZFromNormalized(zn);
                int sz = Mathf.Min(splatRes - 1, (int)(zn * splatRes));
                for (int xi = 0; xi < detailRes; xi++)
                {
                    float xn = (xi + 0.5f) / detailRes;
                    float u = OutwardFromNormalizedX(xn, west);
                    int sx = Mathf.Min(splatRes - 1, (int)(xn * splatRes));
                    float grass = splat[sz, sx, LayerGrass];
                    float slope = SlopeDegrees(heights, xn, zn);
                    float noise = flowers
                        ? Hash01(xi, zi, seed + 41)
                        : ValueNoise(u * 0.2f, v * 0.2f, seed + 43);
                    map[zi, xi] = flowers ? FlowerDensity(grass, slope, u, noise) : GrassDensity(grass, slope, u, noise);
                }
            }
            return map;
        }

        // ---- Budget helper ----------------------------------------------------------------------------

        /// <summary>Detail objects that Unity would draw at most: sum of density values in the map.</summary>
        public static long SumDetail(int[,] map)
        {
            long s = 0;
            foreach (int d in map) s += d;
            return s;
        }
    }
}
