using UnityEngine;
using TMPro;

namespace MedicalTerminology.Puzzle
{
    using Legacy;

    /// <summary>
    /// Displays WordModel data on puzzle card tokens.
    /// Used for drag-and-drop puzzle gameplay.
    /// </summary>
    public class PuzzleCardValue : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private TextMeshProUGUI englishText;
        [SerializeField] private TextMeshProUGUI vietnameseText;

        [SerializeField] private UnityEngine.UI.Image cardIcon; // ← NEW: Reference to UI Image

        private WordModel _model;

        public void Initialize(WordModel model)
        {
            _model = model;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_model == null) return;

            if (idText != null)
                idText.SetText(_model.idvalue);

            if (englishText != null)
                englishText.SetText(_model.stringvalue);

            if (vietnameseText != null)
                vietnameseText.SetText(_model.apologetic);
                
            if (cardIcon != null)
            {
                if (_model.icon != null)
                {
                    cardIcon.sprite = _model.icon;
                    cardIcon.gameObject.SetActive(true);
                }
                else
                {
                    // Hide icon if null, or set default sprite
                     // cardIcon.gameObject.SetActive(false); // Optional: Hide if empty
                }
            }
        }

        public WordModel GetModel() => _model;
    }
}

//Archive
    // using UnityEngine.UI; // For Button/Image
    // using UnityEngine.EventSystems; // For IPointerClickHandler
    // using System.Collections; // For IEnumerator

    // /// <summary>
    // /// Displays WordModel data on puzzle card tokens.
    // /// Used for drag-and-drop puzzle gameplay.
    // /// </summary>
    // public class PuzzleCardValue : MonoBehaviour, IPointerClickHandler
    // {
    //     [Header("--- FRONT SIDE ---")]
    //     [SerializeField] private GameObject frontObj;
    //     [SerializeField] private TextMeshProUGUI idText;
    //     [SerializeField] private TextMeshProUGUI englishText;
    //     [SerializeField] private TextMeshProUGUI vietnameseText;
    //     [SerializeField] private Image cardIcon;

    //     [Header("--- BACK SIDE ---")]
    //     [SerializeField] private GameObject backObj; // Create this in prefab!
    //     [SerializeField] private TextMeshProUGUI backDescriptionText; // Description
    //     [SerializeField] private TextMeshProUGUI backDefinitionText; // Definition/Structure
        
    //     private WordModel _model;
    //     private bool _isFlipped;
    //     private bool _isAnimating;
    //     private RectTransform _rectTransform;

    //     private void Awake()
    //     {
    //         _rectTransform = GetComponent<RectTransform>();
    //         if (frontObj != null) frontObj.SetActive(true);
    //         if (backObj != null) backObj.SetActive(false);
    //     }

    //     public void Initialize(WordModel model)
    //     {
    //         _model = model;
    //         UpdateDisplay();
    //     }

    //     private void UpdateDisplay()
    //     {
    //         if (_model == null) return;

    //         // Front
    //         if (idText != null) idText.SetText(_model.idvalue);
    //         if (englishText != null) englishText.SetText(_model.stringvalue);
    //         if (vietnameseText != null) vietnameseText.SetText(_model.apologetic);
            
    //         if (cardIcon != null)
    //         {
    //             if (_model.icon != null)
    //             {
    //                 cardIcon.sprite = _model.icon;
    //                 cardIcon.gameObject.SetActive(true);
    //             }
    //         }

    //         // Back (nếu có data)
    //         // Lưu ý: WordModel hiện tại khá ít field, ta tạm dùng english/vietnamese làm mẫu
    //         // Nếu bạn muốn hiển thị Description/Structure, cần update WordModel thêm fields
    //         if (backDescriptionText != null) backDescriptionText.SetText(_model.stringvalue); 
    //         if (backDefinitionText != null) backDefinitionText.SetText(_model.apologetic);
    //     }

    //     public void OnPointerClick(PointerEventData eventData)
    //     {
    //         // Chỉ lật nếu không phải đang drag (dù drag thường chặn click, nhưng cứ check cho chắc)
    //         if (eventData.dragging) return;
            
    //         // Nếu không có object để lật thì thôi
    //         if (frontObj == null || backObj == null) return;

    //         if (_isAnimating) return;
    //         StartCoroutine(FlipCoroutine());
    //     }

    //     private IEnumerator FlipCoroutine()
    //     {
    //         _isAnimating = true;
    //         const float duration = 0.2f;
    //         float elapsed = 0f;
    //         Vector3 originalScale = _rectTransform.localScale;

    //         // Shrink
    //         while (elapsed < duration)
    //         {
    //             elapsed += Time.deltaTime;
    //             float scaleX = Mathf.Lerp(originalScale.x, 0f, elapsed / duration);
    //             _rectTransform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
    //             yield return null;
    //         }

    //         // Switch
    //         _isFlipped = !_isFlipped;
    //         if (frontObj != null) frontObj.SetActive(!_isFlipped);
    //         if (backObj != null) backObj.SetActive(_isFlipped);

    //         // Expand
    //         elapsed = 0f;
    //         while (elapsed < duration)
    //         {
    //             elapsed += Time.deltaTime;
    //             float scaleX = Mathf.Lerp(0f, originalScale.x, elapsed / duration);
    //             _rectTransform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
    //             yield return null;
    //         }

    //         _rectTransform.localScale = originalScale;
    //         _isAnimating = false;
    //     }

    //     public WordModel GetModel() => _model;
    // }