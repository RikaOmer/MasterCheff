using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using MasterCheff.Core;
using MasterCheff.Data;

namespace MasterCheff.Multiplayer
{
    /// <summary>
    /// Network Manager - Handles Photon PUN2 connection, rooms, and player sync
    /// </summary>
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        private static NetworkManager _instance;
        public static NetworkManager Instance => _instance;
        public static bool HasInstance => _instance != null;

        [Header("Room Settings")]
        [SerializeField] private byte _maxPlayersPerRoom = 4;
        [SerializeField] private string _gameVersion = "1.0";
        [SerializeField] private float _connectionTimeout = 30f;

        // Connection State
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private RoomJoinMode _currentJoinMode;
        private string _pendingRoomCode;

        // Player Data
        private Dictionary<int, PlayerNetworkData> _playersInRoom = new Dictionary<int, PlayerNetworkData>();

        // Events
        public event Action OnConnectedToServer;
        public event Action<string> OnConnectionFailed;
        public event Action OnRoomCreated;
        public event Action OnRoomJoined;
        public event Action<string> OnRoomJoinFailed;
        public event Action<PlayerNetworkData> OnPlayerJoined;
        public event Action<PlayerNetworkData> OnPlayerLeft;
        public event Action OnAllPlayersReady;
        public event Action<PlayerSubmission[]> OnAllSubmissionsReceived;

        // Properties
        public ConnectionState CurrentConnectionState => _connectionState;
        public bool IsConnected => PhotonNetwork.IsConnected;
        public bool IsInRoom => PhotonNetwork.InRoom;
        public bool IsMasterClient => PhotonNetwork.IsMasterClient;
        public int LocalPlayerId => PhotonNetwork.LocalPlayer.ActorNumber;
        public string LocalPlayerName => PhotonNetwork.LocalPlayer.NickName;
        public string CurrentRoomCode => PhotonNetwork.CurrentRoom?.Name ?? string.Empty;
        public int PlayerCount => PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
        public int MaxPlayers => _maxPlayersPerRoom;
        public Dictionary<int, PlayerNetworkData> PlayersInRoom => _playersInRoom;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                PhotonNetwork.AutomaticallySyncScene = true;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #region Connection Methods

        /// <summary>
        /// Connect to Photon servers
        /// </summary>
        public void Connect(string playerName = "Player")
        {
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[NetworkManager] Already connected to Photon");
                OnConnectedToServer?.Invoke();
                return;
            }

            _connectionState = ConnectionState.Connecting;
            PhotonNetwork.NickName = playerName;
            PhotonNetwork.GameVersion = _gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            
            Debug.Log($"[NetworkManager] Connecting to Photon as '{playerName}'...");
        }

        /// <summary>
        /// Disconnect from Photon servers
        /// </summary>
        public void Disconnect()
        {
            if (!PhotonNetwork.IsConnected) return;

            _connectionState = ConnectionState.Disconnecting;
            PhotonNetwork.Disconnect();
            Debug.Log("[NetworkManager] Disconnecting from Photon...");
        }

        /// <summary>
        /// Set the local player's display name
        /// </summary>
        public void SetPlayerName(string playerName)
        {
            PhotonNetwork.NickName = playerName;
        }

        #endregion

        #region Room Methods

        /// <summary>
        /// Quick match - join random room or create new one
        /// </summary>
        public void QuickMatch()
        {
            if (!PhotonNetwork.IsConnected)
            {
                OnRoomJoinFailed?.Invoke("Not connected to server");
                return;
            }

            _currentJoinMode = RoomJoinMode.QuickMatch;
            _connectionState = ConnectionState.JoiningRoom;
            PhotonNetwork.JoinRandomRoom();
            Debug.Log("[NetworkManager] Attempting quick match...");
        }

        /// <summary>
        /// Create a room with a specific code
        /// </summary>
        public void CreateRoom(string roomCode)
        {
            if (!PhotonNetwork.IsConnected)
            {
                OnRoomJoinFailed?.Invoke("Not connected to server");
                return;
            }

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                roomCode = GenerateRoomCode();
            }

            _currentJoinMode = RoomJoinMode.CreateRoom;
            _connectionState = ConnectionState.JoiningRoom;

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = _maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true,
                PublishUserId = true
            };

            PhotonNetwork.CreateRoom(roomCode.ToUpper(), roomOptions);
            Debug.Log($"[NetworkManager] Creating room: {roomCode}");
        }

        /// <summary>
        /// Join a room by code
        /// </summary>
        public void JoinRoom(string roomCode)
        {
            if (!PhotonNetwork.IsConnected)
            {
                OnRoomJoinFailed?.Invoke("Not connected to server");
                return;
            }

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                OnRoomJoinFailed?.Invoke("Invalid room code");
                return;
            }

            _currentJoinMode = RoomJoinMode.JoinByCode;
            _pendingRoomCode = roomCode.ToUpper();
            _connectionState = ConnectionState.JoiningRoom;

            PhotonNetwork.JoinRoom(_pendingRoomCode);
            Debug.Log($"[NetworkManager] Joining room: {_pendingRoomCode}");
        }

        /// <summary>
        /// Leave the current room
        /// </summary>
        public void LeaveRoom()
        {
            if (!PhotonNetwork.InRoom) return;

            _playersInRoom.Clear();
            PhotonNetwork.LeaveRoom();
            Debug.Log("[NetworkManager] Leaving room...");
        }

        /// <summary>
        /// Close the room (prevent new players from joining)
        /// </summary>
        public void CloseRoom()
        {
            if (!IsMasterClient) return;
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }

        /// <summary>
        /// Open the room (allow new players to join)
        /// </summary>
        public void OpenRoom()
        {
            if (!IsMasterClient) return;
            PhotonNetwork.CurrentRoom.IsOpen = true;
        }

        /// <summary>
        /// Generate a random 6-character room code
        /// </summary>
        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] code = new char[6];
            System.Random random = new System.Random();
            
            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[random.Next(chars.Length)];
            }
            
            return new string(code);
        }

        #endregion

        #region RPC Methods

        /// <summary>
        /// Broadcast round ingredients to all players (Master Client only)
        /// </summary>
        public void BroadcastIngredients(RoundIngredients ingredients)
        {
            if (!IsMasterClient) return;
            photonView.RPC(nameof(RPC_ReceiveIngredients), RpcTarget.All, 
                ingredients.Ingredient1, ingredients.Ingredient2);
        }

        [PunRPC]
        private void RPC_ReceiveIngredients(string ingredient1, string ingredient2)
        {
            RoundIngredients ingredients = new RoundIngredients(ingredient1, ingredient2);
            Managers.EventManager.Instance.Trigger("IngredientsReceived", ingredients);
            Debug.Log($"[NetworkManager] Received ingredients: {ingredients}");
        }

        /// <summary>
        /// Submit player's dish to the room
        /// </summary>
        public void SubmitDish(PlayerSubmission submission)
        {
            string json = JsonUtility.ToJson(submission);
            photonView.RPC(nameof(RPC_ReceiveSubmission), RpcTarget.MasterClient, json);
        }

        private List<PlayerSubmission> _collectedSubmissions = new List<PlayerSubmission>();

        [PunRPC]
        private void RPC_ReceiveSubmission(string submissionJson, PhotonMessageInfo info)
        {
            if (!IsMasterClient) return;

            PlayerSubmission submission = JsonUtility.FromJson<PlayerSubmission>(submissionJson);
            _collectedSubmissions.Add(submission);
            Debug.Log($"[NetworkManager] Received submission from player {submission.PlayerId}: {submission.DishName}");

            // Check if all submissions received
            if (_collectedSubmissions.Count >= PlayerCount)
            {
                OnAllSubmissionsReceived?.Invoke(_collectedSubmissions.ToArray());
                _collectedSubmissions.Clear();
            }
        }

        /// <summary>
        /// Clear collected submissions for new round
        /// </summary>
        public void ClearSubmissions()
        {
            _collectedSubmissions.Clear();
        }

        /// <summary>
        /// Broadcast round results to all players (Master Client only)
        /// </summary>
        public void BroadcastRoundResult(RoundResult result)
        {
            if (!IsMasterClient) return;
            string json = JsonUtility.ToJson(result);
            photonView.RPC(nameof(RPC_ReceiveRoundResult), RpcTarget.All, json);
        }

        [PunRPC]
        private void RPC_ReceiveRoundResult(string resultJson)
        {
            RoundResult result = JsonUtility.FromJson<RoundResult>(resultJson);
            Managers.EventManager.Instance.Trigger("RoundResultReceived", result);
            Debug.Log($"[NetworkManager] Received round result. Winner: Player {result.WinnerPlayerId}");
        }

        /// <summary>
        /// Broadcast phase change to all players (Master Client only)
        /// </summary>
        public void BroadcastPhaseChange(GameState newPhase)
        {
            if (!IsMasterClient) return;
            photonView.RPC(nameof(RPC_ReceivePhaseChange), RpcTarget.All, (int)newPhase);
        }

        [PunRPC]
        private void RPC_ReceivePhaseChange(int phaseInt)
        {
            GameState phase = (GameState)phaseInt;
            Managers.EventManager.Instance.Trigger("PhaseChanged", phase);
            Debug.Log($"[NetworkManager] Phase changed to: {phase}");
        }

        /// <summary>
        /// Broadcast timer sync to all players
        /// </summary>
        public void BroadcastTimerSync(float remainingTime)
        {
            if (!IsMasterClient) return;
            photonView.RPC(nameof(RPC_ReceiveTimerSync), RpcTarget.Others, remainingTime);
        }

        [PunRPC]
        private void RPC_ReceiveTimerSync(float remainingTime)
        {
            Managers.EventManager.Instance.Trigger("TimerSync", remainingTime);
        }

        /// <summary>
        /// Set player ready status
        /// </summary>
        public void SetPlayerReady(bool ready)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "IsReady", ready }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        /// <summary>
        /// Check if all players are ready
        /// </summary>
        public bool AreAllPlayersReady()
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue("IsReady", out object isReady))
                {
                    if (!(bool)isReady) return false;
                }
                else
                {
                    return false;
                }
            }
            return PhotonNetwork.PlayerList.Length >= 2; // Minimum 2 players
        }

        #endregion

        #region Photon Callbacks

        public override void OnConnectedToMaster()
        {
            _connectionState = ConnectionState.Connected;
            Debug.Log("[NetworkManager] Connected to Photon Master Server");
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("[NetworkManager] Joined Photon Lobby");
            OnConnectedToServer?.Invoke();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            _connectionState = ConnectionState.Disconnected;
            _playersInRoom.Clear();
            Debug.Log($"[NetworkManager] Disconnected: {cause}");
            
            if (cause != DisconnectCause.DisconnectByClientLogic)
            {
                OnConnectionFailed?.Invoke(cause.ToString());
            }
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"[NetworkManager] Room created: {PhotonNetwork.CurrentRoom.Name}");
            OnRoomCreated?.Invoke();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            _connectionState = ConnectionState.Connected;
            Debug.LogWarning($"[NetworkManager] Create room failed: {message}");
            OnRoomJoinFailed?.Invoke(message);
        }

        public override void OnJoinedRoom()
        {
            _connectionState = ConnectionState.InRoom;
            Debug.Log($"[NetworkManager] Joined room: {PhotonNetwork.CurrentRoom.Name}");

            // Add all current players to our dictionary
            _playersInRoom.Clear();
            foreach (var player in PhotonNetwork.PlayerList)
            {
                var playerData = new PlayerNetworkData(player.ActorNumber, player.NickName, player.IsMasterClient);
                _playersInRoom[player.ActorNumber] = playerData;
            }

            OnRoomJoined?.Invoke();
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            _connectionState = ConnectionState.Connected;
            Debug.LogWarning($"[NetworkManager] Join room failed: {message}");
            OnRoomJoinFailed?.Invoke(message);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("[NetworkManager] No random room available, creating new room...");
            CreateRoom(null);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            var playerData = new PlayerNetworkData(newPlayer.ActorNumber, newPlayer.NickName, newPlayer.IsMasterClient);
            _playersInRoom[newPlayer.ActorNumber] = playerData;
            
            Debug.Log($"[NetworkManager] Player joined: {newPlayer.NickName} (ID: {newPlayer.ActorNumber})");
            OnPlayerJoined?.Invoke(playerData);
            
            CheckAllPlayersReady();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (_playersInRoom.TryGetValue(otherPlayer.ActorNumber, out var playerData))
            {
                _playersInRoom.Remove(otherPlayer.ActorNumber);
                Debug.Log($"[NetworkManager] Player left: {otherPlayer.NickName}");
                OnPlayerLeft?.Invoke(playerData);
            }
        }

        public override void OnLeftRoom()
        {
            _connectionState = ConnectionState.Connected;
            _playersInRoom.Clear();
            Debug.Log("[NetworkManager] Left room");
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("IsReady"))
            {
                CheckAllPlayersReady();
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"[NetworkManager] Master client switched to: {newMasterClient.NickName}");
            
            // Update player data
            foreach (var kvp in _playersInRoom)
            {
                kvp.Value.IsMasterClient = kvp.Key == newMasterClient.ActorNumber;
            }
        }

        private void CheckAllPlayersReady()
        {
            if (AreAllPlayersReady() && PlayerCount >= 2)
            {
                OnAllPlayersReady?.Invoke();
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get player data by ID
        /// </summary>
        public PlayerNetworkData GetPlayerData(int playerId)
        {
            return _playersInRoom.TryGetValue(playerId, out var data) ? data : null;
        }

        /// <summary>
        /// Get all player IDs in the room
        /// </summary>
        public int[] GetAllPlayerIds()
        {
            int[] ids = new int[_playersInRoom.Count];
            _playersInRoom.Keys.CopyTo(ids, 0);
            return ids;
        }

        #endregion
    }

    /// <summary>
    /// Data class for network player information
    /// </summary>
    [Serializable]
    public class PlayerNetworkData
    {
        public int PlayerId;
        public string PlayerName;
        public bool IsMasterClient;
        public bool IsReady;
        public int Score;
        public int RoundsWon;

        public PlayerNetworkData() { }

        public PlayerNetworkData(int playerId, string playerName, bool isMasterClient)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            IsMasterClient = isMasterClient;
            IsReady = false;
            Score = 0;
            RoundsWon = 0;
        }
    }
}


