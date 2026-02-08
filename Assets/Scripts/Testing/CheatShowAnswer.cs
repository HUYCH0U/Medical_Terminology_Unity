using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace MedicalTerminology.Testing
{
    using Managers;
    using Legacy;

    /// <summary>
    /// Cheat tool for testing: Auto-solve puzzle by placing correct cards in slots.
    /// Attach to a Button in UI.
    /// </summary>
    public class CheatShowAnswer : MonoBehaviour
    {
        [Header("Cheat Button")]
        [SerializeField] private Button cheatButton;
        
        [Header("Answer Display (Optional)")]
        [SerializeField] private TextMeshProUGUI answerText;
        
        [Header("Settings")]
        [SerializeField] private bool autoSolve = true;
        [SerializeField] private bool showAnswerText = true;
        [SerializeField] private float autoSolveDelay = 0.2f; // Delay between placing cards

        private GameManager _gameManager;

        private void Awake()
        {
            // Get button component if not assigned
            if (cheatButton == null)
                cheatButton = GetComponent<Button>();

            // Setup button click
            if (cheatButton != null)
            {
                cheatButton.onClick.AddListener(ShowAnswer);
            }
        }

        private void Start()
        {
            _gameManager = FindObjectOfType<GameManager>();
            
            if (_gameManager == null)
            {
                Debug.LogWarning("[CheatShowAnswer] GameManager not found!");
            }
        }

        /// <summary>
        /// Show answer - called when button is clicked.
        /// </summary>
        public void ShowAnswer()
        {
            if (_gameManager == null)
            {
                Debug.LogError("[CheatShowAnswer] GameManager is NULL!");
                return;
            }

            Debug.Log("🔑 [CHEAT] Showing answer...");

            // Get current target word
            WordModel target = GetCurrentTarget();
            
            if (target == null)
            {
                Debug.LogWarning("[CheatShowAnswer] No target word found!");
                return;
            }

            // Display answer text
            if (showAnswerText && answerText != null)
            {
                DisplayAnswerText(target);
            }

            // Auto-solve puzzle
            if (autoSolve)
            {
                StartCoroutine(AutoSolvePuzzle(target));
            }
        }

        /// <summary>
        /// Get current target word from GameManager using reflection.
        /// </summary>
        private WordModel GetCurrentTarget()
        {
            var field = _gameManager.GetType().GetField("_currentTarget", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return field.GetValue(_gameManager) as WordModel;
            }

            return null;
        }

        /// <summary>
        /// Display answer in text UI.
        /// </summary>
        private void DisplayAnswerText(WordModel target)
        {
            string answer = $"<b>🔑 ANSWER:</b>\n\n" +
                          $"<b>English:</b> {target.stringvalue}\n" +
                          $"<b>Vietnamese:</b> {target.apologetic}\n" +
                          $"<b>ID:</b> {target.idvalue}\n\n" +
                          $"<b>Components:</b>\n" +
                          $"{FormatComponents(target)}";

            answerText.text = answer;
            answerText.color = Color.yellow;

            Debug.Log($"🔑 Answer: {target.stringvalue} ({target.apologetic})");
            Debug.Log($"🔑 ID: {target.idvalue}");
        }

        /// <summary>
        /// Format components for display.
        /// </summary>
        private string FormatComponents(WordModel target)
        {
            string components = "";
            string[] parts = target.idvalue.Split('-');

            foreach (string part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part.StartsWith("P"))
                    components += $"  • Prefix: {part}\n";
                else if (part.StartsWith("R"))
                    components += $"  • Root: {part}\n";
                else if (part.StartsWith("S"))
                    components += $"  • Suffix: {part}\n";
            }

            return components;
        }

        /// <summary>
        /// Auto-solve by placing correct cards in slots.
        /// </summary>
        private System.Collections.IEnumerator AutoSolvePuzzle(WordModel target)
        {
            Debug.Log("🎯 [CHEAT] Auto-solving puzzle...");

            // Get slots
            var slotsField = _gameManager.GetType().GetField("slotTransforms", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (slotsField == null)
            {
                Debug.LogError("[CheatShowAnswer] Cannot find slotTransforms!");
                yield break;
            }

            Transform[] slots = slotsField.GetValue(_gameManager) as Transform[];
            
            if (slots == null || slots.Length == 0)
            {
                Debug.LogError("[CheatShowAnswer] Slots array is empty!");
                yield break;
            }

            // Get token spawn area
            var tokenAreaField = _gameManager.GetType().GetField("tokenSpawnArea", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Transform tokenArea = tokenAreaField?.GetValue(_gameManager) as Transform;
            
            if (tokenArea == null)
            {
                Debug.LogError("[CheatShowAnswer] Cannot find tokenSpawnArea!");
                yield break;
            }

            // Get component IDs from target's quiz data structure
            string[] componentIds = GetComponentIds(target);
            
            if (componentIds == null || componentIds.Length == 0)
            {
                Debug.LogError($"[CheatShowAnswer] No component IDs found! Target ID: {target.idvalue}");
                yield break;
            }

            Debug.Log($"🔍 Looking for components: {string.Join(", ", componentIds)}");

            // Find and place correct cards
            for (int i = 0; i < componentIds.Length && i < slots.Length; i++)
            {
                string targetId = componentIds[i];
                Transform card = FindCardWithId(tokenArea, targetId);

                if (card != null)
                {
                    // Place card in slot
                    card.SetParent(slots[i]);
                    card.localPosition = Vector3.zero;
                    card.localRotation = Quaternion.Euler(-60f, 0f, 0f);
                    card.localScale = Vector3.one;

                    Debug.Log($"✅ Placed {targetId} in slot {i}");

                    // Small delay for visual feedback
                    yield return new WaitForSeconds(autoSolveDelay);
                }
                else
                {
                    Debug.LogWarning($"⚠️ Card with ID {targetId} not found!");
                }
            }

            // Check answer
            yield return new WaitForSeconds(0.1f);
            _gameManager.CheckSlots();

            Debug.Log("✅ [CHEAT] Auto-solve complete!");
        }

        /// <summary>
        /// Extract component IDs from target WordModel.
        /// Finds component IDs by checking which spawned token IDs are part of the target.
        /// </summary>
        private string[] GetComponentIds(WordModel target)
        {
            var componentIds = new System.Collections.Generic.List<string>();

            // Strategy: Get all spawned tokens and check which ones are part of target ID
            var tokenAreaField = _gameManager.GetType().GetField("tokenSpawnArea", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Transform tokenArea = tokenAreaField?.GetValue(_gameManager) as Transform;
            
            if (tokenArea != null)
            {
                // Get all token cards
                foreach (Transform child in tokenArea)
                {
                    var cardValue = child.GetComponent<Puzzle.PuzzleCardValue>();
                    if (cardValue != null)
                    {
                        var wordModel = GetWordModelFromCard(cardValue);
                        if (wordModel != null)
                        {
                            // Check if this token ID is part of target ID
                            // Target might be "CARD_263" or contain component references
                            // We check if target's ID structure contains this component
                            
                            // For now, use simple heuristic: 
                            // If token is P/R/S type and we don't have it yet, it's likely correct
                            string tokenId = wordModel.idvalue;
                            
                            if (!string.IsNullOrEmpty(tokenId))
                            {
                                // Check if it's a component (starts with P_, R_, S_)
                                if (tokenId.StartsWith("P_") || tokenId.StartsWith("P") ||
                                    tokenId.StartsWith("R_") || tokenId.StartsWith("R") ||
                                    tokenId.StartsWith("S_") || tokenId.StartsWith("S"))
                                {
                                    // Check if target contains reference to this
                                    // This is a simplified check - in real scenario, 
                                    // GameManager already spawned correct components
                                    if (!componentIds.Contains(tokenId))
                                    {
                                        componentIds.Add(tokenId);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Sort by type (Prefix, Root, Suffix)
            componentIds.Sort((a, b) => 
            {
                int GetPriority(string id)
                {
                    if (id.StartsWith("P")) return 0;
                    if (id.StartsWith("R")) return 1;
                    if (id.StartsWith("S")) return 2;
                    return 3;
                }
                return GetPriority(a).CompareTo(GetPriority(b));
            });

            if (componentIds.Count > 0)
            {
                Debug.Log($"📌 Found {componentIds.Count} component IDs: {string.Join(", ", componentIds)}");
                return componentIds.ToArray();
            }

            // Fallback: Try parsing from target.idvalue if it has structure
            if (target.idvalue.Contains("-"))
            {
                var ids = target.idvalue.Split('-')
                    .Where(s => !string.IsNullOrEmpty(s) && 
                               (s.StartsWith("P") || s.StartsWith("R") || s.StartsWith("S")))
                    .ToArray();
                
                if (ids.Length > 0)
                {
                    Debug.Log($"📌 Found component IDs from target idvalue: {string.Join(", ", ids)}");
                    return ids;
                }
            }

            Debug.LogWarning($"⚠️ Could not extract component IDs from target: {target.idvalue}");
            Debug.LogWarning($"⚠️ GameManager should have spawned correct tokens. Using all available tokens.");
            
            // Last resort: return all found component IDs
            return componentIds.ToArray();
        }



        /// <summary>
        /// Find card with specific ID in token area.
        /// </summary>
        private Transform FindCardWithId(Transform tokenArea, string targetId)
        {
            foreach (Transform child in tokenArea)
            {
                var cardValue = child.GetComponent<Puzzle.PuzzleCardValue>();
                if (cardValue != null)
                {
                    var wordModel = GetWordModelFromCard(cardValue);
                    if (wordModel != null && wordModel.idvalue == targetId)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get WordModel from PuzzleCardValue using reflection.
        /// </summary>
        private WordModel GetWordModelFromCard(Puzzle.PuzzleCardValue cardValue)
        {
            var field = cardValue.GetType().GetField("_wordModel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return field?.GetValue(cardValue) as WordModel;
        }

        /// <summary>
        /// Toggle cheat button visibility (call from Inspector or another script).
        /// </summary>
        public void ToggleCheatButton(bool show)
        {
            if (cheatButton != null)
                cheatButton.gameObject.SetActive(show);
        }
    }
}
