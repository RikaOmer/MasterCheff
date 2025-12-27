using UnityEngine;
using System;
using System.Collections.Generic;
using MasterCheff.Core;

namespace MasterCheff.Input
{
    /// <summary>
    /// Touch Input Handler for mobile games
    /// Handles taps, swipes, pinch, and multi-touch
    /// </summary>
    public class TouchInputHandler : Singleton<TouchInputHandler>
    {
        [Header("Tap Settings")]
        [SerializeField] private float _tapThreshold = 0.2f;
        [SerializeField] private float _doubleTapThreshold = 0.3f;
        [SerializeField] private float _tapMaxDistance = 50f;

        [Header("Swipe Settings")]
        [SerializeField] private float _swipeMinDistance = 50f;
        [SerializeField] private float _swipeMaxTime = 0.5f;

        [Header("Hold Settings")]
        [SerializeField] private float _holdThreshold = 0.5f;

        [Header("Pinch Settings")]
        [SerializeField] private float _pinchThreshold = 0.01f;

        // Touch state
        private Dictionary<int, TouchData> _activeTouches = new Dictionary<int, TouchData>();
        private float _lastTapTime;
        private Vector2 _lastTapPosition;

        // Pinch state
        private float _initialPinchDistance;
        private float _lastPinchDistance;

        // Events
        public event Action<Vector2> OnTap;
        public event Action<Vector2> OnDoubleTap;
        public event Action<Vector2> OnHoldStart;
        public event Action<Vector2> OnHoldEnd;
        public event Action<SwipeDirection, Vector2> OnSwipe;
        public event Action<float> OnPinch; // delta scale
        public event Action<Vector2, Vector2> OnDrag; // position, delta
        public event Action<int, Vector2> OnTouchBegin;
        public event Action<int, Vector2> OnTouchEnd;

        // Properties
        public int TouchCount => UnityEngine.Input.touchCount;
        public bool IsTouching => UnityEngine.Input.touchCount > 0;
        public Vector2 LastTapPosition => _lastTapPosition;

        public enum SwipeDirection
        {
            None,
            Up,
            Down,
            Left,
            Right
        }

        private class TouchData
        {
            public int fingerId;
            public Vector2 startPosition;
            public Vector2 currentPosition;
            public float startTime;
            public bool isHolding;
            public bool hasMoved;
        }

        private void Update()
        {
            HandleTouches();
            HandleMouseInput(); // For editor testing
        }

        private void HandleTouches()
        {
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnTouchBegan(touch);
                        break;

                    case TouchPhase.Moved:
                        OnTouchMoved(touch);
                        break;

                    case TouchPhase.Stationary:
                        OnTouchStationary(touch);
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        OnTouchEnded(touch);
                        break;
                }
            }

            // Handle pinch zoom
            if (UnityEngine.Input.touchCount == 2)
            {
                HandlePinch();
            }
        }

        private void OnTouchBegan(Touch touch)
        {
            TouchData data = new TouchData
            {
                fingerId = touch.fingerId,
                startPosition = touch.position,
                currentPosition = touch.position,
                startTime = Time.time,
                isHolding = false,
                hasMoved = false
            };

            _activeTouches[touch.fingerId] = data;
            OnTouchBegin?.Invoke(touch.fingerId, touch.position);
        }

        private void OnTouchMoved(Touch touch)
        {
            if (!_activeTouches.TryGetValue(touch.fingerId, out TouchData data)) return;

            data.currentPosition = touch.position;

            float distance = Vector2.Distance(data.startPosition, touch.position);
            if (distance > _tapMaxDistance)
            {
                data.hasMoved = true;
            }

            // Trigger drag event
            OnDrag?.Invoke(touch.position, touch.deltaPosition);
        }

        private void OnTouchStationary(Touch touch)
        {
            if (!_activeTouches.TryGetValue(touch.fingerId, out TouchData data)) return;

            // Check for hold
            if (!data.isHolding && !data.hasMoved)
            {
                float holdTime = Time.time - data.startTime;
                if (holdTime >= _holdThreshold)
                {
                    data.isHolding = true;
                    OnHoldStart?.Invoke(touch.position);
                }
            }
        }

        private void OnTouchEnded(Touch touch)
        {
            if (!_activeTouches.TryGetValue(touch.fingerId, out TouchData data))
            {
                OnTouchEnd?.Invoke(touch.fingerId, touch.position);
                return;
            }

            float duration = Time.time - data.startTime;
            float distance = Vector2.Distance(data.startPosition, touch.position);

            // Check for hold end
            if (data.isHolding)
            {
                OnHoldEnd?.Invoke(touch.position);
            }
            // Check for tap
            else if (!data.hasMoved && duration < _tapThreshold && distance < _tapMaxDistance)
            {
                HandleTap(touch.position);
            }
            // Check for swipe
            else if (duration < _swipeMaxTime && distance > _swipeMinDistance)
            {
                SwipeDirection direction = GetSwipeDirection(data.startPosition, touch.position);
                OnSwipe?.Invoke(direction, touch.position);
            }

            _activeTouches.Remove(touch.fingerId);
            OnTouchEnd?.Invoke(touch.fingerId, touch.position);
        }

        private void HandleTap(Vector2 position)
        {
            float timeSinceLastTap = Time.time - _lastTapTime;
            float distanceFromLastTap = Vector2.Distance(position, _lastTapPosition);

            if (timeSinceLastTap < _doubleTapThreshold && distanceFromLastTap < _tapMaxDistance)
            {
                OnDoubleTap?.Invoke(position);
                _lastTapTime = 0f; // Reset to prevent triple tap detection
            }
            else
            {
                OnTap?.Invoke(position);
                _lastTapTime = Time.time;
                _lastTapPosition = position;
            }
        }

        private void HandlePinch()
        {
            Touch touch0 = UnityEngine.Input.GetTouch(0);
            Touch touch1 = UnityEngine.Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                _initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                _lastPinchDistance = _initialPinchDistance;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                float deltaScale = (currentDistance - _lastPinchDistance) / Screen.height;

                if (Mathf.Abs(deltaScale) > _pinchThreshold)
                {
                    OnPinch?.Invoke(deltaScale);
                    _lastPinchDistance = currentDistance;
                }
            }
        }

        private SwipeDirection GetSwipeDirection(Vector2 start, Vector2 end)
        {
            Vector2 direction = end - start;
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            if (absX > absY)
            {
                return direction.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                return direction.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }
        }

        #region Mouse Input (Editor Testing)

        private Vector2 _mouseStartPosition;
        private float _mouseDownTime;
        private bool _isMouseDown;
        private bool _mouseHasMoved;
        private bool _isMouseHolding;

        private void HandleMouseInput()
        {
#if UNITY_EDITOR
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _mouseStartPosition = UnityEngine.Input.mousePosition;
                _mouseDownTime = Time.time;
                _isMouseDown = true;
                _mouseHasMoved = false;
                _isMouseHolding = false;
                OnTouchBegin?.Invoke(0, _mouseStartPosition);
            }
            else if (UnityEngine.Input.GetMouseButton(0) && _isMouseDown)
            {
                Vector2 currentPos = UnityEngine.Input.mousePosition;
                Vector2 delta = currentPos - (Vector2)UnityEngine.Input.mousePosition;

                float distance = Vector2.Distance(_mouseStartPosition, currentPos);
                if (distance > _tapMaxDistance)
                {
                    _mouseHasMoved = true;
                }

                // Check for hold
                if (!_isMouseHolding && !_mouseHasMoved)
                {
                    float holdTime = Time.time - _mouseDownTime;
                    if (holdTime >= _holdThreshold)
                    {
                        _isMouseHolding = true;
                        OnHoldStart?.Invoke(currentPos);
                    }
                }
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _isMouseDown)
            {
                Vector2 endPosition = UnityEngine.Input.mousePosition;
                float duration = Time.time - _mouseDownTime;
                float distance = Vector2.Distance(_mouseStartPosition, endPosition);

                if (_isMouseHolding)
                {
                    OnHoldEnd?.Invoke(endPosition);
                }
                else if (!_mouseHasMoved && duration < _tapThreshold && distance < _tapMaxDistance)
                {
                    HandleTap(endPosition);
                }
                else if (duration < _swipeMaxTime && distance > _swipeMinDistance)
                {
                    SwipeDirection direction = GetSwipeDirection(_mouseStartPosition, endPosition);
                    OnSwipe?.Invoke(direction, endPosition);
                }

                _isMouseDown = false;
                OnTouchEnd?.Invoke(0, endPosition);
            }
#endif
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Convert screen position to world position (2D)
        /// </summary>
        public Vector2 ScreenToWorld2D(Vector2 screenPosition)
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector2.zero;
            return cam.ScreenToWorldPoint(screenPosition);
        }

        /// <summary>
        /// Convert screen position to world position (3D)
        /// </summary>
        public Vector3 ScreenToWorld3D(Vector2 screenPosition, float distance = 10f)
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;
            Vector3 screenPos = new Vector3(screenPosition.x, screenPosition.y, distance);
            return cam.ScreenToWorldPoint(screenPos);
        }

        /// <summary>
        /// Raycast from touch position
        /// </summary>
        public bool RaycastFromTouch(Vector2 screenPosition, out RaycastHit hit, float maxDistance = 100f, LayerMask layerMask = default)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                hit = default;
                return false;
            }

            Ray ray = cam.ScreenPointToRay(screenPosition);
            return Physics.Raycast(ray, out hit, maxDistance, layerMask);
        }

        /// <summary>
        /// Raycast from touch position (2D)
        /// </summary>
        public RaycastHit2D RaycastFromTouch2D(Vector2 screenPosition, LayerMask layerMask = default)
        {
            Camera cam = Camera.main;
            if (cam == null) return default;

            Vector2 worldPos = cam.ScreenToWorldPoint(screenPosition);
            return Physics2D.Raycast(worldPos, Vector2.zero, 0f, layerMask);
        }

        #endregion
    }
}

