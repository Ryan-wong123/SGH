using UnityEngine;

public class PuzzleTrigger : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Refs")]
    [SerializeField] private PuzzleStateManager puzzleStateManager;

    private HighlightEffect highlightEffect;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        highlightEffect = GetComponent<HighlightEffect>();
    }

    // IInteractable
    public void Interact()
    {
        if (puzzleStateManager == null)
        {
            Debug.LogWarning("PuzzleTrigger: PuzzleStateManager is not assigned.");
            return;
        }

        if (puzzleStateManager.TryBeginAttempt(out string reason))
        {
            // Allow cursor and open panel for current stage
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            puzzleStateManager.OpenCurrentStagePanel();
        }
        else
        {
            Debug.Log(reason);
            // Optionally show a UI toast here
        }
    }

    public string GetInteractionPrompt() => "Press E to start puzzle";

    // IHighlightable
    public void Highlight() => highlightEffect?.Highlight();
    public void Unhighlight() => highlightEffect?.Unhighlight();

    public void ClosePuzzle()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
