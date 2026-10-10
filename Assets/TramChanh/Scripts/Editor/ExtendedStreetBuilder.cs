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
            swCol.center = new Vector3(0.0f, -0.075f, -0.8f);
            swCol.size = new Vector3(36.0f, 0.15f, 8.8f);

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
            fillLight.intensity = 0.45f;
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

            // South Sidewalk (x in [-40, 40])
            if (sw80Mesh != null)
            {
                var s80 = new GameObject("SouthSidewalk_80m");
                s80.layer = TramChanhLayers.EnvironmentIndex;
                s80.transform.SetParent(extendedGround.transform, false);
                s80.AddComponent<MeshFilter>().sharedMesh = sw80Mesh;
                s80.AddComponent<MeshRenderer>().sharedMaterial = swMat;

                // BoxColliders covering the walkable south sidewalk surface (top at y = 0.0m)
                // West segment
                var colW = s80.AddComponent<BoxCollider>();
                colW.center = new Vector3(-11.75f, -0.075f, -0.1f);
                colW.size = new Vector3(56.5f, 0.15f, 7.4f);

                // Alley threshold
                var colThresh = s80.AddComponent<BoxCollider>();
                colThresh.center = new Vector3(18.5f, -0.075f, -0.1f);
                colThresh.size = new Vector3(4.0f, 0.15f, 7.4f);

                // East segment
                var colE = s80.AddComponent<BoxCollider>();
                colE.center = new Vector3(30.25f, -0.075f, -0.1f);
                colE.size = new Vector3(19.5f, 0.15f, 7.4f);
            }

            // North Sidewalk (x in [-40, 40], z in [12.6, 16.6])
            if (nsw80Mesh != null)
            {
                var n80 = new GameObject("NorthSidewalk_80m");
                n80.layer = TramChanhLayers.EnvironmentIndex;
                n80.transform.SetParent(extendedGround.transform, false);
                n80.AddComponent<MeshFilter>().sharedMesh = nsw80Mesh;
                n80.AddComponent<MeshRenderer>().sharedMaterial = swMat;

                var colN = n80.AddComponent<BoxCollider>();
                colN.center = new Vector3(0.0f, -0.075f, 14.6f);
                colN.size = new Vector3(80.0f, 0.15f, 4.0f);
            }

            // 2-lane Roadway (x in [-40, 40], z in [3.6, 12.6], y = -0.15m)
            if (rd80Mesh != null)
            {
                var r80 = new GameObject("Roadway_80m");
                r80.layer = TramChanhLayers.EnvironmentIndex;
                r80.transform.SetParent(extendedGround.transform, false);
                r80.AddComponent<MeshFilter>().sharedMesh = rd80Mesh;
                r80.AddComponent<MeshRenderer>().sharedMaterial = rdMat;

                var colR = r80.AddComponent<BoxCollider>();
                colR.center = new Vector3(0.0f, -0.185f, 8.1f);
                colR.size = new Vector3(80.0f, 0.07f, 9.0f);
            }

            // Side Alley Pavement (x in [16.5, 20.5], z in [-15.0, -3.8], top at y = 0.0m)
            if (alMesh != null)
            {
                var alObj = new GameObject("Alley_Pavement");
                alObj.layer = TramChanhLayers.EnvironmentIndex;
                alObj.transform.SetParent(extendedGround.transform, false);
                alObj.AddComponent<MeshFilter>().sharedMesh = alMesh;
                alObj.AddComponent<MeshRenderer>().sharedMaterial = swMat;

                var colAl = alObj.AddComponent<BoxCollider>();
                colAl.center = new Vector3(18.5f, -0.075f, -9.4f);
                colAl.size = new Vector3(4.0f, 0.15f, 11.2f);
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

            AddLimitBox(limits.transform, "Boundary_RoadCurb", new Vector3(0.0f, 1.25f, 3.65f), new Vector3(80.0f, 2.5f, 0.1f));
            AddLimitBox(limits.transform, "Boundary_SouthBuildings_West", new Vector3(-11.75f, 1.5f, -3.85f), new Vector3(56.5f, 3.0f, 0.1f));
            AddLimitBox(limits.transform, "Boundary_SouthBuildings_East", new Vector3(30.25f, 1.5f, -3.85f), new Vector3(19.5f, 3.0f, 0.1f));
            AddLimitBox(limits.transform, "Boundary_Alley_West", new Vector3(16.45f, 1.5f, -9.4f), new Vector3(0.1f, 3.0f, 11.2f));
            AddLimitBox(limits.transform, "Boundary_Alley_East", new Vector3(20.55f, 1.5f, -9.4f), new Vector3(0.1f, 3.0f, 11.2f));
            AddLimitBox(limits.transform, "Boundary_Alley_Back", new Vector3(18.5f, 1.5f, -15.05f), new Vector3(4.2f, 3.0f, 0.1f));
            AddLimitBox(limits.transform, "Boundary_MapEnd_West", new Vector3(-40.05f, 1.5f, -0.1f), new Vector3(0.1f, 3.0f, 7.5f));
            AddLimitBox(limits.transform, "Boundary_MapEnd_East", new Vector3(40.05f, 1.5f, -0.1f), new Vector3(0.1f, 3.0f, 7.5f));
            AddLimitBox(limits.transform, "Boundary_NorthSidewalk_Back", new Vector3(0.0f, 1.5f, 16.65f), new Vector3(80.0f, 3.0f, 0.1f));

            // 5. Extended Buildings & Architecture
            var buildings = new GameObject("ExtendedBuildings");
            buildings.layer = TramChanhLayers.EnvironmentIndex;
            buildings.transform.SetParent(root.transform, false);
            PopulateModularBuildings(buildings.transform, -40f, 40f, true);

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
            vehiclePoint.transform.localPosition = new Vector3(2.0f, 0f, -1.3f);
            vehiclePoint.transform.localScale = Vector3.one;

            var vehVisual = new GameObject("GenericVehicleVisual");
            vehVisual.layer = TramChanhLayers.EnvironmentIndex;
            vehVisual.transform.SetParent(vehiclePoint.transform, false);

            var takeawayCust = new GameObject("TakeawayCustomer");
            takeawayCust.layer = TramChanhLayers.EnvironmentIndex;
            takeawayCust.transform.SetParent(vehiclePoint.transform, false);

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
            menuBoard.transform.localPosition = new Vector3(1.15f, 1.10f, 0.25f);
            menuBoard.transform.localRotation = Quaternion.Euler(0f, -15f, 0f);
            var menuQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            menuQuad.name = "BoardVisual";
            menuQuad.layer = TramChanhLayers.EnvironmentIndex;
            menuQuad.transform.SetParent(menuBoard.transform, false);
            menuQuad.transform.localScale = new Vector3(0.60f, 0.85f, 1.0f);
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
                // West of service zone (x in [-40, -9])
                slots.Add(new BuildingSlot { x = -37.5f, z = -5.0f, rotY = 180f, type = 1 });
                slots.Add(new BuildingSlot { x = -33.0f, z = -5.0f, rotY = 180f, type = 0 });
                slots.Add(new BuildingSlot { x = -28.5f, z = -5.0f, rotY = 180f, type = 2 });
                slots.Add(new BuildingSlot { x = -24.0f, z = -5.0f, rotY = 180f, type = 1 });
                slots.Add(new BuildingSlot { x = -19.5f, z = -5.0f, rotY = 180f, type = 0 });
                slots.Add(new BuildingSlot { x = -15.0f, z = -5.0f, rotY = 180f, type = 2 });
                slots.Add(new BuildingSlot { x = -10.5f, z = -5.0f, rotY = 180f, type = 1 });

                // Behind service zone backdrop (preserving canonical positions)
                slots.Add(new BuildingSlot { x = -7.0f, z = -5.0f, rotY = 180f, type = 2 });
                slots.Add(new BuildingSlot { x = -2.6f, z = -5.0f, rotY = 180f, type = 0 });
                slots.Add(new BuildingSlot { x =  2.5f, z = -5.0f, rotY = 180f, type = 1 });
                slots.Add(new BuildingSlot { x =  7.2f, z = -5.0f, rotY = 180f, type = 0 });
                slots.Add(new BuildingSlot { x = 11.8f, z = -5.0f, rotY = 180f, type = 2 });

                // Alley West wall facades (facing +X)
                slots.Add(new BuildingSlot { x = 16.0f, z = -6.5f, rotY = 90f, type = 1 });
                slots.Add(new BuildingSlot { x = 16.0f, z = -11.5f, rotY = 90f, type = 0 });

                // Alley East wall facades (facing -X)
                slots.Add(new BuildingSlot { x = 21.0f, z = -6.5f, rotY = -90f, type = 2 });
                slots.Add(new BuildingSlot { x = 21.0f, z = -11.5f, rotY = -90f, type = 1 });

                // East of alley (x in [21, 40])
                slots.Add(new BuildingSlot { x = 23.5f, z = -5.0f, rotY = 180f, type = 2 });
                slots.Add(new BuildingSlot { x = 28.0f, z = -5.0f, rotY = 180f, type = 0 });
                slots.Add(new BuildingSlot { x = 32.5f, z = -5.0f, rotY = 180f, type = 1 });
                slots.Add(new BuildingSlot { x = 37.0f, z = -5.0f, rotY = 180f, type = 2 });

                // North edge shophouses across the road (lining z = 17.0m, facing South towards -Z)
                float[] northXs = new float[] { -36f, -31f, -26f, -21f, -16f, -11f, -6f, -1f, 4f, 9f, 14f, 19f, 24f, 29f, 34f };
                for (int i = 0; i < northXs.Length; i++)
                {
                    slots.Add(new BuildingSlot { x = northXs[i], z = 17.0f, rotY = 0f, type = i % 3 });
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

        private static void PopulateStreetProps(Transform parent)
        {
            GameObject treePf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetTreePrefabPath);
            GameObject lampPf = AssetDatabase.LoadAssetAtPath<GameObject>(StreetLampPrefabPath);
            GameObject polePf = AssetDatabase.LoadAssetAtPath<GameObject>(UtilityPolePrefabPath);
            GameObject bikePf = AssetDatabase.LoadAssetAtPath<GameObject>(MotorbikePrefabPath);

            // Street Trees along South sidewalk curb (z = 3.2m)
            float[] southTreeXs = new[] { -36f, -26f, -16f, -7.5f, -4.5f, 4.5f, 7.5f, 14.0f, 25.0f, 35.0f };
            if (treePf != null)
            {
                for (int i = 0; i < southTreeXs.Length; i++)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(treePf, parent);
                    t.transform.position = new Vector3(southTreeXs[i], 0.0f, 3.2f);
                    t.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                }

                // Trees along North sidewalk curb (z = 13.0m)
                float[] northTreeXs = new[] { -32f, -20f, -8f, 6f, 20f, 32f };
                for (int i = 0; i < northTreeXs.Length; i++)
                {
                    var t = (GameObject)PrefabUtility.InstantiatePrefab(treePf, parent);
                    t.transform.position = new Vector3(northTreeXs[i], 0.0f, 13.0f);
                    t.transform.rotation = Quaternion.Euler(0f, i * 63f, 0f);
                }
            }

            // Street Lamps along South curb
            float[] lampXs = new[] { -31f, -18f, -0.6f, 12.0f, 28.0f };
            if (lampPf != null)
            {
                foreach (var x in lampXs)
                {
                    var l = (GameObject)PrefabUtility.InstantiatePrefab(lampPf, parent);
                    l.transform.position = new Vector3(x, 0.0f, 3.4f);
                    l.transform.rotation = Quaternion.identity;
                }
            }

            // Utility Poles
            float[] poleXs = new[] { -22f, 2.0f, 22.0f };
            if (polePf != null)
            {
                foreach (var x in poleXs)
                {
                    var p = (GameObject)PrefabUtility.InstantiatePrefab(polePf, parent);
                    p.transform.position = new Vector3(x, 0.0f, 3.4f);
                    p.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }

            // Parked Motorbikes
            BikeSlot[] bikePositions = new BikeSlot[]
            {
                new BikeSlot { x = -24.0f, z = 2.6f, rotY = 15f },
                new BikeSlot { x = -15.0f, z = 2.6f, rotY = -10f },
                new BikeSlot { x = -12.5f, z = 2.6f, rotY = 5f },
                new BikeSlot { x = -6.5f,  z = 2.7f, rotY = 20f },
                new BikeSlot { x = 5.2f,   z = 2.6f, rotY = -15f },
                new BikeSlot { x = 13.0f,  z = 2.6f, rotY = 10f },
                new BikeSlot { x = 22.5f,  z = 2.6f, rotY = 5f },
                new BikeSlot { x = 31.0f,  z = 2.6f, rotY = -20f },
                new BikeSlot { x = 17.5f,  z = -7.0f, rotY = 85f }, // Inside alley
            };

            if (bikePf != null)
            {
                foreach (var bp in bikePositions)
                {
                    var mb = (GameObject)PrefabUtility.InstantiatePrefab(bikePf, parent);
                    mb.transform.position = new Vector3(bp.x, 0.0f, bp.z);
                    mb.transform.rotation = Quaternion.Euler(0f, bp.rotY, 0f);
                }
            }
        }

        private static void PopulateNightLighting(Transform parent)
        {
            // Requirement: "no real-time shadows from more than 4 lights"
            // Light 1: Warm stall bulb
            var stallLight = new GameObject("WarmStallBulb").AddComponent<Light>();
            stallLight.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            stallLight.transform.SetParent(parent, false);
            stallLight.transform.position = new Vector3(0.0f, 2.0f, 0.0f);
            stallLight.type = LightType.Point;
            stallLight.range = 6.0f;
            stallLight.color = new Color(1.0f, 0.82f, 0.50f);
            stallLight.intensity = 3.8f;
            stallLight.shadows = LightShadows.Soft;

            // Light 2: Center Streetlight
            var streetCenter = new GameObject("Streetlight_Center").AddComponent<Light>();
            streetCenter.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            streetCenter.transform.SetParent(parent, false);
            streetCenter.transform.position = new Vector3(-0.6f, 5.8f, 2.5f);
            streetCenter.type = LightType.Point;
            streetCenter.range = 22f;
            streetCenter.color = new Color(1.0f, 0.86f, 0.65f);
            streetCenter.intensity = 3.2f;
            streetCenter.shadows = LightShadows.Soft;

            // Light 3: West Streetlight
            var streetWest = new GameObject("Streetlight_West").AddComponent<Light>();
            streetWest.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            streetWest.transform.SetParent(parent, false);
            streetWest.transform.position = new Vector3(-18.0f, 5.8f, 2.5f);
            streetWest.type = LightType.Point;
            streetWest.range = 22f;
            streetWest.color = new Color(1.0f, 0.86f, 0.65f);
            streetWest.intensity = 2.8f;
            streetWest.shadows = LightShadows.Soft;

            // Light 4: East Streetlight
            var streetEast = new GameObject("Streetlight_East").AddComponent<Light>();
            streetEast.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            streetEast.transform.SetParent(parent, false);
            streetEast.transform.position = new Vector3(14.0f, 5.8f, 2.5f);
            streetEast.type = LightType.Point;
            streetEast.range = 22f;
            streetEast.color = new Color(1.0f, 0.86f, 0.65f);
            streetEast.intensity = 2.8f;
            streetEast.shadows = LightShadows.Soft;

            // Ambient fills (No shadows)
            var skyFill = new GameObject("SkyFill").AddComponent<Light>();
            skyFill.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            skyFill.transform.SetParent(parent, false);
            skyFill.transform.rotation = Quaternion.Euler(55f, -140f, 0f);
            skyFill.type = LightType.Directional;
            skyFill.color = new Color(0.35f, 0.45f, 0.70f);
            skyFill.intensity = 0.45f;
            skyFill.shadows = LightShadows.None;

            var alleyLight = new GameObject("Alley_FillLight").AddComponent<Light>();
            alleyLight.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            alleyLight.transform.SetParent(parent, false);
            alleyLight.transform.position = new Vector3(18.5f, 4.0f, -8.0f);
            alleyLight.type = LightType.Point;
            alleyLight.range = 14.0f;
            alleyLight.color = new Color(1.0f, 0.82f, 0.60f);
            alleyLight.intensity = 2.2f;
            alleyLight.shadows = LightShadows.None;
        }

        private static void BuildWalkwaysGraph(Transform root)
        {
            // Empty parent Walkways per §5
            var walkways = new GameObject("Walkways");
            walkways.layer = TramChanhLayers.EnvironmentIndex;
            walkways.transform.SetParent(root, false);

            // 1. SidewalkNorth (spacing 2.5m, x in [-40, 40], z = 14.5m)
            var swNorth = new GameObject("SidewalkNorth");
            swNorth.layer = TramChanhLayers.EnvironmentIndex;
            swNorth.transform.SetParent(walkways.transform, false);
            int idx = 1;
            for (float x = -40f; x <= 40.1f; x += 2.5f)
            {
                var wp = new GameObject($"WP_SidewalkNorth_{idx:D2}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(swNorth.transform, false);
                wp.transform.localPosition = new Vector3(x, 0.0f, 14.5f);
                idx++;
            }

            // 2. SidewalkSouth (spacing 2.5m, x in [-40, 40], z = 1.8m)
            var swSouth = new GameObject("SidewalkSouth");
            swSouth.layer = TramChanhLayers.EnvironmentIndex;
            swSouth.transform.SetParent(walkways.transform, false);
            idx = 1;
            for (float x = -40f; x <= 40.1f; x += 2.5f)
            {
                var wp = new GameObject($"WP_SidewalkSouth_{idx:D2}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(swSouth.transform, false);
                wp.transform.localPosition = new Vector3(x, 0.0f, 1.8f);
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

            // 4. RoadEast (motorbikes heading +X, spacing 4m, z = 5.85m, y = -0.15m)
            var roadEast = new GameObject("RoadEast");
            roadEast.layer = TramChanhLayers.EnvironmentIndex;
            roadEast.transform.SetParent(walkways.transform, false);
            idx = 1;
            for (float x = -40f; x <= 40.1f; x += 4.0f)
            {
                var wp = new GameObject($"WP_RoadEast_{idx:D2}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(roadEast.transform, false);
                wp.transform.localPosition = new Vector3(x, -0.15f, 5.85f);
                idx++;
            }

            // 5. RoadWest (motorbikes heading -X, spacing 4m, z = 10.35m, y = -0.15m)
            var roadWest = new GameObject("RoadWest");
            roadWest.layer = TramChanhLayers.EnvironmentIndex;
            roadWest.transform.SetParent(walkways.transform, false);
            idx = 1;
            for (float x = 40f; x >= -40.1f; x -= 4.0f)
            {
                var wp = new GameObject($"WP_RoadWest_{idx:D2}");
                wp.layer = TramChanhLayers.EnvironmentIndex;
                wp.transform.SetParent(roadWest.transform, false);
                wp.transform.localPosition = new Vector3(x, -0.15f, 10.35f);
                idx++;
            }

            // Direct children of root per Section 5:
            // StreetEntryWest & StreetEntryEast (hidden behind building edges)
            var entryWest = new GameObject("StreetEntryWest");
            entryWest.layer = TramChanhLayers.EnvironmentIndex;
            entryWest.transform.SetParent(root, false);
            entryWest.transform.localPosition = new Vector3(-41.0f, 0.0f, 1.8f);
            entryWest.transform.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

            var entryEast = new GameObject("StreetEntryEast");
            entryEast.layer = TramChanhLayers.EnvironmentIndex;
            entryEast.transform.SetParent(root, false);
            entryEast.transform.localPosition = new Vector3(41.0f, 0.0f, 1.8f);
            entryEast.transform.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

            // TableApproach_01..10 beside each table's open side, facing the table, on the ground (y = 0)
            var approachSpecs = new TableApproachSpec[]
            {
                new TableApproachSpec { name = "TableApproach_01", pos = new Vector3(-2.10f, 0f,  0.90f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_02", pos = new Vector3(-3.35f, 0f, -0.60f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_03", pos = new Vector3(-4.60f, 0f,  0.90f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_04", pos = new Vector3(-5.85f, 0f, -0.60f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_05", pos = new Vector3(-7.10f, 0f,  0.90f), lookDir = new Vector3( 0f, 0f,  1f) },
                new TableApproachSpec { name = "TableApproach_06", pos = new Vector3(-3.35f, 0f, -1.00f), lookDir = new Vector3( 0f, 0f, -1f) },
                new TableApproachSpec { name = "TableApproach_07", pos = new Vector3(-5.85f, 0f, -1.00f), lookDir = new Vector3( 0f, 0f, -1f) },
                new TableApproachSpec { name = "TableApproach_08", pos = new Vector3( 3.40f, 0f,  0.60f), lookDir = new Vector3( 1f, 0f,  0f) },
                new TableApproachSpec { name = "TableApproach_09", pos = new Vector3( 4.80f, 0f, -0.90f), lookDir = new Vector3( 1f, 0f,  0f) },
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
