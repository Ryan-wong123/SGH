using System.Collections;
using UnityEngine;

public class PuzzleTrigger : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Puzzle Panel")]
    private HighlightEffect highlightEffect;
    [SerializeField] private PuzzleStateManager puzzleStateManager;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        highlightEffect = GetComponent<HighlightEffect>();
    }
    public void Highlight() => highlightEffect?.Highlight();
    public void Unhighlight() => highlightEffect?.Unhighlight();

    public void Interact()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Register this puzzle with the SidebarMenuManager
        FindObjectOfType<SidebarMenuManager>().SetActivePuzzle(this);
        puzzleStateManager.OpenCurrentStagePanel();
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
    }

}
