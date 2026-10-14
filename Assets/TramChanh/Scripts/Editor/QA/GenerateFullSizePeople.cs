using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TramChanh.EditorScripts
{
    public static class GenerateFullSizePeople
    {
        private static readonly string[] FbxPaths = new[]
        {
            "Assets/TramChanh/Art/Models/People/SM_Person_MaleCasual.fbx",   // 01
            "Assets/TramChanh/Art/Models/People/SM_Person_FemaleTankTop.fbx", // 02
            "Assets/TramChanh/Art/Models/People/SM_Person_MaleHoodie.fbx",    // 03
            "Assets/TramChanh/Art/Models/People/SM_Person_FemaleCasual.fbx",  // 04
            "Assets/TramChanh/Art/Models/People/SM_Person_MaleWorker.fbx",    // 05
            "Assets/TramChanh/Art/Models/People/SM_Person_FemaleDress.fbx",   // 06
        };

        private static readonly string[] PrefabPaths = new[]
        {
            "Assets/TramChanh/Prefabs/People/PF_Person_01.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_02.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_03.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_04.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_05.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_06.prefab",
        };

        private static readonly string[] MeshAssetPaths = new[]
        {
            "Assets/TramChanh/Art/Models/People/M_Person_01_LOD0.asset",
            "Assets/TramChanh/Art/Models/People/M_Person_02_LOD0.asset",
            "Assets/TramChanh/Art/Models/People/M_Person_03_LOD0.asset",
            "Assets/TramChanh/Art/Models/People/M_Person_04_LOD0.asset",
            "Assets/TramChanh/Art/Models/People/M_Person_05_LOD0.asset",
            "Assets/TramChanh/Art/Models/People/M_Person_06_LOD0.asset",
        };

        public static void Execute()
        {
            Debug.Log("[GeneratePeople] Starting full-size character mesh generation...");

            var matA = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/People/MAT_Person_Clothing_A.mat");
            var matB = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/People/MAT_Person_Clothing_B.mat");

            for (int charIdx = 0; charIdx < 6; charIdx++)
            {
                string fbxPath = FbxPaths[charIdx];
                string pfPath = PrefabPaths[charIdx];
                string meshPath = MeshAssetPaths[charIdx];

                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                var fbxSmrs = fbx.GetComponentsInChildren<SkinnedMeshRenderer>();
                
                Mesh srcMesh;
                Transform[] fbxBones;
                if (fbxSmrs.Length == 1)
                {
                    srcMesh = fbxSmrs[0].sharedMesh;
                    fbxBones = fbxSmrs[0].bones;
                }
                else
                {
                    srcMesh = CombineModularSmrs(fbxSmrs);
                    fbxBones = fbxSmrs[0].bones;
                }

                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(pfPath);
                var pfInst = Object.Instantiate(pf);

                // Build dictionary of canonical bones
                var canonicalBones = new Dictionary<string, Transform>();
                foreach (var t in pfInst.GetComponentsInChildren<Transform>(true))
                {
                    canonicalBones[t.name] = t;
                }

                // Map FBX bones to canonical bone names
                int[] boneMap = new int[fbxBones.Length];
                for (int b = 0; b < fbxBones.Length; b++)
                {
                    string fbxBoneName = fbxBones[b].name;
                    string targetName = MapFbxBoneToCanonical(fbxBoneName);
                    boneMap[b] = GetCanonicalBoneIndex(targetName);
                }

                // 21 Canonical bones in order
                string[] canonicalNames = new string[]
                {
                    "Hips", "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
                    "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes",
                    "Spine", "Chest", "Neck", "Head",
                    "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
                    "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand"
                };

                Transform[] bonesArray = new Transform[canonicalNames.Length];
                for (int b = 0; b < canonicalNames.Length; b++)
                {
                    bonesArray[b] = canonicalBones[canonicalNames[b]];
                }

                // Calculate scale factor and coordinate conversion
                // Find raw mesh bounds in FBX space
                Vector3[] rawVerts = srcMesh.vertices;
                Vector3 rawMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 rawMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                foreach (var v in rawVerts)
                {
                    rawMin = Vector3.Min(rawMin, v);
                    rawMax = Vector3.Max(rawMax, v);
                }

                float rawHeight = rawMax.z - rawMin.z;
                if (rawHeight < 0.001f) rawHeight = rawMax.y - rawMin.y;

                float targetHeight = 1.74f;
                float scale = targetHeight / rawHeight;

                Debug.Log($"[GeneratePeople] Char {charIdx+1} ({fbx.name}): rawHeight={rawHeight:F5}, scale={scale:F2}");

                // Transform vertices into Unity space:
                // FBX: Z is UP, Y is -depth, X is width
                // Unity: Y is UP, Z is depth, X is width
                Vector3[] newVerts = new Vector3[rawVerts.Length];
                for (int v = 0; v < rawVerts.Length; v++)
                {
                    float x = rawVerts[v].x * scale;
                    float y = (rawVerts[v].z - rawMin.z) * scale; // align foot base to y = 0
                    float z = -rawVerts[v].y * scale;
                    newVerts[v] = new Vector3(x, y, z);
                }

                Vector3[] rawNormals = srcMesh.normals;
                Vector3[] newNormals = new Vector3[rawNormals.Length];
                for (int v = 0; v < rawNormals.Length; v++)
                {
                    newNormals[v] = new Vector3(rawNormals[v].x, rawNormals[v].z, -rawNormals[v].y).normalized;
                }

                // Map bone weights
                BoneWeight[] rawWeights = srcMesh.boneWeights;
                BoneWeight[] newWeights = new BoneWeight[rawWeights.Length];
                for (int v = 0; v < rawWeights.Length; v++)
                {
                    var bw = rawWeights[v];
                    var nbw = new BoneWeight
                    {
                        boneIndex0 = boneMap[bw.boneIndex0],
                        weight0 = bw.weight0,
                        boneIndex1 = boneMap[bw.boneIndex1],
                        weight1 = bw.weight1,
                        boneIndex2 = boneMap[bw.boneIndex2],
                        weight2 = bw.weight2,
                        boneIndex3 = boneMap[bw.boneIndex3],
                        weight3 = bw.weight3,
                    };
                    newWeights[v] = nbw;
                }

                // Bindposes for canonical bones
                // bindpose[i] = bone[i].worldToLocalMatrix * smr.localToWorldMatrix
                var targetSmr = pfInst.GetComponentInChildren<SkinnedMeshRenderer>();
                Transform lod0Transform = pfInst.transform.Find("LOD0") ?? targetSmr.transform;
                Matrix4x4 smrToWorld = lod0Transform.localToWorldMatrix;

                Matrix4x4[] bindposes = new Matrix4x4[canonicalNames.Length];
                for (int b = 0; b < canonicalNames.Length; b++)
                {
                    bindposes[b] = bonesArray[b].worldToLocalMatrix * smrToWorld;
                }

                // Create and save new mesh
                var newMesh = new Mesh();
                newMesh.name = System.IO.Path.GetFileNameWithoutExtension(meshPath);
                newMesh.vertices = newVerts;
                newMesh.normals = newNormals;
                newMesh.uv = srcMesh.uv;
                newMesh.triangles = srcMesh.triangles;
                newMesh.boneWeights = newWeights;
                newMesh.bindposes = bindposes;
                newMesh.subMeshCount = srcMesh.subMeshCount;
                for (int s = 0; s < srcMesh.subMeshCount; s++)
                {
                    newMesh.SetTriangles(srcMesh.GetTriangles(s), s);
                }
                newMesh.RecalculateBounds();

                // Save mesh asset
                AssetDatabase.CreateAsset(newMesh, meshPath);

                // Also update LOD1 and LOD2 with same mesh (or simplified)
                string lod1Path = meshPath.Replace("LOD0", "LOD1");
                string lod2Path = meshPath.Replace("LOD0", "LOD2");
                var mesh1 = Object.Instantiate(newMesh);
                mesh1.name = System.IO.Path.GetFileNameWithoutExtension(lod1Path);
                AssetDatabase.CreateAsset(mesh1, lod1Path);
                var mesh2 = Object.Instantiate(newMesh);
                mesh2.name = System.IO.Path.GetFileNameWithoutExtension(lod2Path);
                AssetDatabase.CreateAsset(mesh2, lod2Path);

                // Update SkinnedMeshRenderer on prefab instance
                Material assignedMat = (charIdx < 3) ? matA : matB;
                foreach (var smr in pfInst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.bones = bonesArray;
                    smr.rootBone = canonicalBones["Hips"];
                    if (smr.name == "LOD0") smr.sharedMesh = newMesh;
                    else if (smr.name == "LOD1") smr.sharedMesh = mesh1;
                    else smr.sharedMesh = mesh2;

                    Material[] mats = new Material[smr.sharedMesh.subMeshCount];
                    for (int m = 0; m < mats.Length; m++) mats[m] = assignedMat;
                    smr.sharedMaterials = mats;
                }

                PrefabUtility.SaveAsPrefabAsset(pfInst, pfPath);
                Object.DestroyImmediate(pfInst);

                Debug.Log($"[GeneratePeople] Saved upgraded {pfPath} with {meshPath} (Bounds size={newMesh.bounds.size}, Tris={newMesh.triangles.Length/3})");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[GeneratePeople] Successfully upgraded all 6 pedestrian prefabs to full size!");
        }

        private static Mesh CombineModularSmrs(SkinnedMeshRenderer[] smrs)
        {
            var combined = new Mesh();
            combined.name = "CombinedModularMesh";

            var allVerts = new List<Vector3>();
            var allNormals = new List<Vector3>();
            var allUvs = new List<Vector2>();
            var allWeights = new List<BoneWeight>();
            var allTris = new List<int>();

            int vertOffset = 0;
            foreach (var smr in smrs)
            {
                var m = smr.sharedMesh;
                if (m == null) continue;

                Vector3[] verts = m.vertices;
                Vector3[] norms = m.normals;
                Vector2[] uvs = m.uv;
                BoneWeight[] weights = m.boneWeights;
                int[] tris = m.triangles;

                allVerts.AddRange(verts);
                if (norms != null && norms.Length == verts.Length) allNormals.AddRange(norms);
                if (uvs != null && uvs.Length == verts.Length) allUvs.AddRange(uvs);
                if (weights != null && weights.Length == verts.Length) allWeights.AddRange(weights);

                for (int t = 0; t < tris.Length; t++)
                {
                    allTris.Add(tris[t] + vertOffset);
                }

                vertOffset += verts.Length;
            }

            combined.vertices = allVerts.ToArray();
            if (allNormals.Count == allVerts.Count) combined.normals = allNormals.ToArray();
            if (allUvs.Count == allVerts.Count) combined.uv = allUvs.ToArray();
            if (allWeights.Count == allVerts.Count) combined.boneWeights = allWeights.ToArray();
            combined.triangles = allTris.ToArray();
            combined.RecalculateBounds();

            return combined;
        }

        private static string MapFbxBoneToCanonical(string fbxBoneName)
        {
            switch (fbxBoneName)
            {
                case "Foot.L": return "LeftFoot";
                case "Toe.L": return "LeftToes";
                case "LowerLeg.L": return "LeftLowerLeg";
                case "UpperLeg.L": return "LeftUpperLeg";

                case "Foot.R": return "RightFoot";
                case "Toe.R": return "RightToes";
                case "LowerLeg.R": return "RightLowerLeg";
                case "UpperLeg.R": return "RightUpperLeg";

                case "Hips":
                case "Body":
                case "Bone":
                    return "Hips";

                case "Abdomen": return "Spine";
                case "Torso": return "Chest";
                case "Neck": return "Neck";
                case "Head": return "Head";

                case "Shoulder.L": return "LeftShoulder";
                case "UpperArm.L": return "LeftUpperArm";
                case "LowerArm.L": return "LeftLowerArm";
                case "Palm.L":
                case "MiddleHand.L":
                case "Fingers.L":
                case "Thumb1.L":
                case "Thumb2.L":
                    return "LeftHand";

                case "Shoulder.R": return "RightShoulder";
                case "UpperArm.R": return "RightUpperArm";
                case "LowerArm.R": return "RightLowerArm";
                case "Palm.R":
                case "MiddleHand.R":
                case "Fingers.R":
                case "Thumb1.R":
                case "Thumb2.R":
                    return "RightHand";

                default:
                    if (fbxBoneName.Contains(".L")) return "LeftUpperArm";
                    if (fbxBoneName.Contains(".R")) return "RightUpperArm";
                    return "Hips";
            }
        }

        private static int GetCanonicalBoneIndex(string name)
        {
            switch (name)
            {
                case "Hips": return 0;
                case "LeftUpperLeg": return 1;
                case "LeftLowerLeg": return 2;
                case "LeftFoot": return 3;
                case "LeftToes": return 4;
                case "RightUpperLeg": return 5;
                case "RightLowerLeg": return 6;
                case "RightFoot": return 7;
                case "RightToes": return 8;
                case "Spine": return 9;
                case "Chest": return 10;
                case "Neck": return 11;
                case "Head": return 12;
                case "LeftShoulder": return 13;
                case "LeftUpperArm": return 14;
                case "LeftLowerArm": return 15;
                case "LeftHand": return 16;
                case "RightShoulder": return 17;
                case "RightUpperArm": return 18;
                case "RightLowerArm": return 19;
                case "RightHand": return 20;
                default: return 0;
            }
        }
    }
}
