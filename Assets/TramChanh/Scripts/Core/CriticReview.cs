namespace TramChanh.Core
{
    /// <summary>What a food critic leaves behind: a money multiplier on the order and a change of reputation points.</summary>
    public readonly struct CriticVerdict
    {
        public CriticVerdict(double moneyMultiplier, int reputationPoints, string text)
        {
            MoneyMultiplier = moneyMultiplier;
            ReputationPoints = reputationPoints;
            Text = text;
        }

        public double MoneyMultiplier { get; }
        public int ReputationPoints { get; }
        public string Text { get; }
        public bool IsGood => ReputationPoints > 0;
    }

    /// <summary>The critic judges quality hard and has no tolerance for waiting (GAMEPLAY_DESIGN.md, section 3). Pure C#.</summary>
    public static class CriticReview
    {
        public const double FastLeft = 2d / 3d;
        public const double SlowLeft = 1d / 3d;

        /// <summary>
        /// Perfect and quick: big tip and a good review. Poor, or kept waiting for most of the patience: half pay and a bad review.
        /// Anything in between: normal pay and a small plus.
        /// </summary>
        public static CriticVerdict Judge(QualityTier tier, double patienceLeft)
        {
            if (tier == QualityTier.Perfect && patienceLeft >= FastLeft)
            {
                return new CriticVerdict(2d, 25, "Nhà phê bình rất hài lòng: tiền boa lớn, +25 uy tín.");
            }
            if (tier == QualityTier.Poor || patienceLeft < SlowLeft)
            {
                return new CriticVerdict(0.5d, -20, "Nhà phê bình chê: trả nửa tiền, -20 uy tín.");
            }
            return new CriticVerdict(1d, 5, "Nhà phê bình khá hài lòng: +5 uy tín.");
        }

        /// <summary>A critic that left without being served always leaves a bad review.</summary>
        public static CriticVerdict Walkout() => new CriticVerdict(0d, -15, "Nhà phê bình bỏ về: -15 uy tín.");
    }
}
