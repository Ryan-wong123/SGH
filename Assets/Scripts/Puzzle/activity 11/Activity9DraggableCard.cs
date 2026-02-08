using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Activity9DraggableCard : DraggableCard
{
    [SerializeField] public string lapsesAnswer;
    public bool isLocked = false;

    // FIX: remember where this drag started (so reject returns into the same slot)
    private Transform dragStartParent;
    private Vector2 dragStartAnchoredPos;
    private Vector2 dragStartScale;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return; // ❌ prevent drag if locked

        // Record drag origin BEFORE base potentially re-parents to canvas
        dragStartParent = transform.parent;
        dragStartAnchoredPos = rectTransform.anchoredPosition;
        dragStartScale = rectTransform.localScale;

        base.OnBeginDrag(eventData); // ✅ default drag
    }

    /// <summary>
    /// FIX: If drop is rejected (e.g. target slot occupied), return to exact previous slot.
    /// </summary>
    public void RevertToDragOrigin()
    {
        if (dragStartParent != null)
        {
            // IMPORTANT: worldPositionStays=false keeps UI local placement stable
            transform.SetParent(dragStartParent, false);
            rectTransform.anchoredPosition = dragStartAnchoredPos;
            rectTransform.localScale = dragStartScale;
        }
        else
        {
            // fallback (should rarely happen)
            SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f);
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // FIX: If we are already inside a slot (NOT on the canvas), do nothing.
        // This prevents panel snapping logic from overriding a successful revert/accept.
        if (transform.parent != null && transform.parent != canvas.transform)
            return;

        if (transform.parent == canvas.transform || transform.parent == null)
        {
            bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

            // 1. From panel2, dropped on panel1(not a slot) → snap to panel1
            if (IsPointerOverPanel("Panel1") && fromPanel2)
            {
                Debug.Log("snap to panel 1");
                SnapTo(panel1GridLayout, 150, -150, 0.66f, 0.66f);
            }
            // 2. From panel2, dropped on panel2(not a slot)
            else if (IsPointerOverPanel("Panel2") && fromPanel2)
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                if (emptySlot != null)
                {
                    Debug.Log("snap to empty slot");
                    SnapTo(emptySlot, 150, -150, 0.66f, 0.66f);
                    rectTransform.localScale = Vector2.one;
                }
                else
                {
                    Debug.Log("snap to panel 2");
                    SnapTo(panel2GridLayout, 150, -150, 0.66f, 0.66f);
                    rectTransform.localScale = Vector2.one;
                }
            }
            else
            {
                Debug.Log("snap to true home");
                SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f);
            }

            var img = GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
    }

    public override void ResetCard()
    {
        base.ResetCard();
        isLocked = false;
        GetComponent<Image>().color = Color.white; // reset color when reused
    }
}
