using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Core;
using MasterCheff.Data;
using MasterCheff.Gameplay;
using MasterCheff.Managers;
using MasterCheff.UI.Components;

namespace MasterCheff.UI.Panels
{
    /// <summary>
    /// UI Panel for the cooking phase - handles dish submission input
    /// </summary>
    public class CookingPanel : UIPanel
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField _dishNameInput;
        [SerializeField] private TMP_InputField _descriptionInput;
        [SerializeField] private TMP_InputField _secretIngredientInput;
        [SerializeField] private int _dishNameMaxLength = 50;
        [SerializeField] private int _descriptionMaxLength = 200;

        [Header("Ingredients Display")]
        [SerializeField] private TextMeshProUGUI _ingredient1Text;
        [SerializeField] private TextMeshProUGUI _ingredient2Text;
        [SerializeField] private Image _ingredient1Icon;
        [SerializeField] private Image _ingredient2Icon;
        [SerializeField] private TextMeshProUGUI _ingredientsPlusText;

        [Header("Style Tag Buttons")]
        [SerializeField] private StyleTagButton[] _styleTagButtons;
        [SerializeField] private Color _selectedTagColor = new Color(0.2f, 0.8f, 0.4f);
        [SerializeField] private Color _normalTagColor = Color.white;

        [Header("Timer Display")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Image _timerFillImage;
        [SerializeField] private Color _timerNormalColor = Color.white;
        [SerializeField] private Color _timerWarningColor = Color.yellow;
        [SerializeField] private Color _timerCriticalColor = Color.red;
        [SerializeField] private float _warningThreshold = 15f;
        [SerializeField] private float _criticalThreshold = 5f;

        [Header("Submission")]
        [SerializeField] private Button _submitButton;
        [SerializeField] private TextMeshProUGUI _submitButtonText;
        [SerializeField] private GameObject _submittedOverlay;

        [Header("Round Info")]
        [SerializeField] private TextMeshProUGUI _roundNumberText;

        [Header("Secret Ingredient Section")]
        [SerializeField] private GameObject _secretIngredientSection;

        // State
        private DishStyleTag _selectedStyleTag = DishStyleTag.HomeyComfort;
        private bool _hasSubmitted = false;
        private bool _secretIngredientEnabled = false;
        private float _totalCookingTime;

        // Events
        public event Action<PlayerSubmission> OnDishSubmitted;

        protected override void Awake()
        {
            base.Awake();
            SetupInputFields();
            SetupStyleTagButtons();
            SetupSubmitButton();
        }

        protected override void Start()
        {
            base.Start();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SetupInputFields()
        {
            if (_dishNameInput != null)
            {
                _dishNameInput.characterLimit = _dishNameMaxLength;
                _dishNameInput.onValueChanged.AddListener(OnDishNameChanged);
            }

            if (_descriptionInput != null)
            {
                _descriptionInput.characterLimit = _descriptionMaxLength;
            }

            if (_secretIngredientSection != null)
            {
                _secretIngredientSection.SetActive(false);
            }
        }

        private void SetupStyleTagButtons()
        {
            for (int i = 0; i < _styleTagButtons.Length; i++)
            {
                int index = i; // Capture for closure
                if (_styleTagButtons[i] != null)
                {
                    _styleTagButtons[i].Initialize((DishStyleTag)index, OnStyleTagSelected);
                }
            }

            // Select default
            SelectStyleTag(DishStyleTag.HomeyComfort);
        }

        private void SetupSubmitButton()
        {
            if (_submitButton != null)
            {
                _submitButton.onClick.AddListener(OnSubmitClicked);
                UpdateSubmitButtonState();
            }
        }

        private void SubscribeToEvents()
        {
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.OnPhaseChanged += HandlePhaseChanged;
                RoundLoopController.Instance.OnIngredientsRevealed += HandleIngredientsRevealed;
                RoundLoopController.Instance.OnTimerUpdated += HandleTimerUpdated;
                RoundLoopController.Instance.OnSubmissionConfirmed += HandleSubmissionConfirmed;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.OnPhaseChanged -= HandlePhaseChanged;
                RoundLoopController.Instance.OnIngredientsRevealed -= HandleIngredientsRevealed;
                RoundLoopController.Instance.OnTimerUpdated -= HandleTimerUpdated;
                RoundLoopController.Instance.OnSubmissionConfirmed -= HandleSubmissionConfirmed;
            }
        }

        #region Public Methods

        /// <summary>
        /// Reset the panel for a new round
        /// </summary>
        public void ResetForNewRound()
        {
            _hasSubmitted = false;

            // Clear inputs
            if (_dishNameInput != null) _dishNameInput.text = string.Empty;
            if (_descriptionInput != null) _descriptionInput.text = string.Empty;
            if (_secretIngredientInput != null) _secretIngredientInput.text = string.Empty;

            // Reset UI state
            if (_submittedOverlay != null) _submittedOverlay.SetActive(false);
            if (_submitButton != null) _submitButton.interactable = false;

            // Reset style tag to default
            SelectStyleTag(DishStyleTag.HomeyComfort);

            // Update round display
            UpdateRoundDisplay();

            UpdateSubmitButtonState();
        }

        /// <summary>
        /// Enable secret ingredient input (power-up)
        /// </summary>
        public void EnableSecretIngredient(bool enable)
        {
            _secretIngredientEnabled = enable;
            if (_secretIngredientSection != null)
            {
                _secretIngredientSection.SetActive(enable);
            }
        }

        /// <summary>
        /// Display the current round's ingredients
        /// </summary>
        public void DisplayIngredients(RoundIngredients ingredients)
        {
            if (_ingredient1Text != null)
                _ingredient1Text.text = ingredients.Ingredient1;
            
            if (_ingredient2Text != null)
                _ingredient2Text.text = ingredients.Ingredient2;

            // Animate ingredients reveal (optional)
            StartCoroutine(AnimateIngredientReveal());
        }

        /// <summary>
        /// Update the timer display
        /// </summary>
        public void UpdateTimer(float remainingTime)
        {
            if (_timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                _timerText.text = $"{minutes:00}:{seconds:00}";

                // Update timer color based on remaining time
                if (remainingTime <= _criticalThreshold)
                {
                    _timerText.color = _timerCriticalColor;
                    // Pulse effect for critical time
                    float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                    _timerText.transform.localScale = Vector3.one * (1f + pulse * 0.1f);
                }
                else if (remainingTime <= _warningThreshold)
                {
                    _timerText.color = _timerWarningColor;
                    _timerText.transform.localScale = Vector3.one;
                }
                else
                {
                    _timerText.color = _timerNormalColor;
                    _timerText.transform.localScale = Vector3.one;
                }
            }

            // Update fill image
            if (_timerFillImage != null && _totalCookingTime > 0)
            {
                _timerFillImage.fillAmount = remainingTime / _totalCookingTime;
                _timerFillImage.color = remainingTime <= _criticalThreshold ? _timerCriticalColor :
                                         remainingTime <= _warningThreshold ? _timerWarningColor : _timerNormalColor;
            }
        }

        #endregion

        #region UI Callbacks

        private void OnDishNameChanged(string text)
        {
            UpdateSubmitButtonState();
        }

        private void OnStyleTagSelected(DishStyleTag tag)
        {
            SelectStyleTag(tag);
        }

        private void SelectStyleTag(DishStyleTag tag)
        {
            _selectedStyleTag = tag;

            // Update button visuals
            for (int i = 0; i < _styleTagButtons.Length; i++)
            {
                if (_styleTagButtons[i] != null)
                {
                    bool isSelected = (DishStyleTag)i == tag;
                    _styleTagButtons[i].SetSelected(isSelected);
                }
            }
        }

        private void OnSubmitClicked()
        {
            if (_hasSubmitted) return;

            string dishName = _dishNameInput?.text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dishName))
            {
                // Show error - dish name required
                Debug.LogWarning("[CookingPanel] Dish name is required!");
                return;
            }

            string description = _descriptionInput?.text?.Trim() ?? string.Empty;
            string secretIngredient = _secretIngredientEnabled ? 
                (_secretIngredientInput?.text?.Trim() ?? string.Empty) : string.Empty;

            // Submit via RoundLoopController
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.SubmitLocalDish(
                    dishName,
                    description,
                    _selectedStyleTag,
                    secretIngredient
                );
            }
        }

        private void UpdateSubmitButtonState()
        {
            if (_submitButton == null) return;

            bool hasName = !string.IsNullOrWhiteSpace(_dishNameInput?.text);
            _submitButton.interactable = hasName && !_hasSubmitted;

            if (_submitButtonText != null)
            {
                _submitButtonText.text = _hasSubmitted ? "Submitted!" : "Submit Dish";
            }
        }

        #endregion

        #region Event Handlers

        private void HandlePhaseChanged(GameState phase)
        {
            switch (phase)
            {
                case GameState.IngredientReveal:
                    ResetForNewRound();
                    Show();
                    break;
                case GameState.Cooking:
                    _totalCookingTime = RoundLoopController.Instance?.RemainingTime ?? 60f;
                    break;
                case GameState.Judging:
                case GameState.RoundResults:
                case GameState.MatchEnd:
                    Hide();
                    break;
            }
        }

        private void HandleIngredientsRevealed(RoundIngredients ingredients)
        {
            DisplayIngredients(ingredients);
        }

        private void HandleTimerUpdated(float remainingTime)
        {
            UpdateTimer(remainingTime);
        }

        private void HandleSubmissionConfirmed(PlayerSubmission submission)
        {
            _hasSubmitted = true;
            UpdateSubmitButtonState();

            if (_submittedOverlay != null)
            {
                _submittedOverlay.SetActive(true);
            }

            // Trigger haptic feedback
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif

            OnDishSubmitted?.Invoke(submission);
            Debug.Log($"[CookingPanel] Dish submitted: {submission.DishName}");
        }

        #endregion

        #region Helpers

        private void UpdateRoundDisplay()
        {
            if (_roundNumberText != null && RoundLoopController.HasInstance)
            {
                _roundNumberText.text = $"Round {RoundLoopController.Instance.CurrentRound}/{RoundLoopController.Instance.TotalRounds}";
            }
        }

        private System.Collections.IEnumerator AnimateIngredientReveal()
        {
            // Simple scale animation for ingredients
            Transform[] targets = { _ingredient1Text?.transform, _ingredient2Text?.transform };
            
            foreach (var target in targets)
            {
                if (target == null) continue;
                
                target.localScale = Vector3.zero;
            }

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.SmoothStep(0f, 1f, t);

                foreach (var target in targets)
                {
                    if (target != null)
                        target.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            // Ensure final scale
            foreach (var target in targets)
            {
                if (target != null)
                    target.localScale = Vector3.one;
            }

            // Animate plus sign
            if (_ingredientsPlusText != null)
            {
                _ingredientsPlusText.transform.localScale = Vector3.zero;
                elapsed = 0f;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / 0.3f;
                    _ingredientsPlusText.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t);
                    yield return null;
                }
                _ingredientsPlusText.transform.localScale = Vector3.one;
            }
        }

        #endregion
    }
}


