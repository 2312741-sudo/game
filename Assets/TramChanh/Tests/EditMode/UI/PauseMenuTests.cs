using NUnit.Framework;
using TramChanh.Core;
using TramChanh.UI.Menu;
using UnityEngine.UIElements;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class PauseMenuTests
    {
        private GameSettings _settings;
        private int _changed;
        private int _appliedQuality;
        private VisualElement _root;

        private PauseMenu Make(string[] quality)
        {
            _settings = new GameSettings();
            _root = new VisualElement();
            _appliedQuality = -999;
            return new PauseMenu(_root, _settings, null, null, null, () => _changed++, applyQuality: l => _appliedQuality = l, qualityNames: quality);
        }

        [Test]
        public void HasAllControls()
        {
            Make(new[] { "Thấp", "Vừa", "Cao" });
            Assert.That(_root.Q<Slider>("MasterVolume"), Is.Not.Null);
            Assert.That(_root.Q<Slider>("SfxVolume"), Is.Not.Null);
            Assert.That(_root.Q<Slider>("AmbientVolume"), Is.Not.Null);
            Assert.That(_root.Q<Slider>("LookScale"), Is.Not.Null);
            Assert.That(_root.Q<Toggle>("ShowKeyHints"), Is.Not.Null);
            var dropdown = _root.Q<DropdownField>("QualityLevel");
            Assert.That(dropdown, Is.Not.Null);
            Assert.That(dropdown.choices.Count, Is.EqualTo(3));
        }

        [Test]
        public void Setters_UpdateSettingsAndReport()
        {
            PauseMenu menu = Make(new[] { "Thấp", "Cao" });
            menu.SetSfxVolume(0.3f);
            menu.SetAmbientVolume(0.2f);
            menu.SetShowKeyHints(false);
            menu.SetQualityLevel(1);
            Assert.That(_settings.SfxVolume, Is.EqualTo(0.3f).Within(1e-6f));
            Assert.That(_settings.AmbientVolume, Is.EqualTo(0.2f).Within(1e-6f));
            Assert.That(_settings.ShowKeyHints, Is.False);
            Assert.That(_settings.QualityLevel, Is.EqualTo(1));
            Assert.That(_appliedQuality, Is.EqualTo(1));
            Assert.That(_changed, Is.EqualTo(4));
            Assert.That(_root.Q<Slider>("SfxVolume").value, Is.EqualTo(0.3f).Within(1e-6f));
        }

        [Test]
        public void ResetToDefaults_RestoresNewAndOldValues()
        {
            PauseMenu menu = Make(new[] { "Thấp", "Cao" });
            _settings.MasterVolume = 0.1f;
            _settings.LookScale = 2f;
            menu.SetSfxVolume(0.1f);
            menu.SetAmbientVolume(0.1f);
            menu.SetShowKeyHints(false);
            menu.SetQualityLevel(0);
            int before = _changed;
            menu.ResetToDefaults();
            Assert.That(_settings.MasterVolume, Is.EqualTo(GameSettings.DefaultVolume));
            Assert.That(_settings.LookScale, Is.EqualTo(GameSettings.DefaultLookScale));
            Assert.That(_settings.SfxVolume, Is.EqualTo(GameSettings.DefaultSfxVolume));
            Assert.That(_settings.AmbientVolume, Is.EqualTo(GameSettings.DefaultAmbientVolume));
            Assert.That(_settings.ShowKeyHints, Is.EqualTo(GameSettings.DefaultShowKeyHints));
            Assert.That(_settings.QualityLevel, Is.EqualTo(GameSettings.DefaultQualityLevel));
            Assert.That(_appliedQuality, Is.EqualTo(GameSettings.DefaultQualityLevel));
            Assert.That(_changed, Is.EqualTo(before + 1));
            Assert.That(_root.Q<Slider>("AmbientVolume").value, Is.EqualTo(GameSettings.DefaultAmbientVolume).Within(1e-6f));
            Assert.That(_root.Q<Toggle>("ShowKeyHints").value, Is.True);
        }

        [Test]
        public void SingleQualityLevel_DropdownDisabledAndNothingApplied()
        {
            PauseMenu menu = Make(new[] { "Chuẩn" });
            Assert.That(_root.Q<DropdownField>("QualityLevel").enabledSelf, Is.False);
            menu.SetQualityLevel(0);
            Assert.That(_appliedQuality, Is.EqualTo(-999));
            Assert.DoesNotThrow(() => menu.ResetToDefaults());
        }

        [Test]
        public void NoQualityLevels_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Make(new string[0]));
        }
    }
}
