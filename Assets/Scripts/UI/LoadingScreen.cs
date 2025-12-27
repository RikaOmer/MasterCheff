using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Managers;

namespace MasterCheff.UI
{
    /// <summary>
    /// Loading Screen UI component
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TextMeshProUGUI _tipText;

        [Header("Animation")]
        [SerializeField] private GameObject _loadingIcon;
        [SerializeField] private float _rotationSpeed = 180f;
        [SerializeField] private bool _smoothProgress = true;
        [SerializeField] private float _progressSmoothSpeed = 3f;

        [Header("Tips")]
        [SerializeField] private string[] _loadingTips;
        [SerializeField] private float _tipChangeInterval = 3f;

        private float _displayedProgress = 0f;
        private float _targetProgress = 0f;
        private float _tipTimer = 0f;
        private int _currentTipIndex = 0;

        private void Awake()
        {
            // Subscribe to scene loader events
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.OnLoadProgress += OnLoadProgress;
            }
        }

        private void OnDestroy()
        {
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.OnLoadProgress -= OnLoadProgress;
            }
        }

        private void Start()
        {
            ShowRandomTip();
        }

        private void Update()
        {
            UpdateProgress();
            UpdateLoadingIcon();
            UpdateTips();
        }

        private void OnLoadProgress(float progress)
        {
            _targetProgress = progress;
        }

        private void UpdateProgress()
        {
            if (_smoothProgress)
            {
                _displayedProgress = Mathf.Lerp(_displayedProgress, _targetProgress, Time.deltaTime * _progressSmoothSpeed);
            }
            else
            {
                _displayedProgress = _targetProgress;
            }

            // Update progress bar
            if (_progressBar != null)
            {
                _progressBar.value = _displayedProgress;
            }

            if (_progressFill != null)
            {
                _progressFill.fillAmount = _displayedProgress;
            }

            // Update progress text
            if (_progressText != null)
            {
                int percentage = Mathf.RoundToInt(_displayedProgress * 100f);
                _progressText.text = $"{percentage}%";
            }
        }

        private void UpdateLoadingIcon()
        {
            if (_loadingIcon != null)
            {
                _loadingIcon.transform.Rotate(0, 0, -_rotationSpeed * Time.deltaTime);
            }
        }

        private void UpdateTips()
        {
            if (_loadingTips == null || _loadingTips.Length == 0) return;

            _tipTimer += Time.deltaTime;
            if (_tipTimer >= _tipChangeInterval)
            {
                _tipTimer = 0f;
                ShowNextTip();
            }
        }

        private void ShowRandomTip()
        {
            if (_tipText == null || _loadingTips == null || _loadingTips.Length == 0) return;

            _currentTipIndex = Random.Range(0, _loadingTips.Length);
            _tipText.text = _loadingTips[_currentTipIndex];
        }

        private void ShowNextTip()
        {
            if (_tipText == null || _loadingTips == null || _loadingTips.Length == 0) return;

            _currentTipIndex = (_currentTipIndex + 1) % _loadingTips.Length;
            _tipText.text = _loadingTips[_currentTipIndex];
        }

        /// <summary>
        /// Set loading text
        /// </summary>
        public void SetLoadingText(string text)
        {
            if (_loadingText != null)
            {
                _loadingText.text = text;
            }
        }

        /// <summary>
        /// Set progress directly (for manual loading)
        /// </summary>
        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);
        }
    }
}


