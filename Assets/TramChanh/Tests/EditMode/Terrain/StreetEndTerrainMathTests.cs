using NUnit.Framework;
using TramChanh.EditorTools;
using UnityEngine;

namespace TramChanh.Tests.EditMode.StreetEndTerrain
{
    /// <summary>Pure-logic tests for TerrainBuilderMath (no scene, no assets).</summary>
    public sealed class StreetEndTerrainMathTests
    {
        private const int Res = TerrainBuilderMath.HeightmapResolution;

        private static float[,] Heights(bool west) =>
            TerrainBuilderMath.BuildHeights(Res, west ? 1703 : 4219, west);

        // ---- Heights ----------------------------------------------------------------

        [TestCase(false)]
        [TestCase(true)]
        public void JointEdge_IsExactlyFlatNormalized_AllAlongZ(bool west)
        {
            float[,] h = Heights(west);
            int edgeX = west ? Res - 1 : 0;
            for (int z = 0; z < Res; z++)
                Assert.That(h[z, edgeX], Is.EqualTo(TerrainBuilderMath.FlatNormalized),
                    $"joint height differs at z index {z} (west={west})");
        }

        [Test]
        public void FlatNormalized_MapsToWorldYZero_AfterSixteenBitStorage()
        {
            // Unity stores heights as ushort with 32766 as the maximum.
            double stored = System.Math.Round(TerrainBuilderMath.FlatNormalized * 32766.0);
            double back = stored / 32766.0;
            double worldY = TerrainBuilderMath.TerrainOriginY + back * TerrainBuilderMath.TerrainSizeY;
            Assert.That(worldY, Is.EqualTo(0.0).Within(1e-9));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FlatBand_NextToRoad_StaysFlat(bool west)
        {
            float[,] h = Heights(west);
            float cell = TerrainBuilderMath.TerrainLength / (Res - 1);
            for (int z = 0; z < Res; z++)
            {
                for (int i = 0; i * cell <= TerrainBuilderMath.FlatBandMeters; i++)
                {
                    int x = west ? Res - 1 - i : i;
                    Assert.That(h[z, x], Is.EqualTo(TerrainBuilderMath.FlatNormalized), $"band not flat at i={i} z={z}");
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OuterBorders_ReturnToGroundLevel(bool west)
        {
            float[,] h = Heights(west);
            int farX = west ? 0 : Res - 1;
            for (int i = 0; i < Res; i++)
            {
                Assert.That(h[i, farX], Is.EqualTo(TerrainBuilderMath.FlatNormalized), "far edge");
                Assert.That(h[0, i], Is.EqualTo(TerrainBuilderMath.FlatNormalized), "z min edge");
                Assert.That(h[Res - 1, i], Is.EqualTo(TerrainBuilderMath.FlatNormalized), "z max edge");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Heights_StayInRange_AndHaveRelief(bool west)
        {
            float[,] h = Heights(west);
            float min = 1f, max = 0f;
            foreach (float v in h)
            {
                Assert.That(v, Is.InRange(0f, 1f));
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }
            float maxMeters = TerrainBuilderMath.NormalizedToMeters(max);
            float minMeters = TerrainBuilderMath.NormalizedToMeters(min);
            Assert.That(maxMeters, Is.LessThanOrEqualTo(TerrainBuilderMath.AmplitudeMeters));
            Assert.That(minMeters, Is.GreaterThanOrEqualTo(-TerrainBuilderMath.AmplitudeMeters * TerrainBuilderMath.NoiseBias - 1e-3f));
            Assert.That(maxMeters - minMeters, Is.GreaterThan(0.5f), "terrain is flat, no relief");
        }

        [Test]
        public void Heights_AreDeterministic()
        {
            float[,] a = Heights(false), b = Heights(false);
            for (int z = 0; z < Res; z++)
                for (int x = 0; x < Res; x++)
                    Assert.That(a[z, x], Is.EqualTo(b[z, x]));
        }

        [Test]
        public void WestIsMirrorOfEast_ForSameSeed()
        {
            float[,] e = TerrainBuilderMath.BuildHeights(Res, 77, false);
            float[,] w = TerrainBuilderMath.BuildHeights(Res, 77, true);
            for (int z = 0; z < Res; z += 7)
                for (int x = 0; x < Res; x += 7)
                    Assert.That(w[z, Res - 1 - x], Is.EqualTo(e[z, x]).Within(1e-6f));
        }

        [Test]
        public void Corridor_AlongRoad_IsCalmerThanSides()
        {
            // same outward distance, one point on the road axis, one 25 m away
            float u = 50f;
            float vRoad = TerrainBuilderMath.RoadCenterZ - TerrainBuilderMath.TerrainOriginZ;
            Assert.That(TerrainBuilderMath.HillMask(u, vRoad), Is.LessThan(0.2f));
            Assert.That(TerrainBuilderMath.HillMask(u, vRoad + 25f), Is.GreaterThan(0.9f));
        }

        // ---- Slope -------------------------------------------------------------------

        [Test]
        public void Slope_OfFlatGround_IsZero()
        {
            var h = new float[Res, Res];
            for (int z = 0; z < Res; z++) for (int x = 0; x < Res; x++) h[z, x] = 0.5f;
            Assert.That(TerrainBuilderMath.SlopeDegrees(h, 0.5f, 0.5f), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Slope_OfKnownRamp_MatchesAtan()
        {
            // rise of 0.5 m per metre along x: heightNormalized = 0.5 * length / sizeY * xNorm
            var h = new float[Res, Res];
            float perMetre = 0.5f;
            for (int z = 0; z < Res; z++)
                for (int x = 0; x < Res; x++)
                    h[z, x] = perMetre * TerrainBuilderMath.TerrainLength * (x / (float)(Res - 1)) / TerrainBuilderMath.TerrainSizeY;
            float expected = Mathf.Atan(perMetre) * Mathf.Rad2Deg;   // 26.57 degrees
            Assert.That(TerrainBuilderMath.SlopeDegrees(h, 0.5f, 0.5f), Is.EqualTo(expected).Within(0.2f));
        }

        // ---- Splat map -----------------------------------------------------------------

        [Test]
        public void SplatWeights_SumToOne_AndAreNonNegative()
        {
            var w = new float[TerrainBuilderMath.LayerCount];
            for (float slope = 0; slope <= 60; slope += 7.5f)
                for (float hm = -1; hm <= 4; hm += 0.75f)
                    for (float u = 0; u <= 96; u += 12)
                        for (float dz = 0; dz <= 40; dz += 8)
                        {
                            TerrainBuilderMath.SplatWeights(slope, hm, u, dz, 0.5f, w);
                            float sum = 0;
                            foreach (float v in w) { Assert.That(v, Is.GreaterThanOrEqualTo(0f)); sum += v; }
                            Assert.That(sum, Is.EqualTo(1f).Within(1e-4f));
                        }
        }

        [Test]
        public void SplatWeights_SteepSlope_IsMostlyDirt()
        {
            var w = new float[TerrainBuilderMath.LayerCount];
            TerrainBuilderMath.SplatWeights(35f, 1f, 60f, 30f, 0.5f, w);
            Assert.That(w[TerrainBuilderMath.LayerDirt], Is.GreaterThan(0.6f));
        }

        [Test]
        public void SplatWeights_LowFlatGround_IsMostlyGrass()
        {
            var w = new float[TerrainBuilderMath.LayerCount];
            TerrainBuilderMath.SplatWeights(2f, 0.3f, 60f, 30f, 0.5f, w);
            Assert.That(w[TerrainBuilderMath.LayerGrass], Is.GreaterThan(0.6f));
        }

        [Test]
        public void SplatWeights_RoadBedBehindGate_IsGravel()
        {
            var w = new float[TerrainBuilderMath.LayerCount];
            TerrainBuilderMath.SplatWeights(0f, 0f, 4f, 1f, 0.5f, w);
            Assert.That(w[TerrainBuilderMath.LayerGravel], Is.GreaterThan(0.6f));
        }

        [Test]
        public void SplatWeights_HighGround_IsMostlyGravel()
        {
            var w = new float[TerrainBuilderMath.LayerCount];
            TerrainBuilderMath.SplatWeights(5f, 3.4f, 60f, 30f, 0.5f, w);
            Assert.That(w[TerrainBuilderMath.LayerGravel], Is.GreaterThan(0.5f));
        }

        [Test]
        public void BuildSplat_EveryCellSumsToOne_AndAllThreeLayersAppear()
        {
            float[,] h = Heights(false);
            int res = TerrainBuilderMath.AlphamapResolution;
            float[,,] map = TerrainBuilderMath.BuildSplat(h, res, 4219, false);
            var present = new bool[TerrainBuilderMath.LayerCount];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    float sum = 0, best = -1; int arg = 0;
                    for (int l = 0; l < TerrainBuilderMath.LayerCount; l++)
                    {
                        float v = map[z, x, l];
                        sum += v;
                        if (v > best) { best = v; arg = l; }
                    }
                    Assert.That(sum, Is.EqualTo(1f).Within(1e-3f));
                    present[arg] = true;
                }
            Assert.That(present[TerrainBuilderMath.LayerGrass], "no grass-dominant cell");
            Assert.That(present[TerrainBuilderMath.LayerGravel], "no gravel-dominant cell");
        }

        // ---- Trees ----------------------------------------------------------------------

        [TestCase(false)]
        [TestCase(true)]
        public void Trees_RespectBudgetAndRules(bool west)
        {
            float[,] h = Heights(west);
            var trees = TerrainBuilderMath.PlaceTrees(h, 1703, west, 0.45f);
            Assert.That(trees.Count, Is.InRange(1, TerrainBuilderMath.MaxTreesPerTerrain));
            foreach (var t in trees)
            {
                Assert.That(t.XNorm, Is.InRange(0f, 1f));
                Assert.That(t.ZNorm, Is.InRange(0f, 1f));
                float u = TerrainBuilderMath.OutwardFromNormalizedX(t.XNorm, west);
                float dz = Mathf.Abs(t.ZNorm * TerrainBuilderMath.TerrainWidth + TerrainBuilderMath.TerrainOriginZ - TerrainBuilderMath.RoadCenterZ);
                Assert.That(u, Is.GreaterThanOrEqualTo(TerrainBuilderMath.TreeMinOutward), "tree too close to the gate");
                if (u < TerrainBuilderMath.TreeCorridorUntil)
                    Assert.That(dz, Is.GreaterThanOrEqualTo(TerrainBuilderMath.TreeCorridorHalfWidth), "tree in the view corridor");
                Assert.That(TerrainBuilderMath.SlopeDegrees(h, t.XNorm, t.ZNorm), Is.LessThanOrEqualTo(TerrainBuilderMath.TreeMaxSlopeDegrees + 0.01f));
                Assert.That(t.Scale, Is.InRange(0.8f, 1.4f));
            }
        }

        [Test]
        public void Trees_ZeroDensity_GivesNone()
        {
            Assert.That(TerrainBuilderMath.PlaceTrees(Heights(false), 1, false, 0f), Is.Empty);
        }

        [Test]
        public void Trees_DensityScalesCount_AndIsDeterministic()
        {
            float[,] h = Heights(false);
            int sparse = TerrainBuilderMath.PlaceTrees(h, 9, false, 0.15f).Count;
            int dense = TerrainBuilderMath.PlaceTrees(h, 9, false, 0.9f).Count;
            Assert.That(dense, Is.GreaterThan(sparse));
            Assert.That(TerrainBuilderMath.PlaceTrees(h, 9, false, 0.9f).Count, Is.EqualTo(dense));
        }

        // ---- Details --------------------------------------------------------------------

        [Test]
        public void GrassDensity_OnlyOnGrass_NotOnSteepOrJointStrip()
        {
            Assert.That(TerrainBuilderMath.GrassDensity(1f, 5f, 40f, 1f), Is.InRange(1, TerrainBuilderMath.MaxGrassDensity));
            Assert.That(TerrainBuilderMath.GrassDensity(0.2f, 5f, 40f, 1f), Is.EqualTo(0), "no grass on dirt/gravel");
            Assert.That(TerrainBuilderMath.GrassDensity(1f, 40f, 40f, 1f), Is.EqualTo(0), "no grass on steep slope");
            Assert.That(TerrainBuilderMath.GrassDensity(1f, 5f, 1f, 1f), Is.EqualTo(0), "no grass on the joint strip");
        }

        [Test]
        public void FlowerDensity_IsRare()
        {
            Assert.That(TerrainBuilderMath.FlowerDensity(1f, 5f, 40f, 0.5f), Is.EqualTo(0));
            Assert.That(TerrainBuilderMath.FlowerDensity(1f, 5f, 40f, 0.97f), Is.EqualTo(TerrainBuilderMath.MaxFlowerDensity));
            Assert.That(TerrainBuilderMath.FlowerDensity(0.3f, 5f, 40f, 0.97f), Is.EqualTo(0));
        }

        [Test]
        public void DetailBudget_StaysLow()
        {
            float[,] h = Heights(false);
            float[,,] splat = TerrainBuilderMath.BuildSplat(h, TerrainBuilderMath.AlphamapResolution, 4219, false);
            int res = TerrainBuilderMath.DetailResolution;
            int[,] grass = TerrainBuilderMath.BuildDetailLayer(h, splat, res, 4219, false, false);
            int[,] flowers = TerrainBuilderMath.BuildDetailLayer(h, splat, res, 4220, false, true);
            foreach (int d in grass) Assert.That(d, Is.InRange(0, TerrainBuilderMath.MaxGrassDensity));
            foreach (int d in flowers) Assert.That(d, Is.InRange(0, TerrainBuilderMath.MaxFlowerDensity));
            // low density: on average under 1.5 blades per detail cell (cell is about 0.75 m)
            Assert.That(TerrainBuilderMath.SumDetail(grass), Is.LessThan(res * res * 3 / 2));
            Assert.That(TerrainBuilderMath.SumDetail(flowers), Is.LessThan(res * res / 10));
        }
    }
}
