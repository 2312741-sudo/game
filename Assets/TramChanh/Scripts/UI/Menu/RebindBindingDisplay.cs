using System;
using UnityEngine.InputSystem;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Reads the live key of an action so hints never hard-code letters. <c>display.For("Interact")</c> gives "E" (or whatever the
    /// player rebound it to); <c>display.Format("[[Interact]]/[[Interact:mouse]]: quầy")</c> expands tokens inside a hint text.
    /// Hold one instance next to the InputActionAsset the game actually reads (the PlayerInputReader's runtime copy).
    /// </summary>
    public sealed class RebindBindingDisplay
    {
        public const string DefaultMap = "Gameplay";
        private readonly InputActionAsset _asset;
        private readonly string _map;

        public RebindBindingDisplay(InputActionAsset asset, string mapName = DefaultMap)
        {
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _map = string.IsNullOrEmpty(mapName) ? DefaultMap : mapName;
        }

        /// <summary>Display name of the first binding of the action on that device, for a composite action (Move) its first part. "(chưa gán)" when none.</summary>
        public string For(string actionName, RebindDevice device = RebindDevice.Keyboard)
        {
            InputAction action = _asset.FindAction(_map + "/" + actionName, false);
            if (action == null) { return RebindPathText.Unassigned; }
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (binding.isComposite) { continue; }
                if (RebindPathText.MatchesDevice(binding.effectivePath, device)) { return RebindPathText.Display(binding.effectivePath); }
            }
            return RebindPathText.Unassigned;
        }

        /// <summary>Expands [[Action]] and [[Action:mouse]] tokens in a hint text.</summary>
        public string Format(string template)
        {
            return RebindPathText.ExpandTokens(template, For);
        }
    }
}
