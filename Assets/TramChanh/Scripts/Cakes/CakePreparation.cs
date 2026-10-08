using TramChanh.Content;
using System;
using TramChanh.Core;
using TramChanh.Orders;

namespace TramChanh.Cakes
{
    /// <summary>Strict cake sequence. Ready mutates silently for the order owner's atomic commit.</summary>
    public sealed class CakePreparation : IPreparedItem, IPreparationFeedback
    {
        private readonly IStallTicketQueue _tickets;
        private readonly OrderItemRef _binding;
        public CakeRecipe Recipe { get; }
        public PreparationId PreparationId => _binding.PreparationId;
        public CakeState State { get; private set; } = CakeState.Waiting;
        public BatterMeasurement Measurement { get; private set; }
        public float PouredMl { get; private set; }
        public double Doneness { get; private set; }
        public ItemKind Kind => ItemKind.Cake;
        public OrderItemRef BoundItem => _tickets.IsBound(_binding) ? _binding : default;
        public bool IsFinished => State == CakeState.Wrapped || State == CakeState.Ready;
        public int Quality => State == CakeState.Ruined ? 0 : 100 - (int)Math.Min(100d, Math.Ceiling(Math.Max(0d,
            Math.Abs((double)Measurement.DeviationMl) - Recipe.BatterToleranceMl) * Recipe.DeviationPenaltyPerMl));
        public string PreparationStateKey => "cake.state." + State.ToString().ToLowerInvariant();
        public string NextActionKey => State switch
        {
            CakeState.Waiting => "cake.fill", CakeState.BatterMeasured => "cake.pour", CakeState.BatterPoured => "grill.close",
            CakeState.Cooking => "cake.wait", CakeState.Cooked => "cake.flip", CakeState.Flipped => "cake.cut",
            CakeState.Cut => "cake.sauce", CakeState.Sauced => "cake.roll_vertical", CakeState.Rolled => "cake.wrap",
            CakeState.Wrapped => "ready.place", CakeState.Ruined => "cake.remove_burnt", _ => "cake.ready"
        };
        public CakePreparation(OrderItemRef binding, IStallTicketQueue tickets, CakeRecipe recipe)
        {
            if (!binding.IsValid) { throw new ArgumentException("Cake requires a claim.", nameof(binding)); }
            _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
            Recipe = recipe != null && recipe.IsConfigured ? recipe : throw new ArgumentException("Cake recipe is not configured.", nameof(recipe));
            _binding = binding;
            Measurement = new BatterMeasurement(0f, recipe.TargetBatterMl, recipe.BatterToleranceMl);
        }
        public Result Measure(float ml)
        {
            if (!BoundItem.IsValid) { return Result.Fail("ready.no_order"); }
            if (State != CakeState.Waiting && State != CakeState.BatterMeasured) { return Result.Fail("cake.already_poured"); }
            if (!CakeRecipe.Nonnegative(ml) || ml > MeasureCupDefinition.NominalCapacityMl) { return Result.Fail("cake.invalid_measurement"); }
            Measurement = new BatterMeasurement(ml, Recipe.TargetBatterMl, Recipe.BatterToleranceMl);
            State = ml > 0f ? CakeState.BatterMeasured : CakeState.Waiting;
            return Result.Success();
        }
        public Result CanPour()
        {
            if (!BoundItem.IsValid) { return Result.Fail("ready.no_order"); }
            if (State != CakeState.BatterMeasured || Measurement.MeasuredMl <= 0f) { return Result.Fail("cake.batter_not_measured"); }
            return Recipe.OutOfTolerancePolicy == BatterOutOfTolerancePolicy.BlockPour && Measurement.Result != BatterMeasureResult.WithinTolerance
                ? Result.Fail("cake.batter_out_of_tolerance") : Result.Success();
        }
        internal Result Pour()
        {
            Result result = CanPour(); if (!result.IsSuccess) { return result; }
            PouredMl = Measurement.MeasuredMl; State = CakeState.BatterPoured; return Result.Success();
        }
        internal Result StartCooking() => Step(CakeState.BatterPoured, CakeState.Cooking, "cake.batter_not_poured");
        internal void Heat(double delta)
        {
            if (State != CakeState.Cooking && State != CakeState.Cooked) { return; }
            Doneness += delta;
            if (Doneness >= Recipe.BurnThreshold) { State = CakeState.Ruined; }
            else if (Doneness >= Recipe.CookedThreshold) { State = CakeState.Cooked; }
        }
        internal Result Flip() => Step(CakeState.Cooked, CakeState.Flipped, "cake.not_cooked");
        public Result Cut() => Step(CakeState.Flipped, CakeState.Cut, "cake.need_flip_first");
        public Result Sauce(string sauce)
        {
            if (State != CakeState.Cut) { return Result.Fail("cake.need_cut_first"); }
            if (string.IsNullOrWhiteSpace(Recipe.Sauce)) { return Result.Fail("cake.sauce_unconfigured"); }
            if (!string.Equals(sauce, Recipe.Sauce, StringComparison.Ordinal)) { return Result.Fail("cake.wrong_sauce"); }
            return Step(CakeState.Cut, CakeState.Sauced, "cake.need_cut_first");
        }
        public Result RollVertically() => Step(CakeState.Sauced, CakeState.Rolled, "cake.need_sauce_first");
        public Result Wrap() => Step(CakeState.Rolled, CakeState.Wrapped, "cake.need_roll_first");
        public Result MarkReady() => Step(CakeState.Wrapped, CakeState.Ready, State == CakeState.Ready ? "ready.already_ready" : "ready.not_finished");
        internal void Delivered() { if (State == CakeState.Ready) { State = CakeState.Delivered; } }
        internal void UndoWrap() { if (State == CakeState.Wrapped) { State = CakeState.Rolled; } }
        private Result Step(CakeState required, CakeState next, string reason)
        {
            if (!BoundItem.IsValid) { return Result.Fail("ready.no_order"); }
            if (State != required) { return Result.Fail(reason); }
            State = next; return Result.Success();
        }
    }
}
