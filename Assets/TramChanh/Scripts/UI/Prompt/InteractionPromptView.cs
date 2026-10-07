using System;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace TramChanh.UI.Prompt
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private PromptLocalizationTable _table;
        [SerializeField] private string _language = "en";
        private IEventBus _events;
        private PlayerInteractor _source;
        private IDisposable _subscription;
        private VisualElement _panel;
        private Label _text;
        private Label _reason;
        private ProgressBar _progress;
        private InteractionQuery _renderedQuery;
        private bool _hasRenderedQuery;
        private bool _renderedHeldUse;
        public PromptLocalizationTable Table => _table;
        public string Language => _language;
        public bool IsPromptVisible => _panel != null && _panel.style.display.value == DisplayStyle.Flex;
        public string PromptText => _text != null ? _text.text : string.Empty;

        public void Initialize(IEventBus events, PlayerInteractor source)
        {
            _events = events;
            _source = source;
        }
        private void Start()
        {
            BuildView();
            Subscribe();
            Render(_source.Prompt);
        }
        private void BuildView()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            _hasRenderedQuery = false;
            root.pickingMode = PickingMode.Ignore;
            var instructions = new Label(_table.Resolve("preview.controls", _language));
            instructions.style.position = Position.Absolute;
            instructions.style.left = 20f;
            instructions.style.top = 20f;
            instructions.style.fontSize = 16f;
            instructions.style.color = Color.white;
            root.Add(instructions);
            var crosshair = new Label("+");
            crosshair.style.position = Position.Absolute;
            crosshair.style.left = Length.Percent(50f);
            crosshair.style.top = Length.Percent(50f);
            crosshair.style.marginLeft = -6f;
            crosshair.style.marginTop = -12f;
            crosshair.style.fontSize = 24f;
            crosshair.style.color = Color.white;
            root.Add(crosshair);
            _panel = new VisualElement();
            _panel.style.position = Position.Absolute;
            _panel.style.bottom = 35f;
            _panel.style.left = Length.Percent(50f);
            _panel.style.width = 620f;
            _panel.style.marginLeft = -310f;
            _panel.style.paddingTop = 12f;
            _panel.style.paddingBottom = 12f;
            _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            _text = new Label();
            _text.style.fontSize = 22f;
            _text.style.color = Color.white;
            _text.style.unityTextAlign = TextAnchor.MiddleCenter;
            _panel.Add(_text);
            _reason = new Label();
            _reason.style.fontSize = 16f;
            _reason.style.color = Color.gray;
            _reason.style.unityTextAlign = TextAnchor.MiddleCenter;
            _panel.Add(_reason);
            _progress = new ProgressBar { lowValue = 0f, highValue = 100f };
            _panel.Add(_progress);
            root.Add(_panel);
        }
        private void Subscribe()
        {
            if (_subscription == null && _events != null && isActiveAndEnabled)
            {
                _subscription = _events.Subscribe<InteractionPromptChanged>(Render);
            }
        }
        private void Render(InteractionPromptChanged state)
        {
            _panel.style.display = state.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!state.IsVisible)
            {
                _text.text = string.Empty;
                _hasRenderedQuery = false;
                return;
            }
            if (!_hasRenderedQuery || !_renderedQuery.Equals(state.Query) || _renderedHeldUse != state.IsHeldUse)
            {
                _text.text = (state.IsHeldUse ? "[F]" : _table.Resolve("preview.key", _language)) + "  " + _table.Resolve(state.Query.PromptKey, _language);
                _reason.text = _table.Resolve(state.Query.BlockedReasonKey, _language);
                _progress.style.display = state.Query.Kind == InteractionKind.Hold ? DisplayStyle.Flex : DisplayStyle.None;
                _renderedQuery = state.Query;
                _renderedHeldUse = state.IsHeldUse;
                _hasRenderedQuery = true;
            }
            _progress.value = state.Progress * 100f;
        }
        private void OnEnable()
        {
            if (_events != null && _panel != null)
            {
                BuildView();
                Subscribe();
                Render(_source.Prompt);
            }
        }
        public void Disconnect()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
        private void OnDisable() => Disconnect();
        private void OnDestroy() => Disconnect();
    }
}
