// DropSlot.cs (add these checks)
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;

public class DropSlot : MonoBehaviour, IDropHandler
{
    [Header("Tint Colors")]
    [SerializeField] protected Color correctColor = Color.green;
    [SerializeField] protected Color wrongColor = Color.red;

    public static List<DropSlot> allDropSlots = new();
    protected Image cardImage;

    [SerializeField] private Vector2 positionSnap;
    [SerializeField] private Vector2 scaleSnap;

    public virtual bool IsCardCorrect(DraggableCard card) => false;

    protected virtual void Start()
    {
        allDropSlots.Add(this);
    }
    protected virtual void OnDestroy()
    {
        allDropSlots.Remove(this);
    }
    public List<DraggableCard> GetCardsInSlot()
    {
        var cards = new List<DraggableCard>();

        // If your slot holds the card directly as a child:
        foreach (Transform child in transform)
        {
            var card = child.GetComponent<DraggableCard>();
            if (card != null) cards.Add(card);
        }

        return cards;
    }
    public virtual void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        // NETWORK: only owner can perform the drop action
        var pv = droppedObj.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
            return;

        if (transform.childCount > 0)
        {
            Debug.Log("Slot already occupied");
            TrySnapBack(droppedObj);
            return;
        }

        DraggableCard card = droppedObj.GetComponent<DraggableCard>();
        if (card != null)
        {
            AcceptCard(card);
        }
    }

    protected virtual void AcceptCard(DraggableCard card)
    {
        card.transform.SetParent(transform);

        var rect = card.GetComponent<RectTransform>();
        rect.anchoredPosition = positionSnap;
        rect.localScale = scaleSnap;

        card.trueHome = transform.parent;
        cardImage = card.GetComponent<Image>();
    }

    protected virtual void TrySnapBack(GameObject droppedObj)
    {
        DraggableCard card = droppedObj?.GetComponent<DraggableCard>();
        card?.SnapBack();
    }

    public static void ResetAllSlots()
    {
        foreach (DropSlot slot in allDropSlots)
        {
            slot.ClearSlot();
        }
    }

    public void ClearSlot()
    {
        foreach (Transform child in transform)
        {
            child.SetParent(null);
        }
    }
}
