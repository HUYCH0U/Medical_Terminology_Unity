using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MedicalTerminology.Core
{
    /// <summary>
    /// Centralized database for all card data using ScriptableObject pattern.
    /// Organized into groups for better Inspector management.
    /// Provides O(1) lookup and eliminates Resources.Load() usage.
    /// </summary>
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "GenThuatNgu/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        [Header("=== TERMS (Main Cards) ===")]
        [SerializeField] private CardDataSO[] terms;
        
        [Header("=== COMPONENTS ===")]
        [SerializeField] private CardDataSO[] prefixes;
        [SerializeField] private CardDataSO[] roots;
        [SerializeField] private CardDataSO[] suffixes;
        
        [Header("=== OTHER ===")]
        [SerializeField] private CardDataSO[] eponyms;
        [SerializeField] private CardDataSO[] others;
        
        private Dictionary<string, CardDataSO> _cardLookup;
        private bool _isInitialized;

        // Public accessors for Custom Editor
        public CardDataSO[] Terms => terms;
        public CardDataSO[] Prefixes => prefixes;
        public CardDataSO[] Roots => roots;
        public CardDataSO[] Suffixes => suffixes;
        public CardDataSO[] Eponyms => eponyms;
        public CardDataSO[] Others => others;

        private void OnEnable()
        {
            _isInitialized = false;
            _cardLookup = null;
        }

        /// <summary>
        /// Initialize the lookup dictionary. Call once at game start.
        /// Merges all groups into single lookup.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            // Merge all groups
            var allCards = MergeAllGroups();
            _cardLookup = new Dictionary<string, CardDataSO>(allCards.Count);
            
            foreach (var card in allCards)
            {
                if (card == null)
                {
                    Debug.LogWarning("[CardDatabase] Null card reference found in database!");
                    continue;
                }
                
                if (string.IsNullOrEmpty(card.id))
                {
                    Debug.LogWarning($"[CardDatabase] Card '{card.name}' has no ID!");
                    continue;
                }
                
                if (_cardLookup.ContainsKey(card.id))
                {
                    Debug.LogError($"[CardDatabase] Duplicate card ID found: {card.id}");
                    continue;
                }
                
                _cardLookup[card.id] = card;
            }
            
            _isInitialized = true;
            Debug.Log($"[CardDatabase] Initialized with {_cardLookup.Count} cards " +
                     $"(Terms: {terms?.Length ?? 0}, Roots: {roots?.Length ?? 0}, " +
                     $"Suffixes: {suffixes?.Length ?? 0}, Prefixes: {prefixes?.Length ?? 0})");
        }

        /// <summary>
        /// Merge all groups into single list.
        /// </summary>
        private List<CardDataSO> MergeAllGroups()
        {
            var merged = new List<CardDataSO>();
            
            if (terms != null) merged.AddRange(terms);
            if (prefixes != null) merged.AddRange(prefixes);
            if (roots != null) merged.AddRange(roots);
            if (suffixes != null) merged.AddRange(suffixes);
            if (eponyms != null) merged.AddRange(eponyms);
            if (others != null) merged.AddRange(others);
            
            return merged;
        }

        /// <summary>
        /// Get card by ID. O(1) lookup time.
        /// </summary>
        public CardDataSO GetCard(string id)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[CardDatabase] Database not initialized! Call Initialize() first.");
                return null;
            }
            
            return _cardLookup.TryGetValue(id, out var card) ? card : null;
        }

        /// <summary>
        /// Get all cards matching a category.
        /// </summary>
        public IEnumerable<CardDataSO> GetCardsByCategory(CardCategory category)
        {
            if (!_isInitialized) Initialize();
            return _cardLookup.Values.Where(c => c.category == category);
        }

        /// <summary>
        /// Get all cards matching a word type.
        /// </summary>
        public IEnumerable<CardDataSO> GetCardsByType(WordType wordType)
        {
            if (!_isInitialized) Initialize();
            return _cardLookup.Values.Where(c => c.wordType == wordType);
        }

        /// <summary>
        /// Get all cards matching a rarity.
        /// </summary>
        public IEnumerable<CardDataSO> GetCardsByRarity(CardRarity rarity)
        {
            if (!_isInitialized) Initialize();
            return _cardLookup.Values.Where(c => c.rarity == rarity);
        }

        /// <summary>
        /// Get cards by specialty.
        /// </summary>
        public IEnumerable<CardDataSO> GetCardsBySpecialty(MedicalSpecialty specialty)
        {
            if (!_isInitialized) Initialize();
            return _cardLookup.Values.Where(c => c.specialty == specialty);
        }

        /// <summary>
        /// Get total number of cards in database.
        /// </summary>
        public int CardCount
        {
            get
            {
                if (_isInitialized) return _cardLookup.Count;
                return (terms?.Length ?? 0) + (prefixes?.Length ?? 0) + 
                       (roots?.Length ?? 0) + (suffixes?.Length ?? 0) + 
                       (eponyms?.Length ?? 0) + (others?.Length ?? 0);
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Auto-organize cards from single folder into groups.
        /// Called by Custom Editor.
        /// </summary>
        public void AutoOrganizeFromFolder(string folderPath)
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:CardDataSO", new[] { folderPath });
            var allCards = new List<CardDataSO>();
            
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardDataSO>(path);
                if (card != null) allCards.Add(card);
            }
            
            OrganizeCards(allCards);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CardDatabase] Auto-organized {allCards.Count} cards into groups");
        }

        /// <summary>
        /// Organize cards into groups by their WordType.
        /// </summary>
        private void OrganizeCards(List<CardDataSO> cards)
        {
            terms = cards.Where(c => c.wordType == WordType.Term).OrderBy(c => c.id).ToArray();
            prefixes = cards.Where(c => c.wordType == WordType.Prefix).OrderBy(c => c.id).ToArray();
            roots = cards.Where(c => c.wordType == WordType.Root).OrderBy(c => c.id).ToArray();
            suffixes = cards.Where(c => c.wordType == WordType.Suffix).OrderBy(c => c.id).ToArray();
            eponyms = cards.Where(c => c.wordType == WordType.Eponym).OrderBy(c => c.id).ToArray();
            
            // Others: anything that doesn't fit above categories
            var categorized = new HashSet<WordType> { WordType.Term, WordType.Prefix, WordType.Root, WordType.Suffix, WordType.Eponym };
            others = cards.Where(c => !categorized.Contains(c.wordType)).OrderBy(c => c.id).ToArray();
        }
        #endif
    }
}
