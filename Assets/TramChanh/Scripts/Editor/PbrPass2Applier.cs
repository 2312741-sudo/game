using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TramChanh.Editor
{
    /// <summary>
    /// Assigns the generated PBR pass 2 maps (ArtSource/Scripts/generate_pbr_pass2.py) to the
    /// listed URP Lit materials. Idempotent: running it twice changes nothing the second time.
    /// Only the materials in the table below are touched.
    /// Mask map layout: R metallic, G occlusion, B detail mask, A smoothness.
    /// URP Lit multiplies the mask alpha by _Smoothness, so mask-driven materials use _Smoothness = 1
    /// and the target smoothness is baked into the mask. Metallic comes straight from R.
    /// Occlusion is read by URP Lit from _OcclusionMap (G), so the same mask is assigned there.
    /// </summary>
    public static class PbrPass2Applier
    {
        const string Tex = "Assets/TramChanh/Art/Textures/";
        const string Mat = "Assets/TramChanh/Art/Materials/";

        struct Entry
        {
            public string Material;
            public string Surface;     // texture name stem, e.g. "Asphalt"
            public bool UseMask;       // false = normal map only, existing scalar values kept
            public float BumpScale;
            public float Occlusion;    // _OcclusionStrength when mask used
        }

        static Entry E(string mat, string surface, bool mask, float bump, float occ = 0.8f)
        {
            return new Entry { Material = mat, Surface = surface, UseMask = mask, BumpScale = bump, Occlusion = occ };
        }

        static readonly Entry[] Table =
        {
            // Roads. The night/rain variants keep their own puddle smoothness (existing mask or >= 0.88 scalar).
            E("MAT_Street_WetAsphalt",      "Asphalt",    true,  0.9f),
            E("MAT_Street_WetAsphaltNight", "Asphalt",    false, 0.9f),
            E("MAT_Road_WetRain",           "Asphalt",    false, 0.9f),
            // Sidewalks
            E("MAT_Street_SidewalkTiles",   "Sidewalk",   true,  1.0f),
            E("MAT_Street_Sidewalk",        "Sidewalk",   true,  1.0f),
            E("MAT_Sidewalk_WetRain",       "Sidewalk",   false, 1.0f),
            // Stall
            E("MAT_Stall_WoodCounter",      "Wood",       true,  0.8f),
            E("MAT_Stall_CorrugatedMetal",  "Corrugated", true,  1.0f, 1.0f),
            // Plastics
            E("MAT_Crate_YellowHDPE",       "Plastic",    true,  0.6f),
            E("MAT_PlasticCrate_Street",    "Plastic",    true,  0.6f),
            E("MAT_Plastic_BeigeStool",     "Plastic",    true,  0.6f),
            // Cloth
            E("MAT_Sidewalk_Umbrella",      "Cloth",      true,  0.8f),
            E("MAT_Shopfront_Canopy",       "Cloth",      true,  0.8f),
            // Facade plaster (normal only: the facade atlases also hold glass, keep their smoothness)
            E("MAT_Shopfront_A", "Plaster", false, 0.5f),
            E("MAT_Shopfront_B", "Plaster", false, 0.5f),
            E("MAT_Shopfront_C", "Plaster", false, 0.5f),
            E("MAT_Shopfront_D", "Plaster", false, 0.5f),
            E("MAT_Shopfront_E", "Plaster", false, 0.5f),
            E("MAT_Shopfront_F", "Plaster", false, 0.5f),
            E("MAT_Building_Tower_A", "Plaster", false, 0.5f),
            E("MAT_Building_Tower_B", "Plaster", false, 0.5f),
            E("MAT_Building_Tower_C", "Plaster", false, 0.5f),
        };

        [MenuItem("Tram Chanh/Art/Apply PBR Pass 2")]
        public static void Apply()
        {
            int changedMaterials = 0;
            var configured = new HashSet<string>();

            foreach (Entry e in Table)
            {
                string normalPath = Tex + "T_" + e.Surface + "_Normal_v2.png";
                string maskPath = Tex + "T_" + e.Surface + "_Mask_v2.png";

                if (configured.Add(normalPath)) ConfigureImporter(normalPath, true);
                if (e.UseMask && configured.Add(maskPath)) ConfigureImporter(maskPath, false);

                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                Texture2D mask = e.UseMask ? AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath) : null;
                var mat = AssetDatabase.LoadAssetAtPath<Material>(Mat + e.Material + ".mat");

                if (mat == null) { Debug.LogWarning("[PbrPass2] material not found, skipped: " + e.Material); continue; }
                if (normal == null) { Debug.LogWarning("[PbrPass2] missing " + normalPath + ", skipped " + e.Material + " (run generate_pbr_pass2.py)"); continue; }
                if (e.UseMask && mask == null) { Debug.LogWarning("[PbrPass2] missing " + maskPath + ", skipped " + e.Material); continue; }

                var log = new List<string>();
                SetTex(mat, "_BumpMap", normal, log);
                SetFloat(mat, "_BumpScale", e.BumpScale, log);
                SetKeyword(mat, "_NORMALMAP", true, log);

                if (e.UseMask)
                {
                    SetTex(mat, "_MetallicGlossMap", mask, log);
                    SetKeyword(mat, "_METALLICSPECGLOSSMAP", true, log);
                    SetTex(mat, "_OcclusionMap", mask, log);
                    SetFloat(mat, "_OcclusionStrength", e.Occlusion, log);
                    SetKeyword(mat, "_OCCLUSIONMAP", true, log);
                    // Smoothness = mask.a * _Smoothness. Mask alpha already holds the intended value
                    // (plastic <= 0.57, dry road ~0.4-0.6, puddles >= 0.93). Metallic comes from mask.r.
                    SetFloat(mat, "_Smoothness", 1f, log);
                    SetFloat(mat, "_SmoothnessTextureChannel", 0f, log); // mask alpha, not albedo alpha
                    SetFloat(mat, "_Metallic", 0f, log);                 // unused with a mask; keep it tidy
                }

                if (log.Count > 0)
                {
                    EditorUtility.SetDirty(mat);
                    changedMaterials++;
                    Debug.Log("[PbrPass2] " + e.Material + " <- " + e.Surface + ": " + string.Join(", ", log));
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[PbrPass2] done. Materials changed: " + changedMaterials + " of " + Table.Length
                      + (changedMaterials == 0 ? " (already applied)" : ""));
        }

        static void ConfigureImporter(string path, bool isNormal)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool dirty = false;
            if (isNormal)
            {
                if (imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; dirty = true; }
            }
            else
            {
                if (imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; dirty = true; }
                if (imp.sRGBTexture) { imp.sRGBTexture = false; dirty = true; }
            }
            if (!imp.mipmapEnabled) { imp.mipmapEnabled = true; dirty = true; }
            if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (imp.maxTextureSize < 2048) { imp.maxTextureSize = 2048; dirty = true; }
            if (dirty)
            {
                imp.SaveAndReimport();
                Debug.Log("[PbrPass2] importer set: " + path + (isNormal ? " (Normal map)" : " (sRGB off)"));
            }
        }

        static void SetTex(Material m, string prop, Texture2D t, List<string> log)
        {
            if (!m.HasProperty(prop)) return;
            if (m.GetTexture(prop) == t) return;
            m.SetTexture(prop, t);
            log.Add(prop + "=" + t.name);
        }

        static void SetFloat(Material m, string prop, float v, List<string> log)
        {
            if (!m.HasProperty(prop)) return;
            if (Mathf.Abs(m.GetFloat(prop) - v) < 1e-4f) return;
            m.SetFloat(prop, v);
            log.Add(prop + "=" + v);
        }

        static void SetKeyword(Material m, string kw, bool on, List<string> log)
        {
            if (m.IsKeywordEnabled(kw) == on) return;
            if (on) m.EnableKeyword(kw); else m.DisableKeyword(kw);
            log.Add(kw + (on ? " on" : " off"));
        }
    }
}
