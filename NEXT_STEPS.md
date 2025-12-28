# AI Chef Battle - Next Steps

This document outlines the remaining setup and configuration steps to get the AI Chef Battle game fully operational.

---

## 1. Install Photon PUN2 ✅ COMPLETE

The game uses Photon PUN2 for multiplayer networking.

### Option A: Unity Asset Store
1. Open Unity and go to **Window → Asset Store**
2. Search for "PUN 2 - FREE"
3. Download and Import the package
4. After import, the Photon Setup Wizard will appear
5. Create a free account at [photonengine.com](https://www.photonengine.com/)
6. Create a new Photon PUN application in the dashboard
7. Copy your **App ID** and paste it in the Setup Wizard

### Option B: Package Manager
1. Go to **Window → Package Manager**
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/PhotonEngine/PhotonUnityNetworking.git`

### Configuration
After installation, configure Photon:
1. Open **Window → Photon Unity Networking → PUN Wizard**
2. Enter your App ID
3. Select your region (or use "Best Region")

---

## 2. Set Up the Relay Backend ✅ COMPLETE

The game requires a backend server to securely call the OpenAI API for AI judging without exposing API keys to clients.

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

## 3. Create Unity Scenes ⚠️ PARTIALLY COMPLETE

Create the following scenes in `Assets/Scenes/`:

| Scene | Purpose | Status |
|-------|---------|--------|
| `Loading.unity` | Initial loading, manager initialization | ✅ Complete |
| `MainMenu.unity` | Title screen, play button | ❌ Missing UI |
| `Lobby.unity` | Matchmaking, room management | ⚠️ Partially Complete |
| `Gameplay.unity` | Main game loop | ✅ Complete |

### Scene Setup Status:

#### Loading Scene: ✅ COMPLETE
- ✅ `GameBootstrapper` prefab added
- ✅ `LoadingPanel` with `LoadingScreen` component added

#### Lobby Scene: ⚠️ PARTIALLY COMPLETE
- ✅ `NetworkManager` with `PhotonView` component added
- ❌ **Missing**: Canvas with `LobbyPanel` component and all UI elements

**To Complete:**
1. Open the `Lobby.unity` scene in Unity
2. Create a Canvas:
   - GameObject → UI → Canvas
   - Add CanvasScaler component (set to Scale With Screen Size, Reference Resolution: 1080x1920)
   - Add GraphicRaycaster component
3. Create LobbyPanel GameObject:
   - Create empty GameObject as child of Canvas, name it "LobbyPanel"
   - Add RectTransform (anchor: stretch-stretch, size: 0,0)
   - Add the `LobbyPanel` component (Scripts/UI/Panels/LobbyPanel.cs)
4. Create and assign all required UI elements in the Inspector:
   - **Player Name**: TMP_InputField for player name input
   - **Quick Match**: Button and TextMeshProUGUI for quick match button
   - **Room Code**: TMP_InputField for room code, Create Room button, Join Room button
   - **Room Info Panel**: GameObject panel, room code display text, copy code button, leave room button
   - **Ready Button**: Button and TextMeshProUGUI for ready status
   - **Player List**: Transform container for player list items, PlayerListItem prefab reference
   - **Player Count**: TextMeshProUGUI for displaying player count
   - **Status**: TextMeshProUGUI for status messages, loading indicator GameObject
   - **Start Game**: Button and TextMeshProUGUI (only visible to master client)
   - **Connection**: Connection panel GameObject, connect button, connection status text
   
   **Note**: This is a complex setup. Consider using the Scene Setup Wizard (MasterCheff → Scene Setup Wizard) to automatically create the basic structure, then manually assign the references in the Inspector.

#### Gameplay Scene: ✅ COMPLETE
- ✅ Canvas with all panels:
  - ✅ `CookingPanel`
  - ✅ `JudgingPanel`
  - ✅ `ResultsPanel`
- ✅ `RoundLoopController` added
- ✅ `PowerUpManager` with `PhotonView` component added
- ✅ `AIJudgeService` added
- ✅ `RelayAPIClient` added

#### MainMenu Scene: ❌ INCOMPLETE
- ❌ **Missing**: Canvas
- ❌ **Missing**: MainMenuPanel GameObject with UI elements
- ❌ **Missing**: TitleText ("AI Chef Battle")
- ❌ **Missing**: PlayButton that loads Lobby scene

**To Complete:**
1. Open the `MainMenu.unity` scene in Unity
2. Create a Canvas:
   - GameObject → UI → Canvas
   - Add CanvasScaler component (set to Scale With Screen Size, Reference Resolution: 1080x1920)
   - Add GraphicRaycaster component
3. Create MainMenuPanel:
   - Create empty GameObject as child of Canvas, name it "MainMenuPanel"
   - Add RectTransform (anchor: stretch-stretch, size: 0,0)
4. Create TitleText:
   - Create TextMeshPro - Text (UI) as child of MainMenuPanel
   - Name it "TitleText"
   - Set text to "AI Chef Battle", font size 48, center alignment
   - Position: Anchor (0.1, 0.7) to (0.9, 0.9)
5. Create PlayButton:
   - Create Button as child of MainMenuPanel
   - Name it "PlayButton"
   - Position: Anchor (0.3, 0.4) to (0.7, 0.5)
   - Add TextMeshPro - Text (UI) as child, set text to "PLAY"
   - Configure button onClick to load Lobby scene:
     ```csharp
     // In button's OnClick event, add:
     Managers.SceneLoader.Instance.LoadScene(Constants.Scenes.LOBBY);
     ```

---

## 4. Create the Ingredient Database Asset ✅ COMPLETE

1. ✅ In Unity, right-click in the Project window
2. ✅ Select **Create → MasterCheff → Ingredient Database**
3. ✅ Name it `MainIngredientDatabase`
4. ✅ Select the asset and in the Inspector, right-click the component header
5. ✅ Select **Populate Default Ingredients** to fill with 70+ ingredients
6. ✅ Assign this asset to the `RoundLoopController` component

### Adding Ingredient Sprites ✅ COMPLETE

✅ All 70 ingredients have been matched with sprites using the "Scan for Sprites" feature.

**Note**: The "Scan for Sprites" context menu option automatically matches PNG files to ingredients (handles accents, special characters, etc.). Simply right-click the IngredientDatabase component header and select "Scan for Sprites" to auto-assign all matching sprite files.

---

## 5. Create UI Prefabs

Create prefabs in `Assets/Prefabs/UI/`:

### Required Prefabs:

| Prefab | Components |
|--------|------------|
| `PlayerListItem.prefab` | Contains NameText, ReadyIndicator |
| `PlayerScoreItem.prefab` | Contains RankText, NameText, DishText, ScoreText |
| `JudgeCommentDisplay.prefab` | Contains JudgeNameText, ScoreText, CommentText |

### Panel Layout Suggestions:

#### CookingPanel:
```
┌─────────────────────────────────────┐
│  Round 3/10              00:45      │
├─────────────────────────────────────┤
│                                     │
│    🥕 Carrot  +  🍫 Chocolate       │
│                                     │
├─────────────────────────────────────┤
│  Dish Name: [________________]      │
│                                     │
│  Description:                       │
│  [____________________________]     │
│  [____________________________]     │
│                                     │
├─────────────────────────────────────┤
│ [Homey] [Gourmet] [Dessert]        │
│ [Healthy] [Fusion]                  │
├─────────────────────────────────────┤
│         [ SUBMIT DISH ]             │
└─────────────────────────────────────┘
```

---

## 6. Configure Build Settings

### Player Settings:
1. Go to **Edit → Project Settings → Player**
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
- [ ] Ingredient sprites display correctly in UI

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

## 9. File Structure Overview

```
Assets/
├── Scenes/
│   ├── Loading.unity
│   ├── MainMenu.unity
│   ├── Lobby.unity
│   └── Gameplay.unity
├── Scripts/
│   ├── AI/
│   │   ├── AIJudgeService.cs
│   │   └── RelayAPIClient.cs
│   ├── Core/
│   │   ├── Constants.cs
│   │   ├── GameBootstrapper.cs
│   │   ├── GameState.cs
│   │   └── Singleton.cs
│   ├── Data/
│   │   ├── GameSaveData.cs
│   │   └── MultiplayerData.cs
│   ├── Gameplay/
│   │   ├── IngredientDatabase.cs
│   │   ├── PowerUpManager.cs
│   │   └── RoundLoopController.cs
│   ├── Managers/
│   │   ├── AudioManager.cs
│   │   ├── EventManager.cs
│   │   ├── GameManager.cs
│   │   ├── SaveManager.cs
│   │   ├── SceneLoader.cs
│   │   └── UIManager.cs
│   ├── Multiplayer/
│   │   └── NetworkManager.cs
│   └── UI/
│       ├── Components/
│       │   ├── IngredientCard.cs
│       │   ├── PowerUpButton.cs
│       │   ├── StyleTagButton.cs
│       │   └── TimerDisplay.cs
│       └── Panels/
│           ├── CookingPanel.cs
│           ├── JudgingPanel.cs
│           ├── LobbyPanel.cs
│           └── ResultsPanel.cs
├── Sprites/
│   └── Ingredients/           # Your ingredient images go here
│       ├── Proteins/          # chicken.png, beef.png, salmon.png, etc.
│       ├── Vegetables/        # garlic.png, onion.png, tomato.png, etc.
│       ├── Fruits/            # lemon.png, mango.png, apple.png, etc.
│       ├── Spices/            # chili.png, cumin.png, cinnamon.png, etc.
│       ├── Dairy/             # butter.png, cheese.png, cream.png, etc.
│       ├── Sweets/            # chocolate.png, honey.png, vanilla.png, etc.
│       ├── Herbs/             # basil.png, mint.png, rosemary.png, etc.
│       ├── Grains/            # rice.png, pasta.png, bread.png, etc.
│       ├── Seafood/           # shrimp.png, lobster.png, crab.png, etc.
│       └── Other/             # miscellaneous ingredients
├── Prefabs/
│   └── UI/
│       ├── PlayerListItem.prefab
│       └── PlayerScoreItem.prefab
└── Resources/
    └── IngredientDatabase/
        └── MainIngredientDatabase.asset
```

---

## Need Help?

- **Photon Documentation**: https://doc.photonengine.com/pun/current/getting-started/pun-intro
- **OpenAI API Docs**: https://platform.openai.com/docs
- **Firebase Functions**: https://firebase.google.com/docs/functions
- **Unity UI Toolkit**: https://docs.unity3d.com/Manual/UIElements.html

---

*Document generated for AI Chef Battle - MasterCheff Project*

