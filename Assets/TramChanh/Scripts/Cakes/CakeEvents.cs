using TramChanh.Core;

namespace TramChanh.Cakes
{
    public readonly struct BatterMeasured
    {
        public PreparationId PreparationId { get; }
        public BatterMeasurement Measurement { get; }
        public BatterMeasured(PreparationId id, BatterMeasurement measurement) { PreparationId = id; Measurement = measurement; }
    }
    public readonly struct CakeStepCompleted
    {
        public PreparationId PreparationId { get; }
        public CakeState State { get; }
        public CakeStepCompleted(PreparationId id, CakeState state) { PreparationId = id; State = state; }
    }
}
