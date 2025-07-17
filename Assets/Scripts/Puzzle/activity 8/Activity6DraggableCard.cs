using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Activity6DraggableCard : DraggableCard
{

    [SerializeField] public string stepName;

    public override void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (transform.parent == canvas.transform || transform.parent == null)
        {
            bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;

            // 1. From panel2, dropped on panel1(not a slot) → snap to panel1
            if (IsPointerOverPanel("Panel1") && fromPanel2)
            {
                Debug.Log("snap to panel 1");
                SnapTo(panel1GridLayout, 150, -150, 0.6409343f, 0.6409343f);
            }
            // 2. From panel2, dropped on panel2(not a slot)
            else if (IsPointerOverPanel("Panel2") && fromPanel2)
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                if (emptySlot != null)
                {
                    Debug.Log("snap to empty slot");
                    SnapTo(emptySlot, 225, -230, 1.55f, 1.55f);
                    rectTransform.localScale = new Vector2(1.55f, 1.55f);
                }
                else
                {
                    Debug.Log("snap to panel 2");
                    SnapTo(panel2GridLayout, 150, -150, 1.55f, 1.55f);
                    rectTransform.localScale = Vector2.one;
                }
            }
            else
            {
                Debug.Log("snap to true home");
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.6409343f);
            }
            var img = GetComponent<Image>();
            if (img != null) img.color = Color.white;

        }
    }
}
