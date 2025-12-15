using UnityEngine;
using Photon.Pun;
using System;

public class PuzzleTrigger : MonoBehaviourPun, IInteractable, IHighlightable
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

        if (string.IsNullOrWhiteSpace(clueId))
        {
            OpenPuzzle();
            return;
        }

        // Only one authority broadcasts (Master). If you prefer owner, switch to ownership model.
        if (!PhotonNetwork.IsConnected)
        {
            // Offline fallback
            ShowClueLocalThenOpen(clueId);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            // Ask Master to show this clue for everyone
            photonView.RPC(nameof(RPC_RequestShowClue), RpcTarget.MasterClient, clueId);
            return;
        }

        // Master clicked
        HandleShowClueAsMaster(clueId);
    }
    [PunRPC]
    private void RPC_RequestShowClue(string requestedClueId, PhotonMessageInfo info)
    {
        // Master receives request from any client
        if (!PhotonNetwork.IsMasterClient) return;
        HandleShowClueAsMaster(requestedClueId);
    }

    private void HandleShowClueAsMaster(string id)
    {
        if (clueAlreadyViewed)
        {
            // Already shown -> just open puzzle for everyone (optional)
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
        // Each client shows UI locally (no networked UI objects)
        if (ClueUI.Instance != null && ClueUI.Instance.HasClueId(id))
        {
            ClueUI.Instance.ShowById(id, onClosed: () =>
            {
                // When each player closes their own clue UI, open puzzle locally for them
                // If you want the puzzle to open ONLY when everyone closed, tell me and I’ll add a ready-check.
                OpenPuzzle();
            });
        }
        else
        {
            Debug.LogWarning($"[PuzzlePanelTrigger] ClueUI missing or clue id not found: '{id}'");
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
        if (ClueUI.Instance != null && ClueUI.Instance.HasClueId(id))
            ClueUI.Instance.ShowById(id, onClosed: OpenPuzzle);
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
