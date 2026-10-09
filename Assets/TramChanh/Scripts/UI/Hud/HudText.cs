using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TramChanh.UI.Hud
{
    /// <summary>
    /// A localization key plus arguments, rendered late by the view. An argument is either another
    /// <see cref="HudText"/> (localized), a string or a number (literal). Placeholders are {0}, {1}...;
    /// rendering never throws on a malformed template.
    /// </summary>
    public readonly struct HudText : IEquatable<HudText>
    {
        private static readonly object[] NoArgs = Array.Empty<object>();
        private readonly object[] _args;

        public string Key { get; }
        public IReadOnlyList<object> Args => _args ?? NoArgs;
        public bool IsEmpty => string.IsNullOrEmpty(Key);

        private HudText(string key, object[] args)
        {
            Key = key;
            _args = args == null || args.Length == 0 ? null : (object[])args.Clone();
        }

        public static HudText Of(string key, params object[] args) => new HudText(key, args);

        public static readonly HudText Empty = default;

        public string Render(Func<string, string> resolve)
        {
            if (IsEmpty) { return string.Empty; }
            string template = resolve != null ? resolve(Key) ?? Key : Key;
            if (_args == null) { return template; }
            var rendered = new string[_args.Length];
            for (int i = 0; i < _args.Length; i++) { rendered[i] = RenderArg(_args[i], resolve); }
            return Fill(template, rendered);
        }

        /// <summary>Every key this text needs, including nested argument keys.</summary>
        public IEnumerable<string> Keys()
        {
            if (IsEmpty) { yield break; }
            yield return Key;
            if (_args == null) { yield break; }
            foreach (object arg in _args)
            {
                if (arg is HudText nested)
                {
                    foreach (string key in nested.Keys()) { yield return key; }
                }
            }
        }

        private static string RenderArg(object arg, Func<string, string> resolve)
        {
            switch (arg)
            {
                case null: return string.Empty;
                case HudText text: return text.Render(resolve);
                case IFormattable formattable: return formattable.ToString(null, CultureInfo.InvariantCulture);
                default: return arg.ToString();
            }
        }

        private static string Fill(string template, string[] args)
        {
            var output = new StringBuilder(template.Length + 16);
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];
                if (c == '{')
                {
                    int close = template.IndexOf('}', i + 1);
                    if (close > i + 1 && int.TryParse(template.Substring(i + 1, close - i - 1), NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                        && index < args.Length)
                    {
                        output.Append(args[index]);
                        i = close;
                        continue;
                    }
                }
                output.Append(c);
            }
            return output.ToString();
        }

        public bool Equals(HudText other)
        {
            if (!string.Equals(Key, other.Key, StringComparison.Ordinal)) { return false; }
            IReadOnlyList<object> mine = Args;
            IReadOnlyList<object> theirs = other.Args;
            if (mine.Count != theirs.Count) { return false; }
            for (int i = 0; i < mine.Count; i++)
            {
                if (!Equals(mine[i], theirs[i])) { return false; }
            }
            return true;
        }

        public override bool Equals(object obj) => obj is HudText other && Equals(other);

        public override int GetHashCode()
        {
            int hash = Key != null ? StringComparer.Ordinal.GetHashCode(Key) : 0;
            foreach (object arg in Args) { hash = hash * 31 + (arg?.GetHashCode() ?? 0); }
            return hash;
        }

        public override string ToString() => Render(null);
    }
}
