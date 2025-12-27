using UnityEngine;
using System;

namespace MasterCheff.UI
{
    /// <summary>
    /// Base class for UI Panels
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        [Header("Panel Settings")]
        [SerializeField] private string _panelName;
        [SerializeField] private bool _animateOnShow = true;
        [SerializeField] private bool _animateOnHide = true;

        [Header("Animation Settings")]
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private AnimationType _showAnimation = AnimationType.FadeIn;
        [SerializeField] private AnimationType _hideAnimation = AnimationType.FadeOut;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private bool _isShowing = false;

        // Events
        public event Action OnPanelShown;
        public event Action OnPanelHidden;

        // Properties
        public string PanelName => string.IsNullOrEmpty(_panelName) ? gameObject.name : _panelName;
        public bool IsShowing => _isShowing;

        public enum AnimationType
        {
            None,
            FadeIn,
            FadeOut,
            SlideFromLeft,
            SlideFromRight,
            SlideFromTop,
            SlideFromBottom,
            ScaleUp,
            ScaleDown
        }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _rectTransform = GetComponent<RectTransform>();
        }

        protected virtual void Start()
        {
            // Register with UIManager
            if (Managers.UIManager.HasInstance)
            {
                Managers.UIManager.Instance.RegisterPanel(this);
            }
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            _isShowing = true;

            if (_animateOnShow)
            {
                PlayShowAnimation();
            }
            else
            {
                OnShowComplete();
            }
        }

        public virtual void Hide()
        {
            if (_animateOnHide)
            {
                PlayHideAnimation();
            }
            else
            {
                OnHideComplete();
            }
        }

        protected virtual void OnShowComplete()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            OnPanelShown?.Invoke();
        }

        protected virtual void OnHideComplete()
        {
            _isShowing = false;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            OnPanelHidden?.Invoke();
        }

        private void PlayShowAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateShow());
        }

        private void PlayHideAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateHide());
        }

        private System.Collections.IEnumerator AnimateShow()
        {
            _canvasGroup.interactable = false;
            float elapsed = 0f;

            Vector2 startPos = GetStartPosition(_showAnimation);
            Vector2 endPos = Vector2.zero;

            float startAlpha = (_showAnimation == AnimationType.FadeIn) ? 0f : 1f;
            float endAlpha = 1f;

            Vector3 startScale = (_showAnimation == AnimationType.ScaleUp) ? Vector3.zero : Vector3.one;
            Vector3 endScale = Vector3.one;

            _canvasGroup.alpha = startAlpha;
            _rectTransform.anchoredPosition = startPos;
            transform.localScale = startScale;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _animationDuration);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;
            }

            _canvasGroup.alpha = endAlpha;
            _rectTransform.anchoredPosition = endPos;
            transform.localScale = endScale;

            OnShowComplete();
        }

        private System.Collections.IEnumerator AnimateHide()
        {
            _canvasGroup.interactable = false;
            float elapsed = 0f;

            Vector2 startPos = Vector2.zero;
            Vector2 endPos = GetEndPosition(_hideAnimation);

            float startAlpha = 1f;
            float endAlpha = (_hideAnimation == AnimationType.FadeOut) ? 0f : 1f;

            Vector3 startScale = Vector3.one;
            Vector3 endScale = (_hideAnimation == AnimationType.ScaleDown) ? Vector3.zero : Vector3.one;

            while (elapsed < _animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _animationDuration);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;
            }

            OnHideComplete();
        }

        private Vector2 GetStartPosition(AnimationType animType)
        {
            float offset = 1000f;
            return animType switch
            {
                AnimationType.SlideFromLeft => new Vector2(-offset, 0),
                AnimationType.SlideFromRight => new Vector2(offset, 0),
                AnimationType.SlideFromTop => new Vector2(0, offset),
                AnimationType.SlideFromBottom => new Vector2(0, -offset),
                _ => Vector2.zero
            };
        }

        private Vector2 GetEndPosition(AnimationType animType)
        {
            float offset = 1000f;
            return animType switch
            {
                AnimationType.SlideFromLeft => new Vector2(-offset, 0),
                AnimationType.SlideFromRight => new Vector2(offset, 0),
                AnimationType.SlideFromTop => new Vector2(0, offset),
                AnimationType.SlideFromBottom => new Vector2(0, -offset),
                _ => Vector2.zero
            };
        }
    }
}


