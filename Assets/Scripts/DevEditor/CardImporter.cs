using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System;

public class CardImporter : EditorWindow
{
    private string csvPathRaw = "Assets/Resources/WordData/20251024 - Nha Khoa.csv";
    private string savePathRelative = "Assets/Resources/DataSO/Cards/Biological/";

    [MenuItem("GenThuatNgu/Open Card Importer")]
    public static void ShowWindow()
    {
        GetWindow<CardImporter>("Card Importer Final");
    }

    void OnGUI()
    {
        GUILayout.Label("CÔNG CỤ NHẬP THẺ", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        csvPathRaw = EditorGUILayout.TextField("File CSV:", csvPathRaw);
        if (GUILayout.Button("Chọn", GUILayout.Width(50)))
        {
            string absPath = EditorUtility.OpenFilePanel("Chọn file CSV", "Assets", "csv");
            if (!string.IsNullOrEmpty(absPath))
            {
                if (absPath.StartsWith(Application.dataPath))
                    csvPathRaw = "Assets" + absPath.Substring(Application.dataPath.Length);
                else
                    csvPathRaw = absPath;
            }
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("BẮT ĐẦU TẠO THẺ", GUILayout.Height(40)))
        {
            ImportCards();
        }
    }

    void ImportCards()
    {
        string finalCsvPathToCheck = csvPathRaw;
        if (csvPathRaw.StartsWith("Assets"))
        {
            finalCsvPathToCheck = Application.dataPath + csvPathRaw.Substring(6);
        }

        if (!File.Exists(finalCsvPathToCheck))
        {
            Debug.LogError($"Không tìm thấy file CSV tại: {finalCsvPathToCheck}");
            return;
        }

        string[] lines = File.ReadAllLines(finalCsvPathToCheck);
        
        if (!AssetDatabase.IsValidFolder(savePathRelative.TrimEnd('/')))
        {
            string sysPath = Application.dataPath + savePathRelative.Substring(6);
            if (!Directory.Exists(sysPath)) Directory.CreateDirectory(sysPath);
        }

        int count = 0;
        
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] cols = SplitCsvLine(line);

            if (cols.Length < 14) 
            {
                continue; 
            }

            string stt = cols[0].Trim();
            string cardID = "CARD_" + stt;
            string termEn = cols[1].Trim();
            string termVi = cols[3].Trim();
            string defVi = cols[4].Trim();
            string termAnalysis = cols[5].Trim();

            string catStr = cols[11].Trim(); 
            Enum.TryParse(catStr, true, out CardCategory categoryVal);

            string typeStr = cols[12].Trim();
            Enum.TryParse(typeStr, true, out WordType typeVal);

            string rarStr = cols[13].Trim();
            Enum.TryParse(rarStr, true, out CardRarity rarityVal);

            string fileName = $"{cardID}_{SanitizeFileName(termEn)}.asset";
            string assetPath = savePathRelative + fileName; 

            CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(assetPath);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<CardDataSO>();
                AssetDatabase.CreateAsset(card, assetPath);
            }

            card.id = cardID;
            card.termEnglish = termEn;
            card.termVietnamese = termVi;
            
            card.structure = termAnalysis;
            card.description = defVi;

            card.category = categoryVal;
            card.wordType = typeVal;
            card.rarity = rarityVal;
            card.illustration = Resources.Load<Sprite>("CardImages/" + cardID);

            EditorUtility.SetDirty(card);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private string[] SplitCsvLine(string line)
    {
        string pattern = ",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
        string[] rawCols = Regex.Split(line, pattern);
        for (int i = 0; i < rawCols.Length; i++)
        {
            rawCols[i] = rawCols[i].TrimStart('"').TrimEnd('"').Replace("\"\"", "\"");
        }
        return rawCols;
    }

    private string SanitizeFileName(string name)
    {
        string invalidChars = new string(Path.GetInvalidFileNameChars());
        foreach (char c in invalidChars) 
        { 
            name = name.Replace(c, '_'); 
        }
        return name;
    }
}