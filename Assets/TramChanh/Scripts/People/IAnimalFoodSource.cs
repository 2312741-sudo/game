using UnityEngine;

namespace TramChanh.People
{
    /// <summary>Tells animals where somebody is eating. Implemented by the game code that knows which tables have a customer with food.</summary>
    public interface IAnimalFoodSource
    {
        /// <summary>The nearest eating spot within <paramref name="range"/> metres of <paramref name="from"/>, or false when there is none.</summary>
        bool TryFindFood(Vector3 from, float range, out Vector3 position);
    }
}
