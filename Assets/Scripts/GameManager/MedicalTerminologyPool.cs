using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MedicalTerminologyPool : MonoBehaviour
{
    protected WordModel[] TerminologyPool;
    protected WordModel[] SurfixPool;
    protected WordModel[] PrefixPool;
    protected WordModel[] RootPool;
    private TextAsset SurfixTxt;
    private TextAsset PrefixTxt;
    private TextAsset RootTxt;
    private TextAsset TerminologyTxt;
    [SerializeField] protected GameObject CardPrefab;
    [SerializeField] protected Transform CardSpawnPoint;
    [SerializeField] private TMP_Dropdown surfixDropdown;
    [SerializeField] private TMP_Dropdown prefixDropdown;
    [SerializeField] private TMP_Dropdown rootDropdown;

    private void VocabularyBoost()
    {
        SurfixTxt = Resources.Load<TextAsset>("WordPool/Surfix");
        PrefixTxt = Resources.Load<TextAsset>("WordPool/Prefix");
        RootTxt = Resources.Load<TextAsset>("WordPool/Root");
        TerminologyTxt = Resources.Load<TextAsset>("WordPool/MedicalTerminology");
    }

    public void DisableAllDropdown()
    {
        if (surfixDropdown != null) surfixDropdown.interactable = false;
        if (prefixDropdown != null) prefixDropdown.interactable = false;
        if (rootDropdown != null) rootDropdown.interactable = false;
    }

    public WordModel GetRandomTopic()
    {
        return TerminologyPool[Random.Range(0, TerminologyPool.Length)];
    }

   public List<string> GetRandomWrongAnswers(WordModel.Type type, string correctAnswer, int amount)
{
    List<string> wrongList = new List<string>();
    WordModel[] sourcePool;

    if (type == WordModel.Type.Prefix) sourcePool = PrefixPool;
    else if (type == WordModel.Type.Suffix) sourcePool = SurfixPool;
    else sourcePool = RootPool;

    if (sourcePool == null || sourcePool.Length == 0) return wrongList;

    int safetyCounter = 0;
    while (wrongList.Count < amount && safetyCounter < 100)
    {
        int randomIndex = Random.Range(0, sourcePool.Length);
        string potentialAnswer = sourcePool[randomIndex].apologetic;

        if (potentialAnswer != correctAnswer && !wrongList.Contains(potentialAnswer))
        {
            wrongList.Add(potentialAnswer);
        }
        safetyCounter++;
    }
    return wrongList;
}

    private void ResourcesLoader(ref WordModel[] Model, TextAsset textAsset, WordModel.Type type)
    {
        if (textAsset == null) return;

        string[] data = textAsset.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        List<WordModel> validWords = new List<WordModel>();

        foreach (string lineData in data)
        {
            string[] line = lineData.Split(',');
            if (line.Length == 3)
            {
                WordModel word = new WordModel();
                word.Initialize(line[0].Trim(), line[1].Trim(), line[2].Trim(), type);
                validWords.Add(word);
            }
        }
        Model = validWords.ToArray();
    }

    public void StartDesk()
    {
        VocabularyBoost();
        ResourcesLoader(ref SurfixPool, SurfixTxt, WordModel.Type.Suffix);
        ResourcesLoader(ref PrefixPool, PrefixTxt, WordModel.Type.Prefix);
        ResourcesLoader(ref RootPool, RootTxt, WordModel.Type.Root);
        ResourcesLoader(ref TerminologyPool, TerminologyTxt, WordModel.Type.Terminology);
    }

    private void LoadOptionForDropDown(TMP_Dropdown DropDown, WordModel[] wordModels, string HeaderTitle)
    {
        if (DropDown == null) return;
        DropDown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData(HeaderTitle));
        if (wordModels != null)
        {
            foreach (WordModel word in wordModels)
            {
                if (word != null && !string.IsNullOrEmpty(word.stringvalue))
                {
                    options.Add(new TMP_Dropdown.OptionData(word.stringvalue));
                }
            }
        }
        DropDown.AddOptions(options);
        SpwanCardListener(DropDown, wordModels);

    }

    private void SpwanCardListener(TMP_Dropdown DropDown, WordModel[] wordModels)
    {
        DropDown.onValueChanged.AddListener((index) =>
        {
            if (index == 0) return;
            CreateCard(wordModels[index - 1]);
            DropDown.value = 0;
            DropDown.RefreshShownValue();
        });
    }

    private void LoadCardForDropDown()
    {
        if (surfixDropdown == null) surfixDropdown = GameObject.Find("Surfix")?.GetComponent<TMP_Dropdown>();
        if (prefixDropdown == null) prefixDropdown = GameObject.Find("Prefix")?.GetComponent<TMP_Dropdown>();
        if (rootDropdown == null) rootDropdown = GameObject.Find("Root")?.GetComponent<TMP_Dropdown>();
        LoadOptionForDropDown(surfixDropdown, SurfixPool, "SURFIX");
        LoadOptionForDropDown(prefixDropdown, PrefixPool, "PREFIX");
        LoadOptionForDropDown(rootDropdown, RootPool, "ROOT  ");
    }

    void Start()
    {
        StartDesk();
        LoadCardForDropDown();
    }

    public void CreateCard(WordModel model)
    {
        if (model == null || CardSpawnPoint.childCount >= 5) return;
        GameObject newCard = Instantiate(CardPrefab, CardSpawnPoint);
        CardValue cardValue = newCard.GetComponentInChildren<CardValue>();
        if (cardValue != null)
        {
            cardValue.Initialize(model);
        }
    }

}
