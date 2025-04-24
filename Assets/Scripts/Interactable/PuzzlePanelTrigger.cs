using UnityEngine;

public class PuzzleTrigger : MonoBehaviour, IInteractable
{
    [Header("Puzzle Panel")]
    [SerializeField] private GameObject puzzleUIPanel;
    [SerializeField] private GameObject closePanelUI;

    public void Interact()
    {
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
            closePanelUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f; // Optional: Pause game
            // Register this puzzle with the SidebarMenuManager
            FindObjectOfType<SidebarMenuManager>().SetActivePuzzle(this);
        }
    }

    public string GetInteractionPrompt()
    {
        return "Press E to start puzzle";
    }

    public void ClosePuzzle()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        puzzleUIPanel.SetActive(false); // hide this specific panel
    }
}
