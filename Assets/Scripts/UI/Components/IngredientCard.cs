using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MasterCheff.Gameplay;

namespace MasterCheff.UI.Components
{
    /// <summary>
    /// UI Component for displaying an ingredient card
    /// </summary>
    public class IngredientCard : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _ingredientNameText;
        [SerializeField] private Image _ingredientIcon;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _rarityBorder;

        [Header("Rarity Colors")]
        [SerializeField] private Color _commonColor = Color.white;
        [SerializeField] private Color _uncommonColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _rareColor = new Color(0.2f, 0.4f, 1f);
        [SerializeField] private Color _legendaryColor = new Color(1f, 0.8f, 0f);

        [Header("Animation")]
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _animationSpeed = 10f;
        [SerializeField] private bool _animateOnReveal = true;

        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private IngredientData _ingredientData;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _targetScale = _originalScale;
        }

        private void Update()
        {
            // Smooth scale animation
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _animationSpeed);
        }

        /// <summary>
        /// Display an ingredient on this card
        /// </summary>
        public void SetIngredient(string ingredientName, Sprite icon = null, IngredientRarity rarity = IngredientRarity.Common)
        {
            if (_ingredientNameText != null)
            {
                _ingredientNameText.text = ingredientName;
            }

            if (_ingredientIcon != null && icon != null)
            {
                _ingredientIcon.sprite = icon;
                _ingredientIcon.gameObject.SetActive(true);
            }
            else if (_ingredientIcon != null)
            {
                _ingredientIcon.gameObject.SetActive(false);
            }

            SetRarity(rarity);

            if (_animateOnReveal)
            {
                PlayRevealAnimation();
            }
        }

        /// <summary>
        /// Display an ingredient using IngredientData
        /// </summary>
        public void SetIngredient(IngredientData data, Sprite icon = null)
        {
            _ingredientData = data;
            SetIngredient(data.Name, icon, data.Rarity);
        }

        /// <summary>
        /// Set the rarity visual
        /// </summary>
        public void SetRarity(IngredientRarity rarity)
        {
            Color rarityColor = rarity switch
            {
                IngredientRarity.Common => _commonColor,
                IngredientRarity.Uncommon => _uncommonColor,
                IngredientRarity.Rare => _rareColor,
                IngredientRarity.Legendary => _legendaryColor,
                _ => _commonColor
            };

            if (_rarityBorder != null)
            {
                _rarityBorder.color = rarityColor;
            }

            // Add glow effect for legendary
            if (rarity == IngredientRarity.Legendary && _backgroundImage != null)
            {
                // Could add glow shader or animation here
            }
        }

        /// <summary>
        /// Play the reveal animation
        /// </summary>
        public void PlayRevealAnimation()
        {
            StartCoroutine(RevealAnimationCoroutine());
        }

        private System.Collections.IEnumerator RevealAnimationCoroutine()
        {
            transform.localScale = Vector3.zero;
            _targetScale = _originalScale;

            float elapsed = 0f;
            float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Elastic ease out
                float c4 = (2f * Mathf.PI) / 3f;
                float eased = t == 0f ? 0f : t == 1f ? 1f : 
                    Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;

                transform.localScale = _originalScale * eased;
                yield return null;
            }

            transform.localScale = _originalScale;
        }

        /// <summary>
        /// Hover effect
        /// </summary>
        public void OnPointerEnter()
        {
            _targetScale = _originalScale * _hoverScale;
        }

        /// <summary>
        /// End hover effect
        /// </summary>
        public void OnPointerExit()
        {
            _targetScale = _originalScale;
        }

        /// <summary>
        /// Clear the card display
        /// </summary>
        public void Clear()
        {
            if (_ingredientNameText != null)
                _ingredientNameText.text = string.Empty;

            if (_ingredientIcon != null)
                _ingredientIcon.gameObject.SetActive(false);

            _ingredientData = null;
        }
    }
}

