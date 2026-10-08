using System;
using TramChanh.Core;

namespace TramChanh.Cakes
{
    public sealed class GrillModel
    {
        private readonly IGameClock _clock;
        private readonly double _preheatSeconds;
        private double _last;
        private double _preheatedAt;
        public GrillState State { get; private set; }
        public CakePreparation CakeOnPlate { get; private set; }
        public GrillModel(IGameClock clock, double preheatSeconds)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (double.IsNaN(preheatSeconds) || double.IsInfinity(preheatSeconds) || preheatSeconds < 0d) { throw new ArgumentOutOfRangeException(nameof(preheatSeconds)); }
            _preheatSeconds = preheatSeconds;
        }
        public void PowerOn()
        {
            if (State != GrillState.Off) { return; }
            _last = _clock.Now; _preheatedAt = _last + _preheatSeconds; State = GrillState.Preheating;
        }
        public void Advance()
        {
            double now = _clock.Now; double elapsed = Math.Max(0d, now - _last); _last = now;
            if (_clock.IsPaused) { return; }
            if (State == GrillState.Preheating && now >= _preheatedAt) { State = GrillState.Ready; }
            if (CakeOnPlate == null) { return; }
            double heat = State == GrillState.Open ? CakeOnPlate.Recipe.OpenLidHeatFactor : 1d;
            CakeOnPlate.Heat(elapsed * heat);
            if (State != GrillState.Open)
            {
                if (CakeOnPlate.State == CakeState.Ruined) { State = GrillState.Overcooked; }
                else if (CakeOnPlate.State == CakeState.Cooked) { State = GrillState.Finished; }
            }
        }
        public Result OpenLid()
        {
            Advance();
            if (State == GrillState.Off || State == GrillState.Preheating) { return Result.Fail("grill.preheating"); }
            if (State == GrillState.Open) { return Result.Fail("grill.already_open"); }
            State = GrillState.Open; return Result.Success();
        }
        public Result CanPour(CakePreparation cake)
        {
            if (State != GrillState.Open) { return Result.Fail("grill.lid_closed"); }
            if (CakeOnPlate != null) { return Result.Fail("grill.occupied"); }
            return cake == null ? Result.Fail("cake.batter_not_measured") : cake.CanPour();
        }
        public Result Pour(CakePreparation cake)
        {
            Advance(); Result guard = CanPour(cake); if (!guard.IsSuccess) { return guard; }
            Result result = cake.Pour(); if (result.IsSuccess) { CakeOnPlate = cake; } return result;
        }
        public Result CloseLid()
        {
            Advance(); if (State != GrillState.Open) { return Result.Fail("grill.lid_closed"); }
            if (CakeOnPlate == null) { State = GrillState.Ready; return Result.Success(); }
            if (CakeOnPlate.State == CakeState.BatterPoured)
            {
                Result result = CakeOnPlate.StartCooking(); if (!result.IsSuccess) { return result; }
            }
            State = CakeOnPlate.State == CakeState.Ruined ? GrillState.Overcooked
                : CakeOnPlate.State == CakeState.Cooked ? GrillState.Finished : GrillState.Cooking;
            return Result.Success();
        }
        public Result CanFlip()
        {
            if (State != GrillState.Open) { return Result.Fail("grill.lid_closed"); }
            return CakeOnPlate != null && CakeOnPlate.State == CakeState.Cooked ? Result.Success() : Result.Fail("cake.not_cooked");
        }
        public Result Flip(IFlipAction action)
        {
            Advance(); Result guard = CanFlip(); if (!guard.IsSuccess) { return guard; }
            Availability available = action.CanFlip(this, CakeOnPlate); if (!available.IsAvailable) { return Result.Fail(available.ReasonKey ?? "cake.flip_unavailable"); }
            Result result = CakeOnPlate.Flip(); if (result.IsSuccess) { CakeOnPlate = null; } return result;
        }
        internal void ClearInvalidated()
        {
            if (CakeOnPlate != null && !CakeOnPlate.BoundItem.IsValid)
            { CakeOnPlate = null; if (State != GrillState.Open) { State = GrillState.Ready; } }
        }
        public Result RemoveBurnt()
        {
            if (State != GrillState.Open) { return Result.Fail("grill.lid_closed"); }
            if (CakeOnPlate == null || CakeOnPlate.State != CakeState.Ruined) { return Result.Fail("cake.not_burnt"); }
            CakeOnPlate = null; return Result.Success();
        }
    }
}
