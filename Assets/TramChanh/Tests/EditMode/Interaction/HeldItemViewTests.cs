using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Core.GroundTruth;
using TramChanh.Drinks.Domain;
using TramChanh.Drinks.Runtime;
using TramChanh.Interaction;
using UnityEditor;
using UnityEngine;

namespace TramChanh.Tests.EditMode.Interaction
{
    public sealed class HeldItemViewTests
    {
        [Test]
        public void TC_INT_004_GripAlignsToHoldAnchorAndViewReconnectsWithoutDuplicateSubscriptions()
        {
            var player = new GameObject("Player");
            var bag = new GameObject("Bag");
            using var events = new EventBus();
            try
            {
                var anchor = new GameObject("HoldAnchor").transform;
                anchor.SetParent(player.transform, false);
                anchor.localPosition = new Vector3(0.2f, 1.4f, 0.4f);
                anchor.localRotation = Quaternion.Euler(15f, 30f, 20f);
                var grip = new GameObject("HandGrip").transform;
                grip.SetParent(bag.transform, false);
                grip.localPosition = new Vector3(0.02f, 0.1f, -0.03f);
                grip.localRotation = Quaternion.Euler(10f, 20f, 30f);
                var item = bag.AddComponent<TeaBagItem>();
                SetReference(item, "_handGrip", grip);
                var preparation = new DrinkPreparation(new PreparationId(1));
                preparation.TryPickUp();
                item.Initialize(preparation);
                var collider = bag.AddComponent<BoxCollider>();
                var view = player.AddComponent<HeldItemView>();
                SetReference(view, "_holdAnchor", anchor);
                var hands = new HeldItemSlot(events);
                view.Initialize(hands, events);
                Assert.That(hands.TryPickUp(item), Is.True);
                Assert.That(item.State, Is.EqualTo(TeaBagState.PickedUp));
                Assert.That(bag.transform.parent, Is.SameAs(anchor));
                Assert.That(Vector3.Distance(grip.position, anchor.position), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(grip.rotation, anchor.rotation), Is.LessThan(0.001f));
                Assert.That(bag.layer, Is.EqualTo(TramChanhLayers.HeldItemIndex));
                Assert.That(collider.enabled, Is.False);
                view.enabled = false;
                view.enabled = true;
                Assert.That(Vector3.Distance(grip.position, anchor.position), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(grip.rotation, anchor.rotation), Is.LessThan(0.001f));
                view.Disconnect();
                // Disconnected view no longer reparents on later notifications.
                bag.transform.SetParent(null, true);
                events.Publish(new HeldItemChanged(null, item));
                Assert.That(bag.transform.parent, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(bag);
                Object.DestroyImmediate(player);
            }
        }

        private static void SetReference(Object target, string name, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
