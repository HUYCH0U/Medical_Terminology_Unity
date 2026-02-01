using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using UnityEngine.SceneManagement;

public class PuzzleGameManager : MonoBehaviour
{
    public static PuzzleGameManager Instance;

    public enum MissionType
    {
        TargetWord_VN,
        Sentence_EN,
        Sentence_VN,
        MissingChar
    }

    [Header("UI References")]
    public TextMeshProUGUI MissionTitleText;
    public TextMeshProUGUI QuestionContentText;
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI TimeText;
    public Transform TokenSpawnArea;
    public Transform[] SlotTransforms;
    public GameObject CardPrefab;
    public Button ResetButton;

    [Header("Game Settings")]
    public float LevelTime = 120f;
    
    private float currentTime;
    private int currentScore;
    private WordModel currentTarget;
    private bool isGameActive;
    private MissionType currentMissionType;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        StartCoroutine(StartGameDelayed());
        ResetButton.onClick.AddListener(OnResetClick);
    }

    IEnumerator StartGameDelayed()
    {
        yield return null;
        StartNewWave();
    }

    void Update()
    {
        if (!isGameActive) return;

        currentTime -= Time.deltaTime;
        TimeText.text = Mathf.CeilToInt(currentTime).ToString();

        if (currentTime <= 0) EndGame();
    }

    public void StartNewWave()
    {
        ClearBoard();
        
        currentTarget = PuzzleDataManager.Instance.GetRandomTarget();
        if (currentTarget == null)
        {
            Debug.LogError("No Target Found!");
            return;
        }

        currentMissionType = (MissionType)Random.Range(0, 4);

        SetupMissionUI();
        SpawnTokens();

        currentTime = LevelTime;
        isGameActive = true;
    }

    private void SetupMissionUI()
    {
        string displayContent = "";
        string title = "";

        switch (currentMissionType)
        {
            case MissionType.TargetWord_VN:
                title = "DỊCH THUẬT NGỮ:";
                displayContent = currentTarget.apologetic;
                break;

            case MissionType.Sentence_EN:
                title = "HOÀN THÀNH CÂU (EN):";
                if (PuzzleDataManager.Instance.SentencesEN.TryGetValue(currentTarget.idvalue, out string enSentence))
                    displayContent = enSentence;
                else
                    displayContent = $"______ define as {currentTarget.stringvalue}";
                break;

            case MissionType.Sentence_VN:
                title = "HOÀN THÀNH CÂU (VN):";
                if (PuzzleDataManager.Instance.SentencesVN.TryGetValue(currentTarget.idvalue, out string vnSentence))
                    displayContent = vnSentence;
                else
                    displayContent = $"______ có nghĩa là {currentTarget.apologetic}";
                break;

            case MissionType.MissingChar:
                title = "ĐOÁN TỪ BỊ KHUYẾT:";
                displayContent = MaskString(currentTarget.stringvalue);
                break;
        }

        MissionTitleText.text = title;
        QuestionContentText.text = displayContent;
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

    private void SpawnTokens()
    {
        List<WordModel> finalTokenList = new List<WordModel>();

        List<WordModel> allSourceWords = new List<WordModel>();
        if (PuzzleDataManager.Instance.PrefixList != null) allSourceWords.AddRange(PuzzleDataManager.Instance.PrefixList);
        if (PuzzleDataManager.Instance.RootList != null) allSourceWords.AddRange(PuzzleDataManager.Instance.RootList);
        if (PuzzleDataManager.Instance.SuffixList != null) allSourceWords.AddRange(PuzzleDataManager.Instance.SuffixList);

        foreach (var word in allSourceWords)
        {
            if (word != null && !string.IsNullOrEmpty(word.idvalue))
            {
                if (currentTarget.idvalue.Contains(word.idvalue))
                {
                    if (!finalTokenList.Exists(x => x.idvalue == word.idvalue))
                    {
                        finalTokenList.Add(word);
                    }
                }
            }
        }

        int slotsToFill = 6 - finalTokenList.Count;
        
        if (slotsToFill > 0)
        {
            List<WordModel> randomTokens = PuzzleDataManager.Instance.GetRandomTokens(slotsToFill * 2);
            
            foreach (var randomWord in randomTokens)
            {
                if (finalTokenList.Count < 6 && !finalTokenList.Exists(x => x.idvalue == randomWord.idvalue))
                {
                    finalTokenList.Add(randomWord);
                }
            }
        }

        finalTokenList = finalTokenList.OrderBy(x => Random.value).ToList();

        foreach (var model in finalTokenList)
        {
            if (model == null) continue;
            GameObject card = Instantiate(CardPrefab, TokenSpawnArea);
            
            PuzzleCardValue cv = card.GetComponent<PuzzleCardValue>();
            if (cv != null) cv.Initialize(model);
            
            if (card.GetComponent<PuzzleDragObject>() == null)
                card.AddComponent<PuzzleDragObject>();
        }
    }

    public void CheckSlots()
    {
        string combinedID = "";
        
        foreach (Transform slot in SlotTransforms)
        {
            if (slot.childCount > 0)
            {
                PuzzleCardValue cv = slot.GetChild(0).GetComponent<PuzzleCardValue>();
                if (cv != null) combinedID += cv.GetModel().getcode();
            }
        }

        if (currentTarget != null && combinedID == currentTarget.idvalue)
        {
            OnCorrectAnswer();
        }
    }

    private void OnCorrectAnswer()
    {
        currentScore += 100 + (int)currentTime;
        ScoreText.text = "Score: " + currentScore;
        StartNewWave();
    }

    private void OnResetClick()
    {
        foreach (Transform slot in SlotTransforms)
        {
            if (slot.childCount > 0)
            {
                Transform card = slot.GetChild(0);
                card.SetParent(TokenSpawnArea);
                card.localPosition = Vector3.zero;
                card.localRotation = Quaternion.identity;
                card.localScale = Vector3.one;
            }
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

    private void EndGame()
    {
        isGameActive = false;
        MissionTitleText.text = "GAME OVER";
        QuestionContentText.text = "Final Score: " + currentScore;
    }
     public void Menu(){
        SceneManager.LoadScene("StartMenu");
    }
}