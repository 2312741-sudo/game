using System;
using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.Audio
{
    /// <summary>Inspector-assigned map from cue to clip. Create via Assets > Create > Tram Chanh > Audio Cue Library.</summary>
    [CreateAssetMenu(fileName = "AudioCueLibrary", menuName = "Tram Chanh/Audio Cue Library")]
    public sealed class AudioCueLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public AudioCue Cue;
            [Tooltip("Empty = the cue stays silent.")] public AudioClip Clip;
            [Tooltip("When off, the built-in default profile for this cue is used.")] public bool OverrideProfile;
            public AudioCueProfile Profile;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        public AudioClip GetClip(AudioCue cue)
        {
            Entry e = Find(cue);
            return e == null ? null : e.Clip;
        }

        public AudioCueProfile GetProfile(AudioCue cue)
        {
            Entry e = Find(cue);
            return e != null && e.OverrideProfile ? e.Profile : AudioCueProfile.Default(cue);
        }

        /// <summary>Runtime and test helper; in the editor assign entries in the Inspector.</summary>
        public void SetEntry(AudioCue cue, AudioClip clip)
        {
            Entry e = Find(cue);
            if (e == null)
            {
                e = new Entry { Cue = cue, Profile = AudioCueProfile.Default(cue) };
                _entries.Add(e);
            }
            e.Clip = clip;
        }

        private Entry Find(AudioCue cue)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i] != null && _entries[i].Cue == cue) { return _entries[i]; }
            }
            return null;
        }
    }
}
