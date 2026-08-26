using System; // Imports a namespace used by the clue UI.
using System.Collections.Generic; // Imports a namespace used by the clue UI.
using ExitGames.Client.Photon; // Imports a namespace used by the clue UI.
using Photon.Pun; // Imports a namespace used by the clue UI.
using Photon.Realtime; // Imports a namespace used by the clue UI.
using UnityEngine; // Imports a namespace used by the clue UI.
using UnityEngine.UI; // Imports a namespace used by the clue UI.
using TMPro; // Imports a namespace used by the clue UI.

[Serializable] // Allows Unity to serialize this clue UI data type.
public class ClueEntry // Defines one clue ID and its optional classic panel.
{ // Starts this block.
    [Tooltip("Unique ID for this clue (e.g., 'clue_diary_page1')")] // Shows guidance for this field in the Unity inspector.
    public string id; // Stores the unique clue ID for this entry.

    [Tooltip("Panel GameObject to toggle for this clue (child of mainRoot). Optional if using image detail view only.")] // Shows guidance for this field in the Unity inspector.
    public GameObject panelRoot; // References the optional panel shown for this clue ID.
} // Ends this block.

public class ClueUI : MonoBehaviourPunCallbacks, IOnEventCallback // Declares the clue UI controller and Photon event listener.
{ // Starts this block.
    public static ClueUI Instance; // singleton for easy access from ClueSystem // Stores the shared ClueUI singleton instance.

    [Header("Main Root UI Panel (container for classic panels)")] // Groups related clue UI fields in the Unity inspector.
    [SerializeField] private GameObject mainRoot; // Exposes this private clue UI field in the Unity inspector.

    [Header("Clue Panels (id ? panel) - optional")] // Groups related clue UI fields in the Unity inspector.
    [SerializeField] private List<ClueEntry> clues = new(); // Exposes this private clue UI field in the Unity inspector.

    [Header("General Close Button (inside mainRoot)")] // Groups related clue UI fields in the Unity inspector.
    [SerializeField] private Button closeButton; // Exposes this private clue UI field in the Unity inspector.

    [Header("Unread Badge (on your Clue button)")] // Groups related clue UI fields in the Unity inspector.
    [Tooltip("Red dot (or container) that shows the unread number.")] // Shows guidance for this field in the Unity inspector.
    [SerializeField] private GameObject unreadBadgeRoot; // Exposes this private clue UI field in the Unity inspector.
    [SerializeField] private TMP_Text unreadText; // number label // Exposes this private clue UI field in the Unity inspector.

    [Header("Optional: extra badge panel to auto-close")] // Groups related clue UI fields in the Unity inspector.
    [Tooltip("If you have a small dropdown/panel tied to the badge, assign it so we can SetActive(false).")] // Shows guidance for this field in the Unity inspector.
    [SerializeField] private GameObject badgePanelRoot; // Exposes this private clue UI field in the Unity inspector.

    [Header("Detail Image View")] // Groups related clue UI fields in the Unity inspector.
    [Tooltip("Panel that contains a full-size Image (and its own Close button).")] // Shows guidance for this field in the Unity inspector.
    [SerializeField] private GameObject detailPanelRoot; // Exposes this private clue UI field in the Unity inspector.
    [SerializeField] private Image detailImage; // Exposes this private clue UI field in the Unity inspector.
    [SerializeField] private Button detailCloseButton; // Exposes this private clue UI field in the Unity inspector.
    [SerializeField] private GameObject[] comics; // Exposes this private clue UI field in the Unity inspector.

    [Header("Multiplayer Close Gate")] // Groups related clue UI fields in the Unity inspector.
    [SerializeField] private bool requireAllPlayersToClickClose = true; // Exposes this private clue UI field in the Unity inspector.
    [SerializeField] private string clueCloseId = "SharedClue"; // Exposes this private clue UI field in the Unity inspector.

    [Header("Close Button Auto Size")] // Groups related clue UI fields in the Unity inspector.
    [SerializeField] private Vector2 closeButtonTextPadding = new Vector2(48f, 20f); // Exposes this private clue UI field in the Unity inspector.
    [SerializeField] private float closeButtonMaxWidth = 260f; // Exposes this private clue UI field in the Unity inspector.

    private readonly Dictionary<string, GameObject> _panels = new(); // Maps clue IDs to their classic clue panels.
    private GameObject _currentPanel; // Tracks the currently displayed classic clue panel.
    private Action _onClosed; // Stores the callback invoked after a clue panel closes.
    private string _clueClosePropertyKey; // Stores the Photon key prefix for this shared close gate.
    private string _clueClosedPropertyKey; // Stores the Photon key that marks the clue as closed for everyone.
    private string _clueCloseRoundPropertyKey; // Stores the Photon key for the current close-vote round.
    private string _clueRemainingPropertyKey; // Stores the Photon key for remaining players who must close.
    private TMP_Text _closeButtonLabel; // Caches the close button text label.
    private string _defaultCloseButtonText = "Close"; // Stores the close button text used when no waiting message is needed.
    private RectTransform _closeButtonRect; // Caches the close button RectTransform for resizing.
    private Vector2 _closeButtonMinSize; // Stores the original close button size.
    private bool _pendingCloseClick; // Tracks whether this player clicked close before the close round was ready.
    private bool _allowSharedCloseHide; // Allows the shared close flow to hide the clue panel.
    private int _broadcastRemainingPlayers = -1; // Caches the latest remaining-player count received by event.
    private const byte ClueCloseRemainingEventCode = 42; // Defines the Photon event code for remaining-player broadcasts.

    [Header("Image Layout")] // Groups related clue UI fields in the Unity inspector.
    [SerializeField] private Vector2 portraitSize = new Vector2(600, 900); // Exposes this private clue UI field in the Unity inspector.
    [SerializeField] private Vector2 landscapeSize = new Vector2(900, 600); // Exposes this private clue UI field in the Unity inspector.
    // unread counter
    private int _unreadCount = 0; // Stores the number of unread clue notifications.

    private void Awake() // Initializes singleton state, panel mappings, buttons, and initial UI visibility.
    { // Starts this block.
        if (Instance == null) Instance = this; // Checks whether this clue UI condition requires action.
        else { Destroy(gameObject); return; } // Handles this part of the clue UI flow.

        // Map IDs to panels, hide them all
        _panels.Clear(); // Clears the clue panel lookup before rebuilding it.
        foreach (var c in clues) // Loops through clue UI or Photon player data.
        { // Starts this block.
            if (c == null || string.IsNullOrWhiteSpace(c.id) || c.panelRoot == null) continue; // Checks whether this clue UI condition requires action.

            if (_panels.ContainsKey(c.id)) // Checks whether this clue UI condition requires action.
                Debug.LogWarning($"Duplicate clue id '{c.id}' in ClueUI."); // Writes a clue UI warning to the Unity console.
            else // Runs when the previous condition is false.
                _panels.Add(c.id, c.panelRoot); // Registers this clue ID and panel in the lookup.

            c.panelRoot.SetActive(false); // Shows or hides a clue UI GameObject.
        } // Ends this block.

        if (mainRoot != null) mainRoot.SetActive(false); // Checks whether this clue UI condition requires action.

        if (closeButton != null) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            _closeButtonLabel = closeButton.GetComponentInChildren<TMP_Text>(true); // Handles this part of the clue UI flow.
            _closeButtonRect = closeButton.GetComponent<RectTransform>(); // Handles this part of the clue UI flow.
            if (_closeButtonRect != null) // Checks whether this clue UI condition requires action.
                _closeButtonMinSize = _closeButtonRect.rect.size; // Handles this part of the clue UI flow.

            if (_closeButtonLabel != null) // Checks whether this clue UI condition requires action.
                _defaultCloseButtonText = _closeButtonLabel.text; // Handles this part of the clue UI flow.

            closeButton.onClick = new Button.ButtonClickedEvent(); // Configures the main clue close button.
            closeButton.onClick.AddListener(() => Hide(true)); // Configures the main clue close button.
        } // Ends this block.

        if (detailPanelRoot != null) // Checks whether this clue UI condition requires action.
            detailPanelRoot.SetActive(false); // Shows or hides a clue UI GameObject.

        if (detailCloseButton != null) // Checks whether this clue UI condition requires action.
            detailCloseButton.onClick.AddListener(HideImage); // Configures the image detail close button.

        UpdateUnreadUI(); // ensure correct initial state // Refreshes unread badge display.
    } // Ends this block.

    /// <summary>Show the panel for a clue by its id (classic per-clue panel flow).</summary>
    public bool ShowById(string id, Action onClosed = null) // Shows a classic clue panel by clue ID.
    { // Starts this block.
        if (string.IsNullOrWhiteSpace(id) || !_panels.TryGetValue(id, out var panel)) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            Debug.LogWarning($"ClueUI: No panel found for id '{id}'."); // Writes a clue UI warning to the Unity console.
            return false; // Reports that the requested clue UI action failed.
        } // Ends this block.

        // Hide any currently visible panel first
        if (_currentPanel != null) // Checks whether this clue UI condition requires action.
            _currentPanel.SetActive(false); // Shows or hides a clue UI GameObject.

        _currentPanel = panel; // Updates the currently active clue panel reference.
        _onClosed = onClosed; // Updates the close callback reference.
        _pendingCloseClick = false; // Updates this client pending close-click state.
        _allowSharedCloseHide = false; // Updates whether shared close may hide the panel.
        _broadcastRemainingPlayers = -1; // Updates the cached broadcast remaining-player count.
        EnsureCloseGateKeys(id); // Handles this part of the clue UI flow.
        HideActiveSlideshows(); // Gives the regular clue exclusive ownership of the shared close button.

        if (mainRoot != null) mainRoot.SetActive(true); // Checks whether this clue UI condition requires action.
        _currentPanel.SetActive(true); // Shows or hides a clue UI GameObject.
        ShowStandardCloseButton(); // Ensures standard clue panels always expose their close action.
        RefreshCloseButtonState(); // Refreshes close button text and interactability.
        BeginCloseGateRoundIfNeeded(); // Handles this part of the clue UI flow.

        Cursor.lockState = CursorLockMode.None; // Unlocks or restores cursor lock state for UI interaction.
        Cursor.visible = true; // Shows the cursor for clue UI interaction.
        Time.timeScale = 0f; // Pauses or resumes gameplay time while clue UI is open.

        // Reset unread when opened
        ResetUnreadCount(); // Clears unread clue notifications.

        return true; // Reports that the requested clue UI action succeeded.
    } // Ends this block.

    private void ShowStandardCloseButton() // Makes the shared close button available for regular clue content.
    { // Starts this block.
        if (closeButton == null || SlideshowUI.ShouldSuppressLegacyCloseHandlers()) // Checks whether a slideshow owns the shared close button.
            return; // Leaves the current method early.

        closeButton.gameObject.SetActive(true); // Shows the shared close button for a standard clue.
        RestoreDefaultCloseButtonText(); // Restores normal close button text.
        closeButton.interactable = true; // Enables the standard clue close action.
    } // Ends this block.

    private void HideActiveSlideshows() // Stops an earlier slideshow from controlling the shared close button.
    { // Starts this block.
        if (mainRoot == null) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        foreach (SlideshowUI slideshow in mainRoot.GetComponentsInChildren<SlideshowUI>(true)) // Loops through slideshow clue panels.
        { // Starts this block.
            if (slideshow != null && slideshow.gameObject.activeSelf) // Checks whether a slideshow panel is still active.
                slideshow.gameObject.SetActive(false); // Releases the shared close button for the regular clue.
        } // Ends this block.
    } // Ends this block.

    /// <summary>Hide the current clue panel + root. If userAction=true, invoke onClosed callback.</summary>
    public void Hide(bool userAction) // Requests closing the current classic clue UI.
    { // Starts this block.
        if (SlideshowUI.ShouldSuppressLegacyCloseHandlers()) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (ShouldUseSharedCloseGate()) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            HandleSharedCloseClicked(); // Handles this part of the clue UI flow.
            return; // Leaves the current method early.
        } // Ends this block.

        HideNow(userAction); // Immediately hides the clue UI.
    } // Ends this block.

    private void HideNow(bool userAction) // Immediately hides the current clue UI and restores gameplay state.
    { // Starts this block.
        _allowSharedCloseHide = true; // Updates whether shared close may hide the panel.
        _pendingCloseClick = false; // Updates this client pending close-click state.
        RestoreDefaultCloseButtonText(); // Restores normal close button text.
        if (closeButton != null) // Checks whether this clue UI condition requires action.
            closeButton.interactable = true; // Configures the main clue close button.

        if (_currentPanel != null) // Checks whether this clue UI condition requires action.
            _currentPanel.SetActive(false); // Shows or hides a clue UI GameObject.

        if (mainRoot != null) // Checks whether this clue UI condition requires action.
            mainRoot.SetActive(false); // Shows or hides a clue UI GameObject.

        Time.timeScale = 1f; // Pauses or resumes gameplay time while clue UI is open.

        if (userAction && _onClosed != null) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            var cb = _onClosed; // Handles this part of the clue UI flow.
            _onClosed = null; // Updates the close callback reference.
            cb.Invoke(); // Handles this part of the clue UI flow.
        } // Ends this block.

        _currentPanel = null; // Updates the currently active clue panel reference.

        // Reset unread
        ResetUnreadCount(); // Clears unread clue notifications.
        // Force-close any optional badge panel
        if (badgePanelRoot != null && badgePanelRoot.activeSelf) // Checks whether this clue UI condition requires action.
            badgePanelRoot.SetActive(false); // Shows or hides a clue UI GameObject.
    } // Ends this block.

    private void LateUpdate() // Keeps shared-close clue panels visible until the gate completes.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate() || _allowSharedCloseHide) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (mainRoot != null && !mainRoot.activeSelf) // Checks whether this clue UI condition requires action.
            mainRoot.SetActive(true); // Shows or hides a clue UI GameObject.

        if (_currentPanel != null && !_currentPanel.activeSelf) // Checks whether this clue UI condition requires action.
            _currentPanel.SetActive(true); // Shows or hides a clue UI GameObject.

        RefreshCloseButtonState(); // Refreshes close button text and interactability.
    } // Ends this block.

    private void HandleSharedCloseClicked() // Handles the local player pressing close during shared close mode.
    { // Starts this block.
        int currentRound = GetCurrentCloseRound(); // Stores a clue UI count, round, or actor value.
        if (currentRound <= 0) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            _pendingCloseClick = true; // Updates this client pending close-click state.
            RefreshCloseButtonState(); // Refreshes close button text and interactability.
            return; // Leaves the current method early.
        } // Ends this block.

        _pendingCloseClick = false; // Updates this client pending close-click state.

        if (!HasLocalPlayerClickedClose()) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            SetLocalPlayerCloseClicked(true); // Writes local close-vote state.
            _pendingCloseClick = true; // Updates this client pending close-click state.
        } // Ends this block.

        RefreshSharedCloseGateState(); // Refreshes shared close-gate UI and completion.
    } // Ends this block.

    private bool ShouldUseSharedCloseGate() // Checks whether multiplayer shared-close gating should be active.
    { // Starts this block.
        return requireAllPlayersToClickClose && PhotonNetwork.IsConnected && PhotonNetwork.InRoom && _currentPanel != null; // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private void EnsureCloseGateKeys(string clueId) // Builds sanitized Photon property keys for this clue close gate.
    { // Starts this block.
        string rawId = string.IsNullOrWhiteSpace(clueCloseId) ? "SharedClue" : clueCloseId.Trim(); // Stores a clue UI text or property-key value.
        if (!string.IsNullOrWhiteSpace(clueId)) // Checks whether this clue UI condition requires action.
            rawId += "_" + clueId.Trim(); // Handles this part of the clue UI flow.

        char[] chars = rawId.ToCharArray(); // Handles this part of the clue UI flow.
        for (int i = 0; i < chars.Length; i++) // Loops through indexed clue UI or player data.
        { // Starts this block.
            if (!char.IsLetterOrDigit(chars[i])) // Checks whether this clue UI condition requires action.
                chars[i] = '_'; // Handles this part of the clue UI flow.
        } // Ends this block.

        _clueClosePropertyKey = "ClueClose_" + new string(chars); // Stores the base Photon property key for this clue close gate.
        _clueClosedPropertyKey = _clueClosePropertyKey + "_Done"; // Stores the Photon property key for the closed flag.
        _clueCloseRoundPropertyKey = _clueClosePropertyKey + "_Round"; // Stores the Photon property key for the close round.
        _clueRemainingPropertyKey = _clueClosePropertyKey + "_Remaining"; // Stores the Photon property key for remaining players.
    } // Ends this block.

    private void BeginCloseGateRoundIfNeeded() // Starts a new shared close-vote round on the master client.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate() || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        int nextRound = GetCurrentCloseRound() + 1; // Stores a clue UI count, round, or actor value.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Writes shared clue close state to Photon room properties.
        { // Starts this block.
            [_clueClosedPropertyKey] = false, // Handles this part of the clue UI flow.
            [_clueCloseRoundPropertyKey] = nextRound, // Handles this part of the clue UI flow.
            [_clueRemainingPropertyKey] = GetExpectedClosePlayerCount() // Handles this part of the clue UI flow.
        }); // Handles this part of the clue UI flow.
    } // Ends this block.

    private void RefreshCloseButtonState() // Updates close button text and interactability for shared close state.
    { // Starts this block.
        if (closeButton == null) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (!SlideshowUI.ShouldSuppressLegacyCloseHandlers()) // Checks whether a slideshow owns the shared close button.
            closeButton.gameObject.SetActive(true); // Keeps the shared close button visible while its status changes.

        if (!ShouldUseSharedCloseGate()) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            RestoreDefaultCloseButtonText(); // Restores normal close button text.
            closeButton.interactable = true; // Configures the main clue close button.
            return; // Leaves the current method early.
        } // Ends this block.

        int currentRound = GetCurrentCloseRound(); // Stores a clue UI count, round, or actor value.
        if (currentRound <= 0) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            if (_pendingCloseClick) // Checks whether this clue UI condition requires action.
                SetCloseButtonText("Waiting for remaining players to close"); // Sets close button status text.
            else // Runs when the previous condition is false.
                RestoreDefaultCloseButtonText(); // Restores normal close button text.

            closeButton.interactable = !_pendingCloseClick; // Configures the main clue close button.
            return; // Leaves the current method early.
        } // Ends this block.

        int remainingPlayers = GetDisplayedRemainingPlayersCount(); // Stores a clue UI count, round, or actor value.
        bool localClicked = HasLocalPlayerClickedClose() || _pendingCloseClick; // Stores a clue UI boolean state value.

        if (remainingPlayers <= 0) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            SetCloseButtonText("Closing..."); // Sets close button status text.
            closeButton.interactable = false; // Configures the main clue close button.
            return; // Leaves the current method early.
        } // Ends this block.

        if (localClicked) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            SetCloseButtonText($"Waiting for ({remainingPlayers} players to close)"); // Sets close button status text.
            closeButton.interactable = false; // Configures the main clue close button.
            return; // Leaves the current method early.
        } // Ends this block.

        RestoreDefaultCloseButtonText(); // Restores normal close button text.
        closeButton.interactable = true; // Configures the main clue close button.
    } // Ends this block.

    private void SetCloseButtonText(string value) // Applies text to the close button label.
    { // Starts this block.
        if (_closeButtonLabel != null) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            _closeButtonLabel.text = value; // Updates the close button label.
            ResizeCloseButtonToText(); // Fits the close button to its current label.
        } // Ends this block.
    } // Ends this block.

    private void RestoreDefaultCloseButtonText() // Restores the close button label to its default text.
    { // Starts this block.
        SetCloseButtonText(_defaultCloseButtonText); // Sets close button status text.
    } // Ends this block.

    private void ResizeCloseButtonToText() // Resizes the close button so waiting text fits.
    { // Starts this block.
        if (_closeButtonRect == null || _closeButtonLabel == null) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        _closeButtonLabel.ForceMeshUpdate(); // Updates the close button label.

        float targetWidth = Mathf.Max(_closeButtonMinSize.x, closeButtonMaxWidth); // Stores an image layout measurement.
        float wrappedTextWidth = Mathf.Max(1f, targetWidth - closeButtonTextPadding.x); // Stores an image layout measurement.
        Vector2 preferredValues = _closeButtonLabel.GetPreferredValues(_closeButtonLabel.text, wrappedTextWidth, 0f); // Stores a UI size measurement.

        float targetHeight = Mathf.Max( // Stores an image layout measurement.
            _closeButtonMinSize.y, // Handles this part of the clue UI flow.
            preferredValues.y + closeButtonTextPadding.y); // Handles this part of the clue UI flow.

        _closeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth); // Updates the close button size.
        _closeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight); // Updates the close button size.
    } // Ends this block.

    private void TryCompleteSharedCloseGate() // Checks whether all expected players have clicked close.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate()) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (GetRemainingPlayersCount() == 0 && HasAnyPlayerClickedClose()) // Checks whether this clue UI condition requires action.
            MarkClueClosedForEveryone(); // Handles this part of the clue UI flow.
    } // Ends this block.

    private void MarkClueClosedForEveryone() // Marks the clue panel closed through shared Photon room state.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.CurrentRoom == null) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            HideNow(true); // Immediately hides the clue UI.
            return; // Leaves the current method early.
        } // Ends this block.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Writes shared clue close state to Photon room properties.
        { // Starts this block.
            [_clueClosedPropertyKey] = true // Handles this part of the clue UI flow.
        }); // Handles this part of the clue UI flow.
    } // Ends this block.

    private void SetLocalPlayerCloseClicked(bool clicked) // Writes this player close vote into Photon room properties.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.LocalPlayer == null || PhotonNetwork.CurrentRoom == null) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        int currentRound = GetCurrentCloseRound(); // Stores a clue UI count, round, or actor value.
        int clickedRound = clicked && currentRound > 0 ? currentRound : -1; // Stores a clue UI count, round, or actor value.
        int remainingPlayers = CalculateRemainingPlayersCountWithLocalVote(clickedRound); // Stores a clue UI count, round, or actor value.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Writes shared clue close state to Photon room properties.
        { // Starts this block.
            [BuildPlayerClosePropertyKey(PhotonNetwork.LocalPlayer.ActorNumber)] = clickedRound, // Handles this part of the clue UI flow.
            [_clueRemainingPropertyKey] = remainingPlayers // Handles this part of the clue UI flow.
        }); // Handles this part of the clue UI flow.

        BroadcastRemainingPlayers(remainingPlayers); // Broadcasts remaining close voters.
    } // Ends this block.

    private bool HasLocalPlayerClickedClose() // Checks whether the local player has voted to close.
    { // Starts this block.
        return HasPlayerClickedClose(PhotonNetwork.LocalPlayer); // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private bool HasPlayerClickedClose(Player player) // Checks whether a specific player has voted to close this round.
    { // Starts this block.
        if (player == null || string.IsNullOrEmpty(_clueClosePropertyKey)) // Checks whether this clue UI condition requires action.
            return false; // Reports that the requested clue UI action failed.

        int currentRound = GetCurrentCloseRound(); // Stores a clue UI count, round, or actor value.
        if (currentRound <= 0) // Checks whether this clue UI condition requires action.
            return false; // Reports that the requested clue UI action failed.

        if (PhotonNetwork.CurrentRoom != null && // Checks whether this clue UI condition requires action.
            PhotonNetwork.CurrentRoom.CustomProperties != null && // Uses an external UI, math, or Photon API for clue behavior.
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BuildPlayerClosePropertyKey(player.ActorNumber), out object value) && // Uses an external UI, math, or Photon API for clue behavior.
            value is int clickedRound) // Handles this part of the clue UI flow.
        { // Starts this block.
            return clickedRound == currentRound; // Returns the computed clue UI value to the caller.
        } // Ends this block.

        return false; // Reports that the requested clue UI action failed.
    } // Ends this block.

    private string BuildPlayerClosePropertyKey(int actorNumber) // Builds the per-player Photon close-vote key.
    { // Starts this block.
        return $"{_clueClosePropertyKey}_{actorNumber}"; // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private int GetRemainingPlayersCount() // Counts players still expected to click close.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate()) // Checks whether this clue UI condition requires action.
            return 0; // Returns the computed clue UI value to the caller.

        int remaining = 0; // Stores a clue UI count, round, or actor value.
        Player[] players = PhotonNetwork.PlayerList; // Reads the current Photon player list.
        for (int i = 0; i < players.Length; i++) // Loops through indexed clue UI or player data.
        { // Starts this block.
            if (!ShouldExpectPlayerClose(players[i])) // Checks whether this clue UI condition requires action.
                continue; // Handles this part of the clue UI flow.

            if (!HasPlayerClickedClose(players[i])) // Checks whether this clue UI condition requires action.
                remaining++; // Handles this part of the clue UI flow.
        } // Ends this block.

        return remaining; // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private int GetExpectedClosePlayerCount() // Counts active players expected to participate in close voting.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate()) // Checks whether this clue UI condition requires action.
            return 0; // Returns the computed clue UI value to the caller.

        int count = 0; // Stores a clue UI count, round, or actor value.
        Player[] players = PhotonNetwork.PlayerList; // Reads the current Photon player list.
        for (int i = 0; i < players.Length; i++) // Loops through indexed clue UI or player data.
        { // Starts this block.
            if (ShouldExpectPlayerClose(players[i])) // Checks whether this clue UI condition requires action.
                count++; // Handles this part of the clue UI flow.
        } // Ends this block.

        return count; // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private bool ShouldExpectPlayerClose(Player player) // Checks whether a player should be included in close voting.
    { // Starts this block.
        return player != null && !player.IsInactive; // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private int GetDisplayedRemainingPlayersCount() // Chooses the remaining-player count displayed on the close button.
    { // Starts this block.
        if (_broadcastRemainingPlayers >= 0) // Checks whether this clue UI condition requires action.
            return Mathf.Min(_broadcastRemainingPlayers, GetRemainingPlayersCount()); // Returns the computed clue UI value to the caller.

        if (PhotonNetwork.CurrentRoom != null && // Checks whether this clue UI condition requires action.
            PhotonNetwork.CurrentRoom.CustomProperties != null && // Uses an external UI, math, or Photon API for clue behavior.
            !string.IsNullOrEmpty(_clueRemainingPropertyKey) && // Handles this part of the clue UI flow.
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(_clueRemainingPropertyKey, out object value) && // Uses an external UI, math, or Photon API for clue behavior.
            value is int remaining) // Handles this part of the clue UI flow.
        { // Starts this block.
            return Mathf.Min(remaining, GetRemainingPlayersCount()); // Returns the computed clue UI value to the caller.
        } // Ends this block.

        return GetRemainingPlayersCount(); // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private int CalculateRemainingPlayersCountWithLocalVote(int localClickedRound) // Predicts remaining players after applying the local close vote.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate()) // Checks whether this clue UI condition requires action.
            return 0; // Returns the computed clue UI value to the caller.

        int currentRound = GetCurrentCloseRound(); // Stores a clue UI count, round, or actor value.
        int remaining = 0; // Stores a clue UI count, round, or actor value.
        Player[] players = PhotonNetwork.PlayerList; // Reads the current Photon player list.
        for (int i = 0; i < players.Length; i++) // Loops through indexed clue UI or player data.
        { // Starts this block.
            Player player = players[i]; // Stores the player currently being evaluated.
            if (!ShouldExpectPlayerClose(player)) // Checks whether this clue UI condition requires action.
                continue; // Handles this part of the clue UI flow.

            bool clicked = player == PhotonNetwork.LocalPlayer // Stores a clue UI boolean state value.
                ? localClickedRound == currentRound // Handles this part of the clue UI flow.
                : HasPlayerClickedClose(player); // Handles this part of the clue UI flow.

            if (!clicked) // Checks whether this clue UI condition requires action.
                remaining++; // Handles this part of the clue UI flow.
        } // Ends this block.

        return remaining; // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private void BroadcastRemainingPlayers(int remainingPlayers) // Broadcasts the remaining-player count with a Photon event.
    { // Starts this block.
        if (!PhotonNetwork.InRoom || string.IsNullOrEmpty(_clueClosePropertyKey)) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        object[] content = // Creates the payload for the Photon remaining-player event.
        { // Starts this block.
            _clueClosePropertyKey, // Handles this part of the clue UI flow.
            GetCurrentCloseRound(), // Handles this part of the clue UI flow.
            remainingPlayers // Handles this part of the clue UI flow.
        }; // Ends this initializer block.

        PhotonNetwork.RaiseEvent( // Sends a custom Photon event to other clients.
            ClueCloseRemainingEventCode, // Handles this part of the clue UI flow.
            content, // Handles this part of the clue UI flow.
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, // Configures which clients receive the Photon event.
            SendOptions.SendReliable); // Sends the Photon event reliably.
    } // Ends this block.

    public void OnEvent(EventData photonEvent) // Handles Photon events received by the clue UI.
    { // Starts this block.
        if (photonEvent.Code != ClueCloseRemainingEventCode) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (!ShouldUseSharedCloseGate() || !(photonEvent.CustomData is object[] content) || content.Length < 3) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        string closeKey = content[0] as string; // Stores a clue UI text or property-key value.
        if (closeKey != _clueClosePropertyKey) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (!(content[1] is int round)) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (!(content[2] is int remainingPlayers)) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        int currentRound = GetCurrentCloseRound(); // Stores a clue UI count, round, or actor value.
        if (currentRound > 0 && round != currentRound) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        _broadcastRemainingPlayers = remainingPlayers; // Updates the cached broadcast remaining-player count.

        if (HasLocalPlayerClickedClose()) // Checks whether this clue UI condition requires action.
            _pendingCloseClick = false; // Updates this client pending close-click state.

        RefreshSharedCloseGateState(); // Refreshes shared close-gate UI and completion.
    } // Ends this block.

    private int GetCurrentCloseRound() // Reads the current shared close round from room properties.
    { // Starts this block.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || string.IsNullOrEmpty(_clueCloseRoundPropertyKey)) // Checks whether this clue UI condition requires action.
            return 0; // Returns the computed clue UI value to the caller.

        if (PhotonNetwork.CurrentRoom.CustomProperties != null && // Checks whether this clue UI condition requires action.
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(_clueCloseRoundPropertyKey, out object value) && // Uses an external UI, math, or Photon API for clue behavior.
            value is int round) // Handles this part of the clue UI flow.
        { // Starts this block.
            return round; // Returns the computed clue UI value to the caller.
        } // Ends this block.

        return 0; // Returns the computed clue UI value to the caller.
    } // Ends this block.

    private bool HasAnyPlayerClickedClose() // Checks whether at least one active player voted to close.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate()) // Checks whether this clue UI condition requires action.
            return false; // Reports that the requested clue UI action failed.

        Player[] players = PhotonNetwork.PlayerList; // Reads the current Photon player list.
        for (int i = 0; i < players.Length; i++) // Loops through indexed clue UI or player data.
        { // Starts this block.
            if (!ShouldExpectPlayerClose(players[i])) // Checks whether this clue UI condition requires action.
                continue; // Handles this part of the clue UI flow.

            if (HasPlayerClickedClose(players[i])) // Checks whether this clue UI condition requires action.
                return true; // Reports that the requested clue UI action succeeded.
        } // Ends this block.

        return false; // Reports that the requested clue UI action failed.
    } // Ends this block.

    private void ResetLocalCloseState() // Clears this client close-vote state for the active gate.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate()) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        SetLocalPlayerCloseClicked(false); // Writes local close-vote state.
    } // Ends this block.

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) // Responds to Photon player property changes for close voting.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate() || changedProps == null || string.IsNullOrEmpty(_clueClosePropertyKey)) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (!changedProps.ContainsKey(_clueClosePropertyKey)) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (targetPlayer == PhotonNetwork.LocalPlayer && HasLocalPlayerClickedClose()) // Checks whether this clue UI condition requires action.
            _pendingCloseClick = false; // Updates this client pending close-click state.

        RefreshSharedCloseGateState(); // Refreshes shared close-gate UI and completion.
    } // Ends this block.

    public override void OnPlayerEnteredRoom(Player newPlayer) // Refreshes close-vote state when a player joins.
    { // Starts this block.
        PublishRemainingPlayersIfMaster(); // Publishes remaining close voters if this client is master.
        RefreshSharedCloseGateState(); // Refreshes shared close-gate UI and completion.
    } // Ends this block.

    public override void OnPlayerLeftRoom(Player otherPlayer) // Refreshes close-vote state when a player leaves.
    { // Starts this block.
        PublishRemainingPlayersIfMaster(); // Publishes remaining close voters if this client is master.
        RefreshSharedCloseGateState(); // Refreshes shared close-gate UI and completion.
    } // Ends this block.

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) // Responds to Photon room property changes for clue close state.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate() || propertiesThatChanged == null) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        bool closeVoteChanged = !string.IsNullOrEmpty(_clueRemainingPropertyKey) && // Stores a clue UI boolean state value.
                                propertiesThatChanged.ContainsKey(_clueRemainingPropertyKey); // Handles this part of the clue UI flow.
        foreach (object key in propertiesThatChanged.Keys) // Loops through clue UI or Photon player data.
        { // Starts this block.
            if (key is string keyString && keyString.StartsWith(_clueClosePropertyKey + "_")) // Checks whether this clue UI condition requires action.
            { // Starts this block.
                closeVoteChanged = true; // Handles this part of the clue UI flow.
                break; // Handles this part of the clue UI flow.
            } // Ends this block.
        } // Ends this block.

        if (closeVoteChanged) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            if (!string.IsNullOrEmpty(_clueRemainingPropertyKey) && // Checks whether this clue UI condition requires action.
                PhotonNetwork.CurrentRoom != null && // Uses an external UI, math, or Photon API for clue behavior.
                PhotonNetwork.CurrentRoom.CustomProperties != null && // Uses an external UI, math, or Photon API for clue behavior.
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(_clueRemainingPropertyKey, out object remainingValue) && // Uses an external UI, math, or Photon API for clue behavior.
                remainingValue is int remainingPlayers) // Handles this part of the clue UI flow.
            { // Starts this block.
                _broadcastRemainingPlayers = remainingPlayers; // Updates the cached broadcast remaining-player count.
            } // Ends this block.

            if (HasLocalPlayerClickedClose()) // Checks whether this clue UI condition requires action.
                _pendingCloseClick = false; // Updates this client pending close-click state.

            RefreshSharedCloseGateState(); // Refreshes shared close-gate UI and completion.
        } // Ends this block.

        if (!string.IsNullOrEmpty(_clueCloseRoundPropertyKey) && propertiesThatChanged.ContainsKey(_clueCloseRoundPropertyKey)) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            ResetLocalCloseState(); // Handles this part of the clue UI flow.

            if (_pendingCloseClick) // Checks whether this clue UI condition requires action.
            { // Starts this block.
                _pendingCloseClick = false; // Updates this client pending close-click state.
                SetLocalPlayerCloseClicked(true); // Writes local close-vote state.
                _pendingCloseClick = true; // Updates this client pending close-click state.
            } // Ends this block.

            RefreshSharedCloseGateState(); // Refreshes shared close-gate UI and completion.
        } // Ends this block.

        if (!string.IsNullOrEmpty(_clueClosedPropertyKey) && // Checks whether this clue UI condition requires action.
            propertiesThatChanged.TryGetValue(_clueClosedPropertyKey, out object value) && // Handles this part of the clue UI flow.
            value is bool isClosed && isClosed) // Handles this part of the clue UI flow.
        { // Starts this block.
            HideNow(true); // Immediately hides the clue UI.
        } // Ends this block.
    } // Ends this block.

    private void RefreshSharedCloseGateState() // Refreshes the shared close button and completion state.
    { // Starts this block.
        RefreshCloseButtonState(); // Refreshes close button text and interactability.
        TryCompleteSharedCloseGate(); // Checks whether the shared close gate can complete.
    } // Ends this block.

    private void PublishRemainingPlayersIfMaster() // Publishes remaining close voters from the master client.
    { // Starts this block.
        if (!ShouldUseSharedCloseGate() || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        int remainingPlayers = GetRemainingPlayersCount(); // Stores a clue UI count, round, or actor value.
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Writes shared clue close state to Photon room properties.
        { // Starts this block.
            [_clueRemainingPropertyKey] = remainingPlayers // Handles this part of the clue UI flow.
        }); // Handles this part of the clue UI flow.

        BroadcastRemainingPlayers(remainingPlayers); // Broadcasts remaining close voters.
    } // Ends this block.

    public bool HasClueId(string id) => // Reports whether this UI knows about a clue ID.
        !string.IsNullOrWhiteSpace(id) && _panels.ContainsKey(id); // Handles this part of the clue UI flow.

    // ---------- Image Detail View API ----------

    /// <summary>Show a full-size image in the detail panel (called by ClueSystem on tile click).</summary>
    public void ShowImage(Sprite sprite) // Shows a full-size clue image in the detail panel.
    { // Starts this block.
        if (sprite == null || detailImage == null || detailPanelRoot == null) // Checks whether this clue UI condition requires action.
        { // Starts this block.
            Debug.LogWarning("[ClueUI] Missing detail image/panel or sprite."); // Writes a clue UI warning to the Unity console.
            return; // Leaves the current method early.
        } // Ends this block.
        detailImage.gameObject.SetActive(true); // Shows or hides a clue UI GameObject.
        detailImage.sprite = sprite; // Configures the full-size clue image display.
        RectTransform rt = detailImage.rectTransform; // Stores the RectTransform used for image or button layout.

        float spriteAspect = sprite.rect.width / sprite.rect.height; // Stores an image layout measurement.

        float targetHeight = rt.sizeDelta.y; // Stores an image layout measurement.
        float targetWidth = targetHeight * spriteAspect; // Stores an image layout measurement.

        bool isPortrait = false; // Stores a clue UI boolean state value.
        // Auto-detect from sprite pixel rect
        isPortrait = sprite.rect.height > sprite.rect.width; // Handles this part of the clue UI flow.

        rt.sizeDelta = isPortrait ? portraitSize : landscapeSize; // Handles this part of the clue UI flow.



        detailPanelRoot.SetActive(true); // Shows or hides a clue UI GameObject.
        ShowStandardCloseButton(); // Ensures image-detail clues have a visible close action.

        Cursor.lockState = CursorLockMode.None; // Unlocks or restores cursor lock state for UI interaction.
        Cursor.visible = true; // Shows the cursor for clue UI interaction.
        Time.timeScale = 0f; // Pauses or resumes gameplay time while clue UI is open.

        ResetUnreadCount(); // Clears unread clue notifications.
    } // Ends this block.

    public void HideImage() // Hides the image detail panel and restores gameplay time.
    { // Starts this block.
        if (SlideshowUI.ShouldSuppressLegacyCloseHandlers()) // Checks whether this clue UI condition requires action.
            return; // Leaves the current method early.

        if (detailPanelRoot != null) // Checks whether this clue UI condition requires action.
            detailPanelRoot.SetActive(false); // Shows or hides a clue UI GameObject.
        foreach (var go in comics) // Loops through clue UI or Photon player data.
        { // Starts this block.
            go.SetActive(false); // Shows or hides a clue UI GameObject.
        } // Ends this block.
        detailImage.gameObject.SetActive(false); // Shows or hides a clue UI GameObject.
        Time.timeScale = 1f; // Pauses or resumes gameplay time while clue UI is open.
    } // Ends this block.

    // ---------- Unread badge API (called by ClueSystem) ----------

    public void IncrementUnread() // Increments unread clue count and refreshes the badge.
    { // Starts this block.
        _unreadCount++; // Updates the unread clue counter.
        UpdateUnreadUI(); // Refreshes unread badge display.
    } // Ends this block.

    private void ResetUnreadCount() // Clears unread clue count and refreshes the badge.
    { // Starts this block.
        _unreadCount = 0; // Updates the unread clue counter.
        UpdateUnreadUI(); // Refreshes unread badge display.
    } // Ends this block.

    private void UpdateUnreadUI() // Shows or hides the unread badge and updates its label.
    { // Starts this block.
        bool show = _unreadCount > 0; // Stores a clue UI boolean state value.

        if (unreadBadgeRoot != null) // Checks whether this clue UI condition requires action.
            unreadBadgeRoot.SetActive(show); // Shows or hides a clue UI GameObject.

        if (unreadText != null) // Checks whether this clue UI condition requires action.
            unreadText.text = _unreadCount.ToString(); // Handles this part of the clue UI flow.
    } // Ends this block.
} // Ends this block.
