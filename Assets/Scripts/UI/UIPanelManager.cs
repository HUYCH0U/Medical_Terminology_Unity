using UnityEngine;
using System.Collections.Generic;

namespace MedicalTerminology.UI
{
    /// <summary>
    /// Manages multiple UI panels in a scene.
    /// Handles showing/hiding panels with enum-based state management.
    /// </summary>
    public class UIPanelManager : MonoBehaviour
    {
        /// <summary>
        /// Enum defining all available panels in this scene.
        /// Add new panels here as needed.
        /// </summary>
        public enum PanelType
        {
            None,
            MainLobby,
            StageSelect1,
            StageSelect2,
            Shop,
            Inventory,
            Cards,
            Settings,
            Credits
        }

        [System.Serializable]
        public class PanelMapping
        {
            public PanelType type;
            public GameObject panel;
        }

        [Header("Panel Configuration")]
        [SerializeField] private List<PanelMapping> panelMappings = new List<PanelMapping>();
        
        [Header("Settings")]
        [SerializeField] private PanelType startingPanel = PanelType.MainLobby;
        [SerializeField] private bool hideAllOnStart = true;

        // Runtime cache for fast lookups
        private Dictionary<PanelType, GameObject> panelDictionary = new Dictionary<PanelType, GameObject>();
        private PanelType currentPanel = PanelType.None;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializePanelDictionary();
        }

        private void Start()
        {
            if (hideAllOnStart)
            {
                HideAllPanels();
            }

            ShowPanel(startingPanel);
        }

        #endregion

        #region Initialization

        private void InitializePanelDictionary()
        {
            panelDictionary.Clear();

            foreach (var mapping in panelMappings)
            {
                if (mapping.panel != null)
                {
                    if (!panelDictionary.ContainsKey(mapping.type))
                    {
                        panelDictionary[mapping.type] = mapping.panel;
                    }
                    else
                    {
                        Debug.LogWarning($"[UIPanelManager] Duplicate panel type: {mapping.type}. Skipping.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[UIPanelManager] Panel not assigned for type: {mapping.type}");
                }
            }

            Debug.Log($"[UIPanelManager] Initialized {panelDictionary.Count} panels");
        }

        #endregion

        #region Public Methods - Call from Buttons

        /// <summary>
        /// Show Main Lobby panel.
        /// </summary>
        public void ShowMainLobby()
        {
            ShowPanel(PanelType.MainLobby);
        }

        /// <summary>
        /// Show Stage Select 1 panel.
        /// </summary>
        public void ShowStageSelect1()
        {
            ShowPanel(PanelType.StageSelect1);
        }

        /// <summary>
        /// Show Stage Select 2 panel.
        /// </summary>
        public void ShowStageSelect2()
        {
            ShowPanel(PanelType.StageSelect2);
        }

        /// <summary>
        /// Show Shop panel.
        /// </summary>
        public void ShowShop()
        {
            ShowPanel(PanelType.Shop);
        }

        /// <summary>
        /// Show Inventory panel.
        /// </summary>
        public void ShowInventory()
        {
            ShowPanel(PanelType.Inventory);
        }
        public void ShowCards()
        {
            ShowPanel(PanelType.Cards);
        }

        /// <summary>
        /// Show Settings panel.
        /// </summary>
        public void ShowSettings()
        {
            ShowPanel(PanelType.Settings);
        }

        /// <summary>
        /// Show Credits panel.
        /// </summary>
        public void ShowCredits()
        {
            ShowPanel(PanelType.Credits);
        }

        /// <summary>
        /// Go back to previous panel (if history tracking implemented).
        /// For now, returns to Main Lobby.
        /// </summary>
        public void GoBack()
        {
            ShowPanel(PanelType.MainLobby);
        }

        #endregion

        #region Core Panel Management

        /// <summary>
        /// Show a specific panel, hiding the current one.
        /// </summary>
        public void ShowPanel(PanelType panelType)
        {
            if (panelType == PanelType.None)
            {
                HideAllPanels();
                currentPanel = PanelType.None;
                return;
            }

            if (!panelDictionary.ContainsKey(panelType))
            {
                Debug.LogError($"[UIPanelManager] Panel type {panelType} not found in dictionary!");
                return;
            }

            // Hide current panel
            if (currentPanel != PanelType.None && panelDictionary.ContainsKey(currentPanel))
            {
                panelDictionary[currentPanel].SetActive(false);
            }

            // Show new panel
            panelDictionary[panelType].SetActive(true);
            currentPanel = panelType;

            Debug.Log($"[UIPanelManager] Switched to panel: {panelType}");
        }

        /// <summary>
        /// Hide all panels.
        /// </summary>
        public void HideAllPanels()
        {
            foreach (var kvp in panelDictionary)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(false);
                }
            }

            currentPanel = PanelType.None;
            Debug.Log("[UIPanelManager] All panels hidden");
        }

        /// <summary>
        /// Toggle a specific panel on/off.
        /// </summary>
        public void TogglePanel(PanelType panelType)
        {
            if (!panelDictionary.ContainsKey(panelType))
            {
                Debug.LogError($"[UIPanelManager] Panel type {panelType} not found!");
                return;
            }

            bool isActive = panelDictionary[panelType].activeSelf;
            panelDictionary[panelType].SetActive(!isActive);

            if (!isActive)
            {
                currentPanel = panelType;
            }
        }

        #endregion

        #region Getters

        /// <summary>
        /// Get currently active panel type.
        /// </summary>
        public PanelType GetCurrentPanel()
        {
            return currentPanel;
        }

        /// <summary>
        /// Check if a specific panel is currently active.
        /// </summary>
        public bool IsPanelActive(PanelType panelType)
        {
            if (panelDictionary.ContainsKey(panelType))
            {
                return panelDictionary[panelType].activeSelf;
            }
            return false;
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Validate Panel Setup")]
        private void ValidatePanelSetup()
        {
            Debug.Log("=== Panel Setup Validation ===");
            
            foreach (PanelType type in System.Enum.GetValues(typeof(PanelType)))
            {
                if (type == PanelType.None) continue;

                bool found = panelMappings.Exists(m => m.type == type && m.panel != null);
                if (found)
                {
                    Debug.Log($"✅ {type}: Assigned");
                }
                else
                {
                    Debug.LogWarning($"⚠️ {type}: NOT assigned!");
                }
            }
        }

        [ContextMenu("Auto-Find Panels")]
        private void AutoFindPanels()
        {
            Debug.Log("[UIPanelManager] Auto-finding panels...");
            
            panelMappings.Clear();

            // Try to find panels by name convention
            Transform[] children = GetComponentsInChildren<Transform>(true);
            
            foreach (Transform child in children)
            {
                if (child == transform) continue;

                string name = child.name;
                
                // Map names to enum types
                if (name.Contains("MainLobby") || name.Contains("Main_Lobby"))
                {
                    panelMappings.Add(new PanelMapping { type = PanelType.MainLobby, panel = child.gameObject });
                }
                else if (name.Contains("StageSelect1") || name.Contains("Stage_Select_1"))
                {
                    panelMappings.Add(new PanelMapping { type = PanelType.StageSelect1, panel = child.gameObject });
                }
                else if (name.Contains("StageSelect2") || name.Contains("Stage_Select_2"))
                {
                    panelMappings.Add(new PanelMapping { type = PanelType.StageSelect2, panel = child.gameObject });
                }
                else if (name.Contains("Shop"))
                {
                    panelMappings.Add(new PanelMapping { type = PanelType.Shop, panel = child.gameObject });
                }
                else if (name.Contains("Inventory"))
                {
                    panelMappings.Add(new PanelMapping { type = PanelType.Inventory, panel = child.gameObject });
                }
            }

            Debug.Log($"[UIPanelManager] Auto-found {panelMappings.Count} panels");
        }
#endif
    }
}
