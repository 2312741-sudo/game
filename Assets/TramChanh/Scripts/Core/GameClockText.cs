using System;

namespace TramChanh.Core
{
    /// <summary>Text for the on-screen game clock: "18:42  Mở cửa" while open, "03:10  Đóng cửa (tua nhanh)" while closed.</summary>
    public static class GameClockText
    {
        public const string Open = "Mở cửa";
        public const string Closed = "Đóng cửa (tua nhanh)";

        /// <summary>Hour of day in [0, 24); anything else is clamped into range. Seconds are dropped, never rounded up to the next hour.</summary>
        public static string Format(double hourOfDay, bool open)
        {
            if (double.IsNaN(hourOfDay) || double.IsInfinity(hourOfDay)) { hourOfDay = 0d; }
            int totalMinutes = (int)Math.Floor(hourOfDay * 60d);
            totalMinutes = ((totalMinutes % 1440) + 1440) % 1440;
            return (totalMinutes / 60).ToString("00") + ":" + (totalMinutes % 60).ToString("00") + "  " + (open ? Open : Closed);
        }

        /// <summary>The whole minute of the day (0 to 1439), used to refresh the label only when the shown text can change.</summary>
        public static int MinuteOfDay(double hourOfDay)
        {
            if (double.IsNaN(hourOfDay) || double.IsInfinity(hourOfDay)) { return 0; }
            int totalMinutes = (int)Math.Floor(hourOfDay * 60d);
            return ((totalMinutes % 1440) + 1440) % 1440;
        }
    }
}
