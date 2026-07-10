using UnityEngine; // Imports Unity engine types such as MonoBehaviour, GameObject, Color, and Screen.
using TMPro; // Imports TextMeshPro UI text types.
using Photon.Pun; // Imports Photon PUN networking APIs and callbacks.
using UnityEngine.UI; // Imports Unity UI types such as Image.

public class GameTimerManager : MonoBehaviourPunCallbacks // Declares the networked game countdown timer manager.
{ // Starts the GameTimerManager class body.
    [Header("Timer Settings")] // Groups timer settings in the Unity inspector.
    [SerializeField] private float countdownDuration = 60f; // Exposes the countdown length in seconds.

    [Header("UI")] // Groups timer display fields in the Unity inspector.
    [SerializeField] private TMP_Text timerText; // References the text label that shows the remaining time.

    [Header("Warning UI")] // Groups warning popup fields in the Unity inspector.
    [SerializeField] private GameObject timeWarningPanel; // References the panel used to show timed warning messages.
    [SerializeField] private TMP_Text warningText; // References the text label inside the warning panel.

    [Header("Warnings (add as many as you want)")] // Groups configurable warning entries in the Unity inspector.
    [SerializeField] private TimerWarning[] warnings; // Exposes the list of warning thresholds and display settings.

    private float timerEndTime; // Stores the synchronized Photon time when the countdown should end.
    private bool timerRunning = false; // Tracks whether the timer should currently count down.

    private RectTransform warningRect; // Caches the warning panel RectTransform for slide animation.
    private CanvasGroup warningCanvasGroup; // Caches or creates a CanvasGroup for fading the warning panel.

    // ---------------- STRUCT ----------------
    [System.Serializable] // Allows Unity to display TimerWarning entries in the inspector.
    public struct TimerWarning // Defines one configurable countdown warning.
    { // Starts the TimerWarning struct body.
        [Min(0f)] public float thresholdSeconds; // Sets the remaining-time threshold that triggers this warning.
        [TextArea] public string message; // Stores the message shown when this warning triggers.
        public Color color; // Stores the panel color used for this warning.

        [Header("Animation")] // Groups animation settings for this warning in the inspector.
        [Min(0.1f)] public float slideDuration; // Sets this warning's slide animation duration.

        [HideInInspector] public bool shown; // Tracks at runtime whether this warning has already fired.
    } // Ends the TimerWarning struct body.
    // ----------------------------------------

    private void Awake() // Runs before Start to prepare networking and warning UI references.
    { // Starts the Awake method body.
        SetupPhotonView(); // Ensures this timer has a PhotonView for RPC calls.

        if (timeWarningPanel != null) // Checks whether a warning panel was assigned.
        { // Starts the warning-panel setup block.
            timeWarningPanel.SetActive(false); // Hides the warning panel at startup.
            warningRect = timeWarningPanel.GetComponent<RectTransform>(); // Caches the warning panel transform used by LeanTween.

            warningCanvasGroup = timeWarningPanel.GetComponent<CanvasGroup>(); // Reads the existing CanvasGroup if one is attached.
            if (warningCanvasGroup == null) // Checks whether the warning panel needs a CanvasGroup.
                warningCanvasGroup = timeWarningPanel.AddComponent<CanvasGroup>(); // Adds a CanvasGroup so the warning can fade out.
        } // Ends the warning-panel setup block.
    } // Ends the Awake method body.

    void SetupPhotonView() // Ensures the timer object can send Photon RPCs.
    { // Starts the SetupPhotonView method body.
        PhotonView view = GetComponent<PhotonView>(); // Gets the PhotonView attached to this timer object.
        if (view == null) view = gameObject.AddComponent<PhotonView>(); // Adds a PhotonView if the object does not already have one.

        view.OwnershipTransfer = OwnershipOption.Fixed; // Prevents runtime ownership transfer for this timer view.
        view.Synchronization = ViewSynchronization.UnreliableOnChange; // Sets the PhotonView sync mode for lightweight updates.

        if (PhotonNetwork.IsConnectedAndReady && view.ViewID == 0) // Checks whether this connected scene object still needs a Photon ViewID.
        { // Starts the PhotonView registration block.
            PhotonNetwork.RegisterPhotonView(view); // Registers the PhotonView so RPCs can target it.
            Debug.Log($"Registered scene object with ViewID: {view.ViewID}"); // Logs the assigned Photon ViewID for debugging.
        } // Ends the PhotonView registration block.
    } // Ends the SetupPhotonView method body.

    private void Start() // Runs when Unity starts this timer component.
    { // Starts the Start method body.
        if (PhotonNetwork.IsMasterClient) // Checks whether this client should control the shared countdown.
        { // Starts the master-client timer block.
            StartTimer(); // Starts and syncs the countdown from the master client.
        } // Ends the master-client timer block.
    } // Ends the Start method body.

    private void Update() // Runs every frame to update countdown state and UI.
    { // Starts the Update method body.
        if (!timerRunning) return; // Skips work until the synchronized timer has started.

        float timeLeft = timerEndTime - (float)PhotonNetwork.Time; // Calculates remaining time from Photon network time.

        // Trigger warnings (master decides; everyone sees via RPC)
        if (PhotonNetwork.IsMasterClient) // Checks whether this client should decide when warnings fire.
        { // Starts the warning-check block.
            CheckAndFireWarnings(timeLeft); // Triggers any warning thresholds reached by the remaining time.
        } // Ends the warning-check block.

        if (timeLeft <= 0f) // Checks whether the countdown has expired.
        { // Starts the timer-expired block.
            timeLeft = 0f; // Clamps the displayed time to zero.
            timerRunning = false; // Stops the local countdown loop.
            Debug.Log("Timer ended!"); // Logs that the timer reached zero.
            PuzzleStateManager.Instance?.OnTimerExpired(); // Notifies the puzzle state manager that time has expired if it exists.
        } // Ends the timer-expired block.

        UpdateTimerUI(timeLeft); // Refreshes the visible countdown text.
    } // Ends the Update method body.

    private void CheckAndFireWarnings(float timeLeft) // Checks each configured warning against the remaining time.
    { // Starts the CheckAndFireWarnings method body.
        if (warnings == null || warnings.Length == 0) return; // Skips warning logic when no warnings are configured.

        for (int i = 0; i < warnings.Length; i++) // Loops through every configured warning entry.
        { // Starts the warning loop body.
            // Because TimerWarning is a struct (value type), copy -> edit -> write back
            TimerWarning w = warnings[i]; // Copies the warning struct so it can be edited safely.

            if (!w.shown && timeLeft <= w.thresholdSeconds && timeLeft > 0f) // Checks whether this warning should fire now and has not fired before.
            { // Starts the fire-warning block.
                w.shown = true; // Marks the copied warning as already shown.
                warnings[i] = w; // Writes the updated warning state back into the array.

                photonView.RPC( // Sends the warning display request to every client.
                    nameof(ShowTimeWarning), // Names the RPC method that should run on clients.
                    RpcTarget.All, // Targets all clients, including the sender.
                    w.message, // Sends the warning message text.
                    w.color.r, w.color.g, w.color.b, w.color.a, // Sends the warning color components.
                    w.slideDuration // Sends the warning slide duration.
                ); // Ends the warning RPC call.
            } // Ends the fire-warning block.
        } // Ends the warning loop body.
    } // Ends the CheckAndFireWarnings method body.

    void UpdateTimerUI(float time) // Updates the countdown display text.
    { // Starts the UpdateTimerUI method body.
        int minutes = Mathf.FloorToInt(time / 60f); // Converts the remaining seconds into whole minutes.
        int seconds = Mathf.FloorToInt(time % 60f); // Converts the remaining seconds into the seconds part.
        timerText.text = $"{minutes:00}:{seconds:00}"; // Displays the time in MM:SS format.
    } // Ends the UpdateTimerUI method body.

    public void StartTimer() // Starts the network-synchronized countdown.
    { // Starts the StartTimer method body.
        timerEndTime = (float)PhotonNetwork.Time + countdownDuration; // Calculates the absolute Photon time when the timer should end.

        ResetWarningsLocal(); // Clears warning shown-state before the timer begins.

        photonView.RPC(nameof(SyncTimer), RpcTarget.AllBuffered, timerEndTime); // Sends the timer end time to all current and future clients.
    } // Ends the StartTimer method body.

    private void ResetWarningsLocal() // Resets all warning entries so they can fire again.
    { // Starts the ResetWarningsLocal method body.
        if (warnings == null) return; // Skips reset when no warnings are configured.

        for (int i = 0; i < warnings.Length; i++) // Loops through each configured warning.
        { // Starts the warning reset loop body.
            TimerWarning w = warnings[i]; // Copies the warning struct for editing.
            w.shown = false; // Marks the warning as not yet shown.
            warnings[i] = w; // Writes the reset warning back into the array.
        } // Ends the warning reset loop body.
    } // Ends the ResetWarningsLocal method body.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    void SyncTimer(float endTime) // Receives and applies the synchronized timer end time.
    { // Starts the SyncTimer RPC body.
        timerEndTime = endTime; // Stores the shared Photon end time locally.
        timerRunning = true; // Starts the local countdown loop.

        ResetWarningsLocal(); // Clears local warning state for the synced timer.
    } // Ends the SyncTimer RPC body.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    void ShowTimeWarning(string message, float colorR, float colorG, float colorB, float colorA, float slideDuration) // Shows a synced timer warning popup.
    { // Starts the ShowTimeWarning RPC body.
        Debug.Log("showing warning message"); // Logs that a warning popup is being shown.
        if (timeWarningPanel == null || warningRect == null || warningCanvasGroup == null) // Checks whether required warning UI references are missing.
            return; // Leaves because the warning UI cannot be shown safely.

        timeWarningPanel.SetActive(true); // Shows the warning panel before animating it.

        Image img = timeWarningPanel.GetComponent<Image>(); // Reads the warning panel Image component for color changes.
        if (img != null) img.color = new Color(colorR, colorG, colorB, colorA); // Applies the warning color when an Image component exists.

        LeanTween.cancel(warningRect.gameObject); // Cancels any active movement tween on the warning panel.
        LeanTween.cancel(warningCanvasGroup.gameObject); // Cancels any active fade tween on the warning panel.

        float screenWidth = Screen.width; // Stores the screen width used for offscreen slide positions.
        warningRect.anchoredPosition = new Vector2(screenWidth, warningRect.anchoredPosition.y); // Places the warning just off the right side of the screen.
        warningCanvasGroup.alpha = 1f; // Makes the warning fully visible before fading.

        if (warningText != null) // Checks whether warning text is assigned.
            warningText.text = message; // Displays the warning message.

        LeanTween.moveX(warningRect, -screenWidth, slideDuration) // Animates the warning panel from right to left across the screen.
            .setEase(LeanTweenType.easeInOutQuad); // Uses a smooth ease-in-out curve for the slide.

        LeanTween.alphaCanvas(warningCanvasGroup, 0f, 1.5f) // Starts fading the warning panel to transparent.
            .setDelay(Mathf.Max(0f, slideDuration - 1.5f)) // Delays the fade until near the end of the slide.
            .setOnComplete(() => // Registers cleanup to run after the fade finishes.
            { // Starts the fade-complete callback body.
                timeWarningPanel.SetActive(false); // Hides the warning panel after the animation finishes.
            }); // Ends the fade-complete callback and tween chain.
    } // Ends the ShowTimeWarning RPC body.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    public void HideTimeWarning() // Hides the timer warning panel when requested.
    { // Starts the HideTimeWarning RPC body.
        if (timeWarningPanel != null && timeWarningPanel.activeSelf) // Checks whether the warning panel exists and is currently visible.
            timeWarningPanel.SetActive(false); // Hides the active warning panel.
    } // Ends the HideTimeWarning RPC body.
} // Ends the GameTimerManager class body.
