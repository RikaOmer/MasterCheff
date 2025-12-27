using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using MasterCheff.Core;

namespace MasterCheff.Managers
{
    /// <summary>
    /// Scene Loader - Handles scene transitions with loading screen support
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Header("Settings")]
        [SerializeField] private string _loadingSceneName = "Loading";
        [SerializeField] private float _minimumLoadTime = 0.5f;
        [SerializeField] private bool _useLoadingScene = true;

        [Header("Fade Settings")]
        [SerializeField] private bool _useFade = true;
        [SerializeField] private float _fadeTime = 0.3f;
        [SerializeField] private CanvasGroup _fadeCanvasGroup;

        // State
        private bool _isLoading = false;
        private float _loadProgress = 0f;
        private string _targetScene;

        // Events
        public event Action OnLoadStart;
        public event Action<float> OnLoadProgress;
        public event Action OnLoadComplete;

        // Properties
        public bool IsLoading => _isLoading;
        public float LoadProgress => _loadProgress;
        public string CurrentScene => SceneManager.GetActiveScene().name;

        protected override void OnSingletonAwake()
        {
            CreateFadeCanvas();
            Debug.Log("[SceneLoader] Initialized");
        }

        private void CreateFadeCanvas()
        {
            if (_fadeCanvasGroup != null) return;

            // Create fade overlay
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create fade image
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform);

            RectTransform rectTransform = imageObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image image = imageObj.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.black;

            _fadeCanvasGroup = imageObj.AddComponent<CanvasGroup>();
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
        }

        #region Scene Loading

        /// <summary>
        /// Load a scene by name
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;

            _targetScene = sceneName;

            if (_useLoadingScene)
            {
                StartCoroutine(LoadWithLoadingScene(sceneName));
            }
            else if (_useFade)
            {
                StartCoroutine(LoadWithFade(sceneName));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        /// <summary>
        /// Load a scene by build index
        /// </summary>
        public void LoadScene(int buildIndex)
        {
            string sceneName = SceneManager.GetSceneByBuildIndex(buildIndex).name;
            LoadScene(sceneName);
        }

        /// <summary>
        /// Reload the current scene
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(CurrentScene);
        }

        /// <summary>
        /// Load next scene in build order
        /// </summary>
        public void LoadNextScene()
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning("[SceneLoader] No next scene available");
            }
        }

        #endregion

        #region Async Loading

        /// <summary>
        /// Load scene asynchronously
        /// </summary>
        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            if (_isLoading) return;

            StartCoroutine(LoadSceneAsyncRoutine(sceneName, onComplete));
        }

        private IEnumerator LoadSceneAsyncRoutine(string sceneName, Action onComplete)
        {
            _isLoading = true;
            _loadProgress = 0f;
            OnLoadStart?.Invoke();

            if (_useFade)
            {
                yield return StartCoroutine(Fade(1f));
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            float startTime = Time.unscaledTime;

            while (!operation.isDone)
            {
                _loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
                OnLoadProgress?.Invoke(_loadProgress);

                if (operation.progress >= 0.9f)
                {
                    float elapsed = Time.unscaledTime - startTime;
                    if (elapsed >= _minimumLoadTime)
                    {
                        operation.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            _loadProgress = 1f;
            OnLoadProgress?.Invoke(_loadProgress);

            if (_useFade)
            {
                yield return StartCoroutine(Fade(0f));
            }

            _isLoading = false;
            OnLoadComplete?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator LoadWithLoadingScene(string targetScene)
        {
            _isLoading = true;
            OnLoadStart?.Invoke();

            // Load loading scene
            if (_useFade)
            {
                yield return StartCoroutine(Fade(1f));
            }

            SceneManager.LoadScene(_loadingSceneName);

            yield return null; // Wait a frame

            if (_useFade)
            {
                yield return StartCoroutine(Fade(0f));
            }

            // Load target scene async
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
            operation.allowSceneActivation = false;

            float startTime = Time.unscaledTime;

            while (!operation.isDone)
            {
                _loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
                OnLoadProgress?.Invoke(_loadProgress);

                if (operation.progress >= 0.9f)
                {
                    float elapsed = Time.unscaledTime - startTime;
                    if (elapsed >= _minimumLoadTime)
                    {
                        if (_useFade)
                        {
                            yield return StartCoroutine(Fade(1f));
                        }

                        operation.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            _loadProgress = 1f;

            if (_useFade)
            {
                yield return StartCoroutine(Fade(0f));
            }

            _isLoading = false;
            OnLoadComplete?.Invoke();
        }

        private IEnumerator LoadWithFade(string sceneName)
        {
            _isLoading = true;
            OnLoadStart?.Invoke();

            yield return StartCoroutine(Fade(1f));

            SceneManager.LoadScene(sceneName);

            yield return null;

            yield return StartCoroutine(Fade(0f));

            _isLoading = false;
            OnLoadComplete?.Invoke();
        }

        #endregion

        #region Additive Loading

        /// <summary>
        /// Load a scene additively
        /// </summary>
        public void LoadSceneAdditive(string sceneName, Action onComplete = null)
        {
            StartCoroutine(LoadAdditiveRoutine(sceneName, onComplete));
        }

        private IEnumerator LoadAdditiveRoutine(string sceneName, Action onComplete)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            while (!operation.isDone)
            {
                yield return null;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Unload an additive scene
        /// </summary>
        public void UnloadScene(string sceneName, Action onComplete = null)
        {
            StartCoroutine(UnloadRoutine(sceneName, onComplete));
        }

        private IEnumerator UnloadRoutine(string sceneName, Action onComplete)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

            while (!operation.isDone)
            {
                yield return null;
            }

            onComplete?.Invoke();
        }

        #endregion

        #region Fade

        private IEnumerator Fade(float targetAlpha)
        {
            if (_fadeCanvasGroup == null) yield break;

            _fadeCanvasGroup.blocksRaycasts = true;
            float startAlpha = _fadeCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeTime);
                yield return null;
            }

            _fadeCanvasGroup.alpha = targetAlpha;
            _fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.5f;
        }

        #endregion
    }
}

