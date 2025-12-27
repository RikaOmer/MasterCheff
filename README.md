# MasterCheff - Unity Mobile Game Infrastructure

תשתית מקצועית למשחק מובייל ב-Unity עם כל המערכות הבסיסיות הנדרשות.

## 📁 מבנה הפרויקט

```
Assets/
├── Audio/              # קבצי אודיו (מוזיקה ואפקטים)
├── Prefabs/            # Prefabs מוכנים לשימוש
├── Resources/          # משאבים שנטענים דינמית
├── Scenes/             # סצנות המשחק
├── Scripts/
│   ├── Core/           # מחלקות בסיס
│   │   ├── Singleton.cs
│   │   ├── GameState.cs
│   │   ├── GameBootstrapper.cs
│   │   └── Constants.cs
│   ├── Data/           # מבני נתונים
│   │   └── GameSaveData.cs
│   ├── Input/          # טיפול בקלט
│   │   └── TouchInputHandler.cs
│   ├── Managers/       # מנהלי מערכות
│   │   ├── GameManager.cs
│   │   ├── AudioManager.cs
│   │   ├── UIManager.cs
│   │   ├── SaveManager.cs
│   │   ├── EventManager.cs
│   │   └── SceneLoader.cs
│   ├── UI/             # רכיבי ממשק משתמש
│   │   ├── UIPanel.cs
│   │   ├── UIPopup.cs
│   │   ├── UIButton.cs
│   │   └── LoadingScreen.cs
│   └── Utils/          # כלי עזר
│       ├── ObjectPool.cs
│       ├── MobileUtils.cs
│       ├── Extensions.cs
│       ├── Timer.cs
│       └── SafeAreaHandler.cs
└── Sprites/            # תמונות וגרפיקה
```

## 🎮 מערכות עיקריות

### GameManager
מנהל המשחק הראשי - שולט בזרימת המשחק ומצביו.

```csharp
// שימוש בסיסי
GameManager.Instance.StartGame();
GameManager.Instance.PauseGame();
GameManager.Instance.AddScore(100);

// האזנה לאירועים
GameManager.Instance.OnGameStateChanged += (state) => { /* ... */ };
```

### AudioManager
מנהל אודיו - מוזיקה, אפקטים קוליים ושליטה בווליום.

```csharp
// השמעת מוזיקה
AudioManager.Instance.PlayMusic(musicClip);

// השמעת אפקט
AudioManager.Instance.PlaySFX(sfxClip);

// שליטה בווליום
AudioManager.Instance.SetMusicVolume(0.8f);
```

### UIManager
מנהל ממשק משתמש - פאנלים, פופאפים וניווט.

```csharp
// הצגת פאנל
UIManager.Instance.ShowPanel("MainMenu");

// הצגת פופאפ
UIManager.Instance.ShowPopup("Settings");

// חזרה אחורה
UIManager.Instance.GoBack();
```

### SaveManager
מנהל שמירה וטעינה - שמירת התקדמות והגדרות.

```csharp
// שמירה
SaveManager.Instance.SaveGame();

// טעינה
SaveManager.Instance.LoadGame();

// גישה לנתונים
var data = SaveManager.Instance.CurrentSaveData;
```

### EventManager
מערכת אירועים מרכזית - תקשורת מנותקת בין רכיבים.

```csharp
// הרשמה לאירוע
EventManager.Instance.Subscribe(GameEvents.PLAYER_SCORE, OnPlayerScore);

// שליחת אירוע
EventManager.Instance.Trigger(GameEvents.PLAYER_SCORE);
```

### TouchInputHandler
טיפול בקלט מגע - נגיעות, החלקות, צביטה ועוד.

```csharp
// האזנה לנגיעה
TouchInputHandler.Instance.OnTap += (pos) => { /* ... */ };

// האזנה להחלקה
TouchInputHandler.Instance.OnSwipe += (dir, pos) => { /* ... */ };
```

### SceneLoader
טעינת סצנות עם מסך טעינה ואנימציות מעבר.

```csharp
// טעינת סצנה
SceneLoader.Instance.LoadScene("Gameplay");

// טעינה אסינכרונית
SceneLoader.Instance.LoadSceneAsync("Level2", () => Debug.Log("Loaded!"));
```

### ObjectPool
מערכת Pool לאובייקטים - שיפור ביצועים.

```csharp
// הוספת Pool
ObjectPool.Instance.RegisterPool("Bullet", bulletPrefab, 20);

// יצירת אובייקט
GameObject bullet = ObjectPool.Instance.Spawn("Bullet", position, rotation);

// החזרת אובייקט
ObjectPool.Instance.Despawn("Bullet", bullet);
```

## 📱 תמיכה במובייל

### Safe Area
```csharp
// הוסף את SafeAreaHandler לRectTransform שצריך להתחשב בNotch
```

### כלי עזר למובייל
```csharp
// בדיקת חיבור לאינטרנט
if (MobileUtils.HasInternetConnection()) { }

// רטט
MobileUtils.Vibrate();

// בדיקת מכשיר חלש
if (MobileUtils.IsLowEndDevice()) { }

// התאמת איכות אוטומטית
MobileUtils.SetQualityForDevice();
```

## 🚀 התחלה מהירה

1. **יצירת סצנת Bootstrap**
   - צור סצנה חדשה בשם "Bootstrap"
   - הוסף GameObject עם `GameBootstrapper`
   - הגדר אותה כסצנה ראשונה ב-Build Settings

2. **הגדרת Canvas**
   - צור Canvas ראשי
   - הוסף את `SafeAreaHandler` לקונטיינר הראשי
   - הוסף את `UIManager` ל-Canvas

3. **יצירת פאנלים**
   - צור פאנלים שיורשים מ-`UIPanel`
   - רשום אותם ב-UIManager

4. **הגדרת אודיו**
   - הוסף AudioClips לפרויקט
   - השתמש ב-AudioManager להשמעה

## 📋 דרישות

- Unity 2022.3 LTS ומעלה
- TextMeshPro (כלול ב-Unity)

## 🔧 הגדרות מומלצות

### Build Settings
- Platform: Android / iOS
- Graphics API: OpenGLES3 / Metal
- Scripting Backend: IL2CPP

### Quality Settings
- VSync: Don't Sync
- Target Frame Rate: 60

### Player Settings
- Allow Rotation: As needed
- Status Bar: Hidden
- Rendering: Auto Graphics API

## 📝 הערות

- כל המחלקות משתמשות ב-Namespace `MasterCheff`
- Singleton מבטיח מופע יחיד ושרידות בין סצנות
- EventManager מאפשר תקשורת ללא תלויות ישירות
- ObjectPool משפר ביצועים משמעותית בפרויקטים עם הרבה אובייקטים

## 📄 רישיון

MIT License - חופשי לשימוש מסחרי ואישי

