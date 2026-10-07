using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Interaction.Preview
{
    /// <summary>Press-only visual test adapter. Never dispenses tea or runs a grill.</summary>
    public sealed class InspectionInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _id;
        [SerializeField] private Transform _interactionPoint;
        [SerializeField] private string _inspectPromptKey;
        [SerializeField] private string _resetPromptKey;
        [SerializeField] private string _blockedReasonKey;
        [SerializeField] private AvailabilityStatus _availability;
        [SerializeField] private Color _highlightColor;
        private readonly InspectionState _state = new InspectionState();
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private MaterialPropertyBlock _properties;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public InteractableId Id => new InteractableId(_id);
        public Transform InteractionPoint => _interactionPoint;
        public string InspectPromptKey => _inspectPromptKey;
        public string ResetPromptKey => _resetPromptKey;
        public string BlockedReasonKey => _blockedReasonKey;
        public AvailabilityStatus AvailabilityStatus => _availability;
        public Color HighlightColor => _highlightColor;
        public bool IsHighlighted => _state.IsHighlighted;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
            _properties = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                _originalColors[i] = _renderers[i].sharedMaterial.GetColor(BaseColorId);
            }
        }
        public InteractionQuery Query(InteractionContext context)
        {
            Availability availability = _availability == AvailabilityStatus.Available ? Availability.Available
                : _availability == AvailabilityStatus.Blocked ? Availability.Blocked(_blockedReasonKey) : Availability.Hidden;
            return new InteractionQuery(availability, _state.IsHighlighted ? _resetPromptKey : _inspectPromptKey);
        }
        public void Execute(InteractionContext context)
        {
            InteractionQuery query = Query(context);
            if (!query.Availability.IsAvailable)
            {
                if (query.Availability.Status == AvailabilityStatus.Blocked)
                {
                    context.Events.Publish(new ActionBlocked(Id, query.BlockedReasonKey));
                }
                return;
            }
            if (context.Clock.IsPaused)
            {
                return;
            }
            _state.Toggle();
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].GetPropertyBlock(_properties);
                _properties.SetColor(BaseColorId, _state.IsHighlighted ? _highlightColor : _originalColors[i]);
                _renderers[i].SetPropertyBlock(_properties);
            }
        }
    }
}
