using System.Collections.Generic;
using TramChanh.Cakes;
using TramChanh.Core;

namespace TramChanh.Effects
{
    /// <summary>
    /// Follows the cake steps published on the event bus and answers two questions for the grill steam:
    /// is any cake on the grill (steady steam), and does this step deserve a one-off puff (burst).
    /// Pure C#: the steam view only reads the numbers.
    /// </summary>
    public sealed class GrillSteamTracker
    {
        /// <summary>Puff size when the cake has just been flipped (the moment the owner asked for).</summary>
        public const int FlipBurstCount = 14;
        /// <summary>Smaller puff when the cake is done cooking and the lid is lifted.</summary>
        public const int CookedBurstCount = 6;

        private readonly Dictionary<int, CakeState> _onGrill = new Dictionary<int, CakeState>();

        /// <summary>Number of preparations whose last reported state is on the grill.</summary>
        public int ActiveCakes => _onGrill.Count;

        public bool AnyCakeOnGrill => _onGrill.Count > 0;

        /// <summary>True for the states in which the cake is still on the hot grill plate.</summary>
        public static bool IsOnGrill(CakeState state)
        {
            return state == CakeState.Cooking || state == CakeState.Cooked || state == CakeState.Flipped;
        }

        /// <summary>Puff size for a step, 0 when the step makes no puff.</summary>
        public static int BurstFor(CakeState state)
        {
            switch (state)
            {
                case CakeState.Flipped: return FlipBurstCount;
                case CakeState.Cooked: return CookedBurstCount;
                default: return 0;
            }
        }

        /// <summary>Records one step and returns the burst size to emit now (0 for none).</summary>
        public int OnStep(PreparationId id, CakeState state)
        {
            if (!id.IsValid)
            {
                return 0;
            }
            if (IsOnGrill(state))
            {
                _onGrill[id.Value] = state;
            }
            else
            {
                _onGrill.Remove(id.Value);
            }
            return BurstFor(state);
        }

        public void Clear()
        {
            _onGrill.Clear();
        }
    }
}
