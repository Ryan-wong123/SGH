// DraggableCard.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using static PuzzleStateManager;

public class DraggableCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] protected Canvas canvas;

    [SerializeField] protected Transform panel1GridLayout;  // Right Slot All Symptoms
    [SerializeField] protected Transform panel2GridLayout;  // Left Slot Correct Symptom

    protected Image cardImage;
    [SerializeField] protected Sprite frontSprite;

    protected RectTransform rectTransform;
    protected CanvasGroup canvasGroup;

    [HideInInspector] public Transform trueHome; // last successful grid parent
    public Transform lastSlotOrParent;
    protected Vector3 originalPosition;
    protected Transform originalParent;

    protected virtual void Awake()
    {
        cardImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    protected virtual void Start()
    {
        if (trueHome == null)
            trueHome = transform.parent;
        originalParent = transform.parent;
        originalPosition = transform.position;
    }
    
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        lastSlotOrParent = transform.parent;
        transform.SetParent(canvas.transform);
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // Check if dragged out of a slot
        if (lastSlotOrParent != null && lastSlotOrParent.GetComponent<DropSlot>() != null)
        {
            DropSlot previousSlot = lastSlotOrParent.GetComponent<DropSlot>();

            bool droppedOutsideSlot = transform.parent == canvas.transform || transform.parent.GetComponent<DropSlot>() == null;

            if (droppedOutsideSlot)
            {
                OnCardRemovedFromSlot(previousSlot);
            }   
        }

        if (transform.parent == canvas.transform || transform.parent == null)
        {
            bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

            // 1. From panel2, dropped on panel1(not a slot) → snap to panel1
            if (IsPointerOverPanel("Panel1") && fromPanel2)
            {
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f ,0.66764f);
            }
            // 2. From panel2, dropped on panel2(not a slot)
            else if (IsPointerOverPanel("Panel2") && fromPanel2)
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                if (emptySlot != null)
                {
                    SnapTo(emptySlot, 100, -100, 0.6409343f, 0.66764f);
                    rectTransform.localScale = Vector2.one;
                }
                else
                {
                    SnapTo(panel2GridLayout, 100, -100, 0.6409343f, 0.66764f);
                    rectTransform.localScale = Vector2.one;
                }
            }
            else
            {
                SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f);
            }
            var img = GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
    }

    public void SnapBack()
    {
        if (!panel1GridLayout.gameObject.activeSelf)
        {
            Transform slot = FindFirstEmptyChild(trueHome);

            if (slot != null)
            {
                SnapTo(slot, 100, -100, 0.6409343f, 0.66764f);
                rectTransform.localScale = Vector2.one;
            }
            else
            {
                SnapTo(panel2GridLayout, 100, -100, 0.6409343f, 0.66764f);
                rectTransform.localScale = Vector2.one;
            }
        }
        else
        {
            SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f);
        }
    }

    protected virtual void SnapTo(Transform parent, float xPos, float yPos, float xScale, float yScale )
    {
        transform.SetParent(parent);
        rectTransform.anchoredPosition = new Vector2(xPos, yPos);
        rectTransform.localScale = new Vector2(xScale, yScale);
    }

    protected Transform FindFirstEmptyChild(Transform gridParent)
    {
        foreach (Transform child in gridParent)
        {
            if (child.childCount == 0)
                return child;
        }
        return null;
    }

    protected bool IsPointerOverPanel(string panelTag)
    {
        var results = new List<RaycastResult>();
        var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.CompareTag(panelTag))
                return true;
        }
        return false;
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            Sprite spriteToShow =  frontSprite;
            CardPopupManager.Instance.ShowCard(spriteToShow);
        }
    }

    public virtual void ResetCard()
    {
        transform.SetParent(originalParent);
        transform.position = originalPosition;

        if (cardImage != null)
            cardImage.color = Color.white;
    }

    protected virtual void OnCardRemovedFromSlot(DropSlot previousSlot)
    {
        // To be overridden by child class
    }
}
