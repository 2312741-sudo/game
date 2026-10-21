using System;
using System.IO;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Save;

namespace TramChanh.Tests.EditMode.Save
{
    public sealed class SaveServiceTests
    {
        private string _dir;
        private string _file;

        private sealed class World
        {
            public readonly GameProgress Progress = new GameProgress();
            public readonly Reputation Reputation = new Reputation();
            public readonly UpgradeBook Upgrades = new UpgradeBook();
            public readonly StockBook Stock = new StockBook();
            public Difficulty Difficulty = Difficulty.Normal;
            public SaveService Service;

            public World(ISaveStore store)
            {
                Service = new SaveService(store, Progress, Reputation, Upgrades, Stock, () => Difficulty, d => Difficulty = d);
            }
        }

        private sealed class ThrowingStore : ISaveStore
        {
            public bool Exists() { throw new InvalidOperationException(); }
            public SaveReadResult Read() { throw new InvalidOperationException(); }
            public TramChanh.Core.Result Write(SaveData data) { throw new InvalidOperationException(); }
            public TramChanh.Core.Result Delete() { throw new InvalidOperationException(); }
        }

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "tramchanh_save_test_" + Guid.NewGuid().ToString("N"));
            _file = Path.Combine(_dir, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) { Directory.Delete(_dir, true); } }
            catch (Exception) { }
        }

        [Test]
        public void RoundTripRestoresEverything()
        {
            var a = new World(new JsonFileSaveStore(_file));
            a.Progress.AddBonus(123500);
            a.Progress.EndDay();
            a.Progress.EndDay();
            a.Reputation.AddPoints(150);
            a.Upgrades.Grant(UpgradeId.BiggerIceBin);
            a.Upgrades.Grant(UpgradeId.HelperWorker);
            a.Stock.SetCapacityBonus(a.Upgrades.IceCapacityBonus, a.Upgrades.OtherCapacityBonus);
            a.Stock.FillStart();
            a.Stock.Add(Ingredient.Ice, 10);
            a.Difficulty = Difficulty.Rush;

            Assert.IsTrue(a.Service.Save().IsSuccess);
            Assert.IsTrue(a.Service.HasSave());

            var b = new World(new JsonFileSaveStore(_file));
            Result load = b.Service.TryLoad();

            Assert.IsTrue(load.IsSuccess);
            Assert.AreEqual(3, b.Progress.Day);
            Assert.AreEqual(123500, b.Progress.Money);
            Assert.AreEqual(a.Reputation.Points, b.Reputation.Points);
            Assert.IsTrue(b.Upgrades.Owns(UpgradeId.BiggerIceBin));
            Assert.IsTrue(b.Upgrades.Owns(UpgradeId.HelperWorker));
            Assert.IsFalse(b.Upgrades.Owns(UpgradeId.Pantry));
            Assert.AreEqual(40, b.Stock.Level(Ingredient.Ice));
            Assert.AreEqual(a.Stock.Level(Ingredient.Batter), b.Stock.Level(Ingredient.Batter));
            Assert.AreEqual(Difficulty.Rush, b.Difficulty);
        }

        [Test]
        public void MissingFileFailsCleanlyAndChangesNothing()
        {
            var w = new World(new JsonFileSaveStore(_file));
            w.Progress.AddBonus(1000);

            Assert.IsFalse(w.Service.HasSave());
            Result r = w.Service.TryLoad();

            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual("save.missing", r.ReasonKey);
            Assert.AreEqual(1000, w.Progress.Money);
        }

        [Test]
        public void CorruptFileFailsCleanlyAndChangesNothing()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file, "{ this is not json ][");
            var w = new World(new JsonFileSaveStore(_file));
            w.Progress.AddBonus(1000);

            Result r = w.Service.TryLoad();

            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual("save.corrupt", r.ReasonKey);
            Assert.AreEqual(1000, w.Progress.Money);
            Assert.AreEqual(SaveReadStatus.Corrupt, w.Service.Peek());
        }

        [Test]
        public void EmptyFileIsCorrupt()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file, "");
            var w = new World(new JsonFileSaveStore(_file));

            Assert.AreEqual("save.corrupt", w.Service.TryLoad().ReasonKey);
        }

        [Test]
        public void OtherVersionIsRefused()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file, "{\"version\":999,\"entries\":[{\"key\":\"progress.day\",\"value\":7}]}");
            var w = new World(new JsonFileSaveStore(_file));

            Result r = w.Service.TryLoad();

            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual("save.version", r.ReasonKey);
            Assert.AreEqual(1, w.Progress.Day);
        }

        [Test]
        public void SecondSaveReplacesFirstAndLeavesNoTempFile()
        {
            var w = new World(new JsonFileSaveStore(_file));
            Assert.IsTrue(w.Service.Save().IsSuccess);
            w.Progress.EndDay();
            Assert.IsTrue(w.Service.Save().IsSuccess);

            Assert.IsFalse(File.Exists(_file + ".tmp"));
            var b = new World(new JsonFileSaveStore(_file));
            Assert.IsTrue(b.Service.TryLoad().IsSuccess);
            Assert.AreEqual(2, b.Progress.Day);
        }

        [Test]
        public void DeleteSaveRemovesFile()
        {
            var w = new World(new JsonFileSaveStore(_file));
            w.Service.Save();
            Assert.IsTrue(w.Service.DeleteSave().IsSuccess);
            Assert.IsFalse(w.Service.HasSave());
            Assert.IsTrue(w.Service.DeleteSave().IsSuccess);
        }

        [Test]
        public void StoreThatThrowsNeverReachesTheCaller()
        {
            var w = new World(new ThrowingStore());

            Assert.IsFalse(w.Service.Save().IsSuccess);
            Assert.IsFalse(w.Service.TryLoad().IsSuccess);
        }

        [Test]
        public void UnwritablePathReturnsFailure()
        {
            // A path whose parent is an existing file cannot be created.
            Directory.CreateDirectory(_dir);
            string blocker = Path.Combine(_dir, "blocker");
            File.WriteAllText(blocker, "x");
            var store = new JsonFileSaveStore(Path.Combine(blocker, "save.json"));

            Result r = store.Write(new SaveData());

            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual(SaveReadStatus.Missing, store.Read().Status);
        }

        [Test]
        public void OutOfRangeValuesAreClampedByTheHolders()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_file,
                "{\"version\":1,\"entries\":[{\"key\":\"progress.day\",\"value\":-5},{\"key\":\"reputation.points\",\"value\":99999},{\"key\":\"settings.difficulty\",\"value\":9}]}");
            var w = new World(new JsonFileSaveStore(_file));

            Assert.IsTrue(w.Service.TryLoad().IsSuccess);

            Assert.AreEqual(1, w.Progress.Day);
            Assert.AreEqual(Reputation.MaxPoints, w.Reputation.Points);
            Assert.AreEqual(Difficulty.Normal, w.Difficulty);
        }
    }
}
