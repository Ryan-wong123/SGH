using UnityEngine;

public class PuzzlePanelTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject puzzleUIPanel;

    public void Interact()
    {
        puzzleUIPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f; // optional pause
    }

    public string GetInteractionPrompt()
    {
        return "Press E to solve puzzle";
    }
}
