using System;
using System.Collections.Generic;
using UnityEngine;
using MasterCheff.Core;

namespace MasterCheff.Managers
{
    /// <summary>
    /// Event Manager - Central event system for decoupled communication
    /// </summary>
    public class EventManager : Singleton<EventManager>
    {
        private Dictionary<string, Action> _eventDictionary = new Dictionary<string, Action>();
        private Dictionary<string, Delegate> _parameterizedEvents = new Dictionary<string, Delegate>();

        protected override void OnSingletonAwake()
        {
            Debug.Log("[EventManager] Initialized");
        }

        #region Simple Events (No Parameters)

        /// <summary>
        /// Subscribe to an event
        /// </summary>
        public void Subscribe(string eventName, Action listener)
        {
            if (_eventDictionary.TryGetValue(eventName, out Action existingEvent))
            {
                _eventDictionary[eventName] = existingEvent + listener;
            }
            else
            {
                _eventDictionary[eventName] = listener;
            }
        }

        /// <summary>
        /// Unsubscribe from an event
        /// </summary>
        public void Unsubscribe(string eventName, Action listener)
        {
            if (_eventDictionary.TryGetValue(eventName, out Action existingEvent))
            {
                _eventDictionary[eventName] = existingEvent - listener;
            }
        }

        /// <summary>
        /// Trigger an event
        /// </summary>
        public void Trigger(string eventName)
        {
            if (_eventDictionary.TryGetValue(eventName, out Action thisEvent))
            {
                thisEvent?.Invoke();
            }
        }

        #endregion

        #region Parameterized Events (One Parameter)

        /// <summary>
        /// Subscribe to a parameterized event
        /// </summary>
        public void Subscribe<T>(string eventName, Action<T> listener)
        {
            if (_parameterizedEvents.TryGetValue(eventName, out Delegate existingEvent))
            {
                _parameterizedEvents[eventName] = Delegate.Combine(existingEvent, listener);
            }
            else
            {
                _parameterizedEvents[eventName] = listener;
            }
        }

        /// <summary>
        /// Unsubscribe from a parameterized event
        /// </summary>
        public void Unsubscribe<T>(string eventName, Action<T> listener)
        {
            if (_parameterizedEvents.TryGetValue(eventName, out Delegate existingEvent))
            {
                _parameterizedEvents[eventName] = Delegate.Remove(existingEvent, listener);
            }
        }

        /// <summary>
        /// Trigger a parameterized event
        /// </summary>
        public void Trigger<T>(string eventName, T parameter)
        {
            if (_parameterizedEvents.TryGetValue(eventName, out Delegate thisEvent))
            {
                (thisEvent as Action<T>)?.Invoke(parameter);
            }
        }

        #endregion

        #region Two Parameter Events

        /// <summary>
        /// Subscribe to a two-parameter event
        /// </summary>
        public void Subscribe<T1, T2>(string eventName, Action<T1, T2> listener)
        {
            string key = $"{eventName}_2";
            if (_parameterizedEvents.TryGetValue(key, out Delegate existingEvent))
            {
                _parameterizedEvents[key] = Delegate.Combine(existingEvent, listener);
            }
            else
            {
                _parameterizedEvents[key] = listener;
            }
        }

        /// <summary>
        /// Unsubscribe from a two-parameter event
        /// </summary>
        public void Unsubscribe<T1, T2>(string eventName, Action<T1, T2> listener)
        {
            string key = $"{eventName}_2";
            if (_parameterizedEvents.TryGetValue(key, out Delegate existingEvent))
            {
                _parameterizedEvents[key] = Delegate.Remove(existingEvent, listener);
            }
        }

        /// <summary>
        /// Trigger a two-parameter event
        /// </summary>
        public void Trigger<T1, T2>(string eventName, T1 param1, T2 param2)
        {
            string key = $"{eventName}_2";
            if (_parameterizedEvents.TryGetValue(key, out Delegate thisEvent))
            {
                (thisEvent as Action<T1, T2>)?.Invoke(param1, param2);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Clear all listeners for an event
        /// </summary>
        public void ClearEvent(string eventName)
        {
            _eventDictionary.Remove(eventName);
            _parameterizedEvents.Remove(eventName);
            _parameterizedEvents.Remove($"{eventName}_2");
        }

        /// <summary>
        /// Clear all events
        /// </summary>
        public void ClearAllEvents()
        {
            _eventDictionary.Clear();
            _parameterizedEvents.Clear();
        }

        /// <summary>
        /// Check if an event has listeners
        /// </summary>
        public bool HasListeners(string eventName)
        {
            return _eventDictionary.ContainsKey(eventName) ||
                   _parameterizedEvents.ContainsKey(eventName);
        }

        #endregion
    }

    /// <summary>
    /// Static event names for common game events
    /// </summary>
    public static class GameEvents
    {
        // Game Flow
        public const string GAME_START = "GameStart";
        public const string GAME_PAUSE = "GamePause";
        public const string GAME_RESUME = "GameResume";
        public const string GAME_OVER = "GameOver";
        public const string GAME_WIN = "GameWin";
        public const string LEVEL_START = "LevelStart";
        public const string LEVEL_COMPLETE = "LevelComplete";

        // Player
        public const string PLAYER_SPAWN = "PlayerSpawn";
        public const string PLAYER_DEATH = "PlayerDeath";
        public const string PLAYER_DAMAGE = "PlayerDamage";
        public const string PLAYER_HEAL = "PlayerHeal";
        public const string PLAYER_SCORE = "PlayerScore";

        // Items
        public const string ITEM_COLLECTED = "ItemCollected";
        public const string POWERUP_ACTIVATED = "PowerupActivated";
        public const string POWERUP_EXPIRED = "PowerupExpired";

        // UI
        public const string UI_BUTTON_CLICK = "UIButtonClick";
        public const string UI_POPUP_OPEN = "UIPopupOpen";
        public const string UI_POPUP_CLOSE = "UIPopupClose";

        // Audio
        public const string PLAY_SFX = "PlaySFX";
        public const string PLAY_MUSIC = "PlayMusic";
        public const string STOP_MUSIC = "StopMusic";

        // Save/Load
        public const string SAVE_GAME = "SaveGame";
        public const string LOAD_GAME = "LoadGame";

        // Multiplayer / AI Chef Battle
        public const string CONNECTED_TO_SERVER = "ConnectedToServer";
        public const string DISCONNECTED_FROM_SERVER = "DisconnectedFromServer";
        public const string ROOM_JOINED = "RoomJoined";
        public const string ROOM_LEFT = "RoomLeft";
        public const string PLAYER_JOINED_ROOM = "PlayerJoinedRoom";
        public const string PLAYER_LEFT_ROOM = "PlayerLeftRoom";
        public const string ALL_PLAYERS_READY = "AllPlayersReady";

        // Match Events
        public const string MATCH_STARTED = "MatchStarted";
        public const string MATCH_ENDED = "MatchEnded";
        public const string ROUND_STARTED = "RoundStarted";
        public const string ROUND_ENDED = "RoundEnded";

        // Phase Events
        public const string PHASE_CHANGED = "PhaseChanged";
        public const string INGREDIENTS_RECEIVED = "IngredientsReceived";
        public const string TIMER_SYNC = "TimerSync";
        public const string DISH_SUBMITTED = "DishSubmitted";
        public const string ROUND_RESULT_RECEIVED = "RoundResultReceived";

        // Power-Up Events
        public const string REROLL_PANTRY = "RerollPantry";
        public const string TIME_EXTENSION_ACTIVATED = "TimeExtensionActivated";
        public const string SECRET_INGREDIENT_ENABLED = "SecretIngredientEnabled";

        // AI Events
        public const string JUDGING_STARTED = "JudgingStarted";
        public const string JUDGING_COMPLETED = "JudgingCompleted";
        public const string IMAGE_GENERATION_STARTED = "ImageGenerationStarted";
        public const string IMAGE_GENERATION_COMPLETED = "ImageGenerationCompleted";
    }
}
