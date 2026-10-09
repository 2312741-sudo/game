using System;
using TramChanh.Content;
using TramChanh.Core;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using TramChanh.Orders;
using UnityEngine;

namespace TramChanh.Drinks.Runtime
{
    public sealed class TeaBagItem : MonoBehaviour, IHoldable, IHeldItemAction, IPreparedItem, IInteractable
    {
        public const string DiscardOrphanPromptKey = "drink.discard_orphan";
        [SerializeField] private Transform _handGrip;
        [SerializeField] private Transform _placementPoint;
        [SerializeField] private TeaBagStateView _stateView;
        private DrinkPreparation _preparation;
        private IEventBus _events;
        public Transform HandGrip => _handGrip;
        public Transform PlacementPoint => _placementPoint;
        public DrinkPreparation Preparation => _preparation;
        public DrinkRecipe Recipe { get; private set; }
        public TeaBagState State => _preparation?.State ?? TeaBagState.Stored;
        public PreparationId PreparationId => _preparation?.PreparationId ?? default;
        public ItemKind Kind => ItemKind.Drink;
        public OrderItemRef BoundItem => _preparation?.BoundItem ?? default;
        public bool IsFinished => _preparation != null && _preparation.IsFinished;
        public int Quality => _preparation?.Quality ?? 0;
        public InteractableId Id => new InteractableId(PreparationId.Value);
        public Transform InteractionPoint => _handGrip;

        public void Initialize(DrinkPreparation preparation, DrinkRecipe recipe = null, IEventBus events = null)
        {
            _preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
            Recipe = recipe;
            _events = events;
            _stateView ??= GetComponent<TeaBagStateView>();
            _stateView?.Initialize(preparation, events);
        }

        public void OnPickedUp(IHeldItemSlot hands)
        {
            if (!ReferenceEquals(hands.Current, this) || State != TeaBagState.PickedUp)
            {
                throw new InvalidOperationException("Only a committed rack pickup can enter its receiving hand slot.");
            }
        }

        public void OnReleased() { }

        public InteractionQuery QueryUse(InteractionContext context)
        {
            if (!ReferenceEquals(context.Hands.Current, this))
            {
                return new InteractionQuery(Availability.Hidden, "drink.bag.open");
            }
            Availability available = DrinkActionGuards.Actor(context);
            // MAIN-102 (mirrors cake.discard_orphan): the bound order item is no longer live, so this bag can
            // never be placed Ready and would otherwise block the hands forever. Discard replaces the step action.
            if (_preparation != null && _preparation.IsOrphaned)
            {
                return new InteractionQuery(available, DiscardOrphanPromptKey);
            }
            if (!available.IsAvailable)
            {
                return new InteractionQuery(available, State == TeaBagState.PickedUp ? "drink.bag.open" : "drink.bag.shake");
            }
            if (State == TeaBagState.PickedUp)
            {
                return new InteractionQuery(Availability.Available, "drink.bag.open");
            }
            if (State == TeaBagState.IceAdded)
            {
                if (Recipe == null || !Recipe.HasValidDurations)
                {
                    return new InteractionQuery(Availability.Blocked("drink.recipe.not_configured"), "drink.bag.shake");
                }
                return new InteractionQuery(Availability.Available, "drink.bag.shake", InteractionKind.Hold, Recipe.ShakeHoldSeconds);
            }
            if (State == TeaBagState.Shaken || IsFinished)
            {
                return new InteractionQuery(Availability.Hidden, "drink.bag.shake");
            }
            return new InteractionQuery(Availability.Blocked("drink.need_ice_first"), "drink.bag.shake");
        }

        public void ExecuteUse(InteractionContext context)
        {
            InteractionQuery query = QueryUse(context);
            if (!query.Availability.IsAvailable)
            {
                context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey ?? "interaction.item_not_held"));
                _stateView?.SetShaking(false);
                return;
            }
            if (query.PromptKey == DiscardOrphanPromptKey)
            {
                Discard(context);
                return;
            }
            Result result = State == TeaBagState.PickedUp ? CompleteStep(_preparation.Open()) : CompleteStep(_preparation.Shake());
            _stateView?.SetShaking(false);
            _stateView?.Apply(State);
            if (!result.IsSuccess)
            {
                context.Events.Publish(new ActionBlocked(Id, result.ReasonKey));
            }
        }

        // Publishes nothing order-related: the order owner already released the item before the bag became orphaned.
        private void Discard(InteractionContext context)
        {
            if (!_preparation.Retire().IsSuccess)
            {
                return;
            }
            _stateView?.SetShaking(false);
            if (ReferenceEquals(context.Hands.Current, this))
            {
                context.Hands.TryRelease();
            }
            gameObject.SetActive(false);
            if (Application.isPlaying) { Destroy(gameObject); }
            else { DestroyImmediate(gameObject); }
        }

        public Result AddCoconutJelly() => CompleteStep(_preparation.AddCoconutJelly());
        public Result AddLemonJelly() => CompleteStep(_preparation.AddLemonJelly());
        public Result AddIce() => CompleteStep(_preparation.AddIce());
        public Result Wipe() => CompleteStep(_preparation.Wipe());
        private Result CompleteStep(Result result)
        {
            if (result.IsSuccess) { _events?.Publish(new DrinkStepCompleted(PreparationId, State)); }
            return result;
        }

        public Result MarkReady() => _preparation.MarkReady();

        public InteractionQuery Query(InteractionContext context) => QueryUse(context);
        public void Execute(InteractionContext context) => ExecuteUse(context);
        public void OnHoldStarted(InteractionContext context)
        {
            if (State == TeaBagState.IceAdded && QueryUse(context).Availability.IsAvailable)
            {
                _stateView?.SetShaking(true);
            }
        }
        public void OnHoldCancelled(InteractionContext context) => _stateView?.SetShaking(false);
    }
}
