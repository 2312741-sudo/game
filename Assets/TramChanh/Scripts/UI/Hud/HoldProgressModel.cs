using System;
using TramChanh.Interaction;

namespace TramChanh.UI.Hud
{
    /// <summary>
    /// Caption for the prompt's hold feedback. Hold actions expose 0..1 progress through
    /// <see cref="InteractionPromptChanged"/>; Continuous actions expose no progress yet, so they get a
    /// "hold, release to stop" hint instead of a bar.
    /// </summary>
    public static class HoldProgressModel
    {
        public static bool ShowsBar(InteractionQuery query) => query.Kind == InteractionKind.Hold && query.Availability.IsAvailable;

        public static int Percent(float progress)
        {
            if (float.IsNaN(progress) || progress <= 0f) { return 0; }
            return progress >= 1f ? 100 : (int)Math.Floor(progress * 100f);
        }

        public static HudText Caption(InteractionQuery query, float progress)
        {
            if (!query.Availability.IsAvailable) { return HudText.Empty; }
            if (query.Kind == InteractionKind.Hold) { return HudText.Of(HudKeys.HoldProgress, Percent(progress)); }
            if (query.Kind == InteractionKind.Continuous) { return HudText.Of(HudKeys.HoldContinuous); }
            return HudText.Empty;
        }
    }
}
