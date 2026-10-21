using System;
using TramChanh.Interaction.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Thin adapter around <see cref="PauseModel"/>. It does NOT read Esc: <see cref="PlayerInputReader"/> already owns Esc, the cursor
    /// lock and the game clock pause, and <c>PauseMenu</c> (via InteractionPromptView) already draws Resume, volume, look speed, Credits
    /// and Quit. This adds what was missing: Time.timeScale 0 while paused (previous value restored) and a "Về menu chính" button.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        private PauseModel _model;
        private PlayerInputReader _input;
        private Button _mainMenuButton;
        private Action _beforeMainMenu;

        public bool IsPaused => _model != null && _model.IsPaused;

        /// <summary>
        /// The single entry point. <paramref name="root"/> is the UIDocument root (the one InteractionPromptView uses).
        /// <paramref name="beforeMainMenu"/> may save progress; it runs before the scene reloads. Call once after AttachPauseMenu.
        /// </summary>
        public static PauseController Attach(GameObject host, PlayerInputReader input, VisualElement root, Action beforeMainMenu = null)
        {
            if (host == null) { throw new ArgumentNullException(nameof(host)); }
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            if (root == null) { throw new ArgumentNullException(nameof(root)); }
            PauseController controller = host.GetComponent<PauseController>();
            if (controller == null) { controller = host.AddComponent<PauseController>(); }
            controller._input = input;
            controller._beforeMainMenu = beforeMainMenu;
            controller._model = new PauseModel();
            controller.BuildButton(root);
            return controller;
        }

        private void BuildButton(VisualElement root)
        {
            if (_mainMenuButton != null && _mainMenuButton.parent != null) { _mainMenuButton.RemoveFromHierarchy(); }
            _mainMenuButton = new Button(GoToMainMenu) { name = "PauseMainMenuButton", text = "Về menu chính" };
            _mainMenuButton.style.position = Position.Absolute;
            _mainMenuButton.style.bottom = 36f;
            _mainMenuButton.style.left = new Length(50f, LengthUnit.Percent);
            _mainMenuButton.style.marginLeft = -130f;
            _mainMenuButton.style.width = 260f;
            _mainMenuButton.style.height = 40f;
            _mainMenuButton.style.fontSize = 16f;
            _mainMenuButton.style.color = new Color(0.95f, 0.96f, 0.94f, 1f);
            _mainMenuButton.style.backgroundColor = new Color(0.12f, 0.24f, 0.18f, 0.95f);
            _mainMenuButton.style.display = DisplayStyle.None;
            root.Add(_mainMenuButton);
        }

        private void Update()
        {
            if (_model == null || _input == null) { return; }
            PauseTransition change = _model.Sync(_input.IsPausedByUser, _input.ModalOpen, Time.timeScale);
            if (change == PauseTransition.Entered) { Time.timeScale = PauseModel.FrozenTimeScale; }
            else if (change == PauseTransition.Exited) { Time.timeScale = _model.SavedTimeScale; }
            if (_mainMenuButton != null)
            {
                _mainMenuButton.style.display = _model.IsPaused ? DisplayStyle.Flex : DisplayStyle.None;
                if (change == PauseTransition.Entered) { _mainMenuButton.BringToFront(); }
            }
        }

        /// <summary>Restores time and reloads the active scene, which shows the title screen again.</summary>
        public void GoToMainMenu()
        {
            if (_beforeMainMenu != null) { _beforeMainMenu(); }
            RestoreTime();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }

        private void RestoreTime()
        {
            if (_model != null && _model.IsPaused) { Time.timeScale = _model.Release(); }
        }

        private void OnDisable() { RestoreTime(); }
        private void OnDestroy() { RestoreTime(); }
    }
}
