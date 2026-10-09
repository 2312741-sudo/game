using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TramChanh.Tests.EditMode.UI
{
    /// <summary>
    /// Plain .NET reader for a serialized PromptLocalizationTable asset (Unity YAML), so localization
    /// coverage can be checked without the AssetDatabase. Handles plain, single-quoted and double-quoted
    /// (\xNN, \uNNNN escapes) scalars, as Unity writes them.
    /// </summary>
    internal sealed class PromptTableFile
    {
        public const string MainTablePath = "Assets/TramChanh/ScriptableObjects/UI/SO_PromptText_TramChanhMain.asset";

        public readonly struct Entry
        {
            public Entry(string key, string en, string vi) { Key = key; English = en; Vietnamese = vi; }
            public string Key { get; }
            public string English { get; }
            public string Vietnamese { get; }
        }

        private readonly Dictionary<string, Entry> _byKey = new Dictionary<string, Entry>(StringComparer.Ordinal);

        public List<Entry> Entries { get; } = new List<Entry>();
        public List<string> DuplicateKeys { get; } = new List<string>();

        public bool Contains(string key) => key != null && _byKey.ContainsKey(key);

        /// <summary>Same contract as PromptLocalizationTable.Resolve: a missing key returns the key.</summary>
        public string Resolve(string key, string language)
        {
            if (string.IsNullOrEmpty(key)) { return string.Empty; }
            return _byKey.TryGetValue(key, out Entry entry) ? (language == "vi" ? entry.Vietnamese : entry.English) : key;
        }

        public static string ProjectRoot()
        {
            string directory = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(directory))
            {
                if (Directory.Exists(Path.Combine(directory, "Assets", "TramChanh"))) { return directory; }
                directory = Path.GetDirectoryName(directory);
            }
            throw new DirectoryNotFoundException("Run from inside the Unity project (Assets/TramChanh not found).");
        }

        public static PromptTableFile LoadMain() => Load(Path.Combine(ProjectRoot(), MainTablePath));

        public static PromptTableFile Load(string path)
        {
            var table = new PromptTableFile();
            string key = null, en = null, vi = null;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.TrimEnd('\r');
                if (line.StartsWith("  - _key:", StringComparison.Ordinal))
                {
                    table.Add(key, en, vi);
                    key = Scalar(line.Substring("  - _key:".Length));
                    en = vi = null;
                }
                else if (key != null && line.StartsWith("    _en:", StringComparison.Ordinal)) { en = Scalar(line.Substring("    _en:".Length)); }
                else if (key != null && line.StartsWith("    _vi:", StringComparison.Ordinal)) { vi = Scalar(line.Substring("    _vi:".Length)); }
            }
            table.Add(key, en, vi);
            return table;
        }

        private void Add(string key, string en, string vi)
        {
            if (key == null) { return; }
            var entry = new Entry(key, en, vi);
            if (_byKey.ContainsKey(key)) { DuplicateKeys.Add(key); }
            _byKey[key] = entry;
            Entries.Add(entry);
        }

        internal static string Scalar(string text)
        {
            text = text.Trim();
            if (text.Length == 0) { return string.Empty; }
            if (text[0] == '\'')
            {
                return text.Substring(1, text.Length - 2).Replace("''", "'");
            }
            if (text[0] != '"') { return text; }
            var output = new StringBuilder();
            for (int i = 1; i < text.Length - 1; i++)
            {
                char c = text[i];
                if (c != '\\') { output.Append(c); continue; }
                char code = text[++i];
                switch (code)
                {
                    case 'x': output.Append((char)int.Parse(text.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 2; break;
                    case 'u': output.Append((char)int.Parse(text.Substring(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 4; break;
                    case 'U': output.Append(char.ConvertFromUtf32(int.Parse(text.Substring(i + 1, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture))); i += 8; break;
                    case 'n': output.Append('\n'); break;
                    case 't': output.Append('\t'); break;
                    case '0': output.Append('\0'); break;
                    case ' ': output.Append(' '); break;
                    case '/': output.Append('/'); break;
                    default: output.Append(code); break;
                }
            }
            return output.ToString();
        }
    }
}
