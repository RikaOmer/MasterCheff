# AI Chef Battle - Next Steps

This document outlines the remaining setup and configuration steps to get the AI Chef Battle game fully operational.

---

## 1. Install Photon PUN2

The game uses Photon PUN2 for multiplayer networking.

### Option A: Unity Asset Store
1. Open Unity and go to **Window > Asset Store**
2. Search for "PUN 2 - FREE"
3. Download and Import the package
4. After import, the Photon Setup Wizard will appear
5. Create a free account at [photonengine.com](https://www.photonengine.com/)
6. Create a new Photon PUN application in the dashboard
7. Copy your **App ID** and paste it in the Setup Wizard

### Option B: Package Manager
1. Go to **Window > Package Manager**
2. Click **+** > **Add package from git URL**
3. Enter: `https://github.com/PhotonEngine/PhotonUnityNetworking.git`

### Configuration
After installation, configure Photon:
1. Open **Window > Photon Unity Networking > PUN Wizard**
2. Enter your App ID
3. Select your region (or use "Best Region")

---

## 2. Set Up the Relay Backend

The game requires a backend server to securely call OpenAI and DALL-E APIs without exposing API keys to clients.

### Recommended: Firebase Cloud Functions

#### Setup Steps:
```bash
# Install Firebase CLI
npm install -g firebase-tools

# Login to Firebase
firebase login

# Initialize a new project
mkdir ai-chef-backend
cd ai-chef-backend
firebase init functions

# Select TypeScript when prompted
```

#### Create the Judge Endpoint (`functions/src/index.ts`):
```typescript
import * as functions from 'firebase-functions';
import OpenAI from 'openai';

const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY,
});

export const judge = functions.https.onRequest(async (req, res) => {
  if (req.method !== 'POST') {
    res.status(405).send('Method Not Allowed');
    return;
  }

  const { Ingredient1, Ingredient2, Submissions } = req.body;

  const systemPrompt = `You are three renowned culinary judges...`; // Full prompt from AIJudgeService.cs

  try {
    const completion = await openai.chat.completions.create({
      model: 'gpt-4o',
      messages: [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: JSON.stringify({ Ingredient1, Ingredient2, Submissions }) }
      ],
      response_format: { type: 'json_object' }
    });

    const result = JSON.parse(completion.choices[0].message.content || '{}');
    res.json(result);
  } catch (error) {
    res.status(500).json({ error: 'Judgment failed' });
  }
});

export const generateImage = functions.https.onRequest(async (req, res) => {
  if (req.method !== 'POST') {
    res.status(405).send('Method Not Allowed');
    return;
  }

  const { Prompt, Size } = req.body;

  try {
    const response = await openai.images.generate({
      model: 'dall-e-3',
      prompt: Prompt,
      size: Size || '1024x1024',
      quality: 'standard',
      n: 1,
    });

    res.json({
      ImageUrl: response.data[0].url,
      Success: true
    });
  } catch (error) {
    res.status(500).json({ Success: false, Error: 'Image generation failed' });
  }
});
```

#### Deploy:
```bash
# Set your OpenAI API key
firebase functions:config:set openai.key="sk-your-api-key"

# Deploy
firebase deploy --only functions
```

#### Update Unity Configuration:
In your Unity scene, find the `RelayAPIClient` component and set:
- **Base URL**: `https://your-project.cloudfunctions.net`

---

## 3. Create Unity Scenes

Create the following scenes in `Assets/Scenes/`:

| Scene | Purpose |
|-------|---------|
| `Loading.unity` | Initial loading, manager initialization |
| `MainMenu.unity` | Title screen, play button |
| `Lobby.unity` | Matchmaking, room management |
| `Gameplay.unity` | Main game loop |

### Scene Setup:

#### Loading Scene:
- Add `GameBootstrapper` prefab
- Add `LoadingScreen` UI

#### Lobby Scene:
- Create Canvas with `LobbyPanel`
- Add `NetworkManager` (with PhotonView component)

#### Gameplay Scene:
- Create Canvas with:
  - `CookingPanel`
  - `JudgingPanel`
  - `ResultsPanel`
- Add `RoundLoopController`
- Add `PowerUpManager` (with PhotonView component)
- Add `AIJudgeService`
- Add `RelayAPIClient`

---

## 4. Create the Ingredient Database Asset

1. In Unity, right-click in the Project window
2. Select **Create > MasterCheff > Ingredient Database**
3. Name it `MainIngredientDatabase`
4. Select the asset and in the Inspector, right-click the component header
5. Select **Populate Default Ingredients** to fill with 70+ ingredients
6. Assign this asset to the `RoundLoopController` component

---

## 5. Create UI Prefabs

Create prefabs in `Assets/Prefabs/UI/`:

### Required Prefabs:

| Prefab | Components |
|--------|------------|
| `PlayerListItem.prefab` | Contains NameText, ReadyIndicator |
| `PlayerScoreItem.prefab` | Contains RankText, NameText, DishText, ScoreText |
| `JudgeCommentDisplay.prefab` | Contains JudgeNameText, ScoreText, CommentText |

---

## 6. Configure Build Settings

### Player Settings:
1. Go to **Edit > Project Settings > Player**
2. Set Company Name and Product Name
3. Set the default orientation to **Portrait** or **Auto Rotation**

### Build Settings:
1. Add scenes in order: Loading, MainMenu, Lobby, Gameplay
2. For Android: Set minimum API level to 24+
3. For iOS: Set target iOS version to 13.0+

---

## 7. Testing Checklist

### Local Testing:
- [ ] Photon connects successfully
- [ ] Can create and join rooms
- [ ] Player list updates when players join/leave
- [ ] Ready system works
- [ ] Timer counts down correctly

### Multiplayer Testing:
- [ ] Quick Match finds/creates rooms
- [ ] Room codes work for joining
- [ ] Ingredients sync across all players
- [ ] Submissions are collected from all players
- [ ] Results display correctly for everyone

### AI Integration Testing:
- [ ] Backend health check passes
- [ ] Judge API returns valid JSON
- [ ] Scores are calculated correctly
- [ ] Image generation returns valid URLs
- [ ] Images display in results panel

---

## 8. Optional Enhancements

### Audio
- Add cooking/kitchen ambient sounds
- Add timer tick sounds (especially for last 10 seconds)
- Add victory/defeat jingles
- Add UI button click sounds

### Visual Polish
- Add particle effects for submissions
- Add confetti for round winners
- Add animated judge avatars
- Add ingredient icons/sprites

### Monetization
- Add more power-ups as IAP
- Add cosmetic chef avatars
- Add premium ingredients

### Social Features
- Add friend system via PlayFab or Firebase
- Add leaderboards
- Add match history

---

## Need Help?

- **Photon Documentation**: https://doc.photonengine.com/pun/current/getting-started/pun-intro
- **OpenAI API Docs**: https://platform.openai.com/docs
- **Firebase Functions**: https://firebase.google.com/docs/functions
- **Unity UI Toolkit**: https://docs.unity3d.com/Manual/UIElements.html

---

*Document generated for AI Chef Battle - MasterCheff Project*
