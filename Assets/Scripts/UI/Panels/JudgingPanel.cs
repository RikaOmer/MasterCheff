using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Core;
using MasterCheff.Gameplay;

namespace MasterCheff.UI.Panels
{
    /// <summary>
    /// UI Panel displayed during the judging phase while AI is evaluating
    /// </summary>
    public class JudgingPanel : UIPanel
    {
        [Header("Status Display")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _subStatusText;

        [Header("Judge Avatars")]
        [SerializeField] private Image _criticAvatar;
        [SerializeField] private Image _visionaryAvatar;
        [SerializeField] private Image _soulCookAvatar;
        [SerializeField] private float _avatarPulseSpeed = 2f;

        [Header("Animation")]
        [SerializeField] private GameObject _loadingSpinner;
        [SerializeField] private RectTransform _dotsContainer;
        [SerializeField] private float _dotAnimationSpeed = 0.5f;

        [Header("Messages")]
        [SerializeField] private string[] _judgingMessages = new string[]
        {
            "The judges are deliberating...",
            "Tasting the creativity...",
            "Evaluating technique...",
            "Considering presentation...",
            "The verdict approaches..."
        };

        private Coroutine _animationCoroutine;
        private int _currentMessageIndex;

        protected override void Start()
        {
            base.Start();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(GameState phase)
        {
            if (phase == GameState.Judging)
            {
                Show();
                StartAnimations();
            }
            else
            {
                Hide();
                StopAnimations();
            }
        }

        public override void Show()
        {
            base.Show();
            StartAnimations();
        }

        public override void Hide()
        {
            StopAnimations();
            base.Hide();
        }

        private void StartAnimations()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(JudgingAnimationRoutine());
        }

        private void StopAnimations()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        private IEnumerator JudgingAnimationRoutine()
        {
            _currentMessageIndex = 0;
            float messageTimer = 0f;
            float messageInterval = 2f;

            while (true)
            {
                // Update status message
                messageTimer += Time.deltaTime;
                if (messageTimer >= messageInterval)
                {
                    messageTimer = 0f;
                    _currentMessageIndex = (_currentMessageIndex + 1) % _judgingMessages.Length;
                    UpdateStatusMessage();
                }

                // Animate judge avatars
                AnimateJudgeAvatars();

                // Animate loading spinner
                AnimateLoadingSpinner();

                yield return null;
            }
        }

        private void UpdateStatusMessage()
        {
            if (_statusText != null)
            {
                _statusText.text = _judgingMessages[_currentMessageIndex];
            }
        }

        private void AnimateJudgeAvatars()
        {
            float time = Time.time * _avatarPulseSpeed;

            // Staggered pulsing for each judge
            if (_criticAvatar != null)
            {
                float pulse1 = 0.9f + Mathf.Sin(time) * 0.1f;
                _criticAvatar.transform.localScale = Vector3.one * pulse1;
            }

            if (_visionaryAvatar != null)
            {
                float pulse2 = 0.9f + Mathf.Sin(time + 2f) * 0.1f;
                _visionaryAvatar.transform.localScale = Vector3.one * pulse2;
            }

            if (_soulCookAvatar != null)
            {
                float pulse3 = 0.9f + Mathf.Sin(time + 4f) * 0.1f;
                _soulCookAvatar.transform.localScale = Vector3.one * pulse3;
            }
        }

        private void AnimateLoadingSpinner()
        {
            if (_loadingSpinner != null)
            {
                _loadingSpinner.transform.Rotate(0f, 0f, -180f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Update the sub-status text with custom message
        /// </summary>
        public void SetSubStatus(string message)
        {
            if (_subStatusText != null)
            {
                _subStatusText.text = message;
            }
        }
    }
}

