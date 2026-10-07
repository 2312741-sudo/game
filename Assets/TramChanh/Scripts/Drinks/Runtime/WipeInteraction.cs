using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TramChanh.Drinks.Runtime
{
    public sealed class WipeInteraction : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _id;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private UnityEvent _onStarted = new UnityEvent();
        [SerializeField] private UnityEvent _onStopped = new UnityEvent();
        private TeaBagItem _activeBag;
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;
        public bool IsWiping => _activeBag != null;

        public InteractionQuery Query(InteractionContext context)
        {
            var bag = context.Hands.Current as TeaBagItem;
            Availability available = DrinkActionGuards.Step(context, bag, TeaBagState.Shaken, "drink.need_shake_first");
            if (_activeBag != null && !ReferenceEquals(_activeBag, bag))
            {
                available = Availability.Blocked("interaction.held_item_changed");
            }
            if (!available.IsAvailable)
            {
                return new InteractionQuery(available, "drink.wipe");
            }
            if (bag.Recipe == null || !bag.Recipe.HasValidDurations)
            {
                return new InteractionQuery(Availability.Blocked("drink.recipe.not_configured"), "drink.wipe");
            }
            return new InteractionQuery(Availability.Available, "drink.wipe", InteractionKind.Hold, bag.Recipe.WipeHoldSeconds);
        }

        public void OnHoldStarted(InteractionContext context)
        {
            if (Query(context).Availability.IsAvailable)
            {
                _activeBag = (TeaBagItem)context.Hands.Current;
                _onStarted.Invoke();
            }
        }

        public void OnHoldCancelled(InteractionContext context)
        {
            if (_activeBag != null)
            {
                _activeBag = null;
                _onStopped.Invoke();
            }
        }

        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey ?? "interaction.item_not_held"));
                OnHoldCancelled(context);
                return;
            }
            Result result = ((TeaBagItem)context.Hands.Current).Wipe();
            OnHoldCancelled(context);
            if (!result.IsSuccess)
            {
                context.Events.Publish(new ActionBlocked(Id, result.ReasonKey));
            }
        }

        private void OnDisable()
        {
            if (_activeBag != null)
            {
                _activeBag = null;
                _onStopped.Invoke();
            }
        }
    }
}
