using System;
using System.Collections.Generic;
using System.IO;
using TramChanh.Core.GroundTruth;
using TramChanh.Core.Provisional;
using TramChanh.Stall.Anchors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools
{
    public static class ExtendedStreetBuilder
    {
        public const string MaterialsFolder = "Assets/TramChanh/Art/Materials";
        public const string EnvironmentModelsFolder = "Assets/TramChanh/Art/Models/Environment";

        public const string BaseStreetPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_TramChanh_Street.prefab";
        public const string ExtendedStreetPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_TramChanh_StreetExtended.prefab";

        public const float StreetHalfLength = 250f;
        public static readonly float[] AlleyPositions = new float[] { -221.5f, -101.5f, 18.5f, 138.5f };

        public const string StallPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Stall_TramChanh.prefab";
        public const string SignPrefabPath = "Assets/TramChanh/Prefabs/Stall/PF_Sign_TramChanh_New.prefab";
        public const string YellowCrateTablePrefabPath = "Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab";
        public const string CustomerArea10TablesPrefabPath = "Assets/TramChanh/Prefabs/CustomerArea/PF_CustomerArea_10Tables.prefab";

        public const string ShopfrontAPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_A.prefab";
        public const string ShopfrontBPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_B.prefab";
        public const string ShopfrontCPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_VietnameseShopfront_C.prefab";
        public const string StreetTreePrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_StreetTree.prefab";
        public const string StreetLampPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_StreetLamp.prefab";
        public const string UtilityPolePrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_UtilityPole.prefab";
        public const string MotorbikePrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Motorbike.prefab";
        public const string SedanPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Car_Sedan.prefab";
        public const string StreetDogPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Street_Dog.prefab";
        public const string StreetCatPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Street_Cat.prefab";
        public const string StreetChickenPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Street_Chicken.prefab";
        public const string StreetPigeonPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Street_Pigeon.prefab";
        public const string StreetVendorCartPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_StreetVendor_Cart.prefab";
        public const string SidewalkUmbrellaPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Sidewalk_Umbrella.prefab";
        public const string ShopfrontCanopyPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Shopfront_Canopy.prefab";
        public const string StreetPostLanternPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Street_PostLantern.prefab";
        public const string PlasticTableChairsVNPrefabPath = "Assets/TramChanh/Prefabs/Furniture/PF_PlasticTableChairs_VN.prefab";
        public const string PlasticCrateStreetPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_PlasticCrate_Street.prefab";
        public const string BicyclePrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Bicycle.prefab";
        public const string MotorcycleCubPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Motorcycle_Cub.prefab";
        public const string TruckSmallPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Truck_Small.prefab";
        public const string CarSUVPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_Car_SUV.prefab";
        public const string Person07PrefabPath = "Assets/TramChanh/Prefabs/People/PF_Person_07.prefab";
        public const string Person08PrefabPath = "Assets/TramChanh/Prefabs/People/PF_Person_08.prefab";
        public const string TeaBagPrefabPath = "Assets/TramChanh/Prefabs/Items/PF_TeaBag_PrePortioned.prefab";

        public const string ExtendedPreviewScenePath = "Assets/TramChanh/Scenes/Art/SCN_Art_EnvironmentExtendedPreview.unity";
        public const string ScreenshotsFolder = "QA/Screenshots/Map_Extended";

        private struct BuildingSlot
        {
            public float x;
            public float z;
            public float rotY;
            public int type; // 0=A, 1=B, 2=C
        }

        private struct BikeSlot
        {
            public float x;
            public float z;
            public float rotY;
        }

        private struct TableApproachSpec
        {
            public string name;
            public Vector3 pos;
            public Vector3 lookDir;
        }

        private struct CameraShot
        {
            public string filename;
            public Vector3 pos;
            public Vector3 target;
            public float fov;
        }

        [MenuItem("Tram Chanh/Art/Build Extended Street (ART-MAP-002)")]
        public static void BuildAll()
        {
            Debug.Log("[TramChanh] Starting Extended Street Construction (ART-MAP-002)...");
            ConfigureProjectAndBuildSettings();
            EnsureModelsImported();
            BuildBaseStreetPrefab();
            BuildExtendedStreetPrefab();
            CreatePreviewSceneAndCapture();
            Debug.Log("[TramChanh] Extended Street Construction (ART-MAP-002) Complete!");
        }

        [MenuItem("Tram Chanh/Art/Build Extended Street Prefabs Only")]
        public static void BuildPrefabsOnly()
        {
            EnsureModelsImported();
            BuildBaseStreetPrefab();
            BuildExtendedStreetPrefab();
            Debug.Log("[TramChanh] Extended Street Prefabs Built Successfully!");
        }

        public static void ConfigureProjectAndBuildSettings()
        {
            TramChanh.EditorTools.ProjectSetup.TramChanhProjectSetup.Apply(false);
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in new[] { TramChanhSceneMenu.Main, TramChanhSceneMenu.PlayableDrinkWave, TramChanhSceneMenu.Blockout })
            {
                if (File.Exists(path)) { scenes.Add(new EditorBuildSettingsScene(path, true)); }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static void EnsureModelsImported()
        {
            string[] models = new[]
            {
                "SM_Sidewalk_Extended_80m.obj",
                "SM_North_Sidewalk_80m.obj",
                "SM_Asphalt_Road_80m.obj",
                "SM_Alley_Extended.obj",
                "SM_Alley_Walls.obj"
            };

            foreach (var m in models)
            {
                string path = $"{EnvironmentModelsFolder}/{m}";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
        }

        public static Mesh LoadMesh(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Mesh direct = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (direct != null) return direct;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var sub in subAssets)
            {
                if (sub is Mesh m) return m;
            }
            return null;
        }

        public static void BuildBaseStreetPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(BaseStreetPrefabPath));

            GameObject root = new GameObject("PF_TramChanh_Street");
            root.layer = TramChanhLayers.EnvironmentIndex;

            var placeholder = root.AddComponent<PlaceholderAsset>();
            placeholder.Configure("ART-MAP-002", "", "PF_TramChanh_Street base street module with provisional service zone and composition anchors.");

            PopulateServiceZone(root.transform);

            // Base Roadside module
            var roadside = new GameObject("Roadside");
            roadside.layer = TramChanhLayers.EnvironmentIndex;
            roadside.transform.SetParent(root.transform, false);

            var sidewalk = new GameObject("Sidewalk");
            sidewalk.layer = TramChanhLayers.EnvironmentIndex;
            sidewalk.transform.SetParent(roadside.transform, false);
            sidewalk.transform.localPosition = Vector3.zero;
            Mesh swMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Sidewalk_Extended.obj");
            Material swMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_SidewalkTiles.mat");
            if (swMesh != null)
            {
                sidewalk.AddComponent<MeshFilter>().sharedMesh = swMesh;
                sidewalk.AddComponent<MeshRenderer>().sharedMaterial = swMat;
            }
            var swCol = sidewalk.AddComponent<BoxCollider>();
            swCol.center = new Vector3(0.0f, -0.075f, -3.2f);
            swCol.size = new Vector3(36.0f, 0.15f, 13.6f);

            var street = new GameObject("Street");
            street.layer = TramChanhLayers.EnvironmentIndex;
            street.transform.SetParent(roadside.transform, false);
            street.transform.localPosition = Vector3.zero;
            Mesh rdMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Asphalt_Road_Extended.obj");
            Material rdMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_WetAsphaltNight.mat");
            if (rdMesh != null)
            {
                street.AddComponent<MeshFilter>().sharedMesh = rdMesh;
                street.AddComponent<MeshRenderer>().sharedMaterial = rdMat;
            }
            var rdCol = street.AddComponent<BoxCollider>();
            rdCol.center = new Vector3(0.0f, -0.175f, 8.8f);
            rdCol.size = new Vector3(36.0f, 0.05f, 10.4f);

            // Neighbourhood backdrop
            var backdrop = new GameObject("NeighbourhoodBackdrop");
            backdrop.layer = TramChanhLayers.EnvironmentIndex;
            backdrop.transform.SetParent(root.transform, false);
            PopulateModularBuildings(backdrop.transform, -10f, 10f, false);

            // Lighting
            var lighting = new GameObject("NightLighting");
            lighting.layer = TramChanhLayers.EnvironmentIndex;
            lighting.transform.SetParent(root.transform, false);

            var keyLight = new GameObject("Streetlight").AddComponent<Light>();
            keyLight.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            keyLight.transform.SetParent(lighting.transform, false);
            keyLight.transform.position = new Vector3(-0.6f, 5.8f, 2.5f);
            keyLight.type = LightType.Point;
            keyLight.range = 22f;
            keyLight.color = new Color(1.0f, 0.86f, 0.65f);
            keyLight.intensity = 3.2f;
            keyLight.shadows = LightShadows.Soft;

            var fillLight = new GameObject("SkyFill").AddComponent<Light>();
            fillLight.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            fillLight.transform.SetParent(lighting.transform, false);
            fillLight.transform.rotation = Quaternion.Euler(55f, -140f, 0f);
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.35f, 0.45f, 0.70f);
            fillLight.intensity = 1.5f;
            fillLight.shadows = LightShadows.None;

            PrefabUtility.SaveAsPrefabAsset(root, BaseStreetPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[TramChanh] Created Base Street Prefab: " + BaseStreetPrefabPath);
        }

        public static void BuildExtendedStreetPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(ExtendedStreetPrefabPath));

            GameObject root = new GameObject("PF_TramChanh_StreetExtended");
            root.layer = TramChanhLayers.EnvironmentIndex;

            var placeholder = root.AddComponent<PlaceholderAsset>();
            placeholder.Configure("ART-MAP-002", "", "PF_TramChanh_StreetExtended high-fidelity street expansion x in [-40, 40] with provisional Walkways graph and boundary colliders.");

            // 1. Service Zone Anchors & Content (Preserving exact positions & hierarchy)
            PopulateServiceZone(root.transform);

            // 2. Base Roadside node for audit contract compatibility
            var roadside = new GameObject("Roadside");
            roadside.layer = TramChanhLayers.EnvironmentIndex;
            roadside.transform.SetParent(root.transform, false);

            var swCore = new GameObject("Sidewalk");
            swCore.layer = TramChanhLayers.EnvironmentIndex;
            swCore.transform.SetParent(roadside.transform, false);
            var swCoreCol = swCore.AddComponent<BoxCollider>();
            swCoreCol.center = new Vector3(0.0f, -0.075f, 0.0f);
            swCoreCol.size = new Vector3(16.0f, 0.15f, 7.2f);

            var stCore = new GameObject("Street");
            stCore.layer = TramChanhLayers.EnvironmentIndex;
            stCore.transform.SetParent(roadside.transform, false);
            var stCoreCol = stCore.AddComponent<BoxCollider>();
            stCoreCol.center = new Vector3(0.0f, -0.175f, 8.1f);
            stCoreCol.size = new Vector3(16.0f, 0.05f, 9.0f);

            // 3. Extended Ground & Geometry (x in [-40, 40])
            var extendedGround = new GameObject("ExtendedGround");
            extendedGround.layer = TramChanhLayers.EnvironmentIndex;
            extendedGround.transform.SetParent(root.transform, false);

            Mesh sw80Mesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Sidewalk_Extended_80m.obj");
            Mesh nsw80Mesh = LoadMesh($"{EnvironmentModelsFolder}/SM_North_Sidewalk_80m.obj");
            Mesh rd80Mesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Asphalt_Road_80m.obj");
            Mesh alMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Alley_Extended.obj");
            Mesh alWallMesh = LoadMesh($"{EnvironmentModelsFolder}/SM_Alley_Walls.obj");

            Material swMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_SidewalkTiles.mat");
            Material rdMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Street_WetAsphaltNight.mat");
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Shopfront_C.mat");

            // South Sidewalk (x in [-StreetHalfLength, StreetHalfLength])
            if (sw80Mesh != null)
            {
                var s80 = new GameObject("SouthSidewalk_80m");
                s80.layer = TramChanhLayers.EnvironmentIndex;
                s80.transform.SetParent(extendedGround.transform, false);
                s80.AddComponent<MeshFilter>().sharedMesh = sw80Mesh;
                s80.AddComponent<MeshRenderer>().sharedMaterial = swMat;

                // BoxColliders covering the walkable south sidewalk surface (top at y = 0.0m) across [-250, 250]
                float xPrev = -StreetHalfLength;
                int segIdx = 1;
                foreach (float al in AlleyPositions)
                {
                    float xStartAl = al - 2.0f;
                    float xEndAl = al + 2.0f;
                    float w = xStartAl - xPrev;
                    if (w > 0.05f)
                    {
                        var colSeg = s80.AddComponent<BoxCollider>();
                        colSeg.center = new Vector3(xPrev + w * 0.5f, -0.075f, -3.2f);
                        colSeg.size = new Vector3(w, 0.15f, 13.6f);
                        segIdx++;
                    }
                    var colThresh = s80.AddComponent<BoxCollider>();
                    colThresh.center = new Vector3(al, -0.075f, -3.2f);
                    colThresh.size = new Vector3(4.0f, 0.15f, 13.6f);

                    xPrev = xEndAl;
                }
                float lastW = StreetHalfLength - xPrev;
                if (lastW > 0.05f)
                {
                    var colSeg = s80.AddComponent<BoxCollider>();
                    colSeg.center = new Vector3(xPrev + lastW * 0.5f, -0.075f, -3.2f);
                    colSeg.size = new Vector3(lastW, 0.15f, 13.6f);
                }
            }

            // North Sidewalk (x in [-StreetHalfLength, StreetHalfLength], z in [12.6, 22.0])
            if (nsw80Mesh != null)
            {
                var n80 = new GameObject("NorthSidewalk_80m");
                n80.layer = TramChanhLayers.EnvironmentIndex;
                n80.transform.SetParent(extendedGround.transform, false);
                n80.AddComponent<MeshFilter>().sharedMesh = nsw80Mesh;
                n80.AddComponent<MeshRenderer>().sharedMaterial = swMat;

                var colN = n80.AddComponent<BoxCollider>();
                colN.center = new Vector3(0.0f, -0.075f, 17.3f);
                colN.size = new Vector3(StreetHalfLength * 2f, 0.15f, 9.4f);
            }

            // 2-lane Roadway (x in [-StreetHalfLength, StreetHalfLength], z in [3.6, 12.6], y = -0.15m)
            if (rd80Mesh != null)
            {
                var r80 = new GameObject("Roadway_80m");
                r80.layer = TramChanhLayers.EnvironmentIndex;
                r80.transform.SetParent(extendedGround.transform, false);
                r80.AddComponent<MeshFilter>().sharedMesh = rd80Mesh;
                r80.AddComponent<MeshRenderer>().sharedMaterial = rdMat;

                var colR = r80.AddComponent<BoxCollider>();
                colR.center = new Vector3(0.0f, -0.185f, 8.1f);
                colR.size = new Vector3(StreetHalfLength * 2f, 0.07f, 9.0f);
            }

            // Side Alley Pavements (4 alleys every 120m, top at y = 0.0m)
            if (alMesh != null)
            {
                var alObj = new GameObject("Alley_Pavement");
                alObj.layer = TramChanhLayers.EnvironmentIndex;
                alObj.transform.SetParent(extendedGround.transform, false);
                alObj.AddComponent<MeshFilter>().sharedMesh = alMesh;
                alObj.AddComponent<MeshRenderer>().sharedMaterial = swMat;

                foreach (float al in AlleyPositions)
                {
                    var colAl = alObj.AddComponent<BoxCollider>();
                    colAl.center = new Vector3(al, -0.075f, -9.4f);
                    colAl.size = new Vector3(4.0f, 0.15f, 11.2f);
                }
            }

            // Side Alley Walls
            if (alWallMesh != null)
            {
                var alWallObj = new GameObject("Alley_Walls");
                alWallObj.layer = TramChanhLayers.EnvironmentIndex;
                alWallObj.transform.SetParent(extendedGround.transform, false);
                alWallObj.AddComponent<MeshFilter>().sharedMesh = alWallMesh;
                alWallObj.AddComponent<MeshRenderer>().sharedMaterial = wallMat;
            }

            // 4. Playable Limits & Invisible Boundary Colliders (Layer 8, NOT Layer 9)
            var limits = new GameObject("PlayableLimits");
            limits.layer = TramChanhLayers.EnvironmentIndex;
            limits.transform.SetParent(root.transform, false);

            // A low curb, not a wall: the player (step offset 0.25 m) can walk from the sidewalk onto the road.
            AddLimitBox(limits.transform, "Boundary_RoadCurb", new Vector3(0.0f, 0.06f, 3.65f), new Vector3(StreetHalfLength * 2f, 0.12f, 0.1f));
            AddLimitBox(limits.transform, "Boundary_NorthSidewalk_Back", new Vector3(0.0f, 1.5f, 16.65f), new Vector3(StreetHalfLength * 2f, 3.0f, 0.1f));

            // South buildings boundaries
            float xLimPrev = -StreetHalfLength;
            int limIdx = 1;
            foreach (float al in AlleyPositions)
            {
                float xStartAl = al - 2.0f;
                float xEndAl = al + 2.0f;
                float w = xStartAl - xLimPrev;
                if (w > 0.05f)
                {
                    AddLimitBox(limits.transform, $"Boundary_SouthBuildings_{limIdx}", new Vector3(xLimPrev + w * 0.5f, 1.5f, -3.85f), new Vector3(w, 3.0f, 0.1f));
                    limIdx++;
                }
                xLimPrev = xEndAl;
            }
            float lastLimW = StreetHalfLength - xLimPrev;
            if (lastLimW > 0.05f)
            {
                AddLimitBox(limits.transform, $"Boundary_SouthBuildings_{limIdx}", new Vector3(xLimPrev + lastLimW * 0.5f, 1.5f, -3.85f), new Vector3(lastLimW, 3.0f, 0.1f));
            }

            // Flanking boundaries for each alley
            for (int i = 0; i < AlleyPositions.Length; i++)
            {
                float al = AlleyPositions[i];
                string suffix = i == 2 ? "" : $"_{i + 1}"; // Alley at index 2 (18.5) retains canonical Boundary_Alley_West/East/Back
                AddLimitBox(limits.transform, $"Boundary_Alley_West{suffix}", new Vector3(al - 2.05f, 1.5f, -9.4f), new Vector3(0.1f, 3.0f, 11.2f));
                AddLimitBox(limits.transform, $"Boundary_Alley_East{suffix}", new Vector3(al + 2.05f, 1.5f, -9.4f), new Vector3(0.1f, 3.0f, 11.2f));
                AddLimitBox(limits.transform, $"Boundary_Alley_Back{suffix}", new Vector3(al, 1.5f, -15.05f), new Vector3(4.2f, 3.0f, 0.1f));
            }

            // Map ends: blocking at +/-250m spanning from sidewalk (z=-3.9) to north sidewalk (z=16.65)
            AddLimitBox(limits.transform, "Boundary_MapEnd_West", new Vector3(-StreetHalfLength - 0.05f, 2.5f, 6.5f), new Vector3(0.1f, 5.0f, 22.0f));
            AddLimitBox(limits.transform, "Boundary_MapEnd_East", new Vector3(StreetHalfLength + 0.05f, 2.5f, 6.5f), new Vector3(0.1f, 5.0f, 22.0f));

            // 5. Extended Buildings & Architecture
            var buildings = new GameObject("ExtendedBuildings");
            buildings.layer = TramChanhLayers.EnvironmentIndex;
            buildings.transform.SetParent(root.transform, false);
            PopulateModularBuildings(buildings.transform, -StreetHalfLength, StreetHalfLength, true);

            // 6. Street Props & Night Lighting
            var props = new GameObject("StreetProps");
            props.layer = TramChanhLayers.EnvironmentIndex;
            props.transform.SetParent(root.transform, false);
            PopulateStreetProps(props.transform);

            var lighting = new GameObject("NightLighting");
            lighting.layer = TramChanhLayers.EnvironmentIndex;
            lighting.transform.SetParent(root.transform, false);
            PopulateNightLighting(lighting.transform);

            // 7. Walkways & Waypoint Graph (Section 5 Contract)
            BuildWalkwaysGraph(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, ExtendedStreetPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[TramChanh] Created Extended Street Prefab: " + ExtendedStreetPrefabPath);
        }

        private static void PopulateServiceZone(Transform root)
        {
            // StallRoot with canonical nested stall
            var stallRoot = new GameObject("StallRoot");
            stallRoot.layer = TramChanhLayers.EnvironmentIndex;
            stallRoot.transform.SetParent(root, false);
            stallRoot.transform.localPosition = Vector3.zero;
            stallRoot.transform.localScale = Vector3.one;

            GameObject stallPf = AssetDatabase.LoadAssetAtPath<GameObject>(StallPrefabPath);
            if (stallPf != null)
            {
                var stallInst = (GameObject)PrefabUtility.InstantiatePrefab(stallPf, stallRoot.transform);
                stallInst.transform.localPosition = Vector3.zero;
                stallInst.transform.localRotation = Quaternion.identity;
                stallInst.transform.localScale = Vector3.one;
            }

            // PlayerSpawn
            var playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.layer = TramChanhLayers.EnvironmentIndex;
            playerSpawn.transform.SetParent(root, false);
            playerSpawn.transform.localPosition = new Vector3(0f, 0f, -1.2f);
            playerSpawn.transform.localScale = Vector3.one;

            // LobbyPosition
            var lobbyPos = new GameObject("LobbyPosition");
            lobbyPos.layer = TramChanhLayers.EnvironmentIndex;
            lobbyPos.transform.SetParent(root, false);
            lobbyPos.transform.localPosition = new Vector3(0f, 0f, 1.6f);
            lobbyPos.transform.localScale = Vector3.one;

            // ReadyHandoff
            var readyHandoff = new GameObject("ReadyHandoff");
            readyHandoff.layer = TramChanhLayers.EnvironmentIndex;
            readyHandoff.transform.SetParent(root, false);
            readyHandoff.transform.localPosition = new Vector3(0.5f, 0.95f, 0.35f);
            readyHandoff.transform.localScale = Vector3.one;

            // TablePoint (with nested YellowCrateTable + DeliveryPoint)
            var tablePoint = new GameObject("TablePoint");
            tablePoint.layer = TramChanhLayers.EnvironmentIndex;
            tablePoint.transform.SetParent(root, false);
            tablePoint.transform.localPosition = new Vector3(-3.35f, 0f, 0.20f);
            tablePoint.transform.localScale = Vector3.one;
            GameObject cratePf = AssetDatabase.LoadAssetAtPath<GameObject>(YellowCrateTablePrefabPath);
            if (cratePf != null)
            {
                var crateInst = (GameObject)PrefabUtility.InstantiatePrefab(cratePf, tablePoint.transform);
                crateInst.transform.localPosition = Vector3.zero;
                crateInst.transform.localRotation = Quaternion.identity;
                crateInst.transform.localScale = Vector3.one;
            }

            // CakeTablePoint (with nested YellowCrateTable + DeliveryPoint)
            var cakeTablePoint = new GameObject("CakeTablePoint");
            cakeTablePoint.layer = TramChanhLayers.EnvironmentIndex;
            cakeTablePoint.transform.SetParent(root, false);
            cakeTablePoint.transform.localPosition = new Vector3(-4.60f, 0f, 1.70f);
            cakeTablePoint.transform.localScale = Vector3.one;
            if (cratePf != null)
            {
                var crateInst = (GameObject)PrefabUtility.InstantiatePrefab(cratePf, cakeTablePoint.transform);
                crateInst.transform.localPosition = Vector3.zero;
                crateInst.transform.localRotation = Quaternion.identity;
                crateInst.transform.localScale = Vector3.one;
            }

            // VehiclePoint (with GenericVehicleVisual and TakeawayCustomer)
            var vehiclePoint = new GameObject("VehiclePoint");
            vehiclePoint.layer = TramChanhLayers.EnvironmentIndex;
            vehiclePoint.transform.SetParent(root, false);
            // Kerb side in front of the stall (a rider pulls in off lane RoadEast and stops here), clear of the tea rack row at z = -1.5.
            vehiclePoint.transform.localPosition = new Vector3(0.3f, 0f, 4.6f);
            vehiclePoint.transform.localScale = Vector3.one;

            var vehVisual = new GameObject("GenericVehicleVisual");
            vehVisual.layer = TramChanhLayers.EnvironmentIndex;
            vehVisual.transform.SetParent(vehiclePoint.transform, false);
            vehVisual.transform.localPosition = new Vector3(0f, 0f, 3.0f);

            var takeawayCust = new GameObject("TakeawayCustomer");
            takeawayCust.layer = TramChanhLayers.EnvironmentIndex;
            takeawayCust.transform.SetParent(vehiclePoint.transform, false);
            takeawayCust.transform.localPosition = new Vector3(0f, 0f, 3.0f);

            // CustomerArea with 10 tables
            GameObject custAreaPf = AssetDatabase.LoadAssetAtPath<GameObject>(CustomerArea10TablesPrefabPath);
            if (custAreaPf != null)
            {
                var custInst = (GameObject)PrefabUtility.InstantiatePrefab(custAreaPf, root);
                custInst.name = "CustomerArea";
                custInst.transform.localPosition = Vector3.zero;
                custInst.transform.localRotation = Quaternion.identity;
                custInst.transform.localScale = Vector3.one;
            }

            // DineInCustomer & CakeDineInCustomer
            var dineInCust = new GameObject("DineInCustomer");
            dineInCust.layer = TramChanhLayers.EnvironmentIndex;
            dineInCust.transform.SetParent(root, false);
            dineInCust.transform.localPosition = new Vector3(-3.35f, 0f, 0.20f);

            var cakeDineInCust = new GameObject("CakeDineInCustomer");
            cakeDineInCust.layer = TramChanhLayers.EnvironmentIndex;
            cakeDineInCust.transform.SetParent(root, false);
            cakeDineInCust.transform.localPosition = new Vector3(-4.60f, 0f, 1.70f);
            var seatedBody = new GameObject("SeatedBody");
            seatedBody.layer = TramChanhLayers.EnvironmentIndex;
            seatedBody.transform.SetParent(cakeDineInCust.transform, false);

            // MenuBoard & SnackDisplayPlaceholder
            var menuBoard = new GameObject("MenuBoard");
            menuBoard.layer = TramChanhLayers.EnvironmentIndex;
            menuBoard.transform.SetParent(root, false);
            menuBoard.transform.localPosition = new Vector3(1.05f, 1.15f, 0.44f);
            menuBoard.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var menuQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            menuQuad.name = "BoardVisual";
            menuQuad.layer = TramChanhLayers.EnvironmentIndex;
            menuQuad.transform.SetParent(menuBoard.transform, false);
            menuQuad.transform.localScale = new Vector3(0.70f, 0.70f, 1.0f);
            Material menuMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/MAT_Menu_Board.mat");
            if (menuMat != null) menuQuad.GetComponent<MeshRenderer>().sharedMaterial = menuMat;
            Object.DestroyImmediate(menuQuad.GetComponent<Collider>());

            var snackDisplay = new GameObject("SnackDisplayPlaceholder");
            snackDisplay.layer = TramChanhLayers.EnvironmentIndex;
            snackDisplay.transform.SetParent(root, false);
            snackDisplay.transform.localPosition = new Vector3(-1.10f, 1.05f, 0.25f);
        }

        private static void PopulateModularBuildings(Transform parent, float minX, float maxX, bool isExtended)
        {
            GameObject shopAPf = AssetDatabase.LoadAssetAtPath<GameObject>(ShopfrontAPrefabPath);
            GameObject shopBPf = AssetDatabase.LoadAssetAtPath<GameObject>(ShopfrontBPrefabPath);
            GameObject shopCPf = AssetDatabase.LoadAssetAtPath<GameObject>(ShopfrontCPrefabPath);

            var slots = new List<BuildingSlot>();

            if (isExtended)
            {
                // South side buildings along z = -5.0m, facing North (rotY = 180f)
                // Behind service zone backdrop (preserving canonical positions)
                slots.Add(new BuildingSlot { x = -7.0f, z = -5.0f, rotY = 180f, type = 2 });
                slots.Add(new BuildingSlot { x = -2.6f, z = -5.0f, rotY = 180f, type = 0 });
                slots.Add(new BuildingSlot { x =  2.5f, z = -5.0f, rotY = 180f, type = 1 });
                slots.Add(new BuildingSlot { x =  7.2f, z = -5.0f, rotY = 180f, type = 0 });
                slots.Add(new BuildingSlot { x = 11.8f, z = -5.0f, rotY = 180f, type = 2 });

                // West of service zone (x in [-StreetHalfLength, -9.5])
                int bType = 1;
                for (float x = -StreetHalfLength + 2.5f; x <= -9.5f; x += 4.5f)
                {
                    bool nearAlley = false;
                    foreach (float al in AlleyPositions)
                    {
                        if (Mathf.Abs(x - al) < 3.2f) { nearAlley = true; break; }
                    }
                    if (nearAlley) continue;
                    slots.Add(new BuildingSlot { x = x, z = -5.0f, rotY = 180f, type = bType % 3 });
                    bType++;
                }

                // East of service zone (x in [14.0, StreetHalfLength])
                for (float x = 14.5f; x <= StreetHalfLength - 2.5f; x += 4.5f)
                {
                    bool nearAlley = false;
                    foreach (float al in AlleyPositions)
                    {
                        if (Mathf.Abs(x - al) < 3.2f) { nearAlley = true; break; }
                    }
                    if (nearAlley) continue;
                    slots.Add(new BuildingSlot { x = x, z = -5.0f, rotY = 180f, type = bType % 3 });
                    bType++;
                }

                // Alley flanking facades for all 4 alleys
                for (int i = 0; i < AlleyPositions.Length; i++)
                {
                    float al = AlleyPositions[i];
                    slots.Add(new BuildingSlot { x = al - 2.5f, z = -6.5f, rotY = 90f, type = 1 });
                    slots.Add(new BuildingSlot { x = al - 2.5f, z = -11.5f, rotY = 90f, type = 0 });
                    slots.Add(new BuildingSlot { x = al + 2.5f, z = -6.5f, rotY = -90f, type = 2 });
                    slots.Add(new BuildingSlot { x = al + 2.5f, z = -11.5f, rotY = -90f, type = 1 });
                }

                // Street end side buildings along building lines (at x = -StreetHalfLength and +StreetHalfLength)
                // South sidewalk building line: z <= -3.8m; North sidewalk building line: z >= 16.0m
                float[] endClosureZs = new float[] { -14.0f, -9.5f, -5.0f, 17.0f, 21.5f, 26.0f };
                for (int i = 0; i < endClosureZs.Length; i++)
                {
                    slots.Add(new BuildingSlot { x = -StreetHalfLength + 1.5f, z = endClosureZs[i], rotY = -90f, type = i % 3 });
                    slots.Add(new BuildingSlot { x = StreetHalfLength - 1.5f, z = endClosureZs[i], rotY = 90f, type = (i + 1) % 3 });
                }

                // North edge shophouses across the road (lining z = 17.0m, facing South towards -Z)
                int northIdx = 0;
                for (float x = -StreetHalfLength + 4.0f; x <= StreetHalfLength - 4.0f; x += 5.0f)
                {
                    slots.Add(new BuildingSlot { x = x, z = 17.0f, rotY = 0f, type = northIdx % 3 });
                    northIdx++;
                }
            }
            else
            {
                // Base street backdrop (canonical)
                slots.Add(new BuildingSlot { x = -7.0f, z = -5.0f, rotY = 180f, type = 2 });
                slots.Add(new BuildingSlot { x = -2.6f, z = -5.0f, rotY = 180f, type = 0 });
                slots.Add(new BuildingSlot { x =  2.5f, z = -5.0f, rotY = 180f, type = 1 });
                slots.Add(new BuildingSlot { x =  7.2f, z = -5.0f, rotY = 180f, type = 0 });
            }

            foreach (var s in slots)
            {
                GameObject pf = s.type == 0 ? shopAPf : (s.type == 1 ? shopBPf : shopCPf);
                if (pf != null)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                    b.transform.position = new Vector3(s.x, 0.0f, s.z);
                    b.transform.rotation = Quaternion.Euler(0f, s.rotY, 0f);
                }
            }
        }

        // Smallest slide along the street (0.5 m steps, either way) that leaves x at least minDistance from every blocker.
        private static float NudgeClear(float x, List<float> blockers, float minDistance)
        {
            for (float offset = 0f; offset <= 8f; offset += 0.5f)
            {
                foreach (float candidate in new[] { x + offset, x - offset })
                {
                    bool clear = true;
                    foreach (float b in blockers) { if (Mathf.Abs(candidate - b) < minDistance) { clear = false; break; } }
                    if (clear) { return candidate; }
                }
            }
            return x;
        }

        private static void PopulateStreetProps(Transform parent)
        {
            GameObject treePf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetTreePrefabPath);
            GameObject lampPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetLampPrefabPath);
            GameObject polePf = AssetDatabase.LoadAssetAtPath<GameObject>(UtilityPolePrefabPath);
            GameObject bikePf = AssetDatabase.LoadAssetAtPath<GameObject>(MotorbikePrefabPath);

            // Street Trees along South sidewalk curb (z = 3.2m) every 12m
            var southTrees = new List<float>();
            for (float x = -StreetHalfLength + 10f; x <= StreetHalfLength - 10f; x += 12.0f)
            {
                if (Mathf.Abs(x) < 9.0f) continue;
                bool nearAlley = false;
                foreach (float al in AlleyPositions)
                {
                    if (Mathf.Abs(x - al) < 3.0f) { nearAlley = true; break; }
                }
                if (!nearAlley) southTrees.Add(x);
            }
            southTrees.AddRange(new float[] { -13.0f, -11.0f, 11.0f, 13.0f, 14.0f });

            if (treePf != null)
            {
                for (int i = 0; i < southTrees.Count; i++)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(treePf, parent);
                    t.transform.position = new Vector3(southTrees[i], 0.0f, 3.2f);
                    t.transform.rotation = Quaternion.Euler(0f, (i * 47f) % 360f, 0f);
                }

                // Trees along North sidewalk curb (z = 13.0m) every 12m
                int tIdx = 0;
                for (float x = -StreetHalfLength + 10f; x <= StreetHalfLength - 10f; x += 12.0f)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(treePf, parent);
                    t.transform.position = new Vector3(x, 0.0f, 13.0f);
                    t.transform.rotation = Quaternion.Euler(0f, (tIdx * 63f) % 360f, 0f);
                    tIdx++;
                }
            }

            // Street Lamps along South curb every 24m
            var lampList = new List<float>();
            lampList.Add(-0.6f);
            for (float x = -StreetHalfLength + 10f; x <= StreetHalfLength - 10f; x += 24.0f)
            {
                if (Mathf.Abs(x) < 7.0f) continue;
                bool nearAlley = false;
                foreach (float al in AlleyPositions)
                {
                    if (Mathf.Abs(x - al) < 3.0f) { nearAlley = true; break; }
                }
                if (!nearAlley) lampList.Add(x);
            }
            if (lampPf != null)
            {
                foreach (var lampX in lampList)
                {
                    // Lamp posts sit on the 24 m grid, which lines up with every second tree on the 12 m grid: slide them clear of the trunks.
                    float x = Mathf.Abs(lampX - (-0.6f)) < 0.1f ? lampX : NudgeClear(lampX, southTrees, 2.8f);
                    var l = (GameObject)PrefabUtility.InstantiatePrefab(lampPf, parent);
                    l.transform.position = new Vector3(x, 0.0f, 3.4f);
                    l.transform.rotation = Quaternion.identity;
                    if (Mathf.Abs(lampX - (-0.6f)) < 0.1f)
                    {
                        var light = l.GetComponentInChildren<Light>();
                        if (light != null) Object.DestroyImmediate(light);
                    }
                }
            }

            // Utility Poles every 36m along South curb (z = 3.4m)
            var poleList = new List<float>();
            poleList.Add(2.0f);
            for (float x = -StreetHalfLength + 16f; x <= StreetHalfLength - 16f; x += 36.0f)
            {
                if (Mathf.Abs(x) < 10.0f) continue;
                bool nearAlley = false;
                foreach (float al in AlleyPositions)
                {
                    if (Mathf.Abs(x - al) < 3.0f) { nearAlley = true; break; }
                }
                if (!nearAlley) poleList.Add(x);
            }
            if (polePf != null)
            {
                foreach (var x in poleList)
                {
                    var p = (GameObject)PrefabUtility.InstantiatePrefab(polePf, parent);
                    p.transform.position = new Vector3(x, 0.0f, 3.4f);
                    p.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }

            // Parked Two-Wheelers (Bicycles, Cub Motorcycles, Scooters)
            GameObject bicyclePf = AssetDatabase.LoadAssetAtPath<GameObject>(BicyclePrefabPath);
            GameObject motorcycleCubPf = AssetDatabase.LoadAssetAtPath<GameObject>(MotorcycleCubPrefabPath);

            BikeSlot[] bikePositions = new BikeSlot[]
            {
                new BikeSlot { x = -24.0f, z = 2.6f, rotY = 15f },  // Bicycle
                new BikeSlot { x = -15.0f, z = 2.6f, rotY = -10f }, // Cub
                new BikeSlot { x = -12.5f, z = 2.6f, rotY = 5f },   // Scooter
                new BikeSlot { x = -10.0f, z = 2.7f, rotY = 20f },  // Scooter
                new BikeSlot { x = 10.0f,  z = 2.6f, rotY = -15f }, // Scooter
                new BikeSlot { x = 13.0f,  z = 2.6f, rotY = 10f },  // Cub
                new BikeSlot { x = 22.5f,  z = 2.6f, rotY = 5f },   // Scooter
                new BikeSlot { x = 31.0f,  z = 2.6f, rotY = -20f }, // Bicycle
                new BikeSlot { x = 17.5f,  z = -7.0f, rotY = 85f }, // Inside alley (Cub)
            };

            for (int i = 0; i < bikePositions.Length; i++)
            {
                var bp = bikePositions[i];
                GameObject pfToUse = bikePf;
                if ((i == 0 || i == 7) && bicyclePf != null)
                {
                    pfToUse = bicyclePf;
                }
                else if ((i == 1 || i == 5 || i == 8) && motorcycleCubPf != null)
                {
                    pfToUse = motorcycleCubPf;
                }

                if (pfToUse != null)
                {
                    var mb = (GameObject)PrefabUtility.InstantiatePrefab(pfToUse, parent);
                    mb.transform.position = new Vector3(bp.x, 0.0f, bp.z);
                    mb.transform.rotation = Quaternion.Euler(0f, bp.rotY, 0f);
                }
            }

            // Parked Four-Wheelers along North curb (clear of RoadWest/RoadEast lanes): Sedans, SUV, Delivery Truck
            GameObject sedanPf = AssetDatabase.LoadAssetAtPath<GameObject>(SedanPrefabPath);
            GameObject truckPf = AssetDatabase.LoadAssetAtPath<GameObject>(TruckSmallPrefabPath);
            GameObject suvPf = AssetDatabase.LoadAssetAtPath<GameObject>(CarSUVPrefabPath);

            if (suvPf != null)
            {
                var suv = (GameObject)PrefabUtility.InstantiatePrefab(suvPf, parent);
                suv.name = "PF_Car_SUV_NorthCurb";
                suv.transform.position = new Vector3(-26.0f, 0.0f, 12.0f);
                suv.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            if (truckPf != null)
            {
                var truck = (GameObject)PrefabUtility.InstantiatePrefab(truckPf, parent);
                truck.name = "PF_Truck_Small_NorthCurb";
                truck.transform.position = new Vector3(20.0f, 0.0f, 12.0f);
                truck.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }

            if (sedanPf != null)
            {
                var car1 = (GameObject)PrefabUtility.InstantiatePrefab(sedanPf, parent);
                car1.name = "PF_Car_Sedan_NorthCurb_01";
                car1.transform.position = new Vector3(6.0f, -0.15f, 12.0f);
                car1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                var car2 = (GameObject)PrefabUtility.InstantiatePrefab(sedanPf, parent);
                car2.name = "PF_Car_Sedan_NorthCurb_02";
                car2.transform.position = new Vector3(-12.0f, -0.15f, 12.0f);
                car2.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }

            // Street Animals (fixed on sidewalks and porches, never blocking table paths or roads)
            var animalsNode = new GameObject("Animals");
            animalsNode.layer = TramChanhLayers.EnvironmentIndex;
            animalsNode.transform.SetParent(parent, false);

            GameObject dogPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetDogPrefabPath);
            GameObject catPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetCatPrefabPath);
            GameObject chickPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetChickenPrefabPath);
            GameObject pigPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetPigeonPrefabPath);

            if (dogPf != null)
            {
                var d1 = (GameObject)PrefabUtility.InstantiatePrefab(dogPf, animalsNode.transform);
                d1.name = "PF_Street_Dog_WestPorch";
                d1.transform.position = new Vector3(-13.4f, 0.0f, -2.8f);
                d1.transform.rotation = Quaternion.Euler(0f, 20f, 0f);

                var d2 = (GameObject)PrefabUtility.InstantiatePrefab(dogPf, animalsNode.transform);
                d2.name = "PF_Street_Dog_EastCorner";
                d2.transform.position = new Vector3(22.0f, 0.0f, -3.4f);
                d2.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
            }

            if (catPf != null)
            {
                var c1 = (GameObject)PrefabUtility.InstantiatePrefab(catPf, animalsNode.transform);
                c1.name = "PF_Street_Cat_WestStoop";
                c1.transform.position = new Vector3(-12.2f, 0.0f, -2.8f);
                c1.transform.rotation = Quaternion.Euler(0f, 15f, 0f);

                var c2 = (GameObject)PrefabUtility.InstantiatePrefab(catPf, animalsNode.transform);
                c2.name = "PF_Street_Cat_AlleyMouth";
                c2.transform.position = new Vector3(17.2f, 0.0f, -4.8f);
                c2.transform.rotation = Quaternion.Euler(0f, 85f, 0f);
            }

            if (chickPf != null)
            {
                var ch0 = (GameObject)PrefabUtility.InstantiatePrefab(chickPf, animalsNode.transform);
                ch0.name = "PF_Street_Chicken_WestPorch";
                ch0.transform.position = new Vector3(-11.0f, 0.0f, -2.8f);
                ch0.transform.rotation = Quaternion.Euler(0f, 10f, 0f);

                var ch1 = (GameObject)PrefabUtility.InstantiatePrefab(chickPf, animalsNode.transform);
                ch1.name = "PF_Street_Chicken_EastCurbside";
                ch1.transform.position = new Vector3(27.0f, 0.0f, 3.5f);
                ch1.transform.rotation = Quaternion.Euler(0f, -60f, 0f);

                var ch2 = (GameObject)PrefabUtility.InstantiatePrefab(chickPf, animalsNode.transform);
                ch2.name = "PF_Street_Chicken_AlleyBack";
                ch2.transform.position = new Vector3(19.2f, 0.0f, -12.0f);
                ch2.transform.rotation = Quaternion.Euler(0f, 120f, 0f);
            }

            if (pigPf != null)
            {
                var p1 = (GameObject)PrefabUtility.InstantiatePrefab(pigPf, animalsNode.transform);
                p1.name = "PF_Street_Pigeon_North_01";
                p1.transform.position = new Vector3(6.0f, 0.0f, 14.2f);
                p1.transform.rotation = Quaternion.Euler(0f, 35f, 0f);

                var p2 = (GameObject)PrefabUtility.InstantiatePrefab(pigPf, animalsNode.transform);
                p2.name = "PF_Street_Pigeon_North_02";
                p2.transform.position = new Vector3(7.2f, 0.0f, 14.6f);
                p2.transform.rotation = Quaternion.Euler(0f, -110f, 0f);

                var p3 = (GameObject)PrefabUtility.InstantiatePrefab(pigPf, animalsNode.transform);
                p3.name = "PF_Street_Pigeon_WestCurbside";
                p3.transform.position = new Vector3(-21.5f, 0.0f, 2.2f);
                p3.transform.rotation = Quaternion.Euler(0f, 75f, 0f);
            }

            // Street Furniture and Vendor Props (authentic Vietnamese sidewalk elements)
            var vendorPropsNode = new GameObject("VendorAndSidewalkDecor");
            vendorPropsNode.layer = TramChanhLayers.EnvironmentIndex;
            vendorPropsNode.transform.SetParent(parent, false);

            GameObject cartPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetVendorCartPrefabPath);
            GameObject umbPf = AssetDatabase.LoadAssetAtPath<GameObject>(SidewalkUmbrellaPrefabPath);
            GameObject canopyPf = AssetDatabase.LoadAssetAtPath<GameObject>(ShopfrontCanopyPrefabPath);
            GameObject lanternPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetPostLanternPrefabPath);
            GameObject vnTablesPf = AssetDatabase.LoadAssetAtPath<GameObject>(PlasticTableChairsVNPrefabPath);
            GameObject crateStreetPf = AssetDatabase.LoadAssetAtPath<GameObject>(PlasticCrateStreetPrefabPath);

            if (cartPf != null)
            {
                var cart = (GameObject)PrefabUtility.InstantiatePrefab(cartPf, vendorPropsNode.transform);
                cart.name = "PF_StreetVendor_Cart_East";
                cart.transform.position = new Vector3(25.0f, 0.0f, 1.8f);
                cart.transform.rotation = Quaternion.Euler(0f, -100f, 0f);
            }

            if (umbPf != null)
            {
                var u1 = (GameObject)PrefabUtility.InstantiatePrefab(umbPf, vendorPropsNode.transform);
                u1.name = "PF_Sidewalk_Umbrella_East";
                u1.transform.position = new Vector3(25.0f, 0.0f, 0.7f);
                u1.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

                var u2 = (GameObject)PrefabUtility.InstantiatePrefab(umbPf, vendorPropsNode.transform);
                u2.name = "PF_Sidewalk_Umbrella_West";
                u2.transform.position = new Vector3(-14.0f, 0.0f, 1.5f);
                u2.transform.rotation = Quaternion.Euler(0f, -30f, 0f);
            }

            if (canopyPf != null)
            {
                var cp1 = (GameObject)PrefabUtility.InstantiatePrefab(canopyPf, vendorPropsNode.transform);
                cp1.name = "PF_Shopfront_Canopy_West";
                cp1.transform.position = new Vector3(-24.0f, 2.4f, -3.8f);
                cp1.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                var cp2 = (GameObject)PrefabUtility.InstantiatePrefab(canopyPf, vendorPropsNode.transform);
                cp2.name = "PF_Shopfront_Canopy_East";
                cp2.transform.position = new Vector3(28.0f, 2.4f, -3.8f);
                cp2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            if (lanternPf != null)
            {
                var l1 = (GameObject)PrefabUtility.InstantiatePrefab(lanternPf, vendorPropsNode.transform);
                l1.name = "PF_Street_PostLantern_AlleyWest";
                l1.transform.position = new Vector3(16.3f, 0.0f, -3.6f);
                l1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                var l2 = (GameObject)PrefabUtility.InstantiatePrefab(lanternPf, vendorPropsNode.transform);
                l2.name = "PF_Street_PostLantern_AlleyEast";
                l2.transform.position = new Vector3(20.7f, 0.0f, -3.6f);
                l2.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }

            if (vnTablesPf != null)
            {
                var vn1 = (GameObject)PrefabUtility.InstantiatePrefab(vnTablesPf, vendorPropsNode.transform);
                vn1.name = "PF_PlasticTableChairs_VN_West";
                vn1.transform.position = new Vector3(-10.2f, 0.0f, 1.6f);
                vn1.transform.rotation = Quaternion.Euler(0f, -10f, 0f);

                var vn2 = (GameObject)PrefabUtility.InstantiatePrefab(vnTablesPf, vendorPropsNode.transform);
                vn2.name = "PF_PlasticTableChairs_VN_East";
                vn2.transform.position = new Vector3(8.5f, 0.0f, 1.6f);
                vn2.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            }

            if (crateStreetPf != null)
            {
                var cr1 = (GameObject)PrefabUtility.InstantiatePrefab(crateStreetPf, vendorPropsNode.transform);
                cr1.name = "PF_PlasticCrate_Street_WestSide";
                cr1.transform.position = new Vector3(-1.6f, 0.0f, -0.6f);
                cr1.transform.rotation = Quaternion.Euler(0f, 12f, 0f);

                var cr2 = (GameObject)PrefabUtility.InstantiatePrefab(crateStreetPf, vendorPropsNode.transform);
                cr2.name = "PF_PlasticCrate_Street_EastSide";
                cr2.transform.position = new Vector3(1.7f, 0.0f, -0.6f);
                cr2.transform.rotation = Quaternion.Euler(0f, -8f, 0f);

                GameObject teaBagPf = AssetDatabase.LoadAssetAtPath<GameObject>(TeaBagPrefabPath);
                if (teaBagPf != null)
                {
                    // Upright tea bags inside West crate (standing vertical, not laid flat)
                    var tb1 = (GameObject)PrefabUtility.InstantiatePrefab(teaBagPf, cr1.transform);
                    tb1.name = "TeaBag_Upright_01";
                    tb1.transform.localPosition = new Vector3(-0.08f, 0.09f, 0.0f);
                    tb1.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);

                    var tb2 = (GameObject)PrefabUtility.InstantiatePrefab(teaBagPf, cr1.transform);
                    tb2.name = "TeaBag_Upright_02";
                    tb2.transform.localPosition = new Vector3(0.08f, 0.09f, 0.0f);
                    tb2.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);

                    // Upright tea bags inside East crate
                    var tb3 = (GameObject)PrefabUtility.InstantiatePrefab(teaBagPf, cr2.transform);
                    tb3.name = "TeaBag_Upright_03";
                    tb3.transform.localPosition = new Vector3(-0.06f, 0.09f, 0.0f);
                    tb3.transform.localRotation = Quaternion.Euler(0f, -6f, 0f);

                    var tb4 = (GameObject)PrefabUtility.InstantiatePrefab(teaBagPf, cr2.transform);
                    tb4.name = "TeaBag_Upright_04";
                    tb4.transform.localPosition = new Vector3(0.06f, 0.09f, 0.0f);
                    tb4.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
                }
            }

            // No static ambient pedestrians: they ended up at the stall origin and stood in the counter. Walking people come from PeopleController.
        }

        private static void PopulateNightLighting(Transform parent)
        {
            // Requirement: Real-time light budget <= 8 near stall (ART-FIX-017 Task B)
            // Light 1: Warm stall bulb (illuminates stall floor and interior)
            var stallLight = new GameObject("WarmStallBulb").AddComponent<Light>();
            stallLight.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            stallLight.transform.SetParent(parent, false);
            stallLight.transform.position = new Vector3(0.0f, 2.0f, 0.0f);
            stallLight.type = LightType.Point;
            stallLight.range = 6.0f;
            stallLight.color = new Color(1.0f, 0.82f, 0.50f);
            stallLight.intensity = 3.8f;
            stallLight.shadows = LightShadows.Soft;

            // Light 2: Center Streetlight (illuminating stall front & sidewalk)
            var streetCenter = new GameObject("Streetlight_Center").AddComponent<Light>();
            streetCenter.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            streetCenter.transform.SetParent(parent, false);
            streetCenter.transform.position = new Vector3(-0.6f, 5.8f, 2.5f);
            streetCenter.type = LightType.Point;
            streetCenter.range = 22f;
            streetCenter.color = new Color(1.0f, 0.86f, 0.65f);
            streetCenter.intensity = 3.2f;
            streetCenter.shadows = LightShadows.None;

            // Ambient fills (No shadows)
            var skyFill = new GameObject("SkyFill").AddComponent<Light>();
            skyFill.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            skyFill.transform.SetParent(parent, false);
            skyFill.transform.rotation = Quaternion.Euler(55f, -140f, 0f);
            skyFill.type = LightType.Directional;
            skyFill.color = new Color(0.35f, 0.45f, 0.70f);
            skyFill.intensity = 1.5f;
            skyFill.shadows = LightShadows.None;
        }

        private static void BuildWalkwaysGraph(Transform root)
        {
            // Empty parent Walkways per §5
            var walkways = new GameObject("Walkways");
            walkways.layer = TramChanhLayers.EnvironmentIndex;
            walkways.transform.SetParent(root, false);

            // 1. SidewalkNorth (spacing 2.5m, x in [-StreetHalfLength, StreetHalfLength], z = 14.5m) -> 201 points
            var swNorth = new GameObject("SidewalkNorth");
            swNorth.layer = TramChanhLayers.EnvironmentIndex;
            swNorth.transform.SetParent(walkways.transform, false);
            int idx = 1;
            for (float x = -StreetHalfLength; x <= StreetHalfLength + 0.1f; x += 2.5f)
            {
                string numStr = idx < 100 ? $"{idx:D2}" : $"{idx}";
                var wp = new GameObject($"WP_SidewalkNorth_{numStr}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(swNorth.transform, false);
                wp.transform.localPosition = new Vector3(x, 0.0f, 14.5f);
                idx++;
            }

            // 2. SidewalkSouth (spacing 2.0m, x in [-StreetHalfLength, StreetHalfLength], z = 3.1m for x in [-10, 8], transition step <= 0.5m) -> 251 points
            var swSouth = new GameObject("SidewalkSouth");
            swSouth.layer = TramChanhLayers.EnvironmentIndex;
            swSouth.transform.SetParent(walkways.transform, false);
            idx = 1;
            for (float x = -StreetHalfLength; x <= StreetHalfLength + 0.1f; x += 2.0f)
            {
                string numStr = idx < 100 ? $"{idx:D2}" : $"{idx}";
                var wp = new GameObject($"WP_SidewalkSouth_{numStr}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(swSouth.transform, false);
                float z = 2.0f;
                if (x >= -10.0f && x <= 8.0f)
                {
                    z = 3.1f;
                }
                else if (x < -10.0f && x >= -16.0f)
                {
                    float t = (-10.0f - x) / 6.0f;
                    z = Mathf.Lerp(3.1f, 2.0f, t);
                }
                else if (x > 8.0f && x <= 14.0f)
                {
                    float t = (x - 8.0f) / 6.0f;
                    z = Mathf.Lerp(3.1f, 2.0f, t);
                }
                wp.transform.localPosition = new Vector3(x, 0.0f, z);
                idx++;
            }

            // 3. Alley (from z = 1.8 down to -14.0m, x = 18.5m)
            var alley = new GameObject("Alley");
            alley.layer = TramChanhLayers.EnvironmentIndex;
            alley.transform.SetParent(walkways.transform, false);
            float[] alleyZs = new[] { 1.8f, -0.5f, -3.0f, -5.5f, -8.0f, -10.5f, -12.5f, -14.0f };
            for (int i = 0; i < alleyZs.Length; i++)
            {
                var wp = new GameObject($"WP_Alley_{i + 1:D2}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(alley.transform, false);
                wp.transform.localPosition = new Vector3(18.5f, 0.0f, alleyZs[i]);
            }

            // 4. RoadEast (motorbikes heading +X, spacing 4m, z = 5.85m, y = -0.15m) -> 126 points
            var roadEast = new GameObject("RoadEast");
            roadEast.layer = TramChanhLayers.EnvironmentIndex;
            roadEast.transform.SetParent(walkways.transform, false);
            idx = 1;
            for (float x = -StreetHalfLength; x <= StreetHalfLength + 0.1f; x += 4.0f)
            {
                string numStr = idx < 100 ? $"{idx:D2}" : $"{idx}";
                var wp = new GameObject($"WP_RoadEast_{numStr}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(roadEast.transform, false);
                wp.transform.localPosition = new Vector3(x, -0.15f, 5.85f);
                idx++;
            }

            // 5. RoadWest (motorbikes heading -X, spacing 4m, z = 10.35m, y = -0.15m) -> 126 points
            var roadWest = new GameObject("RoadWest");
            roadWest.layer = TramChanhLayers.EnvironmentIndex;
            roadWest.transform.SetParent(walkways.transform, false);
            idx = 1;
            for (float x = StreetHalfLength; x >= -StreetHalfLength - 0.1f; x -= 4.0f)
            {
                string numStr = idx < 100 ? $"{idx:D2}" : $"{idx}";
                var wp = new GameObject($"WP_RoadWest_{numStr}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(roadWest.transform, false);
                wp.transform.localPosition = new Vector3(x, -0.15f, 10.35f);
                idx++;
            }

            // Direct children of root per Section 5:
            // StreetEntryWest & StreetEntryEast (hidden behind building edges, <= 22m walk to any table approach)
            var entryWest = new GameObject("StreetEntryWest");
            entryWest.layer = TramChanhLayers.EnvironmentIndex;
            entryWest.transform.SetParent(root, false);
            entryWest.transform.localPosition = new Vector3(-16.0f, 0.0f, 2.0f);
            entryWest.transform.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

            var entryEast = new GameObject("StreetEntryEast");
            entryEast.layer = TramChanhLayers.EnvironmentIndex;
            entryEast.transform.SetParent(root, false);
            entryEast.transform.localPosition = new Vector3(12.0f, 0.0f, 2.0f);
            entryEast.transform.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

            // AmbientEntryWest & AmbientEntryEast (far ends at +/-250m for ambient pedestrians and bikes per STREET_500M_SPEC §4)
            var ambientWest = new GameObject("AmbientEntryWest");
            ambientWest.layer = TramChanhLayers.EnvironmentIndex;
            ambientWest.transform.SetParent(root, false);
            ambientWest.transform.localPosition = new Vector3(-StreetHalfLength, 0.0f, 2.0f);
            ambientWest.transform.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

            var ambientEast = new GameObject("AmbientEntryEast");
            ambientEast.layer = TramChanhLayers.EnvironmentIndex;
            ambientEast.transform.SetParent(root, false);
            ambientEast.transform.localPosition = new Vector3(StreetHalfLength, 0.0f, 2.0f);
            ambientEast.transform.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

            // TableApproach_01..10 beside each table's open side, facing the table, on the ground (y = 0)
            var approachSpecs = new TableApproachSpec[]
            {
                new TableApproachSpec { name = "TableApproach_01", pos = new Vector3(-2.10f, 0f,  2.50f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_02", pos = new Vector3(-3.35f, 0f,  1.00f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_03", pos = new Vector3(-4.60f, 0f,  2.50f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_04", pos = new Vector3(-5.85f, 0f,  1.00f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_05", pos = new Vector3(-7.10f, 0f,  2.50f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_06", pos = new Vector3(-3.35f, 0f, -1.00f), lookDir = new Vector3( 0f, 0f, -1f) },
                new TableApproachSpec { name = "TableApproach_07", pos = new Vector3(-5.85f, 0f, -1.00f), lookDir = new Vector3( 0f, 0f, -1f) },
                new TableApproachSpec { name = "TableApproach_08", pos = new Vector3( 4.20f, 0f,  1.40f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_09", pos = new Vector3( 5.60f, 0f, -0.10f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_10", pos = new Vector3( 4.20f, 0f, -1.40f), lookDir = new Vector3( 0f, 0f, -1f) },
            };

            foreach (var spec in approachSpecs)
            {
                var ta = new GameObject(spec.name);
                ta.layer = TramChanhLayers.EnvironmentIndex;
                ta.transform.SetParent(root, false);
                ta.transform.localPosition = spec.pos;
                ta.transform.localRotation = Quaternion.LookRotation(spec.lookDir, Vector3.up);
            }

            // BikeParking_01..04
            Vector3[] bikeParkings = new Vector3[]
            {
                new Vector3(-14.0f, 0.0f, 2.8f),
                new Vector3(-11.5f, 0.0f, 2.8f),
                new Vector3( 12.0f, 0.0f, 2.8f),
                new Vector3( 14.5f, 0.0f, 2.8f)
            };

            for (int i = 0; i < bikeParkings.Length; i++)
            {
                var bp = new GameObject($"BikeParking_{i + 1:D2}");
                bp.layer = TramChanhLayers.EnvironmentIndex;
                bp.transform.SetParent(root, false);
                bp.transform.localPosition = bikeParkings[i];
            }
        }

        private static void AddLimitBox(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.layer = TramChanhLayers.EnvironmentIndex;
            go.transform.SetParent(parent, false);
            var col = go.AddComponent<BoxCollider>();
            col.center = center;
            col.size = size;
        }

        public static void CreatePreviewSceneAndCapture()
        {
            EnsureFolder(Path.GetDirectoryName(ExtendedPreviewScenePath));
            EnsureFolder(ScreenshotsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Ambient environment
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.20f, 0.26f);

            GameObject extPf = AssetDatabase.LoadAssetAtPath<GameObject>(ExtendedStreetPrefabPath);
            if (extPf != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(extPf);
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;
            }

            EditorSceneManager.SaveScene(scene, ExtendedPreviewScenePath);
            Debug.Log("[TramChanh] Saved Extended Preview Scene: " + ExtendedPreviewScenePath);

            CaptureScreenshots();
        }

        public static void CaptureScreenshots()
        {
            EnsureFolder(ScreenshotsFolder);

            // 5 Required Perspectives:
            // 1. Service zone from PlayerSpawn (eye height 1.6m)
            // 2. Walkable sidewalk looking east (eye height 1.6m)
            // 3. Walkable sidewalk looking west (eye height 1.6m)
            // 4. Road with parked motorbikes (eye height 1.6m)
            // 5. Side alley (eye height 1.6m)

            var shots = new CameraShot[]
            {
                new CameraShot
                {
                    filename = "01_Service_Zone.png",
                    pos = new Vector3(0.0f, 1.6f, -1.2f), // Eye height 1.6m at PlayerSpawn
                    target = new Vector3(0.0f, 1.1f, 1.8f), // Looking towards counter and seating area
                    fov = 70f
                },
                new CameraShot
                {
                    filename = "02_Sidewalk_East.png",
                    pos = new Vector3(-8.0f, 1.6f, 1.8f), // Sidewalk eye level looking East
                    target = new Vector3(25.0f, 1.5f, 1.8f),
                    fov = 65f
                },
                new CameraShot
                {
                    filename = "03_Sidewalk_West.png",
                    pos = new Vector3(10.0f, 1.6f, 1.8f), // Sidewalk eye level looking West
                    target = new Vector3(-25.0f, 1.5f, 1.8f),
                    fov = 65f
                },
                new CameraShot
                {
                    filename = "04_Road_And_Motorbikes.png",
                    pos = new Vector3(-3.0f, 1.6f, 1.8f), // At curb looking across road and parked bikes
                    target = new Vector3(-5.0f, 0.8f, 7.5f),
                    fov = 65f
                },
                new CameraShot
                {
                    filename = "05_Side_Alley.png",
                    pos = new Vector3(18.5f, 1.6f, 2.5f), // Eye level at alley mouth looking South into alley
                    target = new Vector3(18.5f, 1.2f, -12.0f),
                    fov = 65f
                }
            };

            int w = 1920;
            int h = 1080;

            var camGo = new GameObject("QA_ValidationCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f, 1.0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            try
            {
                cam.targetTexture = rt;

                foreach (var shot in shots)
                {
                    cam.transform.position = shot.pos;
                    cam.transform.rotation = Quaternion.LookRotation(shot.target - shot.pos, Vector3.up);
                    cam.fieldOfView = shot.fov;

                    cam.Render();

                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();

                    byte[] bytes = tex.EncodeToPNG();
                    string outPath = Path.Combine(ScreenshotsFolder, shot.filename);
                    File.WriteAllBytes(outPath, bytes);
                    Debug.Log($"[TramChanh] Captured validation render: {outPath}");
                }
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camGo);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
