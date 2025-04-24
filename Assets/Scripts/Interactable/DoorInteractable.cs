using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private string keyName = "Key";

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
            Debug.Log($"Door requires a {keyName}");
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
            return $"Door requires a {keyName}";
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
