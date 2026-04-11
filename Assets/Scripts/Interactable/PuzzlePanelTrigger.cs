using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;

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

    [Header("UI Feedback")]
    [SerializeField] private GameObject alreadyPlayedImage;
    [SerializeField] private TextMeshProUGUI warningMessage;
    [SerializeField] private float showDuration = 2f;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        highlightEffect = GetComponent<HighlightEffect>();

        if(alreadyPlayedImage != null)
        {
            alreadyPlayedImage.SetActive(false);
        }

    }

    // IInteractable
    public void Interact()
    {
        if (puzzleStateManager == null)
        {
            Debug.LogWarning("PuzzleTrigger: PuzzleStateManager is not assigned.");
            return;
        }

        int submitOwnerActorNumber = PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : -1;

        // Fair-turn gate
        if (!puzzleStateManager.TryBeginAttempt(out string reason))
        {
            Debug.Log(reason);
            ShowAlreadyPlayedUI(reason);
            return;
        }

        // ✅ If this trigger should NOT start with clue OR clueId is empty -> open puzzle immediately
        if (!startWithClue || string.IsNullOrWhiteSpace(clueId))
        {
            // Keep consistent network behavior: open for everyone
            if (PhotonNetwork.IsConnected)
                photonView.RPC(nameof(RPC_OpenPuzzle), RpcTarget.All, submitOwnerActorNumber);
            else
                OpenPuzzle(submitOwnerActorNumber);

            return;
        }

        // If not connected, do local-only fallback
        if (!PhotonNetwork.IsConnected)
        {
            ShowClueLocalThenOpen(clueId, submitOwnerActorNumber);
            return;
        }

        // Only one authority decides showing clue for everyone (Master)
        if (!PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_RequestShowClue), RpcTarget.MasterClient, clueId);
            return;
        }

        // Master clicked
        HandleShowClueAsMaster(clueId, submitOwnerActorNumber);
    }

    [PunRPC]
    private void RPC_RequestShowClue(string requestedClueId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        int submitOwnerActorNumber = info.Sender != null ? info.Sender.ActorNumber : -1;
        HandleShowClueAsMaster(requestedClueId, submitOwnerActorNumber);
    }

    private void HandleShowClueAsMaster(string id, int submitOwnerActorNumber)
    {
        // If already shown once, skip clue and open puzzle for everyone
        if (clueAlreadyViewed)
        {
            photonView.RPC(nameof(RPC_OpenPuzzle), RpcTarget.All, submitOwnerActorNumber);
            return;
        }

        clueAlreadyViewed = true;

        // Broadcast: everyone shows the same clue UI now
        photonView.RPC(nameof(RPC_ShowClueForAll), RpcTarget.All, id, submitOwnerActorNumber);
    }

    [PunRPC]
    private void RPC_ShowClueForAll(string id, int submitOwnerActorNumber)
    {
        // Each client shows UI locally (UI itself isn't networked)
        var ui = (clueUI != null) ? clueUI : ClueUI.Instance;

        if (ui != null && ui.HasClueId(id))
        {
            ui.ShowById(id, onClosed: () =>
            {
                // When player closes clue UI, open puzzle locally for them
                OpenPuzzle(submitOwnerActorNumber);
            });
        }
        else
        {
            Debug.LogWarning($"[PuzzleTrigger] ClueUI missing or clue id not found: '{id}'");
            OpenPuzzle(submitOwnerActorNumber);
        }
    }

    [PunRPC]
    private void RPC_OpenPuzzle(int submitOwnerActorNumber)
    {
        OpenPuzzle(submitOwnerActorNumber);
    }

    private void ShowClueLocalThenOpen(string id, int submitOwnerActorNumber)
    {
        var ui = (clueUI != null) ? clueUI : ClueUI.Instance;

        if (ui != null && ui.HasClueId(id))
            ui.ShowById(id, onClosed: () => OpenPuzzle(submitOwnerActorNumber));
        else
            OpenPuzzle(submitOwnerActorNumber);
    }

    private void OpenPuzzle(int submitOwnerActorNumber = -1)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        puzzleStateManager.OpenCurrentStagePanel(submitOwnerActorNumber);
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


    private void ShowAlreadyPlayedUI(string message)
    {
        if (alreadyPlayedImage == null) return;

        StopAllCoroutines();
        StartCoroutine(ShowImageTemporarily(message));
    }

    private IEnumerator ShowImageTemporarily(string message)
    {
        alreadyPlayedImage.SetActive(true);
        warningMessage.text = message;
        yield return new WaitForSeconds(showDuration);
        alreadyPlayedImage.SetActive(false);
    }
}
