#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TramChanh.App;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using TramChanh.Interaction.Player;
using TramChanh.UI.Prompt;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace TramChanh.Tests.PlayMode.Drinks
{
    public sealed class TeaRackPickupPlayModeTests
    {
        private const string ScenePath = "Assets/TramChanh/Scenes/Test/SCN_TeaRackPickupTest.unity";
        private Scene _scene;
        private PlayerInteractor _interactor;
        private TeaRackController _rack;
        private GameObject _player;
        private InteractionPromptView _prompt;
        private Keyboard _keyboard;
        private readonly InputTestFixture _fixture = new InputTestFixture();

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _fixture.Setup();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(ScenePath);
            yield return null;
            yield return null;
            _player = _scene.GetRootGameObjects().Single(o => o.name == "PF_Player");
            _interactor = _player.GetComponent<PlayerInteractor>();
            _rack = _scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<TeaRackController>()).Single();
            _prompt = _scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<InteractionPromptView>()).Single();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _player.GetComponent<FirstPersonController>().enabled = false;
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            Vector3 point = _rack.InteractionPoint.position;
            _player.transform.position = new Vector3(point.x, 0f, point.z - 0.85f);
            body.enabled = true;
            _player.GetComponent<FirstPersonController>().ViewCamera.transform.LookAt(point);
            _player.GetComponent<PlayerInputReader>().SetCaptured(true);
            Physics.SyncTransforms();
            _interactor.RefreshFocus();
        }

        [UnityTearDown]
        public IEnumerator UnloadScene()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _fixture.TearDown();
        }

        [UnityTest]
        public IEnumerator DRINK_001_EPickupMovesTheSameStoredBagFromRackToPlayerHand()
        {
            int stock = _rack.Stock;
            Assert.That(stock, Is.GreaterThan(0));
            TeaBagItem first = _rack.GetComponentsInChildren<TeaBagItem>().First();
            Assert.That(first.State, Is.EqualTo(TeaBagState.Stored));
            Assert.That(_interactor.Focused, Is.SameAs(_rack));
            Assert.That(_prompt.IsPromptVisible, Is.True);
            Assert.That(_prompt.PromptText, Does.Contain("Take pre-portioned tea bag"));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
            yield return null;
            var held = _interactor.Context.Hands.Current as TeaBagItem;
            Assert.That(held, Is.SameAs(first), "Pickup transfers a stocked bag, not a new quantity of tea.");
            Assert.That(held.State, Is.EqualTo(TeaBagState.Held));
            Assert.That(_rack.Stock, Is.EqualTo(stock - 1));
            Assert.That(_rack.GetComponentsInChildren<TeaBagItem>().Length, Is.EqualTo(stock - 1));
            Transform anchor = _player.GetComponent<HeldItemView>().HoldAnchor;
            Assert.That(held.transform.parent, Is.SameAs(anchor));
            Assert.That(Vector3.Distance(held.HandGrip.position, anchor.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(held.HandGrip.rotation, anchor.rotation), Is.LessThan(0.001f));
            Assert.That(held.GetComponentsInChildren<Renderer>().All(r => r.enabled), Is.True);
            Assert.That(held.GetComponentsInChildren<Collider>().All(c => !c.enabled), Is.True);
            Assert.That(held.GetComponentsInChildren<Transform>(true).All(t => t.gameObject.layer == TramChanhLayers.HeldItemIndex), Is.True);
            Camera camera = _player.GetComponent<FirstPersonController>().ViewCamera;
            foreach (Renderer renderer in held.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 local = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    Vector3 viewport = camera.WorldToViewportPoint(renderer.transform.TransformPoint(local));
                    Assert.That(viewport.z, Is.GreaterThan(camera.nearClipPlane), renderer.name);
                    Assert.That(viewport.x, Is.InRange(0f, 1f), renderer.name);
                    Assert.That(viewport.y, Is.InRange(0f, 1f), renderer.name + " must be visible in the hand");
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DRINK_001_SecondPressAndStaleExecuteCannotConsumeAnotherBag()
        {
            int stock = _rack.Stock;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
            yield return null;
            IHoldable first = _interactor.Context.Hands.Current;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
            yield return null;
            Assert.That(_interactor.Context.Hands.Current, Is.SameAs(first));
            Assert.That(_rack.Stock, Is.EqualTo(stock - 1));
            Assert.That(_interactor.Prompt.Query.BlockedReasonKey, Is.EqualTo("hands.full"));
            ActionBlocked blocked = default;
            using (_interactor.Context.Events.Subscribe<ActionBlocked>(e => blocked = e))
            {
                _rack.Execute(_interactor.Context);
                Assert.That(blocked.ReasonKey, Is.EqualTo("hands.full"));
                Assert.That(_rack.Stock, Is.EqualTo(stock - 1));
            }
        }

        [UnityTest]
        public IEnumerator DRINK_001_EmptyConfiguredRackBlocksWithoutCreatingAHeldBag()
        {
            var root = new GameObject("EmptyRack");
            root.SetActive(false);
            var balance = Object.Instantiate(_scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<PlayerInteractionTestBootstrap>()).Single().Balance);
            try
            {
                var data = new SerializedObject(balance);
                data.FindProperty("_teaRackInitialStock").intValue = 0;
                data.ApplyModifiedPropertiesWithoutUndo();
                var rack = root.AddComponent<TeaRackController>();
                var rackData = new SerializedObject(rack);
                rackData.FindProperty("_id").intValue = 99;
                rackData.FindProperty("_balance").objectReferenceValue = balance;
                rackData.FindProperty("_bagPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TramChanh/Prefabs/Items/PF_TeaBag_PrePortioned.prefab").GetComponent<TeaBagItem>();
                rackData.FindProperty("_allowUnboundPickupForTest").boolValue = true;
                rackData.FindProperty("_bagSlots").arraySize = balance.TeaRackCapacity;
                rackData.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(true);
                Assert.That(rack.Stock, Is.Zero);
                Assert.That(rack.Query(_interactor.Context).BlockedReasonKey, Is.EqualTo("drink.rack.empty"));
                ActionBlocked blocked = default;
                using (_interactor.Context.Events.Subscribe<ActionBlocked>(e => blocked = e))
                {
                    rack.Execute(_interactor.Context);
                    Assert.That(blocked.ReasonKey, Is.EqualTo("drink.rack.empty"));
                    Assert.That(_interactor.Context.Hands.Current, Is.Null);
                    Assert.That(root.GetComponentsInChildren<TeaBagItem>(), Is.Empty);
                }
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(balance);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TC_DRINK_008_TicketFreePickupIsOptInAndCannotCreateAnOrder()
        {
            var serialized = new SerializedObject(_rack);
            serialized.FindProperty("_allowUnboundPickupForTest").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            int stock = _rack.Stock;
            Assert.That(_rack.Query(_interactor.Context).BlockedReasonKey, Is.EqualTo("stall.no_ticket.drink"));
            _rack.Execute(_interactor.Context);
            Assert.That(_rack.Stock, Is.EqualTo(stock));
            Assert.That(_interactor.Context.Hands.Current, Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DRINK_001_QueryIsPureAndPausedExecuteDoesNotConsumeStock()
        {
            int stock = _rack.Stock;
            _rack.Query(_interactor.Context);
            Assert.That((TestDelegate)(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    _rack.Query(_interactor.Context);
                }
            }), Is.Not.AllocatingGCMemory());
            Assert.That(_rack.Stock, Is.EqualTo(stock));
            _interactor.Context.Clock.Pause();
            Assert.That(_rack.Query(_interactor.Context).BlockedReasonKey, Is.EqualTo("interaction.paused"));
            _rack.Execute(_interactor.Context);
            Assert.That(_rack.Stock, Is.EqualTo(stock));
            Assert.That(_interactor.Context.Hands.Current, Is.Null);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
#endif
