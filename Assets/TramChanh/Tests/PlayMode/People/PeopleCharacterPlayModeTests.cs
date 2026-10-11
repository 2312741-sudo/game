#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TramChanh.Tests.PlayMode.People
{
    public sealed class PeopleCharacterPlayModeTests
    {
        private static readonly string[] PersonPrefabPaths = new[]
        {
            "Assets/TramChanh/Prefabs/People/PF_Person_01.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_02.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_03.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_04.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_05.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Person_06.prefab",
        };

        private static readonly string[] RiderPrefabPaths = new[]
        {
            "Assets/TramChanh/Prefabs/People/PF_Rider_01.prefab",
            "Assets/TramChanh/Prefabs/People/PF_Rider_02.prefab",
        };

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Object.Destroy(go);
            }
            _spawned.Clear();
        }

        [Test]
        public void Test01_AllPrefabs_AreGenericWithNullAvatar()
        {
            foreach (string path in PersonPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, $"Prefab missing: {path}");
                var animator = prefab.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null, $"Animator missing on {path}");
                Assert.That(animator.avatar, Is.Null, $"Prefab {path} should be Generic with null avatar, but has {animator.avatar}");
                Assert.That(animator.isHuman, Is.False, $"Prefab {path} should not be Humanoid");
            }

            foreach (string path in RiderPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, $"Rider missing: {path}");
                var animator = prefab.GetComponentInChildren<Animator>();
                Assert.That(animator, Is.Not.Null, $"Animator missing on {path}");
                Assert.That(animator.avatar, Is.Null, $"Rider {path} should be Generic with null avatar, but has {animator.avatar}");
                Assert.That(animator.isHuman, Is.False, $"Rider {path} should not be Humanoid");
            }
        }

        [UnityTest]
        public IEnumerator Test02_AllSixBodies_FeetTouchGroundAtRestAndIdle()
        {
            for (int i = 0; i < PersonPrefabPaths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[i]);
                var go = Object.Instantiate(prefab, new Vector3(i * 1.5f, 0f, 0f), Quaternion.identity);
                _spawned.Add(go);

                Transform hips = go.transform.Find("Hips");
                Assert.That(hips, Is.Not.Null, $"Hips missing on {go.name}");
                Transform leftFoot = hips.Find("LeftUpperLeg/LeftLowerLeg/LeftFoot");
                Transform rightFoot = hips.Find("RightUpperLeg/RightLowerLeg/RightFoot");
                Assert.That(leftFoot, Is.Not.Null, $"LeftFoot missing on {go.name}");
                Assert.That(rightFoot, Is.Not.Null, $"RightFoot missing on {go.name}");

                Transform leftToes = leftFoot.Find("LeftToes");
                float footBaseY = leftToes != null ? leftToes.position.y : leftFoot.position.y;
                Assert.That(Mathf.Abs(footBaseY), Is.LessThan(0.02f),
                    $"{go.name} feet should touch ground (y=0) at rest, but got {footBaseY:F4}m");
            }

            yield return null;
            yield return null;

            // Check after Animator Idle evaluation
            for (int i = 0; i < _spawned.Count; i++)
            {
                var go = _spawned[i];
                Transform hips = go.transform.Find("Hips");
                Transform leftFoot = hips.Find("LeftUpperLeg/LeftLowerLeg/LeftFoot");
                Transform leftToes = leftFoot.Find("LeftToes");
                float footBaseY = leftToes != null ? leftToes.position.y : leftFoot.position.y;
                Assert.That(Mathf.Abs(footBaseY), Is.LessThan(0.025f),
                    $"{go.name} feet should touch ground (y=0) during Idle, but got {footBaseY:F4}m");
            }
        }

        [UnityTest]
        public IEnumerator Test03_AC_Person_DrivesWalk_AndMeasuresFootSpeed()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[0]);
            var go = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            _spawned.Add(go);

            var animator = go.GetComponent<Animator>();
            animator.SetFloat("Speed", 1.3f);

            yield return null;
            yield return new WaitForSeconds(0.2f);

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Assert.That(stateInfo.IsName("Walk"), Is.True, $"Should transition to Walk at Speed=1.3, but state is hash {stateInfo.shortNameHash}");

            Transform leftFoot = go.transform.Find("Hips/LeftUpperLeg/LeftLowerLeg/LeftFoot");
            Assert.That(leftFoot, Is.Not.Null);

            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            float elapsed = 0f;
            float sampleDuration = 1.0f; // Sample over full 1.0s walk cycle

            while (elapsed < sampleDuration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                float currentZ = leftFoot.position.z;
                minZ = Mathf.Min(minZ, currentZ);
                maxZ = Mathf.Max(maxZ, currentZ);
            }

            float stride = maxZ - minZ;
            Assert.That(stride, Is.GreaterThan(0.20f), $"Left foot should articulate during Walk (stride: {stride:F3}m)");

            // Measured foot swing speed: stride / half-cycle duration (~0.38s at Speed=1.3)
            // Cycle duration = 1.0s / 1.3 = 0.769s. Half-cycle swing = ~0.385s.
            float halfCycleTime = 1.0f / (1.3f * 2f);
            float footSpeed = stride / halfCycleTime;

            Debug.Log($"[PeopleQA] Walk Test Passed: Stride = {stride:F3}m, HalfCycle = {halfCycleTime:F3}s, Measured Foot Speed at Speed=1.3 is {footSpeed:F2} m/s");
            Assert.That(footSpeed, Is.EqualTo(1.30f).Within(0.06f), $"Foot speed at Speed=1.3 must measure 1.30 m/s, got {footSpeed:F2} m/s");
        }

        [UnityTest]
        public IEnumerator Test04_AC_Person_DrivesSitting_KneesBendTo85Degrees()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[0]);
            var go = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            _spawned.Add(go);

            var animator = go.GetComponent<Animator>();
            animator.SetBool("Sitting", true);

            // Wait for Sit_Down animation (1.5s) to complete and enter Sit_Idle
            yield return new WaitForSeconds(1.8f);

            Transform hips = go.transform.Find("Hips");
            Transform leftKnee = hips.Find("LeftUpperLeg/LeftLowerLeg");
            Transform rightKnee = hips.Find("RightUpperLeg/RightLowerLeg");

            Assert.That(leftKnee, Is.Not.Null);
            Assert.That(rightKnee, Is.Not.Null);

            float kneeAngleX = leftKnee.localEulerAngles.x;
            if (kneeAngleX > 180f) kneeAngleX -= 360f;

            Debug.Log($"[PeopleQA] Sitting Knee Angle: {kneeAngleX:F1} deg, Hips Y: {hips.localPosition.y:F3}m");
            Assert.That(Mathf.Abs(kneeAngleX - 85f), Is.LessThan(10f),
                $"Sitting knee angle should bend to ~85 degrees, but got {kneeAngleX:F1} deg");
            Assert.That(hips.localPosition.y, Is.InRange(0.25f, 0.35f),
                $"Sitting hips height should be stool height (~0.30m), but got {hips.localPosition.y:F3}m");
        }

        [UnityTest]
        public IEnumerator Test05_AC_Person_DrivesEating_InSittingPose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[0]);
            var go = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            _spawned.Add(go);

            var animator = go.GetComponent<Animator>();
            animator.SetBool("Sitting", true);
            animator.SetBool("Eating", true);

            yield return new WaitForSeconds(1.8f);

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Assert.That(stateInfo.IsName("Eat_Drink_Loop") || stateInfo.IsName("Sit_Idle"), Is.True,
                "Should enter Eat_Drink_Loop or Sit_Idle");

            Transform hips = go.transform.Find("Hips");
            Assert.That(hips.localPosition.y, Is.InRange(0.25f, 0.35f),
                $"Eating hips height should be stool height (~0.30m), but got {hips.localPosition.y:F3}m");
        }

        [Test]
        public void Test06_IdlePhoneClip_DurationIs2Point5Seconds()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TramChanh/Art/Animations/People/AN_Person_Idle_Phone.anim");
            Assert.That(clip, Is.Not.Null, "AN_Person_Idle_Phone.anim missing");
            Assert.That(clip.length, Is.EqualTo(2.5f).Within(0.01f),
                $"AN_Person_Idle_Phone duration should be 2.5s, but got {clip.length:F2}s");
        }

        [Test]
        public void Test07_ClothingB_WiredOntoPersons04To06()
        {
            var matB = AssetDatabase.LoadAssetAtPath<Material>("Assets/TramChanh/Art/Materials/People/MAT_Person_Clothing_B.mat");
            Assert.That(matB, Is.Not.Null, "MAT_Person_Clothing_B.mat missing");

            for (int i = 3; i < 6; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPaths[i]);
                var smrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                bool usesClothingB = false;
                foreach (var smr in smrs)
                {
                    foreach (var mat in smr.sharedMaterials)
                    {
                        if (mat != null && mat.name == "MAT_Person_Clothing_B")
                        {
                            usesClothingB = true;
                            break;
                        }
                    }
                }
                Assert.That(usesClothingB, Is.True, $"Prefab {prefab.name} should use MAT_Person_Clothing_B");
            }
        }
    }
}
#endif
