using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MasterCheff.Data;

namespace MasterCheff.Gameplay
{
    /// <summary>
    /// ScriptableObject database containing all possible ingredients
    /// </summary>
    [CreateAssetMenu(fileName = "IngredientDatabase", menuName = "MasterCheff/Ingredient Database")]
    public class IngredientDatabase : ScriptableObject
    {
        [Header("Ingredient Categories")]
        [SerializeField] private IngredientCategory[] _categories;

        [Header("Pairing Rules")]
        [SerializeField] private bool _allowSameCategoryPairing = false;
        [SerializeField] private int _minCategoryDifference = 1;

        [Header("Default Ingredients (Fallback)")]
        [SerializeField] private string[] _fallbackIngredients = new string[]
        {
            "Chicken", "Beef", "Salmon", "Shrimp",
            "Lemon", "Lime", "Orange", "Mango",
            "Garlic", "Onion", "Ginger", "Basil",
            "Chocolate", "Vanilla", "Cinnamon", "Honey",
            "Butter", "Cream", "Cheese", "Olive Oil",
            "Chili", "Pepper", "Cumin", "Paprika"
        };

        // Cached flat list
        private List<IngredientData> _allIngredients;
        private bool _isCacheValid = false;

        private void OnValidate()
        {
            _isCacheValid = false;
        }

        #region Public Methods

        /// <summary>
        /// Get a random pair of ingredients
        /// </summary>
        public RoundIngredients GetRandomPair()
        {
            EnsureCache();

            if (_allIngredients == null || _allIngredients.Count < 2)
            {
                return GetFallbackPair();
            }

            // Pick first ingredient
            int idx1 = UnityEngine.Random.Range(0, _allIngredients.Count);
            IngredientData ingredient1 = _allIngredients[idx1];

            // Pick second ingredient (different category if required)
            IngredientData ingredient2 = null;
            int attempts = 0;
            int maxAttempts = 50;

            while (ingredient2 == null && attempts < maxAttempts)
            {
                int idx2 = UnityEngine.Random.Range(0, _allIngredients.Count);
                
                if (idx2 == idx1)
                {
                    attempts++;
                    continue;
                }

                IngredientData candidate = _allIngredients[idx2];

                // Check category pairing rules
                if (!_allowSameCategoryPairing && 
                    ingredient1.Category == candidate.Category)
                {
                    attempts++;
                    continue;
                }

                ingredient2 = candidate;
            }

            // Fallback if we couldn't find a valid pair
            if (ingredient2 == null)
            {
                int idx2 = (idx1 + 1) % _allIngredients.Count;
                ingredient2 = _allIngredients[idx2];
            }

            return new RoundIngredients
            {
                Ingredient1 = ingredient1.Name,
                Ingredient2 = ingredient2.Name,
                Ingredient1Icon = ingredient1.IconName,
                Ingredient2Icon = ingredient2.IconName,
                Ingredient1Sprite = ingredient1.Icon,
                Ingredient2Sprite = ingredient2.Icon
            };
        }

        /// <summary>
        /// Get all ingredients in a specific category
        /// </summary>
        public List<IngredientData> GetIngredientsByCategory(IngredientCategoryType category)
        {
            EnsureCache();

            List<IngredientData> result = new List<IngredientData>();
            foreach (var ingredient in _allIngredients)
            {
                if (ingredient.Category == category)
                {
                    result.Add(ingredient);
                }
            }
            return result;
        }

        /// <summary>
        /// Get a specific ingredient by name
        /// </summary>
        public IngredientData GetIngredientByName(string name)
        {
            EnsureCache();

            foreach (var ingredient in _allIngredients)
            {
                if (ingredient.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return ingredient;
                }
            }
            return null;
        }

        /// <summary>
        /// Get all available ingredients
        /// </summary>
        public List<IngredientData> GetAllIngredients()
        {
            EnsureCache();
            return new List<IngredientData>(_allIngredients);
        }

        /// <summary>
        /// Get total ingredient count
        /// </summary>
        public int GetIngredientCount()
        {
            EnsureCache();
            return _allIngredients?.Count ?? 0;
        }

        /// <summary>
        /// Get a random ingredient from a specific category
        /// </summary>
        public IngredientData GetRandomFromCategory(IngredientCategoryType category)
        {
            var categoryIngredients = GetIngredientsByCategory(category);
            
            if (categoryIngredients.Count == 0)
                return null;

            return categoryIngredients[UnityEngine.Random.Range(0, categoryIngredients.Count)];
        }

        #endregion

        #region Private Methods

        private void EnsureCache()
        {
            if (_isCacheValid && _allIngredients != null)
                return;

            _allIngredients = new List<IngredientData>();

            if (_categories != null)
            {
                foreach (var category in _categories)
                {
                    if (category.Ingredients != null)
                    {
                        foreach (var ingredient in category.Ingredients)
                        {
                            ingredient.Category = category.CategoryType;
                            _allIngredients.Add(ingredient);
                        }
                    }
                }
            }

            _isCacheValid = true;
        }

        private RoundIngredients GetFallbackPair()
        {
            if (_fallbackIngredients == null || _fallbackIngredients.Length < 2)
            {
                return new RoundIngredients("Mystery Ingredient 1", "Mystery Ingredient 2");
            }

            int idx1 = UnityEngine.Random.Range(0, _fallbackIngredients.Length);
            int idx2 = (idx1 + UnityEngine.Random.Range(1, _fallbackIngredients.Length - 1)) % _fallbackIngredients.Length;

            return new RoundIngredients(_fallbackIngredients[idx1], _fallbackIngredients[idx2]);
        }

        #endregion

        #region Editor Utilities

#if UNITY_EDITOR
        /// <summary>
        /// Populate with default ingredients (Editor only)
        /// </summary>
        [ContextMenu("Populate Default Ingredients")]
        private void PopulateDefaults()
        {
            _categories = new IngredientCategory[]
            {
                new IngredientCategory
                {
                    CategoryType = IngredientCategoryType.Proteins,
                    Ingredients = new IngredientData[]
                    {
                        new IngredientData { Name = "Chicken", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Beef", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Pork", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Salmon", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Shrimp", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Lobster", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Duck", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Wagyu Beef", Rarity = IngredientRarity.Legendary },
                        new IngredientData { Name = "Tofu", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Eggs", Rarity = IngredientRarity.Common }
                    }
                },
                new IngredientCategory
                {
                    CategoryType = IngredientCategoryType.Fruits,
                    Ingredients = new IngredientData[]
                    {
                        new IngredientData { Name = "Lemon", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Lime", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Orange", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Mango", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Pineapple", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Passion Fruit", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Dragon Fruit", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Truffle", Rarity = IngredientRarity.Legendary },
                        new IngredientData { Name = "Apple", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Strawberry", Rarity = IngredientRarity.Common }
                    }
                },
                new IngredientCategory
                {
                    CategoryType = IngredientCategoryType.Vegetables,
                    Ingredients = new IngredientData[]
                    {
                        new IngredientData { Name = "Garlic", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Onion", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Tomato", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Spinach", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Asparagus", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Artichoke", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Matsutake Mushroom", Rarity = IngredientRarity.Legendary },
                        new IngredientData { Name = "Bell Pepper", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Carrot", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Eggplant", Rarity = IngredientRarity.Uncommon }
                    }
                },
                new IngredientCategory
                {
                    CategoryType = IngredientCategoryType.Spices,
                    Ingredients = new IngredientData[]
                    {
                        new IngredientData { Name = "Chili", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Black Pepper", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Cumin", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Paprika", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Saffron", Rarity = IngredientRarity.Legendary },
                        new IngredientData { Name = "Cardamom", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Star Anise", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Cinnamon", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Ginger", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Turmeric", Rarity = IngredientRarity.Uncommon }
                    }
                },
                new IngredientCategory
                {
                    CategoryType = IngredientCategoryType.Dairy,
                    Ingredients = new IngredientData[]
                    {
                        new IngredientData { Name = "Butter", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Cream", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Cheese", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Parmesan", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Goat Cheese", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Burrata", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Aged Gruyère", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Mascarpone", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Yogurt", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Ricotta", Rarity = IngredientRarity.Common }
                    }
                },
                new IngredientCategory
                {
                    CategoryType = IngredientCategoryType.Sweets,
                    Ingredients = new IngredientData[]
                    {
                        new IngredientData { Name = "Dark Chocolate", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Honey", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Vanilla", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Maple Syrup", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Caramel", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "White Chocolate", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Matcha", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Lavender", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Rose Water", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Belgian Chocolate", Rarity = IngredientRarity.Legendary }
                    }
                },
                new IngredientCategory
                {
                    CategoryType = IngredientCategoryType.Herbs,
                    Ingredients = new IngredientData[]
                    {
                        new IngredientData { Name = "Basil", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Cilantro", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Mint", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Rosemary", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Thyme", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Oregano", Rarity = IngredientRarity.Common },
                        new IngredientData { Name = "Dill", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Tarragon", Rarity = IngredientRarity.Rare },
                        new IngredientData { Name = "Lemongrass", Rarity = IngredientRarity.Uncommon },
                        new IngredientData { Name = "Shiso", Rarity = IngredientRarity.Rare }
                    }
                }
            };

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("[IngredientDatabase] Populated with default ingredients");
        }

        /// <summary>
        /// Scan for sprites in Assets/Sprites/Ingredients/ and auto-assign them to matching ingredients
        /// </summary>
        [ContextMenu("Scan for Sprites")]
        private void ScanForSprites()
        {
            if (_categories == null || _categories.Length == 0)
            {
                Debug.LogWarning("[IngredientDatabase] No categories found. Please populate ingredients first.");
                return;
            }

            // Map category types to folder names
            Dictionary<IngredientCategoryType, string> categoryFolders = new Dictionary<IngredientCategoryType, string>
            {
                { IngredientCategoryType.Proteins, "Proteins" },
                { IngredientCategoryType.Fruits, "Fruits" },
                { IngredientCategoryType.Vegetables, "Vegetables" },
                { IngredientCategoryType.Spices, "Spices" },
                { IngredientCategoryType.Dairy, "Dairy" },
                { IngredientCategoryType.Sweets, "Sweets" },
                { IngredientCategoryType.Herbs, "Herbs" },
                { IngredientCategoryType.Grains, "Grains" },
                { IngredientCategoryType.Seafood, "Seafood" },
                { IngredientCategoryType.Other, "Other" }
            };

            int matchedCount = 0;
            int totalIngredients = 0;
            int totalSpritesFound = 0;

            foreach (var category in _categories)
            {
                if (category.Ingredients == null) continue;

                string folderName = categoryFolders.ContainsKey(category.CategoryType) 
                    ? categoryFolders[category.CategoryType] 
                    : category.CategoryType.ToString();

                string folderPath = $"Assets/Sprites/Ingredients/{folderName}";
                
                // Check if folder exists
                if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
                {
                    Debug.LogWarning($"[IngredientDatabase] Folder not found: {folderPath}");
                    continue;
                }
                
                // Find all texture assets (PNG files are imported as Texture2D)
                string[] textureGuids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
                
                // Create a dictionary of sprite names (normalized) to sprites
                Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();
                
                // Process each texture file
                foreach (string guid in textureGuids)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    
                    // Get the texture importer to check/change settings
                    UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
                    
                    if (importer != null)
                    {
                        // Check if it's already set as a sprite
                        bool needsReimport = false;
                        if (importer.textureType != UnityEditor.TextureImporterType.Sprite)
                        {
                            importer.textureType = UnityEditor.TextureImporterType.Sprite;
                            importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                            needsReimport = true;
                        }
                        
                        if (needsReimport)
                        {
                            UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
                        }
                    }
                    
                    // Now try to load as sprite
                    Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    
                    if (sprite != null)
                    {
                        string normalizedName = NormalizeName(sprite.name);
                        if (!spriteMap.ContainsKey(normalizedName))
                        {
                            spriteMap[normalizedName] = sprite;
                            totalSpritesFound++;
                        }
                    }
                    else
                    {
                        // Try loading all sprites from the asset (for sprite sheets)
                        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath)
                            .OfType<Sprite>()
                            .ToArray();
                        
                        foreach (var s in sprites)
                        {
                            if (s != null)
                            {
                                string normalizedName = NormalizeName(s.name);
                                if (!spriteMap.ContainsKey(normalizedName))
                                {
                                    spriteMap[normalizedName] = s;
                                    totalSpritesFound++;
                                }
                            }
                        }
                    }
                }

                // Debug: Log found sprites for this category
                if (spriteMap.Count > 0)
                {
                    Debug.Log($"[IngredientDatabase] Found {spriteMap.Count} sprites in {folderName}: {string.Join(", ", spriteMap.Keys)}");
                }

                // Match ingredients to sprites
                foreach (var ingredient in category.Ingredients)
                {
                    totalIngredients++;
                    string normalizedIngredientName = NormalizeName(ingredient.Name);
                    
                    if (spriteMap.ContainsKey(normalizedIngredientName))
                    {
                        ingredient.Icon = spriteMap[normalizedIngredientName];
                        ingredient.IconName = spriteMap[normalizedIngredientName].name;
                        matchedCount++;
                    }
                    else
                    {
                        // Try fuzzy matching - check if any sprite name contains the ingredient name or vice versa
                        foreach (var kvp in spriteMap)
                        {
                            if (kvp.Key.Contains(normalizedIngredientName) || normalizedIngredientName.Contains(kvp.Key))
                            {
                                ingredient.Icon = kvp.Value;
                                ingredient.IconName = kvp.Value.name;
                                matchedCount++;
                                Debug.Log($"[IngredientDatabase] Fuzzy matched: '{ingredient.Name}' -> '{kvp.Value.name}'");
                                break;
                            }
                        }
                    }
                }
            }

            // Find and report unmatched ingredients
            List<string> unmatchedIngredients = new List<string>();
            foreach (var category in _categories)
            {
                if (category.Ingredients == null) continue;
                
                string folderName = categoryFolders.ContainsKey(category.CategoryType) 
                    ? categoryFolders[category.CategoryType] 
                    : category.CategoryType.ToString();
                
                string folderPath = $"Assets/Sprites/Ingredients/{folderName}";
                
                if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
                {
                    foreach (var ingredient in category.Ingredients)
                    {
                        if (ingredient.Icon == null)
                        {
                            unmatchedIngredients.Add($"{ingredient.Name} (Category: {folderName} - Folder missing!)");
                        }
                    }
                    continue;
                }
                
                string[] textureGuids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
                HashSet<string> availableSpriteNames = new HashSet<string>();
                
                foreach (string guid in textureGuids)
                {
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    availableSpriteNames.Add(NormalizeName(fileName));
                }
                
                foreach (var ingredient in category.Ingredients)
                {
                    if (ingredient.Icon == null)
                    {
                        string normalizedName = NormalizeName(ingredient.Name);
                        string suggestion = "";
                        
                        // Find closest match
                        foreach (var spriteName in availableSpriteNames)
                        {
                            if (spriteName.Contains(normalizedName) || normalizedName.Contains(spriteName))
                            {
                                suggestion = $" (Found similar: {spriteName})";
                                break;
                            }
                        }
                        
                        unmatchedIngredients.Add($"{ingredient.Name} (Category: {folderName}{suggestion})");
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[IngredientDatabase] Scan complete! Matched {matchedCount} out of {totalIngredients} ingredients with sprites.");
            
            if (unmatchedIngredients.Count > 0)
            {
                Debug.LogWarning($"[IngredientDatabase] {unmatchedIngredients.Count} ingredient(s) without sprites:");
                foreach (var unmatched in unmatchedIngredients)
                {
                    Debug.LogWarning($"  - {unmatched}");
                }
            }
        }

        /// <summary>
        /// Normalize a name for matching (lowercase, remove spaces, remove file extensions, remove diacritics)
        /// </summary>
        private string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            
            // Remove file extension if present
            name = System.IO.Path.GetFileNameWithoutExtension(name);
            
            // Remove diacritics (accents) - convert è, é, ê, etc. to e
            name = RemoveDiacritics(name);
            
            // Convert to lowercase and remove spaces/special characters for matching
            return name.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "").Replace("'", "");
        }
        
        /// <summary>
        /// Remove diacritics (accents) from a string
        /// </summary>
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();
            
            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            
            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
#endif

        #endregion
    }

    /// <summary>
    /// Category of ingredients
    /// </summary>
    [Serializable]
    public class IngredientCategory
    {
        public IngredientCategoryType CategoryType;
        public IngredientData[] Ingredients;
    }

    /// <summary>
    /// Individual ingredient data
    /// </summary>
    [Serializable]
    public class IngredientData
    {
        public string Name;
        public string IconName;
        public Sprite Icon;  // Manually assigned ingredient sprite
        public IngredientRarity Rarity;
        public string Description;

        [NonSerialized]
        public IngredientCategoryType Category;

        /// <summary>
        /// Returns the sprite if assigned, otherwise null
        /// </summary>
        public Sprite GetSprite()
        {
            return Icon;
        }
    }

    /// <summary>
    /// Ingredient category types
    /// </summary>
    public enum IngredientCategoryType
    {
        Proteins,
        Vegetables,
        Fruits,
        Dairy,
        Spices,
        Herbs,
        Sweets,
        Grains,
        Seafood,
        Other
    }

    /// <summary>
    /// Ingredient rarity levels
    /// </summary>
    public enum IngredientRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
}

