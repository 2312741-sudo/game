using TramChanh.Cakes;
using TramChanh.Core;

namespace TramChanh.Effects
{
    /// <summary>
    /// All decisions of the night effects in plain C#: which night event wants rain, how fast the rain fades in and out,
    /// when the steam runs, which effect groups are switched on. <see cref="NightEffectsController"/> only reads these
    /// numbers and applies them to the particle systems. No Unity types, no clock: time is passed in as seconds.
    /// </summary>
    public sealed class NightEffectsModel
    {
        /// <summary>Seconds for the rain to grow from nothing to full (a night starts with a light shower).</summary>
        public const float RainFadeInSeconds = 4f;
        /// <summary>Seconds for the rain to stop completely.</summary>
        public const float RainFadeOutSeconds = 3f;
        public const float SteamFadeInSeconds = 1.2f;
        public const float SteamFadeOutSeconds = 2f;

        private readonly GrillSteamTracker _grill = new GrillSteamTracker();
        private EffectKinds _enabled = EffectKinds.All;
        private bool _forceRain;
        private NightEvent _night = NightEvent.Clear;

        /// <summary>0 (dry) to 1 (full rain). Scales the emission rate of the rain system.</summary>
        public SmoothedLevel Rain { get; } = new SmoothedLevel(RainFadeInSeconds, RainFadeOutSeconds);
        /// <summary>Steady steam above the grill while a cake is on it.</summary>
        public SmoothedLevel GrillSteam { get; } = new SmoothedLevel(SteamFadeInSeconds, SteamFadeOutSeconds);
        /// <summary>Steam over the hot tea (always on while steam is enabled).</summary>
        public SmoothedLevel CupSteam { get; } = new SmoothedLevel(SteamFadeInSeconds, SteamFadeOutSeconds);

        public NightEvent Night => _night;
        public bool ForceRain => _forceRain;
        public int ActiveCakesOnGrill => _grill.ActiveCakes;

        /// <summary>True while the rain system must keep emitting (target above zero or still fading out).</summary>
        public bool RainShouldEmit => Rain.Current > 0f || Rain.Target > 0f;

        /// <summary>Which night events bring rain. Only the rain night.</summary>
        public static bool EventHasRain(NightEvent night)
        {
            return night == NightEvent.Rain;
        }

        public void SetNight(NightEvent night)
        {
            _night = night;
            RefreshTargets();
        }

        /// <summary>Demo switch: rain whatever the night event is.</summary>
        public void SetForceRain(bool force)
        {
            _forceRain = force;
            RefreshTargets();
        }

        public void SetEnabled(EffectKinds kind, bool on)
        {
            _enabled = on ? (_enabled | kind) : (_enabled & ~kind);
            RefreshTargets();
        }

        public bool IsEnabled(EffectKinds kind)
        {
            return (_enabled & kind) == kind && kind != EffectKinds.None;
        }

        /// <summary>Feeds one cake step. Returns the number of steam particles to puff now (0 when steam is off or the step has no puff).</summary>
        public int OnCakeStep(PreparationId id, CakeState state)
        {
            int burst = _grill.OnStep(id, state);
            RefreshTargets();
            return IsEnabled(EffectKinds.Steam) ? burst : 0;
        }

        /// <summary>A new night starts: forget the cakes of the previous one.</summary>
        public void ResetNight()
        {
            _grill.Clear();
            RefreshTargets();
        }

        public void Step(float deltaSeconds)
        {
            RefreshTargets();
            Rain.Step(deltaSeconds);
            GrillSteam.Step(deltaSeconds);
            CupSteam.Step(deltaSeconds);
        }

        private void RefreshTargets()
        {
            bool rain = IsEnabled(EffectKinds.Rain) && (_forceRain || EventHasRain(_night));
            Rain.SetTarget(rain ? 1f : 0f);
            bool steam = IsEnabled(EffectKinds.Steam);
            GrillSteam.SetTarget(steam && _grill.AnyCakeOnGrill ? 1f : 0f);
            CupSteam.SetTarget(steam ? 1f : 0f);
        }
    }
}
