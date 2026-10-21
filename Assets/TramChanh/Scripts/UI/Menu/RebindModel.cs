using System;
using System.Collections.Generic;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Pure rebinding rules, no Unity types: which key a slot may take, conflict detection (refuse or swap), reserved keys and reset.
    /// The service mirrors the <see cref="RebindResult.Changes"/> onto the real input actions.
    /// </summary>
    public sealed class RebindModel
    {
        /// <summary>Keys read directly by code instead of through an action (R restock/cake step, V view, F1 credits).</summary>
        public static readonly string[] DefaultReservedPaths = { "<keyboard>/r", "<keyboard>/v", "<keyboard>/f1" };

        private readonly List<RebindSlot> _slots;
        private readonly HashSet<string> _reserved = new HashSet<string>();

        public RebindModel(IEnumerable<RebindSlot> slots, IEnumerable<string> reservedPaths = null)
        {
            if (slots == null) { throw new ArgumentNullException(nameof(slots)); }
            _slots = new List<RebindSlot>(slots);
            if (reservedPaths != null)
            {
                foreach (string path in reservedPaths) { _reserved.Add(RebindPathText.Normalize(path)); }
            }
        }

        public IReadOnlyList<RebindSlot> Slots => _slots;

        public RebindSlot Find(string slotId)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Id == slotId) { return _slots[i]; }
            }
            return null;
        }

        public bool IsReserved(string path) => _reserved.Contains(RebindPathText.Normalize(path));

        /// <summary>The other slot that currently uses <paramref name="path"/>, or null.</summary>
        public RebindSlot FindUser(string path, string exceptSlotId)
        {
            string p = RebindPathText.Normalize(path);
            if (p.Length == 0) { return null; }
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Id != exceptSlotId && _slots[i].CurrentPath == p) { return _slots[i]; }
            }
            return null;
        }

        /// <summary>True when every slot has a key of its own device and no two slots share a key. Used to reject corrupt saved overrides.</summary>
        public bool IsConsistent()
        {
            var seen = new HashSet<string>();
            foreach (RebindSlot slot in _slots)
            {
                if (!RebindPathText.MatchesDevice(slot.CurrentPath, slot.Device)) { return false; }
                if (!seen.Add(slot.CurrentPath)) { return false; }
                if (_reserved.Contains(slot.CurrentPath)) { return false; }
            }
            return true;
        }

        /// <summary>Give a slot a new key. With <paramref name="swapOnConflict"/> a taken key is exchanged with the other slot, otherwise refused.</summary>
        public RebindResult TrySet(string slotId, string newPath, bool swapOnConflict)
        {
            return Set(slotId, newPath, swapOnConflict, true);
        }

        /// <summary>Put one slot back to its default key. If another slot holds that key the two are swapped when possible.</summary>
        public RebindResult Reset(string slotId)
        {
            RebindSlot slot = Find(slotId);
            if (slot == null) { return new RebindResult(RebindOutcome.UnknownSlot, "Không tìm thấy phím này."); }
            return Set(slotId, slot.DefaultPath, true, false);
        }

        /// <summary>Put every slot back to its default key. Returns the slots that changed.</summary>
        public RebindResult ResetAll()
        {
            var result = new RebindResult(RebindOutcome.Unchanged, "Các phím đã là mặc định.");
            var changed = new List<RebindSlot>();
            foreach (RebindSlot slot in _slots)
            {
                if (!slot.IsDefault) { slot.CurrentPath = slot.DefaultPath; changed.Add(slot); }
            }
            if (changed.Count == 0) { return result; }
            var applied = new RebindResult(RebindOutcome.Applied, "Đã đặt lại tất cả phím về mặc định.");
            foreach (RebindSlot slot in changed) { applied.AddChange(slot.Id, slot.CurrentPath); }
            return applied;
        }

        private RebindResult Set(string slotId, string newPath, bool swapOnConflict, bool checkReserved)
        {
            RebindSlot slot = Find(slotId);
            if (slot == null) { return new RebindResult(RebindOutcome.UnknownSlot, "Không tìm thấy phím này."); }
            string path = RebindPathText.Normalize(newPath);
            if (!RebindPathText.MatchesDevice(path, slot.Device))
            {
                return new RebindResult(RebindOutcome.WrongDevice,
                    slot.Device == RebindDevice.Keyboard ? "Mục này chỉ nhận phím bàn phím." : "Mục này chỉ nhận nút chuột.");
            }
            if (checkReserved && _reserved.Contains(path))
            {
                return new RebindResult(RebindOutcome.Reserved, "Phím " + RebindPathText.Display(path) + " đã dành cho chức năng khác, hãy chọn phím khác.");
            }
            if (path == slot.CurrentPath) { return new RebindResult(RebindOutcome.Unchanged, "Phím không đổi."); }

            RebindSlot other = FindUser(path, slotId);
            if (other == null)
            {
                slot.CurrentPath = path;
                var applied = new RebindResult(RebindOutcome.Applied, slot.Label + ": " + RebindPathText.Display(path));
                applied.AddChange(slot.Id, path);
                return applied;
            }
            string shown = RebindPathText.Display(path);
            if (!swapOnConflict)
            {
                return new RebindResult(RebindOutcome.Conflict, "Phím " + shown + " đang dùng cho \"" + other.Label + "\". Hãy chọn phím khác.");
            }
            if (!RebindPathText.MatchesDevice(slot.CurrentPath, other.Device))
            {
                return new RebindResult(RebindOutcome.Conflict,
                    "Phím " + shown + " đang dùng cho \"" + other.Label + "\" và không thể đổi chỗ. Hãy chọn phím khác.");
            }
            string old = slot.CurrentPath;
            other.CurrentPath = old;
            slot.CurrentPath = path;
            var swapped = new RebindResult(RebindOutcome.Swapped, "Đã đổi chỗ phím " + shown + " với \"" + other.Label + "\".");
            swapped.AddChange(slot.Id, path);
            swapped.AddChange(other.Id, old);
            return swapped;
        }
    }
}
