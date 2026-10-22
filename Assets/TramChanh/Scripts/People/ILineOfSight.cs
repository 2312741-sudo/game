using UnityEngine;

namespace TramChanh.People
{
    /// <summary>Answers whether a wall (layer Environment) hides one point from another. The Unity version uses a raycast.</summary>
    public interface ILineOfSight
    {
        bool IsBlocked(Vector3 from, Vector3 to);
    }
}
