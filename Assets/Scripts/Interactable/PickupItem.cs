using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Pickup Info")]
    public string itemName = "Key"; // or "RedCard", etc.

    public void Interact()
    {
        Debug.Log($"Picked up: {itemName}");
        InventorySystem.AddItem(itemName);
        Destroy(gameObject);
    }

    public string GetInteractionPrompt()
    {
        return $"Press E to pick up {itemName}";
    }
}
