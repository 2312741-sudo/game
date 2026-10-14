using System;
using TramChanh.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Menu
{
    /// <summary>
    /// Pause overlay: Resume, volume and look-speed sliders, Credits, Quit. Pure view; the owner decides when to show it
    /// and what Resume, Credits and Quit do, so it can be tested without a scene.
    /// </summary>
    public sealed class PauseMenu
    {
        private readonly VisualElement _panel;
        private readonly Slider _volume;
        private readonly Slider _look;
        private readonly GameSettings _settings;

        public PauseMenu(VisualElement root, GameSettings settings, Action onResume, Action onCredits, Action onQuit, Action onSettingsChanged)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _panel = new VisualElement { name = "PauseMenu" };
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0f;
            _panel.style.right = 0f;
            _panel.style.top = 0f;
            _panel.style.bottom = 0f;
            _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            _panel.style.alignItems = Align.Center;
            _panel.style.justifyContent = Justify.Center;
            _panel.style.display = DisplayStyle.None;

            var box = new VisualElement();
            box.style.width = 420f;
            box.style.paddingTop = 20f;
            box.style.paddingBottom = 20f;
            box.style.paddingLeft = 24f;
            box.style.paddingRight = 24f;
            box.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            _panel.Add(box);

            var title = new Label("TẠM DỪNG");
            title.style.fontSize = 28f;
            title.style.color = Color.white;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 14f;
            box.Add(title);

            box.Add(MakeButton("Tiếp tục", onResume));

            _volume = MakeSlider(box, "Âm lượng", GameSettings.MinVolume, GameSettings.MaxVolume, settings.MasterVolume,
                value => { _settings.MasterVolume = value; onSettingsChanged?.Invoke(); });
            _look = MakeSlider(box, "Tốc độ xoay chuột", GameSettings.MinLookScale, GameSettings.MaxLookScale, settings.LookScale,
                value => { _settings.LookScale = value; onSettingsChanged?.Invoke(); });

            box.Add(MakeButton("Mặc định", () =>
            {
                _settings.ResetToDefaults();
                SyncSliders();
                onSettingsChanged?.Invoke();
            }));
            box.Add(MakeButton("Ghi công (Credits)", onCredits));
            box.Add(MakeButton("Thoát game", onQuit));

            var hint = new Label("Esc: tiếp tục");
            hint.style.fontSize = 13f;
            hint.style.color = new Color(1f, 1f, 1f, 0.6f);
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.style.marginTop = 10f;
            box.Add(hint);

            root.Add(_panel);
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
        }

        private static Button MakeButton(string text, Action onClick)
        {
            var button = new Button(() => onClick?.Invoke()) { text = text };
            button.style.fontSize = 18f;
            button.style.height = 38f;
            button.style.marginTop = 8f;
            return button;
        }

        private static Slider MakeSlider(VisualElement parent, string caption, float min, float max, float value, Action<float> onChanged)
        {
            var label = new Label(caption);
            label.style.color = Color.white;
            label.style.fontSize = 16f;
            label.style.marginTop = 12f;
            parent.Add(label);
            var slider = new Slider(min, max) { value = value };
            slider.RegisterValueChangedCallback(e => onChanged(e.newValue));
            parent.Add(slider);
            return slider;
        }
    }
}
