using System;
using UnityEngine;

namespace MasterCheff.Data
{
    public enum DishStyleTag
    {
        HomeyComfort,
        GourmetFineDining,
        DecadentDessert,
        HealthyFresh,
        CrazyFusion
    }

    public enum PowerUpType
    {
        RerollPantry,
        TimeExtension,
        SecretIngredient
    }

    [Serializable]
    public class PlayerSubmission
    {
        public int PlayerId;
        public string PlayerName;
        public string DishName;
        public string Description;
        public DishStyleTag StyleTag;
        public string SecretIngredient;

        public PlayerSubmission() { }

        public PlayerSubmission(int playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            DishName = string.Empty;
            Description = string.Empty;
            StyleTag = DishStyleTag.HomeyComfort;
            SecretIngredient = string.Empty;
        }

        public bool IsValid() => !string.IsNullOrWhiteSpace(DishName);
        public string ToJson() => JsonUtility.ToJson(this);
    }

    [Serializable]
    public class JudgeVerdict
    {
        public string JudgeName;
        public int Score;
        public string Comment;

        public JudgeVerdict() { }
        public JudgeVerdict(string judgeName, int score, string comment)
        {
            JudgeName = judgeName;
            Score = Mathf.Clamp(score, 1, 10);
            Comment = comment;
        }
    }

    [Serializable]
    public class PlayerJudgeResult
    {
        public int PlayerId;
        public string PlayerName;
        public string DishName;
        public JudgeVerdict Critic;
        public JudgeVerdict Visionary;
        public JudgeVerdict SoulCook;
        public int TotalScore;

        public PlayerJudgeResult()
        {
            Critic = new JudgeVerdict();
            Visionary = new JudgeVerdict();
            SoulCook = new JudgeVerdict();
        }

        public void CalculateTotalScore()
        {
            TotalScore = (Critic?.Score ?? 0) + (Visionary?.Score ?? 0) + (SoulCook?.Score ?? 0);
        }
    }

    [Serializable]
    public class RoundResult
    {
        public int RoundNumber;
        public PlayerJudgeResult[] PlayerResults;
        public int WinnerPlayerId;
        public string WinnerPlayerName;
        public string WinningDishName;
        public string WinningDishImageUrl;
        public string WinningDishImagePrompt;

        public RoundResult() { PlayerResults = Array.Empty<PlayerJudgeResult>(); }

        public PlayerJudgeResult GetPlayerResult(int playerId)
        {
            if (PlayerResults == null) return null;
            foreach (var result in PlayerResults)
                if (result.PlayerId == playerId) return result;
            return null;
        }

        public PlayerJudgeResult GetWinnerResult() => GetPlayerResult(WinnerPlayerId);
    }

    [Serializable]
    public class RoundIngredients
    {
        public string Ingredient1;
        public string Ingredient2;
        public string Ingredient1Icon;
        public string Ingredient2Icon;

        public RoundIngredients() { }
        public RoundIngredients(string ingredient1, string ingredient2)
        {
            Ingredient1 = ingredient1;
            Ingredient2 = ingredient2;
        }
        public override string ToString() => $"{Ingredient1} + {Ingredient2}";
    }

    [Serializable]
    public class MatchData
    {
        public string MatchId;
        public DateTime StartTime;
        public DateTime EndTime;
        public int TotalRounds;
        public int CurrentRound;
        public PlayerMatchScore[] PlayerScores;
        public RoundResult[] RoundResults;
        public int OverallWinnerId;

        public MatchData()
        {
            MatchId = Guid.NewGuid().ToString();
            StartTime = DateTime.UtcNow;
            TotalRounds = 10;
            CurrentRound = 0;
            PlayerScores = new PlayerMatchScore[4];
            RoundResults = new RoundResult[10];
        }
    }

    [Serializable]
    public class PlayerMatchScore
    {
        public int PlayerId;
        public string PlayerName;
        public int TotalScore;
        public int RoundsWon;
        public int[] RoundScores;

        public PlayerMatchScore() { RoundScores = new int[10]; }
        public PlayerMatchScore(int playerId, string playerName) : this()
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }
    }

    [Serializable]
    public class JudgeRequest
    {
        public string Ingredient1;
        public string Ingredient2;
        public PlayerSubmission[] Submissions;

        public JudgeRequest() { }
        public JudgeRequest(RoundIngredients ingredients, PlayerSubmission[] submissions)
        {
            Ingredient1 = ingredients.Ingredient1;
            Ingredient2 = ingredients.Ingredient2;
            Submissions = submissions;
        }
    }

    [Serializable]
    public class JudgeApiResponse
    {
        public PlayerJudgeResultApi[] results;
        public int winnerId;
        public string winningDishPrompt;
    }

    [Serializable]
    public class PlayerJudgeResultApi
    {
        public int playerId;
        public JudgeVerdictApi critic;
        public JudgeVerdictApi visionary;
        public JudgeVerdictApi soulCook;
        public int totalScore;
    }

    [Serializable]
    public class JudgeVerdictApi
    {
        public int score;
        public string comment;
    }

    [Serializable]
    public class ImageGenerationRequest
    {
        public string Prompt;
        public string Size;

        public ImageGenerationRequest() { }
        public ImageGenerationRequest(string prompt, string size = "1024x1024")
        {
            Prompt = prompt;
            Size = size;
        }
    }

    [Serializable]
    public class ImageGenerationResponse
    {
        public string ImageUrl;
        public bool Success;
        public string Error;
    }
}
