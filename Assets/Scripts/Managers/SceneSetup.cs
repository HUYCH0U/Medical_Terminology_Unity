using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MedicalTerminology.Managers
{
    /// <summary>
    /// Auto-setup GameManager references when scene loads.
    /// Attach to Canvas in each gameplay scene (Puzzle, Study).
    /// Solves the problem of persistent GameManager not being able to reference scene objects.
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Common UI References")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI questionContentText;
        [SerializeField] private Transform tokenSpawnArea;
        [SerializeField] private Transform[] slotTransforms;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Button resetButton;

        [Header("Study Mode UI (Optional)")]
        [SerializeField] private Transform checklistContent;
        [SerializeField] private GameObject missionItemPrefab;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject gameoverPanel;

        [Header("Data References")]
        [SerializeField] private CardDataManager dataManager;

        private void Start()
        {
            SetupGameManager();
        }

        /// <summary>
        /// Find GameManager singleton and assign this scene's references.
        /// </summary>
        private void SetupGameManager()
        {
            var gameManager = GameManager.Instance;
            
            if (gameManager == null)
            {
                Debug.LogError("[SceneSetup] GameManager singleton not found! Make sure it exists in StartMenu scene.");
                return;
            }

            // Call public setup method on GameManager
            gameManager.SetupSceneReferences(
                scoreText,
                timeText,
                missionTitleText,
                questionContentText,
                tokenSpawnArea,
                slotTransforms,
                cardPrefab,
                resetButton,
                checklistContent,
                missionItemPrefab,
                progressText,
                gameoverPanel,
                dataManager
            );

            Debug.Log($"[SceneSetup] GameManager configured for scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        }

#if UNITY_EDITOR
        [ContextMenu("Validate References")]
        private void ValidateReferences()
        {
            if (scoreText == null) Debug.LogWarning("[SceneSetup] Score Text not assigned!");
            if (timeText == null) Debug.LogWarning("[SceneSetup] Time Text not assigned!");
            if (missionTitleText == null) Debug.LogWarning("[SceneSetup] Mission Title Text not assigned!");
            if (questionContentText == null) Debug.LogWarning("[SceneSetup] Question Content Text not assigned!");
            if (tokenSpawnArea == null) Debug.LogWarning("[SceneSetup] Token Spawn Area not assigned!");
            if (slotTransforms == null || slotTransforms.Length == 0) Debug.LogWarning("[SceneSetup] Slot Transforms not assigned!");
            if (cardPrefab == null) Debug.LogWarning("[SceneSetup] Card Prefab not assigned!");
            if (dataManager == null) Debug.LogWarning("[SceneSetup] Data Manager not assigned!");
        }
#endif
    }
}
