using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MedicalTerminology.UI
{
    using Managers;

    /// <summary>
    /// UI component for individual mission items in Study mode checklist.
    /// Displays mission number and handles completion state.
    /// </summary>
    public class MissionItemUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button itemButton;
        [SerializeField] private GameObject checkmarkIcon;

        private string _wordId;
        private string _content;

        /// <summary>
        /// Initialize mission item with index and details.
        /// </summary>
        public void Setup(int index, string wordId, string content)
        {
            _wordId = wordId;
            _content = content;

            if (titleText != null)
            {
                titleText.SetText($"Question {index}");
            }

            if (checkmarkIcon != null)
            {
                checkmarkIcon.SetActive(false);
            }

            // Setup button click
            if (itemButton == null)
            {
                itemButton = GetComponent<Button>();
            }

            if (itemButton != null)
            {
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(OnItemClicked);
            }
        }

        private void OnItemClicked()
        {
            // Show mission detail in main question area
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ShowMissionDetail(_content);
            }
        }

        /// <summary>
        /// Mark this mission as completed with visual feedback.
        /// </summary>
        public void MarkCompleted()
        {
            if (checkmarkIcon != null)
            {
                checkmarkIcon.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.color = Color.green;
                titleText.fontStyle = FontStyles.Strikethrough;
            }
        }

        public string GetID() => _wordId;
    }
}
