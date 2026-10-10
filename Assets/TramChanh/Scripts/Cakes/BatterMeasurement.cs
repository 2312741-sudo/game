using System;

namespace TramChanh.Cakes
{
    public readonly struct BatterMeasurement
    {
        public float MeasuredMl { get; }
        public float TargetMl { get; }
        public float ToleranceMl { get; }
        public float DeviationMl => MeasuredMl - TargetMl;
        public BatterMeasureResult Result => Math.Abs(DeviationMl) <= ToleranceMl ? BatterMeasureResult.WithinTolerance
            : DeviationMl < 0f ? BatterMeasureResult.Under : BatterMeasureResult.Over;
        public BatterMeasurement(float measured, float target, float tolerance)
        {
            MeasuredMl = measured; TargetMl = target; ToleranceMl = tolerance;
        }
    }
}
