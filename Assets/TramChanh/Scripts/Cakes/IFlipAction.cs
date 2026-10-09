using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Cakes
{
    /// <summary>DEC-013: physical flip is replaceable independently of the fixed workflow state.</summary>
    public interface IFlipAction
    {
        Availability CanFlip(GrillModel grill, CakePreparation cake);
        Transform Destination { get; }
        void Present(CakeItem item);
    }
}
