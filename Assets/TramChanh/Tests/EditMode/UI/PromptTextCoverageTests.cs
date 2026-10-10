using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TramChanh.Cakes;
using TramChanh.Drinks.Domain;
using TramChanh.Interaction;
using TramChanh.UI.Hud;

namespace TramChanh.Tests.EditMode.UI
{
    /// <summary>
    /// MAIN-103: every localization key the runtime can show must resolve in SO_PromptText_TramChanhMain in
    /// both languages. Keys come from string literals in Scripts/** (Editor excluded) plus composed keys
    /// (cake.state.*, drink.state.*, order/item status, held item types, item definitions, HUD keys).
    /// Plain .NET: reads the asset YAML and source files directly.
    /// </summary>
    public sealed class PromptTextCoverageTests
    {
        private static readonly Regex KeyLiteral = new Regex("\"((?:[a-z][a-z0-9_]*)(?:\\.[A-Za-z0-9_]+)+\\.?)\"", RegexOptions.Compiled);
        private static readonly Regex RawKey = new Regex(@"\b[a-z][a-z0-9_]*(\.[A-Za-z0-9_]+){1,}\b", RegexOptions.Compiled);
        private const int MaxVietnameseLength = 60;
        // Long by design: the controls legend and the order-entry hint that tells the player to enter before sending.
        private static readonly HashSet<string> LongTextAllowed = new HashSet<string> { "preview.controls", "order.transition.invalid" };

        private PromptTableFile _table;

        [SetUp]
        public void SetUp() => _table = PromptTableFile.LoadMain();

        internal static string Root => PromptTableFile.ProjectRoot();

        /// <summary>Key literals used in runtime scripts, with one source location each.</summary>
        internal static Dictionary<string, string> LiteralKeys()
        {
            string scripts = Path.Combine(Root, "Assets", "TramChanh", "Scripts");
            var keys = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/Editor/")) { continue; }
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match match in KeyLiteral.Matches(lines[i]))
                    {
                        string key = match.Groups[1].Value;
                        // A trailing dot is a composed-key prefix; its expansions are listed by ComposedKeys.
                        if (key.EndsWith(".", StringComparison.Ordinal)) { continue; }
                        if (!keys.ContainsKey(key)) { keys.Add(key, normalized.Substring(scripts.Length + 1) + ":" + (i + 1)); }
                    }
                }
            }
            return keys;
        }

        internal static Dictionary<string, string> ComposedKeys()
        {
            var keys = new Dictionary<string, string>(StringComparer.Ordinal);
            void Add(string key, string source) { if (!keys.ContainsKey(key)) { keys.Add(key, source); } }
            foreach (CakeState state in Enum.GetValues(typeof(CakeState))) { Add("cake.state." + state.ToString().ToLowerInvariant(), "CakePreparation.PreparationStateKey"); }
            // IPreparationFeedback contract (Docs/ACCEL-01_CONTRACTS.md): drink.state.* for the tea bag.
            foreach (TeaBagState state in Enum.GetValues(typeof(TeaBagState))) { Add("drink.state." + state.ToString().ToLowerInvariant(), "drink.state contract"); }
            foreach (string key in HudKeys.All) { Add(key, "HudKeys"); }
            foreach (Type type in HoldableTypes()) { Add(HudKeys.HeldType(type), "IHoldable " + type.FullName); }
            foreach (string id in ItemDefinitionIds()) { Add(HudKeys.ItemName(id), "ItemDefinition " + id); }
            return keys;
        }

        internal static IEnumerable<Type> HoldableTypes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || !assembly.GetName().Name.StartsWith("TramChanh", StringComparison.Ordinal)) { continue; }
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException partial) { types = partial.Types.Where(t => t != null).ToArray(); }
                foreach (Type type in types)
                {
                    if (type.IsAbstract || type.IsInterface || !typeof(IHoldable).IsAssignableFrom(type)) { continue; }
                    if (type.Namespace != null && type.Namespace.StartsWith("TramChanh.Tests", StringComparison.Ordinal)) { continue; }
                    yield return type;
                }
            }
        }

        /// <summary>Ids of every serialized ItemDefinition under Assets/TramChanh (OrderEntryUI and the HUD show "item." + id).</summary>
        internal static IEnumerable<string> ItemDefinitionIds()
        {
            string assets = Path.Combine(Root, "Assets", "TramChanh");
            foreach (string file in Directory.GetFiles(assets, "*.asset", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                if (!text.Contains("TramChanh.Content.ItemDefinition")) { continue; }
                Match id = Regex.Match(text, @"(?m)^  _id: (.+)$");
                if (id.Success) { yield return PromptTableFile.Scalar(id.Groups[1].Value); }
            }
        }

        [Test]
        public void MAIN103_EveryKeyUsedByCodeResolvesInBothLanguages()
        {
            var missing = new List<string>();
            var used = LiteralKeys();
            foreach (KeyValuePair<string, string> composed in ComposedKeys()) { if (!used.ContainsKey(composed.Key)) { used.Add(composed.Key, composed.Value); } }
            foreach (KeyValuePair<string, string> pair in used.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (!_table.Contains(pair.Key)) { missing.Add(pair.Key + "  (" + pair.Value + ")"); continue; }
                foreach (string language in new[] { "en", "vi" })
                {
                    string text = _table.Resolve(pair.Key, language);
                    if (string.IsNullOrWhiteSpace(text) || text == pair.Key) { missing.Add(pair.Key + " [" + language + "] is empty or raw"); }
                }
            }
            Assert.That(used.Count, Is.GreaterThan(150), "Key scan found too few keys; the scanner is broken.");
            Assert.That(missing, Is.Empty, "Missing localization:\n" + string.Join("\n", missing));
        }

        [Test]
        public void MAIN103_ComposedKeyFamiliesAreComplete()
        {
            Assert.That(HoldableTypes().Select(t => t.Name), Is.SupersetOf(new[] { "TeaBagItem", "CakeItem", "BatterMeasureCup", "ServedOrder" }));
            Assert.That(ItemDefinitionIds(), Is.SupersetOf(new[] { "drink.slice", "cake.dev.tbd" }));
            foreach (string key in ComposedKeys().Keys) { Assert.That(_table.Contains(key), Is.True, key); }
        }

        [Test]
        public void MAIN103_TableHasUniqueKeysMatchingPlaceholdersAndShortVietnamese()
        {
            Assert.That(_table.DuplicateKeys, Is.Empty);
            foreach (PromptTableFile.Entry entry in _table.Entries)
            {
                Assert.That(entry.English, Is.Not.Null.And.Not.Empty, entry.Key + " en");
                Assert.That(entry.Vietnamese, Is.Not.Null.And.Not.Empty, entry.Key + " vi");
                Assert.That(Placeholders(entry.Vietnamese), Is.EqualTo(Placeholders(entry.English)), entry.Key + " placeholders");
                Assert.That(RawKey.IsMatch(entry.Vietnamese), Is.False, entry.Key + " vi looks like a raw key: " + entry.Vietnamese);
                if (!LongTextAllowed.Contains(entry.Key))
                {
                    Assert.That(entry.Vietnamese.Length, Is.LessThanOrEqualTo(MaxVietnameseLength), entry.Key + " vi is too long");
                }
            }
        }

        [Test]
        public void MAIN103_EveryHudTextRendersWithoutRawKeys()
        {
            // Placeholder arguments are filled, nested keys are localized and nothing is left as a key.
            foreach (string key in HudKeys.All)
            {
                string rendered = HudText.Of(key, HudText.Of(HudKeys.OriginVehicle, 1), 1, 2).Render(k => _table.Resolve(k, "vi"));
                Assert.That(rendered, Does.Not.Contain("{"), key);
                Assert.That(RawKey.IsMatch(rendered), Is.False, key + " -> " + rendered);
            }
        }

        private static string Placeholders(string text)
        {
            var found = new SortedSet<string>(StringComparer.Ordinal);
            foreach (Match match in Regex.Matches(text ?? string.Empty, @"\{\d+\}")) { found.Add(match.Value); }
            return string.Join(",", found);
        }
    }
}
