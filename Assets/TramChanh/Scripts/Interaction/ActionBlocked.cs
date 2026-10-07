using TramChanh.Core;

namespace TramChanh.Interaction
{
    public readonly struct ActionBlocked
    {
        public InteractableId InteractableId { get; }
        public string ReasonKey { get; }
        public ActionBlocked(InteractableId id, string reasonKey)
        {
            InteractableId = id;
            ReasonKey = reasonKey;
        }
    }
}
