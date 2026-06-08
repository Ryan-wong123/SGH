// DropSlot.cs (add these checks)
using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.EventSystems; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.

public class DropSlot : MonoBehaviour, IDropHandler // Declares this type.
{ // Starts this code block.
    [Header("Tint Colors")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] protected Color correctColor = Color.green; // Exposes this private field in the Unity Inspector.
    [SerializeField] protected Color wrongColor = Color.red; // Exposes this private field in the Unity Inspector.

    public static List<DropSlot> allDropSlots = new(); // Declares a field, property, or member used by this script.
    protected Image cardImage; // Declares a field, property, or member used by this script.

    [SerializeField] private Vector2 positionSnap; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Vector2 scaleSnap; // Exposes this private field in the Unity Inspector.

    public virtual bool IsCardCorrect(DraggableCard card) => false; // Declares a field, property, or member used by this script.

    protected virtual void Start() // Declares a field, property, or member used by this script.
    { // Starts this code block.
        allDropSlots.Add(this); // Runs this line of puzzle logic.
    } // Ends this code block.
    protected virtual void OnDestroy() // Declares a field, property, or member used by this script.
    { // Starts this code block.
        allDropSlots.Remove(this); // Runs this line of puzzle logic.
    } // Ends this code block.
    public List<DraggableCard> GetCardsInSlot() // Declares a method used by this script.
    { // Starts this code block.
        var cards = new List<DraggableCard>(); // Creates a local value used by the following logic.

        // If your slot holds the card directly as a child:
        foreach (Transform child in transform) // Repeats this block for each item in a collection.
        { // Starts this code block.
            var card = child.GetComponent<DraggableCard>(); // Creates a local value used by the following logic.
            if (card != null) cards.Add(card); // Checks this condition before continuing.
        } // Ends this code block.

        return cards; // Returns this result to the caller.
    } // Ends this code block.
    public virtual void OnDrop(PointerEventData eventData) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        GameObject droppedObj = eventData.pointerDrag; // Creates a local value used by the following logic.
        if (droppedObj == null) return; // Checks this condition before continuing.

        // NETWORK: only owner can perform the drop action
        var pv = droppedObj.GetComponent<PhotonView>(); // Creates a local value used by the following logic.
        if (pv != null && !pv.IsMine) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (transform.childCount > 0) // Checks this condition before continuing.
        { // Starts this code block.
            Debug.Log("Slot already occupied"); // Writes a diagnostic message to the Unity console.
            TrySnapBack(droppedObj); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        DraggableCard card = droppedObj.GetComponent<DraggableCard>(); // Runs this line of puzzle logic.
        if (card != null) // Checks this condition before continuing.
        { // Starts this code block.
            AcceptCard(card); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    protected virtual void AcceptCard(DraggableCard card) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        card.ApplyLayoutState(transform, positionSnap, scaleSnap); // Runs this line of puzzle logic.

        card.trueHome = transform.parent; // Updates this value.
        cardImage = card.GetComponent<Image>(); // Updates this value.
    } // Ends this code block.

    protected virtual void TrySnapBack(GameObject droppedObj) // Declares a field, property, or member used by this script.
    { // Starts this code block.
        DraggableCard card = droppedObj?.GetComponent<DraggableCard>(); // Runs this line of puzzle logic.
        card?.SnapBack(); // Runs this line of puzzle logic.
    } // Ends this code block.

    public static void ResetAllSlots() // Declares a method used by this script.
    { // Starts this code block.
        foreach (DropSlot slot in allDropSlots) // Repeats this block for each item in a collection.
        { // Starts this code block.
            slot.ClearSlot(); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public void ClearSlot() // Declares a method used by this script.
    { // Starts this code block.
        foreach (Transform child in transform) // Repeats this block for each item in a collection.
        { // Starts this code block.
            child.SetParent(null); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.
} // Ends this code block.
