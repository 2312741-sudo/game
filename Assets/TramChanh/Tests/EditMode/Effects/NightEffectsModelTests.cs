using NUnit.Framework;
using TramChanh.Cakes;
using TramChanh.Core;
using TramChanh.Effects;

namespace TramChanh.Tests.EditMode.Effects
{
    public sealed class NightEffectsModelTests
    {
        [Test]
        public void FX_RAIN_001_OnlyRainNightWantsRain()
        {
            foreach (NightEvent night in new[] { NightEvent.Clear, NightEvent.HotNight, NightEvent.PowerCut, NightEvent.Festival, NightEvent.Inspector })
            {
                Assert.IsFalse(NightEffectsModel.EventHasRain(night), night.ToString());
            }
            Assert.IsTrue(NightEffectsModel.EventHasRain(NightEvent.Rain));
        }

        [Test]
        public void FX_RAIN_002_RainFadesInGraduallyAndStopsEmittingOnlyWhenDry()
        {
            var model = new NightEffectsModel();
            Assert.IsFalse(model.RainShouldEmit);
            model.SetNight(NightEvent.Rain);
            Assert.IsTrue(model.RainShouldEmit);
            model.Step(NightEffectsModel.RainFadeInSeconds * 0.5f);
            Assert.AreEqual(0.5f, model.Rain.Current, 1e-4f);
            model.Step(NightEffectsModel.RainFadeInSeconds);
            Assert.AreEqual(1f, model.Rain.Current, 1e-6f);

            model.SetNight(NightEvent.Clear);
            Assert.IsTrue(model.RainShouldEmit, "still fading out");
            model.Step(NightEffectsModel.RainFadeOutSeconds * 0.5f);
            Assert.AreEqual(0.5f, model.Rain.Current, 1e-4f);
            Assert.IsTrue(model.RainShouldEmit);
            model.Step(NightEffectsModel.RainFadeOutSeconds);
            Assert.AreEqual(0f, model.Rain.Current, 1e-6f);
            Assert.IsFalse(model.RainShouldEmit);
        }

        [Test]
        public void FX_RAIN_003_DemoSwitchesOverrideTheEvent()
        {
            var model = new NightEffectsModel();
            model.SetForceRain(true);
            Assert.AreEqual(1f, model.Rain.Target);
            model.SetEnabled(EffectKinds.Rain, false);
            Assert.AreEqual(0f, model.Rain.Target);
            model.SetNight(NightEvent.Rain);
            Assert.AreEqual(0f, model.Rain.Target, "disabled group wins over the event");
            model.SetEnabled(EffectKinds.Rain, true);
            Assert.AreEqual(1f, model.Rain.Target);
        }

        [Test]
        public void FX_STEAM_001_FlipMakesABurstAndKeepsGrillSteamOn()
        {
            var model = new NightEffectsModel();
            var id = new PreparationId(1);
            Assert.AreEqual(0, model.OnCakeStep(id, CakeState.Cooking));
            Assert.AreEqual(1f, model.GrillSteam.Target);
            Assert.AreEqual(GrillSteamTracker.FlipBurstCount, model.OnCakeStep(id, CakeState.Flipped));
            Assert.AreEqual(1, model.ActiveCakesOnGrill);
            Assert.AreEqual(0, model.OnCakeStep(id, CakeState.Cut));
            Assert.AreEqual(0, model.ActiveCakesOnGrill);
            Assert.AreEqual(0f, model.GrillSteam.Target);
        }

        [Test]
        public void FX_STEAM_002_TwoCakesNeedBothToLeaveTheGrill()
        {
            var model = new NightEffectsModel();
            model.OnCakeStep(new PreparationId(1), CakeState.Cooking);
            model.OnCakeStep(new PreparationId(2), CakeState.Cooking);
            model.OnCakeStep(new PreparationId(1), CakeState.Ruined);
            Assert.AreEqual(1f, model.GrillSteam.Target);
            model.OnCakeStep(new PreparationId(2), CakeState.Delivered);
            Assert.AreEqual(0f, model.GrillSteam.Target);
        }

        [Test]
        public void FX_STEAM_003_DisabledSteamGivesNoBurst()
        {
            var model = new NightEffectsModel();
            model.SetEnabled(EffectKinds.Steam, false);
            Assert.AreEqual(0, model.OnCakeStep(new PreparationId(1), CakeState.Flipped));
            Assert.AreEqual(0f, model.CupSteam.Target);
        }

        [Test]
        public void FX_STEAM_004_InvalidIdIsIgnored()
        {
            var tracker = new GrillSteamTracker();
            Assert.AreEqual(0, tracker.OnStep(default(PreparationId), CakeState.Flipped));
            Assert.AreEqual(0, tracker.ActiveCakes);
        }

        [Test]
        public void FX_LEVEL_001_ZeroDurationJumpsAndNegativeStepIsIgnored()
        {
            var level = new SmoothedLevel(0f, 0f);
            level.SetTarget(2f);
            Assert.AreEqual(1f, level.Target);
            level.Step(-1f);
            Assert.AreEqual(0f, level.Current);
            level.Step(0.01f);
            Assert.AreEqual(1f, level.Current);
        }

        [Test]
        public void FX_TRAIL_001_VehicleNamesAndTeleportRule()
        {
            Assert.IsTrue(VehicleNameRule.IsVehicle("Motorbike_12"));
            Assert.IsTrue(VehicleNameRule.IsVehicle("VehicleCustomer"));
            Assert.IsFalse(VehicleNameRule.IsVehicle("Pedestrian_3"));
            Assert.IsFalse(VehicleNameRule.IsVehicle(null));
            Assert.IsFalse(TrailResetRule.ShouldClear(UnityEngine.Vector3.zero, new UnityEngine.Vector3(1f, 0f, 1f), 3f));
            Assert.IsTrue(TrailResetRule.ShouldClear(UnityEngine.Vector3.zero, new UnityEngine.Vector3(30f, 0f, 0f), 3f));
        }
    }
}
