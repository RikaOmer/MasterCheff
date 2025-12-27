using UnityEngine;
using System.Collections.Generic;

namespace MasterCheff.Utils
{
    /// <summary>
    /// Extension methods for common Unity types
    /// </summary>
    public static class Extensions
    {
        #region Vector Extensions

        /// <summary>
        /// Set X component of Vector3
        /// </summary>
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);

        /// <summary>
        /// Set Y component of Vector3
        /// </summary>
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);

        /// <summary>
        /// Set Z component of Vector3
        /// </summary>
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        /// <summary>
        /// Set X component of Vector2
        /// </summary>
        public static Vector2 WithX(this Vector2 v, float x) => new Vector2(x, v.y);

        /// <summary>
        /// Set Y component of Vector2
        /// </summary>
        public static Vector2 WithY(this Vector2 v, float y) => new Vector2(v.x, y);

        /// <summary>
        /// Convert Vector3 to Vector2 (XY)
        /// </summary>
        public static Vector2 ToVector2XY(this Vector3 v) => new Vector2(v.x, v.y);

        /// <summary>
        /// Convert Vector3 to Vector2 (XZ)
        /// </summary>
        public static Vector2 ToVector2XZ(this Vector3 v) => new Vector2(v.x, v.z);

        /// <summary>
        /// Convert Vector2 to Vector3 with Z = 0
        /// </summary>
        public static Vector3 ToVector3(this Vector2 v, float z = 0f) => new Vector3(v.x, v.y, z);

        /// <summary>
        /// Flatten Y to 0 (useful for 3D ground movement)
        /// </summary>
        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>
        /// Get random point within radius
        /// </summary>
        public static Vector3 RandomPointInRadius(this Vector3 center, float radius)
        {
            return center + Random.insideUnitSphere * radius;
        }

        /// <summary>
        /// Get random point within radius (2D)
        /// </summary>
        public static Vector2 RandomPointInRadius(this Vector2 center, float radius)
        {
            return center + Random.insideUnitCircle * radius;
        }

        #endregion

        #region Transform Extensions

        /// <summary>
        /// Set X position
        /// </summary>
        public static void SetPositionX(this Transform transform, float x)
        {
            transform.position = transform.position.WithX(x);
        }

        /// <summary>
        /// Set Y position
        /// </summary>
        public static void SetPositionY(this Transform transform, float y)
        {
            transform.position = transform.position.WithY(y);
        }

        /// <summary>
        /// Set Z position
        /// </summary>
        public static void SetPositionZ(this Transform transform, float z)
        {
            transform.position = transform.position.WithZ(z);
        }

        /// <summary>
        /// Set local X position
        /// </summary>
        public static void SetLocalPositionX(this Transform transform, float x)
        {
            transform.localPosition = transform.localPosition.WithX(x);
        }

        /// <summary>
        /// Set local Y position
        /// </summary>
        public static void SetLocalPositionY(this Transform transform, float y)
        {
            transform.localPosition = transform.localPosition.WithY(y);
        }

        /// <summary>
        /// Set local Z position
        /// </summary>
        public static void SetLocalPositionZ(this Transform transform, float z)
        {
            transform.localPosition = transform.localPosition.WithZ(z);
        }

        /// <summary>
        /// Reset transform to default values
        /// </summary>
        public static void Reset(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Destroy all children
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Get all children
        /// </summary>
        public static List<Transform> GetChildren(this Transform transform)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }
            return children;
        }

        #endregion

        #region Color Extensions

        /// <summary>
        /// Set alpha of color
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Convert hex string to Color
        /// </summary>
        public static Color HexToColor(string hex)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            
            float r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
            float g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
            float b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
            float a = hex.Length >= 8 ? int.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) / 255f : 1f;
            
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Convert Color to hex string
        /// </summary>
        public static string ToHex(this Color color, bool includeAlpha = false)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            if (includeAlpha)
            {
                hex = ColorUtility.ToHtmlStringRGBA(color);
            }
            return "#" + hex;
        }

        #endregion

        #region List Extensions

        /// <summary>
        /// Get random element from list
        /// </summary>
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Shuffle list in place
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Check if list is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        #endregion

        #region RectTransform Extensions

        /// <summary>
        /// Set anchor to corners
        /// </summary>
        public static void SetAnchorsToCorners(this RectTransform rectTransform)
        {
            RectTransform parent = rectTransform.parent as RectTransform;
            if (parent == null) return;

            Vector2 newAnchorsMin = new Vector2(
                rectTransform.anchorMin.x + rectTransform.offsetMin.x / parent.rect.width,
                rectTransform.anchorMin.y + rectTransform.offsetMin.y / parent.rect.height
            );
            Vector2 newAnchorsMax = new Vector2(
                rectTransform.anchorMax.x + rectTransform.offsetMax.x / parent.rect.width,
                rectTransform.anchorMax.y + rectTransform.offsetMax.y / parent.rect.height
            );

            rectTransform.anchorMin = newAnchorsMin;
            rectTransform.anchorMax = newAnchorsMax;
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Set size from bottom left
        /// </summary>
        public static void SetSize(this RectTransform rectTransform, Vector2 size)
        {
            Vector2 oldSize = rectTransform.rect.size;
            Vector2 deltaSize = size - oldSize;
            rectTransform.offsetMin -= new Vector2(deltaSize.x * rectTransform.pivot.x, deltaSize.y * rectTransform.pivot.y);
            rectTransform.offsetMax += new Vector2(deltaSize.x * (1f - rectTransform.pivot.x), deltaSize.y * (1f - rectTransform.pivot.y));
        }

        #endregion

        #region Component Extensions

        /// <summary>
        /// Get or add component
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Check if GameObject has component
        /// </summary>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }

        #endregion

        #region String Extensions

        /// <summary>
        /// Check if string is null or empty
        /// </summary>
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// Check if string is null, empty, or whitespace
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// Truncate string to max length
        /// </summary>
        public static string Truncate(this string str, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength) return str;
            return str.Substring(0, maxLength - suffix.Length) + suffix;
        }

        #endregion

        #region Float Extensions

        /// <summary>
        /// Remap value from one range to another
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// Check if value is approximately equal
        /// </summary>
        public static bool Approximately(this float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

        #endregion
    }
}

