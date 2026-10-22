using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TramChanh.Core.GroundTruth;

namespace TramChanh.EditorTools
{
    /// <summary>
    /// Menu "Tram Chanh/Physics/Build Physics Props". Creates (or updates in place) the Physics Material assets and the physics prefabs
    /// for lessons 4 and 6. Idempotent: running it twice leaves the same assets with the same GUIDs and logs what changed.
    ///
    /// The Props components are looked up by type name (reflection) because TramChanh.Editor does not reference TramChanh.Props
    /// (adding that reference needs the lead; see the hand-off in Docs/Design/COURSE_BAI4_BAI6_PHYSICS.md). Serialized fields are
    /// written through SerializedObject so private [SerializeField] values are set the same way the Inspector would.
    /// Not compiled or run by the author: first run must be checked in the Unity console.
    /// </summary>
    public static class PhysicsPropsBuilder
    {
        public const string MaterialFolder = "Assets/TramChanh/Art/PhysicsMaterials";
        public const string PrefabFolder = "Assets/TramChanh/Prefabs/Physics";
        public const string VisualMaterialPath = "Assets/TramChanh/Art/Materials/MAT_Physics_Placeholder.mat";
        private const string PropsAssembly = "TramChanh.Props";
        private const string IgnoreRaycastLayer = "Ignore Raycast";

        [MenuItem("Tram Chanh/Physics/Build Physics Props")]
        public static void Build()
        {
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            PhysicsMaterial debris = EnsurePhysicsMaterial("PM_Debris", 0.5f, 0.6f, 0.35f,
                PhysicsMaterialCombine.Average, PhysicsMaterialCombine.Maximum);
            EnsurePhysicsMaterial("PM_HangingProp", 0.4f, 0.4f, 0.05f,
                PhysicsMaterialCombine.Average, PhysicsMaterialCombine.Average);

            BuildSpawnerPrefab(debris);
            BuildPuddlePrefab("PF_Physics_PuddleTrigger_Oval", "Assets/TramChanh/Art/Models/Environment/SM_Puddle_Oval.obj", 2.2f, 1.4f);
            BuildPuddlePrefab("PF_Physics_PuddleTrigger_Irregular", "Assets/TramChanh/Art/Models/Environment/SM_Puddle_Irregular.obj", 2.2f, 1.4f);
            BuildHingeSignPrefab();
            BuildSpringBulbPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PhysicsPropsBuilder] Done.");
        }

        // ------------------------------------------------------------------ physics materials

        private static PhysicsMaterial EnsurePhysicsMaterial(string name, float dynamicFriction, float staticFriction, float bounciness,
            PhysicsMaterialCombine friction, PhysicsMaterialCombine bounce)
        {
            string path = MaterialFolder + "/" + name + ".physicMaterial";
            var asset = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            bool created = asset == null;
            if (created)
            {
                asset = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(asset, path);
            }
            bool changed = created
                || !Mathf.Approximately(asset.dynamicFriction, dynamicFriction)
                || !Mathf.Approximately(asset.staticFriction, staticFriction)
                || !Mathf.Approximately(asset.bounciness, bounciness)
                || asset.frictionCombine != friction
                || asset.bounceCombine != bounce;
            asset.dynamicFriction = dynamicFriction;
            asset.staticFriction = staticFriction;
            asset.bounciness = bounciness;
            asset.frictionCombine = friction;
            asset.bounceCombine = bounce;
            if (changed)
            {
                EditorUtility.SetDirty(asset);
                Debug.Log("[PhysicsPropsBuilder] " + (created ? "Created " : "Updated ") + path
                    + $" (dyn {dynamicFriction}, static {staticFriction}, bounce {bounciness}, {friction}/{bounce})");
            }
            return asset;
        }

        // ------------------------------------------------------------------ prefabs

        private static void BuildSpawnerPrefab(PhysicsMaterial debris)
        {
            var go = new GameObject("PF_Physics_DiscardedPropSpawner");
            try
            {
                Component spawner = AddProps(go, "DiscardedPropSpawner");
                if (spawner != null)
                {
                    var so = new SerializedObject(spawner);
                    SetObject(so, "_material", debris);
                    SetInt(so, "_maxAlive", 8);
                    SetFloat(so, "_lifetimeSeconds", 6f);
                    SetFloat(so, "_fadeSeconds", 1.5f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Save(go);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static void BuildPuddlePrefab(string name, string meshPath, float fallbackX, float fallbackZ)
        {
            Vector3 size = new Vector3(fallbackX, 0.3f, fallbackZ);
            Vector3 center = new Vector3(0f, 0.1f, 0f);
            Mesh mesh = LoadMesh(meshPath);
            if (mesh != null)
            {
                size = new Vector3(mesh.bounds.size.x, 0.3f, mesh.bounds.size.z);
                center = new Vector3(mesh.bounds.center.x, 0.1f, mesh.bounds.center.z);
            }
            else
            {
                Debug.LogWarning("[PhysicsPropsBuilder] Mesh not found at " + meshPath + "; using fallback size.");
            }

            var go = new GameObject(name);
            try
            {
                // Not the Interactable layer: PlayerInteractor raycasts that layer with QueryTriggerInteraction.Collide.
                go.layer = LayerMask.NameToLayer(IgnoreRaycastLayer);
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = center;
                box.size = size;
                var body = go.AddComponent<Rigidbody>();   // kinematic: see PuddleSplashTrigger remarks
                body.isKinematic = true;
                body.useGravity = false;
                AddProps(go, "PuddleSplashTrigger");
                Save(go);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>Anchor (kinematic) + pendulum sign on a HingeJoint with angle limits. Placeholder board, no text or logo.</summary>
        private static void BuildHingeSignPrefab()
        {
            var root = new GameObject("PF_Physics_HangingSign_Hinge");
            try
            {
                Rigidbody anchor = MakeAnchor(root);
                GameObject pendulum = new GameObject("Pendulum");
                pendulum.transform.SetParent(root.transform, false);
                pendulum.layer = LayerMask.NameToLayer(IgnoreRaycastLayer);

                AddVisual(pendulum.transform, PrimitiveType.Cube, "Board", new Vector3(0f, -0.42f, 0f), new Vector3(0.45f, 0.26f, 0.03f));
                AddVisual(pendulum.transform, PrimitiveType.Cylinder, "Cord", new Vector3(0f, -0.145f, 0f), new Vector3(0.012f, 0.145f, 0.012f));

                var solid = pendulum.AddComponent<BoxCollider>();
                solid.center = new Vector3(0f, -0.42f, 0f);
                solid.size = new Vector3(0.45f, 0.26f, 0.05f);
                solid.excludeLayers = ExcludeActors();
                solid.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(MaterialFolder + "/PM_HangingProp.physicMaterial");

                Rigidbody body = pendulum.AddComponent<Rigidbody>();
                body.mass = 0.4f;
                body.linearDamping = 0.05f;
                body.angularDamping = 0.8f;       // the "damper" of a hinge pendulum: settles in a few swings
                body.interpolation = RigidbodyInterpolation.Interpolate;

                var hinge = pendulum.AddComponent<HingeJoint>();
                hinge.connectedBody = anchor;
                hinge.autoConfigureConnectedAnchor = true;
                hinge.anchor = Vector3.zero;       // pivot = top of the cord
                hinge.axis = new Vector3(0f, 0f, 1f);
                hinge.useLimits = true;
                hinge.limits = new JointLimits { min = -30f, max = 30f, bounciness = 0.1f, bounceMinVelocity = 0.2f };
                hinge.useSpring = false;
                hinge.enableCollision = false;
                hinge.breakForce = Mathf.Infinity;
                hinge.breakTorque = Mathf.Infinity;

                AddSwayAndZone(pendulum, 0.75f, new Vector3(0f, -0.4f, 0f));
                Save(root);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        /// <summary>Anchor (kinematic) + bulb on a SpringJoint (Spring, Damper, Min/Max distance). Cord is stretched by JointRopeVisual.</summary>
        private static void BuildSpringBulbPrefab()
        {
            var root = new GameObject("PF_Physics_HangingBulb_Spring");
            try
            {
                Rigidbody anchor = MakeAnchor(root);
                const float drop = 0.5f;

                GameObject bulb = new GameObject("Bulb");
                bulb.transform.SetParent(root.transform, false);
                bulb.transform.localPosition = new Vector3(0f, -drop, 0f);
                bulb.layer = LayerMask.NameToLayer(IgnoreRaycastLayer);
                AddVisual(bulb.transform, PrimitiveType.Sphere, "Glass", Vector3.zero, Vector3.one * 0.16f);

                var solid = bulb.AddComponent<SphereCollider>();
                solid.radius = 0.08f;
                solid.excludeLayers = ExcludeActors();
                solid.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(MaterialFolder + "/PM_HangingProp.physicMaterial");

                Rigidbody body = bulb.AddComponent<Rigidbody>();
                body.mass = 0.3f;
                body.linearDamping = 0.2f;
                body.angularDamping = 0.5f;
                body.interpolation = RigidbodyInterpolation.Interpolate;

                var spring = bulb.AddComponent<SpringJoint>();
                spring.connectedBody = anchor;
                spring.autoConfigureConnectedAnchor = true;   // connected anchor = the root point; rest length = the 0.5 m drop
                spring.anchor = Vector3.zero;
                spring.spring = 40f;                          // N/m: sags about m*g/k = 0.3*9.81/40 = 7 cm
                spring.damper = 4f;                           // kills the bounce within a couple of seconds
                spring.minDistance = drop;
                spring.maxDistance = drop;
                spring.tolerance = 0.025f;
                spring.enableCollision = false;
                spring.breakForce = Mathf.Infinity;
                spring.breakTorque = Mathf.Infinity;

                // Cord: cylinder under the root, stretched from the anchor to the bulb every LateUpdate.
                GameObject cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cord.name = "Cord";
                cord.transform.SetParent(root.transform, false);
                UnityEngine.Object.DestroyImmediate(cord.GetComponent<Collider>());
                ApplyVisualMaterial(cord);
                Component rope = AddProps(cord, "JointRopeVisual");
                if (rope != null)
                {
                    var so = new SerializedObject(rope);
                    SetObject(so, "_top", root.transform);
                    SetObject(so, "_bottom", bulb.transform);
                    SetFloat(so, "_thickness", 0.01f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                AddSwayAndZone(bulb, 0.6f, Vector3.zero);
                Save(root);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        // ------------------------------------------------------------------ helpers

        private static Rigidbody MakeAnchor(GameObject root)
        {
            Rigidbody anchor = root.AddComponent<Rigidbody>();
            anchor.isKinematic = true;   // the joint partner that never moves; it travels with the prefab when the lead places it
            anchor.useGravity = false;
            return anchor;
        }

        private static void AddSwayAndZone(GameObject body, float zoneRadius, Vector3 zoneCenter)
        {
            Component driver = AddProps(body, "HangingSwayDriver");
            var zone = new GameObject("NudgeZone");
            zone.layer = LayerMask.NameToLayer(IgnoreRaycastLayer);
            zone.transform.SetParent(body.transform, false);
            zone.transform.localPosition = zoneCenter;
            var sphere = zone.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = zoneRadius;
            Component forwarder = AddProps(zone, "HangingNudgeZone");
            if (forwarder != null && driver != null)
            {
                var so = new SerializedObject(forwarder);
                SetObject(so, "_driver", driver);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static int ExcludeActors()
        {
            return (1 << TramChanhLayers.PlayerIndex) | (1 << TramChanhLayers.NpcIndex)
                 | (1 << TramChanhLayers.InteractableIndex) | (1 << TramChanhLayers.HeldItemIndex);
        }

        private static void AddVisual(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            visual.layer = parent.gameObject.layer;
            ApplyVisualMaterial(visual);
        }

        private static void ApplyVisualMaterial(GameObject visual)
        {
            Material material = EnsureVisualMaterial();
            if (material != null)
            {
                visual.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static Material EnsureVisualMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialPath);
            if (material != null)
            {
                return material;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) { shader = Shader.Find("Standard"); }
            if (shader == null) { return null; }
            material = new Material(shader) { name = "MAT_Physics_Placeholder" };
            if (material.HasProperty("_BaseColor")) { material.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.55f)); }
            AssetDatabase.CreateAsset(material, VisualMaterialPath);
            Debug.Log("[PhysicsPropsBuilder] Created " + VisualMaterialPath + " (neutral grey placeholder, no brand colour).");
            return material;
        }

        private static Component AddProps(GameObject target, string typeName)
        {
            Type type = Type.GetType("TramChanh.Props." + typeName + ", " + PropsAssembly);
            if (type == null)
            {
                Debug.LogError("[PhysicsPropsBuilder] Type TramChanh.Props." + typeName + " not found. Is Scripts/Props compiled?");
                return null;
            }
            Component existing = target.GetComponent(type);
            return existing != null ? existing : target.AddComponent(type);
        }

        private static Mesh LoadMesh(string path)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Mesh mesh) { return mesh; }
            }
            return null;
        }

        private static void Save(GameObject go)
        {
            string path = PrefabFolder + "/" + go.name + ".prefab";
            bool existed = File.Exists(path);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log("[PhysicsPropsBuilder] " + (existed ? "Rebuilt " : "Created ") + path);
        }

        private static void SetObject(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning("[PhysicsPropsBuilder] Field " + field + " not found on " + so.targetObject.GetType().Name); return; }
            p.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning("[PhysicsPropsBuilder] Field " + field + " not found on " + so.targetObject.GetType().Name); return; }
            p.floatValue = value;
        }

        private static void SetInt(SerializedObject so, string field, int value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning("[PhysicsPropsBuilder] Field " + field + " not found on " + so.targetObject.GetType().Name); return; }
            p.intValue = value;
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
        }
    }
}
