using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;

namespace MedicalTerminology.Managers
{
    using Core;
    using Legacy;
    using UI;

    using Puzzle;

    /// <summary>
    /// Game manager for both Puzzle and Study modes.
    /// Uses config-driven approach with GameMode enum.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public enum GameMode
        {
            Puzzle,     // Single endless mode - keep playing until time runs out
            Study       // Mission-based mode - complete N missions with checklist
        }

        public enum MissionType
        {
            TargetWord_VN,      // Dịch thuật ngữ tiếng Việt
            Sentence_EN,        // Hoàn thành câu tiếng Anh
            Sentence_VN,        // Hoàn thành câu tiếng Việt
            MissingChar         // Đoán từ bị khuyết
        }

        [Header("Game Mode Configuration")]
        [SerializeField] private GameMode gameMode = GameMode.Puzzle;
        [SerializeField] private int missionCount = 5; // For Study mode only
        [SerializeField] private float levelTime = 120f;

        [Header("Data References")]
        [SerializeField] private CardDataManager dataManager;
        [SerializeField] private CardManager cardManager;
        
        // Current level data (set when loading from level select)
        private LevelDataSO currentLevel;

        public void LoadLevel(LevelDataSO levelData)
        {
            currentLevel = levelData;
            
            // Apply level parameters
            if (currentLevel != null)
            {
                missionCount = currentLevel.MissionCount;
                levelTime = currentLevel.GetAdjustedTimeLimit();
                
                Debug.Log($"[GameManager] Loading Level {currentLevel.LevelNumber}: {currentLevel.LevelName}");
                Debug.Log($"[GameManager] Difficulty: {currentLevel.Difficulty}, Time: {levelTime}s, Missions: {missionCount}");
            }
            
            // Load gameplay scene
            SceneManager.LoadScene("StudyWord");
        }

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

        // State
        private float _currentTime;
        private int _currentScore;
        private bool _isGameActive;

        private WordModel _currentTarget;
        private MissionType _currentMissionType;
       

        // Study mode specific
        private List<WordModel> _missionList = new List<WordModel>();
        private HashSet<string> _completedMissionIds = new HashSet<string>();
        private Dictionary<string, MissionItemUI> _missionUiMap = new Dictionary<string, MissionItemUI>();

        #region Unity Lifecycle

        private void Awake()
        {
            // Persistent Singleton - survives scene loads
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // ← CRITICAL! Persist across scenes
                
                // Subscribe to scene loaded events (runs EVERY scene change!)
                SceneManager.sceneLoaded += OnSceneLoaded;
                
                Debug.Log("[GameManager] Singleton created and persisted");
            }
            else
            {
                Debug.LogWarning("[GameManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return; // Exit early - this instance is dead
            }
            
            ValidateReferences(); // Dont check references if bool isMenuScene is true
            
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Clean up singleton reference when destroyed
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            Debug.Log("🔵 [GameManager] Start() ENTRY POINT");
            
            // Only setup gameplay in Puzzle/Study scenes, skip menus
            string currentScene = SceneManager.GetActiveScene().name;
            
            if (currentScene == "StartMenu" || currentScene == "Lobby")
            {
                Debug.Log($"[GameManager] In {currentScene} - No gameplay setup (menu)");
                return; // Don't initialize game logic in menus
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetClick);
            }

            Debug.Log($"🟢 [GameManager] Start() called in scene: {currentScene}");
            Debug.Log($"[GameManager] GameMode: {gameMode}");
            Debug.Log($"[GameManager] DataManager: {(dataManager != null ? "✅ Assigned" : "❌ NULL")}");
            Debug.Log($"[GameManager] TokenSpawnArea: {(tokenSpawnArea != null ? "✅ Assigned" : "❌ NULL")}");
            Debug.Log($"[GameManager] CardPrefab: {(cardPrefab != null ? "✅ Assigned" : "❌ NULL")}");
            Debug.Log($"[GameManager] SlotTransforms: {(slotTransforms != null ? $"✅ {slotTransforms.Length} slots" : "❌ NULL")}");
            
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetClick);
            }

            StartCoroutine(StartGameDelayed());
        }

        /// <summary>
        /// Called EVERY TIME a scene is loaded (unlike Start which only runs once).
        /// This fixes the DontDestroyOnLoad issue where Start() only runs in first scene.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"🔵 [GameManager] OnSceneLoaded: {scene.name} (Mode: {mode})");
            
            string sceneName = scene.name;
            
            // Skip menu scenes - no gameplay initialization needed
            if (sceneName == "StartMenu" || sceneName == "Lobby")
            {
                Debug.Log($"[GameManager] Menu scene '{sceneName}' - No gameplay initialization");
                return;
            }
            
            // For gameplay scenes (StudyWord, MainGame), wait for SceneSetup to assign references
            // Then initialize gameplay
            if (sceneName == "StudyWord" || sceneName == "MainGame")
            {
                Debug.Log($"🟢 [GameManager] Gameplay scene '{sceneName}' detected - Starting initialization...");
                
                // Wait a frame for SceneSetup to complete, then initialize
                StartCoroutine(InitializeAfterSceneSetup());
            }
        }

        /// <summary>
        /// Wait for SceneSetup to assign references, then start the game.
        /// </summary>
        private IEnumerator InitializeAfterSceneSetup()
        {
            Debug.Log("[GameManager] Waiting for SceneSetup...");
            
            // Wait briefly for SceneSetup.Start() to run and assign references
            yield return new WaitForSeconds(0.1f);
            
            Debug.Log("[GameManager] SceneSetup should be complete, starting game...");
            
            // Now run the game initialization
            yield return StartGameDelayed();
        }

        private void Update()
        {
            if (!_isGameActive) return;

            _currentTime -= Time.deltaTime;
            UpdateTimeUI();

            // if (_currentTime <= 0)
            // {
            //     EndGame();
            // }
        }

        #endregion

        #region Game Flow

        private IEnumerator StartGameDelayed()
        {
            yield return null; // Wait for data initialization

            if (dataManager == null)
            {
                Debug.LogError("[UnifiedGameManager] CardDataManager not assigned!");
                yield break;
            }

            dataManager.Initialize();

            if (gameMode == GameMode.Study)
            {
                StartStudySession();
            }
            else
            {
                StartPuzzleSession();
            }
        }

        private void StartPuzzleSession()
        {
            _currentScore = 0;
            _currentTime = levelTime;
            _isGameActive = true;

            UpdateScoreUI();
            StartNewWave();
        }

        private void StartStudySession()
        {
            Debug.Log("[GameManager] === StartStudySession() BEGIN ===");
            
            _currentScore = 0;
            _currentTime = levelTime;
            _isGameActive = true;
            _completedMissionIds.Clear();
            _missionUiMap.Clear();

            Debug.Log($"[GameManager] Study session initialized - Time: {levelTime}s, Active: {_isGameActive}");

            GenerateMissionList();
            Debug.Log("[GameManager] After GenerateMissionList()");
            
            CreateChecklistUI();
            Debug.Log("[GameManager] After CreateChecklistUI()");
            
            UpdateScoreUI();
            UpdateProgressUI();

            Debug.Log("[GameManager] Calling PickNewActiveMissionAndSpawn()...");
            PickNewActiveMissionAndSpawn();
            
            Debug.Log("[GameManager] === StartStudySession() END ===");
        }

        public void StartNewWave()
        {
            ClearBoard();

            _currentTarget = dataManager.GetRandomTarget();
            if (_currentTarget == null)
            {
                Debug.LogError("[UnifiedGameManager] No target found!");
                return;
            }

            _currentMissionType = (MissionType)Random.Range(0, 4);

            SetupMissionUI();
            SpawnTokens(_currentTarget);

            if (gameMode == GameMode.Puzzle)
            {
                _currentTime = levelTime; // Reset time cada wave en Puzzle mode
            }

            _isGameActive = true;
        }

        private void PickNewActiveMissionAndSpawn()
        {
            Debug.Log("[GameManager] === PickNewActiveMissionAndSpawn() BEGIN ===");
            
            ClearBoard();
            Debug.Log("[GameManager] Board cleared");

            var pendingMissions = _missionList.Where(x => !_completedMissionIds.Contains(x.idvalue)).ToList();
            Debug.Log($"[GameManager] Pending missions: {pendingMissions.Count} / {_missionList.Count}");

            if (pendingMissions.Count == 0)
            {
                Debug.Log("[GameManager] No pending missions - EndGame()");
                EndGame(); // Victory
                return;
            }

            _currentTarget = pendingMissions[Random.Range(0, pendingMissions.Count)];
            Debug.Log($"[GameManager] Selected target: {(_currentTarget != null ? _currentTarget.stringvalue : "NULL")}");

            // Set default mission type for Study Mode (e.g., Translate VN -> EN)
            _currentMissionType = MissionType.TargetWord_VN; 
            
            // Update UI
            SetupMissionUI();
            
            Debug.Log("[GameManager] Calling SpawnTokens()...");
            SpawnTokens(_currentTarget);
            Debug.Log("[GameManager] === PickNewActiveMissionAndSpawn() END ===");
        }

        #endregion

        #region Mission Setup

        private void GenerateMissionList()
        {
            _missionList.Clear();

            for (int i = 0; i < missionCount; i++)
            {
                WordModel word = dataManager.GetRandomTarget();
                if (word == null) continue;

                int safeGuard = 0;
                while (_missionList.Exists(x => x.idvalue == word.idvalue) && safeGuard < 50)
                {
                    word = dataManager.GetRandomTarget();
                    safeGuard++;
                }

                _missionList.Add(word);
            }

            Debug.Log($"[UnifiedGameManager] Generated {_missionList.Count} missions");
        }

        private void CreateChecklistUI()
        {
            if (checklistContent == null || missionItemPrefab == null)
            {
                Debug.LogWarning("[UnifiedGameManager] Checklist UI not configured for Study mode");
                return;
            }

            // Auto-add Layout Group if missing
            if (checklistContent.GetComponent<VerticalLayoutGroup>() == null)
            {
                var vlg = checklistContent.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childControlHeight = false;
                vlg.childControlWidth = true;
                vlg.spacing = 10;
                vlg.padding = new RectOffset(10, 10, 10, 10);
            }

            if (checklistContent.GetComponent<ContentSizeFitter>() == null)
            {
                var csf = checklistContent.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Create mission items
            for (int i = 0; i < _missionList.Count; i++)
            {
                var word = _missionList[i];
                GameObject itemObj = Instantiate(missionItemPrefab, checklistContent);

                MissionItemUI itemUI = itemObj.GetComponent<MissionItemUI>();
                if (itemUI != null)
                {
                    string displayContent = GenerateMissionDescription(word);
                    itemUI.Setup(i + 1, word.idvalue, displayContent);
                    _missionUiMap[word.idvalue] = itemUI;
                }
            }
        }

        private string GenerateMissionDescription(WordModel word)
        {
            int missionType = Random.Range(0, 4);

            return missionType switch
            {
                0 => $"Dịch thuật ngữ: {word.apologetic}",
                1 => dataManager.SentencesEN.TryGetValue(word.idvalue, out string enSentence)
                    ? $"Complete (EN): {enSentence}"
                    : $"______ define as {word.stringvalue}",
                2 => dataManager.SentencesVN.TryGetValue(word.idvalue, out string vnSentence)
                    ? $"Hoàn thành (VN): {vnSentence}"
                    : $"______ có nghĩa là {word.apologetic}",
                3 => $"Đoán từ: {MaskString(word.stringvalue)}",
                _ => word.apologetic
            };
        }

        private void SetupMissionUI()
        {
            if (_currentTarget == null) return;

            string title = "";
            string content = "";

            switch (_currentMissionType)
            {
                case MissionType.TargetWord_VN:
                    title = "DỊCH THUẬT NGỮ:";
                    content = _currentTarget.apologetic;
                    break;

                case MissionType.Sentence_EN:
                    title = "HOÀN THÀNH CÂU (EN):";
                    content = dataManager.SentencesEN.TryGetValue(_currentTarget.idvalue, out string enSentence)
                        ? enSentence
                        : $"______ define as {_currentTarget.stringvalue}";
                    break;

                case MissionType.Sentence_VN:
                    title = "HOÀN THÀNH CÂU (VN):";
                    content = dataManager.SentencesVN.TryGetValue(_currentTarget.idvalue, out string vnSentence)
                        ? vnSentence
                        : $"______ có nghĩa là {_currentTarget.apologetic}";
                    break;

                case MissionType.MissingChar:
                    title = "ĐOÁN TỪ BỊ KHUYẾT:";
                    content = MaskString(_currentTarget.stringvalue);
                    break;
            }

            if (missionTitleText != null) missionTitleText.SetText(title);
            if (questionContentText != null) questionContentText.SetText(content);
        }

        private string MaskString(string origin)
        {
            if (string.IsNullOrEmpty(origin)) return "";

            StringBuilder sb = new StringBuilder(origin);
            int hiddenCount = Mathf.Max(1, origin.Length / 3);

            for (int i = 0; i < hiddenCount; i++)
            {
                int randomIndex = Random.Range(1, origin.Length);
                if (sb[randomIndex] != '_')
                {
                    sb[randomIndex] = '_';
                }
            }

            return sb.ToString();
        }

        #endregion

        #region Token & Slot Management

        private void SpawnTokens(WordModel targetWord)
        {
            Debug.Log($"[GameManager] === SpawnTokens() BEGIN === Target: {(targetWord != null ? targetWord.stringvalue : "NULL")}");
            
            if (targetWord == null)
            {
                Debug.LogError("[GameManager] SpawnTokens called with NULL targetWord!");
                return;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[GameManager] cardPrefab is NULL! Cannot spawn tokens!");
                return;
            }

            if (tokenSpawnArea == null)
            {
                Debug.LogError("[GameManager] tokenSpawnArea is NULL! Cannot spawn tokens!");
                return;
            }

            List<WordModel> finalTokenList = new List<WordModel>();

            // Get correct components for target word
            List<WordModel> allSourceWords = new List<WordModel>();
            if (dataManager.PrefixList != null) allSourceWords.AddRange(dataManager.PrefixList);
            if (dataManager.RootList != null) allSourceWords.AddRange(dataManager.RootList);
            if (dataManager.SuffixList != null) allSourceWords.AddRange(dataManager.SuffixList);

            Debug.Log($"[GameManager] Total source words available: {allSourceWords.Count}");

            foreach (var word in allSourceWords)
            {
                if (word != null && !string.IsNullOrEmpty(word.idvalue))
                {
                    if (targetWord.idvalue.Contains(word.idvalue))
                    {
                        if (!finalTokenList.Exists(x => x.idvalue == word.idvalue))
                        {
                            finalTokenList.Add(word);
                            Debug.Log($"[GameManager] Added correct component: {word.stringvalue} ({word.idvalue})");
                        }
                    }
                }
            }

            Debug.Log($"[GameManager] Correct components found: {finalTokenList.Count}");

            // Add distractor tokens
            int slotsToFill = 6 - finalTokenList.Count;
            if (slotsToFill > 0)
            {
                Debug.Log($"[GameManager] Adding {slotsToFill} distractor tokens...");
                var randomTokens = dataManager.GetRandomTokens(slotsToFill + 4);
                foreach (var randomWord in randomTokens)
                {
                    if (finalTokenList.Count < 6 && !finalTokenList.Exists(x => x.idvalue == randomWord.idvalue))
                    {
                        finalTokenList.Add(randomWord);
                    }
                }
            }

            Debug.Log($"[GameManager] Final token list size: {finalTokenList.Count}");

            // Shuffle
            finalTokenList = finalTokenList.OrderBy(x => Random.value).ToList();

            // Instantiate cards
            int spawnedCount = 0;
            foreach (var model in finalTokenList)
            {
                if (model == null) continue;

                GameObject card = Instantiate(cardPrefab, tokenSpawnArea);
                spawnedCount++;

                var cardValue = card.GetComponent<PuzzleCardValue>();
                if (cardValue != null)
                {
                    cardValue.Initialize(model);
                }

                // Ensure drag component exists
                if (card.GetComponent<PuzzleDragObject>() == null)
                {
                    card.AddComponent<PuzzleDragObject>();
                }
            }
            
            Debug.Log($"[GameManager] ✅ Spawned {spawnedCount} tokens in {tokenSpawnArea.name}");
            Debug.Log("[GameManager] === SpawnTokens() END ===");
        }

        public void CheckSlots()
        {
            if (!_isGameActive) return;

            string combinedID = "";

            foreach (Transform slot in slotTransforms)
            {
                if (slot.childCount > 0)
                {
                    var cardValue = slot.GetChild(0).GetComponent<PuzzleCardValue>();
                    if (cardValue != null)
                    {
                        combinedID += cardValue.GetModel().getcode();
                    }
                }
            }

            if (_currentTarget != null && combinedID == _currentTarget.idvalue)
            {
                OnCorrectAnswer();
            }
        }

        private void OnCorrectAnswer()
        {
            int baseScore = 100;
            int timeBonus = Mathf.FloorToInt(_currentTime);
            _currentScore += baseScore + timeBonus;

            UpdateScoreUI();

            if (gameMode == GameMode.Study)
            {
                // Mark mission as completed
                if (!_completedMissionIds.Contains(_currentTarget.idvalue))
                {
                    _completedMissionIds.Add(_currentTarget.idvalue);

                    if (_missionUiMap.TryGetValue(_currentTarget.idvalue, out var itemUI))
                    {
                        itemUI.MarkCompleted();
                    }

                    UpdateProgressUI();
                }

                PickNewActiveMissionAndSpawn();
            }
            else
            {
                // Puzzle mode - continuous waves
                StartNewWave();
            }
        }

        #endregion

        #region UI Updates

        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.SetText($"Score: {_currentScore}");
            }
        }

        private void UpdateTimeUI()
        {
            if (timeText != null)
            {
                int displayTime = Mathf.CeilToInt(Mathf.Max(0, _currentTime));
                timeText.SetText(displayTime.ToString());
            }
        }

        private void UpdateProgressUI()
        {
            if (progressText != null && gameMode == GameMode.Study)
            {
                progressText.SetText($"{_completedMissionIds.Count} / {_missionList.Count}");
            }
        }

        #endregion

        #region User Actions

        private void OnResetClick()
        {
            if (gameMode == GameMode.Study)
            {
                // Study mode: skip to next mission
                if (_isGameActive)
                {
                    PickNewActiveMissionAndSpawn();
                }
            }
            else
            {
                // Puzzle mode: return cards to spawn area
                foreach (Transform slot in slotTransforms)
                {
                    if (slot.childCount > 0)
                    {
                        Transform card = slot.GetChild(0);
                        card.SetParent(tokenSpawnArea);
                        card.localPosition = Vector3.zero;
                        card.localRotation = Quaternion.identity;
                        card.localScale = Vector3.one;
                    }
                }
            }
        }

        private void ClearBoard()
        {
            foreach (Transform child in tokenSpawnArea)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform slot in slotTransforms)
            {
                if (slot.childCount > 0)
                {
                    Destroy(slot.GetChild(0).gameObject);
                }
            }
        }

        private void EndGame()
        {
            _isGameActive = false;

            if (gameMode == GameMode.Study)
            {
                if (progressText != null) progressText.SetText("DONE!");
                if (gameoverPanel != null) gameoverPanel.SetActive(true);
            }
            else
            {
                if (missionTitleText != null) missionTitleText.SetText("GAME OVER");
                if (questionContentText != null) questionContentText.SetText($"Final Score: {_currentScore}");
            }

            Debug.Log($"[UnifiedGameManager] Game Over - Mode: {gameMode}, Score: {_currentScore}");
        }

        #endregion

        #region Public API

        public void Retry()
        {
            if (gameoverPanel != null) gameoverPanel.SetActive(false);

            if (gameMode == GameMode.Study)
            {
                StartStudySession();
            }
            else
            {
                StartPuzzleSession();
            }
        }

        public void Menu()
        {
            SceneManager.LoadScene("StartMenu");
        }
        public void Lobby()
        {
            SceneManager.LoadScene("Lobby");
        }   
 public void StartPuzzleGame()
        {
        SceneManager.LoadScene("MainGame");
        }
        public void StudyMode()
        {
            SceneManager.LoadScene("StudyWord");
        }


        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("[StartMenu] Quit requested (ignored in editor)");
#else
            Application.Quit();
#endif
        }
        public void ShowMissionDetail(string content)
        {
            if (questionContentText != null)
            {
                questionContentText.SetText(content);
            }
        }

        /// <summary>
        /// Setup scene-specific references when scene loads.
        /// Called by SceneSetup component in each gameplay scene.
        /// </summary>
        public void SetupSceneReferences(
            TextMeshProUGUI score,
            TextMeshProUGUI time,
            TextMeshProUGUI title,
            TextMeshProUGUI content,
            Transform tokenArea,
            Transform[] slots,
            GameObject prefab,
            Button reset,
            Transform checklist = null,
            GameObject missionPrefab = null,
            TextMeshProUGUI progress = null,
            GameObject gameoverPnl = null,
            CardDataManager dataMgr = null)
        {
            // Assign common references
            scoreText = score;
            timeText = time;
            missionTitleText = title;
            questionContentText = content;
            tokenSpawnArea = tokenArea;
            slotTransforms = slots;
            cardPrefab = prefab;
            resetButton = reset;

            // Assign data manager if provided
            if (dataMgr != null)
            {
                dataManager = dataMgr;
            }

            // Assign study mode references if provided
            if (checklist != null) checklistContent = checklist;
            if (missionPrefab != null) missionItemPrefab = missionPrefab;
            if (progress != null) progressText = progress;
            if (gameoverPnl != null) gameoverPanel = gameoverPnl;

            // Re-setup button listener with new reference
            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(OnResetClick);
            }

            Debug.Log("[GameManager] Scene references configured successfully!");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
#if UNITY_EDITOR
            string currentScene = SceneManager.GetActiveScene().name;
            
            // Skip UI validation in menu scenes only
            bool skipValidation = currentScene == "StartMenu" || currentScene == "Lobby";
            
            if (skipValidation)
            {
                Debug.Log($"[GameManager] In {currentScene} - Skipping UI validation");
                return;
            }
            
            // Validate UI references (only in gameplay scenes)
            if (dataManager == null)
                Debug.LogError("[GameManager] CardDataManager not assigned!", this);

            if (tokenSpawnArea == null)
                Debug.LogError("[GameManager] TokenSpawnArea not assigned!", this);

            if (slotTransforms == null || slotTransforms.Length == 0)
                Debug.LogError("[GameManager] SlotTransforms not assigned!", this);

            if (cardPrefab == null)
                Debug.LogError("[GameManager] CardPrefab not assigned!", this);

            if (gameMode == GameMode.Study)
            {
                if (checklistContent == null)
                    Debug.LogWarning("[GameManager] ChecklistContent not assigned for Study mode!", this);

                if (missionItemPrefab == null)
                    Debug.LogWarning("[GameManager] MissionItemPrefab not assigned for Study mode!", this);
            }
#endif
        }

        #endregion
    }
}
