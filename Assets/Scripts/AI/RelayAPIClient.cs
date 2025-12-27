using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using MasterCheff.Core;
using MasterCheff.Data;

namespace MasterCheff.AI
{
    /// <summary>
    /// HTTP client for communicating with the relay backend (Firebase/AWS Lambda)
    /// Handles secure API calls without exposing API keys
    /// </summary>
    public class RelayAPIClient : Singleton<RelayAPIClient>
    {
        [Header("API Configuration")]
        [SerializeField] private string _baseUrl = "https://your-project.cloudfunctions.net";
        [SerializeField] private string _judgeEndpoint = "/judge";
        
        [Header("Timeouts")]
        [SerializeField] private int _judgeTimeoutSeconds = 30;

        [Header("Retry Settings")]
        [SerializeField] private int _maxRetries = 2;
        [SerializeField] private float _retryDelaySeconds = 1f;

        // Events
        public event Action<string> OnRequestStarted;
        public event Action<string> OnRequestCompleted;
        public event Action<string, string> OnRequestFailed;

        // Properties
        public string BaseUrl 
        { 
            get => _baseUrl;
            set => _baseUrl = value;
        }

        protected override void OnSingletonAwake()
        {
            Debug.Log("[RelayAPIClient] Initialized");
        }

        #region Public API Methods

        /// <summary>
        /// Send player submissions for AI judging
        /// </summary>
        public async Task<JudgeApiResponse> RequestJudgment(JudgeRequest request)
        {
            string url = $"{_baseUrl}{_judgeEndpoint}";
            string jsonBody = JsonUtility.ToJson(request);

            Debug.Log($"[RelayAPIClient] Sending judge request to {url}");
            OnRequestStarted?.Invoke("judgment");

            try
            {
                string response = await PostJsonAsync(url, jsonBody, _judgeTimeoutSeconds);
                
                if (!string.IsNullOrEmpty(response))
                {
                    var result = JsonUtility.FromJson<JudgeApiResponse>(response);
                    OnRequestCompleted?.Invoke("judgment");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayAPIClient] Judge request failed: {ex.Message}");
                OnRequestFailed?.Invoke("judgment", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Health check for the backend
        /// </summary>
        public async Task<bool> CheckBackendHealth()
        {
            string url = $"{_baseUrl}/health";

            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = 5;
                    
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    return request.result == UnityWebRequest.Result.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region HTTP Helpers

        /// <summary>
        /// POST JSON data to an endpoint
        /// </summary>
        private async Task<string> PostJsonAsync(string url, string jsonBody, int timeout)
        {
            int attempts = 0;

            while (attempts <= _maxRetries)
            {
                attempts++;

                try
                {
                    using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                    {
                        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json");
                        request.timeout = timeout;

                        var operation = request.SendWebRequest();

                        while (!operation.isDone)
                        {
                            await Task.Yield();
                        }

                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            string response = request.downloadHandler.text;
                            Debug.Log($"[RelayAPIClient] Response received: {response.Substring(0, Mathf.Min(200, response.Length))}...");
                            return response;
                        }
                        else
                        {
                            Debug.LogWarning($"[RelayAPIClient] Request failed (attempt {attempts}): {request.error}");

                            if (attempts <= _maxRetries)
                            {
                                await Task.Delay((int)(_retryDelaySeconds * 1000));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RelayAPIClient] Exception (attempt {attempts}): {ex.Message}");

                    if (attempts <= _maxRetries)
                    {
                        await Task.Delay((int)(_retryDelaySeconds * 1000));
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// GET request to an endpoint
        /// </summary>
        private async Task<string> GetAsync(string url, int timeout)
        {
            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = timeout;

                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        return request.downloadHandler.text;
                    }
                    else
                    {
                        Debug.LogError($"[RelayAPIClient] GET failed: {request.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RelayAPIClient] GET exception: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Configure the API endpoints at runtime
        /// </summary>
        public void Configure(string baseUrl, string judgeEndpoint = null)
        {
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _baseUrl = baseUrl.TrimEnd('/');
            }

            if (!string.IsNullOrEmpty(judgeEndpoint))
            {
                _judgeEndpoint = judgeEndpoint.StartsWith("/") ? judgeEndpoint : "/" + judgeEndpoint;
            }

            Debug.Log($"[RelayAPIClient] Configured - Base: {_baseUrl}, Judge: {_judgeEndpoint}");
        }

        #endregion
    }
}

