using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Data;

namespace MasterCheff.UI.Components
{
    /// <summary>
    /// UI Component for style tag selection buttons
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StyleTagButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;

        [Header("Visuals")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.8f, 0.4f);
        [SerializeField] private Color _normalTextColor = Color.black;
        [SerializeField] private Color _selectedTextColor = Color.white;
        [SerializeField] private float _selectedScale = 1.1f;
        [SerializeField] private float _animationSpeed = 10f;

        [Header("Tag Info")]
        [SerializeField] private DishStyleTag _styleTag;
        [SerializeField] private Sprite _tagIcon;

        private Button _button;
        private Action<DishStyleTag> _onSelected;
        private bool _isSelected;
        private Vector3 _originalScale;
        private Vector3 _targetScale;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _originalScale = transform.localScale;
            _targetScale = _originalScale;

            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }

            UpdateLabel();
        }

        private void Update()
        {
            // Smooth scale animation
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _animationSpeed);
        }

        /// <summary>
        /// Initialize the button with a tag and callback
        /// </summary>
        public void Initialize(DishStyleTag tag, Action<DishStyleTag> onSelected)
        {
            _styleTag = tag;
            _onSelected = onSelected;
            UpdateLabel();
        }

        /// <summary>
        /// Set the selected state of the button
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisuals();
        }

        private void OnButtonClicked()
        {
            _onSelected?.Invoke(_styleTag);
        }

        private void UpdateLabel()
        {
            if (_labelText != null)
            {
                _labelText.text = GetTagDisplayName(_styleTag);
            }

            if (_iconImage != null && _tagIcon != null)
            {
                _iconImage.sprite = _tagIcon;
            }
        }

        private void UpdateVisuals()
        {
            // Update colors
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _isSelected ? _selectedColor : _normalColor;
            }

            if (_labelText != null)
            {
                _labelText.color = _isSelected ? _selectedTextColor : _normalTextColor;
            }

            // Update scale
            _targetScale = _isSelected ? _originalScale * _selectedScale : _originalScale;
        }

        private string GetTagDisplayName(DishStyleTag tag)
        {
            return tag switch
            {
                DishStyleTag.HomeyComfort => "Homey & Comfort",
                DishStyleTag.GourmetFineDining => "Gourmet",
                DishStyleTag.DecadentDessert => "Decadent Dessert",
                DishStyleTag.HealthyFresh => "Healthy & Fresh",
                DishStyleTag.CrazyFusion => "Crazy Fusion",
                _ => tag.ToString()
            };
        }
    }
}
