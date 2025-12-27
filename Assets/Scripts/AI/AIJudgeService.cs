using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MasterCheff.Core;
using MasterCheff.Data;

namespace MasterCheff.AI
{
    /// <summary>
    /// Service for AI-powered dish judging
    /// Formats submissions and parses judge responses
    /// </summary>
    public class AIJudgeService : Singleton<AIJudgeService>
    {
        [Header("Judging Settings")]
        [SerializeField] private bool _useDetailedComments = true;
        [SerializeField] private int _minScorePerJudge = 1;
        [SerializeField] private int _maxScorePerJudge = 10;

        [Header("Image Generation")]
        [SerializeField] private string _imageSize = "1024x1024";
        [SerializeField] private string _imageStyle = "photorealistic";

        // Events
        public event Action OnJudgingStarted;
        public event Action<RoundResult> OnJudgingCompleted;
        public event Action<string> OnJudgingFailed;
        public event Action OnImageGenerationStarted;
        public event Action<string> OnImageGenerationCompleted;

        // The system prompt for OpenAI
        private const string JUDGE_SYSTEM_PROMPT = @"You are three renowned culinary judges evaluating dishes made from specific ingredients.

JUDGES:
1. ""The Critic"" - Technical perfectionist. Evaluates: proper technique, ingredient compatibility, edibility. Penalizes absurd combinations (e.g., garlic ice cream). Scores conservatively.

2. ""The Visionary"" - Avant-garde artist. Values: creativity, poetic descriptions, unexpected pairings, storytelling. Rewards boldness and artistry.

3. ""The Soul Cook"" - Comfort food champion. Values: warmth, appetite appeal, heartiness, nostalgia. Loves dishes that ""hug the soul.""

SCORING RULES:
- Each judge gives a score from 1-10
- Consider the style tag when judging (Homey, Gourmet, Dessert, Healthy, Fusion)
- The Critic favors Gourmet and Healthy dishes
- The Visionary favors Fusion and Dessert dishes
- The Soul Cook favors Homey and comfort dishes
- Comments should be brief but flavorful (1-2 sentences max)

INGREDIENTS THIS ROUND: {0}, {1}

SUBMISSIONS:
{2}

RESPOND IN THIS EXACT JSON FORMAT (no markdown, just pure JSON):
{{
  ""results"": [
    {{
      ""playerId"": <player_id>,
      ""critic"": {{ ""score"": <1-10>, ""comment"": ""<brief comment>"" }},
      ""visionary"": {{ ""score"": <1-10>, ""comment"": ""<brief comment>"" }},
      ""soulCook"": {{ ""score"": <1-10>, ""comment"": ""<brief comment>"" }},
      ""totalScore"": <sum of all three scores>
    }}
  ],
  ""winnerId"": <id of player with highest total>,
  ""winningDishPrompt"": ""A photorealistic image of <winning dish name>: <brief visual description based on the dish>""
}}";

        protected override void OnSingletonAwake()
        {
            Debug.Log("[AIJudgeService] Initialized");
        }

        #region Public Methods

        /// <summary>
        /// Judge all player submissions for a round
        /// </summary>
        public async Task<RoundResult> JudgeSubmissions(RoundIngredients ingredients, PlayerSubmission[] submissions)
        {
            if (submissions == null || submissions.Length == 0)
            {
                Debug.LogError("[AIJudgeService] No submissions to judge");
                return null;
            }

            OnJudgingStarted?.Invoke();
            Debug.Log($"[AIJudgeService] Judging {submissions.Length} submissions...");

            try
            {
                // Create the request
                JudgeRequest request = new JudgeRequest(ingredients, submissions);

                // Call relay backend
                if (RelayAPIClient.HasInstance)
                {
                    JudgeApiResponse apiResponse = await RelayAPIClient.Instance.RequestJudgment(request);

                    if (apiResponse != null)
                    {
                        RoundResult result = ConvertApiResponse(apiResponse, submissions);
                        OnJudgingCompleted?.Invoke(result);
                        return result;
                    }
                }

                Debug.LogError("[AIJudgeService] Failed to get response from backend");
                OnJudgingFailed?.Invoke("Backend unavailable");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIJudgeService] Judging error: {ex.Message}");
                OnJudgingFailed?.Invoke(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Generate an image of the winning dish
        /// </summary>
        public async Task<ImageGenerationResponse> GenerateDishImage(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                Debug.LogWarning("[AIJudgeService] Empty image prompt");
                return null;
            }

            OnImageGenerationStarted?.Invoke();
            Debug.Log($"[AIJudgeService] Generating dish image: {prompt}");

            try
            {
                ImageGenerationRequest request = new ImageGenerationRequest(prompt, _imageSize);

                if (RelayAPIClient.HasInstance)
                {
                    ImageGenerationResponse response = await RelayAPIClient.Instance.RequestImageGeneration(request);

                    if (response != null && response.Success)
                    {
                        OnImageGenerationCompleted?.Invoke(response.ImageUrl);
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIJudgeService] Image generation error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Download a dish image as a Texture2D
        /// </summary>
        public async Task<Texture2D> DownloadDishImage(string imageUrl)
        {
            if (RelayAPIClient.HasInstance)
            {
                return await RelayAPIClient.Instance.DownloadImage(imageUrl);
            }
            return null;
        }

        /// <summary>
        /// Get the system prompt for the backend to use
        /// </summary>
        public string GetSystemPrompt(RoundIngredients ingredients, PlayerSubmission[] submissions)
        {
            string submissionsText = FormatSubmissionsForPrompt(submissions);
            return string.Format(JUDGE_SYSTEM_PROMPT, 
                ingredients.Ingredient1, 
                ingredients.Ingredient2, 
                submissionsText);
        }

        #endregion

        #region Conversion Helpers

        /// <summary>
        /// Convert API response to RoundResult
        /// </summary>
        private RoundResult ConvertApiResponse(JudgeApiResponse apiResponse, PlayerSubmission[] submissions)
        {
            RoundResult result = new RoundResult
            {
                WinnerPlayerId = apiResponse.winnerId,
                WinningDishImagePrompt = apiResponse.winningDishPrompt,
                PlayerResults = new PlayerJudgeResult[apiResponse.results.Length]
            };

            for (int i = 0; i < apiResponse.results.Length; i++)
            {
                var apiResult = apiResponse.results[i];
                
                // Find matching submission for player name and dish name
                PlayerSubmission submission = FindSubmission(submissions, apiResult.playerId);

                result.PlayerResults[i] = new PlayerJudgeResult
                {
                    PlayerId = apiResult.playerId,
                    PlayerName = submission?.PlayerName ?? $"Player {apiResult.playerId}",
                    DishName = submission?.DishName ?? "Unknown Dish",
                    Critic = new JudgeVerdict("The Critic", apiResult.critic.score, apiResult.critic.comment),
                    Visionary = new JudgeVerdict("The Visionary", apiResult.visionary.score, apiResult.visionary.comment),
                    SoulCook = new JudgeVerdict("The Soul Cook", apiResult.soulCook.score, apiResult.soulCook.comment),
                    TotalScore = apiResult.totalScore
                };

                // Set winner info
                if (apiResult.playerId == result.WinnerPlayerId)
                {
                    result.WinnerPlayerName = result.PlayerResults[i].PlayerName;
                    result.WinningDishName = result.PlayerResults[i].DishName;
                }
            }

            return result;
        }

        /// <summary>
        /// Find a submission by player ID
        /// </summary>
        private PlayerSubmission FindSubmission(PlayerSubmission[] submissions, int playerId)
        {
            foreach (var submission in submissions)
            {
                if (submission.PlayerId == playerId)
                    return submission;
            }
            return null;
        }

        /// <summary>
        /// Format submissions for the AI prompt
        /// </summary>
        private string FormatSubmissionsForPrompt(PlayerSubmission[] submissions)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < submissions.Length; i++)
            {
                var sub = submissions[i];
                sb.AppendLine($"Player {sub.PlayerId} ({sub.PlayerName}):");
                sb.AppendLine($"  Dish Name: {sub.DishName}");
                sb.AppendLine($"  Style: {GetStyleDisplayName(sub.StyleTag)}");
                
                if (!string.IsNullOrEmpty(sub.Description))
                {
                    sb.AppendLine($"  Description: {sub.Description}");
                }
                
                if (!string.IsNullOrEmpty(sub.SecretIngredient))
                {
                    sb.AppendLine($"  Secret Ingredient: {sub.SecretIngredient}");
                }
                
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get display name for style tag
        /// </summary>
        private string GetStyleDisplayName(DishStyleTag tag)
        {
            return tag switch
            {
                DishStyleTag.HomeyComfort => "Homey & Comfort",
                DishStyleTag.GourmetFineDining => "Gourmet / Fine Dining",
                DishStyleTag.DecadentDessert => "Decadent Dessert",
                DishStyleTag.HealthyFresh => "Healthy & Fresh",
                DishStyleTag.CrazyFusion => "Crazy Fusion",
                _ => tag.ToString()
            };
        }

        #endregion

        #region Debug / Testing

        /// <summary>
        /// Create a mock result for testing without backend
        /// </summary>
        public RoundResult CreateMockResult(PlayerSubmission[] submissions)
        {
            var result = new RoundResult
            {
                PlayerResults = new PlayerJudgeResult[submissions.Length]
            };

            int highestScore = 0;
            int winnerIndex = 0;

            string[] criticComments = {
                "Technically proficient but lacks refinement.",
                "A bold choice that actually works.",
                "The technique is sound, presentation could improve.",
                "Surprisingly well-balanced flavors."
            };

            string[] visionaryComments = {
                "A canvas of culinary creativity!",
                "The artistry speaks to the soul.",
                "Unexpected but delightfully bold.",
                "A journey through flavor and form."
            };

            string[] soulCookComments = {
                "Like a warm hug on a plate.",
                "Grandma would be proud!",
                "This brings back memories.",
                "Pure comfort food excellence."
            };

            for (int i = 0; i < submissions.Length; i++)
            {
                int criticScore = UnityEngine.Random.Range(5, 10);
                int visionaryScore = UnityEngine.Random.Range(5, 10);
                int soulCookScore = UnityEngine.Random.Range(5, 10);
                int total = criticScore + visionaryScore + soulCookScore;

                result.PlayerResults[i] = new PlayerJudgeResult
                {
                    PlayerId = submissions[i].PlayerId,
                    PlayerName = submissions[i].PlayerName,
                    DishName = submissions[i].DishName,
                    Critic = new JudgeVerdict("The Critic", criticScore, criticComments[i % criticComments.Length]),
                    Visionary = new JudgeVerdict("The Visionary", visionaryScore, visionaryComments[i % visionaryComments.Length]),
                    SoulCook = new JudgeVerdict("The Soul Cook", soulCookScore, soulCookComments[i % soulCookComments.Length]),
                    TotalScore = total
                };

                if (total > highestScore)
                {
                    highestScore = total;
                    winnerIndex = i;
                }
            }

            result.WinnerPlayerId = submissions[winnerIndex].PlayerId;
            result.WinnerPlayerName = submissions[winnerIndex].PlayerName;
            result.WinningDishName = submissions[winnerIndex].DishName;
            result.WinningDishImagePrompt = $"A photorealistic image of {submissions[winnerIndex].DishName}, beautifully plated, professional food photography";

            return result;
        }

        #endregion
    }
}
