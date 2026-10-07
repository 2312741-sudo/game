#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TramChanh.Tests.PlayMode.Placeholders
{
    public sealed class GameplayBlockoutSmokeTests
    {
        private Scene _scene;

        [UnityTest]
        public IEnumerator ART_STALL_001_SavedBlockout_EntersPlayWithoutConsoleErrors()
        {
            const string path = "Assets/TramChanh/Scenes/Gameplay/SCN_Gameplay_Blockout.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            _scene = SceneManager.GetSceneByPath(path);
            Assert.That(_scene.isLoaded, Is.True);
            for (int frame = 0; frame < 10; frame++)
            {
                yield return null;
            }

            Camera camera = _scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>()).Single();
            Assert.That(camera.isActiveAndEnabled, Is.True);
            Assert.That(_scene.GetRootGameObjects().Any(o => o.name == "PF_Stall_TramChanh"), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
        }
    }
}
#endif
