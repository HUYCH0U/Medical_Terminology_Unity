using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MedicalTerminology.Managers
{
    using Core;
    using Legacy;

    /// <summary>
    /// Replaces PuzzleDataManager with CardDatabase integration.
    /// Provides WordModel compatibility layer for legacy puzzle code.
    /// </summary>
    [CreateAssetMenu(fileName = "CardDataManager", menuName = "GenThuatNgu/Card Data Manager")]
    public class CardDataManager : ScriptableObject
    {
        [SerializeField] private CardDatabase cardDatabase;

        // Cached WordModel lists for performance
        private List<WordModel> _prefixList;
        private List<WordModel> _rootList;
        private List<WordModel> _suffixList;
        private List<WordModel> _terminologyList;
        private List<WordModel> _allWordsList;

        // Sentence dictionaries (optional - can be populated from CSV)
        private Dictionary<string, string> _sentencesEN = new Dictionary<string, string>();
        private Dictionary<string, string> _sentencesVN = new Dictionary<string, string>();

        private bool _isInitialized;

        private void OnEnable()
        {
            // Reset initialization state when game starts (or domain reloads)
            _isInitialized = false;
            
            // Clear lists to be safe
            _prefixList = null;
            _rootList = null;
            _suffixList = null;
            _terminologyList = null;
            _allWordsList = null;
        }

        /// <summary>
        /// Initialize manager and convert CardData to WordModel.
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[CardDataManager] Initialize() called");
            
            if (_isInitialized)
            {
                Debug.Log("[CardDataManager] Already initialized, skipping");
                return;
            }

            if (cardDatabase == null)
            {
                Debug.LogError("[CardDataManager] CardDatabase not assigned!");
                return;
            }

            Debug.Log($"[CardDataManager] CardDatabase found: {cardDatabase.name}");
            cardDatabase.Initialize();
            
            Debug.Log("[CardDataManager] Converting cards to WordModels...");
            ConvertCardsToWordModels();
            
            _isInitialized = true;
            Debug.Log($"[CardDataManager] ✅ Initialized with {_allWordsList.Count} words");
            Debug.Log($"[CardDataManager]   - Prefixes: {_prefixList.Count}");
            Debug.Log($"[CardDataManager]   - Roots: {_rootList.Count}");
            Debug.Log($"[CardDataManager]   - Suffixes: {_suffixList.Count}");
            Debug.Log($"[CardDataManager]   - Terms: {_terminologyList.Count}");
        }

        /// <summary>
        /// Convert CardDataSO to WordModel for legacy compatibility.
        /// </summary>
        private void ConvertCardsToWordModels()
        {
            Debug.Log("[CardDataManager] ConvertCardsToWordModels() BEGIN");
            
            _prefixList = new List<WordModel>();
            _rootList = new List<WordModel>();
            _suffixList = new List<WordModel>();
            _terminologyList = new List<WordModel>();
            _allWordsList = new List<WordModel>();

            // Get all cards from database
            var allCards = cardDatabase.GetCardsByRarity(CardRarity.Common)
                .Concat(cardDatabase.GetCardsByRarity(CardRarity.Rare))
                .Concat(cardDatabase.GetCardsByRarity(CardRarity.Legendary));

            int cardCount = 0;
            foreach (var card in allCards)
            {
                if (card == null) continue;
                cardCount++;

                WordModel word = WordModel.FromCardData(card);
                if (word == null)
                {
                    Debug.LogWarning($"[CardDataManager] Failed to convert card: {card.name}");
                    continue;
                }

                // Categorize by type
                switch (card.wordType)
                {
                    case WordType.Prefix:
                        _prefixList.Add(word);
                        break;
                    case WordType.Root:
                        _rootList.Add(word);
                        break;
                    case WordType.Suffix:
                        _suffixList.Add(word);
                        break;
                    case WordType.Term:
                    case WordType.Eponym:
                        _terminologyList.Add(word);
                        break;
                }

                _allWordsList.Add(word);
            }
            
            Debug.Log($"[CardDataManager] Processed {cardCount} cards from database");
            Debug.Log("[CardDataManager] ConvertCardsToWordModels() END");
        }

        /// <summary>
        /// Get random target word (term only).
        /// </summary>
        public WordModel GetRandomTarget()
        {
            if (!_isInitialized) Initialize();

            if (_terminologyList == null || _terminologyList.Count == 0)
            {
                Debug.LogWarning("[CardDataManager] No terminology available!");
                return null;
            }

            return _terminologyList[Random.Range(0, _terminologyList.Count)];
        }

        /// <summary>
        /// Get random tokens (prefix/root/suffix) for puzzle.
        /// </summary>
        public List<WordModel> GetRandomTokens(int count)
        {
            if (!_isInitialized) Initialize();

            var availableTokens = new List<WordModel>();
            if (_prefixList != null) availableTokens.AddRange(_prefixList);
            if (_rootList != null) availableTokens.AddRange(_rootList);
            if (_suffixList != null) availableTokens.AddRange(_suffixList);

            if (availableTokens.Count == 0)
            {
                Debug.LogWarning("[CardDataManager] No tokens available!");
                return new List<WordModel>();
            }

            return availableTokens
                .OrderBy(x => Random.value)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Add example sentence for a word ID.
        /// </summary>
        public void AddSentence(string wordId, string englishSentence, string vietnameseSentence)
        {
            if (!string.IsNullOrEmpty(englishSentence))
            {
                _sentencesEN[wordId] = englishSentence;
            }

            if (!string.IsNullOrEmpty(vietnameseSentence))
            {
                _sentencesVN[wordId] = vietnameseSentence;
            }
        }

        // Public accessors (legacy compatibility)
        public List<WordModel> PrefixList => _prefixList;
        public List<WordModel> RootList => _rootList;
        public List<WordModel> SuffixList => _suffixList;
        public List<WordModel> TerminologyList => _terminologyList;
        public Dictionary<string, string> SentencesEN => _sentencesEN;
        public Dictionary<string, string> SentencesVN => _sentencesVN;

        /// <summary>
        /// Get word model by ID.
        /// </summary>
        public WordModel GetWordById(string id)
        {
            if (!_isInitialized) Initialize();
            return _allWordsList?.Find(w => w.idvalue == id);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Show Statistics")]
        private void DebugShowStats()
        {
            if (!_isInitialized) Initialize();

            Debug.Log($"[CardDataManager] Statistics:");
            Debug.Log($"  Prefixes: {_prefixList?.Count ?? 0}");
            Debug.Log($"  Roots: {_rootList?.Count ?? 0}");
            Debug.Log($"  Suffixes: {_suffixList?.Count ?? 0}");
            Debug.Log($"  Terms: {_terminologyList?.Count ?? 0}");
            Debug.Log($"  Total: {_allWordsList?.Count ?? 0}");
            Debug.Log($"  Sentences EN: {_sentencesEN.Count}");
            Debug.Log($"  Sentences VN: {_sentencesVN.Count}");
        }
#endif
    }
}
