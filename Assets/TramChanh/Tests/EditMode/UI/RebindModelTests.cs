using System.Collections.Generic;
using NUnit.Framework;
using TramChanh.UI.Menu;

namespace TramChanh.Tests.EditMode.UI
{
    public sealed class RebindModelTests
    {
        private sealed class DictStore : IRebindTextStore
        {
            public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
            public bool TryGet(string key, out string value) => Data.TryGetValue(key, out value);
            public void Set(string key, string value) { Data[key] = value; }
            public void Delete(string key) { Data.Remove(key); }
        }

        private static RebindModel Make()
        {
            return new RebindModel(new[]
            {
                new RebindSlot("interactKey", "Interact", 4, "Tương tác (phím)", RebindDevice.Keyboard, "<Keyboard>/e", "<Keyboard>/e"),
                new RebindSlot("interactMouse", "Interact", 5, "Tương tác (chuột)", RebindDevice.Mouse, "<Mouse>/leftButton", "<Mouse>/leftButton"),
                new RebindSlot("useKey", "UseHeld", 6, "Dùng vật", RebindDevice.Keyboard, "<Keyboard>/f", "<Keyboard>/f"),
                new RebindSlot("pause", "Pause", 9, "Tạm dừng", RebindDevice.Keyboard, "<Keyboard>/escape", "<Keyboard>/escape")
            }, RebindModel.DefaultReservedPaths);
        }

        [Test]
        public void TrySet_FreeKey_AppliesAndReportsOneChange()
        {
            RebindModel model = Make();
            RebindResult result = model.TrySet("interactKey", "<Keyboard>/g", false);
            Assert.That(result.Outcome, Is.EqualTo(RebindOutcome.Applied));
            Assert.That(result.Changes.Count, Is.EqualTo(1));
            Assert.That(model.Find("interactKey").CurrentPath, Is.EqualTo("<keyboard>/g"));
        }

        [Test]
        public void TrySet_TakenKey_RefusesWhenSwapOff()
        {
            RebindModel model = Make();
            RebindResult result = model.TrySet("interactKey", "<Keyboard>/f", false);
            Assert.That(result.Outcome, Is.EqualTo(RebindOutcome.Conflict));
            Assert.That(result.Message, Does.Contain("Dùng vật"));
            Assert.That(model.Find("interactKey").CurrentPath, Is.EqualTo("<keyboard>/e"));
            Assert.That(model.Find("useKey").CurrentPath, Is.EqualTo("<keyboard>/f"));
        }

        [Test]
        public void TrySet_TakenKey_SwapsWhenSwapOn()
        {
            RebindModel model = Make();
            RebindResult result = model.TrySet("interactKey", "<Keyboard>/f", true);
            Assert.That(result.Outcome, Is.EqualTo(RebindOutcome.Swapped));
            Assert.That(result.Changes.Count, Is.EqualTo(2));
            Assert.That(model.Find("interactKey").CurrentPath, Is.EqualTo("<keyboard>/f"));
            Assert.That(model.Find("useKey").CurrentPath, Is.EqualTo("<keyboard>/e"));
            Assert.That(model.IsConsistent(), Is.True);
        }

        [Test]
        public void TrySet_MouseButtonOnKeyboardSlot_IsWrongDevice()
        {
            RebindModel model = Make();
            Assert.That(model.TrySet("interactKey", "<Mouse>/rightButton", true).Outcome, Is.EqualTo(RebindOutcome.WrongDevice));
            Assert.That(model.TrySet("interactMouse", "<Keyboard>/g", true).Outcome, Is.EqualTo(RebindOutcome.WrongDevice));
        }

        [Test]
        public void TrySet_ReservedKeys_AreRefused()
        {
            RebindModel model = Make();
            Assert.That(model.TrySet("interactKey", "<Keyboard>/r", true).Outcome, Is.EqualTo(RebindOutcome.Reserved));
            Assert.That(model.TrySet("interactKey", "<Keyboard>/V", true).Outcome, Is.EqualTo(RebindOutcome.Reserved));
            Assert.That(model.TrySet("interactKey", "<Keyboard>/f1", true).Outcome, Is.EqualTo(RebindOutcome.Reserved));
        }

        [Test]
        public void TrySet_SameKey_IsUnchanged_AndUnknownSlotIsReported()
        {
            RebindModel model = Make();
            Assert.That(model.TrySet("interactKey", "<Keyboard>/e", true).Outcome, Is.EqualTo(RebindOutcome.Unchanged));
            Assert.That(model.TrySet("nope", "<Keyboard>/g", true).Outcome, Is.EqualTo(RebindOutcome.UnknownSlot));
        }

        [Test]
        public void Reset_SwapsBackWhenAnotherSlotHoldsTheDefault()
        {
            RebindModel model = Make();
            model.TrySet("interactKey", "<Keyboard>/f", true);
            RebindResult result = model.Reset("interactKey");
            Assert.That(result.Succeeded, Is.True);
            Assert.That(model.Find("interactKey").IsDefault, Is.True);
            Assert.That(model.Find("useKey").IsDefault, Is.True);
        }

        [Test]
        public void ResetAll_RestoresEveryDefault_AndReportsUnchangedWhenAlreadyDefault()
        {
            RebindModel model = Make();
            Assert.That(model.ResetAll().Outcome, Is.EqualTo(RebindOutcome.Unchanged));
            model.TrySet("interactKey", "<Keyboard>/g", false);
            model.TrySet("pause", "<Keyboard>/p", false);
            RebindResult result = model.ResetAll();
            Assert.That(result.Outcome, Is.EqualTo(RebindOutcome.Applied));
            Assert.That(result.Changes.Count, Is.EqualTo(2));
            foreach (RebindSlot slot in model.Slots) { Assert.That(slot.IsDefault, Is.True); }
        }

        [Test]
        public void IsConsistent_DetectsDuplicatesAndWrongDevice()
        {
            var dup = new RebindModel(new[]
            {
                new RebindSlot("a", "Interact", 0, "A", RebindDevice.Keyboard, "<Keyboard>/e", "<Keyboard>/x"),
                new RebindSlot("b", "UseHeld", 1, "B", RebindDevice.Keyboard, "<Keyboard>/f", "<Keyboard>/x")
            });
            Assert.That(dup.IsConsistent(), Is.False);
            var wrong = new RebindModel(new[] { new RebindSlot("a", "Interact", 0, "A", RebindDevice.Keyboard, "<Keyboard>/e", "<Mouse>/leftButton") });
            Assert.That(wrong.IsConsistent(), Is.False);
            Assert.That(Make().IsConsistent(), Is.True);
        }

        [Test]
        public void Display_UsesVietnameseNamesForMouseAndSpecialKeys()
        {
            Assert.That(RebindPathText.Display("<Keyboard>/e"), Is.EqualTo("E"));
            Assert.That(RebindPathText.Display("<Keyboard>/escape"), Is.EqualTo("Esc"));
            Assert.That(RebindPathText.Display("<Mouse>/leftButton"), Is.EqualTo("Chuột trái"));
            Assert.That(RebindPathText.Display("<Mouse>/rightButton"), Is.EqualTo("Chuột phải"));
            Assert.That(RebindPathText.Display("<Keyboard>/#(q)"), Is.EqualTo("Q"));
            Assert.That(RebindPathText.Display(""), Is.EqualTo("(chưa gán)"));
        }

        [Test]
        public void ExpandTokens_ReplacesKnownTokensAndLeavesTheRest()
        {
            string text = RebindPathText.ExpandTokens("[[Interact]]/[[Interact:mouse]]: quầy [[Nope]] [[oops",
                (name, device) => name == "Interact" ? (device == RebindDevice.Mouse ? "Chuột trái" : "E") : null);
            Assert.That(text, Is.EqualTo("E/Chuột trái: quầy [[Nope]] [[oops"));
        }

        [Test]
        public void JsonStore_RoundTripsValidText()
        {
            var store = new DictStore();
            var json = new RebindJsonStore(store);
            json.Save("{\"bindings\":[{\"action\":\"Gameplay/Interact\",\"id\":\"x\",\"path\":\"<Keyboard>/g\"}]}");
            Assert.That(json.TryLoad(out string loaded), Is.True);
            Assert.That(loaded, Does.Contain("<Keyboard>/g"));
        }

        [Test]
        public void JsonStore_CorruptDataFallsBackAndIsDeleted()
        {
            var store = new DictStore();
            store.Data[RebindJsonStore.DefaultKey] = "{not json at all";
            var json = new RebindJsonStore(store);
            Assert.That(json.TryLoad(out string loaded), Is.False);
            Assert.That(loaded, Is.Null);
            Assert.That(store.Data.ContainsKey(RebindJsonStore.DefaultKey), Is.False);
        }

        [Test]
        public void JsonStore_RejectsWrongShapes()
        {
            Assert.That(RebindJsonStore.LooksValid(""), Is.False);
            Assert.That(RebindJsonStore.LooksValid("[]"), Is.False);
            Assert.That(RebindJsonStore.LooksValid("{\"other\":1}"), Is.False);
            Assert.That(RebindJsonStore.LooksValid("{\"bindings\":[{\"action\":\"a\",\"path\":\"\"}]}"), Is.False);
            Assert.That(RebindJsonStore.LooksValid("{\"bindings\":[]}"), Is.True);
        }

        [Test]
        public void JsonStore_SavingInvalidTextClearsTheEntry()
        {
            var store = new DictStore();
            var json = new RebindJsonStore(store);
            json.Save("{\"bindings\":[]}");
            Assert.That(store.Data.Count, Is.EqualTo(1));
            json.Save("garbage");
            Assert.That(store.Data.Count, Is.EqualTo(0));
        }
    }
}
