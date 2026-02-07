using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MedicalTerminology.UI
{
    using Core;
    using Managers;

    /// <summary>
    /// Component for individual level selection button.
    /// Auto-configures based on LevelDataSO.
    /// </summary>
    public class LevelButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject[] starIcons; // 0-3 stars

        [Header("State Colors")]
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color completedColor = Color.green;

        private LevelDataSO levelData;
        private bool isUnlocked;
        private int starsEarned;

        #region Setup

        /// <summary>
        /// Initialize button with level data.
        /// Called by LevelSelectionManager.
        /// </summary>
        public void Setup(LevelDataSO data, bool unlocked, int stars)
        {
            levelData = data;
            isUnlocked = unlocked;
            starsEarned = Mathf.Clamp(stars, 0, 3);

            UpdateVisuals();
            SetupButton();
        }

        private void UpdateVisuals()
        {
            if (levelData == null) return;

            // Level number and name
            if (levelNumberText != null)
            {
                levelNumberText.SetText(levelData.LevelNumber.ToString());
            }

            if (levelNameText != null)
            {
                levelNameText.SetText(levelData.LevelName);
            }

            // Icon
            if (iconImage != null && levelData.LevelIcon != null)
            {
                iconImage.sprite = levelData.LevelIcon;
            }

            // Background color
            if (backgroundImage != null)
            {
                if (!isUnlocked)
                {
                    backgroundImage.color = lockedColor;
                }
                else if (starsEarned > 0)
                {
                    backgroundImage.color = completedColor;
                }
                else
                {
                    backgroundImage.color = levelData.LevelColor;
                }
            }

            // Locked overlay
            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!isUnlocked);
            }

            // Stars
            UpdateStars();
        }

        private void UpdateStars()
        {
            if (starIcons == null || starIcons.Length == 0) return;

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    starIcons[i].SetActive(i < starsEarned);
                }
            }
        }

        private void SetupButton()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.interactable = isUnlocked;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClick);
            }
        }

        #endregion

        #region Button Callback

        private void OnButtonClick()
        {
            if (!isUnlocked || levelData == null)
            {
                Debug.LogWarning("[LevelButton] Level is locked or no data!");
                return;
            }

            Debug.Log($"[LevelButton] Starting Level {levelData.LevelNumber}: {levelData.LevelName}");

            // Pass level data to GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadLevel(levelData);
            }
            else
            {
                Debug.LogError("[LevelButton] GameManager not found!");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Unlock this level button.
        /// </summary>
        public void Unlock()
        {
            isUnlocked = true;
            UpdateVisuals();
            SetupButton();
        }

        /// <summary>
        /// Update stars earned for this level.
        /// </summary>
        public void SetStars(int stars)
        {
            starsEarned = Mathf.Clamp(stars, 0, 3);
            UpdateStars();
            UpdateVisuals();
        }

        /// <summary>
        /// Get level data for this button.
        /// </summary>
        public LevelDataSO GetLevelData()
        {
            return levelData;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }
#endif
    }
}
