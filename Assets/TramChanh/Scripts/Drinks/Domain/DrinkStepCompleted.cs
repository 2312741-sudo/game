using TramChanh.Core;

namespace TramChanh.Drinks.Domain
{
    public readonly struct DrinkStepCompleted
    {
        public PreparationId PreparationId { get; }
        public TeaBagState State { get; }

        public DrinkStepCompleted(PreparationId preparationId, TeaBagState state)
        {
            PreparationId = preparationId;
            State = state;
        }
    }
}
