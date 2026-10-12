#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TramChanh.App;
using TramChanh.App.People;
using TramChanh.Core;
using TramChanh.Customers;
using TramChanh.Orders;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TramChanh.Tests.PlayMode.Integration
{
    /// <summary>PEOPLE-001: simulated customers, pedestrians and motorbikes in SCN_TramChanh_Main.</summary>
    public sealed class TramChanhPeopleTests
    {
        private const string Path = "Assets/TramChanh/Scenes/Gameplay/SCN_TramChanh_Main.unity";
        private Scene _scene;
        private TramChanhMainBootstrap _bootstrap;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Path, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(Path);
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out TramChanhMainBootstrap bootstrap)) { _bootstrap = bootstrap; }
            }
            Assert.That(_bootstrap, Is.Not.Null);
            Assert.That(_bootstrap.IsInitialized, Is.True);
            Assert.That(_bootstrap.People, Is.Not.Null, "Simulated people are on by default.");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded) { yield return SceneManager.UnloadSceneAsync(_scene); }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        [UnityTest]
        public IEnumerator PEOPLE_001_WalkwayGraphBuildsWithoutErrors()
        {
            Assert.That(_bootstrap.People.LayoutErrors, Is.Empty, string.Join("\n", _bootstrap.People.LayoutErrors));
            Assert.That(_bootstrap.People.Graph.Count, Is.GreaterThan(10));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PEOPLE_002_WalkingCustomerOrdersOnlyAfterReachingTheSeat()
        {
            int seat = FirstOccupied();
            Assert.That(seat, Is.GreaterThanOrEqualTo(0));
            Assert.That(_bootstrap.Customers.PhaseAt(seat), Is.EqualTo(CustomerPhase.Arriving));
            Assert.That(_bootstrap.Tables[seat].ActiveOrder.IsValid, Is.False);
            // Simulate up to 40 s of walking deterministically (fallback routes are at most ~36 m at 1.3 m/s, about 28 s).
            for (int i = 0; i < 160 && _bootstrap.Customers.PhaseAt(seat) == CustomerPhase.Arriving; i++) { _bootstrap.People.Tick(0.25d); }
            Assert.That(_bootstrap.Customers.PhaseAt(seat), Is.EqualTo(CustomerPhase.Seated));
            OrderId order = _bootstrap.Tables[seat].ActiveOrder;
            Assert.That(order.IsValid, Is.True);
            Assert.That(_bootstrap.Orders.Get(order).Status, Is.EqualTo(OrderStatus.WaitingForLobby), "Arrival goes through the Lobby path.");
            Assert.That(_bootstrap.Orders.Get(order).Origin.TableId.Value, Is.EqualTo(seat + 1));
            PersonView view = _bootstrap.People.CustomerAt(seat);
            Assert.That(view, Is.Not.Null);
            Vector3 seatPosition = _bootstrap.TableAnchors[seat].Find("Seat").position;
            Assert.That(Vector3.Distance(view.transform.position, seatPosition), Is.LessThan(0.05f), "Customer sits on the table's Seat.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PEOPLE_003_PeopleNeverBlockMovementOrTheInteractionRay()
        {
            for (int i = 0; i < 120; i++) { _bootstrap.People.Tick(0.25d); }
            Assert.That(_bootstrap.People.ActivePedestrians + _bootstrap.People.ActiveBikes, Is.GreaterThan(0), "Ambient people should have spawned.");
            foreach (Collider collider in _bootstrap.People.GetComponentsInChildren<Collider>(true))
            {
                Assert.That(collider.enabled, Is.False, collider.name + " would block the player or the interaction ray.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PEOPLE_004_AmbientBudgetsAreRespected()
        {
            int maxPedestrians = 0, maxBikes = 0;
            for (int i = 0; i < 600; i++)
            {
                _bootstrap.People.Tick(0.25d);
                maxPedestrians = Mathf.Max(maxPedestrians, _bootstrap.People.ActivePedestrians);
                maxBikes = Mathf.Max(maxBikes, _bootstrap.People.ActiveBikes);
            }
            Assert.That(maxPedestrians, Is.GreaterThan(0).And.LessThanOrEqualTo(_bootstrap.People.Settings.MaxPedestrians));
            Assert.That(maxBikes, Is.GreaterThan(0).And.LessThanOrEqualTo(_bootstrap.People.Settings.MaxBikes));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PEOPLE_005_PausedGameFreezesPeople()
        {
            int seat = FirstOccupied();
            PersonView view = _bootstrap.People.CustomerAt(seat);
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsWalking, Is.True, "The opening customer is still walking to the table.");
            var clock = _bootstrap.Interactor.Context.Clock;
            clock.Pause();
            Vector3 before = view.transform.position;
            float until = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < until) { yield return null; }
            Assert.That(Vector3.Distance(before, view.transform.position), Is.LessThan(0.0001f), "Customers must not move while paused.");
            _bootstrap.People.Tick(0d);
            Assert.That(Vector3.Distance(before, view.transform.position), Is.LessThan(0.0001f));
            clock.Resume();
            _bootstrap.People.Tick(0.5d);
            Assert.That(Vector3.Distance(before, view.transform.position), Is.GreaterThan(0.1f), "Customers walk again after resume.");
            yield return null;
        }

        private int FirstOccupied()
        {
            for (int i = 0; i < TramChanhMainBootstrap.TableCount; i++) { if (_bootstrap.Customers.IsOccupied(i)) { return i; } }
            return -1;
        }
    }
}
#endif
