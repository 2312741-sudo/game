using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.Core;
using TramChanh.Orders;
using TramChanh.UI.Localization;
using TramChanh.UI.Orders;
using UnityEditor;
using UnityEngine;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class OrderEntryUITests
    {
        private const string TextAssetPath = "Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_OrderEntry.asset";
        private GameObject _host;
        private EventBus _events;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _host = new GameObject("OrderEntryUI");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            _events.Dispose();
        }

        private OrderEntryUI Create(PromptLocalizationTable table, string language)
        {
            var ui = _host.AddComponent<OrderEntryUI>();
            var serialized = new SerializedObject(ui);
            serialized.FindProperty("_table").objectReferenceValue = table;
            serialized.FindProperty("_language").stringValue = language;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ui.Initialize(_events);
            return ui;
        }

        private static PromptLocalizationTable Table()
        {
            var table = AssetDatabase.LoadAssetAtPath<PromptLocalizationTable>(TextAssetPath);
            Assert.That(table, Is.Not.Null, TextAssetPath);
            return table;
        }

        [Test]
        public void EnterAndSendMethodsPublishTypedEventsAndHostHooksFire()
        {
            OrderEntryUI ui = Create(Table(), "en");
            var confirmed = new List<OrderEntryConfirmed>();
            var send = new List<OrderSendRequested>();
            _events.Subscribe<OrderEntryConfirmed>(confirmed.Add);
            _events.Subscribe<OrderSendRequested>(send.Add);
            int shown = 0;
            int hidden = 0;
            ui.Shown += () => shown++;
            ui.Hidden += () => hidden++;
            Assert.That(ui.Enter(), Is.False);
            Assert.That(ui.Send(), Is.False);
            _events.Publish(new OrderEntryRequested(new OrderId(3), new[] { new ItemRequest("drink.demo", 2) }));
            Assert.That(ui.IsOpen, Is.True);
            Assert.That(shown, Is.EqualTo(1));
            Assert.That(ui.Enter(), Is.True);
            Assert.That(confirmed.Count, Is.EqualTo(1));
            Assert.That(send, Is.Empty);
            Assert.That(ui.Send(), Is.True);
            Assert.That(send.Count, Is.EqualTo(1));
            _events.Publish(new OrderStatusChanged(new OrderId(3), OrderStatus.SentToStall));
            Assert.That(ui.IsOpen, Is.False);
            Assert.That(hidden, Is.EqualTo(1));
            _events.Publish(new OrderEntryRequested(new OrderId(4), new[] { new ItemRequest("drink.demo", 1) }));
            ui.Cancel();
            Assert.That(ui.IsOpen, Is.False);
            Assert.That(shown, Is.EqualTo(2));
            Assert.That(hidden, Is.EqualTo(2));
        }

        [Test]
        public void TextResolvesBilingualKeysAndItemDefinitionKeys()
        {
            PromptLocalizationTable table = Table();
            OrderEntryUI english = Create(table, "en");
            _events.Publish(new OrderEntryRequested(new OrderId(3), new[] { new ItemRequest("drink.demo", 2) }));
            Assert.That(english.TitleText, Is.EqualTo(table.Resolve("order.entry.title", "en")));
            Assert.That(english.TitleText, Is.Not.EqualTo("order.entry.title"));
            Assert.That(english.ItemsText, Is.EqualTo("item.drink.demo  x2"), "Unknown definition ids fall back to the key.");
            string vietnamese = table.Resolve("order.entry.title", "vi");
            Assert.That(vietnamese, Is.Not.EqualTo(english.TitleText));
            Assert.That(vietnamese, Is.Not.EqualTo("order.entry.title"));
        }

        [Test]
        public void ItemKeysResolveInBothLanguagesFromTheDefinitionId()
        {
            var table = ScriptableObject.CreateInstance<PromptLocalizationTable>();
            try
            {
                var serialized = new SerializedObject(table);
                SerializedProperty entries = serialized.FindProperty("_entries");
                entries.arraySize = 1;
                SerializedProperty entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("_key").stringValue = "item.drink.demo";
                entry.FindPropertyRelative("_en").stringValue = "Demo drink";
                entry.FindPropertyRelative("_vi").stringValue = "Th\u1EE9c u\u1ED1ng m\u1EABu";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                OrderEntryUI ui = Create(table, "en");
                _events.Publish(new OrderEntryRequested(new OrderId(3), new[] { new ItemRequest("drink.demo", 2), new ItemRequest("drink.other", 1) }));
                Assert.That(ui.ItemsText, Is.EqualTo("Demo drink  x2\nitem.drink.other  x1"));
                Object.DestroyImmediate(_host);
                _host = new GameObject("OrderEntryUIvi");
                OrderEntryUI vi = Create(table, "vi");
                _events.Publish(new OrderEntryRequested(new OrderId(3), new[] { new ItemRequest("drink.demo", 1) }));
                Assert.That(vi.ItemsText, Is.EqualTo("Th\u1EE9c u\u1ED1ng m\u1EABu  x1"));
            }
            finally
            {
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void LocalizationAssetHasEnglishAndVietnameseForEveryKeyTheFlowUses()
        {
            PromptLocalizationTable table = Table();
            foreach (string key in new[]
            {
                OrderEntryUI.TitleKey, OrderEntryUI.EnterKey, OrderEntryUI.SendKey, OrderEntryUI.CloseKey,
                "order.point.take", "order.point.continue", "interaction.lobby_role_required", "interaction.paused",
                "hands.full", "order.point.not_configured", "order.wrong_point", "order.entry.not_available", "order.invalid_transition",
            })
            {
                string en = table.Resolve(key, "en");
                string vi = table.Resolve(key, "vi");
                Assert.That(en, Is.Not.EqualTo(key), "missing English: " + key);
                Assert.That(vi, Is.Not.EqualTo(key), "missing Vietnamese: " + key);
                Assert.That(vi, Is.Not.EqualTo(en), "Vietnamese equals English: " + key);
            }
        }

        [Test]
        public void ReasonTextAndDisconnectBehave()
        {
            OrderEntryUI ui = Create(Table(), "vi");
            _events.Publish(new OrderEntryRequested(new OrderId(3), new[] { new ItemRequest("drink.demo", 1) }));
            _events.Publish(new TramChanh.Interaction.ActionBlocked(new InteractableId(1), "order.invalid_transition"));
            Assert.That(ui.ReasonText, Is.Not.Empty);
            Assert.That(ui.ReasonText, Is.Not.EqualTo("order.invalid_transition"));
            int hidden = 0;
            ui.Hidden += () => hidden++;
            ui.Disconnect();
            Assert.That(hidden, Is.EqualTo(1));
            Assert.That(ui.IsOpen, Is.False);
            _events.Publish(new OrderEntryRequested(new OrderId(4), new[] { new ItemRequest("drink.demo", 1) }));
            Assert.That(ui.IsOpen, Is.False);
            Assert.Throws<System.ArgumentNullException>(() => ui.Initialize(null));
        }
    }
}
