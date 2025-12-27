using System;
using System.Collections;
using UnityEngine;
using MasterCheff.Core;
using MasterCheff.Data;
using MasterCheff.Managers;
using MasterCheff.Multiplayer;
using MasterCheff.Utils;
using MasterCheff.AI;

namespace MasterCheff.Gameplay
{
    /// <summary>
    /// Controls the game loop state machine for match phases
    /// </summary>
    public class RoundLoopController : SingletonScoped<RoundLoopController>
    {
        [Header("Phase Durations")]
        [SerializeField] private float _ingredientRevealDuration = 3f;
        [SerializeField] private float _cookingPhaseDuration = 60f;
        [SerializeField] private float _judgingPhaseDuration = 15f;
        [SerializeField] private float _resultsPhaseDuration = 10f;
        [SerializeField] private float _timeExtensionBonus = 15f;

        [Header("Match Settings")]
        [SerializeField] private int _totalRounds = 10;
        [SerializeField] private int _minPlayersToStart = 2;

        [Header("References")]
        [SerializeField] private IngredientDatabase _ingredientDatabase;

        // Match State
        private MatchData _currentMatch;
        private int _currentRound = 0;
        private GameState _currentPhase = GameState.WaitingForPlayers;
        private RoundIngredients _currentIngredients;
        private PlayerSubmission _localSubmission;
        private RoundResult _currentRoundResult;
        private bool _hasSubmitted = false;

        // Timer
        private Timer _phaseTimer;
        private float _timerSyncInterval = 1f;
        private float _lastTimerSync;

        // Events
        public event Action<GameState> OnPhaseChanged;
        public event Action<RoundIngredients> OnIngredientsRevealed;
        public event Action<float> OnTimerUpdated;
        public event Action<RoundResult> OnRoundComplete;
        public event Action<MatchData> OnMatchComplete;
        public event Action<PlayerSubmission> OnSubmissionConfirmed;

        // Properties
        public int CurrentRound => _currentRound;
        public int TotalRounds => _totalRounds;
        public GameState CurrentPhase => _currentPhase;
        public RoundIngredients CurrentIngredients => _currentIngredients;
        public MatchData CurrentMatch => _currentMatch;
        public float RemainingTime => _phaseTimer?.RemainingTime ?? 0f;
        public bool HasSubmitted => _hasSubmitted;
        public bool IsCookingPhase => _currentPhase == GameState.Cooking;

        protected override void OnSingletonAwake()
        {
            _phaseTimer = new Timer(_cookingPhaseDuration);
            _phaseTimer.OnTimerTick += HandleTimerTick;
            _phaseTimer.OnTimerComplete += HandleTimerComplete;

            SubscribeToEvents();
            Debug.Log("[RoundLoopController] Initialized");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (_phaseTimer != null)
            {
                _phaseTimer.OnTimerTick -= HandleTimerTick;
                _phaseTimer.OnTimerComplete -= HandleTimerComplete;
            }
        }

        private void Update()
        {
            if (_phaseTimer != null && _phaseTimer.IsRunning)
            {
                _phaseTimer.Tick(Time.deltaTime);

                // Sync timer periodically (Master Client only)
                if (NetworkManager.HasInstance && NetworkManager.Instance.IsMasterClient)
                {
                    if (Time.time - _lastTimerSync >= _timerSyncInterval)
                    {
                        _lastTimerSync = Time.time;
                        NetworkManager.Instance.BroadcastTimerSync(_phaseTimer.RemainingTime);
                    }
                }
            }
        }

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            if (EventManager.HasInstance)
            {
                EventManager.Instance.Subscribe<RoundIngredients>("IngredientsReceived", HandleIngredientsReceived);
                EventManager.Instance.Subscribe<RoundResult>("RoundResultReceived", HandleRoundResultReceived);
                EventManager.Instance.Subscribe<GameState>("PhaseChanged", HandlePhaseChangeReceived);
                EventManager.Instance.Subscribe<float>("TimerSync", HandleTimerSync);
            }

            if (NetworkManager.HasInstance)
            {
                NetworkManager.Instance.OnAllPlayersReady += HandleAllPlayersReady;
                NetworkManager.Instance.OnAllSubmissionsReceived += HandleAllSubmissionsReceived;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (EventManager.HasInstance)
            {
                EventManager.Instance.Unsubscribe<RoundIngredients>("IngredientsReceived", HandleIngredientsReceived);
                EventManager.Instance.Unsubscribe<RoundResult>("RoundResultReceived", HandleRoundResultReceived);
                EventManager.Instance.Unsubscribe<GameState>("PhaseChanged", HandlePhaseChangeReceived);
                EventManager.Instance.Unsubscribe<float>("TimerSync", HandleTimerSync);
            }

            if (NetworkManager.HasInstance)
            {
                NetworkManager.Instance.OnAllPlayersReady -= HandleAllPlayersReady;
                NetworkManager.Instance.OnAllSubmissionsReceived -= HandleAllSubmissionsReceived;
            }
        }

        #endregion

        #region Match Control

        /// <summary>
        /// Initialize a new match
        /// </summary>
        public void InitializeMatch()
        {
            _currentMatch = new MatchData();
            _currentRound = 0;
            _currentPhase = GameState.WaitingForPlayers;

            // Initialize player scores from network players
            if (NetworkManager.HasInstance)
            {
                var players = NetworkManager.Instance.PlayersInRoom;
                var scores = new PlayerMatchScore[players.Count];
                int index = 0;
                foreach (var kvp in players)
                {
                    scores[index++] = new PlayerMatchScore(kvp.Key, kvp.Value.PlayerName);
                }
                _currentMatch.InitializePlayers(scores);
            }

            Debug.Log("[RoundLoopController] Match initialized");
            SetPhase(GameState.WaitingForPlayers);
        }

        /// <summary>
        /// Start the match (called when all players are ready)
        /// </summary>
        public void StartMatch()
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsMasterClient)
            {
                Debug.LogWarning("[RoundLoopController] Only Master Client can start match");
                return;
            }

            if (NetworkManager.Instance.PlayerCount < _minPlayersToStart)
            {
                Debug.LogWarning($"[RoundLoopController] Not enough players. Need {_minPlayersToStart}, have {NetworkManager.Instance.PlayerCount}");
                return;
            }

            Debug.Log("[RoundLoopController] Starting match!");
            _currentRound = 0;
            StartNextRound();
        }

        /// <summary>
        /// Start the next round
        /// </summary>
        private void StartNextRound()
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsMasterClient) return;

            _currentRound++;
            _hasSubmitted = false;
            NetworkManager.Instance.ClearSubmissions();

            if (_currentRound > _totalRounds)
            {
                EndMatch();
                return;
            }

            Debug.Log($"[RoundLoopController] Starting round {_currentRound}/{_totalRounds}");

            // Pick random ingredients
            _currentIngredients = PickRandomIngredients();
            NetworkManager.Instance.BroadcastIngredients(_currentIngredients);

            // Transition to ingredient reveal
            TransitionToPhase(GameState.IngredientReveal);
        }

        /// <summary>
        /// End the current match
        /// </summary>
        private void EndMatch()
        {
            var winner = _currentMatch.DetermineOverallWinner();
            Debug.Log($"[RoundLoopController] Match ended! Winner: {winner?.PlayerName ?? "No winner"}");

            TransitionToPhase(GameState.MatchEnd);
            OnMatchComplete?.Invoke(_currentMatch);
        }

        #endregion

        #region Phase Transitions

        /// <summary>
        /// Transition to a new phase (Master Client broadcasts)
        /// </summary>
        private void TransitionToPhase(GameState newPhase)
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsMasterClient) return;

            NetworkManager.Instance.BroadcastPhaseChange(newPhase);
        }

        /// <summary>
        /// Set the current phase locally
        /// </summary>
        private void SetPhase(GameState phase)
        {
            if (_currentPhase == phase) return;

            GameState previousPhase = _currentPhase;
            _currentPhase = phase;

            Debug.Log($"[RoundLoopController] Phase: {previousPhase} -> {phase}");

            // Stop any running timer
            _phaseTimer.Stop();

            // Handle phase-specific logic
            switch (phase)
            {
                case GameState.WaitingForPlayers:
                    HandleWaitingForPlayersPhase();
                    break;
                case GameState.IngredientReveal:
                    HandleIngredientRevealPhase();
                    break;
                case GameState.Cooking:
                    HandleCookingPhase();
                    break;
                case GameState.Judging:
                    HandleJudgingPhase();
                    break;
                case GameState.RoundResults:
                    HandleRoundResultsPhase();
                    break;
                case GameState.MatchEnd:
                    HandleMatchEndPhase();
                    break;
            }

            OnPhaseChanged?.Invoke(phase);

            // Update GameManager state if available
            if (GameManager.HasInstance)
            {
                GameManager.Instance.ChangeState(phase);
            }
        }

        private void HandleWaitingForPlayersPhase()
        {
            // Waiting for players to connect and ready up
            Debug.Log("[RoundLoopController] Waiting for players...");
        }

        private void HandleIngredientRevealPhase()
        {
            Debug.Log($"[RoundLoopController] Revealing ingredients: {_currentIngredients}");
            OnIngredientsRevealed?.Invoke(_currentIngredients);

            // Start timer to auto-transition to cooking
            _phaseTimer.Start(_ingredientRevealDuration);
        }

        private void HandleCookingPhase()
        {
            Debug.Log("[RoundLoopController] Cooking phase started!");
            _hasSubmitted = false;
            _localSubmission = null;

            // Start cooking timer
            _phaseTimer.Start(_cookingPhaseDuration);
        }

        private void HandleJudgingPhase()
        {
            Debug.Log("[RoundLoopController] Judging phase - AI is evaluating...");

            // Start judging timer (for display purposes)
            _phaseTimer.Start(_judgingPhaseDuration);
        }

        private void HandleRoundResultsPhase()
        {
            Debug.Log($"[RoundLoopController] Showing round {_currentRound} results");

            // Update match data
            if (_currentRoundResult != null)
            {
                _currentMatch.RecordRoundResult(_currentRoundResult);
            }

            OnRoundComplete?.Invoke(_currentRoundResult);

            // Start results timer
            _phaseTimer.Start(_resultsPhaseDuration);
        }

        private void HandleMatchEndPhase()
        {
            Debug.Log("[RoundLoopController] Match complete!");
            _phaseTimer.Stop();
        }

        #endregion

        #region Timer Handlers

        private void HandleTimerTick(float remainingTime)
        {
            OnTimerUpdated?.Invoke(remainingTime);
        }

        private void HandleTimerComplete()
        {
            // Handle phase completion based on current phase
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsMasterClient) return;

            switch (_currentPhase)
            {
                case GameState.IngredientReveal:
                    TransitionToPhase(GameState.Cooking);
                    break;
                case GameState.Cooking:
                    TransitionToPhase(GameState.Judging);
                    break;
                case GameState.Judging:
                    // Judging complete - handled by API response
                    break;
                case GameState.RoundResults:
                    StartNextRound();
                    break;
            }
        }

        /// <summary>
        /// Add bonus time to the cooking phase (power-up)
        /// </summary>
        public void AddBonusTime(float seconds)
        {
            if (_currentPhase == GameState.Cooking && _phaseTimer.IsRunning)
            {
                _phaseTimer.AddTime(seconds);
                Debug.Log($"[RoundLoopController] Added {seconds}s bonus time");
            }
        }

        #endregion

        #region Submission Handling

        /// <summary>
        /// Submit the local player's dish
        /// </summary>
        public void SubmitLocalDish(string dishName, string description, DishStyleTag styleTag, string secretIngredient = "")
        {
            if (_currentPhase != GameState.Cooking || _hasSubmitted) return;

            if (!NetworkManager.HasInstance) return;

            _localSubmission = new PlayerSubmission(
                NetworkManager.Instance.LocalPlayerId,
                NetworkManager.Instance.LocalPlayerName
            )
            {
                DishName = dishName,
                Description = description,
                StyleTag = styleTag,
                SecretIngredient = secretIngredient
            };

            if (!_localSubmission.IsValid())
            {
                Debug.LogWarning("[RoundLoopController] Invalid submission - dish name required");
                return;
            }

            _hasSubmitted = true;
            NetworkManager.Instance.SubmitDish(_localSubmission);
            OnSubmissionConfirmed?.Invoke(_localSubmission);

            Debug.Log($"[RoundLoopController] Submitted dish: {dishName}");
        }

        /// <summary>
        /// Get the current local submission
        /// </summary>
        public PlayerSubmission GetLocalSubmission()
        {
            return _localSubmission;
        }

        #endregion

        #region Event Handlers

        private void HandleAllPlayersReady()
        {
            if (_currentPhase == GameState.WaitingForPlayers)
            {
                StartMatch();
            }
        }

        private void HandleIngredientsReceived(RoundIngredients ingredients)
        {
            _currentIngredients = ingredients;
            
            // If we're not master client, we receive this before phase change
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsMasterClient)
            {
                OnIngredientsRevealed?.Invoke(ingredients);
            }
        }

        private void HandlePhaseChangeReceived(GameState phase)
        {
            SetPhase(phase);
        }

        private void HandleTimerSync(float remainingTime)
        {
            // Non-master clients sync their timer
            if (!NetworkManager.HasInstance || NetworkManager.Instance.IsMasterClient) return;

            // Only sync if significant difference
            float diff = Mathf.Abs(_phaseTimer.RemainingTime - remainingTime);
            if (diff > 0.5f)
            {
                // Adjust timer
                float adjustment = remainingTime - _phaseTimer.RemainingTime;
                if (adjustment > 0)
                    _phaseTimer.AddTime(adjustment);
                else
                    _phaseTimer.SubtractTime(-adjustment);
            }
        }

        private void HandleRoundResultReceived(RoundResult result)
        {
            _currentRoundResult = result;
            result.RoundNumber = _currentRound - 1; // 0-indexed

            Debug.Log($"[RoundLoopController] Round result received. Winner: {result.WinnerPlayerName}");
        }

        private async void HandleAllSubmissionsReceived(PlayerSubmission[] submissions)
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsMasterClient) return;

            Debug.Log($"[RoundLoopController] All {submissions.Length} submissions received. Requesting AI judgment...");

            // Transition to judging phase
            TransitionToPhase(GameState.Judging);

            // Call AI service
            if (AIJudgeService.HasInstance)
            {
                try
                {
                    RoundResult result = await AIJudgeService.Instance.JudgeSubmissions(
                        _currentIngredients, 
                        submissions
                    );

                    if (result != null)
                    {
                        _currentRoundResult = result;
                        _currentRoundResult.RoundNumber = _currentRound - 1;

                        // Request image generation for winning dish
                        if (!string.IsNullOrEmpty(result.WinningDishImagePrompt))
                        {
                            StartCoroutine(GenerateWinningDishImage(result));
                        }
                        else
                        {
                            // Broadcast results without image
                            NetworkManager.Instance.BroadcastRoundResult(_currentRoundResult);
                            TransitionToPhase(GameState.RoundResults);
                        }
                    }
                    else
                    {
                        Debug.LogError("[RoundLoopController] AI judgment failed");
                        // Create fallback result
                        CreateFallbackResult(submissions);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RoundLoopController] AI judgment error: {ex.Message}");
                    CreateFallbackResult(submissions);
                }
            }
            else
            {
                Debug.LogWarning("[RoundLoopController] AIJudgeService not available");
                CreateFallbackResult(submissions);
            }
        }

        private IEnumerator GenerateWinningDishImage(RoundResult result)
        {
            if (AIJudgeService.HasInstance)
            {
                var imageTask = AIJudgeService.Instance.GenerateDishImage(result.WinningDishImagePrompt);
                
                while (!imageTask.IsCompleted)
                {
                    yield return null;
                }

                if (imageTask.Result != null)
                {
                    result.WinningDishImageUrl = imageTask.Result.ImageUrl;
                }
            }

            // Broadcast final results
            NetworkManager.Instance.BroadcastRoundResult(result);
            TransitionToPhase(GameState.RoundResults);
        }

        private void CreateFallbackResult(PlayerSubmission[] submissions)
        {
            // Create a basic result with random winner when AI fails
            var result = new RoundResult
            {
                RoundNumber = _currentRound - 1,
                PlayerResults = new PlayerJudgeResult[submissions.Length]
            };

            int highestScore = 0;
            int winnerIndex = 0;

            for (int i = 0; i < submissions.Length; i++)
            {
                int score = UnityEngine.Random.Range(15, 25);
                result.PlayerResults[i] = new PlayerJudgeResult
                {
                    PlayerId = submissions[i].PlayerId,
                    PlayerName = submissions[i].PlayerName,
                    DishName = submissions[i].DishName,
                    Critic = new JudgeVerdict("The Critic", UnityEngine.Random.Range(5, 9), "A respectable effort."),
                    Visionary = new JudgeVerdict("The Visionary", UnityEngine.Random.Range(5, 9), "Shows creative potential."),
                    SoulCook = new JudgeVerdict("The Soul Cook", UnityEngine.Random.Range(5, 9), "Warms the heart."),
                    TotalScore = score
                };

                if (score > highestScore)
                {
                    highestScore = score;
                    winnerIndex = i;
                }
            }

            result.WinnerPlayerId = submissions[winnerIndex].PlayerId;
            result.WinnerPlayerName = submissions[winnerIndex].PlayerName;
            result.WinningDishName = submissions[winnerIndex].DishName;

            _currentRoundResult = result;
            NetworkManager.Instance.BroadcastRoundResult(result);
            TransitionToPhase(GameState.RoundResults);
        }

        #endregion

        #region Ingredients

        /// <summary>
        /// Pick random ingredients for the round
        /// </summary>
        private RoundIngredients PickRandomIngredients()
        {
            if (_ingredientDatabase != null)
            {
                return _ingredientDatabase.GetRandomPair();
            }

            // Fallback ingredients if no database
            string[] fallback = { "Chicken", "Lemon", "Garlic", "Chocolate", "Chili", "Honey", "Ginger", "Butter" };
            int idx1 = UnityEngine.Random.Range(0, fallback.Length);
            int idx2 = (idx1 + UnityEngine.Random.Range(1, fallback.Length - 1)) % fallback.Length;

            return new RoundIngredients(fallback[idx1], fallback[idx2]);
        }

        /// <summary>
        /// Reroll ingredients (power-up)
        /// </summary>
        public void RerollIngredients()
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsMasterClient) return;
            if (_currentPhase != GameState.Cooking) return;

            _currentIngredients = PickRandomIngredients();
            NetworkManager.Instance.BroadcastIngredients(_currentIngredients);
            Debug.Log($"[RoundLoopController] Ingredients rerolled: {_currentIngredients}");
        }

        #endregion
    }
}


