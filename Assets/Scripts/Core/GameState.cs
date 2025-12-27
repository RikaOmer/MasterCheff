namespace MasterCheff.Core
{
    /// <summary>
    /// Enum representing the different states of the game
    /// </summary>
    public enum GameState
    {
        Loading,
        MainMenu,
        
        // Multiplayer Lobby States
        Lobby,
        WaitingForPlayers,
        
        // Match States
        IngredientReveal,
        Cooking,
        Judging,
        RoundResults,
        MatchEnd,
        
        // General States
        Playing,
        Paused,
        GameOver,
        Victory,
        Cutscene
    }

    /// <summary>
    /// Enum for different difficulty levels
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard,
        Expert
    }

    /// <summary>
    /// Enum for multiplayer connection states
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        JoiningRoom,
        InRoom,
        Disconnecting
    }

    /// <summary>
    /// Enum for room join modes
    /// </summary>
    public enum RoomJoinMode
    {
        QuickMatch,
        CreateRoom,
        JoinByCode
    }
}
