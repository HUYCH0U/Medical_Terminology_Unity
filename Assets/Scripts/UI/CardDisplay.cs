using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace MedicalTerminology.UI
{
    using Core;

    /// <summary>
    /// Displays card information with flip animation.
    /// Optimized with component caching and TMP features.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardDisplay : MonoBehaviour
    {
        [Header("--- FRONT SIDE (Mặt Trước) ---")]
        [SerializeField] private GameObject frontObj;
        [SerializeField] private TextMeshProUGUI textTitleEn;
        [SerializeField] private TextMeshProUGUI textTitleVi;
        [SerializeField] private Image imageMain;
        [SerializeField] private Image imageBorder;

        [Header("--- BACK SIDE (Mặt Sau) ---")]
        [SerializeField] private GameObject backObj;
        [SerializeField] private TextMeshProUGUI textStructure;
        [SerializeField] private TextMeshProUGUI textDescription;

        [Header("--- RESOURCES ---")]
        [SerializeField] private Sprite borderCommon;
        [SerializeField] private Sprite borderRare;
        [SerializeField] private Sprite borderLegendary;

        // Cached components
        private RectTransform _rectTransform;
        private Button _button;
        
        private bool _isFlipped;
        private bool _isAnimating;
        private CardDataSO _currentData;

        private void Awake()
        {
            // Cache components for performance
            _rectTransform = GetComponent<RectTransform>();
            _button = GetComponent<Button>();
            
            ValidateReferences();
        }

        private void Start()
        {
            frontObj.SetActive(true);
            backObj.SetActive(false);

            if (_button != null)
            {
                _button.onClick.AddListener(OnCardClicked);
            }
        }

        /// <summary>
        /// Setup card with data. Optimized with TMP features.
        /// </summary>
        public void SetupData(CardDataSO data)
        {
            if (data == null)
            {
                Debug.LogError("[CardDisplay] Cannot setup with null data!", this);
                return;
            }

            _currentData = data;

            // Use TMP UpperCase feature instead of string allocation
            textTitleEn.SetText(data.termEnglish);
            textTitleEn.fontStyle = FontStyles.UpperCase;
            
            textTitleVi.SetText(data.termVietnamese);
            
            if (data.illustration != null)
            {
                imageMain.sprite = data.illustration;
            }

            // Set border based on rarity
            imageBorder.sprite = data.rarity switch
            {
                CardRarity.Common => borderCommon,
                CardRarity.Rare => borderRare,
                CardRarity.Legendary => borderLegendary,
                _ => borderCommon
            };

            // Structure text with rich text
            if (!string.IsNullOrEmpty(data.structure))
            {
                textStructure.SetText($"<color=yellow>{data.structure}</color>");
            }
            else
            {
                textStructure.SetText("");
            }
            
            textDescription.SetText(data.description);
        }

        private void ValidateReferences()
        {
#if UNITY_EDITOR
            if (frontObj == null) Debug.LogError($"[CardDisplay] frontObj not assigned on '{gameObject.name}'", this);
            if (backObj == null) Debug.LogError($"[CardDisplay] backObj not assigned on '{gameObject.name}'", this);
            if (textTitleEn == null) Debug.LogError($"[CardDisplay] textTitleEn not assigned on '{gameObject.name}'", this);
            if (textTitleVi == null) Debug.LogError($"[CardDisplay] textTitleVi not assigned on '{gameObject.name}'", this);
#endif
        }

        public void OnCardClicked()
        {
            if (_isAnimating) return;
            StartCoroutine(FlipCoroutine());
        }

        private IEnumerator FlipCoroutine()
        {
            _isAnimating = true;
            const float duration = 0.2f;
            float elapsed = 0f;

            Vector3 originalScale = _rectTransform.localScale;

            // Shrink (hide current side)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scaleX = Mathf.Lerp(1f, 0f, elapsed / duration);
                _rectTransform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
                yield return null;
            }

            // Switch sides
            _isFlipped = !_isFlipped;
            frontObj.SetActive(!_isFlipped);
            backObj.SetActive(_isFlipped);

            // Expand (show new side)
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scaleX = Mathf.Lerp(0f, 1f, elapsed / duration);
                _rectTransform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
                yield return null;
            }

            _rectTransform.localScale = originalScale;
            _isAnimating = false;
        }
    }
}