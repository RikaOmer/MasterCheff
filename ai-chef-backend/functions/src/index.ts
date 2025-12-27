/**
 * Import function triggers from their respective submodules:
 *
 * import {onCall} from "firebase-functions/v2/https";
 * import {onDocumentWritten} from "firebase-functions/v2/firestore";
 *
 * See a full list of supported triggers at https://firebase.google.com/docs/functions
 */

import {setGlobalOptions} from "firebase-functions";
setGlobalOptions({maxInstances: 10});


import * as functions from "firebase-functions";
import OpenAI from "openai";


export const judge = functions.https.onRequest(async (req, res) => {
  if (req.method !== "POST") {
    res.status(405).send("Method Not Allowed");
    return;
  }
  const openai = new OpenAI({
    apiKey: process.env.OPENAI_API_KEY,
  });

  const {Ingredient1, Ingredient2, Submissions} = req.body;

  // eslint-disable-next-line max-len
  const systemPrompt = `You are three renowned culinary judges evaluating dishes made from specific ingredients.

JUDGES:
1. ""The Critic"" - Technical perfectionist. Evaluates: proper technique,
ingredient compatibility, edibility. Penalizes absurd combinations
(e.g., garlic ice cream). Scores conservatively.

2. ""The Visionary"" - Avant-garde artist. Values: creativity, poetic
descriptions, unexpected pairings, storytelling. Rewards boldness and artistry.

3. ""The Soul Cook"" - Comfort food champion. Values: warmth, appetite appeal,
heartiness, nostalgia. Loves dishes that ""hug the soul.""

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
  ""winningDishPrompt"": ""A photorealistic image of <winning dish name>:
<brief visual description based on the dish>""
}}`;

  try {
    const completion = await openai.chat.completions.create({
      model: "gpt-4o",
      messages: [
        {role: "system", content: systemPrompt},
        {
          role: "user",
          content: JSON.stringify({Ingredient1, Ingredient2, Submissions}),
        },
      ],
      response_format: {type: "json_object"},
    });

    const result = JSON.parse(
      completion.choices[0].message.content || "{}",
    );
    res.json(result);
  } catch (error) {
    res.status(500).json({error: "Judgment failed"});
  }
});
