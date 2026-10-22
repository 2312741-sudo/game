using UnityEngine;
using UnityEngine.Audio;

namespace TramChanh.Audio
{
    /// <summary>Thin Unity layer over one AudioMixer: SetFloat for volumes and the output group of each bus. No rules live here.</summary>
    public sealed class AudioMixerBridge : MonoBehaviour, IMixerFloatSink
    {
        [SerializeField] private AudioMixer _mixer;
        private readonly AudioMixerGroup[] _groups = new AudioMixerGroup[2];

        public bool HasMixer => _mixer != null;

        /// <summary>Loads Resources/Audio/AM_TramChanh. Returns null when the asset has not been built yet.</summary>
        public static AudioMixer LoadDefaultMixer() => Resources.Load<AudioMixer>(AudioMixerController.AssetResourcePath);

        public void SetMixer(AudioMixer mixer)
        {
            _mixer = mixer;
            _groups[0] = null;
            _groups[1] = null;
        }

        public bool SetFloat(string parameter, float value) => _mixer != null && _mixer.SetFloat(parameter, value);

        /// <summary>The Sfx or Ambient group, or null without a mixer (an AudioSource with a null group plays straight to the listener).</summary>
        public AudioMixerGroup GroupFor(AudioBus bus)
        {
            if (_mixer == null) { return null; }
            int i = (int)bus;
            if (_groups[i] == null)
            {
                AudioMixerGroup[] found = _mixer.FindMatchingGroups(AudioMixerController.GroupPathFor(bus));
                _groups[i] = found != null && found.Length > 0 ? found[0] : null;
            }
            return _groups[i];
        }
    }
}
