// Character And Animal PBR Pass 2. NOT TESTED IN UNITY (written without an editor session).
//
// Scope: people (MAT_Person_*), friends (F_*, PF_Friend_*) and animals (MAT_Animal_*, MAT_DogGrin_*).
// It never touches meshes, rigs, prefabs, _BaseMap or _BaseColor.
//
// What it changes:
//   1. _Smoothness and _Metallic per material class (cloth, skin, hair, leather, rubber, fur, eyes ...).
//   2. MAT_Animal_Atlas only: assigns T_Animal_Atlas_Mask_v2 to _MetallicGlossMap (R metallic, A smoothness)
//      because the animal UVs index the atlas palette, so a palette-aligned map is the one texture that works.
//   3. Texture import settings of the generated v2 maps (normal map type, linear mask, no compression for the mask).
//   4. Detail normals (_DetailNormalMap + _DETAIL_SCALED) only if EnableDetailNormals is true. It is false:
//      the people meshes (M_Person_*) have no UV0 and no tangents, the animal FBX UVs are a 4 px palette strip.
//      See Docs/Design/CHARACTER_PBR_PASS2.md.
//
// Idempotent: a value that already matches is logged as "ok" and not written again.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TramChanh.Editor
{
    public static class CharacterPbrApplier
    {
        const string MenuApply = "Tram Chanh/Art/Apply Character And Animal PBR Pass 2";
        const string MenuDry = "Tram Chanh/Art/Apply Character And Animal PBR Pass 2 (Dry Run)";
        const string Tag = "[CharacterPbrPass2] ";

        const string MatRoot = "Assets/TramChanh/Art/Materials";
        const string TexDir = "Assets/TramChanh/Art/Textures/Characters";
        const string AtlasPath = "Assets/TramChanh/Art/Textures/T_Animal_Atlas.png";
        const string AtlasMaskPath = TexDir + "/T_Animal_Atlas_Mask_v2.png";

        // Kept false on purpose, see header.
        static readonly bool EnableDetailNormals = false;

        static readonly string[] NormalMaps =
        {
            "T_Cloth_Weave_Normal_v2.png", "T_Skin_Micro_Normal_v2.png", "T_Fur_Micro_Normal_v2.png",
            "T_Hair_Strand_Normal_v2.png", "T_Leather_Grain_Normal_v2.png", "T_Rubber_Sole_Normal_v2.png",
        };

        sealed class Entry
        {
            public string Path, Name, Kind;
            public float Smoothness;
            public float Metallic;          // -1 = leave alone
            public string AtlasMask;        // non-null: assign this metallic-gloss map
        }

        [MenuItem(MenuApply)]
        static void ApplyMenu() { Run(false, true); }

        [MenuItem(MenuDry)]
        static void DryRunMenu() { Run(true, false); }

        /// <summary>Also callable from -executeMethod. Returns the number of values/textures changed.</summary>
        public static int Run(bool dryRun, bool confirm)
        {
            var entries = BuildPlan(out var skipped);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Tag + (dryRun ? "DRY RUN. " : "") + "materials in scope (" + entries.Count + "):");
            foreach (var e in entries)
                sb.AppendLine("  " + e.Name + "  [" + e.Kind + "]  smoothness " + Fmt(e.Smoothness)
                              + (e.Metallic >= 0 ? "  metallic " + Fmt(e.Metallic) : "")
                              + (e.AtlasMask != null ? "  + mask map " + System.IO.Path.GetFileName(e.AtlasMask) : ""));
            if (skipped.Count > 0)
                sb.AppendLine("  skipped (not people/friends/riders/animals or no rule): " + string.Join(", ", skipped));
            Debug.Log(sb.ToString());

            if (dryRun) return 0;

            if (confirm && !Application.isBatchMode)
            {
                if (!EditorUtility.DisplayDialog("Character And Animal PBR Pass 2",
                        "Apply smoothness / metallic / mask map to " + entries.Count + " materials?\n"
                        + "The full list is in the Console. _BaseMap and _BaseColor are never changed.",
                        "Apply", "Cancel"))
                    return 0;
            }

            int changes = 0;
            changes += ConfigureTextureImports();

            var touched = new List<Material>();
            foreach (var e in entries)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(e.Path);
                if (mat == null) { Debug.LogWarning(Tag + "cannot load " + e.Path); continue; }
                if (mat.shader == null || mat.shader.name != "Universal Render Pipeline/Lit")
                {
                    Debug.LogWarning(Tag + e.Name + ": shader is '" + (mat.shader != null ? mat.shader.name : "null")
                                     + "', expected Universal Render Pipeline/Lit. Skipped.");
                    continue;
                }

                int before = changes;
                // Colour safety net: remember, compare after, never write.
                var baseColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.clear;
                var baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;

                changes += SetFloat(mat, "_Smoothness", e.Smoothness, e.Kind);
                if (e.Metallic >= 0f) changes += SetFloat(mat, "_Metallic", e.Metallic, e.Kind);
                if (e.AtlasMask != null) changes += AssignMaskMap(mat, e.AtlasMask);

                if (mat.HasProperty("_BaseColor") && mat.GetColor("_BaseColor") != baseColor)
                    Debug.LogError(Tag + e.Name + ": _BaseColor changed unexpectedly!");
                if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != baseMap)
                    Debug.LogError(Tag + e.Name + ": _BaseMap changed unexpectedly!");

                if (changes != before) { EditorUtility.SetDirty(mat); touched.Add(mat); }
            }

            if (touched.Count > 0) AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(Tag + "done. " + changes + " change(s) on " + touched.Count + " material/texture asset(s). "
                      + (changes == 0 ? "Everything was already up to date." : ""));
            return changes;
        }

        // ------------------------------------------------------------------ plan

        static List<Entry> BuildPlan(out List<string> skipped)
        {
            skipped = new List<string>();
            var list = new List<Entry>();
            var folders = new List<string>();
            foreach (var f in new[] { MatRoot + "/People", MatRoot + "/Friends", MatRoot })
                if (AssetDatabase.IsValidFolder(f)) folders.Add(f);

            var seen = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", folders.ToArray()))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!seen.Add(path)) continue;
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!IsCharacterOrAnimalName(name)) continue;       // e.g. MAT_Car_*, MAT_Shopfront_* are ignored silently
                var e = Classify(name, path);
                if (e == null) { skipped.Add(name); continue; }
                list.Add(e);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return list;
        }

        static bool IsCharacterOrAnimalName(string n)
        {
            return n.StartsWith("MAT_Person_", StringComparison.Ordinal)
                || n.StartsWith("MAT_Animal_", StringComparison.Ordinal)
                || n.StartsWith("MAT_DogGrin_", StringComparison.Ordinal)
                || n.StartsWith("PF_Friend_", StringComparison.Ordinal)
                || (n.Length > 2 && n[0] == 'F' && n[1] == '_');
        }

        static Entry E(string path, string name, string kind, float s, float m = 0f, string mask = null)
        {
            return new Entry { Path = path, Name = name, Kind = kind, Smoothness = s, Metallic = m, AtlasMask = mask };
        }

        static readonly Regex FriendSlot =
            new Regex(@"^PF_Friend_\d\d_[A-Za-z]+_([0-5])_(Shirt|Skin|Pants|Shoes|Hair)", RegexOptions.CultureInvariant);

        static Entry Classify(string n, string p)
        {
            // ---- friends, per-prefab slot materials PF_Friend_NN_Name_K_Part_Colour
            var m = FriendSlot.Match(n);
            if (m.Success)
            {
                switch (m.Groups[1].Value)
                {
                    case "0": return E(p, n, "shirt cloth", 0.12f);
                    case "1": return E(p, n, "skin", 0.28f);
                    case "2": return E(p, n, "trousers cloth", 0.10f);
                    case "3": return E(p, n, "shoe upper", 0.32f);
                    case "4": return E(p, n, "rubber sole", 0.12f);
                    case "5": return E(p, n, "hair", 0.38f);
                }
            }

            switch (n)
            {
                // ---- people: skin
                case "MAT_Person_Skin": case "MAT_Person_Skin_Bronze": case "MAT_Person_Skin_Fair":
                case "MAT_Person_Skin_Golden": case "MAT_Person_Skin_Light": case "MAT_Person_Skin_Olive":
                case "MAT_Person_Skin_Tan":
                    return E(p, n, "skin", 0.28f);
                case "MAT_Person_Eyes_Dark": return E(p, n, "eye", 0.90f);
                // ---- people: hair
                case "MAT_Person_Hair_Black": case "MAT_Person_Hair_Caramel": case "MAT_Person_Hair_Chestnut":
                case "MAT_Person_Hair_DarkBrown": case "MAT_Person_Hair_DarkIndigo":
                    return E(p, n, "hair", 0.38f);
                // ---- people: cloth
                case "MAT_Person_Clothing_A": case "MAT_Person_Clothing_B": case "MAT_Person_Hair_Accessories":
                case "MAT_Person_Shirt_Coral": case "MAT_Person_Top_Yellow":
                    return E(p, n, "cotton cloth", 0.12f);
                case "MAT_Person_Shirt_White": return E(p, n, "cotton cloth", 0.14f);
                case "MAT_Person_Hoodie_Teal": return E(p, n, "fleece cloth", 0.08f);
                case "MAT_Person_Hat_Orange": return E(p, n, "cap fabric", 0.15f);
                case "MAT_Person_Vest_Yellow": return E(p, n, "hi-vis polyester", 0.30f);
                case "MAT_Person_Pants_Charcoal": case "MAT_Person_Pants_Navy": case "MAT_Person_Pants_Olive":
                    return E(p, n, "twill / denim", 0.10f);
                // ---- people: shoes
                case "MAT_Person_Shoes_Black": case "MAT_Person_Shoes_Brown": return E(p, n, "leather", 0.38f);
                case "MAT_Person_Shoes_Tan": return E(p, n, "leather", 0.35f);
                case "MAT_Person_Shoes_White": return E(p, n, "synthetic upper", 0.30f);
                case "MAT_Person_Shoes_Soles": return E(p, n, "rubber sole", 0.12f);

                // ---- friends, shared F_* materials
                case "F_Black": return E(p, n, "cotton cloth", 0.12f);
                case "F_White": return E(p, n, "cotton cloth", 0.14f);
                case "F_Cream": return E(p, n, "knit cloth", 0.10f);
                case "F_Gray": return E(p, n, "sweat cloth", 0.10f);
                case "F_Fleece": case "F_SweatBeige": return E(p, n, "fleece cloth", 0.08f);
                case "F_CamoCap": return E(p, n, "cap fabric", 0.12f);
                case "F_RedVarsity": return E(p, n, "wool/nylon jacket", 0.18f);
                case "F_HairBlack": case "F_HairBrown": case "F_HairCopper": return E(p, n, "hair", 0.38f);
                case "F_HairBlondTip": return E(p, n, "hair", 0.40f);
                case "F_Mouth": return E(p, n, "lips", 0.40f);
                case "F_Beads": return E(p, n, "plastic beads (unchanged)", 0.60f);
                case "F_GlassFrame": return E(p, n, "acetate frame (unchanged)", 0.80f);
                case "F_Gold": return E(p, n, "metal (unchanged)", 0.70f, 0.90f);

                // ---- animals
                case "MAT_Animal_Atlas": return E(p, n, "atlas fur/feather + palette mask", 1.00f, 0f, AtlasMaskPath);
                case "MAT_Animal_Cat_Main": return E(p, n, "fur", 0.20f);
                case "MAT_Animal_Cat_Secondary": return E(p, n, "fur", 0.20f);
                case "MAT_Animal_Cat_Ears": return E(p, n, "inner ear skin", 0.30f);
                case "MAT_Animal_Dog": return E(p, n, "fur", 0.20f);
                case "MAT_Animal_Pigeon_Main": case "MAT_Animal_Pigeon_Secondary": return E(p, n, "feather", 0.30f);
                case "MAT_Animal_Chicken_Main": return E(p, n, "feather", 0.30f);
                case "MAT_Animal_Chicken_Red": return E(p, n, "comb / wattle", 0.45f);
                case "MAT_Animal_Chicken_Beak": return E(p, n, "keratin", 0.45f);
                case "MAT_Animal_Eye_Black": return E(p, n, "eye", 0.90f);
                case "MAT_Animal_Eye_White": return E(p, n, "eye white", 0.60f);
                case "MAT_DogGrin_Fur": return E(p, n, "fur", 0.12f);
                case "MAT_DogGrin_Hair": return E(p, n, "fur / hair", 0.20f);
                case "MAT_DogGrin_Dark": return E(p, n, "nose / eye", 0.70f);
                case "MAT_DogGrin_Tee": return E(p, n, "cotton cloth", 0.12f);
                case "MAT_DogGrin_Teeth": return E(p, n, "enamel (unchanged)", 0.50f);
                case "MAT_DogGrin_Silver": return E(p, n, "metal (unchanged)", 0.70f, 0.90f);
            }
            return null;
        }

        // ------------------------------------------------------------------ writers

        static string Fmt(float v) { return v.ToString("0.###", CultureInfo.InvariantCulture); }

        static int SetFloat(Material mat, string prop, float value, string why)
        {
            if (!mat.HasProperty(prop)) { Debug.LogWarning(Tag + mat.name + ": no property " + prop); return 0; }
            float old = mat.GetFloat(prop);
            if (Mathf.Abs(old - value) < 0.0005f)
            {
                Debug.Log(Tag + mat.name + ": " + prop + " ok (" + Fmt(old) + ")");
                return 0;
            }
            mat.SetFloat(prop, value);
            Debug.Log(Tag + mat.name + ": " + prop + " " + Fmt(old) + " -> " + Fmt(value) + "  (" + why + ")");
            return 1;
        }

        static int AssignMaskMap(Material mat, string maskPath)
        {
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            if (mask == null)
            {
                Debug.LogWarning(Tag + mat.name + ": mask map missing at " + maskPath
                                 + " (run ArtSource/Scripts/generate_character_pbr_v2.py). Scalar smoothness will be wrong; fix before committing.");
                return 0;
            }
            int n = 0;
            if (mat.GetTexture("_MetallicGlossMap") != mask)
            {
                mat.SetTexture("_MetallicGlossMap", mask);
                Debug.Log(Tag + mat.name + ": _MetallicGlossMap -> " + mask.name);
                n++;
            }
            if (!mat.IsKeywordEnabled("_METALLICSPECGLOSSMAP"))
            {
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                Debug.Log(Tag + mat.name + ": keyword _METALLICSPECGLOSSMAP enabled");
                n++;
            }
            n += SetFloat(mat, "_SmoothnessTextureChannel", 0f, "smoothness from metallic map alpha");
            return n;
        }

        // Only reachable when EnableDetailNormals is true (it is not). Detail tiling in URP Lit is read from
        // _DetailAlbedoMap_ST for both detail maps, and _DETAIL_SCALED with _DetailAlbedoMapScale 0 leaves albedo untouched.
        static int ApplyDetailNormal(Material mat, string normalFile, Vector2 tiling, float scale)
        {
            if (!EnableDetailNormals) return 0;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + normalFile);
            if (tex == null) { Debug.LogWarning(Tag + mat.name + ": missing " + normalFile); return 0; }
            int n = 0;
            if (mat.GetTexture("_DetailNormalMap") != tex) { mat.SetTexture("_DetailNormalMap", tex); n++; }
            if (mat.GetTextureScale("_DetailAlbedoMap") != tiling) { mat.SetTextureScale("_DetailAlbedoMap", tiling); n++; }
            n += SetFloat(mat, "_DetailNormalMapScale", scale, "detail normal strength");
            n += SetFloat(mat, "_DetailAlbedoMapScale", 0f, "no detail albedo, colours stay as approved");
            if (!mat.IsKeywordEnabled("_DETAIL_SCALED")) { mat.EnableKeyword("_DETAIL_SCALED"); n++; }
            return n;
        }

        // ------------------------------------------------------------------ texture imports

        static int ConfigureTextureImports()
        {
            int changes = 0;
            foreach (var f in NormalMaps)
            {
                var ti = AssetImporter.GetAtPath(TexDir + "/" + f) as TextureImporter;
                if (ti == null) { Debug.Log(Tag + f + ": not imported yet / missing, skipped"); continue; }
                bool dirty = false;
                if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; dirty = true; }
                if (ti.filterMode != FilterMode.Bilinear && ti.filterMode != FilterMode.Trilinear) { ti.filterMode = FilterMode.Trilinear; dirty = true; }
                changes += Save(ti, f, dirty, "normal map, repeat, mipmaps");
            }

            var mask = AssetImporter.GetAtPath(AtlasMaskPath) as TextureImporter;
            if (mask == null) { Debug.Log(Tag + "T_Animal_Atlas_Mask_v2.png: not imported yet / missing, skipped"); return changes; }
            var atlas = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            bool d = false;
            if (mask.textureType != TextureImporterType.Default) { mask.textureType = TextureImporterType.Default; d = true; }
            if (mask.sRGBTexture) { mask.sRGBTexture = false; d = true; }
            if (mask.alphaSource != TextureImporterAlphaSource.FromInput) { mask.alphaSource = TextureImporterAlphaSource.FromInput; d = true; }
            if (mask.wrapMode != TextureWrapMode.Clamp) { mask.wrapMode = TextureWrapMode.Clamp; d = true; }
            if (mask.textureCompression != TextureImporterCompression.Uncompressed) { mask.textureCompression = TextureImporterCompression.Uncompressed; d = true; }
            // Same sampling as the atlas it is aligned with, so palette cells never blend.
            var wantFilter = atlas != null ? atlas.filterMode : FilterMode.Point;
            var wantMips = atlas != null && atlas.mipmapEnabled;
            if (mask.filterMode != wantFilter) { mask.filterMode = wantFilter; d = true; }
            if (mask.mipmapEnabled != wantMips) { mask.mipmapEnabled = wantMips; d = true; }
            changes += Save(mask, "T_Animal_Atlas_Mask_v2.png", d, "linear mask, uncompressed, atlas filtering");
            return changes;
        }

        static int Save(TextureImporter ti, string file, bool dirty, string what)
        {
            if (!dirty) { Debug.Log(Tag + file + ": import settings ok"); return 0; }
            ti.SaveAndReimport();
            Debug.Log(Tag + file + ": import settings set (" + what + ")");
            return 1;
        }
    }
}
