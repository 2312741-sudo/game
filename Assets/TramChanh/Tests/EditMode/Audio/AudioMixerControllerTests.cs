using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Audio;

namespace TramChanh.Tests.EditMode.Audio
{
    public sealed class AudioMixerControllerTests
    {
        private sealed class FakeSink : IMixerFloatSink
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public readonly HashSet<string> Exposed = new HashSet<string> { "MasterVolume", "SfxVolume", "AmbientVolume" };
            public bool SetFloat(string parameter, float value)
            {
                if (!Exposed.Contains(parameter)) { return false; }
                Values[parameter] = value;
                return true;
            }
        }

        [Test]
        public void Apply_WritesDecibelsToTheThreeParameters()
        {
            var sink = new FakeSink();
            bool ok = new AudioMixerController(sink).Apply(1f, 0.5f, 0f);
            Assert.That(ok, Is.True);
            Assert.That(sink.Values["MasterVolume"], Is.EqualTo(0f).Within(1e-4f));
            Assert.That(sink.Values["SfxVolume"], Is.EqualTo(-6.0206f).Within(1e-3f));
            Assert.That(sink.Values["AmbientVolume"], Is.EqualTo(-80f));
        }

        [Test]
        public void NoMixer_ReturnsFalseSoCallerKeepsOldPath()
        {
            var c = new AudioMixerController(null);
            Assert.That(c.HasMixer, Is.False);
            Assert.That(c.Apply(1f, 1f, 1f), Is.False);
            Assert.That(c.SetMaster(0.5f), Is.False);
        }

        [Test]
        public void MissingExposedParameter_ReturnsFalse()
        {
            var sink = new FakeSink();
            sink.Exposed.Remove("AmbientVolume");
            Assert.That(new AudioMixerController(sink).Apply(1f, 1f, 1f), Is.False);
        }

        [Test]
        public void BusMapsToItsParameterAndGroup()
        {
            Assert.That(AudioMixerController.ParameterFor(AudioBus.Sfx), Is.EqualTo("SfxVolume"));
            Assert.That(AudioMixerController.ParameterFor(AudioBus.Ambient), Is.EqualTo("AmbientVolume"));
            Assert.That(AudioMixerController.GroupPathFor(AudioBus.Sfx), Is.EqualTo("Master/Sfx"));
            Assert.That(AudioMixerController.GroupPathFor(AudioBus.Ambient), Is.EqualTo("Master/Ambient"));
        }
    }
}
