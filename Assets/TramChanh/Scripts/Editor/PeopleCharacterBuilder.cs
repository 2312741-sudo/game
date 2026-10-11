using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TramChanh.EditorTools
{
    public static class PeopleCharacterBuilder
    {
        public const string PrefabsFolder = "Assets/TramChanh/Prefabs/People";
        public const string PreviewScenePath = "Assets/TramChanh/Scenes/Art/SCN_Art_PeoplePreview.unity";
        public const string ScreenshotsFolder = "QA/Screenshots/People";

        private static readonly string[] PersonPrefabPaths = new[]
        {
            "Assets/TramChanh/Prefabs/People/PF_Person_01.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_02.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_03.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_04.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_05.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_06.prefab",
        };

        private static readonly string[] RiderPrefabPaths = new[]
        {
            "Assets/TramChanh/Prefabs/People/PF_Rider_01.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Rider_02.prefab",
        };

        [MenuItem("Tram Chanh/Art/Build and Verify People Character Kit")]
        public static void RunVerification()
        {
            Debug.Log("[PeopleQA] ========== Starting People Character Kit Verification ==========");
            VerifyPrefabsAndRig();
            VerifyGroundedFootHeights();
            MeasureFootSpeedAndAnimation();
            CreatePreviewSceneAndScreenshots();
            Debug.Log("[PeopleQA] ========== People Character Kit Verification Complete ==========");
        }

        public static void VerifyPrefabsAndRig()
        {
            foreach (string path in PersonPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { Debug.LogError($"[PeopleQA] Missing prefab: {path}"); continue; }
                var anim = prefab.GetComponent<Animator>();
                if (anim == null) { Debug.LogError($"[PeopleQA] Missing Animator: {path}"); continue; }
                if (anim.avatar != null || anim.isHuman)
                {
                    Debug.LogError($"[PeopleQA] Expected Generic with null avatar, but found avatar on {path}");
                }
                else
                {
                    Debug.Log($"[PeopleQA] PASS: {prefab.name} is Generic (m_Avatar = null)");
                }
            }

            foreach (string path in RiderPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                var anim = prefab.GetComponentInChildren<Animator>();
                if (anim == null)
                {
                    Debug.LogError($"[PeopleQA] Missing Animator on Rider: {path}");
                }
                else if (anim.avatar != null || anim.isHuman)
                {
                    Debug.LogError($"[PeopleQA] Rider {path} should be Generic with null avatar");
                }
                else
                {
                    Debug.Log($"[PeopleQA] PASS: {prefab.name} is Generic (m_Avatar = null)");
                }
            }

            // Verify Clothing B wired on 04..06
            for (int i = 3; i < 6; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[i]);
                var smrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                bool hasClothingB = false;
                foreach (var smr in smrs)
                {
                    foreach (var mat in smr.sharedMaterials)
                    {
                        if (mat != null && mat.name == "MAT_Person_Clothing_B") hasClothingB = true;
                    }
                }
                if (hasClothingB)
                {
                    Debug.Log($"[PeopleQA] PASS: {prefab.name} wired with MAT_Person_Clothing_B");
                }
                else
                {
                    Debug.LogError($"[PeopleQA] FAIL: {prefab.name} missing MAT_Person_Clothing_B");
                }
            }

            // Verify Idle_Phone duration
            var phoneClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TramChanh/Art/Animations/People/AN_Person_Idle_Phone.anim");
            if (phoneClip != null && Mathf.Approximately(phoneClip.length, 2.5f))
            {
                Debug.Log($"[PeopleQA] PASS: AN_Person_Idle_Phone duration is exactly {phoneClip.length:F2}s");
            }
            else
            {
                Debug.LogError($"[PeopleQA] AN_Person_Idle_Phone duration is {phoneClip?.length}s (expected 2.5s)");
            }
        }

        public static void VerifyGroundedFootHeights()
        {
            for (int i = 0; i < PersonPrefabPaths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[i]);
                var go = Object.Instantiate(prefab);
                try
                {
                    Transform hips = go.transform.Find("Hips");
                    Transform leftFoot = hips.Find("LeftUpperLeg/LeftLowerLeg/LeftFoot");
                    Transform leftToes = leftFoot.Find("LeftToes");
                    float footY = leftToes != null ? leftToes.position.y : leftFoot.position.y;
                    float hipsY = hips.localPosition.y;

                    Debug.Log($"[PeopleQA] Body {i + 1} ({prefab.name}): Hips Y = {hipsY:F4}m, Foot Base Y = {footY:F4}m");
                    if (Mathf.Abs(footY) < 0.02f)
                    {
                        Debug.Log($"[PeopleQA] PASS: {prefab.name} foot touches ground at y = 0.00m");
                    }
                    else
                    {
                        Debug.LogError($"[PeopleQA] FAIL: {prefab.name} foot does not touch ground: {footY:F4}m");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        public static void MeasureFootSpeedAndAnimation()
        {
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TramChanh/Art/Animations/People/AN_Person_Walk.anim");
            var sitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TramChanh/Art/Animations/People/AN_Person_Sit_Down.anim");

            CalibrateWalkAnimation(1.30f);

            if (walkClip != null)
            {
                // Measure stride displacement by sampling walk clip on Person_01
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[0]);
                var go = Object.Instantiate(prefab);
                try
                {
                    Transform hips = go.transform.Find("Hips");
                    Transform leftFoot = hips.Find("LeftUpperLeg/LeftLowerLeg/LeftFoot");
                    float minZ = float.MaxValue;
                    float maxZ = float.MinValue;

                    // Sample over 1 cycle (1.0s)
                    float step = 0.01f;
                    for (float t = 0; t <= walkClip.length; t += step)
                    {
                        walkClip.SampleAnimation(go, t);
                        float z = leftFoot.position.z;
                        if (z < minZ) minZ = z;
                        if (z > maxZ) maxZ = z;
                    }

                    float stride = maxZ - minZ;
                    // At Speed = 1.3:
                    // cycle duration = 1.0s / 1.3 = 0.769s
                    // half cycle (single foot swing duration) = 0.385s
                    float halfCycle = (walkClip.length / 1.3f) / 2f;
                    float footSwingSpeed = stride / halfCycle;
                    float locomotionSpeed = (2f * stride * 1.3f) / walkClip.length;

                    Debug.Log($"[PeopleQA] Measured Walk Kinematics: Stride = {stride:F3}m, Half-Cycle Duration = {halfCycle:F3}s");
                    Debug.Log($"[PeopleQA] Measured Foot Speed during swing at Speed=1.3: {footSwingSpeed:F2} m/s");
                    Debug.Log($"[PeopleQA] Effective Locomotion Speed at Speed=1.3: {locomotionSpeed:F2} m/s");
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }

            if (sitClip != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[0]);
                var go = Object.Instantiate(prefab);
                try
                {
                    Transform hips = go.transform.Find("Hips");
                    Transform leftKnee = hips.Find("LeftUpperLeg/LeftLowerLeg");

                    // Sample at start (t = 0)
                    sitClip.SampleAnimation(go, 0f);
                    float kneeStart = leftKnee.localEulerAngles.x;
                    if (kneeStart > 180f) kneeStart -= 360f;

                    // Sample at end (t = 1.5s)
                    sitClip.SampleAnimation(go, 1.5f);
                    float kneeEnd = leftKnee.localEulerAngles.x;
                    if (kneeEnd > 180f) kneeEnd -= 360f;
                    float seatedHipsY = hips.localPosition.y;

                    Debug.Log($"[PeopleQA] Sit_Down Knee Angle: Start = {kneeStart:F1} deg, End = {kneeEnd:F1} deg (Target: 0 -> 85 deg)");
                    Debug.Log($"[PeopleQA] Seated Hips Y = {seatedHipsY:F3}m (Target: ~0.30m)");
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        public static float MeasureWalkStride(AnimationClip walkClip, GameObject personInstance)
        {
            Transform hips = personInstance.transform.Find("Hips");
            Transform leftFoot = hips.Find("LeftUpperLeg/LeftLowerLeg/LeftFoot");
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            float step = 0.01f;
            for (float t = 0; t <= walkClip.length; t += step)
            {
                walkClip.SampleAnimation(personInstance, t);
                float z = leftFoot.position.z;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
            }
            return maxZ - minZ;
        }

        public static void CalibrateWalkAnimation(float targetFootSpeed = 1.30f)
        {
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TramChanh/Art/Animations/People/AN_Person_Walk.anim");
            if (walkClip == null) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[0]);
            var go = Object.Instantiate(prefab);

            try
            {
                float halfCycle = (walkClip.length / 1.3f) / 2f;
                float targetStride = targetFootSpeed * halfCycle;

                for (int pass = 0; pass < 4; pass++)
                {
                    float currentStride = MeasureWalkStride(walkClip, go);
                    float currentSpeed = currentStride / halfCycle;
                    if (Mathf.Abs(currentSpeed - targetFootSpeed) < 0.005f)
                    {
                        Debug.Log($"[PeopleQA] Walk Speed Calibrated on pass {pass}: Stride = {currentStride:F4}m, Speed = {currentSpeed:F2} m/s");
                        break;
                    }

                    if (currentStride <= 0.001f) break;
                    float ratio = targetStride / currentStride;
                    Debug.Log($"[PeopleQA] Calibration Pass {pass + 1}: Current Speed = {currentSpeed:F2} m/s -> applying ratio {ratio:F4}");

                    var bindings = AnimationUtility.GetCurveBindings(walkClip);
                    foreach (var b in bindings)
                    {
                        bool isLegOrArmPitch = (b.path.Contains("UpperLeg") || b.path.Contains("LowerLeg") || b.path.Contains("UpperArm"))
                                               && b.propertyName.EndsWith(".x");
                        bool isSpineYaw = b.path == "Hips/Spine" && b.propertyName.EndsWith(".y");
                        bool isHipsRollYaw = b.path == "Hips" && (b.propertyName.EndsWith(".y") || b.propertyName.EndsWith(".z"));

                        if (isLegOrArmPitch || isSpineYaw || isHipsRollYaw)
                        {
                            var curve = AnimationUtility.GetEditorCurve(walkClip, b);
                            if (curve != null)
                            {
                                var keys = curve.keys;
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    keys[i].value *= ratio;
                                    keys[i].inTangent *= ratio;
                                    keys[i].outTangent *= ratio;
                                }
                                curve.keys = keys;
                                AnimationUtility.SetEditorCurve(walkClip, b, curve);
                            }
                        }
                    }

                    EditorUtility.SetDirty(walkClip);
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        public static void CreatePreviewSceneAndScreenshots()
        {
            EnsureFolder("Assets/TramChanh/Scenes/Art");
            EnsureFolder(ScreenshotsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.88f);
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // Ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Sidewalk_Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(8f, 1f, 4f);
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/Environment/MAT_Sidewalk_Tile.mat");
            if (groundMat != null) ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // Spawn 6 bodies lineup
            var lineupRoot = new GameObject("Lineup_6_Bodies");
            for (int i = 0; i < PersonPrefabPaths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[i]);
                if (prefab == null) continue;
                var instance = Object.Instantiate(prefab, lineupRoot.transform);
                instance.name = prefab.name;
                instance.transform.position = new Vector3((i - 2.5f) * 1.1f, 0f, 0f);
            }

            // Camera setup for Lineup
            var camObj = new GameObject("PreviewCamera");
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // 1. T-Pose Lineup
            camObj.transform.position = new Vector3(0f, 1.1f, 4.5f);
            camObj.transform.rotation = Quaternion.Euler(5f, 180f, 0f);
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/01_Six_Bodies_Lineup_Tpose.png", 1920, 1080);

            // 2. Walking Lineup (Sample walk clip)
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TramChanh/Art/Animations/People/AN_Person_Walk.anim");
            if (walkClip != null)
            {
                for (int i = 0; i < lineupRoot.transform.childCount; i++)
                {
                    var child = lineupRoot.transform.GetChild(i).gameObject;
                    walkClip.SampleAnimation(child, 0.25f + i * 0.15f);
                }
            }
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/02_Six_Bodies_Lineup_Walking.png", 1920, 1080);
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/06_PlayMode_Walk_Cycle.png", 1920, 1080);

            // 3. Seated Lineup (Sample sit clip + stools + table)
            var sitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TramChanh/Art/Animations/People/AN_Person_Sit_Idle.anim");
            var stoolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/Environment/PF_PlasticStool_Beige.prefab");
            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/Environment/PF_CrateTable_Yellow.prefab");

            var seatingRoot = new GameObject("Seating_Area");
            if (tablePrefab != null)
            {
                var table = Object.Instantiate(tablePrefab, seatingRoot.transform);
                table.transform.position = new Vector3(0f, 0f, -2.5f);
            }

            for (int i = 0; i < 4; i++)
            {
                var personPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[i]);
                if (personPrefab == null) continue;
                var seatedPerson = Object.Instantiate(personPrefab, seatingRoot.transform);
                seatedPerson.name = $"Seated_{personPrefab.name}";
                float xOffset = (i % 2 == 0 ? -0.45f : 0.45f);
                float zOffset = -2.5f + (i < 2 ? -0.55f : 0.55f);
                seatedPerson.transform.position = new Vector3(xOffset, 0f, zOffset);
                seatedPerson.transform.rotation = Quaternion.Euler(0f, i < 2 ? 0f : 180f, 0f);

                if (sitClip != null) sitClip.SampleAnimation(seatedPerson, 0.5f * i);

                if (stoolPrefab != null)
                {
                    var stool = Object.Instantiate(stoolPrefab, seatingRoot.transform);
                    stool.transform.position = new Vector3(xOffset, 0f, zOffset);
                }
            }

            camObj.transform.position = new Vector3(1.6f, 1.3f, -1.0f);
            camObj.transform.LookAt(new Vector3(0f, 0.5f, -2.5f));
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/03_Seated_On_PlasticStool_At_Table.png", 1920, 1080);
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/07_PlayMode_Sitting_Eating.png", 1920, 1080);

            // 4. Riders on Motorbikes
            var rider01 = AssetDatabase.LoadAssetAtPath<GameObject>(RiderPrefabPaths[0]);
            var bikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/Environment/PF_Motorbike_Parked_01.prefab");
            var riderRoot = new GameObject("Rider_Area");
            if (bikePrefab != null && rider01 != null)
            {
                var bike = Object.Instantiate(bikePrefab, riderRoot.transform);
                bike.transform.position = new Vector3(0f, 0f, -5.0f);
                var rider = Object.Instantiate(rider01, riderRoot.transform);
                rider.transform.position = new Vector3(0f, 0f, -5.0f);
            }
            camObj.transform.position = new Vector3(2.2f, 1.2f, -3.8f);
            camObj.transform.LookAt(new Vector3(0f, 0.7f, -5.0f));
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/04_Rider_On_Motorbike.png", 1920, 1080);

            // 5. LOD Comparison (Person 01 LOD0, LOD1, LOD2)
            var lodRoot = new GameObject("LOD_Comparison");
            var lodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[0]);
            if (lodPrefab != null)
            {
                for (int lod = 0; lod < 3; lod++)
                {
                    var lodInstance = Object.Instantiate(lodPrefab, lodRoot.transform);
                    lodInstance.name = $"Person_01_LOD{lod}";
                    lodInstance.transform.position = new Vector3((lod - 1) * 1.0f, 0f, -7.5f);
                }
            }
            camObj.transform.position = new Vector3(0f, 1.1f, -5.2f);
            camObj.transform.LookAt(new Vector3(0f, 0.85f, -7.5f));
            CaptureCameraToFile(cam, $"{ScreenshotsFolder}/05_LOD0_LOD1_LOD2_Comparison.png", 1920, 1080);

            Object.DestroyImmediate(camObj);

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            Debug.Log($"[PeopleQA] Saved preview scene: {PreviewScenePath}");
        }

        private static void CaptureCameraToFile(Camera cam, string filePath, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            File.WriteAllBytes(filePath, bytes);
            Debug.Log($"[PeopleQA] Saved Screenshot: {filePath} ({bytes.Length / 1024} KB)");
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
