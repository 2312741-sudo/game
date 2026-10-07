using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TramChanh.Drinks.Runtime
{
    public sealed class IceBin : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _id;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private UnityEvent _onScooped = new UnityEvent();
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;

        public InteractionQuery Query(InteractionContext context)
        {
            Availability available = DrinkActionGuards.Step(context, context.Hands.Current as TeaBagItem,
                TeaBagState.LemonJellyAdded, "drink.need_lemon_first");
            return new InteractionQuery(available, "drink.add_ice");
        }

        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey ?? "interaction.item_not_held"));
                return;
            }
            Result result = ((TeaBagItem)context.Hands.Current).AddIce();
            if (result.IsSuccess)
            {
                _onScooped.Invoke();
            }
            else
            {
                context.Events.Publish(new ActionBlocked(Id, result.ReasonKey));
            }
        }
    }
}
