using System.IO;
using TramChanh.Effects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// Builds the particle, trail and line effect prefabs by code (Docs/Design/COURSE_BAI7_PARTICLE_SYSTEM.md lists every
    /// module and value). Menu: "Tram Chanh/Effects/Build Effect Prefabs" and "Tram Chanh/Effects/Remove Effect Prefabs".
    ///
    /// Output (all regenerated from scratch on each run, so running twice gives the same files and keeps the GUIDs):
    ///   Assets/TramChanh/Prefabs/Effects/PF_NightEffects.prefab   root with NightEffectsController, Rain, Splash, GroundPlane, wire
    ///   Assets/TramChanh/Prefabs/Effects/PF_FX_Steam_Grill.prefab, PF_FX_Steam_Cup.prefab, PF_FX_Taillight.prefab, PF_FX_PowerCord.prefab
    ///   Assets/TramChanh/Art/Materials/Effects/MAT_FX_*.mat       (URP Particles/Unlit, textures from generate_particle_textures.py)
    /// Needs the textures in Assets/TramChanh/Art/Textures/Effects (run ArtSource/Scripts/generate_particle_textures.py once).
    /// Every change is logged with the prefix [EffectsBuilder].
    /// STATUS: NOT COMPILED AND NOT RUN. Written without Unity. Treat the first run as a test and read the Console.
    /// </summary>
    public static class EffectsBuilder
    {
        private const string BuildMenu = "Tram Chanh/Effects/Build Effect Prefabs";
        private const string RemoveMenu = "Tram Chanh/Effects/Remove Effect Prefabs";
        private const string Tag = "[EffectsBuilder] ";

        private const string TextureFolder = "Assets/TramChanh/Art/Textures/Effects";
        private const string MaterialFolder = "Assets/TramChanh/Art/Materials/Effects";
        private const string PrefabFolder = "Assets/TramChanh/Prefabs/Effects";

        private const string RainPrefab = PrefabFolder + "/PF_NightEffects.prefab";
        private const string GrillSteamPrefab = PrefabFolder + "/PF_FX_Steam_Grill.prefab";
        private const string CupSteamPrefab = PrefabFolder + "/PF_FX_Steam_Cup.prefab";
        private const string TaillightPrefab = PrefabFolder + "/PF_FX_Taillight.prefab";
        private const string CordPrefab = PrefabFolder + "/PF_FX_PowerCord.prefab";

        // ---------------------------------------------------------------- tuning (reasons are in the course document)
        private const float RainBoxSize = 20f;          // metres, side of the emitter box
        private const float RainRate = 700f;            // particles per second at full rain
        private const int RainMaxParticles = 600;
        private const float SplashProbability = 0.15f;  // share of drops that make a splash
        private static readonly Vector2 WindRange = new Vector2(4f, 8f); // Force over Lifetime X, m/s^2

        private enum Blend { Alpha, Additive }

        [MenuItem(BuildMenu)]
        public static void BuildAll()
        {
            Texture2D rain = LoadTexture("T_FX_Raindrop");
            Texture2D splash = LoadTexture("T_FX_Splash");
            Texture2D steam = LoadTexture("T_FX_Steam");
            Texture2D trail = LoadTexture("T_FX_Trail");
            if (rain == null || splash == null || steam == null || trail == null)
            {
                Debug.LogError(Tag + "Textures missing in " + TextureFolder + ". Run: python3 ArtSource/Scripts/generate_particle_textures.py, then refresh the project and run this menu again.");
                return;
            }

            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            Material rainMat = MakeMaterial("MAT_FX_Rain", rain, new Color(1f, 1f, 1f, 1f), Blend.Alpha);
            Material splashMat = MakeMaterial("MAT_FX_Splash", splash, new Color(1f, 1f, 1f, 1f), Blend.Alpha);
            Material steamMat = MakeMaterial("MAT_FX_Steam", steam, new Color(1f, 1f, 1f, 1f), Blend.Alpha);
            Material trailMat = MakeMaterial("MAT_FX_Taillight", trail, new Color(1f, 1f, 1f, 1f), Blend.Additive);
            Material wireMat = MakeWireMaterial("MAT_FX_Wire", new Color(0.04f, 0.04f, 0.045f, 1f));

            GameObject grill = BuildSteam("PF_FX_Steam_Grill", steamMat, 9f, 0.18f, new Vector2(0.25f, 0.42f), new Vector2(1.8f, 2.6f), new Vector2(0.3f, 0.6f), 80, 2, 3);
            SavePrefab(grill, GrillSteamPrefab);
            GameObject cup = BuildSteam("PF_FX_Steam_Cup", steamMat, 4f, 0.03f, new Vector2(0.08f, 0.14f), new Vector2(1.2f, 1.8f), new Vector2(0.15f, 0.3f), 30, 1, 1);
            SavePrefab(cup, CupSteamPrefab);
            SavePrefab(BuildTaillight("PF_FX_Taillight", trailMat), TaillightPrefab);
            SavePrefab(BuildCord("PF_FX_PowerCord", wireMat), CordPrefab);
            SavePrefab(BuildNightEffects("PF_NightEffects", rainMat, splashMat), RainPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(Tag + "Done. Put PF_NightEffects in the scene (or let the bootstrap instantiate it); see Docs/Design/COURSE_BAI7_PARTICLE_SYSTEM.md.");
        }

        [MenuItem(RemoveMenu)]
        public static void RemoveAll()
        {
            foreach (string folder in new[] { PrefabFolder, MaterialFolder })
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    AssetDatabase.DeleteAsset(folder);
                    Debug.Log(Tag + "Deleted folder " + folder + ". The textures in " + TextureFolder + " are kept.");
                }
            }
        }

        // ================================================================ prefabs

        private static GameObject BuildNightEffects(string name, Material rainMat, Material splashMat)
        {
            var root = new GameObject(name);

            // ---- Splash: sub emitter, sibling of the rain (not a child, so Play() of the rain never starts it by itself)
            ParticleSystem splash = NewSystem("Splash", root.transform);
            ConfigureSplash(splash, splashMat);

            // ---- Ground plane used by the Collision module (only its height is driven at run time)
            var plane = new GameObject("GroundPlane");
            plane.transform.SetParent(root.transform, false);

            // ---- Rain
            ParticleSystem rain = NewSystem("Rain", root.transform);
            ConfigureRain(rain, rainMat, plane.transform, splash);

            // ---- Wire (nested prefab instance, hung on the stall at run time)
            var cordAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CordPrefab);
            SagWire wire = null;
            if (cordAsset != null)
            {
                var cord = (GameObject)PrefabUtility.InstantiatePrefab(cordAsset, root.transform);
                wire = cord.GetComponent<SagWire>();
            }

            // ---- Controller and its references
            var controller = root.AddComponent<NightEffectsController>();
            var so = new SerializedObject(controller);
            so.FindProperty("_rain").objectReferenceValue = rain;
            so.FindProperty("_rainGroundPlane").objectReferenceValue = plane.transform;
            so.FindProperty("_grillSteamPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(GrillSteamPrefab);
            so.FindProperty("_cupSteamPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CupSteamPrefab);
            so.FindProperty("_taillightPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(TaillightPrefab);
            so.FindProperty("_wire").objectReferenceValue = wire;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static void ConfigureRain(ParticleSystem ps, Material material, Transform groundPlane, ParticleSystem splash)
        {
            // Main: world space so drops stay where they fell when the player walks; short life because the drops die on the ground.
            ParticleSystem.MainModule main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.prewarm = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(13f, 17f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.72f, 0.80f, 0.92f, 0.30f), new Color(0.92f, 0.96f, 1f, 0.60f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = RainMaxParticles;

            // Emission: Rate over Time is the only thing the controller changes (rain intensity).
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = RainRate;

            // Shape: flat box above the player; rotated so its forward axis (emission direction) points down.
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(RainBoxSize, RainBoxSize, 0.5f);
            shape.rotation = new Vector3(90f, 0f, 0f);

            // Force over Lifetime: wind. All three axes must use the same curve mode.
            ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;
            force.x = new ParticleSystem.MinMaxCurve(WindRange.x, WindRange.y);
            force.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            force.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            // Collision: one infinite plane at the ground (cheaper than world collision); a drop dies on contact and asks the sub emitter for a splash.
            ParticleSystem.CollisionModule collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.Planes;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.AddPlane(groundPlane);
            collision.dampen = 1f;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f;
            collision.sendCollisionMessages = false;

            // Sub Emitter: splash on collision, only a share of the drops.
            ParticleSystem.SubEmittersModule sub = ps.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(splash, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing, SplashProbability);

            // Renderer: Stretched Billboard turns each particle into a streak along its velocity (so the wind tilt shows).
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 8f;
            renderer.velocityScale = 0.02f;
            renderer.cameraVelocityScale = 0f;
            ApplyCheapRendering(renderer, material);
        }

        private static void ConfigureSplash(ParticleSystem ps, Material material)
        {
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.10f);
            main.startColor = new Color(0.85f, 0.90f, 1f, 0.7f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;

            // A sub emitter has no rate of its own: one small burst per trigger.
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)2, (short)3) });

            // Shape: narrow cone pointing up (rotated -90 about X so +Z becomes up).
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.02f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(MakeGradient(Color.white, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) }));

            // Size by Speed: the fast droplets at the start of the jump are bigger than the ones that have almost stopped.
            ParticleSystem.SizeBySpeedModule sizeBySpeed = ps.sizeBySpeed;
            sizeBySpeed.enabled = true;
            sizeBySpeed.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1f)));
            sizeBySpeed.range = new Vector2(0f, 1.5f);

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ApplyCheapRendering(renderer, material);
        }

        private static GameObject BuildSteam(string name, Material material, float rate, float radius, Vector2 size, Vector2 lifetime, Vector2 speed, int maxParticles, int burstMin, int burstMax)
        {
            var go = new GameObject(name);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 2.5f;
            main.loop = true;
            main.prewarm = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.startColor = new Color(0.93f, 0.95f, 0.97f, 0.55f);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;

            // Emission: a steady rate plus a small burst at the start of every loop. The game adds its own puff with Emit().
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstMin, (short)burstMax) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = radius;
            shape.rotation = new Vector3(-90f, 0f, 0f); // up

            // Force over Lifetime: light buoyancy upward and a little drift. Same curve mode on all three axes.
            ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;
            force.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            force.y = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            force.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            // Color over Lifetime: milky white that fades to transparent (alpha 0 at both ends avoids pop-in and pop-out).
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(MakeGradient(Color.white, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) }));

            // Size over Lifetime: the puff starts small and spreads out as it rises.
            ParticleSystem.SizeOverLifetimeModule sizeOver = ps.sizeOverLifetime;
            sizeOver.enabled = true;
            sizeOver.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(1f, 1.6f)));

            // Rotation over Lifetime: slow random spin (radians per second) so overlapping puffs do not look like copies.
            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ApplyCheapRendering(renderer, material);
            return go;
        }

        private static GameObject BuildTaillight(string name, Material material)
        {
            var go = new GameObject(name);
            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.5f;                       // short: a streak of 4 m at 8 m/s
            trail.minVertexDistance = 0.1f;
            trail.widthMultiplier = 0.14f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));
            trail.colorGradient = MakeGradient2(
                new[] { new GradientColorKey(new Color(1f, 0.18f, 0.12f), 0f), new GradientColorKey(new Color(0.65f, 0.02f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(0f, 1f) });
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.autodestruct = false;
            trail.emitting = true;
            trail.generateLightingData = false;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.lightProbeUsage = LightProbeUsage.Off;
            trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
            trail.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            trail.sharedMaterial = material;
            go.AddComponent<TaillightTrail>();
            return go;
        }

        private static GameObject BuildCord(string name, Material material)
        {
            var go = new GameObject(name);
            var a = new GameObject("End_A");
            a.transform.SetParent(go.transform, false);
            var b = new GameObject("End_B");
            b.transform.SetParent(go.transform, false);
            // Default placement in the local space of the stall root (the lead tunes it on the real stall):
            // a power cord from the roof edge down to a socket 2.4 m to the side, hanging 0.5 m below the straight line.
            a.transform.localPosition = new Vector3(0.9f, 2.1f, 0f);
            b.transform.localPosition = new Vector3(3.3f, 0.45f, 0f);

            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.startWidth = 0.03f;
            line.endWidth = 0.03f;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.loop = false;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.sharedMaterial = material;

            SagWire wire = go.AddComponent<SagWire>();
            wire.Configure(a.transform, b.transform, 0.5f, 24);
            wire.Rebuild(); // bakes the Positions into the prefab so the curve is visible in the editor
            return go;
        }

        // ================================================================ helpers

        private static ParticleSystem NewSystem(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // a fresh system may be previewing; its duration cannot be changed while playing
            return ps;
        }

        private static void ApplyCheapRendering(ParticleSystemRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.sortMode = ParticleSystemSortMode.None;
        }

        private static Gradient MakeGradient(Color color, GradientAlphaKey[] alpha)
        {
            return MakeGradient2(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) }, alpha);
        }

        private static Gradient MakeGradient2(GradientColorKey[] colors, GradientAlphaKey[] alpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(colors, alpha);
            return gradient;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            if (saved == null)
            {
                Debug.LogError(Tag + "Could not save " + path);
                return;
            }
            Debug.Log(Tag + (existed ? "Replaced prefab " : "Created prefab ") + path);
        }

        private static Texture2D LoadTexture(string name)
        {
            string path = TextureFolder + "/" + name + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                return null;
            }
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
                if (!importer.sRGBTexture) { importer.sRGBTexture = true; changed = true; }
                if (importer.maxTextureSize != 128) { importer.maxTextureSize = 128; changed = true; }
                if (changed)
                {
                    importer.SaveAndReimport();
                    Debug.Log(Tag + "Fixed import settings of " + path);
                }
            }
            return texture;
        }

        private static Shader FindParticleShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
                Debug.LogWarning(Tag + "URP Particles/Unlit not found, falling back to Sprites/Default.");
            }
            return shader;
        }

        private static Material MakeMaterial(string name, Texture2D texture, Color tint, Blend blend)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Shader shader = FindParticleShader();
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = material == null;
            if (created)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            SetIfPresent(material, "_BaseMap", texture);
            material.mainTexture = texture;
            SetIfPresent(material, "_BaseColor", tint);
            SetIfPresent(material, "_Surface", 1f);                    // transparent
            SetIfPresent(material, "_Blend", blend == Blend.Additive ? 2f : 0f);
            SetIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetIfPresent(material, "_DstBlend", (float)(blend == Blend.Additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            SetIfPresent(material, "_ZWrite", 0f);
            SetIfPresent(material, "_Cull", 0f);                       // two-sided
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            Debug.Log(Tag + (created ? "Created material " : "Updated material ") + path);
            return material;
        }

        private static Material MakeWireMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
                Debug.LogWarning(Tag + "URP Unlit not found, falling back to Sprites/Default for the wire.");
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = material == null;
            if (created)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            SetIfPresent(material, "_BaseColor", color);
            material.color = color;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            Debug.Log(Tag + (created ? "Created material " : "Updated material ") + path);
            return material;
        }

        private static void SetIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property)) { material.SetFloat(property, value); }
        }

        private static void SetIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property)) { material.SetColor(property, value); }
        }

        private static void SetIfPresent(Material material, string property, Texture value)
        {
            if (material.HasProperty(property)) { material.SetTexture(property, value); }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
            Debug.Log(Tag + "Created folder " + assetPath);
        }
    }
}
