using UnityEngine;

public class PuzzleTrigger : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Refs")]
    [SerializeField] private PuzzleStateManager puzzleStateManager;
    [SerializeField] private ClueUI clueUI;     // <-- Assign your ClueUI manager in Inspector

    [Header("Clue Selection")]
    [SerializeField] private string clueId;     // <-- ID defined in ClueUI.clues

    [Header("Settings")]
    [Tooltip("If true, this trigger will NOT be destroyed after puzzle completion.")]
    public bool dontDestroyOnCompletion = false;

    private HighlightEffect highlightEffect;

    // ✅ Track if this clue was already shown
    private bool clueAlreadyViewed = false;

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

        if (!puzzleStateManager.TryBeginAttempt(out string reason))
        {
            Debug.Log(reason);
            return;
        }

        // First time → show clue
        if (!clueAlreadyViewed && clueUI != null && !string.IsNullOrWhiteSpace(clueId) && clueUI.HasClueId(clueId))
        {
            bool showed = clueUI.ShowById(clueId, onClosed: () =>
            {
                clueAlreadyViewed = true;   // mark clue as viewed
                OpenPuzzle();
            });

            if (!showed)
            {
                clueAlreadyViewed = true;   // mark as viewed anyway
                OpenPuzzle();
            }
        }
        else
        {
            // Already viewed → skip straight to puzzle
            OpenPuzzle();
        }
    }

    private void OpenPuzzle()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        puzzleStateManager.OpenCurrentStagePanel();
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
