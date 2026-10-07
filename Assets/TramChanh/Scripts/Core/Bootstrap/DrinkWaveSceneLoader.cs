using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TramChanh.App
{
    /// <summary>Loads the saved blockout additively; the gameplay overlay contains no copied environment.</summary>
    public sealed class DrinkWaveSceneLoader : MonoBehaviour
    {
        [SerializeField] private string _baseScenePath;
        public Scene BaseScene { get; private set; }
        private bool _ownsBase;
        public IEnumerator LoadBase()
        {
            BaseScene = SceneManager.GetSceneByPath(_baseScenePath);
            if (BaseScene.IsValid() && BaseScene.isLoaded) { yield break; }
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(_baseScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync(_baseScenePath, LoadSceneMode.Additive);
#endif
            BaseScene = SceneManager.GetSceneByPath(_baseScenePath);
            _ownsBase = true;
        }
        private void OnDestroy()
        {
            if (_ownsBase && BaseScene.IsValid() && BaseScene.isLoaded) { SceneManager.UnloadSceneAsync(BaseScene); }
        }
    }
}
