using NUnit.Framework;
using TramChanh.Audio;
using UnityEditor;
using UnityEngine.Audio;

namespace TramChanh.Tests.EditMode.Audio
{
    /// <summary>Checks the mixer asset made by Tram Chanh/Audio/Build Audio Mixer. Skipped (not failed) until the asset has been built and committed.</summary>
    public sealed class AudioMixerAssetTests
    {
        private const string Path = "Assets/TramChanh/Resources/Audio/AM_TramChanh.mixer";

        [Test]
        public void MixerAsset_HasGroupsAndExposedParameters()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Path);
            if (mixer == null) { Assert.Ignore("AM_TramChanh.mixer is not built yet: run Tram Chanh/Audio/Build Audio Mixer."); }
            Assert.That(mixer.FindMatchingGroups(AudioMixerController.MasterGroupPath).Length, Is.GreaterThan(0));
            Assert.That(mixer.FindMatchingGroups(AudioMixerController.SfxGroupPath).Length, Is.GreaterThan(0));
            Assert.That(mixer.FindMatchingGroups(AudioMixerController.AmbientGroupPath).Length, Is.GreaterThan(0));
            foreach (string parameter in new[] { AudioMixerController.MasterParameter, AudioMixerController.SfxParameter, AudioMixerController.AmbientParameter })
            {
                Assert.That(mixer.GetFloat(parameter, out _), Is.True, parameter + " is not exposed");
            }
        }
    }
}
