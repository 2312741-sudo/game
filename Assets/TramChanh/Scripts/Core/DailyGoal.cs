using System;

namespace TramChanh.Core
{
    /// <summary>The goal of one day: serve enough customers and keep most of them. Pure C#.</summary>
    public readonly struct DailyGoal
    {
        public const int MinServedPercent = 70;
        public const int BonusPoints = 20;

        public DailyGoal(int servedTarget, int bonusMoney)
        {
            ServedTarget = servedTarget;
            BonusMoney = bonusMoney;
        }

        public int ServedTarget { get; }
        public int BonusMoney { get; }

        /// <summary>6 customers on day 1, one more per day, up to 20, stretched by the difficulty.</summary>
        public static DailyGoal ForDay(int day, Difficulty difficulty)
        {
            int baseTarget = Math.Min(20, 6 + Math.Max(0, day - 1));
            int target = Math.Max(1, (int)Math.Round(baseTarget * Difficulties.Profile(difficulty).GoalScale));
            int bonus = Math.Min(100000, 20000 + 5000 * Math.Max(1, day));
            return new DailyGoal(target, bonus);
        }

        public bool IsMet(DaySummary day) => day.Served >= ServedTarget && day.ServedPercent >= MinServedPercent;

        public string Describe() => "Mục tiêu: phục vụ " + ServedTarget + " khách (tỉ lệ từ " + MinServedPercent + "%), thưởng " + PriceList.Format(BonusMoney);
    }
}
