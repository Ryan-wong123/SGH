using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class Activity9DraggableCard : DraggableCard
{
    [SerializeField] private Transform panel3GridLayout;
    [SerializeField] public string symptomName;
    [SerializeField] public Sprite backSprite;

    private bool isFlipped = false;

    protected override void Start()
    {
        base.Start();
        ShowFront();
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

    
        if (transform.parent == canvas.transform || transform.parent == null)
        {
            bool fromPanel2 = lastSlotOrParent != null && lastSlotOrParent.parent == panel2GridLayout;
            bool fromPanel3 = lastSlotOrParent != null && lastSlotOrParent.parent == panel3GridLayout;

            if (IsPointerOverPanel("Panel1") && fromPanel2)
            {
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
            }
            else if (IsPointerOverPanel("Panel2") && fromPanel2 && !panel1GridLayout.gameObject.activeSelf)
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                SnapTo(emptySlot ?? panel2GridLayout, 100, -100, 0.6409343f, 0.66764f);
            }
            else if (IsPointerOverPanel("Panel2") && fromPanel2)
            {
                SnapTo(panel1GridLayout, 100, -100, 0.6409343f, 0.66764f);
            }
            else if (IsPointerOverPanel("Panel3") && fromPanel2)
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                SnapTo(emptySlot ?? panel2GridLayout, 100, -100, 0.6409343f, 0.66764f);
            }
            else if ((IsPointerOverPanel("Panel3") && fromPanel3) || (IsPointerOverPanel("Panel2") && fromPanel3))
            {
                Transform emptySlot = FindFirstEmptyChild(panel2GridLayout);
                SnapTo(emptySlot ?? panel2GridLayout, 100, -100, 0.6409343f, 0.66764f);
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
