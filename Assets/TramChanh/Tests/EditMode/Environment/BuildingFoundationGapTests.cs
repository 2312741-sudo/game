using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Environment
{
    public sealed class BuildingFoundationGapTests
    {
        private const float MaxAllowedGapAboveGround = 0.01f; // Ground level is y = 0.0m; building bottom must be <= ground + 0.01m
        private const float GroundLevelY = 0.0f;

        [TestCase("Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_A.prefab")]
        [TestCase("Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_B.prefab")]
        [TestCase("Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_C.prefab")]
        public void ShopfrontPrefabs_BottomReachesGroundOrBelow(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, $"Prefab not found at {prefabPath}");

            var mf = prefab.GetComponentInChildren<MeshFilter>();
            Assert.That(mf, Is.Not.Null, $"MeshFilter not found on {prefabPath}");
            Assert.That(mf.sharedMesh, Is.Not.Null, $"Mesh not assigned on {prefabPath}");

            float meshBottomY = mf.sharedMesh.bounds.min.y;
            Assert.That(meshBottomY, Is.LessThanOrEqualTo(GroundLevelY + MaxAllowedGapAboveGround),
                $"Building {prefab.name} mesh bottom ({meshBottomY:F3}m) exceeds ground + 0.01m threshold, leaving a black gap underneath!");
        }

        [Test]
        public void StreetExtendedPrefab_AllBuildingsBottomReachGroundOrBelow()
        {
            const string streetExtendedPath = "Assets/TramChanh/Prefabs/Environment/PF_TramChanh_StreetExtended.prefab";
            var streetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(streetExtendedPath);
            Assert.That(streetPrefab, Is.Not.Null, $"Street extended prefab not found at {streetExtendedPath}");

            var instance = Object.Instantiate(streetPrefab);
            try
            {
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                int checkedBuildings = 0;

                foreach (var renderer in renderers)
                {
                    string name = renderer.gameObject.name;
                    string parentName = renderer.transform.parent != null ? renderer.transform.parent.name : "";
                    bool isBuilding = name.Contains("Shopfront") || parentName.Contains("Shopfront") ||
                                      name.Contains("Alley_Walls");

                    if (!isBuilding) continue;

                    checkedBuildings++;
                    float worldBottomY = renderer.bounds.min.y;

                    Assert.That(worldBottomY, Is.LessThanOrEqualTo(GroundLevelY + MaxAllowedGapAboveGround),
                        $"Building instance '{name}' world bottom ({worldBottomY:F3}m) exceeds ground + 0.01m threshold!");
                }

                Assert.That(checkedBuildings, Is.GreaterThan(0), "No building renderers found to validate in extended street prefab!");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
