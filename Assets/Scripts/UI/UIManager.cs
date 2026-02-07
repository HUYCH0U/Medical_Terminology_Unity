using UnityEngine;

namespace MedicalTerminology.UI
{
    using Managers;

    public class UIManager : MonoBehaviour
    {

        #region Scene Navigation

        public void GoToStartMenu()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Menu();
            }
            else
            {
                Debug.LogError("[UIManager] GameManager not found!");
            }
        }

        public void GoToLobby()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Lobby();
            }
            else
            {
                Debug.LogError("[UIManager] GameManager not found!");
            }
        }

        public void StartPuzzle()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartPuzzleGame();
            }
            else
            {
                Debug.LogError("[UIManager] GameManager not found!");
            }
        }

        public void StartStudy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StudyMode();
            }
            else
            {
                Debug.LogError("[UIManager] GameManager not found!");
            }
        }

        #endregion

        #region Game Actions

        public void RetryLevel()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Retry();
            }
            else
            {
                Debug.LogError("[UIManager] GameManager not found!");
            }
        }

        public void QuitApplication()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("[UIManager] Quit requested (ignored in editor)");
#else
                Application.Quit();
#endif
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateGameManager();
        }

        private void ValidateGameManager()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[UIManager] GameManager singleton not found! Make sure you started from StartMenu scene.");
            }
            else
            {
                Debug.Log($"[UIManager] Connected to GameManager in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            }
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Test: Check GameManager")]
        private void TestGameManager()
        {
            if (GameManager.Instance != null)
            {
                Debug.Log($"✅ GameManager found! Instance ID: {GameManager.Instance.GetInstanceID()}");
            }
            else
            {
                Debug.LogError("❌ GameManager NOT found!");
            }
        }
#endif
    }
}
