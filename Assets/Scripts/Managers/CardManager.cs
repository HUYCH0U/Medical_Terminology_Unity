using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MedicalTerminology.Managers
{
    using Core;
    using UI;

    /// <summary>
    /// Centralized card management with object pooling and database integration.
    /// Handles card lifecycle from spawning to despawning with zero GC allocation.
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardDatabase database;
        [SerializeField] private CardDisplay cardPrefab;
        [SerializeField] private Transform cardContainer;

        [Header("Pool Settings")]
        [SerializeField] private int poolWarmUpSize = 20;
        [SerializeField] private int poolMaxSize = 50;

        private ObjectPool<CardDisplay> _cardPool;
        private readonly List<CardDisplay> _activeCards = new List<CardDisplay>();

        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize manager and database.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (database == null)
            {
                Debug.LogError("[CardManager] Card database is not assigned!");
                return;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[CardManager] Card prefab is not assigned!");
                return;
            }

            // Initialize database lookup
            database.Initialize();

            // Create card pool
            _cardPool = new ObjectPool<CardDisplay>(
                cardPrefab,
                poolWarmUpSize,
                poolMaxSize,
                cardContainer
            );

            _isInitialized = true;
            Debug.Log($"[CardManager] Initialized with pool size {poolWarmUpSize}");
        }

        /// <summary>
        /// Spawn a card by ID at specific position.
        /// </summary>
        public CardDisplay SpawnCard(string cardID, Vector3 position)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[CardManager] Manager not initialized!");
                return null;
            }

            CardDataSO data = database.GetCard(cardID);
            if (data == null)
            {
                Debug.LogWarning($"[CardManager] Card not found: {cardID}");
                return null;
            }

            return SpawnCard(data, position);
        }

        /// <summary>
        /// Spawn a card with data at specific position.
        /// </summary>
        public CardDisplay SpawnCard(CardDataSO data, Vector3 position)
        {
            CardDisplay card = _cardPool.Get();
            card.transform.position = position;
            card.SetupData(data);
            
            _activeCards.Add(card);
            
            return card;
        }

        /// <summary>
        /// Spawn multiple cards in a grid layout.
        /// </summary>
        public List<CardDisplay> SpawnCards(IEnumerable<CardDataSO> cards, Vector3 startPosition, Vector2 spacing)
        {
            var spawnedCards = new List<CardDisplay>();
            var cardArray = cards.ToArray();
            
            for (int i = 0; i < cardArray.Length; i++)
            {
                int row = i / 5; // 5 cards per row
                int col = i % 5;
                
                Vector3 position = startPosition + new Vector3(
                    col * spacing.x,
                    -row * spacing.y,
                    0f
                );
                
                var card = SpawnCard(cardArray[i], position);
                if (card != null)
                {
                    spawnedCards.Add(card);
                }
            }
            
            return spawnedCards;
        }

        /// <summary>
        /// Despawn a specific card back to pool.
        /// </summary>
        public void DespawnCard(CardDisplay card)
        {
            if (card == null) return;

            _activeCards.Remove(card);
            _cardPool.Return(card);
        }

        /// <summary>
        /// Despawn all active cards.
        /// </summary>
        public void DespawnAllCards()
        {
            var cardsToReturn = new List<CardDisplay>(_activeCards);
            foreach (var card in cardsToReturn)
            {
                DespawnCard(card);
            }
            
            Debug.Log($"[CardManager] Despawned {cardsToReturn.Count} cards");
        }

        /// <summary>
        /// Get cards by category and spawn them.
        /// </summary>
        public List<CardDisplay> SpawnCardsByCategory(CardCategory category, Vector3 startPosition, Vector2 spacing)
        {
            var cards = database.GetCardsByCategory(category);
            return SpawnCards(cards, startPosition, spacing);
        }

        /// <summary>
        /// Get random cards and spawn them.
        /// </summary>
        public List<CardDisplay> SpawnRandomCards(int count, Vector3 startPosition, Vector2 spacing)
        {
            var allCards = database.GetCardsByRarity(CardRarity.Common)
                .Concat(database.GetCardsByRarity(CardRarity.Rare))
                .Concat(database.GetCardsByRarity(CardRarity.Legendary))
                .OrderBy(x => Random.value)
                .Take(count);
            
            return SpawnCards(allCards, startPosition, spacing);
        }

        /// <summary>
        /// Get pool statistics for debugging.
        /// </summary>
        public (int available, int inUse, int total) GetPoolStats()
        {
            return _cardPool.GetStats();
        }

        private void OnDestroy()
        {
            _cardPool?.Clear();
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Show Pool Stats")]
        private void DebugShowPoolStats()
        {
            var stats = GetPoolStats();
            Debug.Log($"[CardManager] Pool Stats - Available: {stats.available}, In Use: {stats.inUse}, Total: {stats.total}");
        }

        [ContextMenu("Debug: Despawn All")]
        private void DebugDespawnAll()
        {
            DespawnAllCards();
        }
#endif
    }
}
