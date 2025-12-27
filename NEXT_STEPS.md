# AI Chef Battle - Next Steps

This document outlines the remaining setup and configuration steps to get the AI Chef Battle game fully operational.

## 1. Install Photon PUN2

1. Open Unity Asset Store and search for "PUN 2 - FREE"
2. Download and Import the package
3. Create account at photonengine.com
4. Create a new Photon PUN application
5. Copy your App ID into the Setup Wizard

## 2. Set Up the Relay Backend

Create Firebase Cloud Functions with `/judge` and `/generate-image` endpoints.

## 3. Create Unity Scenes

- Loading.unity
- MainMenu.unity  
- Lobby.unity
- Gameplay.unity

## 4. Create the Ingredient Database Asset

1. Right-click in Project window
2. Select Create → MasterCheff → Ingredient Database
3. Use context menu to Populate Default Ingredients

## 5. Testing Checklist

- [ ] Photon connects successfully
- [ ] Can create and join rooms
- [ ] Player list updates correctly
- [ ] Timer works
- [ ] AI judging returns results
- [ ] Images display correctly

See full documentation in the project.
