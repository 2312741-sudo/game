using System;

namespace TramChanh.Core
{
    public enum NightEvent { Clear = 0, Rain = 1, HotNight = 2, PowerCut = 3, Festival = 4, Inspector = 5 }

    /// <summary>
    /// The event of each night, picked from the day number and a seed so a day always has the same event.
    /// Pure C#. Numbers are first guesses (GAMEPLAY_DESIGN.md, section 7).
    /// </summary>
    public static class NightPlan
    {
        public const double PowerCutFrom = 21.0;
        public const double PowerCutTo = 21.0 + 20d / 60d;
        public const double FestivalFrom = 20.0;
        public const double FestivalTo = 21.0;
        public const int InspectorReward = 50000;
        public const int InspectorFineMax = 20000;

        private static readonly NightEvent[] Events = { NightEvent.Clear, NightEvent.Rain, NightEvent.HotNight, NightEvent.PowerCut, NightEvent.Festival, NightEvent.Inspector };
        private static readonly int[] Weights = { 35, 20, 15, 10, 10, 10 };

        /// <summary>Day 1 and 2 are always clear. Later days pick by weight; the inspector only comes from day 4.</summary>
        public static NightEvent ForDay(int day, int seed)
        {
            if (day <= 2) { return NightEvent.Clear; }
            var random = new Random(unchecked(day * 7919 + seed * 104729));
            int total = 0;
            for (int i = 0; i < Events.Length; i++) { total += Allowed(Events[i], day) ? Weights[i] : 0; }
            int roll = random.Next(total);
            for (int i = 0; i < Events.Length; i++)
            {
                if (!Allowed(Events[i], day)) { continue; }
                if (roll < Weights[i]) { return Events[i]; }
                roll -= Weights[i];
            }
            return NightEvent.Clear;
        }

        private static bool Allowed(NightEvent night, int day) => night != NightEvent.Inspector || day >= 4;

        public static string Name(NightEvent night)
        {
            switch (night)
            {
                case NightEvent.Rain: return "Đêm mưa";
                case NightEvent.HotNight: return "Đêm nóng";
                case NightEvent.PowerCut: return "Mất điện lúc 21:00";
                case NightEvent.Festival: return "Hội phố";
                case NightEvent.Inspector: return "Thanh tra vệ sinh";
                default: return "Trời quang";
            }
        }

        public static string Announcement(NightEvent night)
        {
            switch (night)
            {
                case NightEvent.Rain: return "Mưa: khách ít hơn.";
                case NightEvent.HotNight: return "Nóng: khách đông hơn, đá dùng gấp đôi.";
                case NightEvent.PowerCut: return "21:00 đến 21:20 mất điện, không có khách.";
                case NightEvent.Festival: return "Hội phố 20:00 đến 21:00: khách ùa đến.";
                case NightEvent.Inspector: return "Thanh tra sẽ chấm chất lượng và tỉ lệ phục vụ.";
                default: return "Không có gì đặc biệt.";
            }
        }

        /// <summary>Multiplies the time between customers at this hour (below 1 is busier).</summary>
        public static double ArrivalScaleAt(NightEvent night, double hour)
        {
            switch (night)
            {
                case NightEvent.Rain: return 1.3;
                case NightEvent.HotNight: return 0.85;
                case NightEvent.Festival: return hour >= FestivalFrom && hour < FestivalTo ? 0.4 : 1.0;
                default: return 1.0;
            }
        }

        public static bool ArrivalsBlocked(NightEvent night, double hour) => night == NightEvent.PowerCut && hour >= PowerCutFrom && hour < PowerCutTo;

        /// <summary>Ice used per drink: double on a hot night.</summary>
        public static int IceUsePerDrink(NightEvent night) => night == NightEvent.HotNight ? 2 : 1;

        /// <summary>What the inspector leaves behind: money (negative is a fine, limited by the cash) and reputation points.</summary>
        public static void Inspect(DaySummary day, out int money, out int points)
        {
            bool passed = day.Served + day.Failed > 0 && day.ServedPercent >= 70 && day.AverageQuality >= 75;
            money = passed ? InspectorReward : -InspectorFineMax;
            points = passed ? 30 : -20;
        }
    }
}
