using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzleDragObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform startParent;
    private CanvasGroup canvasGroup;
    private Transform mainCanvas;
    private int startSiblingIndex;
    private Transform handPanel;
    private bool isDraggingFromSlot;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
       
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            mainCanvas = canvas.rootCanvas.transform;
        }

        GameObject panelObj = GameObject.Find("HandPanel");
        if (panelObj != null)
        {
            handPanel = panelObj.transform;
        }
        else
        {
            Debug.LogError("Không tìm thấy object tên 'HandPanel'. Hãy kiểm tra lại tên trong Hierarchy!");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startParent = transform.parent;
        startSiblingIndex = transform.GetSiblingIndex();

        if (transform.parent.CompareTag("Slot"))
        {
            isDraggingFromSlot = true;
        }
        else
        {
            isDraggingFromSlot = false;
        }

        transform.SetParent(mainCanvas);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        GameObject closestSlot = FindClosestSlot();

        if (closestSlot != null)
        {
            DropInSlot(closestSlot.transform);
        }
        else
        {
            ReturnToPanelOrStart();
        }
    }

    private GameObject FindClosestSlot()
    {
        GameObject[] slots = GameObject.FindGameObjectsWithTag("Slot");
        GameObject closest = null;
        float minDistance = 100f;

        foreach (GameObject slot in slots)
        {
            float dist = Vector3.Distance(transform.position, slot.transform.position);
           
            if (dist < minDistance)
            {
                if (slot.transform.childCount == 0)
                {
                    closest = slot;
                    minDistance = dist;
                }
            }
        }
       
        GameObject discard = GameObject.FindGameObjectWithTag("Discard");
        if (discard != null && Vector3.Distance(transform.position, discard.transform.position) < minDistance)
        {
            return discard;
        }

        return closest;
    }

    private void DropInSlot(Transform target)
    {
        if (target.CompareTag("Discard"))
        {
            Destroy(gameObject);
        }
        else
        {
            transform.SetParent(target);
            transform.localPosition = Vector3.zero;
           
            if (PuzzleGameManager.Instance != null)
            {
                PuzzleGameManager.Instance.CheckSlots();
            }
        }
    }

    private void ReturnToPanelOrStart()
    {
        if (isDraggingFromSlot)
        {
            if (handPanel != null)
            {
                transform.SetParent(handPanel);
                transform.SetAsLastSibling();
            }
            else
            {
                transform.SetParent(startParent);
                transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            transform.SetParent(startParent);
            transform.SetSiblingIndex(startSiblingIndex);
        }
    }
}