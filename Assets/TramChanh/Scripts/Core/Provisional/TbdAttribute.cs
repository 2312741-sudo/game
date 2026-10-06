using System;
using UnityEngine;

namespace TramChanh.Core.Provisional
{
    /// <summary>
    /// Marks a serialized field whose value is a provisional placeholder for an
    /// unconfirmed real-world value (batter amount, cook time, burn time, topping
    /// quantities, station positions, ...). Such values must stay in data
    /// (ScriptableObjects, prefabs, scenes) and never be copied into gameplay code
    /// as constants. See ARCHITECTURE.md §3 "Provisional data".
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class TbdAttribute : PropertyAttribute
    {
        public TbdAttribute(string decisionId, string note = "")
        {
            DecisionId = decisionId;
            Note = note;
        }

        /// <summary>Decision register id that will confirm the value, e.g. "DEC-007".</summary>
        public string DecisionId { get; }

        public string Note { get; }
    }
}
