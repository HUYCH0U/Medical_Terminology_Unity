using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
namespace MedicalTerminology.Puzzle
{
    using Managers;

    /// <summary>
    /// Handles drag-and-drop functionality for puzzle cards.
    /// Implements Unity's event interfaces for pointer handling.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PuzzleDragObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Transform _startParent;
        private int _startSiblingIndex;
        private CanvasGroup _canvasGroup;
        private Canvas _mainCanvas;
        private bool _isDraggingFromSlot;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _mainCanvas = GetComponentInParent<Canvas>();

            if (_mainCanvas == null)
            {
                Debug.LogError("[PuzzleDragObject] No Canvas found in parents!", this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _startParent = transform.parent;
            _startSiblingIndex = transform.GetSiblingIndex();

            // Check if dragging from a slot
            _isDraggingFromSlot = transform.parent.CompareTag("Slot");

            // Move to canvas root for proper rendering order
            transform.SetParent(_mainCanvas.transform);
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = Input.mousePosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;

            GameObject closestSlot = FindClosestSlot();

            if (closestSlot != null)
            {
                DropInSlot(closestSlot.transform);
            }
            else
            {
                ReturnToPanelOrStart();
            }

            // Notify game manager to check answer
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.CheckSlots();
            }
        }

        private GameObject FindClosestSlot()
        {
            GameObject[] slots = GameObject.FindGameObjectsWithTag("Slot");
            GameObject closestSlot = null;
            float minDistance = 100f; // Threshold distance

            foreach (GameObject slot in slots)
            {
                float distance = Vector3.Distance(transform.position, slot.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSlot = slot;
                }
            }

            return closestSlot;
        }

        private void DropInSlot(Transform slot)
        {
            // Check if slot is already occupied
            if (slot.childCount > 0)
            {
                // Return existing card to spawn area
                Transform existingCard = slot.GetChild(0);
                GameObject tokenArea = GameObject.FindGameObjectWithTag("TokenPanel");
                
                if (tokenArea != null)
                {
                    existingCard.SetParent(tokenArea.transform);
                    existingCard.localPosition = Vector3.zero;
                    existingCard.localRotation = Quaternion.identity;
                    existingCard.localScale = Vector3.one;
                }
            }

            // Place this card in slot
            transform.SetParent(slot);
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            
            // Apply flip rotation: -60 degrees on X axis
            StartCoroutine(FlipCardInSlot());
        }
        
        /// <summary>
        /// Smooth flip animation when card enters slot.
        /// Rotates card -60 degrees on X axis.
        /// </summary>
        private IEnumerator FlipCardInSlot()
        {
            Quaternion startRotation = transform.localRotation;
            Quaternion targetRotation = Quaternion.Euler(-60f, 0f, 0f);
           // Vector3 targetScale = Vector3.one;
            float duration = 0.3f; // Animation duration
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Smooth easing
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, smoothT);
                
                yield return null;
            }
            
            // Ensure final rotation is exact
            transform.localRotation = targetRotation;
        }


        private void ReturnToPanelOrStart()
        {
            // If was dragged from slot, find token panel
            if (_isDraggingFromSlot)
            {
                // Try to find token panel by tag first
                GameObject tokenArea = null;
                
                try
                {
                    tokenArea = GameObject.FindGameObjectWithTag("TokenPanel");
                }
                catch (UnityException)
                {
                    // Tag not defined, try finding by name instead
                    tokenArea = GameObject.Find("TokenSpawnArea");
                    
                    // If still not found, search in GameManager
                    if (tokenArea == null)
                    {
                        var gameManager = FindObjectOfType<MedicalTerminology.Managers.GameManager>();
                        if (gameManager != null)
                        {
                            // Try to get tokenSpawnArea from GameManager via reflection
                            var field = gameManager.GetType().GetField("tokenSpawnArea", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                var tokenTransform = field.GetValue(gameManager) as Transform;
                                if (tokenTransform != null)
                                    tokenArea = tokenTransform.gameObject;
                            }
                        }
                    }
                }
                
                if (tokenArea != null)
                {
                    transform.SetParent(tokenArea.transform);
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                    return;
                }
            }

            // Otherwise return to original position
            transform.SetParent(_startParent);
            transform.SetSiblingIndex(_startSiblingIndex);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}