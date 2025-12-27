#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using MasterCheff.Gameplay;

namespace MasterCheff.Editor
{
    /// <summary>
    /// Editor utility to automatically assign ingredient sprites from folders
    /// </summary>
    public class IngredientSpriteAssigner : EditorWindow
    {
        private IngredientDatabase _database;
        private string _spritesPath = "Assets/Sprites/Ingredients";
        private Vector2 _scrollPosition;
        private Dictionary<string, Sprite> _foundSprites = new Dictionary<string, Sprite>();

        [MenuItem("MasterCheff/Ingredient Sprite Assigner")]
        public static void ShowWindow()
        {
            GetWindow<IngredientSpriteAssigner>("Sprite Assigner");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Ingredient Sprite Auto-Assigner", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This tool automatically assigns sprites to ingredients based on file names.\n" +
                "Place your ingredient images in the appropriate category folders under Assets/Sprites/Ingredients/",
                MessageType.Info);

            EditorGUILayout.Space(10);

            _database = (IngredientDatabase)EditorGUILayout.ObjectField(
                "Ingredient Database", 
                _database, 
                typeof(IngredientDatabase), 
                false);

            _spritesPath = EditorGUILayout.TextField("Sprites Path", _spritesPath);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Scan for Sprites", GUILayout.Height(30)))
            {
                ScanForSprites();
            }

            if (_foundSprites.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Found {_foundSprites.Count} sprites", EditorStyles.helpBox);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
                foreach (var kvp in _foundSprites)
                {
                    EditorGUILayout.LabelField($"  - {kvp.Key}");
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(_database == null || _foundSprites.Count == 0);
            if (GUILayout.Button("Assign Sprites to Database", GUILayout.Height(40)))
            {
                AssignSpritesToDatabase();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Expected Folder Structure:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assets/Sprites/Ingredients/\n" +
                "├── Proteins/     (chicken.png, beef.png, etc.)\n" +
                "├── Fruits/       (lemon.png, mango.png, etc.)\n" +
                "├── Vegetables/   (garlic.png, onion.png, etc.)\n" +
                "├── Spices/       (chili.png, cumin.png, etc.)\n" +
                "├── Dairy/        (butter.png, cheese.png, etc.)\n" +
                "├── Sweets/       (chocolate.png, honey.png, etc.)\n" +
                "└── Herbs/        (basil.png, mint.png, etc.)",
                MessageType.None);
        }

        private void ScanForSprites()
        {
            _foundSprites.Clear();

            if (!Directory.Exists(_spritesPath))
            {
                Debug.LogError($"[SpriteAssigner] Path not found: {_spritesPath}");
                return;
            }

            // Find all PNG files recursively
            string[] pngFiles = Directory.GetFiles(_spritesPath, "*.png", SearchOption.AllDirectories);

            foreach (string filePath in pngFiles)
            {
                string assetPath = filePath.Replace("\\", "/");
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                if (sprite != null)
                {
                    // Use filename without extension as key (normalized)
                    string key = Path.GetFileNameWithoutExtension(filePath).ToLower().Replace("_", " ");
                    _foundSprites[key] = sprite;
                }
            }

            Debug.Log($"[SpriteAssigner] Found {_foundSprites.Count} sprites");
        }

        private void AssignSpritesToDatabase()
        {
            if (_database == null)
            {
                Debug.LogError("[SpriteAssigner] No database selected!");
                return;
            }

            int assignedCount = 0;
            var allIngredients = _database.GetAllIngredients();

            foreach (var ingredient in allIngredients)
            {
                // Try to find matching sprite
                string normalizedName = ingredient.Name.ToLower().Replace("_", " ");
                
                if (_foundSprites.TryGetValue(normalizedName, out Sprite sprite))
                {
                    ingredient.Icon = sprite;
                    assignedCount++;
                    Debug.Log($"[SpriteAssigner] Assigned sprite to: {ingredient.Name}");
                }
                else
                {
                    // Try alternate naming patterns
                    string altName = normalizedName.Replace(" ", "_");
                    if (_foundSprites.TryGetValue(altName, out sprite))
                    {
                        ingredient.Icon = sprite;
                        assignedCount++;
                        Debug.Log($"[SpriteAssigner] Assigned sprite to: {ingredient.Name}");
                    }
                }
            }

            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SpriteAssigner] Assigned {assignedCount}/{allIngredients.Count} sprites to ingredients");
            EditorUtility.DisplayDialog(
                "Sprite Assignment Complete",
                $"Assigned {assignedCount} sprites to {allIngredients.Count} ingredients.\n\n" +
                $"Missing: {allIngredients.Count - assignedCount} sprites",
                "OK");
        }
    }
}
#endif

