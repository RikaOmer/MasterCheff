using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using MasterCheff.Core;
using MasterCheff.Data;
using MasterCheff.Managers;
using MasterCheff.Multiplayer;

namespace MasterCheff.Gameplay
{
    /// <summary>
    /// Manages power-ups during gameplay
    /// </summary>
    public class PowerUpManager : MonoBehaviourPunCallbacks
    {
        private static PowerUpManager _instance;
        public static PowerUpManager Instance => _instance;
        public static bool HasInstance => _instance != null;

        [Header("Power-Up Settings")]
        [SerializeField] private int _maxPowerUpsPerMatch = 3;
        [SerializeField] private float _timeExtensionAmount = 15f;
        [SerializeField] private int _powerUpsPerType = 1;

        [Header("Cooldowns")]
        [SerializeField] private float _rerollCooldown = 0f;
        [SerializeField] private float _timeExtensionCooldown = 0f;
        [SerializeField] private float _secretIngredientCooldown = 0f;

        // Player power-up inventory
        private Dictionary<PowerUpType, int> _availablePowerUps = new Dictionary<PowerUpType, int>();
        private int _totalUsedThisMatch = 0;

        // Active states
        private bool _secretIngredientActive = false;
        private float _lastRerollTime = float.MinValue;
        private float _lastTimeExtensionTime = float.MinValue;
        private float _lastSecretIngredientTime = float.MinValue;

        // Events
        public event Action<PowerUpType> OnPowerUpActivated;
        public event Action<PowerUpType> OnPowerUpUsed;
        public event Action<PowerUpType, int> OnPowerUpCountChanged;
        public event Action OnSecretIngredientEnabled;
        public event Action OnSecretIngredientDisabled;

        // Properties
        public bool SecretIngredientActive => _secretIngredientActive;
        public int TotalUsedThisMatch => _totalUsedThisMatch;
        public int MaxPowerUpsPerMatch => _maxPowerUpsPerMatch;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
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

        #region Initialization

        /// <summary>
        /// Initialize power-ups for a new match
        /// </summary>
        public void InitializeForMatch()
        {
            _availablePowerUps.Clear();
            _totalUsedThisMatch = 0;
            _secretIngredientActive = false;

            // Give player starting power-ups
            _availablePowerUps[PowerUpType.RerollPantry] = _powerUpsPerType;
            _availablePowerUps[PowerUpType.TimeExtension] = _powerUpsPerType;
            _availablePowerUps[PowerUpType.SecretIngredient] = _powerUpsPerType;

            Debug.Log("[PowerUpManager] Initialized for new match");

            // Notify UI
            foreach (var kvp in _availablePowerUps)
            {
                OnPowerUpCountChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Reset power-ups for a new round
        /// </summary>
        public void ResetForNewRound()
        {
            _secretIngredientActive = false;
            OnSecretIngredientDisabled?.Invoke();
        }

        #endregion

        #region Power-Up Activation

        /// <summary>
        /// Check if a power-up can be used
        /// </summary>
        public bool CanUsePowerUp(PowerUpType type)
        {
            // Check if in cooking phase
            if (!RoundLoopController.HasInstance || 
                RoundLoopController.Instance.CurrentPhase != GameState.Cooking)
            {
                return false;
            }

            // Check if player has power-ups remaining
            if (!_availablePowerUps.TryGetValue(type, out int count) || count <= 0)
            {
                return false;
            }

            // Check match limit
            if (_totalUsedThisMatch >= _maxPowerUpsPerMatch)
            {
                return false;
            }

            // Check cooldowns
            float now = Time.time;
            switch (type)
            {
                case PowerUpType.RerollPantry:
                    if (now - _lastRerollTime < _rerollCooldown) return false;
                    break;
                case PowerUpType.TimeExtension:
                    if (now - _lastTimeExtensionTime < _timeExtensionCooldown) return false;
                    break;
                case PowerUpType.SecretIngredient:
                    if (now - _lastSecretIngredientTime < _secretIngredientCooldown) return false;
                    if (_secretIngredientActive) return false; // Already active
                    break;
            }

            return true;
        }

        /// <summary>
        /// Activate a power-up
        /// </summary>
        public bool ActivatePowerUp(PowerUpType type)
        {
            if (!CanUsePowerUp(type))
            {
                Debug.LogWarning($"[PowerUpManager] Cannot use power-up: {type}");
                return false;
            }

            // Consume the power-up
            _availablePowerUps[type]--;
            _totalUsedThisMatch++;

            Debug.Log($"[PowerUpManager] Activating: {type}");

            switch (type)
            {
                case PowerUpType.RerollPantry:
                    ActivateRerollPantry();
                    _lastRerollTime = Time.time;
                    break;

                case PowerUpType.TimeExtension:
                    ActivateTimeExtension();
                    _lastTimeExtensionTime = Time.time;
                    break;

                case PowerUpType.SecretIngredient:
                    ActivateSecretIngredient();
                    _lastSecretIngredientTime = Time.time;
                    break;
            }

            OnPowerUpUsed?.Invoke(type);
            OnPowerUpCountChanged?.Invoke(type, _availablePowerUps[type]);
            OnPowerUpActivated?.Invoke(type);

            return true;
        }

        /// <summary>
        /// Reroll the ingredients (affects all players)
        /// </summary>
        private void ActivateRerollPantry()
        {
            // Request Master Client to reroll
            if (NetworkManager.HasInstance)
            {
                photonView.RPC(nameof(RPC_RequestReroll), RpcTarget.MasterClient);
            }
            else if (RoundLoopController.HasInstance)
            {
                // Single player / testing mode
                RoundLoopController.Instance.RerollIngredients();
            }

            Debug.Log("[PowerUpManager] Reroll Pantry activated");
        }

        [PunRPC]
        private void RPC_RequestReroll(PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Debug.Log($"[PowerUpManager] Reroll requested by player {info.Sender.ActorNumber}");

            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.RerollIngredients();
            }
        }

        /// <summary>
        /// Add bonus time to the cooking phase
        /// </summary>
        private void ActivateTimeExtension()
        {
            // Request time extension from Master Client
            if (NetworkManager.HasInstance)
            {
                photonView.RPC(nameof(RPC_RequestTimeExtension), RpcTarget.MasterClient, _timeExtensionAmount);
            }
            else if (RoundLoopController.HasInstance)
            {
                // Single player / testing mode
                RoundLoopController.Instance.AddBonusTime(_timeExtensionAmount);
            }

            Debug.Log($"[PowerUpManager] Time Extension activated (+{_timeExtensionAmount}s)");
        }

        [PunRPC]
        private void RPC_RequestTimeExtension(float seconds, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Debug.Log($"[PowerUpManager] Time extension requested by player {info.Sender.ActorNumber}");

            if (RoundLoopController.HasInstance)
            {
                RoundLoopController.Instance.AddBonusTime(seconds);
                
                // Notify all players
                photonView.RPC(nameof(RPC_NotifyTimeExtension), RpcTarget.All, seconds, info.Sender.ActorNumber);
            }
        }

        [PunRPC]
        private void RPC_NotifyTimeExtension(float seconds, int requestingPlayerId)
        {
            string playerName = "A player";
            if (NetworkManager.HasInstance)
            {
                var playerData = NetworkManager.Instance.GetPlayerData(requestingPlayerId);
                if (playerData != null)
                {
                    playerName = playerData.PlayerName;
                }
            }

            Debug.Log($"[PowerUpManager] {playerName} added {seconds}s bonus time!");
            EventManager.Instance?.Trigger("TimeExtensionActivated", seconds);
        }

        /// <summary>
        /// Enable the secret ingredient input field
        /// </summary>
        private void ActivateSecretIngredient()
        {
            _secretIngredientActive = true;
            OnSecretIngredientEnabled?.Invoke();

            Debug.Log("[PowerUpManager] Secret Ingredient activated");
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Get remaining count of a power-up type
        /// </summary>
        public int GetPowerUpCount(PowerUpType type)
        {
            return _availablePowerUps.TryGetValue(type, out int count) ? count : 0;
        }

        /// <summary>
        /// Get all available power-ups
        /// </summary>
        public Dictionary<PowerUpType, int> GetAllPowerUps()
        {
            return new Dictionary<PowerUpType, int>(_availablePowerUps);
        }

        /// <summary>
        /// Check if any power-ups are available
        /// </summary>
        public bool HasAnyPowerUps()
        {
            foreach (var kvp in _availablePowerUps)
            {
                if (kvp.Value > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Get display name for power-up type
        /// </summary>
        public static string GetPowerUpDisplayName(PowerUpType type)
        {
            return type switch
            {
                PowerUpType.RerollPantry => "Reroll Pantry",
                PowerUpType.TimeExtension => "Extra Time",
                PowerUpType.SecretIngredient => "Secret Ingredient",
                _ => type.ToString()
            };
        }

        /// <summary>
        /// Get description for power-up type
        /// </summary>
        public static string GetPowerUpDescription(PowerUpType type)
        {
            return type switch
            {
                PowerUpType.RerollPantry => "Change the ingredients for everyone!",
                PowerUpType.TimeExtension => "Add 15 seconds to the timer",
                PowerUpType.SecretIngredient => "Add a third secret ingredient",
                _ => string.Empty
            };
        }

        #endregion

        #region Award / Add Power-Ups

        /// <summary>
        /// Award a power-up to the player
        /// </summary>
        public void AwardPowerUp(PowerUpType type, int count = 1)
        {
            if (!_availablePowerUps.ContainsKey(type))
            {
                _availablePowerUps[type] = 0;
            }

            _availablePowerUps[type] += count;
            OnPowerUpCountChanged?.Invoke(type, _availablePowerUps[type]);

            Debug.Log($"[PowerUpManager] Awarded {count}x {type}");
        }

        /// <summary>
        /// Award random power-up (for winning rounds, etc.)
        /// </summary>
        public PowerUpType? AwardRandomPowerUp()
        {
            PowerUpType[] types = (PowerUpType[])Enum.GetValues(typeof(PowerUpType));
            PowerUpType randomType = types[UnityEngine.Random.Range(0, types.Length)];
            
            AwardPowerUp(randomType, 1);
            return randomType;
        }

        #endregion
    }
}
