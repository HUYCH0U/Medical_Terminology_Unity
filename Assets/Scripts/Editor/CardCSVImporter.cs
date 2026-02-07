using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MedicalTerminology.Editor
{
    using Core;

    /// <summary>
    /// Unity Editor tool to import medical terminology cards from CSV files.
    /// Features: Auto-create SO, Smart component reuse, Validation, Progress reporting.
    /// </summary>
    public class CardCSVImporter : EditorWindow
    {
        private string csvPath = "";
        private string outputFolder = "Assets/GenThuatNgu/Cards";
        private TextAsset csvFile;
        
        private Vector2 scrollPos;
        private string logMessage = "";
        
        // Statistics
        private int termsCreated = 0;
        private int componentsCreated = 0;
        private int componentsReused = 0;
        private int errors = 0;

        [MenuItem("GenThuatNgu/Import Cards from CSV")]
        public static void ShowWindow()
        {
            var window = GetWindow<CardCSVImporter>("CSV Card Importer");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            GUILayout.Label("CSV Card Importer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // CSV File Selection
            EditorGUILayout.BeginHorizontal();
            csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Output Folder
            EditorGUILayout.BeginHorizontal();
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Import Button
            GUI.enabled = csvFile != null;
            if (GUILayout.Button("Import Cards", GUILayout.Height(40)))
            {
                ImportCSV();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // Template Button
            if (GUILayout.Button("Open Template CSV", GUILayout.Height(30)))
            {
                string templatePath = "Assets/Resources/WordData/CardImportTemplate.csv";
                var template = AssetDatabase.LoadAssetAtPath<TextAsset>(templatePath);
                if (template != null)
                {
                    EditorGUIUtility.PingObject(template);
                }
                else
                {
                    EditorUtility.DisplayDialog("Template Not Found", 
                        $"Template not found at: {templatePath}\nPlease check the file location.", "OK");
                }
            }

            GUILayout.Space(10);

            // Log Area
            GUILayout.Label("Import Log:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            EditorGUILayout.TextArea(logMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ImportCSV()
        {
            ResetStats();
            logMessage = "Starting import...\n";

            if (csvFile == null)
            {
                LogError("No CSV file selected!");
                return;
            }

            // Ensure output folder exists
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                string parentFolder = Path.GetDirectoryName(outputFolder).Replace('\\', '/');
                string folderName = Path.GetFileName(outputFolder);
                AssetDatabase.CreateFolder(parentFolder, folderName);
                Log($"Created folder: {outputFolder}");
            }

            // Parse CSV
            string[] lines = csvFile.text.Split('\n');
            if (lines.Length < 2)
            {
                LogError("CSV file is empty or invalid!");
                return;
            }

            // Track existing components to reuse
            Dictionary<string, CardDataSO> componentCache = LoadExistingComponents();

            // Skip header line
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    ProcessLine(line, componentCache);
                }
                catch (System.Exception e)
                {
                    LogError($"Error on line {i + 1}: {e.Message}");
                }
            }

            // Save all assets
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Show summary
            LogSuccess($"\n✅ IMPORT COMPLETE!\n" +
                      $"Terms Created: {termsCreated}\n" +
                      $"Components Created: {componentsCreated}\n" +
                      $"Components Reused: {componentsReused}\n" +
                      $"Errors: {errors}");

            EditorUtility.DisplayDialog("Import Complete", 
                $"Successfully imported cards!\n\n" +
                $"Terms: {termsCreated}\n" +
                $"New Components: {componentsCreated}\n" +
                $"Reused Components: {componentsReused}\n" +
                $"Errors: {errors}", "OK");
        }

        private void ProcessLine(string line, Dictionary<string, CardDataSO> componentCache)
        {
            // Split by comma (basic CSV parsing - doesn't handle quoted commas)
            string[] fields = line.Split(',');
            
            if (fields.Length < 11)
            {
                LogError($"Invalid line format (expected 11 fields, got {fields.Length})");
                return;
            }

            // Parse fields
            string id = fields[0].Trim();
            string termEn = fields[1].Trim();
            string termVn = fields[2].Trim();
            string prefix = fields[3].Trim();
            string root = fields[4].Trim();
            string suffix = fields[5].Trim();
            string description = fields[6].Trim();
            string categoryStr = fields[7].Trim();
            string rarityStr = fields[8].Trim();
            string specialtyStr = fields[9].Trim();
            string structure = fields[10].Trim();

            // Validation
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(termEn))
            {
                LogError($"Missing required fields (ID or TermEnglish)");
                return;
            }

            // Determine WordType
            WordType wordType = DetermineWordType(prefix, root, suffix);

            // Check if this is a component or a term
            if (wordType == WordType.Term)
            {
                // Create/Update Term card
                CreateTermCard(id, termEn, termVn, prefix, root, suffix, description, 
                              categoryStr, rarityStr, specialtyStr, structure, componentCache);
            }
            else
            {
                // Create/Update Component card (Prefix/Root/Suffix)
                CreateComponentCard(id, termEn, termVn, wordType, description, 
                                   categoryStr, rarityStr, specialtyStr, componentCache);
            }
        }

        private WordType DetermineWordType(string prefix, string root, string suffix)
        {
            if (!string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(root) && string.IsNullOrEmpty(suffix))
                return WordType.Prefix;
            
            if (string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(root) && string.IsNullOrEmpty(suffix))
                return WordType.Root;
            
            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(root) && !string.IsNullOrEmpty(suffix))
                return WordType.Suffix;
            
            return WordType.Term;
        }

        private void CreateTermCard(string id, string termEn, string termVn, 
                                    string prefix, string root, string suffix,
                                    string description, string categoryStr, string rarityStr, 
                                    string specialtyStr, string structure,
                                    Dictionary<string, CardDataSO> componentCache)
        {
            // Auto-organize in subfolder
            string subfolder = GetSubfolderByType(WordType.Term);
            EnsureFolderExists(subfolder);
            
            // SMART COMPONENT EXTRACTION: Auto-create Prefix/Root/Suffix components
            AutoCreateComponents(prefix, root, suffix, categoryStr, rarityStr, specialtyStr, componentCache);
            
            // Check if already exists
            string assetPath = $"{subfolder}/{id}.asset";
            CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(assetPath);

            if (card == null)
            {
                card = ScriptableObject.CreateInstance<CardDataSO>();
                AssetDatabase.CreateAsset(card, assetPath);
                termsCreated++;
                Log($"Created Term: {id} - {termEn}");
            }
            else
            {
                Log($"Updated Term: {id} - {termEn}");
            }

            // Set data using reflection (since fields are private)
            var cardType = typeof(CardDataSO);
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_termEnglish", termEn);
            SetPrivateField(card, "_termVietnamese", termVn);
            SetPrivateField(card, "_description", description);
            SetPrivateField(card, "_structure", structure);
            SetPrivateField(card, "_category", ParseEnum<CardCategory>(categoryStr));
            SetPrivateField(card, "_wordType", WordType.Term);
            SetPrivateField(card, "_rarity", ParseEnum<CardRarity>(rarityStr));
            SetPrivateField(card, "_specialty", ParseEnum<MedicalSpecialty>(specialtyStr));

            EditorUtility.SetDirty(card);
        }

        /// <summary>
        /// Automatically create component cards from Term's prefix/root/suffix.
        /// Smart reuse: Only creates if component doesn't exist yet.
        /// </summary>
        private void AutoCreateComponents(string prefix, string root, string suffix,
                                         string categoryStr, string rarityStr, string specialtyStr,
                                         Dictionary<string, CardDataSO> componentCache)
        {
            // Create Prefix component if specified
            if (!string.IsNullOrEmpty(prefix))
            {
                string prefixId = GenerateComponentId(prefix, WordType.Prefix);
                CreateComponentCard(prefixId, prefix, prefix, WordType.Prefix, 
                                   $"Prefix: {prefix}", categoryStr, rarityStr, specialtyStr, componentCache);
            }

            // Create Root component if specified
            if (!string.IsNullOrEmpty(root))
            {
                string rootId = GenerateComponentId(root, WordType.Root);
                CreateComponentCard(rootId, root, root, WordType.Root, 
                                   $"Root: {root}", categoryStr, rarityStr, specialtyStr, componentCache);
            }

            // Create Suffix component if specified
            if (!string.IsNullOrEmpty(suffix))
            {
                string suffixId = GenerateComponentId(suffix, WordType.Suffix);
                CreateComponentCard(suffixId, suffix, suffix, WordType.Suffix, 
                                   $"Suffix: {suffix}", categoryStr, rarityStr, specialtyStr, componentCache);
            }
        }

        /// <summary>
        /// Generate standardized component ID based on type.
        /// P_xxx for Prefix, R_xxx for Root, S_xxx for Suffix.
        /// </summary>
        private string GenerateComponentId(string componentName, WordType type)
        {
            // Clean component name (remove dashes if present)
            string cleanName = componentName.Replace("-", "").Trim();
            
            string prefix = type switch
            {
                WordType.Prefix => "P",
                WordType.Root => "R",
                WordType.Suffix => "S",
                _ => "C"
            };

            return $"{prefix}_{cleanName}";
        }


        private void CreateComponentCard(string id, string termEn, string termVn, 
                                        WordType wordType, string description,
                                        string categoryStr, string rarityStr, string specialtyStr,
                                        Dictionary<string, CardDataSO> componentCache)
        {
            // Check cache first
            if (componentCache.ContainsKey(id))
            {
                componentsReused++;
                Log($"Reused {wordType}: {id} - {termEn}");
                return;
            }

            // Auto-organize in subfolder
            string subfolder = GetSubfolderByType(wordType);
            EnsureFolderExists(subfolder);
            
            // Check if already exists on disk
            string assetPath = $"{subfolder}/{id}.asset";
            CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(assetPath);

            if (card == null)
            {
                card = ScriptableObject.CreateInstance<CardDataSO>();
                AssetDatabase.CreateAsset(card, assetPath);
                componentsCreated++;
                Log($"Created {wordType}: {id} - {termEn}");
            }
            else
            {
                componentsReused++;
                Log($"Found existing {wordType}: {id}");
            }

            // Set data
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_termEnglish", termEn);
            SetPrivateField(card, "_termVietnamese", termVn);
            SetPrivateField(card, "_description", description);
            SetPrivateField(card, "_category", ParseEnum<CardCategory>(categoryStr));
            SetPrivateField(card, "_wordType", wordType);
            SetPrivateField(card, "_rarity", ParseEnum<CardRarity>(rarityStr));
            SetPrivateField(card, "_specialty", ParseEnum<MedicalSpecialty>(specialtyStr));

            EditorUtility.SetDirty(card);
            componentCache[id] = card;
        }

        private Dictionary<string, CardDataSO> LoadExistingComponents()
        {
            var cache = new Dictionary<string, CardDataSO>();
            
            string[] guids = AssetDatabase.FindAssets("t:CardDataSO", new[] { outputFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardDataSO card = AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
                
                if (card != null && card.wordType != WordType.Term)
                {
                    cache[card.id] = card;
                }
            }

            Log($"Loaded {cache.Count} existing components for reuse");
            return cache;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        private T ParseEnum<T>(string value) where T : struct
        {
            if (System.Enum.TryParse<T>(value, true, out T result))
            {
                return result;
            }
            return default(T);
        }

        /// <summary>
        /// Get subfolder path based on WordType for auto-organization.
        /// </summary>
        private string GetSubfolderByType(WordType type)
        {
            string folderName = type switch
            {
                WordType.Term => "Terms",
                WordType.Prefix => "Prefixes",
                WordType.Root => "Roots",
                WordType.Suffix => "Suffixes",
                WordType.Eponym => "Eponyms",
                _ => "Others"
            };
            
            return $"{outputFolder}/{folderName}";
        }

        /// <summary>
        /// Ensure folder exists, create if needed.
        /// </summary>
        private void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                // Create parent folders recursively if needed
                string parentFolder = Path.GetDirectoryName(folderPath).Replace('\\', '/');
                string folderName = Path.GetFileName(folderPath);
                
                // Ensure parent exists first
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    EnsureFolderExists(parentFolder);
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
                Log($"Created subfolder: {folderName}");
            }
        }

        private void ResetStats()
        {
            termsCreated = 0;
            componentsCreated = 0;
            componentsReused = 0;
            errors = 0;
        }

        private void Log(string message)
        {
            logMessage += $"[INFO] {message}\n";
            Repaint();
        }

        private void LogSuccess(string message)
        {
            logMessage += $"[SUCCESS] {message}\n";
            Repaint();
        }

        private void LogError(string message)
        {
            logMessage += $"[ERROR] {message}\n";
            errors++;
            Repaint();
        }
    }
}
