using UnityEngine;

namespace MedicalTerminology.Core
{
    /// <summary>
    /// Defines difficulty levels for gameplay.
    /// </summary>
    public enum DifficultyLevel
    {
        Tutorial,   // Extra time, hints available
        Easy,       // Normal time, some hints
        Normal,     // Standard gameplay
        Hard,       // Less time, no hints
        Expert      // Minimal time, harder words
    }

    /// <summary>
    /// ScriptableObject containing data for a single level/stage.
    /// Create instances via: Create → GenThuatNgu → Level Data
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "GenThuatNgu/Level Data", order = 3)]
    public class LevelDataSO : ScriptableObject
    {
        [Header("Level Identity")]
        [SerializeField] private int levelNumber;
        [SerializeField] private string levelName;
        [SerializeField] private string levelDescription;

        [Header("Difficulty Settings")]
        [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Normal;
        
        [Header("Gameplay Parameters")]
        [SerializeField] private int missionCount = 5;
        [SerializeField] private float timeLimit = 120f;
        [SerializeField] private int targetScore = 500;

        [Header("Card Pool")]
        [SerializeField] private CardCategory[] allowedCategories;
        [SerializeField] private CardRarity minRarity = CardRarity.Common;
        [SerializeField] private CardRarity maxRarity = CardRarity.Rare;

        [Header("Rewards")]
        [SerializeField] private int goldReward = 100;
        [SerializeField] private int experienceReward = 50;
        [SerializeField] private CardDataSO[] unlockableCards;

        [Header("Unlock Requirements")]
        [SerializeField] private int requiredLevel = 1; // Must complete this level first
        [SerializeField] private int requiredStars = 0; // Stars needed from previous levels

        [Header("Visual")]
        [SerializeField] private Sprite levelIcon;
        [SerializeField] private Color levelColor = Color.white;

        #region Properties

        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public string LevelDescription => levelDescription;
        public DifficultyLevel Difficulty => difficulty;
        public int MissionCount => missionCount;
        public float TimeLimit => timeLimit;
        public int TargetScore => targetScore;
        public CardCategory[] AllowedCategories => allowedCategories;
        public CardRarity MinRarity => minRarity;
        public CardRarity MaxRarity => maxRarity;
        public int GoldReward => goldReward;
        public int ExperienceReward => experienceReward;
        public CardDataSO[] UnlockableCards => unlockableCards;
        public int RequiredLevel => requiredLevel;
        public int RequiredStars => requiredStars;
        public Sprite LevelIcon => levelIcon;
        public Color LevelColor => levelColor;

        #endregion

        #region Difficulty Modifiers

        /// <summary>
        /// Get time multiplier based on difficulty.
        /// </summary>
        public float GetTimeMultiplier()
        {
            return difficulty switch
            {
                DifficultyLevel.Tutorial => 1.5f,  // +50% time
                DifficultyLevel.Easy => 1.25f,     // +25% time
                DifficultyLevel.Normal => 1f,      // Standard
                DifficultyLevel.Hard => 0.8f,      // -20% time
                DifficultyLevel.Expert => 0.6f,    // -40% time
                _ => 1f
            };
        }

        /// <summary>
        /// Get score multiplier based on difficulty.
        /// </summary>
        public float GetScoreMultiplier()
        {
            return difficulty switch
            {
                DifficultyLevel.Tutorial => 0.5f,
                DifficultyLevel.Easy => 0.75f,
                DifficultyLevel.Normal => 1f,
                DifficultyLevel.Hard => 1.5f,
                DifficultyLevel.Expert => 2f,
                _ => 1f
            };
        }

        /// <summary>
        /// Check if hints are allowed for this difficulty.
        /// </summary>
        public bool AreHintsAllowed()
        {
            return difficulty switch
            {
                DifficultyLevel.Tutorial => true,
                DifficultyLevel.Easy => true,
                DifficultyLevel.Normal => false,
                DifficultyLevel.Hard => false,
                DifficultyLevel.Expert => false,
                _ => false
            };
        }

        /// <summary>
        /// Get final time limit with difficulty modifier applied.
        /// </summary>
        public float GetAdjustedTimeLimit()
        {
            return timeLimit * GetTimeMultiplier();
        }

        #endregion

        #region Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure level number is positive
            if (levelNumber < 1)
            {
                levelNumber = 1;
            }

            // Auto-generate name if empty
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = $"Level {levelNumber}";
            }

            // Ensure time limit is reasonable
            if (timeLimit < 30f)
            {
                timeLimit = 30f;
            }

            // Ensure mission count is valid
            if (missionCount < 1)
            {
                missionCount = 1;
            }

            // Ensure rarity range is valid
            if (minRarity > maxRarity)
            {
                maxRarity = minRarity;
            }
        }
#endif

        #endregion
    }
}
