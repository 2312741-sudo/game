using System;
using TramChanh.Core;
using TramChanh.Interaction;
using TramChanh.Orders;
using TramChanh.UI.Hud;
using TramChanh.Interaction.Player;
using TramChanh.UI.Localization;
using TramChanh.UI.Menu;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private SupplierPanel _supplier;
        private Func<OrderId, double> _patienceProvider;
        private Func<OrderId, string> _customerIconProvider;
        private Label _hint;
        private Label _heldHint;
        private Label _status;
        private Label _notice;
        private string _noticeText = string.Empty;
        private bool _noticePositive = true;
        private float _noticeUntil;
        private Label _credits;
        private VisualElement _crosshair;
        private PauseMenu _pauseMenu;
        private PauseMenu _titleMenu;
        private DaySummaryPanel _summary;
        private Action _titlePlay;
        private string _titleInfo = string.Empty;
        private bool _titleVisible;
        private PlayerInputReader _pauseInput;
        private GameSettings _settings;
        private Action _applySettings;
        private Action _quit;
        private string _statusText = string.Empty;
        public PromptLocalizationTable Table => _table;
        public string Language => _language;
        public bool IsPromptVisible => _panel != null && _panel.style.display.value == DisplayStyle.Flex;
        public string PromptText => _text != null ? _text.text : string.Empty;
        public string HeldPromptText => _heldText != null ? _heldText.text : string.Empty;
        /// <summary>True after the player HUD overload of Initialize; legacy scenes keep the prompt-only view.</summary>
        public bool IsHudEnabled => _hudEnabled;
        public PlayerHudPresenter Hud => _hud;

        /// <summary>Gives the order list a way to read each order's patience (share left, negative when not tracked).</summary>
        /// <summary>Gives the order list the icon name of each order's customer kind (null when unknown).</summary>
        public void SetCustomerIconProvider(Func<OrderId, string> provider)
        {
            _customerIconProvider = provider;
            if (_hud != null) { _hud.CustomerIconProvider = provider; }
        }

        public void SetPatienceProvider(Func<OrderId, double> provider)
        {
            _patienceProvider = provider;
            if (_hud != null) { _hud.PatienceProvider = provider; }
        }
        public string HeldItemText => _hud != null ? _hud.HeldText : string.Empty;
        public string OrderListText => _hud != null ? _hud.OrdersText : string.Empty;
        public string ToastText => _hud != null ? _hud.ToastText : string.Empty;

        /// <summary>Text shown top centre (the game clock). Safe to call every frame; the label only changes when the text does.</summary>
        public void SetStatus(string text)
        {
            _statusText = text ?? string.Empty;
            if (_status != null && _status.text != _statusText) { _status.text = _statusText; }
        }

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
            HudTheme.StyleChip(instructions, 14f);
            instructions.style.left = HudTheme.Margin;
            root.Add(instructions);
            _status = new Label(_statusText);
            HudTheme.StyleReadout(_status);
            root.Add(_status);
            _notice = new Label();
            HudTheme.StyleToast(_notice, 620f, _noticePositive);
            _notice.style.top = HudTheme.NoticeTop;
            _notice.style.display = DisplayStyle.None;
            root.Add(_notice);
            ApplyNotice();
            _credits = new Label(CreditsText.Build() + "\n\n[F1] close");
            _credits.pickingMode = PickingMode.Ignore;
            _credits.style.position = Position.Absolute;
            _credits.style.left = Length.Percent(50f);
            _credits.style.top = 80f;
            _credits.style.width = 780f;
            _credits.style.marginLeft = -390f;
            _credits.style.paddingTop = 18f;
            _credits.style.paddingBottom = 18f;
            _credits.style.paddingLeft = 22f;
            _credits.style.paddingRight = 22f;
            _credits.style.fontSize = 14f;
            _credits.style.color = new Color(0.95f, 0.96f, 0.94f, 1f);
            _credits.style.whiteSpace = WhiteSpace.Normal;
            _credits.style.backgroundColor = new Color(0.08f, 0.18f, 0.13f, 0.96f);
            _credits.style.borderTopWidth = 2f;
            _credits.style.borderBottomWidth = 2f;
            _credits.style.borderLeftWidth = 2f;
            _credits.style.borderRightWidth = 2f;
            _credits.style.borderTopColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            _credits.style.borderBottomColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            _credits.style.borderLeftColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            _credits.style.borderRightColor = new Color(0.95f, 0.48f, 0.10f, 0.90f);
            _credits.style.borderTopLeftRadius = 14f;
            _credits.style.borderTopRightRadius = 14f;
            _credits.style.borderBottomLeftRadius = 14f;
            _credits.style.borderBottomRightRadius = 14f;
            _credits.style.display = DisplayStyle.None;
            root.Add(_credits);
            var creditsHint = new Label("[F1] Credits");
            HudTheme.StyleChip(creditsHint, 13f);
            creditsHint.style.right = HudTheme.Margin;
            root.Add(creditsHint);
            var crosshair = new VisualElement();
            crosshair.style.position = Position.Absolute;
            crosshair.style.left = Length.Percent(50f);
            crosshair.style.top = Length.Percent(50f);
            _crosshair = crosshair;
            HudTheme.StyleCrosshair(crosshair, false);
            root.Add(crosshair);
            _panel = new VisualElement();
            _panel.style.position = Position.Absolute;
            _panel.style.bottom = 35f;
            _panel.style.left = Length.Percent(50f);
            _panel.style.width = 620f;
            _panel.style.marginLeft = -310f;
            UiStyle.MakeCard(_panel, 14f, 10f);
            _text = new Label();
            _text.style.fontSize = HudTheme.PromptSize;
            _text.style.unityFontStyleAndWeight = FontStyle.Bold;
            _text.style.color = HudTheme.Text;
            _text.style.unityTextAlign = TextAnchor.MiddleCenter;
            _panel.Add(_text);
            _reason = new Label();
            HudTheme.StyleReason(_reason);
            _reason.style.display = DisplayStyle.None;
            _panel.Add(_reason);
            _progress = new ProgressBar { lowValue = 0f, highValue = 100f };
            HudTheme.StyleProgress(_progress);
            _panel.Add(_progress);
            _heldPanel = new VisualElement();
            _heldText = new Label(); _heldText.style.fontSize = HudTheme.PromptSize; _heldText.style.unityFontStyleAndWeight = FontStyle.Bold; _heldText.style.color = HudTheme.Text; _heldText.style.marginTop = 6f; _heldText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _heldReason = new Label(); HudTheme.StyleReason(_heldReason); _heldReason.style.display = DisplayStyle.None;
            _heldProgress = new ProgressBar { lowValue = 0f, highValue = 100f };
            HudTheme.StyleProgress(_heldProgress);
            _heldPanel.Add(_heldText); _heldPanel.Add(_heldReason); _heldPanel.Add(_heldProgress);
            _panel.Add(_heldPanel);
            _hasRenderedHeldQuery = false;
            root.Add(_panel);
            BuildHud(root);
            BuildPauseMenu(root);
            BuildTitle(root);
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
            _hint.style.fontSize = HudTheme.BodySize;
            _hint.style.color = HudTheme.Hint;
            _hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            _panel.Insert(_panel.IndexOf(_progress) + 1, _hint);
            _heldHint = new Label();
            _heldHint.style.fontSize = HudTheme.BodySize;
            _heldHint.style.color = HudTheme.Hint;
            _heldHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            _heldPanel.Add(_heldHint);
            IHeldItemSlot hands = _source != null && _source.Context != null ? _source.Context.Hands : null;
            _hud = new PlayerHudPresenter(root, key => _table.Resolve(key, _language), _table.Contains, _events, hands, _orders, null);
            _hud.PatienceProvider = _patienceProvider;
            _hud.CustomerIconProvider = _customerIconProvider;
        }

        /// <summary>
        /// Adds the pause menu (Esc). It shows while the player is paused by the user. <paramref name="applySettings"/> runs after
        /// any option changes so the game can apply and save them; <paramref name="quit"/> is the Quit button.
        /// </summary>
        public void AttachPauseMenu(PlayerInputReader input, GameSettings settings, Action applySettings, Action quit)
        {
            _pauseInput = input;
            _settings = settings;
            _applySettings = applySettings;
            _quit = quit;
            if (_panel != null) { BuildPauseMenu(GetComponent<UIDocument>().rootVisualElement); }
        }

        /// <summary>
        /// Title screen shown before the first day: Play, volume, look speed, credits, quit. Call after AttachPauseMenu.
        /// <paramref name="info"/> is the small line under the title (day and totals).
        /// </summary>
        public void ShowTitle(Action play, string info)
        {
            _titlePlay = play;
            _titleInfo = info ?? string.Empty;
            _titleVisible = true;
            if (_panel != null) { BuildTitle(GetComponent<UIDocument>().rootVisualElement); }
        }

        /// <summary>Shows the end-of-day card. <paramref name="onContinue"/> runs when the player presses the button.</summary>
        public void ShowDaySummary(DaySummary summary, GameProgress progress, Action onContinue, string extra = null, int stars = -1)
        {
            _summary?.Hide();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _summary = new DaySummaryPanel(root, () => { _summary.Hide(); onContinue?.Invoke(); });
            _summary.Show(summary, progress, extra, stars);
        }

        /// <summary>
        /// Shows the shop between days (stock, upgrades, difficulty). <paramref name="onDone"/> runs on the open-the-stall button.
        /// </summary>
        public void ShowSupplier(SupplierContext context, Action onDone)
        {
            _supplier?.Hide();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _supplier = new SupplierPanel(root, context, () => { _supplier.Hide(); onDone?.Invoke(); });
            _supplier.Show();
        }

        private void BuildTitle(VisualElement root)
        {
            _titleMenu = null;
            if (!_titleVisible || _settings == null) { return; }
            _titleMenu = new PauseMenu(root, _settings,
                () =>
                {
                    _titleVisible = false;
                    _titleMenu.SetVisible(false);
                    _titlePlay?.Invoke();
                },
                ToggleCredits,
                () => _quit?.Invoke(),
                () => _applySettings?.Invoke(),
                "TRẠM CHANH", "Chơi", "Trà trái cây · Bánh lăn");
            _titleMenu.SetInfo(_titleInfo);
            _titleMenu.SetVisible(true);
        }

        private void BuildPauseMenu(VisualElement root)
        {
            _pauseMenu = null;
            if (_pauseInput == null || _settings == null) { return; }
            _pauseInput.ClickResumesFromPause = false;
            _pauseMenu = new PauseMenu(root, _settings,
                () => _pauseInput.SetCaptured(true),
                ToggleCredits,
                () => _quit?.Invoke(),
                () => _applySettings?.Invoke());
        }

        /// <summary>Shows or hides the attribution panel the CC-BY licences require.</summary>
        public void ToggleCredits()
        {
            if (_credits == null) { return; }
            _credits.style.display = _credits.style.display.value == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
            _credits.BringToFront();
        }

        /// <summary>Shows a short message under the clock for a few seconds (stock ran out, a star was won, the critic came...).</summary>
        public void ShowNotice(string text, bool positive = true, float seconds = 4f)
        {
            _noticeText = text ?? string.Empty;
            _noticePositive = positive;
            _noticeUntil = Time.unscaledTime + Mathf.Max(0.5f, seconds);
            ApplyNotice();
        }

        private void ApplyNotice()
        {
            if (_notice == null) { return; }
            bool show = !string.IsNullOrEmpty(_noticeText) && Time.unscaledTime < _noticeUntil;
            _notice.text = _noticeText;
            _notice.style.color = Color.white;
            _notice.style.backgroundColor = HudTheme.ToastColor(_noticePositive);
            _notice.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>The player pressed R: buy a small pack at the corner shop. The game decides whether it works right now.</summary>
        public event Action EmergencyRestockRequested;

        public string NoticeText => _notice != null && _notice.style.display.value == DisplayStyle.Flex ? _notice.text : string.Empty;

        private void Update()
        {
            if (_notice != null && !string.IsNullOrEmpty(_noticeText) && Time.unscaledTime >= _noticeUntil)
            {
                _noticeText = string.Empty;
                ApplyNotice();
            }
            _hud?.Tick();
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame) { ToggleCredits(); }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) { EmergencyRestockRequested?.Invoke(); }
            if (_pauseMenu != null)
            {
                bool paused = _pauseInput != null && _pauseInput.IsPausedByUser;
                _pauseMenu.SetVisible(paused && !_titleVisible);
                // Closing the pause menu closes the credits panel with it.
                if (!paused && !_titleVisible && _credits != null) { _credits.style.display = DisplayStyle.None; }
            }
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
            if (state.IsVisible && _crosshair != null) { HudTheme.StyleCrosshair(_crosshair, string.IsNullOrEmpty(state.Query.BlockedReasonKey)); }
            if (!state.IsVisible)
            {
                _text.text = string.Empty;
                _heldText.text = string.Empty;
                if (_crosshair != null) { HudTheme.StyleCrosshair(_crosshair, false); }
                _hasRenderedQuery = false;
                _hasRenderedHeldQuery = false;
                return;
            }
            if (!_hasRenderedQuery || !_renderedQuery.Equals(state.Query) || _renderedHeldUse != state.IsHeldUse)
            {
                _text.text = _table.Resolve(state.IsHeldUse ? "preview.held_key" : "preview.key", _language) + "  " + _table.Resolve(state.Query.PromptKey, _language);
                HudTheme.SetReasonText(_reason, string.IsNullOrEmpty(state.Query.BlockedReasonKey) ? string.Empty : _table.Resolve(state.Query.BlockedReasonKey, _language));
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
                HudTheme.SetReasonText(_heldReason, string.IsNullOrEmpty(state.HeldQuery.BlockedReasonKey) ? string.Empty : _table.Resolve(state.HeldQuery.BlockedReasonKey, _language));
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
