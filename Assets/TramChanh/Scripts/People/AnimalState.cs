namespace TramChanh.People
{
    /// <summary>The states of the street-animal finite state machine (see <see cref="AnimalBrain"/>).</summary>
    public enum AnimalState
    {
        /// <summary>Standing still for a short random time.</summary>
        Idle = 0,

        /// <summary>Walking to a random spot near home.</summary>
        Wander = 1,

        /// <summary>Walking toward something interesting (the player) and looking at it.</summary>
        Investigate = 2,

        /// <summary>Sitting near a table where somebody is eating.</summary>
        Beg = 3,

        /// <summary>Running away from a threat (the player or a motorbike that is too close).</summary>
        Flee = 4,

        /// <summary>Lying or sitting still for longer than Idle.</summary>
        Rest = 5,
    }
}
