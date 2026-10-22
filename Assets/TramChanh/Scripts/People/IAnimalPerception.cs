using UnityEngine;

namespace TramChanh.People
{
    /// <summary>Turns the world into <see cref="AnimalSenses"/>. Tests replace it with fake data.</summary>
    public interface IAnimalPerception
    {
        /// <param name="position">Animal position.</param>
        /// <param name="forward">Animal facing (horizontal is enough).</param>
        AnimalSenses Sense(Vector3 position, Vector3 forward);
    }
}
