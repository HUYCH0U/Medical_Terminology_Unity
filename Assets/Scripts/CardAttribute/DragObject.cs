using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragObject : MonoBehaviour
{
    private float zCoord;
    private float snapSpeed = 20f;
    private Vector3 startPosition;
    private Transform startParent; 
    
    private Transform spawnParent;
    private Vector3 spawnPosition;

    private Coroutine currentCoroutine;
    private bool isDragging = false;

    private void Awake()
    {
        if (transform.parent != null)
        {
            spawnParent = transform.parent.parent;
            spawnPosition = transform.parent.position;
        }
    }

    private void OnEnable()
    {
        ResetLayer();
        isDragging = false;
    }

    private void OnMouseDown()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        isDragging = true;
        startPosition = transform.parent.position;
        startParent = transform.parent.parent;

        if (transform.parent.parent != null)
        {
            transform.parent.SetParent(null);
        }

        Vector3 pos = transform.parent.position;
        pos.z = -5f;
        transform.parent.position = pos;

        zCoord = Camera.main.WorldToScreenPoint(pos).z;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;
        ResetLayer();
        CheckForSlot();
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zCoord);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint);
        transform.parent.position = curPosition;
    }

    private void CheckForSlot()
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(transform.parent.position);
        
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Slot"))
            {
                if (collider.transform.childCount > 0)
                {
                    Transform existingCard = collider.transform.GetChild(0);
                    if (existingCard != transform.parent) 
                    {
                        DragObject otherDrag = existingCard.GetComponentInChildren<DragObject>();
                        if (otherDrag != null && otherDrag.currentCoroutine == null)
                        {
                            existingCard.SetParent(null);
                            otherDrag.GoToPosition(startPosition, startParent != null ? startParent.gameObject : null);
                        }
                        else 
                        {
                             GoToPosition(startPosition, startParent != null ? startParent.gameObject : null);
                             return;
                        }
                    }
                }
                
                GoToPosition(collider.transform.position, collider.gameObject);
                return;
            }
            else if (collider.CompareTag("Discard"))
            {
                StartCoroutine(SnapAndDestroy(collider.transform.position));
                return;
            }
        }
        
        GoToPosition(spawnPosition, spawnParent != null ? spawnParent.gameObject : null);
    }

    public void GoToPosition(Vector3 targetPos, GameObject targetParent)
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(SnapToPosition(targetPos, targetParent));
    }

    private void ResetLayer()
    {
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    IEnumerator SnapToPosition(Vector3 targetPos, GameObject targetParent)
    {
        if (targetParent != null)
        {
             targetPos = targetParent.transform.position;
             targetPos.z = -0.1f; 
        }

        while (Vector3.Distance(transform.parent.position, targetPos) > 0.01f)
        {
            transform.parent.position = Vector3.Lerp(transform.parent.position, targetPos, snapSpeed * Time.deltaTime);
            yield return null;
        }

        transform.parent.position = targetPos;

        if (targetParent != null)
        {
            transform.parent.SetParent(targetParent.transform);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckSlotForDisplay();
        }
        
        currentCoroutine = null;
    }

    IEnumerator SnapAndDestroy(Vector3 targetPos)
    {
        isDragging = false;
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        targetPos.z = -0.5f;
        while (Vector3.Distance(transform.parent.position, targetPos) > 0.01f)
        {
            transform.parent.position = Vector3.Lerp(transform.parent.position, targetPos, snapSpeed * Time.deltaTime);
            yield return null;
        }
        GameManager.Instance.CheckSlotForDisplay();
        Destroy(transform.parent.gameObject);

        yield return new WaitForEndOfFrame();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckSlotForDisplay();
        }
    }
}