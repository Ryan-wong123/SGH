using UnityEngine; // Imports Unity engine types such as MonoBehaviour, GameObject, Cursor, Time, and Debug.
using Photon.Pun; // Imports Photon networking types such as MonoBehaviourPun, PhotonNetwork, and PunRPC.
using System.Collections; // Imports IEnumerator for Unity coroutines.
using TMPro; // Imports TextMeshProUGUI for warning message text.

public class PuzzleTrigger : MonoBehaviourPun, IInteractable, IHighlightable // Defines a networked puzzle trigger that can be interacted with and highlighted.
{ // Opens the PuzzleTrigger class body.
    [Header("Refs")] // Groups object reference fields in the Inspector.
    [SerializeField] private PuzzleStateManager puzzleStateManager; // Stores the manager that opens and tracks the current puzzle stage.
    [SerializeField] private ClueUI clueUI; // Stores an optional clue UI reference, while ClueUI.Instance can be used as fallback.

    [Header("Clue Selection")] // Groups clue selection fields in the Inspector.
    [SerializeField] private string clueId; // Stores the clue ID defined in ClueUI.clues.

    [Header("Clue Flow")] // Groups clue flow fields in the Inspector.
    [Tooltip("If true, show the clue page first (if clueId exists). If false, open puzzle immediately.")] // Explains how the clue-first toggle behaves.
    [SerializeField] private bool startWithClue = true; // Stores whether interaction should show a clue before opening the puzzle.
    [SerializeField] private bool restrictToStage; // Prevents this world trigger from opening a different puzzle stage.
    [SerializeField] private PuzzleStateManager.PuzzleStage requiredStage = PuzzleStateManager.PuzzleStage.Puzzle1; // Identifies the stage this trigger belongs to.

    [Header("Settings")] // Groups general trigger settings in the Inspector.
    [Tooltip("If true, this trigger will NOT be destroyed after puzzle completion.")] // Explains the completion lifetime setting.
    public bool dontDestroyOnCompletion = false; // Stores whether this trigger should remain after the puzzle is completed.

    private HighlightEffect highlightEffect; // Caches the highlight effect used by the highlight interface methods.

    // Track if this clue was already shown (per client; master decides the broadcast) // Explains the clueAlreadyViewed flag.
    private bool clueAlreadyViewed = false; // Tracks whether this clue has already been shown for the current client/session path.

    [Header("UI Feedback")] // Groups blocked-attempt UI feedback fields in the Inspector.
    [SerializeField] private GameObject alreadyPlayedImage; // Stores the UI object shown when the puzzle cannot be attempted.
    [SerializeField] private TextMeshProUGUI warningMessage; // Stores the text element used to display the blocked-attempt reason.
    [SerializeField] private float showDuration = 2f; // Stores how many seconds the feedback UI should remain visible.

    private void Awake() // Runs Unity initialization before the first frame.
    { // Opens the Awake method body.
        gameObject.layer = LayerMask.NameToLayer("Interactable"); // Places this trigger on the Interactable layer for raycast detection.
        highlightEffect = GetComponent<HighlightEffect>(); // Finds the optional highlight effect on this trigger.

        if(alreadyPlayedImage != null) // Checks whether the blocked-attempt UI object is assigned.
        { // Opens the assigned-feedback-image block.
            alreadyPlayedImage.SetActive(false); // Hides the blocked-attempt UI at startup.
        } // Closes the assigned-feedback-image block.

    } // Closes the Awake method body.

    // IInteractable // Marks the start of the IInteractable implementation.
    public void Interact() // Handles the player interaction with this puzzle trigger.
    { // Opens the Interact method body.
        if (puzzleStateManager == null) // Checks whether the required puzzle state manager is missing.
        { // Opens the missing-manager block.
            Debug.LogWarning("PuzzleTrigger: PuzzleStateManager is not assigned."); // Logs a warning so the scene setup issue is visible.
            return; // Stops interaction because the puzzle cannot open without a manager.
        } // Closes the missing-manager block.

        if (!IsCurrentStage()) return; // A later puzzle's world object must not open the current puzzle early.

        int submitOwnerActorNumber = PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null // Starts calculating the actor number for the submitting player.
            ? PhotonNetwork.LocalPlayer.ActorNumber // Uses the Photon actor number when connected.
            : -1; // Uses -1 as a local/offline fallback actor number.

        // Fair-turn gate // Marks the check that prevents unfair or repeated puzzle attempts.
        if (!puzzleStateManager.TryBeginAttempt(out string reason)) // Asks the puzzle manager whether a new attempt may begin.
        { // Opens the blocked-attempt block.
            Debug.Log(reason); // Logs the reason the attempt was blocked.
            ShowAlreadyPlayedUI(reason); // Shows the blocked-attempt reason in the UI.
            return; // Stops interaction because the attempt is not allowed.
        } // Closes the blocked-attempt block.

        // If this trigger should NOT start with clue OR clueId is empty -> open puzzle immediately // Explains the immediate-open branch.
        if (!startWithClue || string.IsNullOrWhiteSpace(clueId)) // Checks whether clue display should be skipped.
        { // Opens the immediate-open block.
            // Keep consistent network behavior: open for everyone // Explains why the puzzle open is broadcast.
            if (PhotonNetwork.IsConnected) // Checks whether the game is connected to Photon.
                photonView.RPC(nameof(RPC_OpenPuzzle), RpcTarget.All, submitOwnerActorNumber); // Tells every client to open the puzzle.
            else // Handles offline play.
                OpenPuzzle(submitOwnerActorNumber); // Opens the puzzle locally.

            return; // Stops after opening the puzzle.
        } // Closes the immediate-open block.

        // If not connected, do local-only fallback // Explains the offline clue path.
        if (!PhotonNetwork.IsConnected) // Checks whether Photon is not connected.
        { // Opens the offline clue block.
            ShowClueLocalThenOpen(clueId, submitOwnerActorNumber); // Shows the clue locally and opens the puzzle afterward.
            return; // Stops because the offline clue path has been started.
        } // Closes the offline clue block.

        // Only one authority decides showing clue for everyone (Master) // Explains the master-client authority rule.
        if (!PhotonNetwork.IsMasterClient) // Checks whether this client is not the Photon master client.
        { // Opens the non-master request block.
            photonView.RPC(nameof(RPC_RequestShowClue), RpcTarget.MasterClient, clueId); // Asks the master client to decide whether to show the clue.
            return; // Stops because the master client will handle the clue flow.
        } // Closes the non-master request block.

        // Master clicked // Marks the direct master-client path.
        HandleShowClueAsMaster(clueId, submitOwnerActorNumber); // Lets the master client decide and broadcast the clue or puzzle open.
    } // Closes the Interact method body.

    [PunRPC] // Allows Photon to call this method through RPC.
    private void RPC_RequestShowClue(string requestedClueId, PhotonMessageInfo info) // Receives a non-master client's request to show a clue.
    { // Opens the clue request RPC method body.
        if (!PhotonNetwork.IsMasterClient) return; // Ignores the request if this client is not the master client.
        int submitOwnerActorNumber = info.Sender != null ? info.Sender.ActorNumber : -1; // Gets the requesting player's actor number or -1 if missing.
        HandleShowClueAsMaster(requestedClueId, submitOwnerActorNumber); // Handles the clue flow as the master client.
    } // Closes the clue request RPC method body.

    private void HandleShowClueAsMaster(string id, int submitOwnerActorNumber) // Decides whether to show the clue or skip straight to the puzzle.
    { // Opens the master clue handler body.
        // If already shown once, skip clue and open puzzle for everyone // Explains the repeated-clue branch.
        if (clueAlreadyViewed) // Checks whether this clue has already been shown.
        { // Opens the clue-already-viewed block.
            photonView.RPC(nameof(RPC_OpenPuzzle), RpcTarget.All, submitOwnerActorNumber); // Opens the puzzle for every client without showing the clue again.
            return; // Stops because the puzzle open has been broadcast.
        } // Closes the clue-already-viewed block.

        clueAlreadyViewed = true; // Marks the clue as viewed before broadcasting it.

        // Broadcast: everyone shows the same clue UI now // Explains the clue broadcast.
        photonView.RPC(nameof(RPC_ShowClueForAll), RpcTarget.All, id, submitOwnerActorNumber); // Tells every client to show the clue and then open the puzzle.
    } // Closes the master clue handler body.

    [PunRPC] // Allows Photon to call this method through RPC.
    private void RPC_ShowClueForAll(string id, int submitOwnerActorNumber) // Shows a clue on each client and opens the puzzle afterward.
    { // Opens the show-clue RPC method body.
        // Each client shows UI locally (UI itself isn't networked) // Explains why each client looks up its own UI.
        var ui = (clueUI != null) ? clueUI : ClueUI.Instance; // Uses the assigned clue UI or falls back to the singleton instance.

        if (ui != null && ui.HasClueId(id)) // Checks whether a clue UI exists and contains the requested clue.
        { // Opens the valid-clue-ui block.
            ui.ShowById(id, onClosed: () => // Shows the clue and registers a callback for when the clue UI closes.
            { // Opens the clue-closed callback body.
                // When player closes clue UI, open puzzle locally for them // Explains the callback behavior.
                OpenPuzzle(submitOwnerActorNumber); // Opens the puzzle after the player closes the clue UI.
            }); // Closes the ShowById call.
        } // Closes the valid-clue-ui block.
        else // Handles missing clue UI or missing clue ID.
        { // Opens the missing-clue fallback block.
            Debug.LogWarning($"[PuzzleTrigger] ClueUI missing or clue id not found: '{id}'"); // Logs a warning that the clue cannot be shown.
            OpenPuzzle(submitOwnerActorNumber); // Opens the puzzle anyway so interaction can continue.
        } // Closes the missing-clue fallback block.
    } // Closes the show-clue RPC method body.

    [PunRPC] // Allows Photon to call this method through RPC.
    private void RPC_OpenPuzzle(int submitOwnerActorNumber) // Receives a network request to open the puzzle.
    { // Opens the open-puzzle RPC method body.
        OpenPuzzle(submitOwnerActorNumber); // Opens the puzzle locally for this client.
    } // Closes the open-puzzle RPC method body.

    private void ShowClueLocalThenOpen(string id, int submitOwnerActorNumber) // Handles the offline clue-then-puzzle flow.
    { // Opens the local clue flow method body.
        var ui = (clueUI != null) ? clueUI : ClueUI.Instance; // Uses the assigned clue UI or falls back to the singleton instance.

        if (ui != null && ui.HasClueId(id)) // Checks whether the requested clue can be shown.
            ui.ShowById(id, onClosed: () => OpenPuzzle(submitOwnerActorNumber)); // Shows the clue and opens the puzzle when it closes.
        else // Handles missing UI or missing clue.
            OpenPuzzle(submitOwnerActorNumber); // Opens the puzzle immediately as a fallback.
    } // Closes the local clue flow method body.

    private void OpenPuzzle(int submitOwnerActorNumber = -1) // Opens the current puzzle stage panel for the given submit owner.
    { // Opens the OpenPuzzle method body.
        Cursor.lockState = CursorLockMode.None; // Unlocks the cursor for UI interaction.
        Cursor.visible = true; // Shows the cursor while the puzzle UI is open.
        puzzleStateManager.OpenCurrentStagePanel(submitOwnerActorNumber); // Delegates opening the current puzzle stage to the puzzle state manager.
    } // Closes the OpenPuzzle method body.

    private bool IsCurrentStage() => !restrictToStage || (puzzleStateManager != null && puzzleStateManager.CurrentStage == requiredStage);

    public string GetInteractionPrompt() => IsCurrentStage() ? "Press E to start puzzle" : string.Empty; // Hides the prompt until this trigger's stage.

    // IHighlightable // Marks the start of the IHighlightable implementation.
    public void Highlight() { if (IsCurrentStage()) highlightEffect?.Highlight(); } // Highlights only a currently usable puzzle trigger.
    public void Unhighlight() => highlightEffect?.Unhighlight(); // Turns off the highlight effect if it exists.

    public void ClosePuzzle() // Closes puzzle UI mode and restores gameplay cursor state.
    { // Opens the ClosePuzzle method body.
        Time.timeScale = 1f; // Restores normal game time.
        Cursor.lockState = CursorLockMode.Locked; // Locks the cursor back to the game view.
        Cursor.visible = false; // Hides the cursor after closing the puzzle.
    } // Closes the ClosePuzzle method body.


    private void ShowAlreadyPlayedUI(string message) // Shows temporary feedback when the puzzle cannot be attempted.
    { // Opens the already-played UI method body.
        if (alreadyPlayedImage == null) return; // Stops if the feedback UI object is not assigned.

        StopAllCoroutines(); // Stops any existing temporary feedback coroutine.
        StartCoroutine(ShowImageTemporarily(message)); // Starts a fresh feedback display coroutine.
    } // Closes the already-played UI method body.

    private IEnumerator ShowImageTemporarily(string message) // Shows the blocked-attempt UI for a limited time.
    { // Opens the temporary image coroutine body.
        alreadyPlayedImage.SetActive(true); // Shows the blocked-attempt UI object.
        warningMessage.text = message; // Displays the blocked-attempt reason text.
        yield return new WaitForSecondsRealtime(showDuration); // Hides the warning even while a clue pauses scaled game time.
        alreadyPlayedImage.SetActive(false); // Hides the blocked-attempt UI object.
    } // Closes the temporary image coroutine body.
} // Closes the PuzzleTrigger class body.
