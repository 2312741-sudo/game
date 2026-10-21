using System;
using TramChanh.Core;

namespace TramChanh.Save
{
    /// <summary>
    /// Single entry point for save and load. Captures the persistent state of the existing holders through their
    /// own Save/Load (so their clamping rules apply) and writes it with an <see cref="ISaveStore"/>.
    /// Saved: day, money, totals (GameProgress), reputation, owned upgrades, stock levels, difficulty.
    /// Not saved: today's partial counts (a day starts fresh), the daily goal (derived from day and difficulty),
    /// GameSettings (kept in PlayerPrefs on their own).
    /// Never throws to the caller (except for null constructor arguments).
    /// </summary>
    public sealed class SaveService
    {
        private readonly ISaveStore _store;
        private readonly GameProgress _progress;
        private readonly Reputation _reputation;
        private readonly UpgradeBook _upgrades;
        private readonly StockBook _stock;
        private readonly Func<Difficulty> _getDifficulty;
        private readonly Action<Difficulty> _setDifficulty;

        public SaveService(
            ISaveStore store,
            GameProgress progress,
            Reputation reputation,
            UpgradeBook upgrades,
            StockBook stock,
            Func<Difficulty> getDifficulty,
            Action<Difficulty> setDifficulty)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            if (progress == null) { throw new ArgumentNullException(nameof(progress)); }
            if (reputation == null) { throw new ArgumentNullException(nameof(reputation)); }
            if (upgrades == null) { throw new ArgumentNullException(nameof(upgrades)); }
            if (stock == null) { throw new ArgumentNullException(nameof(stock)); }
            if (getDifficulty == null) { throw new ArgumentNullException(nameof(getDifficulty)); }
            if (setDifficulty == null) { throw new ArgumentNullException(nameof(setDifficulty)); }
            _store = store;
            _progress = progress;
            _reputation = reputation;
            _upgrades = upgrades;
            _stock = stock;
            _getDifficulty = getDifficulty;
            _setDifficulty = setDifficulty;
        }

        /// <summary>True when a save file exists (it may still be unreadable). Use for the Continue button.</summary>
        public bool HasSave() { return _store.Exists(); }

        /// <summary>Reads the file and reports whether it can be loaded, without touching the game state.</summary>
        public SaveReadStatus Peek() { return SafeRead().Status; }

        public Result Save()
        {
            try
            {
                return _store.Write(Capture());
            }
            catch (Exception)
            {
                return Result.Fail("save.capture");
            }
        }

        /// <summary>
        /// Loads the save into the holders. On a missing, corrupt or other-version file nothing is changed
        /// and the failure key says why: save.missing, save.corrupt, save.version, save.io.
        /// </summary>
        public Result TryLoad()
        {
            SaveReadResult read = SafeRead();
            switch (read.Status)
            {
                case SaveReadStatus.Ok: break;
                case SaveReadStatus.Missing: return Result.Fail("save.missing");
                case SaveReadStatus.Corrupt: return Result.Fail("save.corrupt");
                case SaveReadStatus.VersionMismatch: return Result.Fail("save.version");
                default: return Result.Fail("save.io");
            }
            try
            {
                Apply(read.Data);
                return Result.Success();
            }
            catch (Exception)
            {
                return Result.Fail("save.corrupt");
            }
        }

        /// <summary>Deletes the save file (for a New Game).</summary>
        public Result DeleteSave() { return _store.Delete(); }

        public SaveData Capture()
        {
            var snapshot = new DictionarySettingsStore();
            _progress.Save(snapshot);
            _reputation.Save(snapshot);
            _upgrades.Save(snapshot);
            _stock.Save(snapshot);
            Difficulties.Save(snapshot, _getDifficulty());

            var data = new SaveData { savedAtUtc = DateTime.UtcNow.ToString("o") };
            foreach (string key in snapshot.Keys)
            {
                snapshot.TryGetFloat(key, out float value);
                if (float.IsNaN(value) || float.IsInfinity(value)) { continue; }
                data.entries.Add(new SaveEntry(key, value));
            }
            return data;
        }

        public void Apply(SaveData data)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            var snapshot = new DictionarySettingsStore();
            if (data.entries != null)
            {
                foreach (SaveEntry entry in data.entries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.key)) { snapshot.SetFloat(entry.key, entry.value); }
                }
            }
            // Order matters: upgrades set the stock capacity, and stock levels are clamped to it.
            _upgrades.Load(snapshot);
            _reputation.Load(snapshot);
            _progress.Load(snapshot);
            _stock.SetCapacityBonus(_upgrades.IceCapacityBonus, _upgrades.OtherCapacityBonus);
            _stock.Load(snapshot);
            _setDifficulty(Difficulties.Load(snapshot));
        }

        private SaveReadResult SafeRead()
        {
            try { return _store.Read(); }
            catch (Exception) { return SaveReadResult.Of(SaveReadStatus.IoError); }
        }
    }
}
