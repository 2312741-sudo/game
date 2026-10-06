using System.Collections.Generic;
using UnityEngine;

namespace TramChanh.Stall.Anchors
{
    /// <summary>
    /// Lookup of the stall's anchors by id. Lives on the stall prefab root and
    /// reads the child <see cref="StallAnchor"/> components, so moving an anchor
    /// in the prefab or scene is all that is needed to relocate a station.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StallAnchorSet : MonoBehaviour
    {
        private readonly Dictionary<StallAnchorId, StallAnchor> _byId = new Dictionary<StallAnchorId, StallAnchor>();

        public bool TryGet(StallAnchorId id, out StallAnchor anchor)
        {
            if (_byId.Count == 0)
            {
                Rebuild();
            }

            return _byId.TryGetValue(id, out anchor);
        }

        public void Rebuild()
        {
            _byId.Clear();
            foreach (StallAnchor anchor in GetComponentsInChildren<StallAnchor>(true))
            {
                _byId[anchor.Id] = anchor;
            }
        }

        private void Awake()
        {
            Rebuild();
        }
    }
}
