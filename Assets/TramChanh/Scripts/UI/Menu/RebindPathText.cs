using System;

namespace TramChanh.UI.Menu
{
    /// <summary>Pure helpers for input control paths such as "&lt;Keyboard&gt;/e": normalizing, device checks and a Vietnamese display name.</summary>
    public static class RebindPathText
    {
        public const string KeyboardPrefix = "<keyboard>/";
        public const string MousePrefix = "<mouse>/";
        public const string Unassigned = "(chưa gán)";

        public static string Normalize(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().ToLowerInvariant();
        }

        public static bool MatchesDevice(string path, RebindDevice device)
        {
            string p = Normalize(path);
            string prefix = device == RebindDevice.Keyboard ? KeyboardPrefix : MousePrefix;
            return p.Length > prefix.Length && p.StartsWith(prefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Replaces tokens like [[Interact]] (keyboard key) and [[Interact:mouse]] (mouse button) in a hint text with the live key name.
        /// Unknown or unclosed tokens are left as written.
        /// </summary>
        public static string ExpandTokens(string template, Func<string, RebindDevice, string> resolve)
        {
            if (string.IsNullOrEmpty(template) || resolve == null) { return template ?? string.Empty; }
            var sb = new System.Text.StringBuilder(template.Length);
            int i = 0;
            while (i < template.Length)
            {
                int open = template.IndexOf("[[", i, StringComparison.Ordinal);
                int close = open < 0 ? -1 : template.IndexOf("]]", open + 2, StringComparison.Ordinal);
                if (open < 0 || close < 0)
                {
                    sb.Append(template, i, template.Length - i);
                    break;
                }
                sb.Append(template, i, open - i);
                string token = template.Substring(open + 2, close - open - 2);
                RebindDevice device = RebindDevice.Keyboard;
                string name = token;
                int colon = token.IndexOf(':');
                if (colon >= 0)
                {
                    name = token.Substring(0, colon);
                    if (string.Equals(token.Substring(colon + 1), "mouse", StringComparison.OrdinalIgnoreCase)) { device = RebindDevice.Mouse; }
                }
                string value = name.Length == 0 ? null : resolve(name, device);
                sb.Append(value ?? template.Substring(open, close + 2 - open));
                i = close + 2;
            }
            return sb.ToString();
        }

        /// <summary>"&lt;Keyboard&gt;/e" gives "E", "&lt;Mouse&gt;/leftButton" gives "Chuột trái". Empty path gives "(chưa gán)".</summary>
        public static string Display(string path)
        {
            string p = Normalize(path);
            if (p.Length == 0) { return Unassigned; }
            int slash = p.LastIndexOf('/');
            string control = slash >= 0 ? p.Substring(slash + 1) : p;
            // Layout-specific form "#(e)" keeps the character.
            if (control.StartsWith("#(", StringComparison.Ordinal) && control.EndsWith(")", StringComparison.Ordinal) && control.Length > 3)
            {
                control = control.Substring(2, control.Length - 3);
            }
            switch (control)
            {
                case "leftbutton": return "Chuột trái";
                case "rightbutton": return "Chuột phải";
                case "middlebutton": return "Chuột giữa";
                case "escape": return "Esc";
                case "space": return "Space";
                case "enter": return "Enter";
                case "tab": return "Tab";
                case "backspace": return "Backspace";
                case "leftshift": return "Shift trái";
                case "rightshift": return "Shift phải";
                case "leftctrl": return "Ctrl trái";
                case "rightctrl": return "Ctrl phải";
                case "leftalt": return "Alt trái";
                case "rightalt": return "Alt phải";
                case "uparrow": return "Mũi tên lên";
                case "downarrow": return "Mũi tên xuống";
                case "leftarrow": return "Mũi tên trái";
                case "rightarrow": return "Mũi tên phải";
            }
            return control.Length == 0 ? Unassigned : control.ToUpperInvariant();
        }
    }
}
