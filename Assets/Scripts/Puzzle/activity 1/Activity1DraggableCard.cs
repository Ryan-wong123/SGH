using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Activity1DraggableCard : DraggableCard
{
    [SerializeField] private Transform panel3GridLayout;
    [SerializeField] public string symptomName;
    [SerializeField] public Sprite backSprite;

    private bool isFlipped = false;

    // -----------------------------
    // FIX: Only DropSlots accept drops for Panel2.
    // If dropped on Panel2 background (not on a slot), revert to drag origin.
    // BUT allow dropping on Panel1 background (to drag back to right panel).
    // -----------------------------
    private bool acceptedThisDrag = false;
    private Transform dragStartParent;
    private Vector2 dragStartAnchoredPos;
    private Vector2 dragStartScale;

    protected override void Start()
    {
        base.Start();
        ShowFront();
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        // Record drag origin BEFORE base potentially re-parents to canvas.
        dragStartParent = transform.parent;
        dragStartAnchoredPos = rectTransform.anchoredPosition;
        dragStartScale = rectTransform.localScale;

        acceptedThisDrag = false;

        base.OnBeginDrag(eventData);
    }

    /// <summary>
    /// Called by Activity1DropSlot when the card is successfully dropped into a slot.
    /// </summary>
    public void MarkAcceptedThisDrag()
    {
        acceptedThisDrag = true;
    }

    /// <summary>
    /// Revert to exact drag origin. This prevents Panel2 background from "catching" the card.
    /// </summary>
    public void RevertToDragOrigin()
    {
        if (dragStartParent != null)
        {
            transform.SetParent(dragStartParent);
            rectTransform.anchoredPosition = dragStartAnchoredPos;
            rectTransform.localScale = dragStartScale;
        }
        else
        {
            // Fallback to existing default behavior if something is odd
            SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f);
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        // Keep your existing behavior (do not call base.OnEndDrag because your Activity1 already overrides logic)
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == canvas.transform || transform.parent == null)
        {
            // FIX v2:
            // If no slot accepted the card, revert UNLESS the user dropped on Panel1.
            // (Panel1 is your "drag back to right panel" action and is not a slot.)
            bool droppedOnPanel1 = IsPointerOverPanel("Panel1");

            if (!acceptedThisDrag && !droppedOnPanel1)
            {
                Debug.Log("[Activity1DraggableCard] Dropped not on a slot and not on Panel1 -> revert to drag origin");
                RevertToDragOrigin();
                if (cardImage != null) cardImage.color = Color.white;
                return;
            }

            // ---- Existing Activity 1 logic (UNCHANGED intent) ----
            // Make origin detection more robust to slot hierarchies:
            bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.IsChildOf(panel2GridLayout);
            bool fromPanel3 = lastSlotOrParent != null && lastSlotOrParent.IsChildOf(panel3GridLayout);

            if (IsPointerOverPanel("Panel1") && fromPanel2)
            {
                Debug.Log("snap back to panel 1");
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
            }
            else if (IsPointerOverPanel("Panel2") && fromPanel2 && !panel1GridLayout.gameObject.activeSelf)
            {
                Debug.Log("snap back to panel 2 and panel 1 is not active");
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1);
            }
            else if (IsPointerOverPanel("Panel2") && fromPanel2)
            {
                Debug.Log("snap back to panel 2 from panel 2");
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
            }
            else if (IsPointerOverPanel("Panel3") && fromPanel2)
            {
                Debug.Log("snap back to panel 2 from panel 3");
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1);
            }
            else if ((IsPointerOverPanel("Panel3") && fromPanel3) || (IsPointerOverPanel("Panel2") && fromPanel3))
            {
                Debug.Log("snap back to panel 2 or panel 3 from panel 3 ");
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                SnapTo(emptySlot ?? panel2GridLayout, 140, -160, 1, 1);
            }
            else
            {
                SnapTo(trueHome, 100, -100, 0.6409343f, 0.66764f);
            }

            if (cardImage != null) cardImage.color = Color.white;
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Sprite spriteToShow = isFlipped ? backSprite : frontSprite;
            CardPopupManager.Instance.ShowCard(spriteToShow);
        }
    }

    public override void ResetCard()
    {
        isFlipped = false;
        ShowFront();
        base.ResetCard();
    }

    public void ShowFront()
    {
        if (cardImage != null && frontSprite != null)
        {
            isFlipped = false;
            cardImage.sprite = frontSprite;
        }
    }

    public void ShowBack()
    {
        if (cardImage != null && backSprite != null)
        {
            isFlipped = true;
            cardImage.sprite = backSprite;
        }
    }
}
