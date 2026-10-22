using System;
using UnityEngine;

namespace TramChanh.Core
{
    /// <summary>
    /// Announces that a held product is about to be thrown away (a ruined drink bag, a burnt cake). Drinks and Cakes raise it just
    /// before they destroy the object; whoever listens (the physics props, an effect) can copy it first. Static on purpose: the item
    /// classes must not depend on the listeners' assemblies.
    /// </summary>
    public static class ItemDiscardHook
    {
        public static event Action<GameObject> Discarded;

        public static void Raise(GameObject item)
        {
            if (item != null) { Discarded?.Invoke(item); }
        }
    }
}
