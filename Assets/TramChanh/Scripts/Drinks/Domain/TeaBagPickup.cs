using TramChanh.Core;

namespace TramChanh.Drinks.Domain
{
    public sealed class TeaBagPickup
    {
        public TeaBagState State { get; private set; } = TeaBagState.Stored;

        public Result TryPickUp()
        {
            if (State != TeaBagState.Stored)
            {
                return Result.Fail("drink.bag.not_stored");
            }
            State = TeaBagState.PickedUp;
            return Result.Success();
        }
    }
}
