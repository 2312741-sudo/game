using System;

namespace TramChanh.UI.Menu
{
    /// <summary>One rebindable binding: an action name, which binding of it, its default key and its current key. Plain data.</summary>
    public sealed class RebindSlot
    {
        public RebindSlot(string id, string actionName, int bindingIndex, string label, RebindDevice device, string defaultPath, string currentPath)
        {
            if (string.IsNullOrEmpty(id)) { throw new ArgumentException("Slot id is required.", nameof(id)); }
            Id = id;
            ActionName = actionName ?? string.Empty;
            BindingIndex = bindingIndex;
            Label = label ?? id;
            Device = device;
            DefaultPath = RebindPathText.Normalize(defaultPath);
            CurrentPath = RebindPathText.Normalize(currentPath);
        }

        public string Id { get; }
        public string ActionName { get; }
        public int BindingIndex { get; }
        public string Label { get; }
        public RebindDevice Device { get; }
        public string DefaultPath { get; }
        public string CurrentPath { get; internal set; }
        public bool IsDefault => CurrentPath == DefaultPath;
    }
}
