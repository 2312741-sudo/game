using System;
using System.Collections.Generic;
using TramChanh.People;
using UnityEngine;

namespace TramChanh.App.People
{
    /// <summary>
    /// What the street animals share: the waypoint graph, the player, motorbikes, the eating spots and the debug switch.
    /// The bootstrap creates one, fills what it has and calls <see cref="StreetAnimalWander.Bind"/> on every animal.
    /// Every part is optional: without a graph animals walk straight, without a player they use the main camera,
    /// without a food source nobody begs.
    /// </summary>
    public sealed class StreetAnimalContext : IAnimalThreatSource, IAnimalFoodSource
    {
        private static readonly string[] DefaultLanes = { PeopleController.PedestrianLaneNorth, PeopleController.PedestrianLaneSouth };
        private readonly List<StreetAnimalWander> _animals = new List<StreetAnimalWander>();
        private Camera _camera;

        /// <summary>Waypoint graph from <see cref="PeopleController.Graph"/>; null means straight-line walking.</summary>
        public WaypointGraph Graph { get; set; }

        /// <summary>Lanes of the graph animals may follow (sidewalks). Null or empty means every node.</summary>
        public IReadOnlyList<string> Lanes { get; set; } = DefaultLanes;

        /// <summary>The player; when null the main camera stands in for it.</summary>
        public Transform Player { get; set; }

        /// <summary>Where somebody is eating. Implemented by game code (see Docs/Design/COURSE_AI_ANIMALS.md); null means no food.</summary>
        public IAnimalFoodSource Food { get; set; }

        /// <summary>Optional: appends motorbikes (danger above 1) to the list. Called often, so it must not allocate.</summary>
        public Action<List<AnimalThreat>> VehicleThreats { get; set; }

        /// <summary>Demo switch: floating state text above every animal.</summary>
        public bool ShowStateLabels { get; set; }

        public ILineOfSight LineOfSight { get; } = new PhysicsLineOfSight();

        public IReadOnlyList<StreetAnimalWander> Animals => _animals;

        public void Register(StreetAnimalWander animal)
        {
            if (animal != null && !_animals.Contains(animal)) { _animals.Add(animal); }
        }

        public void Unregister(StreetAnimalWander animal)
        {
            _animals.Remove(animal);
        }

        /// <summary>The player's position, or the main camera's. False when neither exists (animals then use full detail).</summary>
        public bool TryGetViewer(out Vector3 position)
        {
            if (Player != null) { position = Player.position; return true; }
            if (_camera == null) { _camera = Camera.main; }
            if (_camera != null) { position = _camera.transform.position; return true; }
            position = Vector3.zero;
            return false;
        }

        public void Collect(List<AnimalThreat> into)
        {
            if (TryGetViewer(out Vector3 viewer)) { into.Add(new AnimalThreat(viewer, 1f, true)); }
            if (VehicleThreats != null) { VehicleThreats(into); }
        }

        public bool TryFindFood(Vector3 from, float range, out Vector3 position)
        {
            if (Food != null) { return Food.TryFindFood(from, range, out position); }
            position = Vector3.zero;
            return false;
        }
    }
}
