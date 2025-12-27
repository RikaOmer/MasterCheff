using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Data;
using MasterCheff.Gameplay;
using MasterCheff.Managers;

namespace MasterCheff.UI.Components
{
    /// <summary>
    /// UI Component for power-up activation buttons
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PowerUpButton : MonoBehaviour
    {
        [Header("Power-Up Type")]
        [SerializeField] private PowerUpType _powerUpType;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _cooldownOverlay;
        [SerializeField] private Image _cooldownFillImage;

        [Header("Visuals")]
        [SerializeField] private Sprite _powerUpIcon;
        [SerializeField] private Color _availableColor = Color.white;
        [SerializeField] private Color _unavailableColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _activatedColor = new Color(0.2f, 1f, 0.4f);

        [Header("Animation")]
        [SerializeField] private float _pressedScale = 0.9f;
        [SerializeField] private float _animationSpeed = 10f;

        private Button _button;
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private int _currentCount = 0;
        private bool _isOnCooldown = false;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _originalScale = transform.localScale;
            _targetScale = _originalScale;

            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }

            if (_iconImage != null && _powerUpIcon != null)
            {
                _iconImage.sprite = _powerUpIcon;
            }

            if (_cooldownOverlay != null)
            {
                _cooldownOverlay.SetActive(false);
            }
        }

        private void Start()
        {
            if (PowerUpManager.HasInstance)
            {
                PowerUpManager.Instance.OnPowerUpCountChanged += HandlePowerUpCountChanged;
                PowerUpManager.Instance.OnPowerUpUsed += HandlePowerUpUsed;
                
                // Initialize count
                UpdateCount(PowerUpManager.Instance.GetPowerUpCount(_powerUpType));
            }
        }

        private void OnDestroy()
        {
            if (PowerUpManager.HasInstance)
            {
                PowerUpManager.Instance.OnPowerUpCountChanged -= HandlePowerUpCountChanged;
                PowerUpManager.Instance.OnPowerUpUsed -= HandlePowerUpUsed;
            }
        }

        private void Update()
        {
            // Smooth scale animation
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _animationSpeed);
        }

        private void OnButtonClicked()
        {
            if (!PowerUpManager.HasInstance) return;

            if (PowerUpManager.Instance.ActivatePowerUp(_powerUpType))
            {
                PlayActivationAnimation();
                
                // Haptic feedback
#if UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
            }
        }

        private void HandlePowerUpCountChanged(PowerUpType type, int count)
        {
            if (type == _powerUpType)
            {
                UpdateCount(count);
            }
        }

        private void HandlePowerUpUsed(PowerUpType type)
        {
            if (type == _powerUpType)
            {
                PlayActivationAnimation();
            }
        }

        private void UpdateCount(int count)
        {
            _currentCount = count;

            if (_countText != null)
            {
                _countText.text = count.ToString();
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            bool canUse = PowerUpManager.HasInstance && PowerUpManager.Instance.CanUsePowerUp(_powerUpType);
            
            if (_button != null)
            {
                _button.interactable = canUse && _currentCount > 0;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = canUse && _currentCount > 0 ? _availableColor : _unavailableColor;
            }

            if (_iconImage != null)
            {
                _iconImage.color = _currentCount > 0 ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            }
        }

        private void PlayActivationAnimation()
        {
            StartCoroutine(ActivationAnimationCoroutine());
        }

        private System.Collections.IEnumerator ActivationAnimationCoroutine()
        {
            // Flash color
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _activatedColor;
            }

            // Scale pop
            transform.localScale = _originalScale * 1.2f;

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                transform.localScale = Vector3.Lerp(_originalScale * 1.2f, _originalScale, t);
                
                if (_backgroundImage != null)
                {
                    _backgroundImage.color = Color.Lerp(_activatedColor, _availableColor, t);
                }

                yield return null;
            }

            transform.localScale = _originalScale;
            UpdateVisuals();
        }

        /// <summary>
        /// Get the display name for this power-up
        /// </summary>
        public string GetDisplayName()
        {
            return PowerUpManager.GetPowerUpDisplayName(_powerUpType);
        }

        /// <summary>
        /// Get the description for this power-up
        /// </summary>
        public string GetDescription()
        {
            return PowerUpManager.GetPowerUpDescription(_powerUpType);
        }
    }
}

