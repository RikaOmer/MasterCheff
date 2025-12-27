# Scene Setup Guide - Step 3 Complete

## ✅ What Has Been Done

I've created a **Unity Editor Wizard** that will automatically set up all 4 game scenes for you. The wizard is located at:

**`Assets/Scripts/Editor/SceneSetupWizard.cs`**

## 🚀 How to Use the Scene Setup Wizard

### Step 1: Open the Wizard
1. In Unity Editor, go to the top menu: **MasterCheff → Scene Setup Wizard**
2. A window will open with options for each scene

### Step 2: Choose Which Scenes to Setup
- Check the boxes next to the scenes you want to set up
- Or click individual "Setup [Scene] Now" buttons for each scene
- Or click "Setup All Scenes" to set up all checked scenes at once

### Step 3: Run the Setup
- Click the appropriate button(s)
- The wizard will:
  - Open each scene
  - Add required GameObjects and components
  - Create UI elements
  - Save the scenes automatically

## 📋 What Each Scene Setup Does

### 1. Loading Scene
✅ Adds GameBootstrapper prefab (if not present)  
✅ Creates LoadingCanvas with proper settings  
✅ Creates LoadingPanel with LoadingScreen component  
✅ Creates ProgressBar, ProgressText, LoadingText, TipText  
✅ Assigns references to LoadingScreen component  

### 2. MainMenu Scene
✅ Creates MainMenuCanvas with proper settings  
✅ Creates MainMenuPanel  
✅ Creates TitleText ("AI Chef Battle")  
✅ Creates PlayButton that loads Lobby scene  
✅ Sets up button click handler  

### 3. Lobby Scene
✅ Verifies/creates NetworkManager with PhotonView  
✅ Verifies LobbyPanel exists  
⚠️ **Note**: UI element references need manual assignment in Inspector  

### 4. Gameplay Scene
✅ Creates RoundLoopController  
✅ Creates PowerUpManager with PhotonView  
✅ Creates AIJudgeService  
✅ Creates RelayAPIClient  
✅ Creates GameplayCanvas  
✅ Creates CookingPanel, JudgingPanel, ResultsPanel  
⚠️ **Note**: UI element references need manual assignment in Inspector  

## ⚠️ Important Notes

### Manual Steps Required

After running the wizard, you'll need to manually assign some UI element references in the Inspector:

#### Lobby Scene:
1. Select **LobbyPanel** GameObject
2. In Inspector, find the **LobbyPanel** component
3. Assign all the UI element references:
   - PlayerNameInput
   - QuickMatchButton
   - RoomCodeInput
   - CreateRoomButton
   - JoinRoomButton
   - RoomInfoPanel
   - RoomCodeDisplayText
   - CopyCodeButton
   - LeaveRoomButton
   - ReadyButton
   - PlayerListContainer
   - PlayerCountText
   - StatusText
   - StartGameButton
   - etc.

#### Gameplay Scene:
1. Select each panel (CookingPanel, JudgingPanel, ResultsPanel)
2. In Inspector, find the panel component
3. Assign all UI element references according to the script requirements

### NetworkManager Configuration

In the Lobby scene, verify the NetworkManager settings:
- Max Players Per Room: 4
- Game Version: "1.0"
- Connection Timeout: 30

### RelayAPIClient Configuration

In the Gameplay scene, set the RelayAPIClient Base URL:
- Select **RelayAPIClient** GameObject
- In Inspector, set **Base URL** to your Firebase function URL
- Example: `https://your-project.cloudfunctions.net`

## 🎯 Next Steps After Setup

1. **Test Scene Loading**: Play the Loading scene and verify GameBootstrapper initializes
2. **Test Navigation**: Click Play button in MainMenu to navigate to Lobby
3. **Test Photon Connection**: In Lobby, verify NetworkManager connects to Photon
4. **Configure Build Settings**: 
   - Go to **File → Build Settings**
   - Add scenes in order: Loading, MainMenu, Lobby, Gameplay
   - Set Loading as index 0

## 📝 Verification Checklist

After running the wizard, verify:

- [ ] Loading Scene: GameBootstrapper prefab + LoadingScreen UI with all references assigned
- [ ] MainMenu Scene: Title + Play button (navigates to Lobby)
- [ ] Lobby Scene: NetworkManager with PhotonView + LobbyPanel (references need manual assignment)
- [ ] Gameplay Scene: All 4 managers + All 3 UI panels (references need manual assignment)

## 🐛 Troubleshooting

### If the wizard doesn't appear:
- Make sure you're in Unity Editor (not just viewing files)
- Check that `Assets/Scripts/Editor/SceneSetupWizard.cs` exists
- Unity may need to recompile scripts - wait a moment

### If scenes don't save:
- Make sure you have write permissions
- Check that scenes aren't locked by another process
- Try saving manually: **File → Save**

### If UI elements are missing:
- The wizard creates basic structure
- Some complex UI hierarchies may need manual creation
- Refer to the NEXT_STEPS.md document for detailed UI layouts

## 📚 Related Documentation

- See `NEXT_STEPS.md` for the original step-by-step guide
- See individual panel scripts for required UI element references:
  - `Assets/Scripts/UI/Panels/LobbyPanel.cs`
  - `Assets/Scripts/UI/Panels/CookingPanel.cs`
  - `Assets/Scripts/UI/Panels/JudgingPanel.cs`
  - `Assets/Scripts/UI/Panels/ResultsPanel.cs`

---

**Scene Setup Wizard created successfully!** 🎉

Now open Unity Editor and use **MasterCheff → Scene Setup Wizard** to set up your scenes.

