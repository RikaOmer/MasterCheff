using UnityEngine;
using System;

namespace MasterCheff.Utils
{
    /// <summary>
    /// Mobile-specific utility functions
    /// </summary>
    public static class MobileUtils
    {
        #region Screen & Display

        /// <summary>
        /// Get the safe area for notched devices
        /// </summary>
        public static Rect GetSafeArea()
        {
            return Screen.safeArea;
        }

        /// <summary>
        /// Check if device has a notch
        /// </summary>
        public static bool HasNotch()
        {
            Rect safeArea = Screen.safeArea;
            return safeArea.y > 0 || 
                   safeArea.x > 0 || 
                   safeArea.width < Screen.width || 
                   safeArea.height < Screen.height;
        }

        /// <summary>
        /// Get screen aspect ratio
        /// </summary>
        public static float GetAspectRatio()
        {
            return (float)Screen.width / Screen.height;
        }

        /// <summary>
        /// Check if device is tablet
        /// </summary>
        public static bool IsTablet()
        {
            float aspectRatio = GetAspectRatio();
            float diagonalInches = GetScreenDiagonalInches();
            
            // Tablets typically have aspect ratio closer to 4:3 and larger screens
            return diagonalInches >= 7f || aspectRatio < 1.5f;
        }

        /// <summary>
        /// Get screen diagonal size in inches
        /// </summary>
        public static float GetScreenDiagonalInches()
        {
            float screenWidth = Screen.width / Screen.dpi;
            float screenHeight = Screen.height / Screen.dpi;
            return Mathf.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);
        }

        /// <summary>
        /// Get optimal UI scale based on screen size
        /// </summary>
        public static float GetOptimalUIScale()
        {
            float dpi = Screen.dpi;
            if (dpi <= 0) dpi = 160f; // Default fallback

            float baseScale = dpi / 160f; // 160 is baseline DPI
            return Mathf.Clamp(baseScale, 0.75f, 2f);
        }

        #endregion

        #region Device Info

        /// <summary>
        /// Get device unique identifier
        /// </summary>
        public static string GetDeviceId()
        {
            return SystemInfo.deviceUniqueIdentifier;
        }

        /// <summary>
        /// Get device model
        /// </summary>
        public static string GetDeviceModel()
        {
            return SystemInfo.deviceModel;
        }

        /// <summary>
        /// Get operating system
        /// </summary>
        public static string GetOperatingSystem()
        {
            return SystemInfo.operatingSystem;
        }

        /// <summary>
        /// Check if running on Android
        /// </summary>
        public static bool IsAndroid()
        {
#if UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Check if running on iOS
        /// </summary>
        public static bool IsiOS()
        {
#if UNITY_IOS
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Get available memory in MB
        /// </summary>
        public static int GetAvailableMemoryMB()
        {
            return SystemInfo.systemMemorySize;
        }

        /// <summary>
        /// Check if device is low-end based on memory
        /// </summary>
        public static bool IsLowEndDevice()
        {
            return GetAvailableMemoryMB() < 2048; // Less than 2GB RAM
        }

        #endregion

        #region Battery & Performance

        /// <summary>
        /// Get battery level (0-1)
        /// </summary>
        public static float GetBatteryLevel()
        {
            return SystemInfo.batteryLevel;
        }

        /// <summary>
        /// Get battery status
        /// </summary>
        public static BatteryStatus GetBatteryStatus()
        {
            return SystemInfo.batteryStatus;
        }

        /// <summary>
        /// Check if device is charging
        /// </summary>
        public static bool IsCharging()
        {
            return SystemInfo.batteryStatus == BatteryStatus.Charging;
        }

        /// <summary>
        /// Check if battery is low
        /// </summary>
        public static bool IsBatteryLow()
        {
            return SystemInfo.batteryLevel < 0.2f && SystemInfo.batteryLevel >= 0f;
        }

        /// <summary>
        /// Set quality based on device capabilities
        /// </summary>
        public static void SetQualityForDevice()
        {
            int memoryMB = GetAvailableMemoryMB();

            if (memoryMB >= 4096)
            {
                QualitySettings.SetQualityLevel(5); // Ultra
            }
            else if (memoryMB >= 3072)
            {
                QualitySettings.SetQualityLevel(4); // Very High
            }
            else if (memoryMB >= 2048)
            {
                QualitySettings.SetQualityLevel(3); // High
            }
            else if (memoryMB >= 1024)
            {
                QualitySettings.SetQualityLevel(2); // Medium
            }
            else
            {
                QualitySettings.SetQualityLevel(1); // Low
            }
        }

        #endregion

        #region Haptics

        /// <summary>
        /// Trigger vibration
        /// </summary>
        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// Trigger light haptic feedback (iOS only with native plugin)
        /// </summary>
        public static void LightHaptic()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android - short vibration would need AndroidJavaObject
            Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS - would need native plugin for taptic engine
            Handheld.Vibrate();
#endif
        }

        #endregion

        #region Network

        /// <summary>
        /// Check if device has internet connection
        /// </summary>
        public static bool HasInternetConnection()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        /// <summary>
        /// Check if connected via WiFi
        /// </summary>
        public static bool IsOnWiFi()
        {
            return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
        }

        /// <summary>
        /// Check if connected via mobile data
        /// </summary>
        public static bool IsOnMobileData()
        {
            return Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork;
        }

        #endregion

        #region Permissions

        /// <summary>
        /// Check if app has camera permission
        /// </summary>
        public static bool HasCameraPermission()
        {
#if UNITY_ANDROID
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
#else
            return true;
#endif
        }

        /// <summary>
        /// Request camera permission
        /// </summary>
        public static void RequestCameraPermission()
        {
#if UNITY_ANDROID
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
#endif
        }

        #endregion

        #region App Management

        /// <summary>
        /// Open app settings
        /// </summary>
        public static void OpenAppSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (var intent = new AndroidJavaObject("android.content.Intent", 
                        "android.settings.APPLICATION_DETAILS_SETTINGS"))
                    {
                        string packageName = Application.identifier;
                        using (var uri = new AndroidJavaClass("android.net.Uri")
                            .CallStatic<AndroidJavaObject>("parse", "package:" + packageName))
                        {
                            intent.Call<AndroidJavaObject>("setData", uri);
                            activity.Call("startActivity", intent);
                        }
                    }
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Application.OpenURL("app-settings:");
#endif
        }

        /// <summary>
        /// Open URL in browser
        /// </summary>
        public static void OpenURL(string url)
        {
            Application.OpenURL(url);
        }

        /// <summary>
        /// Share text/URL
        /// </summary>
        public static void Share(string text, string subject = "")
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intentObject = new AndroidJavaObject("android.content.Intent"))
            {
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Share via"))
                {
                    activity.Call("startActivity", chooser);
                }
            }
#else
            Debug.Log($"Share: {text}");
#endif
        }

        #endregion
    }
}


