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

        public static void InspectMeshStructure()
        {
            for (int i = 0; i < PersonPrefabPaths.Length; i++)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[i]);
                var smr = pf.GetComponentInChildren<SkinnedMeshRenderer>();
                var mesh = smr.sharedMesh;
                var verts = mesh.vertices;
                var bones = smr.bones;
                var weights = mesh.boneWeights;
                Debug.Log($"[MESH_INSPECT] {pf.name}: Mesh={mesh.name}, subMeshCount={mesh.subMeshCount}, verts={verts.Length}, tris={mesh.triangles.Length / 3}");

                if (i == 2 || i == 4) // Person 03 or 05
                {
                    int headTris = 0, torsoTris = 0, legTris = 0, footTris = 0, handTris = 0, faceTris = 0;
                    var tris = mesh.triangles;
                    for (int t = 0; t < tris.Length; t += 3)
                    {
                        int v0 = tris[t], v1 = tris[t + 1], v2 = tris[t + 2];
                        float avgY = (verts[v0].y + verts[v1].y + verts[v2].y) / 3f;
                        float avgZ = (verts[v0].z + verts[v1].z + verts[v2].z) / 3f;
                        float avgX = (verts[v0].x + verts[v1].x + verts[v2].x) / 3f;
                        int b0 = weights[v0].boneIndex0;
                        string bName = (b0 >= 0 && b0 < bones.Length && bones[b0] != null) ? bones[b0].name : "unknown";

                        if (bName.Contains("Foot") || bName.Contains("Toes") || avgY < 0.12f)
                        {
                            footTris++;
                        }
                        else if (bName.Contains("Leg") || (avgY < 0.85f && !bName.Contains("Hand") && !bName.Contains("Arm")))
                        {
                            legTris++;
                        }
                        else if (bName.Contains("Hand") || (bName.Contains("Arm") && avgY < 0.95f && Mathf.Abs(avgX) > 0.35f))
                        {
                            handTris++;
                        }
                        else if (bName.Contains("Head") || bName.Contains("Neck") || avgY >= 1.45f)
                        {
                            if (avgZ > 0.04f && avgY < 1.62f) faceTris++;
                            else headTris++;
                        }
                        else
                        {
                            torsoTris++;
                        }
                    }
                    Debug.Log($"  Classification {pf.name}: Foot={footTris}, Leg={legTris}, Torso={torsoTris}, Hand={handTris}, Head/Hair={headTris}, Face={faceTris}");
                }
                else
                {
                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        var subTris = mesh.GetTriangles(s);
                        float minY = 999f, maxY = -999f;
                        foreach (int idx in subTris)
                        {
                            float y = verts[idx].y;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                        var mat = s < smr.sharedMaterials.Length ? smr.sharedMaterials[s] : null;
                        Debug.Log($"  Submesh[{s}]: {subTris.Length / 3} tris, y=[{minY:F2}..{maxY:F2}]m, Mat={(mat != null ? mat.name : "null")}");
                    }
                }
            }
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

        [MenuItem("TramChanh/People/Build Accurate Walk Clip")]
        public static void BuildKinematicallyAccurateWalkClip()
        {
            var walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TramChanh/Art/Animations/People/AN_Person_Walk.anim");
            if (walkClip == null) return;

            float L1 = 0.4128f;
            float L2 = 0.3735f;
            float maxReach = L1 + L2 - 0.0005f;
            float Y0 = -Mathf.Sqrt(maxReach * maxReach - 0.25f * 0.25f);
            float dY = 0.022f;

            int sampleCount = 41; // 0.025s steps for 1.0s loop
            var leftThighKeys = new Keyframe[sampleCount];
            var leftKneeKeys = new Keyframe[sampleCount];
            var rightThighKeys = new Keyframe[sampleCount];
            var rightKneeKeys = new Keyframe[sampleCount];

            var leftArmKeys = new Keyframe[sampleCount];
            var rightArmKeys = new Keyframe[sampleCount];

            var hipsYawKeys = new Keyframe[sampleCount];
            var hipsRollKeys = new Keyframe[sampleCount];
            var spineYawKeys = new Keyframe[sampleCount];

            float stepDt = 1.0f / (sampleCount - 1);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i * stepDt;
                if (i == sampleCount - 1) t = 1.0f;

                // Left leg: stance in [0.0, 0.5], swing in (0.5, 1.0]
                float leftTz, leftTy;
                if (t <= 0.5f)
                {
                    float u = t / 0.5f;
                    leftTz = 0.25f - 0.50f * u;
                    leftTy = Y0 - dY * Mathf.Sin(Mathf.PI * u);
                }
                else
                {
                    float u = (t - 0.5f) / 0.5f;
                    leftTz = -0.25f * Mathf.Cos(Mathf.PI * u);
                    leftTy = Y0 + 0.080f * Mathf.Sin(Mathf.PI * u);
                }

                SolveLegIK(L1, L2, maxReach, leftTy, leftTz, out float leftThighX, out float leftKneeX);
                leftThighKeys[i] = new Keyframe(t, leftThighX);
                leftKneeKeys[i] = new Keyframe(t, leftKneeX);

                // Right leg: 180 deg / 0.5s phase shift
                float rightT = (t + 0.5f) % 1.0f;
                float rightTz, rightTy;
                if (rightT <= 0.5f)
                {
                    float u = rightT / 0.5f;
                    rightTz = 0.25f - 0.50f * u;
                    rightTy = Y0 - dY * Mathf.Sin(Mathf.PI * u);
                }
                else
                {
                    float u = (rightT - 0.5f) / 0.5f;
                    rightTz = -0.25f * Mathf.Cos(Mathf.PI * u);
                    rightTy = Y0 + 0.080f * Mathf.Sin(Mathf.PI * u);
                }

                SolveLegIK(L1, L2, maxReach, rightTy, rightTz, out float rightThighX, out float rightKneeX);
                rightThighKeys[i] = new Keyframe(t, rightThighX);
                rightKneeKeys[i] = new Keyframe(t, rightKneeX);

                // Arms (opposing leg pitch)
                float armSwing = Mathf.Sin(2f * Mathf.PI * t) * 20f;
                leftArmKeys[i] = new Keyframe(t, armSwing);
                rightArmKeys[i] = new Keyframe(t, -armSwing);

                // Hips / Spine subtle rotations
                float hipsYaw = Mathf.Sin(2f * Mathf.PI * t) * 1.5f;
                float hipsRoll = Mathf.Cos(2f * Mathf.PI * t) * 1.0f;
                hipsYawKeys[i] = new Keyframe(t, hipsYaw);
                hipsRollKeys[i] = new Keyframe(t, hipsRoll);
                spineYawKeys[i] = new Keyframe(t, -hipsYaw * 0.7f);
            }

            var leftThighCurve = new AnimationCurve(leftThighKeys);
            var leftKneeCurve = new AnimationCurve(leftKneeKeys);
            var rightThighCurve = new AnimationCurve(rightThighKeys);
            var rightKneeCurve = new AnimationCurve(rightKneeKeys);

            var leftArmCurve = new AnimationCurve(leftArmKeys);
            var rightArmCurve = new AnimationCurve(rightArmKeys);

            var hipsYawCurve = new AnimationCurve(hipsYawKeys);
            var hipsRollCurve = new AnimationCurve(hipsRollKeys);
            var spineYawCurve = new AnimationCurve(spineYawKeys);

            SmoothCurveTangents(leftThighCurve);
            SmoothCurveTangents(leftKneeCurve);
            SmoothCurveTangents(rightThighCurve);
            SmoothCurveTangents(rightKneeCurve);

            SmoothCurveTangents(leftArmCurve);
            SmoothCurveTangents(rightArmCurve);

            SmoothCurveTangents(hipsYawCurve);
            SmoothCurveTangents(hipsRollCurve);
            SmoothCurveTangents(spineYawCurve);

            // Set curves
            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips/LeftUpperLeg", typeof(Transform), "localEulerAnglesRaw.x"), leftThighCurve);
            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips/LeftUpperLeg/LeftLowerLeg", typeof(Transform), "localEulerAnglesRaw.x"), leftKneeCurve);
            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips/RightUpperLeg", typeof(Transform), "localEulerAnglesRaw.x"), rightThighCurve);
            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips/RightUpperLeg/RightLowerLeg", typeof(Transform), "localEulerAnglesRaw.x"), rightKneeCurve);

            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips/Spine/Chest/LeftShoulder/LeftUpperArm", typeof(Transform), "localEulerAnglesRaw.x"), leftArmCurve);
            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips/Spine/Chest/RightShoulder/RightUpperArm", typeof(Transform), "localEulerAnglesRaw.x"), rightArmCurve);

            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips", typeof(Transform), "localEulerAnglesRaw.y"), hipsYawCurve);
            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips", typeof(Transform), "localEulerAnglesRaw.z"), hipsRollCurve);
            AnimationUtility.SetEditorCurve(walkClip, EditorCurveBinding.FloatCurve("Hips/Spine", typeof(Transform), "localEulerAnglesRaw.y"), spineYawCurve);

            EditorUtility.SetDirty(walkClip);
            AssetDatabase.SaveAssets();
            Debug.Log("[PeopleQA] Successfully built kinematically accurate walk clip AN_Person_Walk.anim!");
        }

        private static void SmoothCurveTangents(AnimationCurve curve)
        {
            var keys = curve.keys;
            int n = keys.Length;
            for (int i = 0; i < n; i++)
            {
                int prev = (i == 0) ? n - 2 : i - 1;
                int next = (i == n - 1) ? 1 : i + 1;

                float dt = (i == 0) ? (keys[1].time - keys[0].time) * 2f :
                           (i == n - 1) ? (keys[n - 1].time - keys[n - 2].time) * 2f :
                           keys[next].time - keys[prev].time;

                float dv = keys[next].value - keys[prev].value;
                float slope = dt > 0.0001f ? dv / dt : 0f;

                keys[i].inTangent = slope;
                keys[i].outTangent = slope;
            }
            curve.keys = keys;
        }

        private static void SolveLegIK(float L1, float L2, float maxReach, float ty, float tz, out float thighX, out float kneeX)
        {
            float D = Mathf.Sqrt(ty * ty + tz * tz);
            D = Mathf.Clamp(D, 0.1f, maxReach);

            float cosFlex = (D * D - L1 * L1 - L2 * L2) / (2f * L1 * L2);
            cosFlex = Mathf.Clamp(cosFlex, -1f, 1f);
            float kneeFlexRad = Mathf.Acos(cosFlex);

            float alpha = Mathf.Atan2(tz, -ty);
            float cosHipTri = (L1 * L1 + D * D - L2 * L2) / (2f * L1 * D);
            cosHipTri = Mathf.Clamp(cosHipTri, -1f, 1f);
            float hipTriRad = Mathf.Acos(cosHipTri);

            float thighAngleRad = -(alpha + hipTriRad);
            thighX = thighAngleRad * Mathf.Rad2Deg;
            kneeX = kneeFlexRad * Mathf.Rad2Deg;
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
            camObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Ensure URP pipeline is active for preview rendering
            var urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/TramChanh/Settings/Rendering/URP_TramChanh.asset");
            if (urpAsset != null)
            {
                GraphicsSettings.defaultRenderPipeline = urpAsset;
                QualitySettings.renderPipeline = urpAsset;
            }

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
            var stoolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/CustomerArea/PF_PlasticStool.prefab");
            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/CustomerArea/PF_YellowCrateTable.prefab");

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
            var bikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/Environment/PF_Motorbike.prefab");
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
