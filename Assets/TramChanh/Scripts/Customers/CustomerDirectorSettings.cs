using System;
using TramChanh.Core.Provisional;
using UnityEngine;

namespace TramChanh.Customers
{
    /// <summary>Provisional tuning for customer arrivals (DEC-010). Values live in data, not in gameplay code.</summary>
    [Serializable]
    public sealed class CustomerDirectorSettings
    {
        [SerializeField, Tbd("DEC-010", "Concurrent dine-in customers; clamped to 0..seat count.")] private int _maxActiveCustomers = 3;
        [SerializeField, Tbd("DEC-010", "Game-clock seconds between arrivals.")] private float _arrivalIntervalSeconds = 6f;
        [SerializeField, Tbd("DEC-010", "Seconds a table stays occupied after its order ends.")] private float _tableClearSeconds = 2f;
        [SerializeField, Tbd("DEC-010", "Relative weight of drink-only orders.")] private float _drinkWeight = 1f;
        [SerializeField, Tbd("DEC-010", "Relative weight of cake-only orders.")] private float _cakeWeight = 1f;
        [SerializeField, Tbd("DEC-010", "Relative weight of drink + cake orders.")] private float _mixedWeight = 1f;
        [SerializeField, Tbd("DEC-010", "Opt-in walking mode: reserve the seat, walk, order on arrival, eat, walk away. Off = legacy instant seating.")] private bool _customersWalk;
        [SerializeField, Tbd("DEC-010", "Walking mode: game-clock seconds a customer eats after the order ends.")] private float _eatSeconds = 4f;
        [SerializeField, Tbd("DEC-010", "Walking mode: arrive anyway if the presenter never reports arrival.")] private float _arrivalTimeoutSeconds = 30f;
        [SerializeField, Tbd("DEC-010", "Walking mode: depart anyway if the presenter never reports departure.")] private float _departureTimeoutSeconds = 20f;

        public CustomerDirectorSettings() { }

        public CustomerDirectorSettings(int maxActiveCustomers, float arrivalIntervalSeconds = 6f, float tableClearSeconds = 2f,
            float drinkWeight = 1f, float cakeWeight = 1f, float mixedWeight = 1f,
            bool customersWalk = false, float eatSeconds = 4f, float arrivalTimeoutSeconds = 30f, float departureTimeoutSeconds = 20f)
        {
            _maxActiveCustomers = maxActiveCustomers;
            _arrivalIntervalSeconds = arrivalIntervalSeconds;
            _tableClearSeconds = tableClearSeconds;
            _drinkWeight = drinkWeight;
            _cakeWeight = cakeWeight;
            _mixedWeight = mixedWeight;
            _customersWalk = customersWalk;
            _eatSeconds = eatSeconds;
            _arrivalTimeoutSeconds = arrivalTimeoutSeconds;
            _departureTimeoutSeconds = departureTimeoutSeconds;
        }

        /// <summary>Never negative; the director additionally clamps it to its seat count.</summary>
        public int MaxActiveCustomers => Math.Max(0, _maxActiveCustomers);
        public float ArrivalIntervalSeconds => NonNegative(_arrivalIntervalSeconds);
        public float TableClearSeconds => NonNegative(_tableClearSeconds);
        public float DrinkWeight => NonNegative(_drinkWeight);
        public float CakeWeight => NonNegative(_cakeWeight);
        public float MixedWeight => NonNegative(_mixedWeight);

        /// <summary>False (default): legacy instant seating, the order is requested at spawn. True: walking mode.</summary>
        public bool CustomersWalk => _customersWalk;
        /// <summary>Walking mode: seconds between the order ending (CustomerEating) and CustomerLeaving.</summary>
        public float EatSeconds => NonNegative(_eatSeconds);
        /// <summary>Walking mode: seconds after CustomerArriving before the director arrives the customer itself.</summary>
        public float ArrivalTimeoutSeconds => NonNegative(_arrivalTimeoutSeconds);
        /// <summary>Walking mode: seconds after CustomerLeaving before the director departs the customer itself.</summary>
        public float DepartureTimeoutSeconds => NonNegative(_departureTimeoutSeconds);

        private static float NonNegative(float value) => float.IsNaN(value) || value < 0f ? 0f : value;
    }
}
