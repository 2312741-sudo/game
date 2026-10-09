#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TramChanh.App;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Lobby;
using TramChanh.Orders;
using TramChanh.UI.Orders;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TramChanh.Tests.PlayMode.Integration
{
    /// <summary>
    /// TABLES-03: SCN_TramChanh_Main composes TABLE_01..TABLE_10 plus the takeaway vehicle, and every
    /// table is reachable, aimable and standing on walkable ground. Uses only the TABLES-10 bootstrap
    /// contract (Tables, TableAnchors, Vehicle, CustomerPoints, Interactor, Environment) and the
    /// environment's PlayerSpawn / StallRoot / VehiclePoint / TablePoint / CakeTablePoint anchor names.
    /// Requires the TABLES-10 bootstrap (feature/TABLES-10-integration c7a5de6 or later) to compile.
    /// </summary>
    public sealed class TramChanhTenTableTests
    {
        private const string Path = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        private const int TableCount = 10;
        private const int FirstTableInteractableId = 31;
        private const float EyeHeight = 1.6f;
        private const float AimDistance = 1.0f;
        private const float ReachDistance = 1.8f;
        private const float StandDistance = 0.8f;
        private const float MinAnchorSpacing = 1.2f;
        private const float GroundTolerance = 0.1f;
        // PF_Player CharacterController defaults; the live controller overrides them when found.
        private const float DefaultRadius = 0.22f;
        private const float DefaultHeight = 1.7f;
        private const float DefaultStepOffset = 0.25f;
        // Stall footprint (stall-local, metres) that tables must stay clear of.
        private const float StallHalfX = 0.9f;
        private const float StallHalfZ = 0.4f;

        private static readonly int InteractableMask = 1 << TramChanhLayers.InteractableIndex;
        /// <summary>Walkable ground: everything except gameplay triggers/points and held items (task contract).</summary>
        private static readonly int GroundMask = ~((1 << TramChanhLayers.InteractableIndex) | (1 << TramChanhLayers.HeldItemIndex));
        /// <summary>What can block the player body: everything except the player itself and held items. Triggers are ignored.</summary>
        private static readonly int BodyBlockMask = ~((1 << TramChanhLayers.PlayerIndex) | (1 << TramChanhLayers.HeldItemIndex));
        /// <summary>Visual occluders for the focus ray: non-gameplay geometry (the player ray itself only sees layer 9).</summary>
        private static readonly int OccluderMask = ~((1 << TramChanhLayers.InteractableIndex) | (1 << TramChanhLayers.PlayerIndex)
            | (1 << TramChanhLayers.NpcIndex) | (1 << TramChanhLayers.HeldItemIndex));

        private Scene _scene;
        private TramChanhMainBootstrap _bootstrap;
        private OrderEntryUI _entry;
        private float _radius = DefaultRadius;
        private float _height = DefaultHeight;
        private float _stepOffset = DefaultStepOffset;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(Path);
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out TramChanhMainBootstrap bootstrap)) { _bootstrap = bootstrap; }
                if (root.TryGetComponent(out OrderEntryUI entry)) { _entry = entry; }
            }
            Assert.That(_bootstrap, Is.Not.Null); Assert.That(_entry, Is.Not.Null);
            Assert.That(_bootstrap.IsInitialized, Is.True, "Composition runs in Awake.");
            CharacterController body = _bootstrap.Interactor != null ? _bootstrap.Interactor.GetComponentInParent<CharacterController>() : null;
            if (body != null) { _radius = body.radius; _height = body.height; _stepOffset = body.stepOffset; }
            yield return null;
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded) { yield return SceneManager.UnloadSceneAsync(_scene); }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        // ------------------------------------------------------------------ identity

        [UnityTest]
        public IEnumerator TABLES_001_ExactlyTenTablesWithUniqueIdsPlusTheVehicle()
        {
            Assert.That(_bootstrap.Tables, Is.Not.Null);
            Assert.That(_bootstrap.Tables.Count, Is.EqualTo(TableCount), "Exactly TABLE_01..TABLE_10.");
            Assert.That(_bootstrap.TableAnchors.Count, Is.EqualTo(TableCount), "One runtime anchor root per table.");
            Assert.That(_bootstrap.Vehicle, Is.Not.Null, "The takeaway vehicle is kept.");
            var tableIds = new HashSet<int>();
            var interactableIds = new HashSet<int> { _bootstrap.Vehicle.Id.Value };
            for (int i = 0; i < TableCount; i++)
            {
                TableOrderPoint table = _bootstrap.Tables[i];
                Transform anchor = _bootstrap.TableAnchors[i];
                string name = AnchorName(i);
                Assert.That(table, Is.Not.Null, name);
                Assert.That(anchor, Is.Not.Null, name);
                Assert.That(anchor.name, Is.EqualTo(name), "TableAnchors[" + i + "]");
                Assert.That(anchor.Find("Seat"), Is.Not.Null, name + " has no Seat child.");
                Assert.That(table.TableId.Value, Is.EqualTo(i + 1), name + " TableId");
                Assert.That(table.Id.Value, Is.EqualTo(FirstTableInteractableId + i), name + " interactable id");
                Assert.That(tableIds.Add(table.TableId.Value), Is.True, name + " duplicates TableId " + table.TableId.Value);
                Assert.That(interactableIds.Add(table.Id.Value), Is.True, name + " duplicates interactable id " + table.Id.Value);
                Assert.That(table.InteractionPoint, Is.Not.Null, name + " lost its InteractionPoint.");
                Assert.That(Horizontal(table.transform.position - anchor.position).magnitude, Is.LessThan(0.5f), name + " point is not at its anchor.");
            }
            Assert.That(_bootstrap.DrinkTable, Is.SameAs(_bootstrap.Tables[0]), "DrinkTable is TABLE_01.");
            Assert.That(_bootstrap.CakeTable, Is.SameAs(_bootstrap.Tables[1]), "CakeTable is TABLE_02.");
            IReadOnlyList<OrderPoint> points = _bootstrap.CustomerPoints;
            Assert.That(points.Count, Is.EqualTo(TableCount + 1), "10 tables + vehicle.");
            for (int i = 0; i < TableCount; i++) { Assert.That(points, Has.Member(_bootstrap.Tables[i])); }
            Assert.That(points, Has.Member(_bootstrap.Vehicle));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TABLES_002_AnchorsResolveFromEnvironmentThenLegacyPointsThenFallback()
        {
            Transform environment = _bootstrap.Environment.transform;
            for (int i = 0; i < TableCount; i++)
            {
                Transform source = FindDeep(environment, AnchorName(i));
                if (source == null && i == 0) { source = FindDeep(environment, TramChanhMainBootstrap.TableAnchor); }
                if (source == null && i == 1) { source = FindDeep(environment, TramChanhMainBootstrap.CakeTableAnchor); }
                if (source == null) { continue; } // fallback layout: covered by spacing/ground/route tests
                Assert.That(Vector3.Distance(_bootstrap.TableAnchors[i].position, source.position), Is.LessThan(0.01f),
                    AnchorName(i) + " must sit on environment anchor " + source.name + ".");
            }
            yield return null;
        }

        // ------------------------------------------------------------------ focus

        [UnityTest]
        public IEnumerator TABLES_003_EveryTableIsAimableFromItsOpenSideAtStandingEyeHeight()
        {
            var failures = new StringBuilder();
            for (int i = 0; i < TableCount; i++)
            {
                TableOrderPoint table = _bootstrap.Tables[i];
                string name = AnchorName(i);
                Collider trigger = OwnTrigger(table);
                Assert.That(trigger, Is.Not.Null, name + " has no trigger collider resolving to its InteractableRef.");
                Assert.That(trigger.gameObject.layer, Is.EqualTo(TramChanhLayers.InteractableIndex), name + " trigger layer");
                Assert.That(trigger.bounds.max.y, Is.GreaterThan(0.4f), name + " trigger must not lie on the ground.");
                if (!TryOpenSide(i, out Vector3 side, out string blocked)) { failures.AppendLine(name + ": no open side. " + blocked); continue; }
                Vector3 eye = Ground(table.transform.position) + side * AimDistance + Vector3.up * EyeHeight;
                Vector3 target = trigger.bounds.center;
                Vector3 direction = (target - eye).normalized;
                if (!Physics.Raycast(eye, direction, out RaycastHit hit, ReachDistance, InteractableMask, QueryTriggerInteraction.Collide))
                { failures.AppendLine(name + ": focus ray from " + eye + " misses within " + ReachDistance + " m."); continue; }
                InteractableRef reference = hit.collider.GetComponentInParent<InteractableRef>();
                if (reference == null || !ReferenceEquals(reference.Target, table))
                {
                    failures.AppendLine(name + ": first Interactable hit is " + Describe(hit.collider) + " (" + (reference != null ? Describe(reference) : "no InteractableRef") + "), not this table.");
                    continue;
                }
                // The player ray only sees layer 9; this second ray makes sure no other geometry visually covers the table.
                foreach (RaycastHit occluder in Physics.RaycastAll(eye, direction, hit.distance, OccluderMask, QueryTriggerInteraction.Ignore))
                {
                    if (IsOwnFurniture(occluder.collider, i)) { continue; }
                    failures.AppendLine(name + ": " + Describe(occluder.collider) + " covers the table at " + occluder.distance.ToString("0.00") + " m (table hit at " + hit.distance.ToString("0.00") + " m).");
                }
            }
            Assert.That(failures.Length, Is.EqualTo(0), failures.ToString());
            yield return null;
        }

        // ------------------------------------------------------------------ layout

        [UnityTest]
        public IEnumerator TABLES_004_EveryAnchorAndSeatStandsOnWalkableGround()
        {
            float groundY = _bootstrap.Environment.transform.position.y;
            var failures = new StringBuilder();
            for (int i = 0; i < TableCount; i++)
            {
                Transform anchor = _bootstrap.TableAnchors[i];
                foreach (Transform probe in new[] { anchor, anchor.Find("Seat") })
                {
                    if (probe == null) { failures.AppendLine(AnchorName(i) + ": missing Seat."); continue; }
                    string label = probe == anchor ? AnchorName(i) : AnchorName(i) + "/Seat";
                    Vector3 origin = new Vector3(probe.position.x, groundY + 2f, probe.position.z);
                    bool grounded = false;
                    float lowest = float.PositiveInfinity;
                    foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 5f, GroundMask, QueryTriggerInteraction.Ignore))
                    {
                        // Stool tops/table tops may be hit first; the ground below them is what counts.
                        lowest = Mathf.Min(lowest, hit.point.y);
                        if (Mathf.Abs(hit.point.y - groundY) <= GroundTolerance) { grounded = true; }
                    }
                    if (!grounded)
                    {
                        failures.AppendLine(label + " at " + probe.position + ": no walkable ground within " + GroundTolerance + " m of y=" + groundY
                            + (float.IsPositiveInfinity(lowest) ? " (nothing below)." : " (lowest hit y=" + lowest.ToString("0.00") + ")."));
                    }
                }
            }
            Assert.That(failures.Length, Is.EqualTo(0), failures.ToString());
            yield return null;
        }

        [UnityTest]
        public IEnumerator TABLES_005_AnchorsAreAtLeast1_2mApartAndClearOfStallAndVehicle()
        {
            var failures = new StringBuilder();
            IReadOnlyList<Transform> anchors = _bootstrap.TableAnchors;
            for (int a = 0; a < anchors.Count; a++)
            {
                for (int b = a + 1; b < anchors.Count; b++)
                {
                    float distance = Horizontal(anchors[a].position - anchors[b].position).magnitude;
                    if (distance < MinAnchorSpacing - 0.001f)
                    { failures.AppendLine(anchors[a].name + " and " + anchors[b].name + " are " + distance.ToString("0.00") + " m apart (< " + MinAnchorSpacing + ")."); }
                }
            }
            Transform environment = _bootstrap.Environment.transform;
            Transform stall = FindDeep(environment, TramChanhMainBootstrap.StallRootAnchor);
            Transform vehicle = FindDeep(environment, TramChanhMainBootstrap.VehicleAnchor);
            Assert.That(stall, Is.Not.Null); Assert.That(vehicle, Is.Not.Null);
            foreach (Transform anchor in anchors)
            {
                Vector3 local = stall.InverseTransformPoint(anchor.position);
                if (Mathf.Abs(local.x) < StallHalfX && Mathf.Abs(local.z) < StallHalfZ) { failures.AppendLine(anchor.name + " is inside the stall footprint (stall-local " + local + ")."); }
                float toVehicle = Horizontal(anchor.position - vehicle.position).magnitude;
                if (toVehicle < MinAnchorSpacing) { failures.AppendLine(anchor.name + " is " + toVehicle.ToString("0.00") + " m from VehiclePoint (< " + MinAnchorSpacing + ")."); }
            }
            Assert.That(failures.Length, Is.EqualTo(0), failures.ToString());
            yield return null;
        }

        [UnityTest]
        public IEnumerator TABLES_006_PlayerCanWalkFromSpawnToEveryTableStandPoint()
        {
            Transform spawnAnchor = FindDeep(_bootstrap.Environment.transform, TramChanhMainBootstrap.PlayerSpawnAnchor);
            Assert.That(spawnAnchor, Is.Not.Null);
            Vector3 spawn = Ground(spawnAnchor.position);
            Assert.That(Blockers(spawn), Is.Empty, "PlayerSpawn itself is blocked.");
            var failures = new StringBuilder();
            for (int i = 0; i < TableCount; i++)
            {
                string name = AnchorName(i);
                if (!TryOpenSide(i, out Vector3 side, out string blocked)) { failures.AppendLine(name + ": no free stand point. " + blocked); continue; }
                Vector3 stand = Ground(_bootstrap.TableAnchors[i].position) + side * StandDistance;
                if (TrySegment(spawn, stand, out Collider direct)) { continue; }
                if (TryDetour(spawn, stand)) { continue; }
                failures.AppendLine(name + ": route from PlayerSpawn " + spawn + " to stand point " + stand + " is blocked by " + Describe(direct) + " and no two-segment detour is clear.");
            }
            Assert.That(failures.Length, Is.EqualTo(0), failures.ToString());
            yield return null;
        }

        // ------------------------------------------------------------------ vehicle (unchanged behaviour)

        [UnityTest]
        public IEnumerator TABLES_007_TakeawayVehicleIsAimableAndItsMixedOrderReachesBothStallQueues()
        {
            OrderPoint vehicle = _bootstrap.Vehicle;
            Collider trigger = vehicle.GetComponentInChildren<Collider>();
            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.gameObject.layer, Is.EqualTo(TramChanhLayers.InteractableIndex));
            Vector3 towardPlayer = Horizontal(_bootstrap.Interactor.transform.position - vehicle.transform.position);
            Vector3 eye = vehicle.transform.position + towardPlayer.normalized * AimDistance + Vector3.up * EyeHeight;
            bool aimed = Physics.Raycast(eye, (trigger.bounds.center - eye).normalized, out RaycastHit info, ReachDistance, InteractableMask, QueryTriggerInteraction.Collide);
            Assert.That(aimed, Is.True, "Vehicle is not reachable from 1 m away at eye height.");
            Assert.That(info.collider.GetComponentInParent<InteractableRef>().Target, Is.SameAs(vehicle), "Ray hit " + Describe(info.collider));

            InteractionContext context = _bootstrap.Interactor.Context;
            OrderId order = vehicle.ActiveOrder;
            Assert.That(order.IsValid, Is.True, "The takeaway vehicle always has a waiting customer.");
            Assert.That(_bootstrap.Orders.Get(order).RequestedItems.Count, Is.EqualTo(2));
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Drink) || _bootstrap.Tickets.HasPending(ItemKind.Cake), Is.False, "No ticket before Lobby intake.");
            vehicle.Execute(context);
            Assert.That(_entry.IsOpen, Is.True);
            Assert.That(_entry.Enter(), Is.True);
            Assert.That(_entry.Send(), Is.True);
            Assert.That(_entry.IsOpen, Is.False);
            Assert.That(_bootstrap.Orders.Get(order).Status, Is.EqualTo(OrderStatus.SentToStall));
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Drink), Is.True, "Drink queue");
            Assert.That(_bootstrap.Tickets.HasPending(ItemKind.Cake), Is.True, "Cake queue");
            TeaRackController rack = _bootstrap.DrinkStation.GetComponentInChildren<TeaRackController>();
            Assert.That(rack.Query(context).Availability.IsAvailable, Is.True);
            yield return null;
        }

        // ------------------------------------------------------------------ helpers

        private static string AnchorName(int index) => "TABLE_" + (index + 1).ToString("00");

        private static Vector3 Horizontal(Vector3 v) { v.y = 0f; return v; }

        private Vector3 Ground(Vector3 position) => new Vector3(position.x, _bootstrap.Environment.transform.position.y, position.z);

        private static Collider OwnTrigger(TableOrderPoint table)
        {
            foreach (Collider collider in table.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || !collider.isTrigger) { continue; }
                InteractableRef reference = collider.GetComponentInParent<InteractableRef>();
                if (reference != null && ReferenceEquals(reference.Target, table)) { return collider; }
            }
            return null;
        }

        /// <summary>
        /// Deterministic open side: toward PlayerSpawn first, then the anchor's 8 compass directions
        /// (forward, ±45°, ±90°, ±135°, back). The first whose stand point (0.8 m out) fits the player body wins.
        /// </summary>
        private bool TryOpenSide(int index, out Vector3 side, out string blocked)
        {
            Transform anchor = _bootstrap.TableAnchors[index];
            Transform spawn = FindDeep(_bootstrap.Environment.transform, TramChanhMainBootstrap.PlayerSpawnAnchor);
            var candidates = new List<Vector3>();
            Vector3 towardSpawn = spawn != null ? Horizontal(spawn.position - anchor.position) : Vector3.zero;
            if (towardSpawn.sqrMagnitude > 1e-4f) { candidates.Add(towardSpawn.normalized); }
            Vector3 forward = Horizontal(anchor.forward).sqrMagnitude > 1e-4f ? Horizontal(anchor.forward).normalized : Vector3.forward;
            foreach (float angle in new[] { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f }) { candidates.Add(Quaternion.Euler(0f, angle, 0f) * forward); }
            var report = new StringBuilder();
            foreach (Vector3 candidate in candidates)
            {
                Vector3 stand = Ground(anchor.position) + candidate * StandDistance;
                List<Collider> hits = Blockers(stand);
                if (hits.Count == 0) { side = candidate; blocked = null; return true; }
                report.Append("[" + candidate.ToString("0.00") + ": " + Describe(hits[0]) + "] ");
            }
            side = Vector3.zero; blocked = report.ToString();
            return false;
        }

        private void Capsule(Vector3 feet, out Vector3 bottom, out Vector3 top)
        {
            // Lift the lower sphere by the step offset: curbs and ground the controller can step over never block.
            float lift = Mathf.Max(_stepOffset, 0.05f);
            bottom = feet + Vector3.up * (lift + _radius);
            top = feet + Vector3.up * Mathf.Max(_height - _radius, lift + _radius + 0.01f);
        }

        private List<Collider> Blockers(Vector3 feet)
        {
            Capsule(feet, out Vector3 bottom, out Vector3 top);
            var result = new List<Collider>(Physics.OverlapCapsule(bottom, top, _radius, BodyBlockMask, QueryTriggerInteraction.Ignore));
            result.Sort((a, b) => string.CompareOrdinal(HierarchyPath(a.transform), HierarchyPath(b.transform)));
            return result;
        }

        private bool TrySegment(Vector3 from, Vector3 to, out Collider blocker)
        {
            blocker = null;
            Vector3 delta = Horizontal(to - from);
            float distance = delta.magnitude;
            if (distance < 1e-3f) { return true; }
            Capsule(from, out Vector3 bottom, out Vector3 top);
            if (Physics.CapsuleCast(bottom, top, _radius, delta / distance, out RaycastHit hit, distance, BodyBlockMask, QueryTriggerInteraction.Ignore))
            {
                blocker = hit.collider;
                return false;
            }
            return true;
        }

        /// <summary>Two-segment detours via fixed waypoints: both L corners, then the midpoint pushed sideways 1/2/3 m either way.</summary>
        private bool TryDetour(Vector3 from, Vector3 to)
        {
            Vector3 mid = (from + to) * 0.5f;
            Vector3 along = Horizontal(to - from).normalized;
            Vector3 lateral = new Vector3(-along.z, 0f, along.x);
            var waypoints = new List<Vector3> { new Vector3(from.x, from.y, to.z), new Vector3(to.x, from.y, from.z) };
            foreach (float offset in new[] { 1f, -1f, 2f, -2f, 3f, -3f }) { waypoints.Add(mid + lateral * offset); }
            foreach (Vector3 waypoint in waypoints)
            {
                if (Blockers(waypoint).Count > 0) { continue; }
                if (TrySegment(from, waypoint, out _) && TrySegment(waypoint, to, out _)) { return true; }
            }
            return false;
        }

        /// <summary>The table's own furniture (environment table top/crate under the anchor) may sit between the eye and the trigger.</summary>
        private bool IsOwnFurniture(Collider collider, int index)
        {
            Transform anchor = _bootstrap.TableAnchors[index];
            if (collider.transform.IsChildOf(anchor) || collider.transform.IsChildOf(_bootstrap.Tables[index].transform)) { return true; }
            Bounds bounds = collider.bounds;
            bounds.Expand(0.05f);
            return bounds.Contains(new Vector3(anchor.position.x, bounds.center.y, anchor.position.z));
        }

        private static Transform FindDeep(Transform root, string name)
        {
            Transform direct = root.Find(name);
            if (direct != null) { return direct; }
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) { if (child.name == name) { return child; } }
            return null;
        }

        private static string Describe(Component component)
        {
            if (component == null) { return "<none>"; }
            return HierarchyPath(component.transform) + " [" + component.GetType().Name + ", layer " + LayerMask.LayerToName(component.gameObject.layer) + "]";
        }

        private static string HierarchyPath(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) { path = p.name + "/" + path; }
            return path;
        }
    }
}
#endif
