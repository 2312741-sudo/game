using UnityEngine;

namespace TramChanh.Interaction.Player
{
    /// <summary>
    /// Procedural upper-body gestures for the player avatar. Each gesture gives, per arm, the direction the upper arm and the forearm
    /// point in chest space (x right, y up, z forward); the pose is blended over whatever the Animator wrote this frame, so walking
    /// legs keep animating while the hands do the work.
    /// </summary>
    public sealed class PlayerArmPose
    {
        public enum Gesture { None, Carry, Reach, Pour, Lid, Flip, Cut, Sauce, Roll, Wrap, Wipe }

        private struct Arm { public Vector3 Upper, Fore; }

        private sealed class Limb
        {
            public Transform Upper, Lower;
            public float Side;   // +1 right arm (rest axis +X), -1 left arm (rest axis -X)
        }

        private readonly Limb _right = new Limb { Side = 1f };
        private readonly Limb _left = new Limb { Side = -1f };
        private Transform _chest;
        private Gesture _gesture = Gesture.None;
        private float _time, _duration;
        private float _weight;
        private bool _carrying;
        private bool _loop;

        public bool IsActive => _weight > 0.001f;

        public bool Bind(Transform root)
        {
            _right.Upper = Find(root, "RightUpperArm"); _right.Lower = Find(root, "RightLowerArm");
            _left.Upper = Find(root, "LeftUpperArm"); _left.Lower = Find(root, "LeftLowerArm");
            _chest = Find(root, "Chest");
            return _chest != null && _right.Upper != null && _right.Lower != null && _left.Upper != null && _left.Lower != null;
        }

        public void SetCarrying(bool carrying) { _carrying = carrying; }

        /// <summary>Starts a gesture. A looping gesture runs until <see cref="Stop"/>; a one-shot lasts <paramref name="duration"/> seconds.</summary>
        public void Play(Gesture gesture, float duration, bool loop)
        {
            _gesture = gesture; _time = 0f; _duration = Mathf.Max(0.2f, duration); _loop = loop;
        }

        public void Stop() { _gesture = Gesture.None; }

        /// <summary>Maps a prompt key (cake.cut, grill.open, ...) to a gesture; unknown keys become a plain reach.</summary>
        public static Gesture ForPrompt(string key)
        {
            if (string.IsNullOrEmpty(key)) { return Gesture.None; }
            if (key == "cake.fill" || key == "cake.pour") { return Gesture.Pour; }
            if (key.StartsWith("grill.")) { return Gesture.Lid; }
            if (key == "cake.flip" || key == "cake.remove_burnt") { return Gesture.Flip; }
            if (key == "cake.cut") { return Gesture.Cut; }
            if (key == "cake.sauce") { return Gesture.Sauce; }
            if (key == "cake.roll_vertical") { return Gesture.Roll; }
            if (key == "cake.wrap") { return Gesture.Wrap; }
            if (key.Contains("wipe")) { return Gesture.Wipe; }
            return Gesture.Reach;
        }

        /// <summary>Call from LateUpdate, after the Animator has posed the skeleton.</summary>
        public void Apply(float deltaTime)
        {
            bool playing = _gesture != Gesture.None;
            Gesture shown = playing ? _gesture : (_carrying ? Gesture.Carry : Gesture.None);
            if (playing)
            {
                _time += deltaTime;
                if (!_loop && _time >= _duration) { _gesture = Gesture.None; playing = false; shown = _carrying ? Gesture.Carry : Gesture.None; }
            }
            float target = shown == Gesture.None ? 0f : 1f;
            _weight = Mathf.MoveTowards(_weight, target, deltaTime * (shown == Gesture.None ? 4f : 7f));
            if (_weight <= 0.001f || _chest == null) { return; }

            float phase = _loop ? _time : Mathf.Clamp01(_time / _duration);
            Evaluate(shown, phase, out Arm right, out Arm left);
            Drive(_right, right);
            Drive(_left, left);
        }

        private void Drive(Limb limb, Arm arm)
        {
            Vector3 rest = new Vector3(limb.Side, 0f, 0f);
            Vector3 upper = ToChest(arm.Upper, limb.Side).normalized;
            Quaternion upperRot = Quaternion.FromToRotation(rest, upper);
            Vector3 foreInUpper = Quaternion.Inverse(upperRot) * ToChest(arm.Fore, limb.Side).normalized;
            Quaternion lowerRot = Quaternion.FromToRotation(rest, foreInUpper);
            limb.Upper.localRotation = Quaternion.Slerp(limb.Upper.localRotation, upperRot, _weight);
            limb.Lower.localRotation = Quaternion.Slerp(limb.Lower.localRotation, lowerRot, _weight);
        }

        // Poses are written for the right arm; the left arm mirrors them across the chest's x = 0 plane.
        private static Vector3 ToChest(Vector3 v, float side) => new Vector3(v.x * side, v.y, v.z);

        private static void Evaluate(Gesture g, float t, out Arm right, out Arm left)
        {
            Arm hang = new Arm { Upper = new Vector3(0.25f, -1f, 0.05f), Fore = new Vector3(0.2f, -1f, 0.35f) };
            right = hang; left = hang;
            float s = Mathf.Sin(t * Mathf.PI);                 // 0 → 1 → 0 across a one-shot
            float wave = Mathf.Sin(t * 6f * Mathf.PI);         // fast oscillation over a loop / hold
            switch (g)
            {
                case Gesture.Carry:
                    right = new Arm { Upper = new Vector3(0.3f, -0.8f, 0.5f), Fore = new Vector3(-0.1f, 0.25f, 1f) };
                    break;
                case Gesture.Reach:
                    right = new Arm { Upper = new Vector3(0.2f, -0.35f, 1f), Fore = new Vector3(0f, -0.15f + 0.1f * s, 1f) };
                    break;
                case Gesture.Pour:   // wrist rolls the cup over the pan
                    right = new Arm { Upper = new Vector3(0.3f, -0.4f, 1f), Fore = new Vector3(-0.2f * s, 0.2f + 0.6f * s, 1f) };
                    left = new Arm { Upper = new Vector3(0.2f, -0.6f, 0.6f), Fore = new Vector3(-0.2f, -0.1f, 1f) };
                    break;
                case Gesture.Lid:    // lift or lower the lid with a hooked hand
                    right = new Arm { Upper = new Vector3(0.25f, 0.1f + 0.4f * s, 1f), Fore = new Vector3(0f, 0.6f * s, 1f) };
                    break;
                case Gesture.Flip:   // slide the spatula under the cake and turn the wrist
                    right = new Arm { Upper = new Vector3(0.25f, -0.35f, 1f), Fore = new Vector3(0f, -0.3f + 1.1f * Mathf.Sin(t * Mathf.PI), 1f - 0.5f * s) };
                    left = new Arm { Upper = new Vector3(0.2f, -0.7f, 0.5f), Fore = new Vector3(-0.1f, -0.2f, 1f) };
                    break;
                case Gesture.Cut:    // scissor snips
                    right = new Arm { Upper = new Vector3(0.25f, -0.3f, 1f), Fore = new Vector3(-0.15f, -0.2f + 0.18f * Mathf.Abs(wave), 1f) };
                    left = new Arm { Upper = new Vector3(0.1f, -0.4f, 1f), Fore = new Vector3(-0.1f, -0.15f, 1f) };
                    break;
                case Gesture.Sauce:  // squeeze the bag, sweeping side to side over the cake
                    right = new Arm { Upper = new Vector3(0.1f + 0.3f * wave, -0.4f, 1f), Fore = new Vector3(-0.3f * wave, -0.35f, 1f) };
                    left = new Arm { Upper = new Vector3(0.05f, -0.45f, 1f), Fore = new Vector3(-0.2f, -0.25f, 1f) };
                    break;
                case Gesture.Roll:   // both hands push forward along the cake
                    right = new Arm { Upper = new Vector3(0.15f, -0.45f, 1f), Fore = new Vector3(-0.3f, -0.4f + 0.25f * Mathf.Abs(wave), 1f) };
                    left = new Arm { Upper = new Vector3(0.15f, -0.45f, 1f), Fore = new Vector3(-0.3f, -0.4f + 0.25f * Mathf.Abs(wave), 1f) };
                    break;
                case Gesture.Wrap:   // hands meet and fold
                    right = new Arm { Upper = new Vector3(0.1f, -0.5f, 1f), Fore = new Vector3(-0.6f * s, -0.3f, 1f) };
                    left = right;
                    break;
                case Gesture.Wipe:
                    right = new Arm { Upper = new Vector3(0.2f, -0.45f, 1f), Fore = new Vector3(-0.5f * wave, -0.4f, 1f) };
                    break;
            }
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) { if (t.name == name) { return t; } }
            return null;
        }
    }
}
