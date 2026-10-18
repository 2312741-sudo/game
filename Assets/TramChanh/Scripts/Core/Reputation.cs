using System;

namespace TramChanh.Core
{
    /// <summary>
    /// Stall reputation in points, 100 per star, 0 to 5 stars. A day moves it by how many orders were served and how good
    /// they were. Higher stars bring a few more customers. Pure C#, saved through <see cref="ISettingsStore"/>.
    /// </summary>
    public sealed class Reputation
    {
        public const string PointsKey = "reputation.points";
        public const int PointsPerStar = 100;
        public const int MaxPoints = 5 * PointsPerStar;
        public const int StartPoints = 100;
        public const int MaxDayGain = 40;
        public const int MaxDayLoss = 30;

        public int Points { get; private set; } = StartPoints;
        public int Stars => Points / PointsPerStar;

        /// <summary>Customers come a little faster for every star: 4 percent shorter interval per star.</summary>
        public double ArrivalFactor => 1d - 0.04d * Stars;

        /// <summary>Score of a day, 0 to 100: half served share, half average quality. A day without orders has no score.</summary>
        public static int DayScore(DaySummary day)
        {
            if (day.Served + day.Failed == 0) { return -1; }
            return (int)Math.Round(0.5 * day.ServedPercent + 0.5 * day.AverageQuality);
        }

        /// <summary>Applies one finished day and returns the change in points (0 for a day without orders).</summary>
        public int ApplyDay(DaySummary day)
        {
            int score = DayScore(day);
            if (score < 0) { return 0; }
            int delta = (int)Math.Round((score - 60) * 0.8);
            delta = Math.Max(-MaxDayLoss, Math.Min(MaxDayGain, delta));
            return AddPoints(delta);
        }

        /// <summary>Adds or removes points inside 0..<see cref="MaxPoints"/>. Returns what really changed.</summary>
        public int AddPoints(int points)
        {
            int before = Points;
            Points = Math.Max(0, Math.Min(MaxPoints, Points + points));
            return Points - before;
        }

        public void Reset() { Points = StartPoints; }

        public static string StarText(int stars)
        {
            stars = Math.Max(0, Math.Min(5, stars));
            return new string('★', stars) + new string('☆', 5 - stars);
        }

        public void Load(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            Points = StartPoints;
            if (store.TryGetFloat(PointsKey, out float value) && !float.IsNaN(value) && !float.IsInfinity(value))
            {
                Points = Math.Max(0, Math.Min(MaxPoints, (int)Math.Round(value)));
            }
        }

        public void Save(ISettingsStore store)
        {
            if (store == null) { throw new ArgumentNullException(nameof(store)); }
            store.SetFloat(PointsKey, Points);
        }
    }
}
