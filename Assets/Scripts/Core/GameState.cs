namespace MasterCheff.Core
{
    public enum GameState
    {
        Loading,
        MainMenu,
        Lobby,
        WaitingForPlayers,
        IngredientReveal,
        Cooking,
        Judging,
        RoundResults,
        MatchEnd,
        Playing,
        Paused,
        GameOver,
        Victory,
        Cutscene
    }

    public enum DifficultyLevel { Easy, Normal, Hard, Expert }
    public enum ConnectionState { Disconnected, Connecting, Connected, JoiningRoom, InRoom, Disconnecting }
    public enum RoomJoinMode { QuickMatch, CreateRoom, JoinByCode }
}
