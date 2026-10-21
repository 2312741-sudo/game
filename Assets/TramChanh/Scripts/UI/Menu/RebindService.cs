using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Rebinding for the Gameplay actions (Move parts, Interact, UseHeld, Discard, Pause) on one InputActionAsset: builds the
    /// <see cref="RebindModel"/>, runs PerformInteractiveRebinding, applies and saves the result, and resets to defaults.
    /// Give it the asset the game reads at run time (the PlayerInputReader's copy), not the project asset on disk.
    /// </summary>
    public sealed class RebindService
    {
        public const string MapName = "Gameplay";
        public const float TimeoutSeconds = 10f;
        private static readonly string[] ActionOrder = { "Move", "Interact", "UseHeld", "Discard", "Pause" };

        private readonly InputActionAsset _asset;
        private readonly RebindStore _store;
        private RebindModel _model;
        private InputActionRebindingExtensions.RebindingOperation _operation;
        private RebindResult _pending;
        private bool _mapWasEnabled;

        /// <summary>Loads any saved overrides into <paramref name="asset"/> (corrupt or inconsistent data falls back to the defaults).</summary>
        public RebindService(InputActionAsset asset, IRebindTextStore textStore)
        {
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _store = new RebindStore(textStore ?? throw new ArgumentNullException(nameof(textStore)));
            _store.Load(_asset);
            _model = BuildModel();
            if (!_model.IsConsistent())
            {
                _asset.RemoveAllBindingOverrides();
                _store.Clear();
                _model = BuildModel();
            }
        }

        public RebindModel Model => _model;
        public bool IsRebinding => _operation != null;
        /// <summary>Raised after any key changed or was reset.</summary>
        public event Action Changed;

        /// <summary>Start listening for the next key or button for a slot. <paramref name="onDone"/> gets the result (Cancelled if the player backed out).</summary>
        public bool BeginRebind(string slotId, bool swapOnConflict, Action<RebindResult> onDone)
        {
            RebindSlot slot = _model.Find(slotId);
            if (slot == null || IsRebinding) { return false; }
            InputAction action = _asset.FindAction(MapName + "/" + slot.ActionName, false);
            if (action == null) { return false; }

            // An action must be disabled while it is being rebound.
            _mapWasEnabled = action.actionMap != null && action.actionMap.enabled;
            if (_mapWasEnabled) { action.actionMap.Disable(); }
            _pending = null;

            InputActionRebindingExtensions.RebindingOperation op = action.PerformInteractiveRebinding(slot.BindingIndex)
                .WithControlsHavingToMatchPath(slot.Device == RebindDevice.Keyboard ? "<Keyboard>" : "<Mouse>")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsExcluding("<Keyboard>/anyKey")
                .OnMatchWaitForAnother(0.1f)
                .WithTimeout(TimeoutSeconds);
            // Esc backs out, except when Pause itself is being rebound (then the Cancel button or the timeout does it).
            if (slot.ActionName != "Pause") { op = op.WithCancelingThrough("<Keyboard>/escape"); }

            op.OnApplyBinding((operation, path) => { _pending = Apply(slot.Id, path, swapOnConflict); });
            op.OnComplete(operation => Finish(onDone));
            op.OnCancel(operation => { _pending = null; Finish(onDone); });
            _operation = op;
            op.Start();
            return true;
        }

        /// <summary>Stop a running interactive rebind without changing anything.</summary>
        public void CancelRebind()
        {
            if (_operation != null) { _operation.Cancel(); }
        }

        public RebindResult Reset(string slotId)
        {
            RebindResult result = _model.Reset(slotId);
            WriteChanges(result);
            return result;
        }

        public RebindResult ResetAll()
        {
            RebindResult result = _model.ResetAll();
            WriteChanges(result);
            return result;
        }

        /// <summary>Set a slot to a control path without listening for input (used by tests and by "swap" offers).</summary>
        public RebindResult Apply(string slotId, string path, bool swapOnConflict)
        {
            RebindResult result = _model.TrySet(slotId, path, swapOnConflict);
            WriteChanges(result);
            return result;
        }

        private void Finish(Action<RebindResult> onDone)
        {
            RebindResult result = _pending ?? new RebindResult(RebindOutcome.Cancelled, "Đã hủy đổi phím.");
            _pending = null;
            if (_operation != null) { _operation.Dispose(); _operation = null; }
            if (_mapWasEnabled)
            {
                InputActionMap map = _asset.FindActionMap(MapName, false);
                if (map != null) { map.Enable(); }
            }
            onDone?.Invoke(result);
        }

        private void WriteChanges(RebindResult result)
        {
            if (result == null || !result.Succeeded) { return; }
            foreach (KeyValuePair<string, string> change in result.Changes)
            {
                RebindSlot slot = _model.Find(change.Key);
                InputAction action = slot == null ? null : _asset.FindAction(MapName + "/" + slot.ActionName, false);
                if (action == null) { continue; }
                if (change.Value == slot.DefaultPath) { action.RemoveBindingOverride(slot.BindingIndex); }
                else { action.ApplyBindingOverride(slot.BindingIndex, change.Value); }
            }
            _store.Save(_asset);
            Changed?.Invoke();
        }

        private RebindModel BuildModel()
        {
            var slots = new List<RebindSlot>();
            foreach (string actionName in ActionOrder)
            {
                InputAction action = _asset.FindAction(MapName + "/" + actionName, false);
                if (action == null) { continue; }
                var bindings = action.bindings;
                int count = 0;
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (!bindings[i].isComposite && IsSupported(bindings[i].path)) { count++; }
                }
                for (int i = 0; i < bindings.Count; i++)
                {
                    InputBinding binding = bindings[i];
                    if (binding.isComposite || !IsSupported(binding.path)) { continue; }
                    RebindDevice device = RebindPathText.MatchesDevice(binding.path, RebindDevice.Keyboard) ? RebindDevice.Keyboard : RebindDevice.Mouse;
                    slots.Add(new RebindSlot(binding.id.ToString(), actionName, i, LabelFor(actionName, binding, device, count), device,
                        binding.path, binding.effectivePath));
                }
            }
            return new RebindModel(slots, RebindModel.DefaultReservedPaths);
        }

        private static bool IsSupported(string path)
        {
            return RebindPathText.MatchesDevice(path, RebindDevice.Keyboard) || RebindPathText.MatchesDevice(path, RebindDevice.Mouse);
        }

        private static string LabelFor(string actionName, InputBinding binding, RebindDevice device, int bindingCount)
        {
            string suffix = bindingCount > 1 && !binding.isPartOfComposite ? (device == RebindDevice.Keyboard ? " (phím)" : " (chuột)") : string.Empty;
            switch (actionName)
            {
                case "Move":
                    switch (binding.name)
                    {
                        case "Up": return "Đi tới";
                        case "Down": return "Đi lùi";
                        case "Left": return "Sang trái";
                        case "Right": return "Sang phải";
                        default: return "Di chuyển";
                    }
                case "Interact": return "Tương tác / chọn" + suffix;
                case "UseHeld": return "Dùng vật đang cầm" + suffix;
                case "Discard": return "Bỏ vật đang cầm (giữ)" + suffix;
                case "Pause": return "Tạm dừng" + suffix;
                default: return actionName + suffix;
            }
        }
    }
}
