using UnityEngine;
using System;
using System.IO;
using MasterCheff.Core;
using MasterCheff.Data;

namespace MasterCheff.Managers
{
    /// <summary>
    /// Save Manager - Handles game save/load functionality
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        [Header("Settings")]
        [SerializeField] private string _saveFileName = "gamesave.json";
        [SerializeField] private bool _encryptSaveData = false;
        [SerializeField] private string _encryptionKey = "MasterCheffGameKey2024";

        private string SavePath => Path.Combine(Application.persistentDataPath, _saveFileName);

        // Events
        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;
        public event Action<string> OnSaveError;
        public event Action<string> OnLoadError;

        // Current save data
        public GameSaveData CurrentSaveData { get; private set; }

        protected override void OnSingletonAwake()
        {
            CurrentSaveData = new GameSaveData();
            Debug.Log($"[SaveManager] Save path: {SavePath}");
        }

        #region Save Operations

        public void SaveGame()
        {
            try
            {
                // Update save timestamp
                CurrentSaveData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                CurrentSaveData.saveVersion++;

                string json = JsonUtility.ToJson(CurrentSaveData, true);

                if (_encryptSaveData)
                {
                    json = EncryptString(json);
                }

                File.WriteAllText(SavePath, json);

                Debug.Log("[SaveManager] Game saved successfully");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
                OnSaveError?.Invoke(e.Message);
            }
        }

        public void SaveGameAsync(Action onComplete = null)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                SaveGame();
            }).ContinueWith(task =>
            {
                // Return to main thread
                UnityMainThreadDispatcher.Enqueue(() => onComplete?.Invoke());
            });
        }

        #endregion

        #region Load Operations

        public bool LoadGame()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Debug.Log("[SaveManager] No save file found, creating new save data");
                    CurrentSaveData = new GameSaveData();
                    return false;
                }

                string json = File.ReadAllText(SavePath);

                if (_encryptSaveData)
                {
                    json = DecryptString(json);
                }

                CurrentSaveData = JsonUtility.FromJson<GameSaveData>(json);

                Debug.Log("[SaveManager] Game loaded successfully");
                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}");
                OnLoadError?.Invoke(e.Message);
                CurrentSaveData = new GameSaveData();
                return false;
            }
        }

        public void LoadGameAsync(Action<bool> onComplete = null)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                return LoadGame();
            }).ContinueWith(task =>
            {
                // Return to main thread
                UnityMainThreadDispatcher.Enqueue(() => onComplete?.Invoke(task.Result));
            });
        }

        #endregion

        #region Delete Operations

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log("[SaveManager] Save file deleted");
                }

                CurrentSaveData = new GameSaveData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Delete failed: {e.Message}");
            }
        }

        public bool HasSaveData()
        {
            return File.Exists(SavePath);
        }

        #endregion

        #region Quick Save Methods

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        public void SavePrefs()
        {
            PlayerPrefs.Save();
        }

        public void DeleteAllPrefs()
        {
            PlayerPrefs.DeleteAll();
        }

        #endregion

        #region Encryption (Basic XOR)

        private string EncryptString(string data)
        {
            char[] result = new char[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (char)(data[i] ^ _encryptionKey[i % _encryptionKey.Length]);
            }
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string(result)));
        }

        private string DecryptString(string data)
        {
            byte[] bytes = Convert.FromBase64String(data);
            string decoded = System.Text.Encoding.UTF8.GetString(bytes);
            char[] result = new char[decoded.Length];
            for (int i = 0; i < decoded.Length; i++)
            {
                result[i] = (char)(decoded[i] ^ _encryptionKey[i % _encryptionKey.Length]);
            }
            return new string(result);
        }

        #endregion
    }

    /// <summary>
    /// Helper class to dispatch actions to the main thread
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly System.Collections.Generic.Queue<Action> _actionQueue = new System.Collections.Generic.Queue<Action>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            lock (_actionQueue)
            {
                while (_actionQueue.Count > 0)
                {
                    _actionQueue.Dequeue()?.Invoke();
                }
            }
        }

        public static void Enqueue(Action action)
        {
            if (_instance == null)
            {
                var go = new GameObject("[UnityMainThreadDispatcher]");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
            }

            lock (_actionQueue)
            {
                _actionQueue.Enqueue(action);
            }
        }
    }
}

