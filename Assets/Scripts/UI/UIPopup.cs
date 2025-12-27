using UnityEngine;
using UnityEngine.UI;
using System;

namespace MasterCheff.UI
{
    /// <summary>
    /// Base class for UI Popups
    /// </summary>
    public class UIPopup : MonoBehaviour
    {
        [Header("Popup Settings")]
        [SerializeField] private bool _closeOnBackgroundClick = true;
        [SerializeField] private bool _destroyOnClose = true;
        [SerializeField] private float _animationDuration = 0.2f;

        [Header("References")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backgroundButton;
        [SerializeField] private RectTransform _contentPanel;

        private CanvasGroup _canvasGroup;

        // Events
        public event Action OnPopupOpened;
        public event Action OnPopupClosed;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_backgroundButton != null && _closeOnBackgroundClick)
            {
                _backgroundButton.onClick.AddListener(Close);
            }
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(AnimateShow());
        }

        public virtual void Hide()
        {
            StartCoroutine(AnimateHide());
        }

        public void Close()
        {
            if (Managers.UIManager.HasInstance)
            {
                Managers.UIManager.Instance.ClosePopup(this);
            }
            else
            {
                Hide();
            }
        }

        private System.Collections.IEnumerator AnimateShow()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;

            if (_contentPanel != null)
            {
                _contentPanel.localScale = Vector3.one * 0.8f;
            }

            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _animationDuration);

                _canvasGroup.alpha = t;
                if (_contentPanel != null)
                {
                    _contentPanel.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                }

                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (_contentPanel != null)
            {
                _contentPanel.localScale = Vector3.one;
            }

            OnPopupOpened?.Invoke();
        }

        private System.Collections.IEnumerator AnimateHide()
        {
            _canvasGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _animationDuration);

                _canvasGroup.alpha = 1f - t;
                if (_contentPanel != null)
                {
                    _contentPanel.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, t);
                }

                yield return null;
            }

            OnPopupClosed?.Invoke();

            if (_destroyOnClose)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}


