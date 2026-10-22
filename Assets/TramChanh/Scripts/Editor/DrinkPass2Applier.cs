using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TramChanh.Editor
{
    /// <summary>
    /// Drink and tool art pass 2. Reads ArtSource/Export/drink_pass2_manifest.json (written by
    /// ArtSource/Scripts/generate_drink_meshes_v2.py) and
    ///   1. sets the import type of the listed textures (normal maps),
    ///   2. creates or updates the _v2 URP Lit materials (transparent where the manifest says),
    ///   3. makes sure the _v2 OBJ files import as plain meshes (no extracted materials),
    ///   4. swaps the mesh and the material slots on the existing prefab nodes.
    /// Hierarchy, anchor names and colliders are never touched. Old meshes and old materials are never deleted.
    /// Running it twice changes nothing the second time. Every change is logged with the prefix [DrinkPass2].
    /// </summary>
    public static class DrinkPass2Applier
    {
        private const string ManifestRelativePath = "ArtSource/Export/drink_pass2_manifest.json";
        private const string Tag = "[DrinkPass2] ";

        // ---- manifest model (JsonUtility; absent fields stay at their default) -------------------------------------
        [Serializable] private class Manifest { public int version; public ImporterInfo importer; public List<TextureInfo> textures; public List<MaterialInfo> materials; public List<MeshInfo> meshes; }
        [Serializable] private class ImporterInfo { public float globalScale = 1f; }
        [Serializable] private class TextureInfo { public string path; public string type; public bool sRGB = true; }
        [Serializable]
        private class MaterialInfo
        {
            public string name, path, shader, surface, blend, baseMap, normalMap, cull, note;
            public float[] baseColor, tiling;
            public float normalScale = 1f, metallic, smoothness = 0.5f;
            public bool zWrite = true, receiveShadows = true;
            public int queue;
        }
        [Serializable] private class MeshInfo { public string mesh, oldMesh, footprintReference, note; public List<PrefabUse> prefabs; }
        [Serializable]
        private class PrefabUse
        {
            public string prefab, node, addChild, note;
            public string[] materials;
            public bool optional, replaceBuiltinPrimitive;
            public float[] localPosition, localScale;
        }

        [MenuItem("Tram Chanh/Art/Apply Drink And Tool Pass 2")]
        public static void ApplyDefault() { Apply(false); }

        [MenuItem("Tram Chanh/Art/Apply Drink And Tool Pass 2 (include optional topping bin visuals)")]
        public static void ApplyWithOptional() { Apply(true); }

        public static void Apply(bool includeOptional)
        {
            string manifestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ManifestRelativePath));
            if (!File.Exists(manifestPath))
            {
                Debug.LogError(Tag + "manifest not found: " + manifestPath + " (run ArtSource/Scripts/generate_drink_meshes_v2.py)");
                return;
            }
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.meshes == null || manifest.materials == null)
            {
                Debug.LogError(Tag + "manifest could not be parsed: " + manifestPath);
                return;
            }

            int changes = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (TextureInfo t in manifest.textures ?? new List<TextureInfo>()) changes += EnsureTextureImport(t);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();

            var materials = new Dictionary<string, Material>();
            foreach (MaterialInfo info in manifest.materials)
            {
                Material m = EnsureMaterial(info, out int c);
                changes += c;
                if (m != null) materials[info.name] = m;
            }

            // Import the meshes once, then group the prefab work by prefab so each one is opened and saved once.
            var meshes = new Dictionary<string, Mesh>();
            foreach (MeshInfo mi in manifest.meshes)
            {
                changes += EnsureMeshImport(mi.mesh, manifest.importer);
                Mesh mesh = LoadMesh(mi.mesh);
                if (mesh == null) { Debug.LogError(Tag + "mesh did not import: " + mi.mesh); continue; }
                meshes[mi.mesh] = mesh;
            }

            var byPrefab = new Dictionary<string, List<KeyValuePair<MeshInfo, PrefabUse>>>();
            foreach (MeshInfo mi in manifest.meshes)
            {
                if (!meshes.ContainsKey(mi.mesh) || mi.prefabs == null) continue;
                foreach (PrefabUse pu in mi.prefabs)
                {
                    if (pu.optional && !includeOptional)
                    {
                        Debug.Log(Tag + "skipped optional: " + pu.prefab + " / " + pu.node + " (" + mi.mesh + ")");
                        continue;
                    }
                    if (!byPrefab.TryGetValue(pu.prefab, out var list)) byPrefab[pu.prefab] = list = new List<KeyValuePair<MeshInfo, PrefabUse>>();
                    list.Add(new KeyValuePair<MeshInfo, PrefabUse>(mi, pu));
                }
            }

            foreach (var kv in byPrefab)
                changes += ApplyToPrefab(kv.Key, kv.Value, meshes, materials);

            AssetDatabase.SaveAssets();
            Debug.Log(Tag + (changes == 0 ? "nothing to change, everything is already applied." : "done, " + changes + " change(s)."));
        }

        // ---- textures ---------------------------------------------------------------------------------------------
        private static int EnsureTextureImport(TextureInfo t)
        {
            var imp = AssetImporter.GetAtPath(t.path) as TextureImporter;
            if (imp == null)
            {
                // the file exists on disk but Unity has not imported it yet
                AssetDatabase.ImportAsset(t.path, ImportAssetOptions.ForceUpdate);
                imp = AssetImporter.GetAtPath(t.path) as TextureImporter;
                if (imp == null) { Debug.LogWarning(Tag + "texture missing: " + t.path); return 0; }
            }
            TextureImporterType want = t.type == "NormalMap" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (imp.textureType == want && imp.sRGBTexture == t.sRGB) return 0;
            imp.textureType = want;
            imp.sRGBTexture = t.sRGB;
            imp.SaveAndReimport();
            Debug.Log(Tag + "texture import set: " + t.path + " -> " + want + ", sRGB=" + t.sRGB);
            return 1;
        }

        // ---- materials --------------------------------------------------------------------------------------------
        private static Material EnsureMaterial(MaterialInfo info, out int changes)
        {
            changes = 0;
            Shader shader = Shader.Find(string.IsNullOrEmpty(info.shader) ? "Universal Render Pipeline/Lit" : info.shader);
            if (shader == null) { Debug.LogError(Tag + "shader not found: " + info.shader); return null; }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(info.path);
            bool created = mat == null;
            if (created) mat = new Material(shader) { name = info.name };

            string before = Snapshot(mat);
            mat.shader = shader;
            bool transparent = info.surface == "Transparent";

            Color color = info.baseColor != null && info.baseColor.Length >= 4
                ? new Color(info.baseColor[0], info.baseColor[1], info.baseColor[2], info.baseColor[3]) : Color.white;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Metallic", info.metallic);
            mat.SetFloat("_Smoothness", info.smoothness);
            mat.SetFloat("_Cull", info.cull == "Off" ? (float)CullMode.Off : (float)CullMode.Back);
            mat.SetFloat("_AlphaClip", 0f);

            Texture2D baseMap = LoadTexture(info.baseMap);
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetTexture("_MainTex", baseMap);
            Texture2D normal = LoadTexture(info.normalMap);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", info.normalScale);
            SetKeyword(mat, "_NORMALMAP", normal != null);
            if (info.tiling != null && info.tiling.Length >= 2) mat.SetTextureScale("_BaseMap", new Vector2(info.tiling[0], info.tiling[1]));

            SetKeyword(mat, "_RECEIVE_SHADOWS_OFF", !info.receiveShadows);
            mat.SetFloat("_ReceiveShadows", info.receiveShadows ? 1f : 0f);
            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", info.zWrite ? 1f : 0f);
                SetKeyword(mat, "_SURFACE_TYPE_TRANSPARENT", true);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = info.queue > 0 ? info.queue : (int)RenderQueue.Transparent;
                mat.SetShaderPassEnabled("ShadowCaster", false);
            }
            else
            {
                mat.SetFloat("_Surface", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1f);
                SetKeyword(mat, "_SURFACE_TYPE_TRANSPARENT", false);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = -1;
                mat.SetShaderPassEnabled("ShadowCaster", true);
            }

            if (created)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(info.path)) ?? ".");
                AssetDatabase.CreateAsset(mat, info.path);
                Debug.Log(Tag + "material created: " + info.path + (transparent ? " (transparent)" : " (opaque)"));
                changes = 1;
            }
            else if (Snapshot(mat) != before)
            {
                EditorUtility.SetDirty(mat);
                Debug.Log(Tag + "material updated: " + info.path);
                changes = 1;
            }
            return mat;
        }

        private static string Snapshot(Material m)
        {
            // cheap change detector: the values the applier writes
            string tex(string p) { Texture t = m.HasProperty(p) ? m.GetTexture(p) : null; return t == null ? "-" : AssetDatabase.GetAssetPath(t); }
            return string.Join("|", m.shader != null ? m.shader.name : "-", m.GetColor("_BaseColor").ToString(), m.GetFloat("_Surface"),
                m.GetFloat("_Smoothness"), m.GetFloat("_Metallic"), m.GetFloat("_Cull"), m.GetFloat("_ZWrite"), m.GetFloat("_BumpScale"),
                m.renderQueue, tex("_BaseMap"), tex("_BumpMap"), m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), m.IsKeywordEnabled("_NORMALMAP"),
                m.GetTextureScale("_BaseMap"));
        }

        private static void SetKeyword(Material m, string keyword, bool on)
        {
            if (on) m.EnableKeyword(keyword); else m.DisableKeyword(keyword);
        }

        private static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t == null) Debug.LogWarning(Tag + "texture not found: " + path);
            return t;
        }

        // ---- meshes -----------------------------------------------------------------------------------------------
        private static int EnsureMeshImport(string objPath, ImporterInfo info)
        {
            if (!File.Exists(Path.GetFullPath(objPath))) { Debug.LogWarning(Tag + "OBJ missing on disk: " + objPath); return 0; }
            var imp = AssetImporter.GetAtPath(objPath) as ModelImporter;
            if (imp == null)
            {
                AssetDatabase.ImportAsset(objPath, ImportAssetOptions.ForceUpdate);
                imp = AssetImporter.GetAtPath(objPath) as ModelImporter;
                if (imp == null) return 0;
            }
            float scale = info != null && info.globalScale > 0 ? info.globalScale : 1f;
            if (imp.materialImportMode == ModelImporterMaterialImportMode.None && Mathf.Approximately(imp.globalScale, scale)
                && !imp.isReadable && !imp.importCameras && !imp.importLights) return 0;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;   // the prefab nodes carry the _v2 materials
            imp.globalScale = scale;
            imp.isReadable = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.addCollider = false;                                          // colliders stay the prefab's boxes
            imp.SaveAndReimport();
            Debug.Log(Tag + "mesh import set: " + objPath);
            return 1;
        }

        private static Mesh LoadMesh(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
            Mesh direct = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (direct != null) return direct;
            foreach (UnityEngine.Object sub in AssetDatabase.LoadAllAssetsAtPath(path))
                if (sub is Mesh m) return m;
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            MeshFilter mf = go != null ? go.GetComponentInChildren<MeshFilter>() : null;
            return mf != null ? mf.sharedMesh : null;
        }

        // ---- prefabs ----------------------------------------------------------------------------------------------
        private static int ApplyToPrefab(string prefabPath, List<KeyValuePair<MeshInfo, PrefabUse>> work,
                                         Dictionary<string, Mesh> meshes, Dictionary<string, Material> materials)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning(Tag + "prefab not found: " + prefabPath);
                return 0;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            int changes = 0;
            try
            {
                foreach (var pair in work)
                {
                    MeshInfo mi = pair.Key;
                    PrefabUse pu = pair.Value;
                    Transform node = string.IsNullOrEmpty(pu.node) ? root.transform : root.transform.Find(pu.node);
                    if (node == null)
                    {
                        Debug.LogWarning(Tag + prefabPath + ": node '" + pu.node + "' not found, skipped (mesh " + mi.mesh + ")");
                        continue;
                    }
                    if (!string.IsNullOrEmpty(pu.addChild))
                    {
                        Transform child = node.Find(pu.addChild);
                        if (child == null)
                        {
                            var go = new GameObject(pu.addChild);
                            go.layer = node.gameObject.layer;
                            child = go.transform;
                            child.SetParent(node, false);
                            Debug.Log(Tag + prefabPath + ": added visual child " + node.name + "/" + pu.addChild + " (no collider)");
                            changes++;
                        }
                        node = child;
                    }
                    changes += ApplyToNode(prefabPath, node, mi, pu, meshes[mi.mesh], materials);
                }
                if (changes > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    Debug.Log(Tag + "saved " + prefabPath + " (" + changes + " change(s))");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return changes;
        }

        private static int ApplyToNode(string prefabPath, Transform node, MeshInfo mi, PrefabUse pu, Mesh mesh,
                                       Dictionary<string, Material> materials)
        {
            int changes = 0;
            string where = prefabPath + " : " + (node.parent == null ? node.name : node.name);

            MeshFilter mf = node.GetComponent<MeshFilter>();
            if (mf == null) mf = node.gameObject.AddComponent<MeshFilter>();
            MeshRenderer mr = node.GetComponent<MeshRenderer>();
            if (mr == null) mr = node.gameObject.AddComponent<MeshRenderer>();

            if (mf.sharedMesh != mesh)
            {
                string old = mf.sharedMesh != null ? mf.sharedMesh.name : "none";
                mf.sharedMesh = mesh;
                Debug.Log(Tag + where + ": mesh " + old + " -> " + mesh.name + " (old mesh asset kept)");
                changes++;
            }

            // material slots: one per submesh
            int slots = Mathf.Max(1, mesh.subMeshCount);
            var wanted = new Material[slots];
            for (int i = 0; i < slots; i++)
            {
                string spec = pu.materials != null && pu.materials.Length > 0 ? pu.materials[Mathf.Min(i, pu.materials.Length - 1)] : null;
                wanted[i] = ResolveMaterial(spec, materials);
            }
            if (pu.materials != null && pu.materials.Length != slots)
                Debug.LogWarning(Tag + where + ": mesh has " + slots + " submesh(es) but the manifest lists " + pu.materials.Length + " material(s)");
            if (!SameMaterials(mr.sharedMaterials, wanted))
            {
                mr.sharedMaterials = wanted;
                Debug.Log(Tag + where + ": materials -> " + string.Join(", ", Array.ConvertAll(wanted, m => m != null ? m.name : "null")));
                changes++;
            }

            if (pu.localPosition != null && pu.localPosition.Length >= 3)
            {
                var p = new Vector3(pu.localPosition[0], pu.localPosition[1], pu.localPosition[2]);
                if ((node.localPosition - p).sqrMagnitude > 1e-10f) { node.localPosition = p; Debug.Log(Tag + where + ": localPosition -> " + p); changes++; }
            }
            if (pu.localScale != null && pu.localScale.Length >= 3)
            {
                var s = new Vector3(pu.localScale[0], pu.localScale[1], pu.localScale[2]);
                if ((node.localScale - s).sqrMagnitude > 1e-10f) { node.localScale = s; Debug.Log(Tag + where + ": localScale -> " + s); changes++; }
            }
            return changes;
        }

        private static Material ResolveMaterial(string spec, Dictionary<string, Material> created)
        {
            if (string.IsNullOrEmpty(spec)) return null;
            if (spec.StartsWith("@existing:", StringComparison.Ordinal))
            {
                string path = spec.Substring("@existing:".Length);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) Debug.LogWarning(Tag + "existing material not found: " + path);
                return m;
            }
            return created.TryGetValue(spec, out Material found) ? found : null;
        }

        private static bool SameMaterials(Material[] a, Material[] b)
        {
            if (a == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
