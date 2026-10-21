using System;
using NUnit.Framework;
using TramChanh.Orders;
using TramChanh.UI.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class HudThemeTests
    {
        [Test]
        public void EveryOrderStatus_HasAToneAndAColour()
        {
            foreach (OrderStatus status in Enum.GetValues(typeof(OrderStatus)))
            {
                StageTone tone = HudTheme.ToneFor(status);
                Assert.That(HudTheme.ToneColor(tone).a, Is.GreaterThan(0.5f), status.ToString());
            }
        }

        [Test]
        public void Tones_FollowTheOrderLifecycle()
        {
            Assert.That(HudTheme.ToneFor(OrderStatus.TakingOrder), Is.EqualTo(StageTone.Waiting));
            Assert.That(HudTheme.ToneFor(OrderStatus.InPreparation), Is.EqualTo(StageTone.Working));
            Assert.That(HudTheme.ToneFor(OrderStatus.Ready), Is.EqualTo(StageTone.Ready));
            Assert.That(HudTheme.ToneFor(OrderStatus.Completed), Is.EqualTo(StageTone.Done));
            Assert.That(HudTheme.ToneFor(OrderStatus.Failed), Is.EqualTo(StageTone.Failed));
        }

        [Test]
        public void ToneColours_AreDistinct()
        {
            Assert.That(HudTheme.ToneColor(StageTone.Ready), Is.Not.EqualTo(HudTheme.ToneColor(StageTone.Failed)));
            Assert.That(HudTheme.ToneColor(StageTone.Working), Is.Not.EqualTo(HudTheme.ToneColor(StageTone.Waiting)));
        }

        [Test]
        public void PatienceAndToastColours_SeparateGoodFromBad()
        {
            Assert.That(HudTheme.PatienceColor(PatienceLevel.Calm), Is.Not.EqualTo(HudTheme.PatienceColor(PatienceLevel.Danger)));
            Assert.That(HudTheme.ToastColor(true), Is.Not.EqualTo(HudTheme.ToastColor(false)));
        }

        [Test]
        public void ReasonLabel_HidesWhenEmptyAndShowsWhenSet()
        {
            var label = new Label();
            HudTheme.SetReasonText(label, null);
            Assert.That(label.style.display.value, Is.EqualTo(DisplayStyle.None));
            HudTheme.SetReasonText(label, "Hết đá");
            Assert.That(label.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(label.text, Is.EqualTo("Hết đá"));
        }
    }
}
