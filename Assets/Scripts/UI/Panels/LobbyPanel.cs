using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Core;
using MasterCheff.Managers;
using MasterCheff.Multiplayer;
using MasterCheff.Gameplay;

namespace MasterCheff.UI.Panels
{
    /// <summary>
    /// UI Panel for the lobby - handles matchmaking and room management
    /// </summary>
    public class LobbyPanel : UIPanel
    {
        [Header("Player Name")]
        [SerializeField] private TMP_InputField _playerNameInput;
        [SerializeField] private int _maxNameLength = 15;

        [Header("Quick Match")]
        [SerializeField] private Button _quickMatchButton;
        [SerializeField] private TextMeshProUGUI _quickMatchButtonText;

        [Header("Room Code")]
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private Button _createRoomButton;
        [SerializeField] private Button _joinRoomButton;
        [SerializeField] private int _roomCodeLength = 6;

        [Header("Room Info Panel")]
        [SerializeField] private GameObject _roomInfoPanel;
        [SerializeField] private TextMeshProUGUI _roomCodeDisplayText;
        [SerializeField] private Button _copyCodeButton;
        [SerializeField] private Button _leaveRoomButton;
        [SerializeField] private Button _readyButton;
        [SerializeField] private TextMeshProUGUI _readyButtonText;

        [Header("Player List")]
        [SerializeField] private Transform _playerListContainer;
        [SerializeField] private GameObject _playerListItemPrefab;
        [SerializeField] private TextMeshProUGUI _playerCountText;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private GameObject _loadingIndicator;

        [Header("Start Game")]
        [SerializeField] private Button _startGameButton;
        [SerializeField] private TextMeshProUGUI _startGameButtonText;

        [Header("Connection")]
        [SerializeField] private GameObject _connectionPanel;
        [SerializeField] private Button _connectButton;
        [SerializeField] private TextMeshProUGUI _connectionStatusText;

        // State
        private bool _isReady = false;
        private bool _isConnecting = false;
        private bool _isJoiningRoom = false;

        protected override void Awake()
        {
            base.Awake();
            SetupUI();
        }

        protected override void Start()
        {
            base.Start();
            SubscribeToEvents();
            LoadPlayerName();
            UpdateUIState();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SetupUI()
        {
            // Player name
            if (_playerNameInput != null)
            {
                _playerNameInput.characterLimit = _maxNameLength;
                _playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
            }

            // Room code input
            if (_roomCodeInput != null)
            {
                _roomCodeInput.characterLimit = _roomCodeLength;
                _roomCodeInput.onValueChanged.AddListener(OnRoomCodeChanged);
                _roomCodeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            }

            // Buttons
            if (_quickMatchButton != null)
                _quickMatchButton.onClick.AddListener(OnQuickMatchClicked);

            if (_createRoomButton != null)
                _createRoomButton.onClick.AddListener(OnCreateRoomClicked);

            if (_joinRoomButton != null)
                _joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

            if (_leaveRoomButton != null)
                _leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

            if (_readyButton != null)
                _readyButton.onClick.AddListener(OnReadyClicked);

            if (_copyCodeButton != null)
                _copyCodeButton.onClick.AddListener(OnCopyCodeClicked);

            if (_startGameButton != null)
                _startGameButton.onClick.AddListener(OnStartGameClicked);

            if (_connectButton != null)
                _connectButton.onClick.AddListener(OnConnectClicked);

            // Initial state
            if (_roomInfoPanel != null)
                _roomInfoPanel.SetActive(false);

            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);
        }

        private void SubscribeToEvents()
        {
            if (NetworkManager.HasInstance)
            {
                NetworkManager.Instance.OnConnectedToServer += HandleConnected;
                NetworkManager.Instance.OnConnectionFailed += HandleConnectionFailed;
                NetworkManager.Instance.OnRoomCreated += HandleRoomCreated;
                NetworkManager.Instance.OnRoomJoined += HandleRoomJoined;
                NetworkManager.Instance.OnRoomJoinFailed += HandleRoomJoinFailed;
                NetworkManager.Instance.OnPlayerJoined += HandlePlayerJoined;
                NetworkManager.Instance.OnPlayerLeft += HandlePlayerLeft;
                NetworkManager.Instance.OnAllPlayersReady += HandleAllPlayersReady;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (NetworkManager.HasInstance)
            {
                NetworkManager.Instance.OnConnectedToServer -= HandleConnected;
                NetworkManager.Instance.OnConnectionFailed -= HandleConnectionFailed;
                NetworkManager.Instance.OnRoomCreated -= HandleRoomCreated;
                NetworkManager.Instance.OnRoomJoined -= HandleRoomJoined;
                NetworkManager.Instance.OnRoomJoinFailed -= HandleRoomJoinFailed;
                NetworkManager.Instance.OnPlayerJoined -= HandlePlayerJoined;
                NetworkManager.Instance.OnPlayerLeft -= HandlePlayerLeft;
                NetworkManager.Instance.OnAllPlayersReady -= HandleAllPlayersReady;
            }
        }

        #region UI Callbacks

        private void OnPlayerNameChanged(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Chef_{UnityEngine.Random.Range(1000, 9999)}";
                _playerNameInput.text = name;
            }

            PlayerPrefs.SetString("PlayerName", name);
            PlayerPrefs.Save();

            if (NetworkManager.HasInstance)
            {
                NetworkManager.Instance.SetPlayerName(name);
            }
        }

        private void OnRoomCodeChanged(string code)
        {
            // Auto-uppercase
            if (_roomCodeInput != null && code != code.ToUpper())
            {
                _roomCodeInput.text = code.ToUpper();
            }

            UpdateJoinButtonState();
        }

        private void OnConnectClicked()
        {
            if (!NetworkManager.HasInstance) return;

            _isConnecting = true;
            SetStatus("Connecting...", true);

            string playerName = _playerNameInput?.text ?? $"Chef_{UnityEngine.Random.Range(1000, 9999)}";
            NetworkManager.Instance.Connect(playerName);
        }

        private void OnQuickMatchClicked()
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsConnected) return;

            _isJoiningRoom = true;
            SetStatus("Finding match...", true);
            NetworkManager.Instance.QuickMatch();
        }

        private void OnCreateRoomClicked()
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsConnected) return;

            _isJoiningRoom = true;
            SetStatus("Creating room...", true);
            NetworkManager.Instance.CreateRoom(null); // Auto-generate code
        }

        private void OnJoinRoomClicked()
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsConnected) return;

            string code = _roomCodeInput?.text?.Trim().ToUpper();
            if (string.IsNullOrEmpty(code) || code.Length != _roomCodeLength)
            {
                SetStatus("Enter a valid room code", false);
                return;
            }

            _isJoiningRoom = true;
            SetStatus("Joining room...", true);
            NetworkManager.Instance.JoinRoom(code);
        }

        private void OnLeaveRoomClicked()
        {
            if (!NetworkManager.HasInstance) return;

            NetworkManager.Instance.LeaveRoom();
            _isReady = false;
            ShowMatchmakingUI();
            SetStatus("Left room", false);
        }

        private void OnReadyClicked()
        {
            if (!NetworkManager.HasInstance) return;

            _isReady = !_isReady;
            NetworkManager.Instance.SetPlayerReady(_isReady);
            UpdateReadyButton();
        }

        private void OnCopyCodeClicked()
        {
            if (NetworkManager.HasInstance && NetworkManager.Instance.IsInRoom)
            {
                GUIUtility.systemCopyBuffer = NetworkManager.Instance.CurrentRoomCode;
                SetStatus("Room code copied!", false);
            }
        }

        private void OnStartGameClicked()
        {
            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsMasterClient) return;

            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.InitializeMatch();
                RoundLoopController.Instance.StartMatch();
            }
        }

        #endregion

        #region Event Handlers

        private void HandleConnected()
        {
            _isConnecting = false;
            SetStatus("Connected!", false);
            UpdateUIState();
        }

        private void HandleConnectionFailed(string error)
        {
            _isConnecting = false;
            SetStatus($"Connection failed: {error}", false);
            UpdateUIState();
        }

        private void HandleRoomCreated()
        {
            SetStatus("Room created!", false);
        }

        private void HandleRoomJoined()
        {
            _isJoiningRoom = false;
            _isReady = false;
            ShowRoomUI();
            RefreshPlayerList();
            SetStatus("Joined room!", false);
        }

        private void HandleRoomJoinFailed(string error)
        {
            _isJoiningRoom = false;
            SetStatus($"Join failed: {error}", false);
        }

        private void HandlePlayerJoined(PlayerNetworkData player)
        {
            RefreshPlayerList();
            SetStatus($"{player.PlayerName} joined!", false);
        }

        private void HandlePlayerLeft(PlayerNetworkData player)
        {
            RefreshPlayerList();
            SetStatus($"{player.PlayerName} left", false);
        }

        private void HandleAllPlayersReady()
        {
            SetStatus("All players ready!", false);
            UpdateStartButton();
        }

        #endregion

        #region UI Updates

        private void UpdateUIState()
        {
            bool isConnected = NetworkManager.HasInstance && NetworkManager.Instance.IsConnected;
            bool isInRoom = NetworkManager.HasInstance && NetworkManager.Instance.IsInRoom;

            // Connection panel
            if (_connectionPanel != null)
                _connectionPanel.SetActive(!isConnected);

            // Matchmaking buttons
            if (_quickMatchButton != null)
                _quickMatchButton.interactable = isConnected && !isInRoom && !_isJoiningRoom;

            if (_createRoomButton != null)
                _createRoomButton.interactable = isConnected && !isInRoom && !_isJoiningRoom;

            UpdateJoinButtonState();

            // Connection status
            if (_connectionStatusText != null)
            {
                _connectionStatusText.text = isConnected ? "Connected" : "Not Connected";
            }
        }

        private void UpdateJoinButtonState()
        {
            if (_joinRoomButton == null) return;

            bool isConnected = NetworkManager.HasInstance && NetworkManager.Instance.IsConnected;
            bool isInRoom = NetworkManager.HasInstance && NetworkManager.Instance.IsInRoom;
            bool hasCode = !string.IsNullOrEmpty(_roomCodeInput?.text) && 
                           _roomCodeInput.text.Length == _roomCodeLength;

            _joinRoomButton.interactable = isConnected && !isInRoom && hasCode && !_isJoiningRoom;
        }

        private void ShowMatchmakingUI()
        {
            if (_roomInfoPanel != null)
                _roomInfoPanel.SetActive(false);

            UpdateUIState();
        }

        private void ShowRoomUI()
        {
            if (_roomInfoPanel != null)
                _roomInfoPanel.SetActive(true);

            // Display room code
            if (_roomCodeDisplayText != null && NetworkManager.HasInstance)
            {
                _roomCodeDisplayText.text = NetworkManager.Instance.CurrentRoomCode;
            }

            UpdateReadyButton();
            UpdateStartButton();
            UpdatePlayerCount();
        }

        private void UpdateReadyButton()
        {
            if (_readyButton == null) return;

            if (_readyButtonText != null)
            {
                _readyButtonText.text = _isReady ? "Not Ready" : "Ready!";
            }

            // Visual feedback
            var buttonImage = _readyButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = _isReady ? Color.green : Color.white;
            }
        }

        private void UpdateStartButton()
        {
            if (_startGameButton == null) return;

            bool isMaster = NetworkManager.HasInstance && NetworkManager.Instance.IsMasterClient;
            bool allReady = NetworkManager.HasInstance && NetworkManager.Instance.AreAllPlayersReady();
            bool enoughPlayers = NetworkManager.HasInstance && NetworkManager.Instance.PlayerCount >= 2;

            _startGameButton.gameObject.SetActive(isMaster);
            _startGameButton.interactable = allReady && enoughPlayers;

            if (_startGameButtonText != null)
            {
                if (!enoughPlayers)
                    _startGameButtonText.text = "Waiting for players...";
                else if (!allReady)
                    _startGameButtonText.text = "Waiting for ready...";
                else
                    _startGameButtonText.text = "Start Game!";
            }
        }

        private void UpdatePlayerCount()
        {
            if (_playerCountText != null && NetworkManager.HasInstance)
            {
                _playerCountText.text = $"{NetworkManager.Instance.PlayerCount}/{NetworkManager.Instance.MaxPlayers}";
            }
        }

        private void RefreshPlayerList()
        {
            if (_playerListContainer == null || !NetworkManager.HasInstance) return;

            // Clear existing items
            foreach (Transform child in _playerListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new items
            foreach (var kvp in NetworkManager.Instance.PlayersInRoom)
            {
                CreatePlayerListItem(kvp.Value);
            }

            UpdatePlayerCount();
            UpdateStartButton();
        }

        private void CreatePlayerListItem(PlayerNetworkData player)
        {
            if (_playerListItemPrefab == null || _playerListContainer == null) return;

            GameObject item = Instantiate(_playerListItemPrefab, _playerListContainer);
            
            // Set player name
            var nameText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                string displayName = player.PlayerName;
                if (player.IsMasterClient)
                    displayName += " (Host)";
                if (NetworkManager.HasInstance && player.PlayerId == NetworkManager.Instance.LocalPlayerId)
                    displayName += " (You)";
                
                nameText.text = displayName;
            }

            // Set ready indicator (if available)
            var readyIndicator = item.transform.Find("ReadyIndicator");
            if (readyIndicator != null)
            {
                readyIndicator.gameObject.SetActive(player.IsReady);
            }
        }

        private void SetStatus(string message, bool showLoading)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }

            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(showLoading);
            }
        }

        private void LoadPlayerName()
        {
            string savedName = PlayerPrefs.GetString("PlayerName", $"Chef_{UnityEngine.Random.Range(1000, 9999)}");
            if (_playerNameInput != null)
            {
                _playerNameInput.text = savedName;
            }
        }

        #endregion

        #region Public Methods

        public override void Show()
        {
            base.Show();
            UpdateUIState();

            // Auto-connect if not connected
            if (NetworkManager.HasInstance && !NetworkManager.Instance.IsConnected)
            {
                StartCoroutine(AutoConnectCoroutine());
            }
        }

        private IEnumerator AutoConnectCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (NetworkManager.HasInstance && !NetworkManager.Instance.IsConnected && !_isConnecting)
            {
                OnConnectClicked();
            }
        }

        #endregion
    }
}

