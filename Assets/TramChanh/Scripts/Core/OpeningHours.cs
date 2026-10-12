using System;

namespace TramChanh.Core
{
    /// <summary>
    /// Opening-hours policy for the stall, in in-game hours of day.
    ///
    /// Boundary rule: the window is half-open. It is open at <c>OpenHour</c> (inclusive)
    /// and closed at <c>CloseHour</c> (exclusive). The stall accepts customers from
    /// 18:00 up to but not including 24:00, so 18.0 is open and 24.0 is closed.
    /// </summary>
    public sealed class OpeningHours
    {
        /// <summary>The stall's trading window: 18:00 (inclusive) to 24:00 (exclusive).</summary>
        public static readonly OpeningHours Stall = new OpeningHours(18.0, 24.0);

        public OpeningHours(double openHour, double closeHour)
        {
            if (double.IsNaN(openHour) || double.IsInfinity(openHour) || openHour < 0 || openHour >= 24.0)
            {
                throw new ArgumentOutOfRangeException(nameof(openHour), "Open hour must be in [0, 24).");
            }
            if (double.IsNaN(closeHour) || double.IsInfinity(closeHour) || closeHour <= openHour || closeHour > 24.0)
            {
                throw new ArgumentOutOfRangeException(nameof(closeHour), "Close hour must be after the open hour and at most 24.");
            }
            OpenHour = openHour;
            CloseHour = closeHour;
        }

        /// <summary>First hour of the day that accepts customers (inclusive).</summary>
        public double OpenHour { get; }

        /// <summary>First hour of the day that no longer accepts customers (exclusive).</summary>
        public double CloseHour { get; }

        /// <summary>True when <paramref name="hourOfDay"/> is in [OpenHour, CloseHour).</summary>
        /// <param name="hourOfDay">Hour of day in [0, 24], as from <see cref="DayCycle.HourOfDay"/>. 24 is treated as midnight-close.</param>
        public bool IsOpen(double hourOfDay)
        {
            if (double.IsNaN(hourOfDay) || double.IsInfinity(hourOfDay) || hourOfDay < 0 || hourOfDay > 24.0)
            {
                throw new ArgumentOutOfRangeException(nameof(hourOfDay), "Hour of day must be in [0, 24].");
            }
            return hourOfDay >= OpenHour && hourOfDay < CloseHour;
        }
    }
}
