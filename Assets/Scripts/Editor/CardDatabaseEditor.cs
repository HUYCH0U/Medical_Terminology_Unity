using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MedicalTerminology.Core.Editor
{
    /// <summary>
    /// Custom Inspector for CardDatabase with organized groups, search, and auto-populate.
    /// </summary>
    [CustomEditor(typeof(CardDatabase))]
    public class CardDatabaseEditor : UnityEditor.Editor
    {
        private CardDatabase database;
        private string searchQuery = "";
        private bool showTerms = true;
        private bool showRoots = true;
        private bool showSuffixes = true;
        private bool showPrefixes = true;
        private bool showEponyms = false;
        private bool showOthers = false;

        private Vector2 scrollPos;

        private void OnEnable()
        {
            database = (CardDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.Space(5);
            GUILayout.Label("Card Database Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Overview Stats
            DrawOverview();
            EditorGUILayout.Space(10);

            // Search Bar
            DrawSearchBar();
            EditorGUILayout.Space(10);

            // Auto-Populate Button
            DrawAutoPopulateButton();
            EditorGUILayout.Space(10);

            // Groups with search filter
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawGroupWithSearch("Terms", ref showTerms, database.Terms, new Color(0.3f, 0.7f, 1f, 0.2f));
            DrawGroupWithSearch("Roots", ref showRoots, database.Roots, new Color(0.3f, 1f, 0.3f, 0.2f));
            DrawGroupWithSearch("Suffixes", ref showSuffixes, database.Suffixes, new Color(1f, 0.7f, 0.3f, 0.2f));
            DrawGroupWithSearch("Prefixes", ref showPrefixes, database.Prefixes, new Color(1f, 0.3f, 0.7f, 0.2f));
            
            if (database.Eponyms != null && database.Eponyms.Length > 0)
                DrawGroupWithSearch("Eponyms", ref showEponyms, database.Eponyms, new Color(0.7f, 0.3f, 1f, 0.2f));
            
            if (database.Others != null && database.Others.Length > 0)
                DrawGroupWithSearch("Others", ref showOthers, database.Others, new Color(0.5f, 0.5f, 0.5f, 0.2f));

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverview()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("📊 Overview", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            int totalCards = database.CardCount;
            int termsCount = database.Terms?.Length ?? 0;
            int rootsCount = database.Roots?.Length ?? 0;
            int suffixesCount = database.Suffixes?.Length ?? 0;
            int prefixesCount = database.Prefixes?.Length ?? 0;
            int eponymsCount = database.Eponyms?.Length ?? 0;
            int othersCount = database.Others?.Length ?? 0;
            int componentsCount = rootsCount + suffixesCount + prefixesCount;

            EditorGUILayout.LabelField("Total Cards:", totalCards.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  • Terms:", termsCount.ToString());
            EditorGUILayout.LabelField("  • Components:", componentsCount.ToString());
            EditorGUILayout.LabelField("    - Roots:", rootsCount.ToString());
            EditorGUILayout.LabelField("    - Suffixes:", suffixesCount.ToString());
            EditorGUILayout.LabelField("    - Prefixes:", prefixesCount.ToString());
            
            if (eponymsCount > 0)
                EditorGUILayout.LabelField("  • Eponyms:", eponymsCount.ToString());
            if (othersCount > 0)
                EditorGUILayout.LabelField("  • Others:", othersCount.ToString());

            EditorGUILayout.EndVertical();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("🔍", GUILayout.Width(20));
            searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchQuery = "";
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(searchQuery))
            {
                EditorGUILayout.HelpBox($"Searching for: \"{searchQuery}\"", MessageType.Info);
            }
        }

        private void DrawAutoPopulateButton()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📁 Auto-Organize from Folder", GUILayout.Height(30)))
            {
                string folderPath = EditorUtility.OpenFolderPanel(
                    "Select CardDataSO Folder", 
                    "Assets/GenThuatNgu/Cards", 
                    ""
                );
                
                if (!string.IsNullOrEmpty(folderPath))
                {
                    // Convert absolute to relative path
                    string relativePath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                    database.AutoOrganizeFromFolder(relativePath);
                    EditorUtility.SetDirty(database);
                }
            }
            
            if (GUILayout.Button("✅ Validate", GUILayout.Width(100), GUILayout.Height(30)))
            {
                ValidateDatabase();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGroupWithSearch(string groupName, ref bool foldout, CardDataSO[] cards, Color bgColor)
        {
            if (cards == null || cards.Length == 0) return;

            // Filter by search
            var filtered = string.IsNullOrEmpty(searchQuery) 
                ? cards 
                : cards.Where(c => c != null && (
                    c.id.ToLower().Contains(searchQuery.ToLower()) ||
                    c.termEnglish.ToLower().Contains(searchQuery.ToLower()) ||
                    c.termVietnamese.ToLower().Contains(searchQuery.ToLower())
                )).ToArray();

            if (filtered.Length == 0 && !string.IsNullOrEmpty(searchQuery)) return;

            // Draw group header
            var originalBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = originalBg;

            string displayCount = filtered.Length != cards.Length 
                ? $"{filtered.Length}/{cards.Length}" 
                : cards.Length.ToString();
            
            foldout = EditorGUILayout.Foldout(foldout, $"{groupName} ({displayCount})", true, EditorStyles.foldoutHeader);

            if (foldout)
            {
                EditorGUI.indentLevel++;
                
                foreach (var card in filtered)
                {
                    if (card == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    
                    // Object field
                    EditorGUILayout.ObjectField(card, typeof(CardDataSO), false);
                    
                    // Info label
                    string info = $"{card.id}";
                    if (!string.IsNullOrEmpty(card.termEnglish))
                        info += $" - {card.termEnglish}";
                    if (!string.IsNullOrEmpty(card.termVietnamese))
                        info += $" ({card.termVietnamese})";
                    
                    EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void ValidateDatabase()
        {
            int issues = 0;
            var allGroups = new[] 
            { 
                database.Terms, database.Prefixes, database.Roots, 
                database.Suffixes, database.Eponyms, database.Others 
            };

            foreach (var group in allGroups)
            {
                if (group == null) continue;
                
                foreach (var card in group)
                {
                    if (card == null)
                    {
                        Debug.LogWarning("[CardDatabase] Null card reference found!");
                        issues++;
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(card.id))
                    {
                        Debug.LogWarning($"[CardDatabase] Card '{card.name}' has no ID!", card);
                        issues++;
                    }
                }
            }

            if (issues == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", 
                    "✅ No issues found! Database is healthy.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Complete", 
                    $"⚠️ Found {issues} issue(s). Check console for details.", "OK");
            }
        }
    }
}
