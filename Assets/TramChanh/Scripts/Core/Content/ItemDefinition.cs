using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Content
{
    [CreateAssetMenu(menuName = "Tram Chanh/Content/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private ItemKind _kind;
        [SerializeField] private GameObject _preparedPrefab;
        public string Id => _id;
        public string DisplayName => _displayName;
        public ItemKind Kind => _kind;
        public GameObject PreparedPrefab => _preparedPrefab;
    }
}
