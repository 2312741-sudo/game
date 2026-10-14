using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;

namespace TramChanh.Tests.EditMode.Core
{
    public sealed class GameSettingsTests
    {
        private sealed class MemoryStore : ISettingsStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void SetFloat(string key, float value) { Values[key] = value; }
        }

        [Test]
        public void Defaults_AreInRange()
        {
            var s = new GameSettings();
            Assert.That(s.MasterVolume, Is.EqualTo(GameSettings.DefaultVolume));
            Assert.That(s.LookScale, Is.EqualTo(GameSettings.DefaultLookScale));
        }

        [TestCase(-1f, 0f)]
        [TestCase(0.3f, 0.3f)]
        [TestCase(5f, 1f)]
        [TestCase(float.NaN, 0.8f)]
        public void MasterVolume_IsClamped(float input, float expected)
        {
            var s = new GameSettings { MasterVolume = input };
            Assert.That(s.MasterVolume, Is.EqualTo(expected).Within(1e-6f));
        }

        [TestCase(0.1f, 0.5f)]
        [TestCase(1.5f, 1.5f)]
        [TestCase(9f, 2f)]
        [TestCase(float.PositiveInfinity, 1f)]
        public void LookScale_IsClamped(float input, float expected)
        {
            var s = new GameSettings { LookScale = input };
            Assert.That(s.LookScale, Is.EqualTo(expected).Within(1e-6f));
        }

        [Test]
        public void SaveThenLoad_RoundTrips()
        {
            var store = new MemoryStore();
            new GameSettings { MasterVolume = 0.25f, LookScale = 1.75f }.Save(store);
            var loaded = new GameSettings();
            loaded.Load(store);
            Assert.That(loaded.MasterVolume, Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(loaded.LookScale, Is.EqualTo(1.75f).Within(1e-6f));
        }

        [Test]
        public void Load_FromEmptyStore_UsesDefaults()
        {
            var s = new GameSettings { MasterVolume = 0.1f, LookScale = 2f };
            s.Load(new MemoryStore());
            Assert.That(s.MasterVolume, Is.EqualTo(GameSettings.DefaultVolume));
            Assert.That(s.LookScale, Is.EqualTo(GameSettings.DefaultLookScale));
        }

        [Test]
        public void Load_OutOfRangeStoredValue_IsClamped()
        {
            var store = new MemoryStore();
            store.Values[GameSettings.VolumeKey] = 7f;
            var s = new GameSettings();
            s.Load(store);
            Assert.That(s.MasterVolume, Is.EqualTo(1f));
        }

        [Test]
        public void ResetToDefaults_RestoresBothValues()
        {
            var s = new GameSettings { MasterVolume = 0f, LookScale = 2f };
            s.ResetToDefaults();
            Assert.That(s.MasterVolume, Is.EqualTo(GameSettings.DefaultVolume));
            Assert.That(s.LookScale, Is.EqualTo(GameSettings.DefaultLookScale));
        }
    }
}
