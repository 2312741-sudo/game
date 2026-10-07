using TramChanh.Core;

namespace TramChanh.Content
{
    public interface IContentDatabase
    {
        bool TryGetKind(string itemDefinitionId, out ItemKind kind);
    }
}
