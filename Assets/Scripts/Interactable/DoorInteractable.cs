using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public bool isLocked = false;
    public bool requiresKey = false;
    public string keyName = "Key";

    private bool isOpen = false;

    public void Interact()
    {
        if (isLocked)
        {
            Debug.Log("Door is locked.");
            return;
        }

        if (requiresKey && !InventorySystem.HasItem(keyName))
        {
            Debug.Log("Door requires a key.");
            return;
        }

        ToggleDoor();
    }

    public string GetInteractionPrompt()
    {
        if (isLocked)
        {
            return "This door is locked.";
        }

        if (requiresKey && !InventorySystem.HasItem(keyName))
        {
            return "Requires a key.";
        }

        return isOpen ? "Press E to close door" : "Press E to open door";
    }

    private void ToggleDoor()
    {
        if (!isOpen)
        {
            transform.eulerAngles = new Vector3(0, 90, 0);
            isOpen = true;
        }
        else
        {
            transform.eulerAngles = new Vector3(0, 0, 0);
            isOpen = false;
        }
    }
}
