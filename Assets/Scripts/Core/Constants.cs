namespace MasterCheff.Core
{
    /// <summary>
    /// Game constants and configuration values
    /// </summary>
    public static class Constants
    {
        // Scenes
        public static class Scenes
        {
            public const string LOADING = "Loading";
            public const string MAIN_MENU = "MainMenu";
            public const string GAMEPLAY = "Gameplay";
            public const string SETTINGS = "Settings";
        }

        // Player Prefs Keys
        public static class PlayerPrefsKeys
        {
            public const string HIGH_SCORE = "HighScore";
            public const string MUSIC_VOLUME = "MusicVolume";
            public const string SFX_VOLUME = "SFXVolume";
            public const string MUSIC_MUTED = "MusicMuted";
            public const string SFX_MUTED = "SFXMuted";
            public const string HAPTIC_ENABLED = "HapticEnabled";
            public const string LANGUAGE = "Language";
            public const string FIRST_LAUNCH = "FirstLaunch";
            public const string TUTORIAL_COMPLETE = "TutorialComplete";
        }

        // Tags
        public static class Tags
        {
            public const string PLAYER = "Player";
            public const string ENEMY = "Enemy";
            public const string COLLECTIBLE = "Collectible";
            public const string OBSTACLE = "Obstacle";
            public const string GROUND = "Ground";
            public const string TRIGGER = "Trigger";
        }

        // Layers
        public static class Layers
        {
            public const string DEFAULT = "Default";
            public const string PLAYER = "Player";
            public const string ENEMY = "Enemy";
            public const string UI = "UI";
            public const string GROUND = "Ground";
            public const string IGNORE_RAYCAST = "Ignore Raycast";
        }

        // Sorting Layers
        public static class SortingLayers
        {
            public const string BACKGROUND = "Background";
            public const string DEFAULT = "Default";
            public const string FOREGROUND = "Foreground";
            public const string UI = "UI";
        }

        // Animator Parameters
        public static class AnimParams
        {
            public const string SPEED = "Speed";
            public const string IS_GROUNDED = "IsGrounded";
            public const string IS_JUMPING = "IsJumping";
            public const string IS_ATTACKING = "IsAttacking";
            public const string IS_DEAD = "IsDead";
            public const string TRIGGER_ATTACK = "Attack";
            public const string TRIGGER_JUMP = "Jump";
            public const string TRIGGER_HURT = "Hurt";
            public const string TRIGGER_DIE = "Die";
        }

        // Pool Tags
        public static class PoolTags
        {
            public const string BULLET = "Bullet";
            public const string ENEMY = "Enemy";
            public const string PARTICLE = "Particle";
            public const string COIN = "Coin";
            public const string DAMAGE_TEXT = "DamageText";
        }

        // Game Balance
        public static class Balance
        {
            public const int MAX_LIVES = 3;
            public const float INVINCIBILITY_TIME = 2f;
            public const float POWER_UP_DURATION = 10f;
            public const int POINTS_PER_COIN = 10;
            public const int POINTS_PER_ENEMY = 100;
        }

        // Multiplayer Settings
        public static class Multiplayer
        {
            public const int MAX_PLAYERS_PER_ROOM = 4;
            public const int MIN_PLAYERS_TO_START = 2;
            public const int TOTAL_ROUNDS = 10;
            public const int ROOM_CODE_LENGTH = 6;
            public const string GAME_VERSION = "1.0";
        }

        // Phase Durations (in seconds)
        public static class PhaseDurations
        {
            public const float INGREDIENT_REVEAL = 3f;
            public const float COOKING_PHASE = 60f;
            public const float JUDGING_PHASE = 15f;
            public const float RESULTS_PHASE = 10f;
            public const float TIME_EXTENSION_BONUS = 15f;
        }

        // API Endpoints (to be configured in RelayAPIClient)
        public static class API
        {
            public const string JUDGE_ENDPOINT = "/judge";
            public const string IMAGE_ENDPOINT = "/generate-image";
            public const string HEALTH_ENDPOINT = "/health";
            public const int JUDGE_TIMEOUT_SECONDS = 30;
            public const int IMAGE_TIMEOUT_SECONDS = 60;
        }

        // Input Validation
        public static class InputLimits
        {
            public const int DISH_NAME_MAX_LENGTH = 50;
            public const int DESCRIPTION_MAX_LENGTH = 200;
            public const int PLAYER_NAME_MAX_LENGTH = 15;
            public const int SECRET_INGREDIENT_MAX_LENGTH = 30;
        }

        // Scoring
        public static class Scoring
        {
            public const int MIN_JUDGE_SCORE = 1;
            public const int MAX_JUDGE_SCORE = 10;
            public const int JUDGES_COUNT = 3;
            public const int MAX_ROUND_SCORE = MAX_JUDGE_SCORE * JUDGES_COUNT;
        }

        // Mobile Settings
        public static class Mobile
        {
            public const int TARGET_FRAME_RATE = 60;
            public const float SWIPE_THRESHOLD = 50f;
            public const float TAP_THRESHOLD = 0.2f;
            public const float HOLD_THRESHOLD = 0.5f;
        }

        // URLs
        public static class URLs
        {
            public const string PRIVACY_POLICY = "https://example.com/privacy";
            public const string TERMS_OF_SERVICE = "https://example.com/terms";
            public const string SUPPORT_EMAIL = "support@example.com";
        }
    }
}

