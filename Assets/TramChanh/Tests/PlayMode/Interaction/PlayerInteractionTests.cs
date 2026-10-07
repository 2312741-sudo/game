#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TramChanh.App;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Interaction;
using TramChanh.Interaction.Player;
using TramChanh.Interaction.Preview;
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

namespace TramChanh.Tests.PlayMode.Interaction
{
    public sealed class PlayerInteractionTests
    {
        private const string ScenePath = "Assets/TramChanh/Scenes/Test/SCN_InteractionTest.unity";
        private Scene _scene;
        private GameObject _player;
        private FirstPersonController _motion;
        private PlayerInputReader _input;
        private PlayerInteractor _interactor;
        private InteractionPromptView _view;
        private readonly InputTestFixture _inputFixture = new InputTestFixture();
        private bool _inputFixtureInitialized;
        private Keyboard _keyboard;
        private Mouse _mouse;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            // Embed the fixture so its setup precedes UnitySetUp scene composition and device creation.
            _inputFixture.Setup();
            _inputFixtureInitialized = true;
            // Isolated test runtime receives queued input even without a focused batch-mode Game view.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Assert.That(System.IO.File.Exists(ScenePath), Is.True, "Saved interaction scene is required.");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(ScenePath);
            yield return null;
            yield return null;
            _player = _scene.GetRootGameObjects().Single(o => o.name == "PF_Player");
            _motion = _player.GetComponent<FirstPersonController>();
            _input = _player.GetComponent<PlayerInputReader>();
            _interactor = _player.GetComponent<PlayerInteractor>();
            _view = _scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<InteractionPromptView>()).Single();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _input.SetCaptured(true);
        }
        [UnityTearDown]
        public IEnumerator UnloadScene()
        {
            if (_keyboard != null && _keyboard.added)
            {
                InputSystem.RemoveDevice(_keyboard);
            }
            if (_mouse != null && _mouse.added)
            {
                InputSystem.RemoveDevice(_mouse);
            }
            if (_scene.IsValid() && _scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_inputFixtureInitialized)
            {
                _inputFixture.TearDown();
                _inputFixtureInitialized = false;
            }
        }
        [UnityTest]
        public IEnumerator CX_011_MoveAndLook_UseGameplayActions()
        {
            // Exercise movement on open ground; collision against the stall is checked separately.
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.position = new Vector3(-3f, 0f, -3f);
            body.enabled = true;
            Vector3 position = _player.transform.position;
            Quaternion rotation = _motion.ViewCamera.transform.rotation;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.A));
            double until = _interactor.Context.Clock.Now + 0.1d;
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (_interactor.Context.Clock.Now < until && timeout.Elapsed.TotalSeconds < 5d)
            {
                yield return null;
            }
            Assert.That(_interactor.Context.Clock.Now, Is.GreaterThanOrEqualTo(until), "Injected movement clock advanced.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(_mouse, new MouseState { delta = new Vector2(60f, -30f) });
            yield return null;
            Assert.That(_player.transform.position.x, Is.LessThan(position.x - 0.05f));
            Assert.That(Quaternion.Angle(rotation, _motion.ViewCamera.transform.rotation), Is.GreaterThan(1f));
        }
        [UnityTest]
        public IEnumerator CX_011_PlayerCannotWalkThroughTheStall()
        {
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.position = new Vector3(0.65f, 0f, -1.2f);
            body.enabled = true;
            _motion.Simulate(new Vector2(0f, 1f), Vector2.zero, 1f);
            Assert.That(_player.transform.position.z, Is.LessThan(-StallDimensions.Depth * 0.5f));
            yield return null;
        }
        [UnityTest]
        public IEnumerator TC_INT_005_CubePromptAppearsAndEExecutesVisibleResponse()
        {
            InspectionInteractable cube = Target("PF_Placeholder_InteractionCube");
            Aim(cube);
            Assert.That(_interactor.Focused, Is.SameAs(cube));
            Assert.That(_view.IsPromptVisible, Is.True);
            Assert.That(_view.PromptText, Does.Contain("test cube"));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
            yield return null;
            Assert.That(cube.IsHighlighted, Is.True);
            Assert.That(_view.PromptText, Does.Contain("Cube inspected"));
            Renderer renderer = cube.GetComponentInChildren<Renderer>();
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_BaseColor").g, Is.GreaterThan(0.7f));
        }
        [UnityTest]
        public IEnumerator TC_INT_005_TeaRackAndGrill_RespondToActionMapPresses()
        {
            foreach (string name in new[] { "PF_RedTeaRack", "PF_Grill_Elmich" })
            {
                InspectionInteractable target = Target(name);
                Aim(target);
                Assert.That(_interactor.Focused, Is.SameAs(target), name);
                Assert.That(_view.IsPromptVisible, Is.True, name);
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
                yield return null;
                Assert.That(target.IsHighlighted, Is.True, name);
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
                yield return null;
            }
        }
        [UnityTest]
        public IEnumerator TC_INT_005_BeyondReachWrongLayerAndEmptyAim_HidePrompt()
        {
            InspectionInteractable cube = Target("PF_Placeholder_InteractionCube");
            Aim(cube);
            Assert.That(_view.IsPromptVisible, Is.True);
            foreach (Collider collider in cube.GetComponentsInChildren<Collider>())
            {
                collider.gameObject.layer = TramChanhLayers.EnvironmentIndex;
            }
            _interactor.RefreshFocus();
            Assert.That(_view.IsPromptVisible, Is.False, "Non-interactable layers are excluded.");
            foreach (Collider collider in cube.GetComponentsInChildren<Collider>())
            {
                collider.gameObject.layer = TramChanhLayers.InteractableIndex;
            }
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.position += Vector3.back * 3f;
            body.enabled = true;
            _motion.ViewCamera.transform.LookAt(cube.InteractionPoint);
            Physics.SyncTransforms();
            _interactor.RefreshFocus();
            Assert.That(_view.IsPromptVisible, Is.False, "Beyond configured reach.");
            _motion.ViewCamera.transform.rotation = Quaternion.LookRotation(Vector3.up);
            _interactor.RefreshFocus();
            Assert.That(_view.IsPromptVisible, Is.False, "Nothing aimed at.");
            yield return null;
        }
        [UnityTest]
        public IEnumerator TC_INT_002_BlockedExecuteRechecksAvailabilityAndPublishesReason()
        {
            InspectionInteractable cube = Target("PF_Placeholder_InteractionCube");
            Aim(cube);
            var serialized = new SerializedObject(cube);
            serialized.FindProperty("_availability").intValue = (int)AvailabilityStatus.Blocked;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ActionBlocked blocked = default;
            using (_interactor.Context.Events.Subscribe<ActionBlocked>(e => blocked = e))
            {
                cube.Execute(_interactor.Context);
                Assert.That(cube.IsHighlighted, Is.False);
                Assert.That(blocked.ReasonKey, Is.EqualTo("preview.blocked"));
            }
            _interactor.RefreshFocus();
            Assert.That(_interactor.Prompt.Query.Availability.Status, Is.EqualTo(AvailabilityStatus.Blocked));
            yield return null;
        }
        [UnityTest]
        public IEnumerator TC_INT_007_UnchangedFocusDoesNotRepublishAndPreviewQueryAllocatesNothing()
        {
            InspectionInteractable cube = Target("PF_Placeholder_InteractionCube");
            Aim(cube);
            int events = 0;
            using (_interactor.Context.Events.Subscribe<InteractionPromptChanged>(e => events++))
            {
                for (int i = 0; i < 30; i++)
                {
                    _interactor.RefreshFocus();
                }
                Assert.That(events, Is.Zero);
            }
            cube.Query(_interactor.Context);
            Assert.That((TestDelegate)(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    cube.Query(_interactor.Context);
                }
            }), Is.Not.AllocatingGCMemory());
            _view.enabled = false;
            _interactor.Press();
            _view.enabled = true;
            Assert.That(_view.PromptText, Does.Contain("Cube inspected"), "Re-enabled UI reads current prompt state.");
            yield return null;
        }
        [UnityTest]
        public IEnumerator TC_INT_007_RepeatedPromptStateDoesNotAllocateLocalizedLabels()
        {
            InspectionInteractable cube = Target("PF_Placeholder_InteractionCube");
            var query = new InteractionQuery(Availability.Available, cube.InspectPromptKey, InteractionKind.Hold, 0.5f);
            IEventBus events = _interactor.Context.Events;
            var state = new InteractionPromptChanged(cube.Id, query, 0.1f);
            events.Publish(state); // Warm UI Toolkit's event pools and layout styles.
            Assert.That((TestDelegate)(() =>
            {
                for (int frame = 0; frame < 10; frame++)
                {
                    events.Publish(state);
                }
            }), Is.Not.AllocatingGCMemory(), "Unchanged prompt rendering must not rebuild localized strings.");
            yield return null;
        }
        [UnityTest]
        public IEnumerator TC_INT_006_EscapePausesMovementAndClickResumes()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
            yield return null;
            Assert.That(_input.IsCaptured, Is.False);
            Assert.That(_interactor.Context.Clock.IsPaused, Is.True);
            Vector3 position = _player.transform.position;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W));
            yield return new WaitForSeconds(0.05f);
            Assert.That(_player.transform.position, Is.EqualTo(position));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left));
            yield return null;
            Assert.That(_input.IsCaptured, Is.True);
            Assert.That(_interactor.Context.Clock.IsPaused, Is.False);
        }
        [UnityTest]
        public IEnumerator CX_011_PlayerReach_CanAimAcrossTheCounterFromFront()
        {
            InspectionInteractable tea = Target("PF_RedTeaRack");
            _motion.enabled = false;
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.position = new Vector3(tea.transform.position.x, 0f, StallDimensions.Depth * 0.5f + 0.5f);
            body.enabled = true;
            _motion.ViewCamera.transform.LookAt(tea.InteractionPoint);
            Physics.SyncTransforms();
            _interactor.RefreshFocus();
            Assert.That(_interactor.Focused, Is.SameAs(tea));
            yield return null;
        }
        [UnityTest]
        public IEnumerator TC_INT_005_SceneSmoke_NoConsoleErrorsAndServicesDisposeOnUnload()
        {
            ServiceRegistry services = _scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<PlayerInteractionTestBootstrap>()).Single().Services;
            for (int frame = 0; frame < 10; frame++)
            {
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
            yield return SceneManager.UnloadSceneAsync(_scene);
            Assert.Throws<ObjectDisposedException>(() => services.Get<IEventBus>());
        }
        private InspectionInteractable Target(string name)
        {
            return _scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<InspectionInteractable>()).Single(t => t.name == name);
        }
        private void Aim(InspectionInteractable target)
        {
            _motion.enabled = false;
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            Vector3 point = target.InteractionPoint.position;
            _player.transform.position = new Vector3(point.x, 0f, point.z - 0.85f);
            body.enabled = true;
            _motion.ViewCamera.transform.LookAt(point);
            Physics.SyncTransforms();
            _input.SetCaptured(true);
            _interactor.RefreshFocus();
        }
    }
}
#endif
