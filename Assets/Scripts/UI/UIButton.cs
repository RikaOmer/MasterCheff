using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MasterCheff.Managers;

namespace MasterCheff.UI
{
    /// <summary>
    /// Enhanced button with animation and sound effects
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Animation")]
        [SerializeField] private bool _animateOnPress = true;
        [SerializeField] private float _pressedScale = 0.95f;
        [SerializeField] private float _animationSpeed = 10f;

        [Header("Audio")]
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private AudioClip _hoverSound;

        [Header("Haptic")]
        [SerializeField] private bool _enableHaptic = true;

        private Button _button;
        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private bool _isPressed = false;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = _rectTransform.localScale;
            _targetScale = _originalScale;

            _button.onClick.AddListener(OnClick);
        }

        private void Update()
        {
            if (_animateOnPress)
            {
                _rectTransform.localScale = Vector3.Lerp(
                    _rectTransform.localScale,
                    _targetScale,
                    Time.unscaledDeltaTime * _animationSpeed
                );
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            _isPressed = true;
            _targetScale = _originalScale * _pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            _targetScale = _originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            if (_hoverSound != null && AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayUISound(_hoverSound);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPressed = false;
            _targetScale = _originalScale;
        }

        private void OnClick()
        {
            // Play click sound
            if (_clickSound != null && AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayUISound(_clickSound);
            }

            // Trigger haptic feedback
            if (_enableHaptic)
            {
                TriggerHaptic();
            }
        }

        private void TriggerHaptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS uses taptic engine - requires native plugin for light impact
#endif
        }

        private void OnDisable()
        {
            _rectTransform.localScale = _originalScale;
            _targetScale = _originalScale;
            _isPressed = false;
        }
    }
}

