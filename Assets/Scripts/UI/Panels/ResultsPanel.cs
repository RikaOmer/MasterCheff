using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Core;
using MasterCheff.Data;
using MasterCheff.Gameplay;
using MasterCheff.Managers;
using MasterCheff.AI;

namespace MasterCheff.UI.Panels
{
    /// <summary>
    /// UI Panel for displaying round results, judge comments, and the AI-generated dish image
    /// </summary>
    public class ResultsPanel : UIPanel
    {
        [Header("Winner Display")]
        [SerializeField] private TextMeshProUGUI _winnerNameText;
        [SerializeField] private TextMeshProUGUI _winningDishNameText;
        [SerializeField] private TextMeshProUGUI _winnerScoreText;
        [SerializeField] private GameObject _winnerCrown;

        [Header("AI Generated Image")]
        [SerializeField] private RawImage _dishImage;
        [SerializeField] private GameObject _imageLoadingIndicator;
        [SerializeField] private GameObject _imageErrorPlaceholder;
        [SerializeField] private AspectRatioFitter _imageAspectFitter;

        [Header("Judge Comments")]
        [SerializeField] private JudgeCommentDisplay _criticComment;
        [SerializeField] private JudgeCommentDisplay _visionaryComment;
        [SerializeField] private JudgeCommentDisplay _soulCookComment;

        [Header("All Player Scores")]
        [SerializeField] private Transform _playerScoresContainer;
        [SerializeField] private GameObject _playerScoreItemPrefab;

        [Header("Round Progress")]
        [SerializeField] private TextMeshProUGUI _roundText;
        [SerializeField] private Slider _roundProgressSlider;

        [Header("Your Result")]
        [SerializeField] private GameObject _yourResultPanel;
        [SerializeField] private TextMeshProUGUI _yourDishNameText;
        [SerializeField] private TextMeshProUGUI _yourTotalScoreText;
        [SerializeField] private TextMeshProUGUI _yourCriticScoreText;
        [SerializeField] private TextMeshProUGUI _yourVisionaryScoreText;
        [SerializeField] private TextMeshProUGUI _yourSoulCookScoreText;

        [Header("Animation")]
        [SerializeField] private float _scoreRevealDelay = 0.5f;
        [SerializeField] private float _commentRevealDelay = 0.3f;
        [SerializeField] private float _imageRevealDelay = 1f;

        [Header("Continue Button")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private TextMeshProUGUI _continueButtonText;
        [SerializeField] private float _autoAdvanceTime = 10f;

        // State
        private RoundResult _currentResult;
        private Texture2D _loadedDishImage;
        private Coroutine _autoAdvanceCoroutine;

        protected override void Awake()
        {
            base.Awake();
            
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        protected override void Start()
        {
            base.Start();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            // Cleanup loaded texture
            if (_loadedDishImage != null)
            {
                Destroy(_loadedDishImage);
            }
        }

        private void SubscribeToEvents()
        {
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.OnPhaseChanged += HandlePhaseChanged;
                RoundLoopController.Instance.OnRoundComplete += HandleRoundComplete;
                RoundLoopController.Instance.OnMatchComplete += HandleMatchComplete;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.OnPhaseChanged -= HandlePhaseChanged;
                RoundLoopController.Instance.OnRoundComplete -= HandleRoundComplete;
                RoundLoopController.Instance.OnMatchComplete -= HandleMatchComplete;
            }
        }

        #region Event Handlers

        private void HandlePhaseChanged(GameState phase)
        {
            switch (phase)
            {
                case GameState.RoundResults:
                    Show();
                    break;
                case GameState.IngredientReveal:
                case GameState.Cooking:
                    Hide();
                    break;
                case GameState.MatchEnd:
                    ShowMatchEndResults();
                    break;
            }
        }

        private void HandleRoundComplete(RoundResult result)
        {
            _currentResult = result;
            DisplayRoundResult(result);
        }

        private void HandleMatchComplete(MatchData matchData)
        {
            DisplayMatchResults(matchData);
        }

        #endregion

        #region Display Methods

        /// <summary>
        /// Display the results of a single round
        /// </summary>
        public void DisplayRoundResult(RoundResult result)
        {
            if (result == null) return;

            _currentResult = result;

            // Clear previous
            ResetDisplay();

            // Start animated reveal
            StartCoroutine(RevealResultsAnimated(result));
        }

        private IEnumerator RevealResultsAnimated(RoundResult result)
        {
            // Round info
            UpdateRoundProgress();

            // Winner info
            yield return new WaitForSeconds(_scoreRevealDelay);
            DisplayWinner(result);

            // Judge comments for winner
            yield return new WaitForSeconds(_commentRevealDelay);
            DisplayJudgeComments(result.GetWinnerResult());

            // All player scores
            yield return new WaitForSeconds(_commentRevealDelay);
            DisplayAllPlayerScores(result);

            // Your personal result
            yield return new WaitForSeconds(_commentRevealDelay);
            DisplayYourResult(result);

            // AI generated image
            yield return new WaitForSeconds(_imageRevealDelay);
            StartCoroutine(LoadAndDisplayDishImage(result.WinningDishImageUrl));

            // Start auto-advance timer
            _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceCoroutine());
        }

        private void DisplayWinner(RoundResult result)
        {
            if (_winnerNameText != null)
            {
                _winnerNameText.text = result.WinnerPlayerName;
                AnimateTextPop(_winnerNameText.transform);
            }

            if (_winningDishNameText != null)
            {
                _winningDishNameText.text = result.WinningDishName;
            }

            var winnerResult = result.GetWinnerResult();
            if (_winnerScoreText != null && winnerResult != null)
            {
                _winnerScoreText.text = $"Score: {winnerResult.TotalScore}";
            }

            if (_winnerCrown != null)
            {
                _winnerCrown.SetActive(true);
                AnimateTextPop(_winnerCrown.transform);
            }
        }

        private void DisplayJudgeComments(PlayerJudgeResult playerResult)
        {
            if (playerResult == null) return;

            if (_criticComment != null)
            {
                _criticComment.Display(playerResult.Critic);
            }

            if (_visionaryComment != null)
            {
                _visionaryComment.Display(playerResult.Visionary);
            }

            if (_soulCookComment != null)
            {
                _soulCookComment.Display(playerResult.SoulCook);
            }
        }

        private void DisplayAllPlayerScores(RoundResult result)
        {
            if (_playerScoresContainer == null || _playerScoreItemPrefab == null) return;

            // Clear existing
            foreach (Transform child in _playerScoresContainer)
            {
                Destroy(child.gameObject);
            }

            // Sort by score descending
            var sortedResults = new PlayerJudgeResult[result.PlayerResults.Length];
            Array.Copy(result.PlayerResults, sortedResults, result.PlayerResults.Length);
            Array.Sort(sortedResults, (a, b) => b.TotalScore.CompareTo(a.TotalScore));

            // Create items
            for (int i = 0; i < sortedResults.Length; i++)
            {
                CreatePlayerScoreItem(sortedResults[i], i + 1, sortedResults[i].PlayerId == result.WinnerPlayerId);
            }
        }

        private void CreatePlayerScoreItem(PlayerJudgeResult playerResult, int rank, bool isWinner)
        {
            GameObject item = Instantiate(_playerScoreItemPrefab, _playerScoresContainer);

            // Rank
            var rankText = item.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            if (rankText != null)
            {
                rankText.text = $"#{rank}";
            }

            // Player name
            var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = playerResult.PlayerName;
            }

            // Dish name
            var dishText = item.transform.Find("DishText")?.GetComponent<TextMeshProUGUI>();
            if (dishText != null)
            {
                dishText.text = playerResult.DishName;
            }

            // Score
            var scoreText = item.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            if (scoreText != null)
            {
                scoreText.text = playerResult.TotalScore.ToString();
            }

            // Winner highlight
            var background = item.GetComponent<Image>();
            if (background != null && isWinner)
            {
                background.color = new Color(1f, 0.84f, 0f, 0.3f); // Gold tint
            }
        }

        private void DisplayYourResult(RoundResult result)
        {
            if (_yourResultPanel == null) return;

            int localPlayerId = Multiplayer.NetworkManager.HasInstance ? 
                Multiplayer.NetworkManager.Instance.LocalPlayerId : -1;

            var yourResult = result.GetPlayerResult(localPlayerId);
            
            if (yourResult == null)
            {
                _yourResultPanel.SetActive(false);
                return;
            }

            _yourResultPanel.SetActive(true);

            if (_yourDishNameText != null)
                _yourDishNameText.text = yourResult.DishName;

            if (_yourTotalScoreText != null)
                _yourTotalScoreText.text = $"Total: {yourResult.TotalScore}";

            if (_yourCriticScoreText != null)
                _yourCriticScoreText.text = $"Critic: {yourResult.Critic.Score}/10";

            if (_yourVisionaryScoreText != null)
                _yourVisionaryScoreText.text = $"Visionary: {yourResult.Visionary.Score}/10";

            if (_yourSoulCookScoreText != null)
                _yourSoulCookScoreText.text = $"Soul Cook: {yourResult.SoulCook.Score}/10";

            // Highlight if you won
            bool youWon = localPlayerId == result.WinnerPlayerId;
            if (youWon)
            {
                // Add celebration effect
                AnimateTextPop(_yourResultPanel.transform);
            }
        }

        private IEnumerator LoadAndDisplayDishImage(string imageUrl)
        {
            if (_dishImage == null) yield break;

            // Show loading
            if (_imageLoadingIndicator != null)
                _imageLoadingIndicator.SetActive(true);

            if (_imageErrorPlaceholder != null)
                _imageErrorPlaceholder.SetActive(false);

            _dishImage.gameObject.SetActive(false);

            // Download image
            if (!string.IsNullOrEmpty(imageUrl) && AIJudgeService.HasInstance)
            {
                var imageTask = AIJudgeService.Instance.DownloadDishImage(imageUrl);

                while (!imageTask.IsCompleted)
                {
                    yield return null;
                }

                if (imageTask.Result != null)
                {
                    // Cleanup previous
                    if (_loadedDishImage != null)
                    {
                        Destroy(_loadedDishImage);
                    }

                    _loadedDishImage = imageTask.Result;
                    _dishImage.texture = _loadedDishImage;
                    _dishImage.gameObject.SetActive(true);

                    // Set aspect ratio
                    if (_imageAspectFitter != null)
                    {
                        _imageAspectFitter.aspectRatio = (float)_loadedDishImage.width / _loadedDishImage.height;
                    }

                    // Animate reveal
                    AnimateImageReveal();
                }
                else
                {
                    // Show error placeholder
                    if (_imageErrorPlaceholder != null)
                        _imageErrorPlaceholder.SetActive(true);
                }
            }
            else
            {
                // No image URL
                if (_imageErrorPlaceholder != null)
                    _imageErrorPlaceholder.SetActive(true);
            }

            // Hide loading
            if (_imageLoadingIndicator != null)
                _imageLoadingIndicator.SetActive(false);
        }

        private void UpdateRoundProgress()
        {
            if (!RoundLoopController.HasInstance) return;

            int current = RoundLoopController.Instance.CurrentRound;
            int total = RoundLoopController.Instance.TotalRounds;

            if (_roundText != null)
            {
                _roundText.text = $"Round {current}/{total}";
            }

            if (_roundProgressSlider != null)
            {
                _roundProgressSlider.value = (float)current / total;
            }
        }

        /// <summary>
        /// Display final match results
        /// </summary>
        private void DisplayMatchResults(MatchData matchData)
        {
            if (matchData == null) return;

            var winner = matchData.DetermineOverallWinner();

            if (_winnerNameText != null)
            {
                _winnerNameText.text = $"MATCH WINNER: {winner?.PlayerName ?? "Unknown"}";
            }

            if (_winnerScoreText != null && winner != null)
            {
                _winnerScoreText.text = $"Total Score: {winner.TotalScore} | Rounds Won: {winner.RoundsWon}";
            }

            if (_continueButtonText != null)
            {
                _continueButtonText.text = "Return to Lobby";
            }
        }

        private void ShowMatchEndResults()
        {
            Show();
            
            if (_continueButtonText != null)
            {
                _continueButtonText.text = "Return to Lobby";
            }
        }

        #endregion

        #region Helpers

        private void ResetDisplay()
        {
            if (_winnerNameText != null) _winnerNameText.text = string.Empty;
            if (_winningDishNameText != null) _winningDishNameText.text = string.Empty;
            if (_winnerScoreText != null) _winnerScoreText.text = string.Empty;
            if (_winnerCrown != null) _winnerCrown.SetActive(false);
            if (_imageLoadingIndicator != null) _imageLoadingIndicator.SetActive(false);
            if (_imageErrorPlaceholder != null) _imageErrorPlaceholder.SetActive(false);
            if (_dishImage != null) _dishImage.gameObject.SetActive(false);

            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }
        }

        private void AnimateTextPop(Transform target)
        {
            StartCoroutine(PopAnimation(target));
        }

        private IEnumerator PopAnimation(Transform target)
        {
            Vector3 originalScale = target.localScale;
            target.localScale = Vector3.zero;

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.SmoothStep(0f, 1f, t);
                
                // Overshoot
                if (t > 0.7f)
                {
                    scale = 1f + (1f - t) * 0.3f;
                }

                target.localScale = originalScale * scale;
                yield return null;
            }

            target.localScale = originalScale;
        }

        private void AnimateImageReveal()
        {
            if (_dishImage != null)
            {
                StartCoroutine(FadeInImage());
            }
        }

        private IEnumerator FadeInImage()
        {
            CanvasGroup cg = _dishImage.GetComponent<CanvasGroup>();
            if (cg == null) cg = _dishImage.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;

            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = elapsed / duration;
                yield return null;
            }

            cg.alpha = 1f;
        }

        private IEnumerator AutoAdvanceCoroutine()
        {
            float remaining = _autoAdvanceTime;

            while (remaining > 0)
            {
                remaining -= Time.deltaTime;

                if (_continueButtonText != null)
                {
                    _continueButtonText.text = $"Continue ({Mathf.CeilToInt(remaining)}s)";
                }

                yield return null;
            }

            OnContinueClicked();
        }

        private void OnContinueClicked()
        {
            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            // The RoundLoopController will handle the transition
            Hide();
        }

        #endregion
    }

    /// <summary>
    /// Helper component for displaying judge comments
    /// </summary>
    [System.Serializable]
    public class JudgeCommentDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _judgeNameText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _commentText;
        [SerializeField] private Image _judgeIcon;
        [SerializeField] private Sprite _judgeSprite;

        public void Display(JudgeVerdict verdict)
        {
            if (verdict == null) return;

            if (_judgeNameText != null)
                _judgeNameText.text = verdict.JudgeName;

            if (_scoreText != null)
                _scoreText.text = $"{verdict.Score}/10";

            if (_commentText != null)
                _commentText.text = $"\"{verdict.Comment}\"";

            if (_judgeIcon != null && _judgeSprite != null)
                _judgeIcon.sprite = _judgeSprite;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
