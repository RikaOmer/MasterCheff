using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Gameplay;

namespace MasterCheff.UI.Components
{
    /// <summary>
    /// Standalone timer display component
    /// </summary>
    public class TimerDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Image _timerFillImage;
        [SerializeField] private Image _timerBackground;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _warningColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color _criticalColor = new Color(1f, 0.2f, 0.2f);

        [Header("Thresholds")]
        [SerializeField] private float _warningThreshold = 15f;
        [SerializeField] private float _criticalThreshold = 5f;

        [Header("Animation")]
        [SerializeField] private bool _pulseOnCritical = true;
        [SerializeField] private float _pulseSpeed = 4f;
        [SerializeField] private float _pulseIntensity = 0.15f;

        private float _maxTime;
        private float _currentTime;
        private Vector3 _originalScale;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        private void Start()
        {
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.OnTimerUpdated += UpdateTimer;
            }
        }

        private void OnDestroy()
        {
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.OnTimerUpdated -= UpdateTimer;
            }
        }

        /// <summary>
        /// Set the maximum time for the timer
        /// </summary>
        public void SetMaxTime(float maxTime)
        {
            _maxTime = maxTime;
        }

        /// <summary>
        /// Update the timer display
        /// </summary>
        public void UpdateTimer(float remainingTime)
        {
            _currentTime = remainingTime;

            // Update text
            if (_timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                _timerText.text = $"{minutes:00}:{seconds:00}";
            }

            // Update fill
            if (_timerFillImage != null && _maxTime > 0)
            {
                _timerFillImage.fillAmount = remainingTime / _maxTime;
            }

            // Update color and animations
            UpdateVisuals(remainingTime);
        }

        private void UpdateVisuals(float remainingTime)
        {
            Color targetColor;
            bool isCritical = remainingTime <= _criticalThreshold;
            bool isWarning = remainingTime <= _warningThreshold;

            if (isCritical)
            {
                targetColor = _criticalColor;
            }
            else if (isWarning)
            {
                targetColor = _warningColor;
            }
            else
            {
                targetColor = _normalColor;
            }

            // Apply color
            if (_timerText != null) _timerText.color = targetColor;
            if (_timerFillImage != null) _timerFillImage.color = targetColor;

            // Pulse animation on critical
            if (_pulseOnCritical && isCritical)
            {
                float pulse = Mathf.PingPong(Time.time * _pulseSpeed, 1f);
                transform.localScale = _originalScale * (1f + pulse * _pulseIntensity);
            }
            else
            {
                transform.localScale = _originalScale;
            }
        }

        /// <summary>
        /// Format time as MM:SS string
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60);
            int secs = Mathf.FloorToInt(seconds % 60);
            return $"{mins:00}:{secs:00}";
        }
    }
}


