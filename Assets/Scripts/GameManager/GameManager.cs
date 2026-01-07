using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    protected MedicalTerminologyPool medicalTerminologyPool;
    protected bool EndGameFlag = false;
    protected int CardSlot;
    protected GameConfig gameConfig;

    [SerializeField] private GameObject[] SlotArray;
    private GameObject MainBoard;

    protected TextMeshPro TopicText;
    protected TextMeshPro ScoreText;
    protected TextMeshPro TimeText;
    protected TextMeshPro DisplayText;

    protected float turntime;
    protected WordModel Topic;

    private List<WordModel> TopicHistory = new List<WordModel>();
    private int score = 0;

    [SerializeField] private Button RestartButton;
    [SerializeField] private Button BackToMenuButton;

    [Header("Quiz UI Settings")]
    [SerializeField] private GameObject QuizPanel;
    [SerializeField] private TextMeshProUGUI QuizQuestionText;
    [SerializeField] private Button[] QuizAnswerButtons;

    private bool isPaused = false;
    private int quizBonusTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        MainBoard = GameObject.Find("MainBoard");
        medicalTerminologyPool = GetComponent<MedicalTerminologyPool>();
        gameConfig = gameObject.GetComponent<GameConfig>();
        InitializeComponent();
    }

    void Start()
    {
        gameConfig.getConfig();
        TurnManager();
    }

    void Update()
    {
        GameTurn();
    }

    private void GameTurn()
    {
        if (!EndGameFlag)
        {
            if (turntime > 0)
            {
                turntime -= Time.deltaTime;
                if (TimeText != null) TimeText.text = Mathf.CeilToInt(turntime).ToString();
            }
            else
            {
                TurnManager();
            }
        }
    }

    private void TurnManager()
    {
        if (gameConfig.gameturns > 0)
        {
            isPaused = false;
            if (QuizPanel != null) QuizPanel.SetActive(false);

            gameConfig.DecreaseTurn();
            turntime = gameConfig.turntime;

            Topic = medicalTerminologyPool.GetRandomTopic();
            while (TopicHistory.Exists(x => x.stringvalue == Topic.stringvalue))
            {
                Topic = medicalTerminologyPool.GetRandomTopic();
            }
            TopicHistory.Add(Topic);

            if (TopicText != null) TopicText.text = Topic.apologetic;
            ClearAllSlots();
        }
        else
        {
            EndGameFlag = true;
            ClearAllSlots();
            if (TopicText != null) TopicText.text = "GAMEe OVER";
            DisplayText.text = "FINAL SCORE: " + score.ToString();
            RestartButton.gameObject.SetActive(true);
            BackToMenuButton.gameObject.SetActive(true);
            QuizPanel.SetActive(false);
            medicalTerminologyPool.DisableAllDropdown();
        }
    }

    private void ClearAllSlots()
    {
        for (int i = 0; i < SlotArray.Length; i++)
        {
            if (SlotArray[i] != null && SlotArray[i].transform.childCount > 0)
            {
                foreach (Transform child in SlotArray[i].transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        if (DisplayText != null) DisplayText.text = "";
    }

    public void CheckSlotForDisplay()
    {
        string displayString = "";
        string checkResult = "";
        List<WordModel> componentsInSlots = new List<WordModel>();

        for (int i = 0; i < CardSlot; i++)
        {
            if (SlotArray[i] == null) continue;

            CardValue cardInSlot = SlotArray[i].GetComponentInChildren<CardValue>();
            if (cardInSlot != null)
            {
                displayString += cardInSlot.getText() + " ";
                checkResult += cardInSlot.getCode();

                componentsInSlots.Add(cardInSlot.GetModel());
            }
        }

        if (DisplayText != null) DisplayText.text = displayString.Trim();

        if (Topic != null && checkResult == Topic.idvalue && !isPaused)
        {
            PrepareQuiz(componentsInSlots);
        }
    }

    private void PrepareQuiz(List<WordModel> components)
    {
        if (components == null || components.Count == 0) return;

        isPaused = true;
        QuizPanel.SetActive(true);

        WordModel selectedComp = components[Random.Range(0, components.Count)];

        QuizQuestionText.text = selectedComp.stringvalue + " có nghĩa là gì?";

        string correctAnswer = selectedComp.apologetic;

        List<string> finalAnswers = medicalTerminologyPool.GetRandomWrongAnswers(selectedComp.type, correctAnswer, 2);
        finalAnswers.Add(correctAnswer);

        finalAnswers = finalAnswers.OrderBy(a => System.Guid.NewGuid()).ToList();

        quizBonusTime = Mathf.CeilToInt(turntime);

        for (int i = 0; i < QuizAnswerButtons.Length; i++)
        {
            if (i < finalAnswers.Count)
            {
                string choiceText = finalAnswers[i];
                QuizAnswerButtons[i].gameObject.SetActive(true);
                QuizAnswerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = choiceText;

                QuizAnswerButtons[i].onClick.RemoveAllListeners();
                QuizAnswerButtons[i].onClick.AddListener(() => OnQuizAnswerClicked(choiceText, correctAnswer));
            }
            else
            {
                QuizAnswerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnQuizAnswerClicked(string selected, string correct)
    {
        int currentTurnBonus = (int) turntime;


        if (selected == correct)
        {
            AddScore(currentTurnBonus);
        }
        else
        {
            AddScore(currentTurnBonus / 2);
        }

        TurnManager();
    }

    private void AddScore(int value)
    {
        score += value;
        if (ScoreText != null) ScoreText.text = score.ToString();
    }

    private void InitializeComponent()
    {
        CardSlot = 5;
        SlotArray = new GameObject[CardSlot];
        int count = 0;
        turntime = gameConfig.turntime;

        foreach (Transform child in MainBoard.transform)
        {
            if (child.name.ToLower().Contains("cardslot") && count < CardSlot)
            {
                SlotArray[count] = child.gameObject;
                count++;
            }
        }

        TopicText = MainBoard.transform.Find("TextValue/Topic").GetComponent<TextMeshPro>();
        ScoreText = MainBoard.transform.Find("TextValue/Score").GetComponent<TextMeshPro>();
        TimeText = MainBoard.transform.Find("TextValue/Time").GetComponent<TextMeshPro>();
        DisplayText = MainBoard.transform.Find("TextValue/Display").GetComponent<TextMeshPro>();

        if (QuizPanel != null) QuizPanel.SetActive(false);
        RestartButton.gameObject.SetActive(false);
        BackToMenuButton.gameObject.SetActive(false);
    }

    public void onrestart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
    }

    public void onbacktomenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenu");
    }
}