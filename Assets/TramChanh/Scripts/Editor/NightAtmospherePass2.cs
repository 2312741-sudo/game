using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace TramChanh.Editor
{
    /// <summary>
    /// Night atmosphere pass 2 (see Docs/Design/NIGHT_ATMOSPHERE_PASS2.md for every number and its reason).
    /// Menu: "Tram Chanh/Art/Apply Night Atmosphere Pass 2" and "Tram Chanh/Art/Remove Night Atmosphere Pass 2".
    ///
    /// What it does (all logged, idempotent):
    ///  1. Tunes the existing lights inside PF_TramChanh_StreetExtended (street lamps, WarmStallBulb, Streetlight_Center,
    ///     the 4 Edison bulbs of the stall): colour temperature 2700-3500 K, range, intensity, shadows off except the stall key light.
    ///     No light is added. The two inner Edison bulbs stop being real-time lights (their emissive filament stays).
    ///  2. Turns the emission of MAT_Stall_Bulb_Filament and MAT_Stall_Bulb_Glass on (they had no _EMISSION keyword).
    ///  3. Fog (exponential squared), ambient trilight and the night skybox material.
    ///  4. Root "NightAtmosphere" in the open scene: fake glow (additive, one shared material, instancing on, culled by LOD distance)
    ///     and two extra baked (Custom) reflection probes for the road; RP_Stall is converted from realtime to baked too.
    ///  5. New profile VP_TramChanh_Night_v2.asset (cloned from VP_TramChanh_Night.asset, which is not touched) and the scene Volume switched to it.
    ///
    /// State and file backups live in Library/TramChanh/NightAtmospherePass2 (not tracked by git). Remove reverts from there.
    /// STATUS: NOT COMPILED AND NOT RUN. Written without Unity. Treat the first run as a test and review the Console log.
    /// </summary>
    public static class NightAtmospherePass2
    {
        // ---------------------------------------------------------------- menu and names
        private const string ApplyMenu = "Tram Chanh/Art/Apply Night Atmosphere Pass 2";
        private const string RemoveMenu = "Tram Chanh/Art/Remove Night Atmosphere Pass 2";
        private const string Tag = "[NightAtmospherePass2] ";
        private const string RootName = "NightAtmosphere";
        private const string MainSceneFile = "SCN_TramChanh_Main.unity";

        // ---------------------------------------------------------------- existing assets (verified to exist in the repo)
        private const string StreetPrefabPath = "Assets/TramChanh/Prefabs/Environment/PF_TramChanh_StreetExtended.prefab";
        private const string FilamentMatPath = "Assets/TramChanh/Art/Materials/MAT_Stall_Bulb_Filament.mat";
        private const string GlassMatPath = "Assets/TramChanh/Art/Materials/MAT_Stall_Bulb_Glass.mat";
        private const string SkyboxMatPath = "Assets/TramChanh/Art/Materials/MAT_Skybox_Night.mat";
        private const string OldProfilePath = "Assets/TramChanh/Art/PostProcessing/VP_TramChanh_Night.asset";
        private const string MoonlightName = "Moonlight";
        private const string StallProbeName = "RP_Stall";
        private const string VolumeObjectName = "PostProcessVolume";

        // ---------------------------------------------------------------- new assets created by this tool
        private const string NewProfilePath = "Assets/TramChanh/Art/PostProcessing/VP_TramChanh_Night_v2.asset";
        private const string GeneratedFolder = "Assets/TramChanh/Art/Lighting/NightAtmosphere";
        private const string GlowMaterialPath = GeneratedFolder + "/MAT_NightGlow_Additive.mat";

        // ---------------------------------------------------------------- numbers (see the doc)
        // Light names inside the street prefab
        private const string LampLightName = "LuminaireLight";
        private const string StallKeyLightName = "WarmStallBulb";
        private const string CenterLightName = "Streetlight_Center";
        private const string EdisonLightName = "PointLight";
        private static readonly float[] LampKelvinPattern = { 2700f, 3000f, 2700f, 3500f };
        private const float LampRange = 13f;
        private const float LampInnerSpotAngle = 45f;
        private const float StallKeyKelvin = 2900f, StallKeyIntensity = 4.0f, StallKeyRange = 6.5f;
        private const float CenterKelvin = 3200f, CenterIntensity = 2.8f, CenterRange = 18f;
        private const float EdisonKelvin = 2700f, EdisonIntensity = 4.0f, EdisonRange = 4.0f;
        /// <summary>true: Edison bulbs _1 and _2 stop being real-time lights (emissive only). Lowers the lights at x = 0 from 8 to 6.</summary>
        private const bool DisableInnerEdisonLights = true;
        /// <summary>Moonlight shadows are the only directional shadow pass. Off to save shadow draw calls. Set to Soft to keep them.</summary>
        private const LightShadows MoonShadows = LightShadows.None;

        // Emission (HDR multipliers on a colour-temperature colour)
        private const float FilamentKelvin = 2700f, FilamentEmission = 6.0f;
        private const float GlassKelvin = 2700f, GlassEmission = 0.6f;

        // Fog and ambient
        private static readonly Color FogColor = new Color(0.055f, 0.070f, 0.100f, 1f);
        private const float FogDensity = 0.0055f;
        private static readonly Color AmbientSky = new Color(0.19f, 0.24f, 0.36f, 1f);
        private static readonly Color AmbientEquator = new Color(0.13f, 0.12f, 0.12f, 1f);
        private static readonly Color AmbientGround = new Color(0.05f, 0.045f, 0.04f, 1f);
        // Skybox material (procedural)
        private const float SkyAtmosphereThickness = 0.6f, SkyExposure = 0.7f;
        private static readonly Color SkyTint = new Color(0.10f, 0.14f, 0.24f, 1f);
        private static readonly Color SkyGround = new Color(0.035f, 0.045f, 0.07f, 1f);

        // Fake glow
        private const float GlowCullScreenHeight = 0.015f;   // culled below 1.5 % of screen height, about 125 m at 60 deg vertical fov
        private static readonly float GlowBaseBrightness = 1.5f;

        // Reflection probes
        private const int ProbeResolution = 128;
        private static readonly Vector3 StreetProbeSize = new Vector3(142f, 12f, 40f);
        private const float StreetProbeX = 91f, StreetProbeY = 3f, StreetProbeZ = 6f;
        private const float ProbeBoxFloorY = -0.15f;        // road top; box projection floor must sit on the road
        private const float ProbeFarClip = 220f;
        private const float StreetProbeBlend = 5f;

        // ---------------------------------------------------------------- state (JSON in Library)
        [Serializable]
        private class ProbeState
        {
            public bool valid;
            public int mode, refreshMode, timeSlicing, resolution, importance;
            public Vector3 size, center;
            public float blend, farClip;
            public bool boxProjection, hdr, renderDynamic;
            public string customTextureGuid;
            public Vector3 localPosition;
        }

        [Serializable]
        private class State
        {
            public bool applied;
            public List<string> backedUp = new List<string>();
            public string volumeOldProfileGuid;
            public bool fogRecorded;
            public bool fog; public int fogMode; public float fogDensity; public Color fogColor;
            public int ambientMode; public Color ambientSky, ambientEquator, ambientGround;
            public bool moonRecorded; public int moonShadows;
            public ProbeState stallProbe = new ProbeState();
        }

        private struct GlowSpot { public Vector3 Position; public float Kelvin; }

        private static string ProjectRoot { get { return Directory.GetParent(Application.dataPath).FullName; } }
        private static string StateDir { get { return Path.Combine(ProjectRoot, "Library/TramChanh/NightAtmospherePass2"); } }
        private static string StatePath { get { return Path.Combine(StateDir, "state.json"); } }
        private static string BackupDir { get { return Path.Combine(StateDir, "Backup"); } }

        private static readonly StringBuilder Report = new StringBuilder();

        // ================================================================ APPLY
        [MenuItem(ApplyMenu)]
        public static void Apply()
        {
            if (!Preconditions()) { return; }
            Report.Length = 0;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Night Atmosphere Pass 2");

            State state = LoadState();
            Directory.CreateDirectory(BackupDir);
            Log("Apply start (re-running is safe: every value is set, nothing is added twice).");

            try
            {
                List<GlowSpot> glowSpots = TuneStreetPrefab(state);
                TuneBulbMaterials(state);
                TuneSkyboxMaterial(state);
                TuneFogAndAmbient(state);
                TuneMoonlight(state);
                ClearOldRootAndAssets();
                EnsureFolder(GeneratedFolder);
                GameObject root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Create NightAtmosphere");
                Log("Created root GameObject '" + RootName + "' in the active scene (remove it with the Remove menu).");
                BuildFakeGlow(root.transform, glowSpots);
                BuildReflectionProbes(root.transform, state);
                BuildAndAssignProfile(state);

                state.applied = true;
            }
            finally
            {
                SaveState(state);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(SceneManagerActiveScene());
            Undo.CollapseUndoOperations(undoGroup);
            Log("Apply done. The scene is marked dirty: review the log, take screenshots, then save the scene (Ctrl+S). "
                + "Scene changes are undoable with Ctrl+Z; prefab, material and generated asset changes are not (use the Remove menu).");
        }

        // ================================================================ REMOVE
        [MenuItem(RemoveMenu)]
        public static void Remove()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError(Tag + "Leave Play Mode first."); return; }
            Report.Length = 0;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remove Night Atmosphere Pass 2");
            State state = LoadState();
            if (!state.applied) { Log("No applied state found in Library; removing leftovers (root, generated folder, v2 profile) only."); }

            // 1. Volume back to the old profile before the v2 asset is deleted.
            Volume volume = FindVolume();
            if (volume != null && !string.IsNullOrEmpty(state.volumeOldProfileGuid))
            {
                VolumeProfile old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AssetDatabase.GUIDToAssetPath(state.volumeOldProfileGuid));
                if (old == null) { old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(OldProfilePath); }
                if (old != null)
                {
                    Undo.RecordObject(volume, "Restore Volume profile");
                    volume.sharedProfile = old;
                    EditorUtility.SetDirty(volume);
                    Log("Volume '" + volume.name + "' sharedProfile restored to " + AssetDatabase.GetAssetPath(old));
                }
            }
            // 2. Root and generated assets.
            ClearOldRootAndAssets();
            if (AssetDatabase.LoadAssetAtPath<Object>(NewProfilePath) != null)
            {
                AssetDatabase.DeleteAsset(NewProfilePath);
                Log("Deleted " + NewProfilePath);
            }
            // 3. Restore backed up files (prefab, materials).
            foreach (string assetPath in state.backedUp)
            {
                string backup = BackupFile(assetPath);
                if (!File.Exists(backup)) { Debug.LogWarning(Tag + "No backup for " + assetPath); continue; }
                File.Copy(backup, Path.Combine(ProjectRoot, assetPath), true);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                Log("Restored file " + assetPath + " from backup.");
            }
            // 4. Scene values.
            if (state.fogRecorded || state.applied) { RestoreSceneValues(state); }
            // 5. Forget.
            if (Directory.Exists(StateDir)) { Directory.Delete(StateDir, true); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(SceneManagerActiveScene());
            Undo.CollapseUndoOperations(undoGroup);
            Log("Remove done. Save the scene to keep the reverted values.");
        }

        // ================================================================ 1. lights in the street prefab
        private static List<GlowSpot> TuneStreetPrefab(State state)
        {
            var spots = new List<GlowSpot>();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(StreetPrefabPath) == null)
            {
                Debug.LogError(Tag + "Street prefab missing: " + StreetPrefabPath + " (lights not tuned).");
                return spots;
            }
            Backup(state, StreetPrefabPath);
            GameObject contents = PrefabUtility.LoadPrefabContents(StreetPrefabPath);
            try
            {
                Log("Street prefab root local position " + contents.transform.localPosition + " (glow positions assume the runtime parent sits at the origin).");
                Light[] lights = contents.GetComponentsInChildren<Light>(true);
                var lamps = new List<Light>();
                foreach (Light l in lights) { if (l.gameObject.name == LampLightName) { lamps.Add(l); } }
                // Stable order: along the street, then across it.
                lamps.Sort((a, b) =>
                {
                    int c = a.transform.position.x.CompareTo(b.transform.position.x);
                    return c != 0 ? c : a.transform.position.z.CompareTo(b.transform.position.z);
                });
                for (int i = 0; i < lamps.Count; i++)
                {
                    float k = LampKelvinPattern[i % LampKelvinPattern.Length];
                    float intensity = k <= 2700f ? 4.0f : (k <= 3000f ? 3.8f : 3.6f);
                    SetLight(lamps[i], "Lamp " + i.ToString("00"), k, intensity, LampRange, LightShadows.None, true);
                    float before = lamps[i].innerSpotAngle;
                    if (lamps[i].type == LightType.Spot)
                    {
                        lamps[i].innerSpotAngle = Mathf.Min(LampInnerSpotAngle, lamps[i].spotAngle - 1f);
                        Log("  Lamp " + i.ToString("00") + " innerSpotAngle " + before.ToString("0.#") + " -> " + lamps[i].innerSpotAngle.ToString("0.#"));
                    }
                    spots.Add(new GlowSpot { Position = lamps[i].transform.position, Kelvin = k });
                }
                Log("Street lamps found: " + lamps.Count + " (expected 21 instances of PF_StreetLamp).");

                foreach (Light l in lights)
                {
                    string n = l.gameObject.name;
                    if (n == StallKeyLightName)
                    {
                        // The one light that keeps a shadow (stall key light).
                        SetLight(l, n + " (stall key light)", StallKeyKelvin, StallKeyIntensity, StallKeyRange, LightShadows.Soft, true);
                    }
                    else if (n == CenterLightName)
                    {
                        SetLight(l, n, CenterKelvin, CenterIntensity, CenterRange, LightShadows.None, true);
                    }
                    else if (n == EdisonLightName && l.transform.parent != null && l.transform.parent.name.StartsWith("PF_EdisonBulb_"))
                    {
                        string bulb = l.transform.parent.name;
                        bool inner = bulb.EndsWith("_1") || bulb.EndsWith("_2");
                        bool keep = !(DisableInnerEdisonLights && inner);
                        SetLight(l, bulb + "/PointLight", EdisonKelvin, EdisonIntensity, EdisonRange, LightShadows.None, keep);
                    }
                    else if (n != LampLightName)
                    {
                        Log("Light left untouched: '" + n + "' type " + l.type + " intensity " + l.intensity + " shadows " + l.shadows);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(contents, StreetPrefabPath);
                Log("Saved " + StreetPrefabPath + " (instance overrides on nested lamp and bulb prefabs). If ExtendedStreetBuilder or "
                    + "StallHighFidelityBuilder is run again these values are lost: apply this tool again after a rebuild.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            return spots;
        }

        private static void SetLight(Light l, string label, float kelvin, float intensity, float range, LightShadows shadows, bool enabled)
        {
            Color color = KelvinToColor(kelvin);
            Log("Light " + label + ": " + kelvin.ToString("0") + " K, color " + Fmt(l.color) + "->" + Fmt(color)
                + ", intensity " + l.intensity.ToString("0.##") + "->" + intensity.ToString("0.##")
                + ", range " + l.range.ToString("0.##") + "->" + range.ToString("0.##")
                + ", shadows " + l.shadows + "->" + shadows
                + ", enabled " + l.enabled + "->" + enabled);
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows;
            l.bounceIntensity = 0f;
            l.enabled = enabled;
            EditorUtility.SetDirty(l);
        }

        // ================================================================ 2. bulb materials
        private static void TuneBulbMaterials(State state)
        {
            ApplyEmission(state, FilamentMatPath, FilamentKelvin, FilamentEmission);
            ApplyEmission(state, GlassMatPath, GlassKelvin, GlassEmission);
            Log("MAT_Street_StreetLamp and MAT_Accel_WarmBulb are NOT touched: the street lamp is one mesh (pole + head) so emission would light the pole.");
        }

        private static void ApplyEmission(State state, string path, float kelvin, float multiplier)
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { Debug.LogError(Tag + "Material missing: " + path); return; }
            Backup(state, path);
            Color c = KelvinToColor(kelvin) * multiplier;
            c.a = 1f;
            Log("Material " + path + ": _EMISSION keyword " + m.IsKeywordEnabled("_EMISSION") + "->true, _EmissionColor "
                + Fmt(m.GetColor("_EmissionColor")) + "->" + Fmt(c) + " (" + kelvin.ToString("0") + " K x " + multiplier.ToString("0.##") + ")");
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
        }

        // ================================================================ 3. skybox, fog, ambient, moonlight
        private static void TuneSkyboxMaterial(State state)
        {
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMatPath);
            if (sky == null) { Debug.LogError(Tag + "Skybox material missing: " + SkyboxMatPath); return; }
            Backup(state, SkyboxMatPath);
            Log("Skybox " + SkyboxMatPath + ": _AtmosphereThickness " + GetF(sky, "_AtmosphereThickness") + "->" + SkyAtmosphereThickness
                + ", _Exposure " + GetF(sky, "_Exposure") + "->" + SkyExposure
                + ", _SkyTint " + Fmt(sky.GetColor("_SkyTint")) + "->" + Fmt(SkyTint)
                + ", _GroundColor " + Fmt(sky.GetColor("_GroundColor")) + "->" + Fmt(SkyGround));
            sky.SetFloat("_AtmosphereThickness", SkyAtmosphereThickness);
            sky.SetFloat("_Exposure", SkyExposure);
            sky.SetColor("_SkyTint", SkyTint);
            sky.SetColor("_GroundColor", SkyGround);
            EditorUtility.SetDirty(sky);
        }

        private static void TuneFogAndAmbient(State state)
        {
            if (!state.fogRecorded)
            {
                state.fogRecorded = true;
                state.fog = RenderSettings.fog; state.fogMode = (int)RenderSettings.fogMode;
                state.fogDensity = RenderSettings.fogDensity; state.fogColor = RenderSettings.fogColor;
                state.ambientMode = (int)RenderSettings.ambientMode;
                state.ambientSky = RenderSettings.ambientSkyColor;
                state.ambientEquator = RenderSettings.ambientEquatorColor;
                state.ambientGround = RenderSettings.ambientGroundColor;
            }
            Object rs = GetRenderSettingsObject();
            if (rs != null) { Undo.RecordObject(rs, "Night atmosphere fog and ambient"); }
            Log("Fog: " + RenderSettings.fog + "/" + RenderSettings.fogMode + "/" + RenderSettings.fogDensity.ToString("0.####") + "/" + Fmt(RenderSettings.fogColor)
                + " -> true/ExponentialSquared/" + FogDensity.ToString("0.####") + "/" + Fmt(FogColor));
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = FogDensity;
            RenderSettings.fogColor = FogColor;
            Log("Ambient: " + RenderSettings.ambientMode + " sky " + Fmt(RenderSettings.ambientSkyColor) + " -> Trilight sky " + Fmt(AmbientSky)
                + " equator " + Fmt(AmbientEquator) + " ground " + Fmt(AmbientGround)
                + " (ambientIntensity " + RenderSettings.ambientIntensity + " is left alone: the bootstrap scales it at runtime)");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            if (rs != null) { EditorUtility.SetDirty(rs); }
        }

        private static void TuneMoonlight(State state)
        {
            Light moon = FindSceneLight(MoonlightName);
            if (moon == null) { Debug.LogWarning(Tag + "Light '" + MoonlightName + "' not found in the open scene; skipped."); return; }
            if (!state.moonRecorded) { state.moonShadows = (int)moon.shadows; state.moonRecorded = true; }
            Undo.RecordObject(moon, "Moonlight shadows");
            Log("Moonlight shadows " + moon.shadows + "->" + MoonShadows + " (colour " + Fmt(moon.color) + " and intensity " + moon.intensity + " untouched; the bootstrap drives intensity and angle).");
            moon.shadows = MoonShadows;
            EditorUtility.SetDirty(moon);
        }

        // ================================================================ 4a. fake glow
        private static void BuildFakeGlow(Transform root, List<GlowSpot> spots)
        {
            var glowRoot = new GameObject("FakeGlow");
            glowRoot.transform.SetParent(root, false);
            if (spots.Count == 0) { Log("No lamp positions known, fake glow skipped."); return; }

            Material mat = CreateGlowMaterial();
            var meshes = new Dictionary<int, Mesh>();
            foreach (GlowSpot s in spots)
            {
                int key = Mathf.RoundToInt(s.Kelvin);
                if (meshes.ContainsKey(key)) { continue; }
                Mesh mesh = BuildGlowMesh(KelvinToColor(s.Kelvin));
                mesh.name = "SM_NightGlow_" + key + "K";
                AssetDatabase.CreateAsset(mesh, GeneratedFolder + "/" + mesh.name + ".asset");
                meshes[key] = mesh;
            }
            for (int i = 0; i < spots.Count; i++)
            {
                GlowSpot s = spots[i];
                var go = new GameObject("Glow_" + i.ToString("00") + "_" + Mathf.RoundToInt(s.Kelvin) + "K");
                go.transform.SetParent(glowRoot.transform, false);
                go.transform.position = s.Position;
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = meshes[Mathf.RoundToInt(s.Kelvin)];
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                // Distance pooling without a runtime script: a one-level LODGroup hides the glow when it is small on screen.
                var lod = go.AddComponent<LODGroup>();
                lod.fadeMode = LODFadeMode.None;
                lod.SetLODs(new[] { new LOD(GlowCullScreenHeight, new Renderer[] { mr }) });
                lod.RecalculateBounds();
            }
            Log("Fake glow: " + spots.Count + " objects, " + meshes.Count + " shared meshes, 1 shared material " + GlowMaterialPath
                + ", culled below " + (GlowCullScreenHeight * 100f).ToString("0.#") + " % screen height (LODGroup). No real-time light added.");
        }

        private static Material CreateGlowMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            bool urp = shader != null;
            if (!urp) { shader = Shader.Find("Legacy Shaders/Particles/Additive"); }
            if (shader == null) { shader = Shader.Find("Sprites/Default"); Debug.LogWarning(Tag + "No additive shader found, glow material uses Sprites/Default (not additive)."); }
            var m = new Material(shader) { name = "MAT_NightGlow_Additive" };
            if (urp)
            {
                m.SetFloat("_Surface", 1f);       // transparent
                m.SetFloat("_Blend", 2f);         // additive
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.One);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetColor("_BaseColor", new Color(GlowBaseBrightness, GlowBaseBrightness, GlowBaseBrightness, 1f));
            }
            else
            {
                m.SetColor("_TintColor", new Color(0.5f * GlowBaseBrightness, 0.5f * GlowBaseBrightness, 0.5f * GlowBaseBrightness, 0.5f));
            }
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.enableInstancing = true;
            m.SetShaderPassEnabled("ShadowCaster", false);
            if (AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath) != null) { AssetDatabase.DeleteAsset(GlowMaterialPath); }
            AssetDatabase.CreateAsset(m, GlowMaterialPath);
            return AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
        }

        /// <summary>Three crossed discs (2 vertical, 1 horizontal) with a radial falloff in the vertex alpha. About 180 triangles.</summary>
        private static Mesh BuildGlowMesh(Color color)
        {
            const int segments = 12;
            float[] radius = { 0f, 0.20f, 0.55f, 1.10f };
            float[] alpha = { 0.90f, 0.45f, 0.12f, 0f };
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            Vector3[][] axes =
            {
                new[] { Vector3.right, Vector3.up },
                new[] { Vector3.forward, Vector3.up },
                new[] { Vector3.right, Vector3.forward },
            };
            foreach (Vector3[] ax in axes)
            {
                int start = verts.Count;
                verts.Add(Vector3.zero);
                cols.Add(new Color(color.r, color.g, color.b, alpha[0]));
                for (int ring = 1; ring < radius.Length; ring++)
                {
                    for (int s = 0; s < segments; s++)
                    {
                        float a = s * Mathf.PI * 2f / segments;
                        verts.Add((ax[0] * Mathf.Cos(a) + ax[1] * Mathf.Sin(a)) * radius[ring]);
                        cols.Add(new Color(color.r, color.g, color.b, alpha[ring]));
                    }
                }
                for (int s = 0; s < segments; s++)
                {
                    int n = (s + 1) % segments;
                    tris.Add(start); tris.Add(start + 1 + s); tris.Add(start + 1 + n);
                }
                for (int ring = 1; ring < radius.Length - 1; ring++)
                {
                    int a0 = start + 1 + (ring - 1) * segments;
                    int b0 = start + 1 + ring * segments;
                    for (int s = 0; s < segments; s++)
                    {
                        int n = (s + 1) % segments;
                        tris.Add(a0 + s); tris.Add(b0 + s); tris.Add(b0 + n);
                        tris.Add(a0 + s); tris.Add(b0 + n); tris.Add(a0 + n);
                    }
                }
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ================================================================ 4b. reflection probes
        private static void BuildReflectionProbes(Transform root, State state)
        {
            var group = new GameObject("ReflectionProbes");
            group.transform.SetParent(root, false);

            // The street is instantiated at runtime by the bootstrap, so it is not in the scene now. Put a temporary copy in for the bake.
            GameObject streetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StreetPrefabPath);
            GameObject temp = null;
            if (streetPrefab != null)
            {
                temp = (GameObject)PrefabUtility.InstantiatePrefab(streetPrefab);
                Log("Temporary street instance placed for the probe bake (destroyed right after).");
            }
            try
            {
                ReflectionProbe stall = FindStallProbe();
                if (stall != null) { TuneStallProbe(stall, state); }
                else { Debug.LogWarning(Tag + "ReflectionProbe '" + StallProbeName + "' not found in the open scene; skipped."); }

                CreateStreetProbe(group.transform, "RP_Night_West", -StreetProbeX);
                CreateStreetProbe(group.transform, "RP_Night_East", StreetProbeX);
            }
            finally
            {
                if (temp != null) { Object.DestroyImmediate(temp); }
            }
        }

        private static ReflectionProbe FindStallProbe()
        {
            GameObject go = GameObject.Find(StallProbeName);
            return go != null ? go.GetComponent<ReflectionProbe>() : null;
        }

        private static void TuneStallProbe(ReflectionProbe p, State state)
        {
            if (!state.stallProbe.valid)
            {
                var so = new SerializedObject(p);
                state.stallProbe = new ProbeState
                {
                    valid = true,
                    mode = (int)p.mode, refreshMode = (int)p.refreshMode, timeSlicing = (int)p.timeSlicingMode,
                    resolution = p.resolution, importance = p.importance, size = p.size, center = p.center,
                    blend = p.blendDistance, farClip = p.farClipPlane, boxProjection = p.boxProjection, hdr = p.hdr,
                    renderDynamic = so.FindProperty("m_RenderDynamicObjects").boolValue,
                    customTextureGuid = p.customBakedTexture != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p.customBakedTexture)) : "",
                };
            }
            Undo.RecordObject(p, "Tune RP_Stall");
            Log("RP_Stall before: mode " + p.mode + ", refresh " + p.refreshMode + ", " + p.resolution + " px, box " + p.size + ", importance " + p.importance
                + ", blend " + p.blendDistance + ", boxProjection " + p.boxProjection);
            ConfigureProbe(p, new Vector3(44f, 12f, 40f), 2, 3f);
            // Box floor on the road (y = -0.15) so the box projection is correct for the wet road.
            p.center = new Vector3(0f, ProbeBoxFloorY + p.size.y * 0.5f - p.transform.position.y, 0f);
            BakeOrFallback(p, GeneratedFolder + "/RP_Night_Stall.exr");
            Log("RP_Stall after: mode " + p.mode + ", refresh " + p.refreshMode + ", " + p.resolution + " px, box " + p.size + ", centre " + p.center + ", importance " + p.importance);
            EditorUtility.SetDirty(p);
        }

        private static void CreateStreetProbe(Transform parent, string name, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, StreetProbeY, StreetProbeZ);
            var p = go.AddComponent<ReflectionProbe>();
            ConfigureProbe(p, StreetProbeSize, 1, StreetProbeBlend);
            p.center = new Vector3(0f, ProbeBoxFloorY + p.size.y * 0.5f - StreetProbeY, 0f);
            BakeOrFallback(p, GeneratedFolder + "/" + name + ".exr");
            Log("Probe " + name + " at " + go.transform.position + ": box " + p.size + ", " + p.resolution + " px, mode " + p.mode + ", boxProjection " + p.boxProjection);
        }

        private static void ConfigureProbe(ReflectionProbe p, Vector3 size, int importance, float blend)
        {
            p.mode = ReflectionProbeMode.Custom;
            p.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            p.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            p.resolution = ProbeResolution;
            p.hdr = true;
            p.boxProjection = true;
            p.size = size;
            p.importance = importance;
            p.blendDistance = blend;
            p.nearClipPlane = 0.3f;
            p.farClipPlane = ProbeFarClip;
            p.clearFlags = ReflectionProbeClearFlags.Skybox;
            var so = new SerializedObject(p);
            so.FindProperty("m_RenderDynamicObjects").boolValue = true; // the street is not static
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BakeOrFallback(ReflectionProbe p, string assetPath)
        {
            bool ok = false;
            try { ok = Lightmapping.BakeReflectionProbe(p, assetPath); }
            catch (Exception e) { Debug.LogWarning(Tag + "BakeReflectionProbe threw: " + e.Message); }
            if (ok)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                Texture baked = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
                p.customBakedTexture = baked;
                Log("Baked " + assetPath + " (" + (baked != null ? baked.width + " px cube" : "texture not loaded") + ").");
            }
            else
            {
                // Documented fallback: realtime, refreshed once when the object wakes up. Cost: 6 face renders at 64 px, once.
                p.mode = ReflectionProbeMode.Realtime;
                p.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                p.resolution = 64;
                p.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                Debug.LogWarning(Tag + "Bake failed for " + assetPath + "; fell back to Realtime / OnAwake / 64 px (one-off cost at scene start).");
            }
        }

        // ================================================================ 5. volume profile
        private static void BuildAndAssignProfile(State state)
        {
            VolumeProfile old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(OldProfilePath);
            if (old == null) { Debug.LogError(Tag + "Old profile missing: " + OldProfilePath); return; }
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(NewProfilePath) == null)
            {
                if (!AssetDatabase.CopyAsset(OldProfilePath, NewProfilePath)) { Debug.LogError(Tag + "Could not copy the profile."); return; }
                Log("Cloned " + OldProfilePath + " -> " + NewProfilePath + " (the old asset is not modified).");
            }
            else { Log(NewProfilePath + " exists; values are set again."); }
            VolumeProfile p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(NewProfilePath);
            p.name = "VP_TramChanh_Night_v2";
            int removed = p.components.RemoveAll(c => c == null);
            if (removed > 0) { Log("Removed " + removed + " empty component slot(s) copied from the old profile (it lists 2 missing components, the DoF and film grain mentioned in its commit are not in the file)."); }

            // Colour adjustments
            var ca = GetOrAdd<ColorAdjustments>(p);
            ca.postExposure.Override(0.10f);
            ca.contrast.Override(16f);
            ca.saturation.Override(6f);
            ca.colorFilter.Override(Color.white);
            // Bloom
            var bloom = GetOrAdd<Bloom>(p);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.70f);
            bloom.scatter.Override(0.75f);
            bloom.clamp.Override(30f);
            bloom.tint.Override(new Color(1f, 0.92f, 0.82f, 1f));
            // Chromatic aberration explicitly off
            var chroma = GetOrAdd<ChromaticAberration>(p);
            chroma.intensity.Override(0f);
            chroma.active = false;
            // Split toning: cool shadows, warm highlights (soft-light blend, grey 0.5 is neutral)
            var split = GetOrAdd<SplitToning>(p);
            split.shadows.Override(new Color(0.40f, 0.50f, 0.66f, 1f));
            split.highlights.Override(new Color(0.65f, 0.54f, 0.38f, 1f));
            split.balance.Override(-10f);
            // Lift / gamma / gain (rgb multipliers, w = offset)
            var lgg = GetOrAdd<LiftGammaGain>(p);
            lgg.lift.Override(new Vector4(0.97f, 0.985f, 1.04f, -0.01f));
            lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            lgg.gain.Override(new Vector4(1.03f, 1.00f, 0.96f, 0f));
            // Film grain, very light (only added if the old profile does not have it)
            var grain = GetOrAdd<FilmGrain>(p);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.12f);
            grain.response.Override(0.8f);
            // Unchanged by design: Tonemapping (ACES), WhiteBalance (+6 / -2), Vignette (0.28), Bloom.highQualityFiltering.
            Log("Profile v2 values: ColorAdjustments exposure +0.10 contrast 16 saturation 6; Bloom threshold 0.9 intensity 0.70 scatter 0.75 clamp 30 tint (1,0.92,0.82); "
                + "ChromaticAberration off; SplitToning shadows (0.40,0.50,0.66) highlights (0.65,0.54,0.38) balance -10; "
                + "LGG lift (0.97,0.985,1.04,-0.01) gamma neutral gain (1.03,1.00,0.96,0); FilmGrain Thin1 0.12 / 0.8. Tonemapping, WhiteBalance, Vignette unchanged.");
            EditorUtility.SetDirty(p);
            foreach (VolumeComponent c in p.components) { if (c != null) { EditorUtility.SetDirty(c); } }
            AssetDatabase.SaveAssets();

            Volume volume = FindVolume();
            if (volume == null) { Debug.LogWarning(Tag + "No Volume found in the open scene (expected '" + VolumeObjectName + "'); profile not assigned."); return; }
            if (volume.sharedProfile != p)
            {
                if (string.IsNullOrEmpty(state.volumeOldProfileGuid) && volume.sharedProfile != null)
                {
                    state.volumeOldProfileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(volume.sharedProfile));
                }
                Undo.RecordObject(volume, "Switch Volume to VP_TramChanh_Night_v2");
                Log("Volume '" + volume.name + "' sharedProfile " + (volume.sharedProfile != null ? AssetDatabase.GetAssetPath(volume.sharedProfile) : "null") + " -> " + NewProfilePath);
                volume.sharedProfile = p;
                EditorUtility.SetDirty(volume);
            }
            else { Log("Volume already uses v2."); }
        }

        private static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
        {
            T c;
            if (!p.TryGet(out c))
            {
                c = p.Add<T>(true);
                c.name = typeof(T).Name;
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, p);
                Log("  profile: added " + typeof(T).Name);
            }
            c.active = true;
            return c;
        }

        // ================================================================ helpers
        private static bool Preconditions()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError(Tag + "Leave Play Mode first."); return false; }
            var scene = SceneManagerActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path) || !scene.path.EndsWith(MainSceneFile))
            {
                Debug.LogError(Tag + "Open " + MainSceneFile + " as the active scene first (active: '" + scene.path + "').");
                return false;
            }
            string[] required = { StreetPrefabPath, FilamentMatPath, GlassMatPath, SkyboxMatPath, OldProfilePath };
            foreach (string r in required)
            {
                if (AssetDatabase.LoadMainAssetAtPath(r) == null) { Debug.LogError(Tag + "Required asset missing: " + r); return false; }
            }
            return true;
        }

        private static UnityEngine.SceneManagement.Scene SceneManagerActiveScene()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        }

        private static void ClearOldRootAndAssets()
        {
            foreach (GameObject go in SceneManagerActiveScene().GetRootGameObjects())
            {
                if (go.name == RootName)
                {
                    Undo.DestroyObjectImmediate(go);
                    Log("Removed existing root '" + RootName + "' from the scene.");
                    break;
                }
            }
            if (AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.DeleteAsset(GeneratedFolder);
                Log("Deleted generated folder " + GeneratedFolder);
            }
        }

        private static Volume FindVolume()
        {
            Volume best = null;
            foreach (Volume v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                if (!v.gameObject.scene.IsValid()) { continue; }
                if (v.gameObject.name == VolumeObjectName) { return v; }
                if (best == null && v.isGlobal) { best = v; }
            }
            return best;
        }

        private static Light FindSceneLight(string name)
        {
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.gameObject.scene.IsValid() && l.gameObject.name == name) { return l; }
            }
            return null;
        }

        private static void RestoreSceneValues(State s)
        {
            Object rs = GetRenderSettingsObject();
            if (rs != null) { Undo.RecordObject(rs, "Restore fog and ambient"); }
            RenderSettings.fog = s.fog; RenderSettings.fogMode = (FogMode)s.fogMode;
            RenderSettings.fogDensity = s.fogDensity; RenderSettings.fogColor = s.fogColor;
            RenderSettings.ambientMode = (AmbientMode)s.ambientMode;
            RenderSettings.ambientSkyColor = s.ambientSky;
            RenderSettings.ambientEquatorColor = s.ambientEquator;
            RenderSettings.ambientGroundColor = s.ambientGround;
            if (rs != null) { EditorUtility.SetDirty(rs); }
            Log("Fog and ambient restored.");

            Light moon = FindSceneLight(MoonlightName);
            if (moon != null && s.moonRecorded)
            {
                Undo.RecordObject(moon, "Restore Moonlight");
                moon.shadows = (LightShadows)s.moonShadows;
                EditorUtility.SetDirty(moon);
                Log("Moonlight shadows restored to " + moon.shadows);
            }
            ReflectionProbe p = FindStallProbe();
            if (p != null && s.stallProbe.valid)
            {
                ProbeState o = s.stallProbe;
                Undo.RecordObject(p, "Restore RP_Stall");
                p.mode = (ReflectionProbeMode)o.mode; p.refreshMode = (ReflectionProbeRefreshMode)o.refreshMode;
                p.timeSlicingMode = (ReflectionProbeTimeSlicingMode)o.timeSlicing;
                p.resolution = o.resolution; p.importance = o.importance; p.size = o.size; p.center = o.center;
                p.blendDistance = o.blend; p.farClipPlane = o.farClip; p.boxProjection = o.boxProjection; p.hdr = o.hdr;
                p.customBakedTexture = string.IsNullOrEmpty(o.customTextureGuid) ? null
                    : AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(o.customTextureGuid));
                var so = new SerializedObject(p);
                so.FindProperty("m_RenderDynamicObjects").boolValue = o.renderDynamic;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(p);
                Log("RP_Stall restored (realtime / on awake / 128 px / original box).");
            }
        }

        private static Object GetRenderSettingsObject()
        {
            try
            {
                MethodInfo m = typeof(RenderSettings).GetMethod("GetRenderSettings", BindingFlags.Static | BindingFlags.NonPublic);
                return m != null ? m.Invoke(null, null) as Object : null;
            }
            catch { return null; }
        }

        // ---- file backups (prefab and materials are edited in place; originals are kept in Library)
        private static string BackupFile(string assetPath) { return Path.Combine(BackupDir, assetPath.Replace('/', '_')); }

        private static void Backup(State state, string assetPath)
        {
            string target = BackupFile(assetPath);
            if (!File.Exists(target))
            {
                Directory.CreateDirectory(BackupDir);
                File.Copy(Path.Combine(ProjectRoot, assetPath), target, false);
                Log("Backup of " + assetPath + " saved (first apply).");
            }
            if (!state.backedUp.Contains(assetPath)) { state.backedUp.Add(assetPath); }
        }

        private static State LoadState()
        {
            try
            {
                if (File.Exists(StatePath)) { return JsonUtility.FromJson<State>(File.ReadAllText(StatePath)) ?? new State(); }
            }
            catch (Exception e) { Debug.LogWarning(Tag + "State file unreadable: " + e.Message); }
            return new State();
        }

        private static void SaveState(State state)
        {
            Directory.CreateDirectory(StateDir);
            File.WriteAllText(StatePath, JsonUtility.ToJson(state, true));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Colour of a black body at the given temperature (Tanner Helland approximation), linear values 0..1.</summary>
        private static Color KelvinToColor(float kelvin)
        {
            float t = kelvin / 100f;
            float r, g, b;
            if (t <= 66f) { r = 255f; g = 99.4708025861f * Mathf.Log(t) - 161.1195681661f; }
            else { r = 329.698727446f * Mathf.Pow(t - 60f, -0.1332047592f); g = 288.1221695283f * Mathf.Pow(t - 60f, -0.0755148492f); }
            if (t >= 66f) { b = 255f; }
            else if (t <= 19f) { b = 0f; }
            else { b = 138.5177312231f * Mathf.Log(t - 10f) - 305.0447927307f; }
            return new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f), 1f);
        }

        private static string GetF(Material m, string prop) { return m.HasProperty(prop) ? m.GetFloat(prop).ToString("0.###") : "n/a"; }
        private static string Fmt(Color c) { return "(" + c.r.ToString("0.###") + "," + c.g.ToString("0.###") + "," + c.b.ToString("0.###") + ")"; }

        private static void Log(string message)
        {
            Report.AppendLine(message);
            Debug.Log(Tag + message);
        }
    }
}
