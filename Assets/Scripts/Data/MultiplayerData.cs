using System;
using UnityEngine;

namespace MasterCheff.Data
{
    /// <summary>
    /// Style tags for dish categorization
    /// </summary>
    public enum DishStyleTag
    {
        HomeyComfort,
        GourmetFineDining,
        DecadentDessert,
        HealthyFresh,
        CrazyFusion
    }

    /// <summary>
    /// Power-up types available during cooking phase
    /// </summary>
    public enum PowerUpType
    {
        RerollPantry,
        TimeExtension,
        SecretIngredient
    }

    /// <summary>
    /// Player submission for a single round
    /// </summary>
    [Serializable]
    public class PlayerSubmission
    {
        public int PlayerId;
        public string PlayerName;
        public string DishName;
        public string Description;
        public DishStyleTag StyleTag;
        public string SecretIngredient; // Only used with SecretIngredient power-up

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

        /// <summary>
        /// Validates that the submission has required fields
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(DishName);
        }

        /// <summary>
        /// Converts to JSON for API submission
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    /// <summary>
    /// Single judge's verdict for a player's dish
    /// </summary>
    [Serializable]
    public class JudgeVerdict
    {
        public string JudgeName;
        public int Score; // 1-10
        public string Comment;

        public JudgeVerdict() { }

        public JudgeVerdict(string judgeName, int score, string comment)
        {
            JudgeName = judgeName;
            Score = Mathf.Clamp(score, 1, 10);
            Comment = comment;
        }
    }

    /// <summary>
    /// Complete judge results for one player in a round
    /// </summary>
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

        /// <summary>
        /// Calculate total score from all judges
        /// </summary>
        public void CalculateTotalScore()
        {
            TotalScore = (Critic?.Score ?? 0) + (Visionary?.Score ?? 0) + (SoulCook?.Score ?? 0);
        }
    }

    /// <summary>
    /// Complete round result from AI judging
    /// </summary>
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

        public RoundResult()
        {
            PlayerResults = Array.Empty<PlayerJudgeResult>();
        }

        /// <summary>
        /// Get the result for a specific player
        /// </summary>
        public PlayerJudgeResult GetPlayerResult(int playerId)
        {
            if (PlayerResults == null) return null;
            
            foreach (var result in PlayerResults)
            {
                if (result.PlayerId == playerId)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Get the winning player's result
        /// </summary>
        public PlayerJudgeResult GetWinnerResult()
        {
            return GetPlayerResult(WinnerPlayerId);
        }
    }

    /// <summary>
    /// Ingredient data for a round
    /// </summary>
    [Serializable]
    public class RoundIngredients
    {
        public string Ingredient1;
        public string Ingredient2;
        public string Ingredient1Icon; // Optional icon/sprite name
        public string Ingredient2Icon;

        [NonSerialized]
        public Sprite Ingredient1Sprite; // Directly assigned sprite reference
        [NonSerialized]
        public Sprite Ingredient2Sprite; // Directly assigned sprite reference

        public RoundIngredients() { }

        public RoundIngredients(string ingredient1, string ingredient2)
        {
            Ingredient1 = ingredient1;
            Ingredient2 = ingredient2;
        }

        public override string ToString()
        {
            return $"{Ingredient1} + {Ingredient2}";
        }
    }

    /// <summary>
    /// Player score data across the entire match
    /// </summary>
    [Serializable]
    public class PlayerMatchScore
    {
        public int PlayerId;
        public string PlayerName;
        public int TotalScore;
        public int RoundsWon;
        public int[] RoundScores; // Score per round

        public PlayerMatchScore()
        {
            RoundScores = new int[10]; // 10 rounds
        }

        public PlayerMatchScore(int playerId, string playerName) : this()
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }

        public void AddRoundScore(int roundIndex, int score, bool won)
        {
            if (roundIndex >= 0 && roundIndex < RoundScores.Length)
            {
                RoundScores[roundIndex] = score;
                TotalScore += score;
                if (won) RoundsWon++;
            }
        }
    }

    /// <summary>
    /// Complete match data containing all rounds
    /// </summary>
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

        /// <summary>
        /// Initialize player scores for a new match
        /// </summary>
        public void InitializePlayers(PlayerMatchScore[] players)
        {
            PlayerScores = players;
        }

        /// <summary>
        /// Record a round result and update scores
        /// </summary>
        public void RecordRoundResult(RoundResult result)
        {
            if (result.RoundNumber >= 0 && result.RoundNumber < RoundResults.Length)
            {
                RoundResults[result.RoundNumber] = result;

                // Update player scores
                foreach (var playerResult in result.PlayerResults)
                {
                    foreach (var playerScore in PlayerScores)
                    {
                        if (playerScore != null && playerScore.PlayerId == playerResult.PlayerId)
                        {
                            bool won = playerResult.PlayerId == result.WinnerPlayerId;
                            playerScore.AddRoundScore(result.RoundNumber, playerResult.TotalScore, won);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine overall winner at match end
        /// </summary>
        public PlayerMatchScore DetermineOverallWinner()
        {
            PlayerMatchScore winner = null;
            int highestScore = int.MinValue;

            foreach (var player in PlayerScores)
            {
                if (player != null && player.TotalScore > highestScore)
                {
                    highestScore = player.TotalScore;
                    winner = player;
                }
            }

            if (winner != null)
            {
                OverallWinnerId = winner.PlayerId;
            }

            EndTime = DateTime.UtcNow;
            return winner;
        }
    }

    /// <summary>
    /// Request payload for AI judging API
    /// </summary>
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

    /// <summary>
    /// Response from AI judging API (matches expected JSON format)
    /// </summary>
    [Serializable]
    public class JudgeApiResponse
    {
        public PlayerJudgeResultApi[] results;
        public int winnerId;
        public string winningDishPrompt;
    }

    /// <summary>
    /// Individual player result from API (JSON naming convention)
    /// </summary>
    [Serializable]
    public class PlayerJudgeResultApi
    {
        public int playerId;
        public JudgeVerdictApi critic;
        public JudgeVerdictApi visionary;
        public JudgeVerdictApi soulCook;
        public int totalScore;
    }

    /// <summary>
    /// Judge verdict from API (JSON naming convention)
    /// </summary>
    [Serializable]
    public class JudgeVerdictApi
    {
        public int score;
        public string comment;
    }

    /// <summary>
    /// Result from image generation API (JSON naming convention)
    /// </summary>
    [Serializable]
    public class ImageGenerationResult
    {
        public string imageUrl;
        public string prompt;

        // Property for PascalCase access (Unity convention)
        public string ImageUrl => imageUrl;
        public string Prompt => prompt;
    }

}

