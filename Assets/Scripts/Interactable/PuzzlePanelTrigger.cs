using UnityEngine;
using Photon.Pun;

public class PuzzleTrigger : MonoBehaviourPun, IInteractable, IHighlightable
{
    [Header("Refs")]
    [SerializeField] private PuzzleStateManager puzzleStateManager;
    [SerializeField] private ClueUI clueUI; // optional; ClueUI.Instance will also work

    [Header("Clue Selection")]
    [SerializeField] private string clueId; // ID defined in ClueUI.clues

    [Header("Clue Flow")]
    [Tooltip("If true, show the clue page first (if clueId exists). If false, open puzzle immediately.")]
    [SerializeField] private bool startWithClue = true;

    [Header("Settings")]
    [Tooltip("If true, this trigger will NOT be destroyed after puzzle completion.")]
    public bool dontDestroyOnCompletion = false;

    private HighlightEffect highlightEffect;

    // Track if this clue was already shown (per client; master decides the broadcast)
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

        // Fair-turn gate
        if (!puzzleStateManager.TryBeginAttempt(out string reason))
        {
            Debug.Log(reason);
            return;
        }

        // ✅ If this trigger should NOT start with clue OR clueId is empty -> open puzzle immediately
        if (!startWithClue || string.IsNullOrWhiteSpace(clueId))
        {
            // Keep consistent network behavior: open for everyone
            if (PhotonNetwork.IsConnected)
                photonView.RPC(nameof(RPC_OpenPuzzle), RpcTarget.All);
            else
                OpenPuzzle();

            return;
        }

        // If not connected, do local-only fallback
        if (!PhotonNetwork.IsConnected)
        {
            ShowClueLocalThenOpen(clueId);
            return;
        }

        // Only one authority decides showing clue for everyone (Master)
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_RequestShowClue), RpcTarget.MasterClient, clueId);
            return;
        }

        // Master clicked
        HandleShowClueAsMaster(clueId);
    }

    [PunRPC]
    private void RPC_RequestShowClue(string requestedClueId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        HandleShowClueAsMaster(requestedClueId);
    }

    private void HandleShowClueAsMaster(string id)
    {
        // If already shown once, skip clue and open puzzle for everyone
        if (clueAlreadyViewed)
        {
            photonView.RPC(nameof(RPC_OpenPuzzle), RpcTarget.All);
            return;
        }

        clueAlreadyViewed = true;

        // Broadcast: everyone shows the same clue UI now
        photonView.RPC(nameof(RPC_ShowClueForAll), RpcTarget.All, id);
    }

    [PunRPC]
    private void RPC_ShowClueForAll(string id)
    {
        // Each client shows UI locally (UI itself isn't networked)
        var ui = (clueUI != null) ? clueUI : ClueUI.Instance;

        if (ui != null && ui.HasClueId(id))
        {
            ui.ShowById(id, onClosed: () =>
            {
                // When player closes clue UI, open puzzle locally for them
                OpenPuzzle();
            });
        }
        else
        {
            Debug.LogWarning($"[PuzzleTrigger] ClueUI missing or clue id not found: '{id}'");
            OpenPuzzle();
        }
    }

    [PunRPC]
    private void RPC_OpenPuzzle()
    {
        OpenPuzzle();
    }

    private void ShowClueLocalThenOpen(string id)
    {
        var ui = (clueUI != null) ? clueUI : ClueUI.Instance;

        if (ui != null && ui.HasClueId(id))
            ui.ShowById(id, onClosed: OpenPuzzle);
        else
            OpenPuzzle();
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
