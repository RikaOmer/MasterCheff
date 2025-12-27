using UnityEngine;
using System;
using MasterCheff.Core;

namespace MasterCheff.Managers
{
    /// <summary>
    /// Main Game Manager - Controls game flow and state
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game Settings")]
        [SerializeField] private bool _pauseOnFocusLost = true;
        [SerializeField] private int _targetFrameRate = 60;

        // Game State
        private GameState _currentState = GameState.Loading;
        private GameState _previousState;
        private bool _isPaused = false;

        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnGameOver;
        public event Action OnVictory;

        // Properties
        public GameState CurrentState => _currentState;
        public GameState PreviousState => _previousState;
        public bool IsPaused => _isPaused;
        public bool IsPlaying => _currentState == GameState.Playing && !_isPaused;

        // Score and Level
        public int CurrentScore { get; private set; }
        public int HighScore { get; private set; }
        public int CurrentLevel { get; private set; } = 1;
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Normal;

        protected override void OnSingletonAwake()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Set target frame rate for mobile
            Application.targetFrameRate = _targetFrameRate;
            
            // Prevent screen from sleeping on mobile
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Load high score
            HighScore = PlayerPrefs.GetInt("HighScore", 0);

            Debug.Log("[GameManager] Initialized");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_pauseOnFocusLost && _currentState == GameState.Playing)
            {
                if (!hasFocus)
                {
                    PauseGame();
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_pauseOnFocusLost && _currentState == GameState.Playing)
            {
                if (pauseStatus)
                {
                    PauseGame();
                }
            }
        }

        #region Game State Management

        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;

            Debug.Log($"[GameManager] State changed: {_previousState} -> {_currentState}");

            OnGameStateChanged?.Invoke(_currentState);

            HandleStateChange();
        }

        private void HandleStateChange()
        {
            switch (_currentState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    _isPaused = false;
                    break;

                case GameState.Playing:
                    Time.timeScale = 1f;
                    _isPaused = false;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    _isPaused = true;
                    OnGamePaused?.Invoke();
                    break;

                case GameState.GameOver:
                    Time.timeScale = 1f;
                    _isPaused = false;
                    CheckHighScore();
                    OnGameOver?.Invoke();
                    break;

                case GameState.Victory:
                    Time.timeScale = 1f;
                    _isPaused = false;
                    CheckHighScore();
                    OnVictory?.Invoke();
                    break;
            }
        }

        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                OnGameResumed?.Invoke();
            }
        }

        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (_currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        #endregion

        #region Score Management

        public void AddScore(int points)
        {
            CurrentScore += points;
        }

        public void ResetScore()
        {
            CurrentScore = 0;
        }

        private void CheckHighScore()
        {
            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                PlayerPrefs.SetInt("HighScore", HighScore);
                PlayerPrefs.Save();
                Debug.Log($"[GameManager] New High Score: {HighScore}");
            }
        }

        #endregion

        #region Level Management

        public void SetLevel(int level)
        {
            CurrentLevel = Mathf.Max(1, level);
        }

        public void NextLevel()
        {
            CurrentLevel++;
        }

        #endregion

        #region Game Flow

        public void StartGame()
        {
            ResetScore();
            SetLevel(1);
            ChangeState(GameState.Playing);
        }

        public void RestartGame()
        {
            StartGame();
        }

        public void GoToMainMenu()
        {
            ChangeState(GameState.MainMenu);
        }

        public void TriggerGameOver()
        {
            ChangeState(GameState.GameOver);
        }

        public void TriggerVictory()
        {
            ChangeState(GameState.Victory);
        }

        #endregion
    }
}


