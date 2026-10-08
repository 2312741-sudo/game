using UnityEngine;

namespace TramChanh.App
{
    /// <summary>Presentation hooks for station tools; owns no workflow state or duration.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class StationToolView : MonoBehaviour
    {
        private static readonly int Active = Animator.StringToHash("Active");
        private static readonly int Action = Animator.StringToHash("Action");
        public void Begin() => GetComponent<Animator>().SetBool(Active, true);
        public void Stop() => GetComponent<Animator>().SetBool(Active, false);
        public void PlayOnce() => GetComponent<Animator>().SetTrigger(Action);
    }
}
