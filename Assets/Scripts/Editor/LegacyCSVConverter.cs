using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MedicalTerminology.Editor
{
    /// <summary>
    /// Converts legacy CSV format to standardized CardDataSO import format.
    /// Handles column mapping, structure parsing, and data normalization.
    /// </summary>
    public class LegacyCSVConverter : EditorWindow
    {
        private TextAsset sourceCSV;
        private string outputPath = "Assets/Resources/WordData/";
        private string defaultSpecialty = "Dentistry";
        
        private Vector2 scrollPos;
        private string logMessage = "";

        [MenuItem("GenThuatNgu/Convert Legacy CSV")]
        public static void ShowWindow()
        {
            var window = GetWindow<LegacyCSVConverter>("Legacy CSV Converter");
            window.minSize = new Vector2(500, 400);
        }

        private void OnGUI()
        {
            GUILayout.Label("Legacy CSV Converter", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Source CSV
            EditorGUILayout.BeginHorizontal();
            sourceCSV = (TextAsset)EditorGUILayout.ObjectField("Source CSV", sourceCSV, typeof(TextAsset), false);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Default Specialty
            EditorGUILayout.BeginHorizontal();
            defaultSpecialty = EditorGUILayout.TextField("Default Specialty", defaultSpecialty);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("This specialty will be assigned to all converted cards (e.g., Dentistry, Cardiology)", MessageType.Info);

            GUILayout.Space(5);

            // Output Path
            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("Output Folder", outputPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    outputPath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Convert Button
            GUI.enabled = sourceCSV != null;
            if (GUILayout.Button("Convert to Standard Format", GUILayout.Height(40)))
            {
                ConvertCSV();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // Log Area
            GUILayout.Label("Conversion Log:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            EditorGUILayout.TextArea(logMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ConvertCSV()
        {
            logMessage = "Starting conversion...\n";

            if (sourceCSV == null)
            {
                LogError("No CSV file selected!");
                return;
            }

            // Parse source CSV
            string[] lines = sourceCSV.text.Split('\n');
            if (lines.Length < 2)
            {
                LogError("CSV file is empty or invalid!");
                return;
            }

            // Create output
            StringBuilder output = new StringBuilder();
            
            // Write standard header
            output.AppendLine("ID,TermEnglish,TermVietnamese,Prefix,Root,Suffix,Description,Category,Rarity,Specialty,Structure");

            int converted = 0;
            int errors = 0;

            // Process data lines (skip header)
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    string convertedLine = ConvertLine(line);
                    if (!string.IsNullOrEmpty(convertedLine))
                    {
                        output.AppendLine(convertedLine);
                        converted++;
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"Error on line {i + 1}: {e.Message}");
                    errors++;
                }
            }

            // Save output file
            string outputFileName = sourceCSV.name + "_CONVERTED.csv";
            string fullPath = Path.Combine(outputPath, outputFileName);
            
            // Ensure directory exists
            Directory.CreateDirectory(outputPath);
            
            File.WriteAllText(fullPath, output.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            LogSuccess($"\n✅ CONVERSION COMPLETE!\n" +
                      $"Converted: {converted} cards\n" +
                      $"Errors: {errors}\n" +
                      $"Output: {fullPath}");

            EditorUtility.DisplayDialog("Conversion Complete",
                $"Successfully converted {converted} cards!\n\n" +
                $"Output file: {outputFileName}\n" +
                $"Location: {outputPath}\n\n" +
                $"You can now import this file using the CSV Import Tool.",
                "OK");

            // Ping the output file
            var outputAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
            if (outputAsset != null)
            {
                EditorGUIUtility.PingObject(outputAsset);
            }
        }

        private string ConvertLine(string line)
        {
            // Legacy format:
            // STT,Tiếng Anh,Giải nghĩa EN,Tiếng Việt,Giải nghĩa VN,Phân tích,Hình,Link,Chương,Trang,IPA,Category,Type,Rarity

            string[] fields = ParseCSVLine(line);
            
            if (fields.Length < 14)
            {
                LogError($"Invalid line format (expected 14 fields, got {fields.Length})");
                return null;
            }

            // Extract fields
            string stt = fields[0].Trim();
            string termEn = fields[1].Trim();
            string descEn = fields[2].Trim();
            string termVn = fields[3].Trim();
            string descVn = fields[4].Trim();
            string analysis = fields[5].Trim();
            string category = fields[11].Trim();
            string type = fields[12].Trim();
            string rarity = fields[13].Trim();

            // Skip if missing required data
            if (string.IsNullOrEmpty(stt) || string.IsNullOrEmpty(termEn))
            {
                return null;
            }

            // Convert ID format
            string id = ConvertID(stt, type);

            // Parse structure analysis into components
            var (prefix, root, suffix, structure) = ParseAnalysis(analysis);

            // Combine descriptions
            string description = CombineDescriptions(descEn, descVn);

            // Map Category (if needed)
            category = MapCategory(category);

            // Build output line in standard format
            return $"{id},{EscapeCSV(termEn)},{EscapeCSV(termVn)},{prefix},{root},{suffix}," +
                   $"{EscapeCSV(description)},{category},{rarity},{defaultSpecialty},{EscapeCSV(structure)}";
        }

        private string ConvertID(string stt, string type)
        {
            // Convert STT (201, 202...) to proper ID format (CARD_201, P_xxx, R_xxx, S_xxx)
            if (type.Equals("Term", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"CARD_{stt}";
            }
            else if (type.Equals("Prefix", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"P_{stt}";
            }
            else if (type.Equals("Root", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"R_{stt}";
            }
            else if (type.Equals("Suffix", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"S_{stt}";
            }
            else
            {
                return $"CARD_{stt}"; // Default to Term
            }
        }

        private (string prefix, string root, string suffix, string structure) ParseAnalysis(string analysis)
        {
            if (string.IsNullOrEmpty(analysis))
            {
                return ("", "", "", "");
            }

            // Common patterns:
            // "Gastr (dạ dày) + itis (viêm)"
            // "Full (toàn) + coverage (phủ) + crown (mão)"
            // "Pre (trước)"

            string prefix = "";
            string root = "";
            string suffix = "";
            string structure = analysis;

            // Try to parse components
            // Pattern 1: "Word + Word + Word"
            if (analysis.Contains("+"))
            {
                string[] parts = analysis.Split('+');
                
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i].Trim();
                    
                    // Extract just the word (before parentheses if any)
                    string word = ExtractWord(part);
                    
                    if (i == 0 && parts.Length >= 2)
                    {
                        // First part could be prefix or root
                        if (word.EndsWith("-") || word.StartsWith("-") == false)
                        {
                            root = word.Replace("-", "");
                        }
                        else
                        {
                            prefix = word.Replace("-", "");
                        }
                    }
                    else if (i == parts.Length - 1)
                    {
                        // Last part is usually suffix
                        suffix = word.Replace("-", "");
                    }
                    else
                    {
                        // Middle parts are usually roots
                        if (string.IsNullOrEmpty(root))
                        {
                            root = word.Replace("-", "");
                        }
                    }
                }
            }
            else
            {
                // Single component
                string word = ExtractWord(analysis);
                
                if (word.EndsWith("-"))
                {
                    prefix = word.Replace("-", "");
                }
                else if (word.StartsWith("-"))
                {
                    suffix = word.Replace("-", "");
                }
                else
                {
                    root = word;
                }
            }

            return (prefix, root, suffix, structure);
        }

        private string ExtractWord(string text)
        {
            // Extract word before parentheses: "Gastr (dạ dày)" → "Gastr"
            int parenIndex = text.IndexOf('(');
            if (parenIndex > 0)
            {
                return text.Substring(0, parenIndex).Trim();
            }
            return text.Trim();
        }

        private string CombineDescriptions(string descEn, string descVn)
        {
            if (!string.IsNullOrEmpty(descEn) && !string.IsNullOrEmpty(descVn))
            {
                return $"{descEn}. Vietnamese: {descVn}";
            }
            else if (!string.IsNullOrEmpty(descEn))
            {
                return descEn;
            }
            else if (!string.IsNullOrEmpty(descVn))
            {
                return descVn;
            }
            return "";
        }

        private string MapCategory(string category)
        {
            // Map legacy category names to standard enum values if needed
            // Currently assuming categories are already correct
            return category;
        }

        private string[] ParseCSVLine(string line)
        {
            // Simple CSV parser (handles quoted fields with commas)
            var fields = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // Add last field
            fields.Add(currentField.ToString());

            return fields.ToArray();
        }

        private string EscapeCSV(string value)
        {
            // Escape CSV values (wrap in quotes if contains comma or quote)
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
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
            Repaint();
        }
    }
}
