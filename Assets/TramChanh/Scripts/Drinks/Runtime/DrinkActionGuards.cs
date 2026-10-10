using TramChanh.Core;
using TramChanh.Interaction;

namespace TramChanh.Drinks.Runtime
{
    internal static class DrinkActionGuards
    {
        public static Availability Actor(InteractionContext context)
        {
            if ((context.Role & ActorRole.Stall) == 0)
            {
                return Availability.Blocked("interaction.stall_role_required");
            }
            return context.Clock.IsPaused ? Availability.Blocked("interaction.paused") : Availability.Available;
        }

        public static Availability Step(InteractionContext context, TeaBagItem bag, Domain.TeaBagState expected, string reason)
        {
            // A step the held bag has already completed is not offered again. Blocking it with the
            // "need previous step" reason would teach a step the bag already passed (e.g. a wiped bag
            // at the coconut bin reading "Open the bag first"), so it is Hidden like a finished held action.
            if (bag == null || (int)bag.State > (int)expected)
            {
                return Availability.Hidden;
            }
            Availability actor = Actor(context);
            if (!actor.IsAvailable)
            {
                return actor;
            }
            // MAIN-102: an orphaned bag (its order failed) can never reach Ready; stop further steps and point
            // the player at the held discard instead of letting them finish an unplaceable drink.
            if (bag.Preparation != null && bag.Preparation.IsOrphaned)
            {
                return Availability.Blocked("ready.no_order");
            }
            return bag.State == expected ? Availability.Available : Availability.Blocked(reason);
        }
    }
}
