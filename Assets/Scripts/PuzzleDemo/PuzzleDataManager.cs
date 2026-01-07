using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PuzzleDataManager : MonoBehaviour
{
    public static PuzzleDataManager Instance;
    
    public List<WordModel> PrefixList = new List<WordModel>();
    public List<WordModel> RootList = new List<WordModel>();
    public List<WordModel> SuffixList = new List<WordModel>();
    public List<WordModel> TerminologyList = new List<WordModel>();

    public Dictionary<string, string> SentencesEN = new Dictionary<string, string>();
    public Dictionary<string, string> SentencesVN = new Dictionary<string, string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        LoadAllData();
    }

    private void LoadAllData()
    {
        LoadWordFile("WordPool/Prefix", PrefixList, WordModel.Type.Prefix);
        LoadWordFile("WordPool/Root", RootList, WordModel.Type.Root);
        LoadWordFile("WordPool/Surfix", SuffixList, WordModel.Type.Suffix);
        LoadWordFile("WordPool/MedicalTerminology", TerminologyList, WordModel.Type.Terminology);

        LoadSentenceFile("WordPool/SentencesEN", SentencesEN);
        LoadSentenceFile("WordPool/SentencesVN", SentencesVN);
    }

    private void LoadWordFile(string path, List<WordModel> list, WordModel.Type type)
    {
        TextAsset file = Resources.Load<TextAsset>(path);
        if (file == null) 
        {
            Debug.LogError("Missing file: " + path);
            return;
        }

        string[] lines = file.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 3)
            {
                WordModel word = new WordModel();
                word.Initialize(parts[0].Trim(), parts[1].Trim(), parts[2].Trim(), type);
                list.Add(word);
            }
        }
    }

    private void LoadSentenceFile(string path, Dictionary<string, string> dict)
    {
        TextAsset file = Resources.Load<TextAsset>(path);
        if (file == null) 
        {
            Debug.LogError("Missing file: " + path);
            return;
        }

        string[] lines = file.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string[] parts = line.Split('|');
            if (parts.Length >= 2)
            {
                string id = parts[0].Trim();
                string sentence = parts[1].Trim();
                if (!dict.ContainsKey(id)) 
                {
                    dict.Add(id, sentence);
                }
            }
        }
    }

    public WordModel GetRandomTarget()
    {
        if (TerminologyList.Count == 0) return null;
        return TerminologyList[Random.Range(0, TerminologyList.Count)];
    }

    public List<WordModel> GetRandomTokens(int count)
    {
        List<WordModel> pool = new List<WordModel>();
        pool.AddRange(PrefixList.OrderBy(x => Random.value).Take(count));
        pool.AddRange(RootList.OrderBy(x => Random.value).Take(count));
        pool.AddRange(SuffixList.OrderBy(x => Random.value).Take(count));
        return pool.OrderBy(x => Random.value).Take(count).ToList();
    }
}