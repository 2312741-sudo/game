using System;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using TramChanh.UI.Hud;
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
        private VisualElement _heldPanel;
        private Label _heldText;
        private Label _heldReason;
        private ProgressBar _heldProgress;
        private InteractionQuery _renderedHeldQuery;
        private bool _hasRenderedHeldQuery;
        private InteractionQuery _renderedQuery;
        private bool _hasRenderedQuery;
        private bool _renderedHeldUse;
        private bool _hudEnabled;
        private IOrderService _orders;
        private PlayerHudPresenter _hud;
        private Label _hint;
        private Label _heldHint;
        public PromptLocalizationTable Table => _table;
        public string Language => _language;
        public bool IsPromptVisible => _panel != null && _panel.style.display.value == DisplayStyle.Flex;
        public string PromptText => _text != null ? _text.text : string.Empty;
        public string HeldPromptText => _heldText != null ? _heldText.text : string.Empty;
        /// <summary>True after the player HUD overload of Initialize; legacy scenes keep the prompt-only view.</summary>
        public bool IsHudEnabled => _hudEnabled;
        public PlayerHudPresenter Hud => _hud;
        public string HeldItemText => _hud != null ? _hud.HeldText : string.Empty;
        public string OrderListText => _hud != null ? _hud.OrdersText : string.Empty;
        public string ToastText => _hud != null ? _hud.ToastText : string.Empty;

        public void Initialize(IEventBus events, PlayerInteractor source)
        {
            _events = events;
            _source = source;
        }

        /// <summary>
        /// Prompt plus the player HUD (held item, live orders, order toast, hold captions). The hand slot is read
        /// from <paramref name="source"/>.Context, so call this after PlayerInteractor.Initialize. Orders are
        /// used through read-only queries only; pass null to show the held-item panel alone.
        /// </summary>
        public void Initialize(IEventBus events, PlayerInteractor source, IOrderService orders)
        {
            Initialize(events, source);
            _orders = orders;
            _hudEnabled = true;
            if (_panel != null && isActiveAndEnabled)
            {
                BuildView();
                Subscribe();
                Render(_source.Prompt);
            }
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
            _heldPanel = new VisualElement();
            _heldText = new Label(); _heldText.style.fontSize = 22f; _heldText.style.color = Color.white; _heldText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _heldReason = new Label(); _heldReason.style.fontSize = 16f; _heldReason.style.color = Color.gray; _heldReason.style.unityTextAlign = TextAnchor.MiddleCenter;
            _heldProgress = new ProgressBar { lowValue = 0f, highValue = 100f };
            _heldPanel.Add(_heldText); _heldPanel.Add(_heldReason); _heldPanel.Add(_heldProgress);
            _panel.Add(_heldPanel);
            _hasRenderedHeldQuery = false;
            root.Add(_panel);
            BuildHud(root);
        }

        private void BuildHud(VisualElement root)
        {
            _hud?.Dispose();
            _hud = null;
            _hint = null;
            _heldHint = null;
            if (!_hudEnabled || _events == null)
            {
                return;
            }
            _hint = new Label();
            _hint.style.fontSize = 15f;
            _hint.style.color = new Color(1f, 0.85f, 0.4f);
            _hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            _panel.Insert(_panel.IndexOf(_progress) + 1, _hint);
            _heldHint = new Label();
            _heldHint.style.fontSize = 15f;
            _heldHint.style.color = new Color(1f, 0.85f, 0.4f);
            _heldHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            _heldPanel.Add(_heldHint);
            IHeldItemSlot hands = _source != null && _source.Context != null ? _source.Context.Hands : null;
            _hud = new PlayerHudPresenter(root, key => _table.Resolve(key, _language), _table.Contains, _events, hands, _orders, null);
        }

        private void Update() => _hud?.Tick();
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
                _heldText.text = string.Empty;
                _hasRenderedQuery = false;
                _hasRenderedHeldQuery = false;
                return;
            }
            if (!_hasRenderedQuery || !_renderedQuery.Equals(state.Query) || _renderedHeldUse != state.IsHeldUse)
            {
                _text.text = _table.Resolve(state.IsHeldUse ? "preview.held_key" : "preview.key", _language) + "  " + _table.Resolve(state.Query.PromptKey, _language);
                _reason.text = _table.Resolve(state.Query.BlockedReasonKey, _language);
                _progress.style.display = state.Query.Kind == InteractionKind.Hold ? DisplayStyle.Flex : DisplayStyle.None;
                _renderedQuery = state.Query;
                _renderedHeldUse = state.IsHeldUse;
                _hasRenderedQuery = true;
            }
            _progress.value = state.Progress * 100f;
            if (_hudEnabled && _hint != null)
            {
                RenderHold(_progress, _hint, state.Query, state.Progress);
            }
            _heldPanel.style.display = state.HasHeldPrompt ? DisplayStyle.Flex : DisplayStyle.None;
            if (!state.HasHeldPrompt)
            {
                _heldText.text = string.Empty; _hasRenderedHeldQuery = false; return;
            }
            if (!_hasRenderedHeldQuery || !_renderedHeldQuery.Equals(state.HeldQuery))
            {
                _heldText.text = _table.Resolve("preview.held_key", _language) + "  " + _table.Resolve(state.HeldQuery.PromptKey, _language);
                _heldReason.text = _table.Resolve(state.HeldQuery.BlockedReasonKey, _language);
                _heldProgress.style.display = state.HeldQuery.Kind == InteractionKind.Hold ? DisplayStyle.Flex : DisplayStyle.None;
                _renderedHeldQuery = state.HeldQuery; _hasRenderedHeldQuery = true;
            }
            _heldProgress.value = state.HeldProgress * 100f;
            if (_hudEnabled && _heldHint != null)
            {
                RenderHold(_heldProgress, _heldHint, state.HeldQuery, state.HeldProgress);
            }
        }
        private void RenderHold(ProgressBar bar, Label hint, InteractionQuery query, float progress)
        {
            bool showBar = HoldProgressModel.ShowsBar(query);
            string caption = HoldProgressModel.Caption(query, progress).Render(key => _table.Resolve(key, _language));
            bar.style.display = showBar ? DisplayStyle.Flex : DisplayStyle.None;
            bar.title = showBar ? caption : string.Empty;
            hint.text = showBar ? string.Empty : caption;
            hint.style.display = string.IsNullOrEmpty(hint.text) ? DisplayStyle.None : DisplayStyle.Flex;
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
            _hud?.Dispose();
            _hud = null;
        }
        private void OnDisable() => Disconnect();
        private void OnDestroy() => Disconnect();
    }
}
