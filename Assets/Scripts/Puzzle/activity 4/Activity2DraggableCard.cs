using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Activity2DraggableCard : DraggableCard
{
    [SerializeField] public string stepName;

    // FIX: remember exactly where this drag started (so we can return into its own slot)
    private Transform dragStartParent;
    private Vector2 dragStartAnchoredPos;
    private Vector2 dragStartScale;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        // Record origin BEFORE base potentially re-parents to canvas
        dragStartParent = transform.parent;
        dragStartAnchoredPos = rectTransform.anchoredPosition;
        dragStartScale = rectTransform.localScale;

        base.OnBeginDrag(eventData);
    }

    /// <summary>
    /// FIX: When a drop is rejected (e.g., target slot occupied), revert into the original slot.
    /// </summary>
    public void RevertToDragOrigin()
    {
        // If networked, base class already prevents non-owners from moving; keep consistent with your setup
        // (SnapBack() has a similar guard in parent, but we're not modifying parent)
        if (dragStartParent != null)
        {
            transform.SetParent(dragStartParent);
            rectTransform.anchoredPosition = dragStartAnchoredPos;
            rectTransform.localScale = dragStartScale;
        }
        else
        {
            // fallback to your existing behavior
            SnapTo(panel1GridLayout, 100, -100, 0.66f, 0.66f);
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (!TryCompleteDrag())
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
}
