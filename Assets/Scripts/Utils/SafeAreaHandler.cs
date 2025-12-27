using UnityEngine;

namespace MasterCheff.Utils
{
    /// <summary>
    /// Handles safe area for notched devices (iPhone X, etc.)
    /// Attach to a RectTransform that should respect safe area
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _conformX = true;
        [SerializeField] private bool _conformY = true;
        [SerializeField] private bool _updateOnOrientationChange = true;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (_updateOnOrientationChange)
            {
                if (_lastSafeArea != Screen.safeArea || _lastOrientation != Screen.orientation)
                {
                    ApplySafeArea();
                }
            }
        }

        public void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;

            if (safeArea == _lastSafeArea && Screen.orientation == _lastOrientation)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastOrientation = Screen.orientation;

            // Get canvas for proper calculation
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Get the root canvas
            Canvas rootCanvas = canvas.rootCanvas;

            // Calculate anchor values based on safe area
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Apply based on conform settings
            if (!_conformX)
            {
                anchorMin.x = _rectTransform.anchorMin.x;
                anchorMax.x = _rectTransform.anchorMax.x;
            }

            if (!_conformY)
            {
                anchorMin.y = _rectTransform.anchorMin.y;
                anchorMax.y = _rectTransform.anchorMax.y;
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;

            Debug.Log($"[SafeAreaHandler] Applied safe area: {safeArea}");
        }

        /// <summary>
        /// Force refresh of safe area
        /// </summary>
        public void Refresh()
        {
            _lastSafeArea = Rect.zero;
            ApplySafeArea();
        }
    }
}

