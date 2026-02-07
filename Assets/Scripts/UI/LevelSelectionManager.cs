using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace MedicalTerminology.UI
{
    using Core;

    /// <summary>
    /// Manages dynamic creation and configuration of level selection buttons.
    /// Handles progression unlock logic.
    /// </summary>
    public class LevelSelectionManager : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private LevelDataSO[] allLevels;

        [Header("UI Setup")]
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private GridLayoutGroup gridLayout;

        [Header("Progression")]
        [SerializeField] private bool unlockAllForTesting = false;

        private List<LevelButton> spawnedButtons = new List<LevelButton>();
        private Dictionary<int, LevelProgress> levelProgress = new Dictionary<int, LevelProgress>();

        [System.Serializable]
        private class LevelProgress
        {
            public bool isUnlocked;
            public bool isCompleted;
            public int starsEarned;
            public int highScore;
        }

        #region Unity Lifecycle

        private void Start()
        {
            LoadProgression();
            SpawnLevelButtons();
        }

        #endregion

        #region Level Button Spawning

        private void SpawnLevelButtons()
        {
            // Clear existing buttons
            ClearButtons();

            if (allLevels == null || allLevels.Length == 0)
            {
                Debug.LogWarning("[LevelSelectionManager] No levels assigned!");
                return;
            }

            // Sort levels by level number
            System.Array.Sort(allLevels, (a, b) => a.LevelNumber.CompareTo(b.LevelNumber));

            // Spawn button for each level
            foreach (var levelData in allLevels)
            {
                if (levelData == null) continue;

                SpawnLevelButton(levelData);
            }

            Debug.Log($"[LevelSelectionManager] Spawned {spawnedButtons.Count} level buttons");
        }

        private void SpawnLevelButton(LevelDataSO levelData)
        {
            if (levelButtonPrefab == null || buttonContainer == null)
            {
                Debug.LogError("[LevelSelectionManager] Button prefab or container not assigned!");
return;
            }

            // Instantiate button
            GameObject buttonObj = Instantiate(levelButtonPrefab, buttonContainer);
            LevelButton levelButton = buttonObj.GetComponent<LevelButton>();

            if (levelButton == null)
            {
                Debug.LogError($"[LevelSelectionManager] LevelButton component not found on prefab!");
                Destroy(buttonObj);
                return;
            }

            // Get progression data
            bool isUnlocked = IsLevelUnlocked(levelData);
            int stars = GetLevelStars(levelData.LevelNumber);

            // Setup button
            levelButton.Setup(levelData, isUnlocked, stars);
            spawnedButtons.Add(levelButton);
        }

        private void ClearButtons()
        {
            foreach (var button in spawnedButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            spawnedButtons.Clear();
        }

        #endregion

        #region Progression Logic

        private bool IsLevelUnlocked(LevelDataSO levelData)
        {
            // Testing mode: unlock all
            if (unlockAllForTesting)
            {
                return true;
            }

            // Level 1 is always unlocked
            if (levelData.LevelNumber == 1)
            {
                return true;
            }

            // Check if explicitly unlocked in save data
            if (levelProgress.TryGetValue(levelData.LevelNumber, out var progress))
            {
                if (progress.isUnlocked)
                {
                    return true;
                }
            }

            // Check required level completion
            if (levelData.RequiredLevel > 0)
            {
                if (!IsLevelCompleted(levelData.RequiredLevel))
                {
                    return false;
                }
            }

            // Check required stars
            if (levelData.RequiredStars > 0)
            {
                int totalStars = GetTotalStarsEarned();
                if (totalStars < levelData.RequiredStars)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsLevelCompleted(int levelNumber)
        {
            if (levelProgress.TryGetValue(levelNumber, out var progress))
            {
                return progress.isCompleted;
            }
            return false;
        }

        private int GetLevelStars(int levelNumber)
        {
            if (levelProgress.TryGetValue(levelNumber, out var progress))
            {
                return progress.starsEarned;
            }
            return 0;
        }

        private int GetTotalStarsEarned()
        {
            int total = 0;
            foreach (var kvp in levelProgress)
            {
                total += kvp.Value.starsEarned;
            }
            return total;
        }

        #endregion

        #region Save/Load

        private void LoadProgression()
        {
            // TODO: Load from PlayerPrefs or save file
            // For now, initialize with default data

            levelProgress.Clear();

            // Level 1 always unlocked
            levelProgress[1] = new LevelProgress
            {
                isUnlocked = true,
                isCompleted = false,
                starsEarned = 0,
                highScore = 0
            };

            // Example: Level 2 completed with 2 stars
            levelProgress[2] = new LevelProgress
            {
                isUnlocked = true,
                isCompleted = true,
                starsEarned = 2,
                highScore = 850
            };

            Debug.Log("[LevelSelectionManager] Progression loaded");
        }

        /// <summary>
        /// Save level completion data.
        /// Called when player completes a level.
        /// </summary>
        public void SaveLevelCompletion(int levelNumber, int score, int stars)
        {
            if (!levelProgress.ContainsKey(levelNumber))
            {
                levelProgress[levelNumber] = new LevelProgress();
            }

            var progress = levelProgress[levelNumber];
            progress.isUnlocked = true;
            progress.isCompleted = true;
            progress.starsEarned = Mathf.Max(progress.starsEarned, stars); // Keep best
            progress.highScore = Mathf.Max(progress.highScore, score); // Keep best

            // Unlock next level
            int nextLevel = levelNumber + 1;
            if (!levelProgress.ContainsKey(nextLevel))
            {
                levelProgress[nextLevel] = new LevelProgress();
            }
            levelProgress[nextLevel].isUnlocked = true;

            // TODO: Save to PlayerPrefs or file
            // PlayerPrefs.SetInt($"Level_{levelNumber}_Stars", stars);
            // PlayerPrefs.SetInt($"Level_{levelNumber}_Score", score);
            // PlayerPrefs.Save();

            Debug.Log($"[LevelSelectionManager] Level {levelNumber} completed: {stars} stars, {score} score");

            // Refresh UI
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            foreach (var button in spawnedButtons)
            {
                if (button != null)
                {
                    var levelData = button.GetLevelData();
                    if (levelData != null)
                    {
                        bool isUnlocked = IsLevelUnlocked(levelData);
                        int stars = GetLevelStars(levelData.LevelNumber);
                        button.Setup(levelData, isUnlocked, stars);
                    }
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get level data by level number.
        /// </summary>
        public LevelDataSO GetLevelData(int levelNumber)
        {
            foreach (var level in allLevels)
            {
                if (level != null && level.LevelNumber == levelNumber)
                {
                    return level;
                }
            }
            return null;
        }

        /// <summary>
        /// Unlock a specific level (cheat/debug).
        /// </summary>
        public void UnlockLevel(int levelNumber)
        {
            if (!levelProgress.ContainsKey(levelNumber))
            {
                levelProgress[levelNumber] = new LevelProgress();
            }
            levelProgress[levelNumber].isUnlocked = true;
            RefreshButtons();
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Unlock All Levels")]
        private void UnlockAllLevels()
        {
            foreach (var level in allLevels)
            {
                if (level != null)
                {
                    UnlockLevel(level.LevelNumber);
                }
            }
            Debug.Log("[LevelSelectionManager] All levels unlocked!");
        }

        [ContextMenu("Reset Progression")]
        private void ResetProgression()
        {
            levelProgress.Clear();
            LoadProgression();
            RefreshButtons();
            Debug.Log("[LevelSelectionManager] Progression reset!");
        }
#endif
    }
}
