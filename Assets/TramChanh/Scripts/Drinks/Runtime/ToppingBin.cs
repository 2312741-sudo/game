using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TramChanh.Drinks.Runtime
{
    public sealed class ToppingBin : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _id;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private ToppingType _toppingType;
        [SerializeField] private UnityEvent _onAdded = new UnityEvent();
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;
        public ToppingType ToppingType => _toppingType;

        public InteractionQuery Query(InteractionContext context)
        {
            bool coconut = _toppingType == ToppingType.CoconutJelly;
            Availability available = DrinkActionGuards.Step(context, context.Hands.Current as TeaBagItem,
                coconut ? TeaBagState.Opened : TeaBagState.CoconutJellyAdded,
                coconut ? "drink.need_open" : "drink.need_coconut_first");
            return new InteractionQuery(available, coconut ? "drink.add_coconut" : "drink.add_lemon");
        }

        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey ?? "interaction.item_not_held"));
                return;
            }
            var bag = (TeaBagItem)context.Hands.Current;
            Result result = _toppingType == ToppingType.CoconutJelly
                ? bag.AddCoconutJelly() : bag.AddLemonJelly();
            if (result.IsSuccess)
            {
                _onAdded.Invoke();
            }
            else
            {
                context.Events.Publish(new ActionBlocked(Id, result.ReasonKey));
            }
        }
    }
}
