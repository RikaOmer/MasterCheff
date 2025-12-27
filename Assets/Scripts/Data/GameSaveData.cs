using System;
using System.Collections.Generic;

namespace MasterCheff.Data
{
    /// <summary>
    /// Main game save data structure
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        // Meta
        public int saveVersion = 1;
        public string lastSaveTime;

        // Player Progress
        public int currentLevel = 1;
        public int highScore = 0;
        public int totalScore = 0;
        public int coins = 0;
        public int gems = 0;

        // Statistics
        public int gamesPlayed = 0;
        public int gamesWon = 0;
        public int totalPlayTime = 0; // in seconds

        // Settings
        public float musicVolume = 0.7f;
        public float sfxVolume = 1.0f;
        public bool isMusicMuted = false;
        public bool isSfxMuted = false;
        public bool hapticEnabled = true;
        public int languageIndex = 0;

        // Achievements
        public List<string> unlockedAchievements = new List<string>();

        // Level Progress
        public List<LevelData> levelProgress = new List<LevelData>();

        // Inventory
        public List<InventoryItem> inventory = new List<InventoryItem>();

        // Tutorial Flags
        public bool hasCompletedTutorial = false;
        public List<string> shownTutorials = new List<string>();
    }

    /// <summary>
    /// Level completion data
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int levelId;
        public bool isUnlocked;
        public bool isCompleted;
        public int stars; // 0-3
        public int bestScore;
        public float bestTime;
    }

    /// <summary>
    /// Inventory item data
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int quantity;
        public bool isEquipped;
    }

    /// <summary>
    /// Player stats for analytics
    /// </summary>
    [Serializable]
    public class PlayerStats
    {
        public int totalKills;
        public int totalDeaths;
        public int itemsCollected;
        public int powerupsUsed;
        public float distanceTraveled;
    }
}


