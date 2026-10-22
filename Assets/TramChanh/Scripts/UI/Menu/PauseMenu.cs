using System;
using TramChanh.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Pause overlay: Resume, volume (master, effects, ambient) and look-speed sliders, key-hint toggle, graphics quality dropdown, Defaults, Credits, Quit. Pure view; the owner decides when to show it
    /// and what Resume, Credits and Quit do, so it can be tested without a scene.
    /// </summary>
    public sealed class PauseMenu
    {
        private readonly VisualElement _panel;
        private readonly Slider _volume;
        private readonly Slider _look;
        private readonly Slider _sfx;
        private readonly Slider _ambient;
        private readonly Toggle _hints;
        private readonly DropdownField _quality;
        private readonly Action _changed;
        private readonly Action<int> _applyQuality;
        private readonly int _qualityCount;
        private readonly Label _info;
        private readonly GameSettings _settings;

        public PauseMenu(VisualElement root, GameSettings settings, Action onResume, Action onCredits, Action onQuit, Action onSettingsChanged,
            string titleText = "TẠM DỪNG", string primaryText = "Tiếp tục", string hintText = "Esc: tiếp tục",
            Action<int> applyQuality = null, string[] qualityNames = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _changed = onSettingsChanged;
            _applyQuality = applyQuality ?? GraphicsQuality.Apply;
            _panel = new VisualElement { name = "PauseMenu" };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0f;
            _panel.style.right = 0f;
            _panel.style.top = 0f;
            _panel.style.bottom = 0f;
            _panel.style.backgroundColor = new Color(0.03f, 0.06f, 0.05f, 0.75f);
            _panel.style.alignItems = Align.Center;
            _panel.style.justifyContent = Justify.Center;
            _panel.style.display = DisplayStyle.None;

            var box = new VisualElement();
            box.style.width = 440f;
            box.style.paddingTop = 24f;
            box.style.paddingBottom = 24f;
            box.style.paddingLeft = 28f;
            box.style.paddingRight = 28f;
            box.style.backgroundColor = new Color(0.08f, 0.17f, 0.12f, 0.96f);
            box.style.borderTopWidth = 2.5f;
            box.style.borderBottomWidth = 2.5f;
            box.style.borderLeftWidth = 2.5f;
            box.style.borderRightWidth = 2.5f;
            box.style.borderTopColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            box.style.borderBottomColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            box.style.borderLeftColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            box.style.borderRightColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            box.style.borderTopLeftRadius = 16f;
            box.style.borderTopRightRadius = 16f;
            box.style.borderBottomLeftRadius = 16f;
            box.style.borderBottomRightRadius = 16f;
            _panel.Add(box);

            // Title or Logo Branding
            if (string.Equals(titleText, "TRẠM CHANH", StringComparison.OrdinalIgnoreCase))
            {
                var logoHeader = new VisualElement();
                logoHeader.style.alignItems = Align.Center;
                logoHeader.style.marginBottom = 16f;

                var logoTitle = new Label("TRẠM CHANH");
                logoTitle.style.fontSize = 36f;
                logoTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                logoTitle.style.color = new Color(0.99f, 0.78f, 0.15f, 1f); // Lime Gold
                logoTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
                logoHeader.Add(logoTitle);

                var subtitleBadge = new Label("TIỆM TRÀ & BÁNH NƯỚNG PHỐ VIỆT");
                subtitleBadge.style.fontSize = 11f;
                subtitleBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
                subtitleBadge.style.color = Color.white;
                subtitleBadge.style.backgroundColor = new Color(0.95f, 0.48f, 0.10f, 1f); // Brand Orange
                subtitleBadge.style.paddingTop = 3f;
                subtitleBadge.style.paddingBottom = 3f;
                subtitleBadge.style.paddingLeft = 10f;
                subtitleBadge.style.paddingRight = 10f;
                subtitleBadge.style.borderTopLeftRadius = 6f;
                subtitleBadge.style.borderTopRightRadius = 6f;
                subtitleBadge.style.borderBottomLeftRadius = 6f;
                subtitleBadge.style.borderBottomRightRadius = 6f;
                subtitleBadge.style.marginTop = 4f;
                logoHeader.Add(subtitleBadge);

                box.Add(logoHeader);
            }
            else
            {
                var title = new Label(titleText);
                title.style.fontSize = 28f;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.color = new Color(0.95f, 0.48f, 0.10f, 1f); // Brand Orange
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                title.style.marginBottom = 14f;
                box.Add(title);
            }

            _info = new Label(string.Empty);
            _info.style.color = new Color(0.90f, 0.95f, 0.90f, 0.85f);
            _info.style.fontSize = 14f;
            _info.style.unityTextAlign = TextAnchor.MiddleCenter;
            _info.style.marginBottom = 8f;
            _info.style.display = DisplayStyle.None;
            box.Add(_info);

            box.Add(MakeButton(primaryText, onResume, true));

            // Options scroll when the window is short, so the buttons below always stay reachable.
            var options = new ScrollView(ScrollViewMode.Vertical);
            options.style.maxHeight = 330f;
            box.Add(options);

            _volume = MakeSlider(options, "Âm lượng", GameSettings.MinVolume, GameSettings.MaxVolume, settings.MasterVolume,
                value => { _settings.MasterVolume = value; onSettingsChanged?.Invoke(); }, "MasterVolume");
            _sfx = MakeSlider(options, "Âm hiệu ứng", GameSettings.MinVolume, GameSettings.MaxVolume, settings.SfxVolume,
                SetSfxVolume, "SfxVolume");
            _ambient = MakeSlider(options, "Âm nền", GameSettings.MinVolume, GameSettings.MaxVolume, settings.AmbientVolume,
                SetAmbientVolume, "AmbientVolume");
            _look = MakeSlider(options, "Tốc độ xoay chuột", GameSettings.MinLookScale, GameSettings.MaxLookScale, settings.LookScale,
                value => { _settings.LookScale = value; onSettingsChanged?.Invoke(); }, "LookScale");

            _hints = new Toggle("Hiện gợi ý phím") { name = "ShowKeyHints", value = settings.ShowKeyHints };
            _hints.style.marginTop = 12f;
            _hints.style.color = UiStyle.Gold;
            _hints.style.fontSize = 15f;
            _hints.RegisterValueChangedCallback(e => SetShowKeyHints(e.newValue));
            options.Add(_hints);

            string[] names = qualityNames ?? QualitySettings.names ?? new string[0];
            _qualityCount = names.Length;
            var qualityLabel = new Label("Chất lượng đồ hoạ");
            qualityLabel.style.color = new Color(0.99f, 0.85f, 0.45f, 1f);
            qualityLabel.style.fontSize = 15f;
            qualityLabel.style.marginTop = 10f;
            qualityLabel.style.marginBottom = 2f;
            options.Add(qualityLabel);
            _quality = new DropdownField(new System.Collections.Generic.List<string>(names), QualityIndex(settings.QualityLevel)) { name = "QualityLevel" };
            _quality.SetEnabled(names.Length > 1);
            _quality.RegisterValueChangedCallback(e => SetQualityLevel(_quality.index));
            options.Add(_quality);

            box.Add(MakeButton("Mặc định", ResetToDefaults, false));
            box.Add(MakeButton("Ghi công (Credits)", onCredits, false));
            box.Add(MakeButton("Thoát game", onQuit, false));

            var hint = new Label(hintText);
            hint.style.fontSize = 13f;
            hint.style.color = new Color(0.80f, 0.90f, 0.84f, 0.70f);
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.style.marginTop = 12f;
            box.Add(hint);

            root.Add(_panel);
        }

        /// <summary>Small line under the title, for example the current day and lifetime totals. Empty hides it.</summary>
        public void SetInfo(string text)
        {
            _info.text = text ?? string.Empty;
            _info.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Sets and saves-through the effects volume (the owner saves in onSettingsChanged).</summary>
        public void SetSfxVolume(float value)
        {
            _settings.SfxVolume = value;
            _sfx.SetValueWithoutNotify(_settings.SfxVolume);
            _changed?.Invoke();
        }

        public void SetAmbientVolume(float value)
        {
            _settings.AmbientVolume = value;
            _ambient.SetValueWithoutNotify(_settings.AmbientVolume);
            _changed?.Invoke();
        }

        public void SetShowKeyHints(bool value)
        {
            _settings.ShowKeyHints = value;
            _hints.SetValueWithoutNotify(_settings.ShowKeyHints);
            _changed?.Invoke();
        }

        /// <summary>Picks a quality level (index into the dropdown), applies it and reports the change. Ignored when the project has fewer than two levels.</summary>
        public void SetQualityLevel(int level)
        {
            if (_qualityCount <= 1) { return; }
            _settings.QualityLevel = level;
            _applyQuality(_settings.QualityLevel);
            _quality.SetValueWithoutNotify(_quality.choices[QualityIndex(_settings.QualityLevel)]);
            _changed?.Invoke();
        }

        /// <summary>What the Defaults button does: every option, including effects, ambient, key hints and quality, goes back to its default.</summary>
        public void ResetToDefaults()
        {
            _settings.ResetToDefaults();
            if (_qualityCount > 1) { _applyQuality(_settings.QualityLevel); }
            SyncSliders();
            _changed?.Invoke();
        }

        public bool IsVisible => _panel.style.display.value == DisplayStyle.Flex;

        public void SetVisible(bool visible)
        {
            if (visible == IsVisible) { return; }
            if (visible) { SyncSliders(); }
            _panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SyncSliders()
        {
            _volume.SetValueWithoutNotify(_settings.MasterVolume);
            _look.SetValueWithoutNotify(_settings.LookScale);
            _sfx.SetValueWithoutNotify(_settings.SfxVolume);
            _ambient.SetValueWithoutNotify(_settings.AmbientVolume);
            _hints.SetValueWithoutNotify(_settings.ShowKeyHints);
            if (_qualityCount > 0) { _quality.SetValueWithoutNotify(_quality.choices[QualityIndex(_settings.QualityLevel)]); }
        }

        private int QualityIndex(int saved)
        {
            if (_qualityCount <= 0) { return 0; }
            int level = saved >= 0 ? saved : GraphicsQuality.Resolve(saved);
            return level >= _qualityCount ? _qualityCount - 1 : (level < 0 ? 0 : level);
        }

        private static Button MakeButton(string text, Action onClick, bool isPrimary = false)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text };
            button.style.fontSize = isPrimary ? 19f : 16f;
            button.style.unityFontStyleAndWeight = isPrimary ? FontStyle.Bold : FontStyle.Normal;
            button.style.height = isPrimary ? 44f : 36f;
            button.style.marginTop = 8f;
            button.style.borderTopLeftRadius = 8f;
            button.style.borderTopRightRadius = 8f;
            button.style.borderBottomLeftRadius = 8f;
            button.style.borderBottomRightRadius = 8f;
            button.style.borderTopWidth = 1.5f;
            button.style.borderBottomWidth = 1.5f;
            button.style.borderLeftWidth = 1.5f;
            button.style.borderRightWidth = 1.5f;

            if (isPrimary)
            {
                button.style.backgroundColor = new Color(0.95f, 0.48f, 0.10f, 1f); // Brand Orange
                button.style.color = Color.white;
                button.style.borderTopColor = new Color(1.0f, 0.68f, 0.28f, 1f);
                button.style.borderBottomColor = new Color(0.80f, 0.38f, 0.05f, 1f);
                button.style.borderLeftColor = new Color(1.0f, 0.68f, 0.28f, 1f);
                button.style.borderRightColor = new Color(0.80f, 0.38f, 0.05f, 1f);
            }
            else
            {
                button.style.backgroundColor = new Color(0.12f, 0.24f, 0.18f, 0.95f); // Deep Tea Green
                button.style.color = new Color(0.95f, 0.96f, 0.94f, 1f);
                button.style.borderTopColor = new Color(0.24f, 0.44f, 0.34f, 0.8f);
                button.style.borderBottomColor = new Color(0.18f, 0.34f, 0.26f, 0.8f);
                button.style.borderLeftColor = new Color(0.24f, 0.44f, 0.34f, 0.8f);
                button.style.borderRightColor = new Color(0.18f, 0.34f, 0.26f, 0.8f);
            }

            return button;
        }

        private static Slider MakeSlider(VisualElement parent, string caption, float min, float max, float value, Action<float> onChanged, string name = null)
        {
            var label = new Label(caption);
            label.style.color = new Color(0.99f, 0.85f, 0.45f, 1f); // Warm Lime Accent
            label.style.fontSize = 15f;
            label.style.marginTop = 10f;
            label.style.marginBottom = 2f;
            parent.Add(label);

            var slider = new Slider(min, max) { value = value };
            if (name != null) { slider.name = name; }
            slider.style.height = 24f;
            slider.RegisterValueChangedCallback(e => onChanged(e.newValue));
            parent.Add(slider);
            return slider;
        }
    }
}
