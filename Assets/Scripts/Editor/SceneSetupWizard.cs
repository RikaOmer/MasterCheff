#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Core;
using MasterCheff.Multiplayer;
using MasterCheff.Gameplay;
using MasterCheff.AI;
using MasterCheff.UI;
using MasterCheff.UI.Panels;
using Photon.Pun;

namespace MasterCheff.Editor
{
    /// <summary>
    /// Scene Setup Wizard - Automatically configures all game scenes
    /// </summary>
    public class SceneSetupWizard : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _setupLoading = false;
        private bool _setupMainMenu = false;
        private bool _setupLobby = false;
        private bool _setupGameplay = false;

        [MenuItem("MasterCheff/Scene Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupWizard>("Scene Setup Wizard");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("AI Chef Battle - Scene Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This wizard will automatically set up all game scenes with required components and UI.\n" +
                "Make sure to save your current work before proceeding!",
                MessageType.Info);

            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Loading Scene
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("1. Loading Scene", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sets up GameBootstrapper prefab and LoadingScreen UI", MessageType.None);
            _setupLoading = EditorGUILayout.Toggle("Setup Loading Scene", _setupLoading);
            if (GUILayout.Button("Setup Loading Scene Now", GUILayout.Height(30)))
            {
                SetupLoadingScene();
            }

            EditorGUILayout.Space(10);

            // MainMenu Scene
            EditorGUILayout.LabelField("2. MainMenu Scene", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Creates title screen with Play button", MessageType.None);
            _setupMainMenu = EditorGUILayout.Toggle("Setup MainMenu Scene", _setupMainMenu);
            if (GUILayout.Button("Setup MainMenu Scene Now", GUILayout.Height(30)))
            {
                SetupMainMenuScene();
            }

            EditorGUILayout.Space(10);

            // Lobby Scene
            EditorGUILayout.LabelField("3. Lobby Scene", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sets up NetworkManager and LobbyPanel UI", MessageType.None);
            _setupLobby = EditorGUILayout.Toggle("Setup Lobby Scene", _setupLobby);
            if (GUILayout.Button("Setup Lobby Scene Now", GUILayout.Height(30)))
            {
                SetupLobbyScene();
            }

            EditorGUILayout.Space(10);

            // Gameplay Scene
            EditorGUILayout.LabelField("4. Gameplay Scene", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sets up all managers and gameplay UI panels", MessageType.None);
            _setupGameplay = EditorGUILayout.Toggle("Setup Gameplay Scene", _setupGameplay);
            if (GUILayout.Button("Setup Gameplay Scene Now", GUILayout.Height(30)))
            {
                SetupGameplayScene();
            }

            EditorGUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(!_setupLoading && !_setupMainMenu && !_setupLobby && !_setupGameplay);
            if (GUILayout.Button("Setup All Scenes", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Setup All Scenes", 
                    "This will modify all selected scenes. Make sure you've saved your work!\n\nContinue?",
                    "Yes", "Cancel"))
                {
                    if (_setupLoading) SetupLoadingScene();
                    if (_setupMainMenu) SetupMainMenuScene();
                    if (_setupLobby) SetupLobbyScene();
                    if (_setupGameplay) SetupGameplayScene();
                    
                    EditorUtility.DisplayDialog("Setup Complete", 
                        "All selected scenes have been set up!\n\nPlease verify the scenes in Unity Editor.",
                        "OK");
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndScrollView();
        }

        private void SetupLoadingScene()
        {
            string scenePath = "Assets/Scenes/Loading.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Error", $"Scene not found: {scenePath}", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Setting up Loading Scene", "Opening scene...", 0.1f);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            EditorUtility.DisplayProgressBar("Setting up Loading Scene", "Setting up GameBootstrapper...", 0.3f);
            
            // Check if GameBootstrapper already exists
            GameBootstrapper bootstrapper = Object.FindObjectOfType<GameBootstrapper>();
            if (bootstrapper == null)
            {
                // Load prefab
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameBootstrapper.prefab");
                if (prefab != null)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    instance.name = "GameBootstrapper";
                    Debug.Log("[SceneSetup] Added GameBootstrapper prefab");
                }
                else
                {
                    Debug.LogWarning("[SceneSetup] GameBootstrapper prefab not found, creating empty GameObject");
                    GameObject go = new GameObject("GameBootstrapper");
                    go.AddComponent<GameBootstrapper>();
                }
            }

            EditorUtility.DisplayProgressBar("Setting up Loading Scene", "Setting up LoadingScreen UI...", 0.6f);
            
            // Setup LoadingScreen UI
            SetupLoadingScreenUI();

            EditorUtility.DisplayProgressBar("Setting up Loading Scene", "Saving scene...", 0.9f);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            EditorUtility.ClearProgressBar();

            Debug.Log("[SceneSetup] Loading scene setup complete!");
            EditorUtility.DisplayDialog("Success", "Loading scene has been set up successfully!", "OK");
        }

        private void SetupLoadingScreenUI()
        {
            // Find or create Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("LoadingCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Find or create LoadingScreen GameObject
            LoadingScreen loadingScreen = Object.FindObjectOfType<LoadingScreen>();
            if (loadingScreen == null)
            {
                GameObject loadingPanel = new GameObject("LoadingPanel");
                loadingPanel.transform.SetParent(canvas.transform, false);
                
                RectTransform rect = loadingPanel.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                
                Image panelImage = loadingPanel.AddComponent<Image>();
                panelImage.color = new Color(0, 0, 0, 0.8f);
                
                loadingScreen = loadingPanel.AddComponent<LoadingScreen>();
            }

            // Create ProgressBar
            Slider slider = loadingScreen.GetComponentInChildren<Slider>();
            if (slider == null)
            {
                GameObject progressBarObj = new GameObject("ProgressBar");
                progressBarObj.transform.SetParent(loadingScreen.transform, false);
                
                RectTransform rect = progressBarObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.1f);
                rect.anchorMax = new Vector2(0.9f, 0.15f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                slider = progressBarObj.AddComponent<Slider>();
                slider.minValue = 0;
                slider.maxValue = 1;
                slider.value = 0;
                
                // Create Fill Area
                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(progressBarObj.transform, false);
                RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;
                
                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                RectTransform fillRect = fill.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(0, 1);
                fillRect.sizeDelta = Vector2.zero;
                Image fillImage = fill.AddComponent<Image>();
                fillImage.color = Color.green;
                slider.fillRect = fillRect;
            }

            // Create ProgressText
            GameObject progressTextObj = GameObject.Find("ProgressText");
            if (progressTextObj == null)
            {
                progressTextObj = new GameObject("ProgressText");
                progressTextObj.transform.SetParent(loadingScreen.transform, false);
                
                RectTransform rect = progressTextObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.15f);
                rect.anchorMax = new Vector2(0.9f, 0.2f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI text = progressTextObj.AddComponent<TextMeshProUGUI>();
                text.text = "0%";
                text.fontSize = 24;
                text.alignment = TextAlignmentOptions.Center;
            }

            // Create LoadingText
            GameObject loadingTextObj = GameObject.Find("LoadingText");
            if (loadingTextObj == null)
            {
                loadingTextObj = new GameObject("LoadingText");
                loadingTextObj.transform.SetParent(loadingScreen.transform, false);
                
                RectTransform rect = loadingTextObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.5f);
                rect.anchorMax = new Vector2(0.9f, 0.6f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI text = loadingTextObj.AddComponent<TextMeshProUGUI>();
                text.text = "Loading...";
                text.fontSize = 32;
                text.alignment = TextAlignmentOptions.Center;
            }

            // Create TipText
            GameObject tipTextObj = GameObject.Find("TipText");
            if (tipTextObj == null)
            {
                tipTextObj = new GameObject("TipText");
                tipTextObj.transform.SetParent(loadingScreen.transform, false);
                
                RectTransform rect = tipTextObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.05f);
                rect.anchorMax = new Vector2(0.9f, 0.1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI text = tipTextObj.AddComponent<TextMeshProUGUI>();
                text.text = "Tip: ...";
                text.fontSize = 18;
                text.alignment = TextAlignmentOptions.Center;
            }

            // Assign references using SerializedObject
            SerializedObject so = new SerializedObject(loadingScreen);
            
            SerializedProperty progressBarProp = so.FindProperty("_progressBar");
            if (progressBarProp != null && slider != null)
            {
                progressBarProp.objectReferenceValue = slider;
            }
            
            SerializedProperty progressFillProp = so.FindProperty("_progressFill");
            if (progressFillProp != null && slider != null && slider.fillRect != null)
            {
                Image fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    progressFillProp.objectReferenceValue = fillImage;
                }
            }
            
            SerializedProperty progressTextProp = so.FindProperty("_progressText");
            if (progressTextProp != null && progressTextObj != null)
            {
                TextMeshProUGUI progressText = progressTextObj.GetComponent<TextMeshProUGUI>();
                if (progressText != null)
                {
                    progressTextProp.objectReferenceValue = progressText;
                }
            }
            
            SerializedProperty loadingTextProp = so.FindProperty("_loadingText");
            if (loadingTextProp != null && loadingTextObj != null)
            {
                TextMeshProUGUI loadingText = loadingTextObj.GetComponent<TextMeshProUGUI>();
                if (loadingText != null)
                {
                    loadingTextProp.objectReferenceValue = loadingText;
                }
            }
            
            SerializedProperty tipTextProp = so.FindProperty("_tipText");
            if (tipTextProp != null && tipTextObj != null)
            {
                TextMeshProUGUI tipText = tipTextObj.GetComponent<TextMeshProUGUI>();
                if (tipText != null)
                {
                    tipTextProp.objectReferenceValue = tipText;
                }
            }
            
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(loadingScreen);
        }

        private void SetupMainMenuScene()
        {
            string scenePath = "Assets/Scenes/MainMenu.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Error", $"Scene not found: {scenePath}", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Setting up MainMenu Scene", "Opening scene...", 0.1f);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            EditorUtility.DisplayProgressBar("Setting up MainMenu Scene", "Creating UI...", 0.5f);
            
            // Create Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MainMenuCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
            }

            // Create MainMenuPanel
            GameObject panel = GameObject.Find("MainMenuPanel");
            if (panel == null)
            {
                panel = new GameObject("MainMenuPanel");
                panel.transform.SetParent(canvas.transform, false);
                
                RectTransform rect = panel.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }

            // Create TitleText
            if (GameObject.Find("TitleText") == null)
            {
                GameObject titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(panel.transform, false);
                
                RectTransform rect = titleObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.7f);
                rect.anchorMax = new Vector2(0.9f, 0.9f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
                text.text = "AI Chef Battle";
                text.fontSize = 48;
                text.alignment = TextAlignmentOptions.Center;
            }

            // Create PlayButton
            if (GameObject.Find("PlayButton") == null)
            {
                GameObject playBtn = new GameObject("PlayButton");
                playBtn.transform.SetParent(panel.transform, false);
                
                RectTransform rect = playBtn.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.3f, 0.4f);
                rect.anchorMax = new Vector2(0.7f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                
                Image btnImage = playBtn.AddComponent<Image>();
                btnImage.color = new Color(0.2f, 0.6f, 0.9f);
                
                Button button = playBtn.AddComponent<Button>();
                
                // Create button text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(playBtn.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
                btnText.text = "PLAY";
                btnText.fontSize = 24;
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.color = Color.white;
                
                // Setup button click to load Lobby scene
                button.onClick.AddListener(() => {
                    if (Managers.SceneLoader.HasInstance)
                    {
                        Managers.SceneLoader.Instance.LoadScene("Lobby");
                    }
                    else
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
                    }
                });
            }

            EditorUtility.DisplayProgressBar("Setting up MainMenu Scene", "Saving scene...", 0.9f);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            EditorUtility.ClearProgressBar();

            Debug.Log("[SceneSetup] MainMenu scene setup complete!");
            EditorUtility.DisplayDialog("Success", "MainMenu scene has been set up successfully!", "OK");
        }

        private void SetupLobbyScene()
        {
            string scenePath = "Assets/Scenes/Lobby.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Error", $"Scene not found: {scenePath}", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Setting up Lobby Scene", "Opening scene...", 0.1f);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            EditorUtility.DisplayProgressBar("Setting up Lobby Scene", "Setting up NetworkManager...", 0.3f);
            
            // Setup NetworkManager
            NetworkManager networkManager = Object.FindObjectOfType<NetworkManager>();
            if (networkManager == null)
            {
                GameObject nmObj = new GameObject("NetworkManager");
                networkManager = nmObj.AddComponent<NetworkManager>();
                
                PhotonView photonView = nmObj.AddComponent<PhotonView>();
                photonView.sceneViewId = 1;
            }

            EditorUtility.DisplayProgressBar("Setting up Lobby Scene", "Setting up LobbyPanel UI...", 0.6f);
            
            // The LobbyPanel already exists in the scene, we just need to verify it
            LobbyPanel lobbyPanel = Object.FindObjectOfType<LobbyPanel>();
            if (lobbyPanel == null)
            {
                Debug.LogWarning("[SceneSetup] LobbyPanel not found. UI elements may need manual setup.");
            }
            else
            {
                Debug.Log("[SceneSetup] LobbyPanel found. Please assign UI element references manually in Inspector.");
            }

            EditorUtility.DisplayProgressBar("Setting up Lobby Scene", "Saving scene...", 0.9f);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            EditorUtility.ClearProgressBar();

            Debug.Log("[SceneSetup] Lobby scene setup complete!");
            EditorUtility.DisplayDialog("Success", 
                "Lobby scene has been set up!\n\n" +
                "Note: Please manually assign UI element references to LobbyPanel component in Inspector.",
                "OK");
        }

        private void SetupGameplayScene()
        {
            string scenePath = "Assets/Scenes/Gameplay.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Error", $"Scene not found: {scenePath}", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Setting up Gameplay Scene", "Opening scene...", 0.1f);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

            EditorUtility.DisplayProgressBar("Setting up Gameplay Scene", "Setting up managers...", 0.3f);
            
            // Setup RoundLoopController
            if (Object.FindObjectOfType<RoundLoopController>() == null)
            {
                GameObject rlcObj = new GameObject("RoundLoopController");
                rlcObj.AddComponent<RoundLoopController>();
            }

            // Setup PowerUpManager
            PowerUpManager powerUpManager = Object.FindObjectOfType<PowerUpManager>();
            if (powerUpManager == null)
            {
                GameObject pumObj = new GameObject("PowerUpManager");
                powerUpManager = pumObj.AddComponent<PowerUpManager>();
                pumObj.AddComponent<PhotonView>();
            }

            // Setup AIJudgeService
            if (Object.FindObjectOfType<AIJudgeService>() == null)
            {
                GameObject aijsObj = new GameObject("AIJudgeService");
                aijsObj.AddComponent<AIJudgeService>();
            }

            // Setup RelayAPIClient
            if (Object.FindObjectOfType<RelayAPIClient>() == null)
            {
                GameObject racObj = new GameObject("RelayAPIClient");
                racObj.AddComponent<RelayAPIClient>();
            }

            EditorUtility.DisplayProgressBar("Setting up Gameplay Scene", "Setting up UI panels...", 0.6f);
            
            // Create Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("GameplayCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create CookingPanel
            if (Object.FindObjectOfType<CookingPanel>() == null)
            {
                GameObject cpObj = new GameObject("CookingPanel");
                cpObj.transform.SetParent(canvas.transform, false);
                
                RectTransform rect = cpObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                
                cpObj.AddComponent<Image>();
                cpObj.AddComponent<CookingPanel>();
            }

            // Create JudgingPanel
            if (Object.FindObjectOfType<JudgingPanel>() == null)
            {
                GameObject jpObj = new GameObject("JudgingPanel");
                jpObj.transform.SetParent(canvas.transform, false);
                
                RectTransform rect = jpObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                
                jpObj.AddComponent<Image>();
                jpObj.AddComponent<JudgingPanel>();
            }

            // Create ResultsPanel
            if (Object.FindObjectOfType<ResultsPanel>() == null)
            {
                GameObject rpObj = new GameObject("ResultsPanel");
                rpObj.transform.SetParent(canvas.transform, false);
                
                RectTransform rect = rpObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                
                rpObj.AddComponent<Image>();
                rpObj.AddComponent<ResultsPanel>();
            }

            EditorUtility.DisplayProgressBar("Setting up Gameplay Scene", "Saving scene...", 0.9f);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            EditorUtility.ClearProgressBar();

            Debug.Log("[SceneSetup] Gameplay scene setup complete!");
            EditorUtility.DisplayDialog("Success", 
                "Gameplay scene has been set up!\n\n" +
                "Note: Please manually assign UI element references to panel components in Inspector.",
                "OK");
        }
    }
}
#endif

