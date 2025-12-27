using UnityEngine;
using MasterCheff.Managers;

namespace MasterCheff.Core
{
    /// <summary>
    /// Game Bootstrapper - Initializes all game systems
    /// This should be in the first scene that loads
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _loadSaveOnStart = true;

        [Header("Optional Prefabs")]
        [SerializeField] private GameObject _gameManagerPrefab;
        [SerializeField] private GameObject _audioManagerPrefab;
        [SerializeField] private GameObject _uiManagerPrefab;

        private static bool _isInitialized = false;

        private void Awake()
        {
            if (_isInitialized)
            {
                Destroy(gameObject);
                return;
            }

            if (_initializeOnAwake)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            DontDestroyOnLoad(gameObject);

            // Initialize managers in order
            InitializeManagers();

            // Load saved data
            if (_loadSaveOnStart)
            {
                LoadSavedData();
            }

            _isInitialized = true;
            Debug.Log("[GameBootstrapper] Game initialized");
        }

        private void InitializeManagers()
        {
            // Game Manager
            if (!GameManager.HasInstance)
            {
                if (_gameManagerPrefab != null)
                {
                    Instantiate(_gameManagerPrefab);
                }
                else
                {
                    // Will auto-create via Singleton pattern
                    var _ = GameManager.Instance;
                }
            }

            // Audio Manager
            if (!AudioManager.HasInstance)
            {
                if (_audioManagerPrefab != null)
                {
                    Instantiate(_audioManagerPrefab);
                }
                else
                {
                    var _ = AudioManager.Instance;
                }
            }

            // Save Manager
            if (!SaveManager.HasInstance)
            {
                var _ = SaveManager.Instance;
            }

            // Event Manager
            if (!EventManager.HasInstance)
            {
                var _ = EventManager.Instance;
            }

            // Scene Loader
            if (!SceneLoader.HasInstance)
            {
                var _ = SceneLoader.Instance;
            }
        }

        private void LoadSavedData()
        {
            if (SaveManager.HasInstance)
            {
                bool hasData = SaveManager.Instance.LoadGame();
                if (hasData)
                {
                    ApplySavedSettings();
                }
            }
        }

        private void ApplySavedSettings()
        {
            var saveData = SaveManager.Instance.CurrentSaveData;

            // Apply audio settings
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.SetMusicVolume(saveData.musicVolume);
                AudioManager.Instance.SetSFXVolume(saveData.sfxVolume);
                AudioManager.Instance.SetMusicMuted(saveData.isMusicMuted);
                AudioManager.Instance.SetSFXMuted(saveData.isSfxMuted);
            }
        }
    }
}


