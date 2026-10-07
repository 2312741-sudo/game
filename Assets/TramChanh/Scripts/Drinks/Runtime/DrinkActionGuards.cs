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
            if (bag == null)
            {
                return Availability.Hidden;
            }
            Availability actor = Actor(context);
            if (!actor.IsAvailable)
            {
                return actor;
            }
            return bag.State == expected ? Availability.Available : Availability.Blocked(reason);
        }
    }
}
