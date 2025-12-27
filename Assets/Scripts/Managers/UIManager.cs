using UnityEngine;
using System;
using System.Collections.Generic;
using MasterCheff.Core;
using MasterCheff.UI;

namespace MasterCheff.Managers
{
    /// <summary>
    /// UI Manager - Handles UI panels, popups, and screen management
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        [Header("References")]
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Transform _panelContainer;
        [SerializeField] private Transform _popupContainer;
        [SerializeField] private Transform _overlayContainer;

        [Header("Settings")]
        [SerializeField] private bool _closePopupOnBackButton = true;

        // Panel management
        private Dictionary<string, UIPanel> _panels = new Dictionary<string, UIPanel>();
        private Stack<UIPanel> _panelStack = new Stack<UIPanel>();
        private Stack<UIPopup> _popupStack = new Stack<UIPopup>();

        // Events
        public event Action<UIPanel> OnPanelOpened;
        public event Action<UIPanel> OnPanelClosed;
        public event Action<UIPopup> OnPopupOpened;
        public event Action<UIPopup> OnPopupClosed;

        // Properties
        public UIPanel CurrentPanel => _panelStack.Count > 0 ? _panelStack.Peek() : null;
        public UIPopup CurrentPopup => _popupStack.Count > 0 ? _popupStack.Peek() : null;
        public bool HasOpenPopup => _popupStack.Count > 0;
        public Canvas MainCanvas => _mainCanvas;

        protected override void OnSingletonAwake()
        {
            if (_mainCanvas == null)
            {
                // Unity 2022.3+ uses FindFirstObjectByType instead of deprecated FindObjectOfType
                _mainCanvas = FindFirstObjectByType<Canvas>();
            }

            CreateContainers();
            RegisterExistingPanels();
            Debug.Log("[UIManager] Initialized");
        }

        private void Update()
        {
            // Handle back button (Android)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackButton();
            }
        }

        private void CreateContainers()
        {
            if (_panelContainer == null)
            {
                GameObject panelObj = new GameObject("Panels");
                panelObj.transform.SetParent(_mainCanvas.transform, false);
                _panelContainer = panelObj.transform;
            }

            if (_popupContainer == null)
            {
                GameObject popupObj = new GameObject("Popups");
                popupObj.transform.SetParent(_mainCanvas.transform, false);
                _popupContainer = popupObj.transform;
            }

            if (_overlayContainer == null)
            {
                GameObject overlayObj = new GameObject("Overlays");
                overlayObj.transform.SetParent(_mainCanvas.transform, false);
                _overlayContainer = overlayObj.transform;
            }
        }

        private void RegisterExistingPanels()
        {
            // Unity 2022.3+ uses FindObjectsByType instead of deprecated FindObjectsOfType
            UIPanel[] panels = FindObjectsByType<UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (UIPanel panel in panels)
            {
                RegisterPanel(panel);
            }
        }

        #region Panel Management

        public void RegisterPanel(UIPanel panel)
        {
            if (panel == null) return;

            string panelName = panel.PanelName;
            if (!_panels.ContainsKey(panelName))
            {
                _panels[panelName] = panel;
            }
        }

        public void UnregisterPanel(UIPanel panel)
        {
            if (panel == null) return;
            _panels.Remove(panel.PanelName);
        }

        public T GetPanel<T>() where T : UIPanel
        {
            foreach (var panel in _panels.Values)
            {
                if (panel is T typedPanel)
                {
                    return typedPanel;
                }
            }
            return null;
        }

        public UIPanel GetPanel(string panelName)
        {
            return _panels.TryGetValue(panelName, out UIPanel panel) ? panel : null;
        }

        public void ShowPanel(string panelName, bool hideCurrent = true)
        {
            if (!_panels.TryGetValue(panelName, out UIPanel panel))
            {
                Debug.LogWarning($"[UIManager] Panel not found: {panelName}");
                return;
            }

            ShowPanel(panel, hideCurrent);
        }

        public void ShowPanel(UIPanel panel, bool hideCurrent = true)
        {
            if (panel == null) return;

            // Hide current panel
            if (hideCurrent && CurrentPanel != null && CurrentPanel != panel)
            {
                CurrentPanel.Hide();
            }

            // Show new panel
            _panelStack.Push(panel);
            panel.Show();
            OnPanelOpened?.Invoke(panel);
        }

        public void HidePanel(UIPanel panel)
        {
            if (panel == null) return;

            panel.Hide();
            OnPanelClosed?.Invoke(panel);
        }

        public void HideCurrentPanel()
        {
            if (CurrentPanel != null)
            {
                UIPanel panel = _panelStack.Pop();
                panel.Hide();
                OnPanelClosed?.Invoke(panel);
            }
        }

        public void GoBack()
        {
            if (_panelStack.Count > 1)
            {
                HideCurrentPanel();
                CurrentPanel?.Show();
            }
        }

        #endregion

        #region Popup Management

        public void ShowPopup(UIPopup popup)
        {
            if (popup == null) return;

            popup.transform.SetParent(_popupContainer, false);
            _popupStack.Push(popup);
            popup.Show();
            OnPopupOpened?.Invoke(popup);
        }

        public void ShowPopup(string popupPrefabName)
        {
            GameObject prefab = Resources.Load<GameObject>($"Popups/{popupPrefabName}");
            if (prefab == null)
            {
                Debug.LogWarning($"[UIManager] Popup prefab not found: {popupPrefabName}");
                return;
            }

            GameObject popupObj = Instantiate(prefab, _popupContainer);
            UIPopup popup = popupObj.GetComponent<UIPopup>();
            if (popup != null)
            {
                ShowPopup(popup);
            }
        }

        public void ClosePopup(UIPopup popup)
        {
            if (popup == null) return;

            popup.Hide();
            OnPopupClosed?.Invoke(popup);

            // Remove from stack
            Stack<UIPopup> temp = new Stack<UIPopup>();
            while (_popupStack.Count > 0)
            {
                UIPopup p = _popupStack.Pop();
                if (p != popup)
                {
                    temp.Push(p);
                }
            }
            while (temp.Count > 0)
            {
                _popupStack.Push(temp.Pop());
            }
        }

        public void CloseCurrentPopup()
        {
            if (CurrentPopup != null)
            {
                UIPopup popup = _popupStack.Pop();
                popup.Hide();
                OnPopupClosed?.Invoke(popup);
            }
        }

        public void CloseAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                UIPopup popup = _popupStack.Pop();
                popup.Hide();
                OnPopupClosed?.Invoke(popup);
            }
        }

        #endregion

        #region Navigation

        private void HandleBackButton()
        {
            if (_closePopupOnBackButton && HasOpenPopup)
            {
                CloseCurrentPopup();
            }
            else if (_panelStack.Count > 1)
            {
                GoBack();
            }
            else
            {
                // At root panel - show quit confirmation or ignore
                ShowQuitConfirmation();
            }
        }

        private void ShowQuitConfirmation()
        {
            // Override this or connect to a quit confirmation popup
            Debug.Log("[UIManager] Quit confirmation requested");
        }

        #endregion
    }
}

