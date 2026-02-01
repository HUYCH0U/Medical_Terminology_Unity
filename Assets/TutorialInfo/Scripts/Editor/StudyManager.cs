using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Text;
using UnityEngine.SceneManagement;

public class StudyManager : MonoBehaviour
{
    public static StudyManager Instance;

    [Header("UI References")]
    public Transform ChecklistContent;      // Content của ScrollView
    public GameObject MissionItemPrefab;    // Prefab chứa MissionItemUI
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI TimeText;        // Giống PuzzleGameManager
    public TextMeshProUGUI ProgressText;    // Hiển thị tiến độ, ví dụ "3/10"
    public TextMeshProUGUI BigQuestionText; // TEXT HIỆN CÂU HỎI TO GIỮA MÀN HÌNH
    public Transform TokenSpawnArea;
    public Transform[] SlotTransforms;
    public GameObject CardPrefab;
    public Button ResetButton;
    public GameObject GameoverPanel;

    [Header("Game Settings")]
    public float LevelTime = 300f; // Thời gian cho cả bài học
    public int MissionCount = 5;   // Số lượng câu hỏi trong 1 bài học

    // State Variables
    private float currentTime;
    private int currentScore;
    private bool isGameActive;

    // Logic cho Study Mode
    private List<WordModel> _missionList = new List<WordModel>();
    private HashSet<string> _completedMissionIds = new HashSet<string>();
    private WordModel _currentHiddenTarget; // Target đang được chọn ngầm để sinh token
    private Dictionary<string, MissionItemUI> _missionUiMap = new Dictionary<string, MissionItemUI>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        ResetButton.onClick.AddListener(OnResetClick);
        StartCoroutine(StartGameDelayed());
    }

    IEnumerator StartGameDelayed()
    {
        yield return null; 
        // Đảm bảo Data đã load
        if (PuzzleDataManager.Instance != null && PuzzleDataManager.Instance.TerminologyList.Count > 0)
        {
            StartNewSession();
        }
        else
        {
            Debug.LogError("Data not ready or empty!");
        }
    }

    void Update()
    {
        if (!isGameActive) return;

        currentTime -= Time.deltaTime;
        if (TimeText != null) TimeText.text = Mathf.CeilToInt(currentTime).ToString();

        if (currentTime <= 0) EndGame();
    }

    public void StartNewSession()
    {
        // 1. Reset dữ liệu cũ
        currentScore = 0;
        currentTime = LevelTime;
        _completedMissionIds.Clear();
        _missionUiMap.Clear();
        isGameActive = true;

        foreach (Transform child in ChecklistContent) Destroy(child.gameObject);
        ClearBoard();

        // 2. Lấy danh sách nhiệm vụ ngẫu nhiên
        GenerateMissionList();

        // 3. Tạo UI Checklist
        CreateChecklistUI();

        // Update UI điểm/tiến độ ban đầu
        if (ScoreText != null) ScoreText.text = "Score: 0";
        UpdateProgressUI();

        // 4. Bắt đầu round đầu tiên (chọn ngầm 1 câu để sinh token)
        PickNewActiveMissionAndSpawn();
    }

    private void GenerateMissionList()
    {
        _missionList = new List<WordModel>();
        for (int i = 0; i < MissionCount; i++)
        {
            WordModel word = PuzzleDataManager.Instance.GetRandomTarget();
            if (word == null) continue;

            int safeGuard = 0;
            while (_missionList.Exists(x => x.idvalue == word.idvalue) && safeGuard < 50)
            {
                word = PuzzleDataManager.Instance.GetRandomTarget();
                safeGuard++;
            }
            _missionList.Add(word);
        }
        Debug.Log($"StudyManager: Generated {_missionList.Count} missions.");
    }

    private void CreateChecklistUI()
    {
        if (ChecklistContent == null || MissionItemPrefab == null)
        {
            Debug.LogError("ChecklistContent or MissionItemPrefab is null inside StudyManager");
            return;
        }

        // --- AUTO FIX LAYOUT: Tự động thêm Layout Group nếu user quên ---
        VerticalLayoutGroup vlg = ChecklistContent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
             vlg = ChecklistContent.gameObject.AddComponent<VerticalLayoutGroup>();
             vlg.childControlHeight = false; 
             vlg.childControlWidth = true;
             vlg.childForceExpandHeight = false;
             vlg.childForceExpandWidth = true;
             vlg.spacing = 10; // Khoảng cách giữa các item
             vlg.padding = new RectOffset(10, 10, 10, 10);
        }

        ContentSizeFitter csf = ChecklistContent.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
             csf = ChecklistContent.gameObject.AddComponent<ContentSizeFitter>();
             csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        // -------------------------------------------------------------

        for (int i = 0; i < _missionList.Count; i++)
        {
            var word = _missionList[i];
            GameObject itemObj = Instantiate(MissionItemPrefab, ChecklistContent);
            
            // Để Layout Group tự xử lý vị trí. Không tự cộng trừ tọa độ ở đây.
            itemObj.transform.localPosition = Vector3.zero;
            itemObj.transform.localRotation = Quaternion.identity;
            itemObj.transform.localScale = Vector3.one;

            MissionItemUI itemUI = itemObj.GetComponent<MissionItemUI>();
            
            if (itemUI != null)
            {
                string displayContent = "";
                
                // Random loại câu hỏi
                // 0: Translate VN, 1: Sentence EN, 2: Sentence VN, 3: Missing Char
                int missionType = Random.Range(0, 4);

                switch (missionType)
                {
                    case 0: // Dịch thuật ngữ
                        displayContent = $"Dịch thuật ngữ: {word.apologetic}";
                        break;
                    
                    case 1: // Câu tiếng Anh
                        if (PuzzleDataManager.Instance.SentencesEN.TryGetValue(word.idvalue, out string enSentence))
                            displayContent = $"Complete (EN): {enSentence}";
                        else
                            displayContent = $"______ define as {word.stringvalue}";
                        break;

                    case 2: // Câu tiếng Việt
                        if (PuzzleDataManager.Instance.SentencesVN.TryGetValue(word.idvalue, out string vnSentence))
                            displayContent = $"Hoàn thành (VN): {vnSentence}";
                        else
                            displayContent = $"______ có nghĩa là {word.apologetic}";
                        break;

                    case 3: // Đục lỗ
                        displayContent = $"Đoán từ: {MaskString(word.stringvalue)}";
                        break;
                }

                // Nếu displayContent vẫn rỗng (do lỗi data), fallback về nghĩa gốc
                if (string.IsNullOrEmpty(displayContent)) displayContent = word.apologetic;

                // Truyền i + 1 để đánh số từ 1, 2, 3...
                itemUI.Setup(i + 1, word.idvalue, displayContent);
                _missionUiMap.Add(word.idvalue, itemUI);
            }
        }
    }

    private string MaskString(string origin)
    {
        StringBuilder sb = new StringBuilder(origin);
        int hiddenCount = Mathf.Max(1, origin.Length / 3);
        
        for (int i = 0; i < hiddenCount; i++)
        {
            int randomIndex = Random.Range(1, origin.Length);
            if (sb[randomIndex] != '_')
                sb[randomIndex] = '_';
        }
        return sb.ToString();
    }

    // Logic chọn ngầm 1 nhiệm vụ chưa hoàn thành
    private void PickNewActiveMissionAndSpawn()
    {
        ClearBoard(); // Xóa bài cũ

        // Tìm các mission chưa xong
        var pendingMissions = _missionList.Where(x => !_completedMissionIds.Contains(x.idvalue)).ToList();
        
        if (pendingMissions.Count == 0)
        {
            EndGame(); // Thắng
            return;
        }

        // Random chọn 1 cái làm Target
        _currentHiddenTarget = pendingMissions[Random.Range(0, pendingMissions.Count)];
        
        // Debug
        // Debug.Log("Hidden Target: " + _currentHiddenTarget.idvalue);

        // Sinh token dựa trên Target này
        SpawnTokens(_currentHiddenTarget);
    }

    // Logic sinh Token (Giống hệt PuzzleGameManager nhưng nhận vào Target cụ thể)
    private void SpawnTokens(WordModel targetWord)
    {
        List<WordModel> finalTokenList = new List<WordModel>();

        // Lấy tất cả component có thể có
        List<WordModel> allSourceWords = new List<WordModel>();
        if (PuzzleDataManager.Instance.PrefixList != null) allSourceWords.AddRange(PuzzleDataManager.Instance.PrefixList);
        if (PuzzleDataManager.Instance.RootList != null) allSourceWords.AddRange(PuzzleDataManager.Instance.RootList);
        if (PuzzleDataManager.Instance.SuffixList != null) allSourceWords.AddRange(PuzzleDataManager.Instance.SuffixList);

        // Tìm các component tạo nên targetWord
        foreach (var word in allSourceWords)
        {
            if (word != null && !string.IsNullOrEmpty(word.idvalue))
            {
                 // Logic check đơn giản: nếu ID thành phần nằm trong ID tổng
                if (targetWord.idvalue.Contains(word.idvalue))
                {
                    if (!finalTokenList.Exists(x => x.idvalue == word.idvalue))
                    {
                        finalTokenList.Add(word);
                    }
                }
            }
        }

        // Thêm Token nhiễu (cho đủ 6 slot)
        int slotsToFill = 6 - finalTokenList.Count;
        if (slotsToFill > 0)
        {
            List<WordModel> randomTokens = PuzzleDataManager.Instance.GetRandomTokens(slotsToFill + 4); // Lấy dư ra chút
            foreach (var randomWord in randomTokens)
            {
                if (finalTokenList.Count < 6 && !finalTokenList.Exists(x => x.idvalue == randomWord.idvalue))
                {
                    finalTokenList.Add(randomWord);
                }
            }
        }

        // Xáo trộn vị trí
        finalTokenList = finalTokenList.OrderBy(x => Random.value).ToList();

        // Instantiate Card
        foreach (var model in finalTokenList)
        {
            if (model == null) continue;
            GameObject card = Instantiate(CardPrefab, TokenSpawnArea);
            
            PuzzleCardValue cv = card.GetComponent<PuzzleCardValue>();
            if (cv != null) cv.Initialize(model);
            
            // Đảm bảo có script kéo thả
            if (card.GetComponent<PuzzleDragObject>() == null)
                card.AddComponent<PuzzleDragObject>();
        }
    }

    public void CheckSlots()
    {
        if (!isGameActive) return;

        string combinedID = "";
        
        foreach (Transform slot in SlotTransforms)
        {
            if (slot.childCount > 0)
            {
                PuzzleCardValue cv = slot.GetChild(0).GetComponent<PuzzleCardValue>();
                if (cv != null) combinedID += cv.GetModel().getcode();
            }
        }

        // Kiểm tra với Target ngầm hiện tại
        if (_currentHiddenTarget != null && combinedID == _currentHiddenTarget.idvalue)
        {
            OnCorrectAnswer(_currentHiddenTarget.idvalue);
        }
    }

    private void OnCorrectAnswer(string wordId)
    {
        // Update data
        if (!_completedMissionIds.Contains(wordId))
        {
            _completedMissionIds.Add(wordId);
            
            // Cộng điểm: ví dụ 100 điểm + bonus thời gian còn lại của bài
            currentScore += 100;
            if (ScoreText != null) ScoreText.text = "Score: " + currentScore;
        }

        // Update UI Checklist (Check mark)
        if (_missionUiMap.ContainsKey(wordId))
        {
            _missionUiMap[wordId].MarkCompleted();
        }

        UpdateProgressUI();

        // Chuyển sang câu tiếp theo
        PickNewActiveMissionAndSpawn();
    }

    private void OnResetClick()
    {
        // Đổi câu hỏi khác nếu còn
        if (isGameActive)
        {
            PickNewActiveMissionAndSpawn();
        }
    }

    private void ClearBoard()
    {
        foreach (Transform child in TokenSpawnArea) Destroy(child.gameObject);
        foreach (Transform slot in SlotTransforms)
        {
            if (slot.childCount > 0) Destroy(slot.GetChild(0).gameObject);
        }
    }
    
    private void UpdateProgressUI()
    {
        if(ProgressText != null)
        {
            ProgressText.text = $"{_completedMissionIds.Count} / {_missionList.Count}";
        }
    }

    private void EndGame()
    {
        isGameActive = false;
        if(ProgressText != null) ProgressText.text = "DONE!";
        Debug.Log("Game Over or Victory! Final Score: " + currentScore);
        GameoverPanel.SetActive(true);
        // Có thể hiện Popup EndGame ở đây
    }

    public void Retry(){
        GameoverPanel.SetActive(false);
        PickNewActiveMissionAndSpawn();
    }

    public void Menu(){
        SceneManager.LoadScene("StartMenu");
    }

    public void ShowMissionDetail(string content)
    {
        if (BigQuestionText != null)
        {
            BigQuestionText.text = content;
        }
    }
}
