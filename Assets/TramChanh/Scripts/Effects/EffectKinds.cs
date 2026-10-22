using System;

namespace TramChanh.Effects
{
    /// <summary>The four effect groups that can be switched on and off one by one (demo, quality fallback).</summary>
    [Flags]
    public enum EffectKinds
    {
        None = 0,
        Rain = 1,
        Steam = 2,
        Taillights = 4,
        Wire = 8,
        All = Rain | Steam | Taillights | Wire
    }
}
