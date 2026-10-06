using UnityEngine;

namespace TramChanh.Core.Provisional
{
    /// <summary>
    /// Marks a prefab root as a placeholder (blockout) that will be replaced by
    /// final art. Gameplay must not depend on anything a placeholder provides
    /// beyond its hierarchy names, anchors and colliders, so the swap needs no
    /// script changes (ASSET_INTEGRATION.md §1).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaceholderAsset : MonoBehaviour
    {
        [SerializeField, Tooltip("Task that delivers the final asset, e.g. ART-STALL-001.")]
        private string _finalAssetTask = string.Empty;

        [SerializeField, Tooltip("Decision ids still open for this asset, e.g. DEC-011.")]
        private string _pendingDecisions = string.Empty;

        [SerializeField, TextArea, Tooltip("What is provisional about this placeholder.")]
        private string _notes = string.Empty;

        public string FinalAssetTask => _finalAssetTask;
        public string PendingDecisions => _pendingDecisions;
        public string Notes => _notes;

        public void Configure(string finalAssetTask, string pendingDecisions, string notes)
        {
            _finalAssetTask = finalAssetTask;
            _pendingDecisions = pendingDecisions;
            _notes = notes;
        }
    }
}
