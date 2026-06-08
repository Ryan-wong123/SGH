using UnityEngine; // Imports a namespace used by this UI script.
using UnityEngine.UI; // Imports a namespace used by this UI script.
using System.Collections; // Imports a namespace used by this UI script.
using ExitGames.Client.Photon; // Imports a namespace used by this UI script.
using Photon.Pun; // Imports a namespace used by this UI script.
using Photon.Realtime; // Imports a namespace used by this UI script.
using TMPro; // Imports a namespace used by this UI script.

public class SlideshowUI : MonoBehaviourPunCallbacks // Declares this UI type.
{ // Starts this code block.
    [Header("UI References")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private Image displayImage; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Button prevButton; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Button nextButton; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Button closeButton; // Exposes this private field in the Unity Inspector.

    [Header("Multiplayer Close Gate")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private bool requireAllPlayersToClickClose = true; // Exposes this private field in the Unity Inspector.
    [SerializeField] private string comicId; // Exposes this private field in the Unity Inspector.

    [Header("Stage Transition")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private bool advancePuzzleStageOnClose; // Exposes this private field in the Unity Inspector.
    [SerializeField] private PuzzleStateManager.PuzzleStage closeTargetStage = PuzzleStateManager.PuzzleStage.Puzzle1; // Exposes this private field in the Unity Inspector.

    [Header("Room Transition")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private bool leaveRoomAndLoadLobbyOnClose; // Exposes this private field in the Unity Inspector.
    [SerializeField] private string lobbySceneName = "Lobby"; // Exposes this private field in the Unity Inspector.

    public enum SlideOrientationMode // Declares this UI type.
    { // Starts this code block.
        Auto, // Runs this line of UI logic.
        ForcePortrait, // Runs this line of UI logic.
        ForceLandscape // Runs this line of UI logic.
    } // Ends this code block.

    [System.Serializable] // Adds an attribute that changes how Unity or C# treats the next declaration.
    public struct SlideData // Declares this UI type.
    { // Starts this code block.
        public Sprite image; // Declares a field, property, or member used by this UI script.
        public float duration; // how long this slide stays (seconds)
        public SlideOrientationMode orientationMode; // Declares a field, property, or member used by this UI script.
    } // Ends this code block.

    [Header("Slides")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private SlideData[] slides; // Exposes this private field in the Unity Inspector.

    private int currentIndex = 0; // Declares a field, property, or member used by this UI script.

    [Header("Audio")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private AudioSource audioSource; // Exposes this private field in the Unity Inspector.
    [SerializeField] private AudioClip onEnableClip; // Exposes this private field in the Unity Inspector.

    [Header("Autoplay")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private bool autoPlay = false; // Exposes this private field in the Unity Inspector.
    [SerializeField] private bool loop = false; // Exposes this private field in the Unity Inspector.
    [SerializeField] private float defaultSlideDuration = 2f; // Exposes this private field in the Unity Inspector.

    [Header("Close Button Auto Size")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private Vector2 closeButtonTextPadding = new Vector2(48f, 20f); // Exposes this private field in the Unity Inspector.
    [SerializeField] private float closeButtonMaxWidth = 260f; // Exposes this private field in the Unity Inspector.

    private Coroutine autoPlayRoutine; // Declares a field, property, or member used by this UI script.
    private TMP_Text closeButtonLabel; // Declares a field, property, or member used by this UI script.
    private string defaultCloseButtonText = "Close"; // Declares a field, property, or member used by this UI script.
    private RectTransform closeButtonRect; // Declares a field, property, or member used by this UI script.
    private Vector2 closeButtonMinSize; // Declares a field, property, or member used by this UI script.
    private string comicClosePropertyKey; // Declares a field, property, or member used by this UI script.
    private string comicClosedPropertyKey; // Declares a field, property, or member used by this UI script.
    private string comicCloseRoundPropertyKey; // Declares a field, property, or member used by this UI script.
    private bool closeListenerRegistered; // Declares a field, property, or member used by this UI script.
    private bool pendingCloseClick; // Declares a field, property, or member used by this UI script.
    private bool hasStartedRoomTransition; // Declares a field, property, or member used by this UI script.
    private static SlideshowUI activeInstance; // Declares a field, property, or member used by this UI script.

    [Header("Image Layout")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private Vector2 portraitSize = new Vector2(600, 900); // Exposes this private field in the Unity Inspector.
    [SerializeField] private Vector2 landscapeSize = new Vector2(900, 600); // Exposes this private field in the Unity Inspector.
    [SerializeField] private bool preserveAspect = true; // Exposes this private field in the Unity Inspector.

    public static bool ShouldSuppressLegacyCloseHandlers() // Declares a method used by this UI script.
    { // Starts this code block.
        return activeInstance != null && activeInstance.isActiveAndEnabled; // Returns this result to the caller.
    } // Ends this code block.

    public override void OnEnable() // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        base.OnEnable(); // Runs this line of UI logic.
        activeInstance = this; // Updates this value.
        currentIndex = 0; // Updates this value.
        EnsureCloseButtonSetup(); // Runs this line of UI logic.
        pendingCloseClick = false; // Updates this value.
        hasStartedRoomTransition = false; // Updates this value.
        ResetLocalCloseState(); // Runs this line of UI logic.
        BeginCloseGateRoundIfNeeded(); // Runs this line of UI logic.

        // Play audio when panel is enabled
        if (audioSource && onEnableClip) // Checks this condition before continuing.
        { // Starts this code block.
            audioSource.clip = onEnableClip; // Updates this value.
            audioSource.Play(); // Runs this line of UI logic.
        } // Ends this code block.

        UpdateImage(); // Runs this line of UI logic.
        UpdateButtonState(); // Runs this line of UI logic.

        // Start autoplay (if enabled and we have slides)
        if (autoPlay && slides.Length > 0) // Checks this condition before continuing.
            StartAutoPlay(); // Runs this line of UI logic.
    } // Ends this code block.

    public override void OnDisable() // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        base.OnDisable(); // Runs this line of UI logic.
        if (activeInstance == this) // Checks this condition before continuing.
            activeInstance = null; // Updates this value.

        if (closeButton != null && closeListenerRegistered) // Checks this condition before continuing.
        { // Starts this code block.
            closeButton.onClick.RemoveListener(HandleCloseButtonClicked); // Runs this line of UI logic.
            closeListenerRegistered = false; // Updates this value.
        } // Ends this code block.

        RestoreDefaultCloseButtonText(); // Runs this line of UI logic.

        // Stop audio
        if (audioSource && audioSource.isPlaying) // Checks this condition before continuing.
            audioSource.Stop(); // Runs this line of UI logic.

        // Stop autoplay coroutine
        if (autoPlayRoutine != null) // Checks this condition before continuing.
        { // Starts this code block.
            StopCoroutine(autoPlayRoutine); // Stops a Unity coroutine.
            autoPlayRoutine = null; // Updates this value.
        } // Ends this code block.

    } // Ends this code block.

    private void Start() // Declares a method used by this UI script.
    { // Starts this code block.
        if (slides.Length == 0) // Checks this condition before continuing.
        { // Starts this code block.
            Debug.LogWarning("No slides assigned!"); // Writes a warning message to the Unity console.
            return; // Returns this result to the caller.
        } // Ends this code block.

        EnsureCloseButtonSetup(); // Runs this line of UI logic.

        // Hook up buttons
        prevButton.onClick.AddListener(ShowPrevImage); // Runs this line of UI logic.
        nextButton.onClick.AddListener(ShowNextImage); // Runs this line of UI logic.

        // Initial button state
        UpdateCloseButton(); // Runs this line of UI logic.
        UpdateButtonState(); // Runs this line of UI logic.
    } // Ends this code block.

    private void EnsureCloseButtonSetup() // Declares a method used by this UI script.
    { // Starts this code block.
        if (closeButton == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        closeButtonLabel = closeButton.GetComponentInChildren<TMP_Text>(true); // Updates this value.
        closeButtonRect = closeButton.GetComponent<RectTransform>(); // Updates this value.
        if (closeButtonRect != null && closeButtonMinSize == Vector2.zero) // Checks this condition before continuing.
            closeButtonMinSize = closeButtonRect.rect.size; // Updates this value.

        if (closeButtonLabel != null && string.IsNullOrWhiteSpace(defaultCloseButtonText)) // Checks this condition before continuing.
            defaultCloseButtonText = closeButtonLabel.text; // Updates this value.
        else if (closeButtonLabel != null) // Runs when the previous condition was false.
            defaultCloseButtonText = closeButtonLabel.text; // Updates this value.

        comicClosePropertyKey = BuildComicClosePropertyKey(); // Updates this value.
        comicClosedPropertyKey = comicClosePropertyKey + "_Done"; // Updates this value.
        comicCloseRoundPropertyKey = comicClosePropertyKey + "_Round"; // Updates this value.

        if (!closeListenerRegistered) // Checks this condition before continuing.
        { // Starts this code block.
            closeButton.onClick.AddListener(HandleCloseButtonClicked); // Runs this line of UI logic.
            closeListenerRegistered = true; // Updates this value.
        } // Ends this code block.
    } // Ends this code block.

    private void ShowPrevImage() // Declares a method used by this UI script.
    { // Starts this code block.
        if (currentIndex > 0) // Checks this condition before continuing.
        { // Starts this code block.
            currentIndex--; // Runs this line of UI logic.
            UpdateImage(); // Runs this line of UI logic.
            UpdateButtonState(); // Runs this line of UI logic.

            // Reset autoplay timer after manual navigation
            if (autoPlay) // Checks this condition before continuing.
                StartAutoPlay(); // Runs this line of UI logic.
        } // Ends this code block.
        else if (loop && slides.Length > 0) // Runs when the previous condition was false.
        { // Starts this code block.
            // Optional: if loop enabled, allow prev wrap-around
            currentIndex = slides.Length - 1; // Updates this value.
            UpdateImage(); // Runs this line of UI logic.
            UpdateButtonState(); // Runs this line of UI logic.

            if (autoPlay) // Checks this condition before continuing.
                StartAutoPlay(); // Runs this line of UI logic.
        } // Ends this code block.
    } // Ends this code block.
    private void UpdateCloseButton() // Declares a method used by this UI script.
    { // Starts this code block.
        bool isLastSlide = currentIndex == slides.Length - 1; // Creates a local value used by the following UI logic.
        closeButton.gameObject.SetActive(isLastSlide); // Runs this line of UI logic.
        RefreshCloseGateUI(); // Runs this line of UI logic.
    } // Ends this code block.

    private void ShowNextImage() // Declares a method used by this UI script.
    { // Starts this code block.
        if (currentIndex < slides.Length - 1) // Checks this condition before continuing.
        { // Starts this code block.
            currentIndex++; // Runs this line of UI logic.
            UpdateImage(); // Runs this line of UI logic.
            UpdateButtonState(); // Runs this line of UI logic.

            // Reset autoplay timer after manual navigation
            if (autoPlay) // Checks this condition before continuing.
                StartAutoPlay(); // Runs this line of UI logic.
        } // Ends this code block.
        else if (loop && slides.Length > 0) // Runs when the previous condition was false.
        { // Starts this code block.
            // Optional: if loop enabled, allow next wrap-around
            currentIndex = 0; // Updates this value.
            UpdateImage(); // Runs this line of UI logic.
            UpdateButtonState(); // Runs this line of UI logic.

            if (autoPlay) // Checks this condition before continuing.
                StartAutoPlay(); // Runs this line of UI logic.
        } // Ends this code block.
    } // Ends this code block.

    private void UpdateImage() // Declares a method used by this UI script.
    { // Starts this code block.
        SlideData slide = slides[currentIndex]; // Runs this line of UI logic.

        displayImage.sprite = slide.image; // Updates this value.
        displayImage.preserveAspect = preserveAspect; // Updates this value.

        RectTransform rt = displayImage.rectTransform; // Runs this line of UI logic.

        bool isPortrait = false; // Creates a local value used by the following UI logic.

        switch (slide.orientationMode) // Chooses behavior based on this value.
        { // Starts this code block.
            case SlideOrientationMode.ForcePortrait: // Handles this switch case.
                isPortrait = true; // Updates this value.
                break; // Exits the current loop or switch.

            case SlideOrientationMode.ForceLandscape: // Handles this switch case.
                isPortrait = false; // Updates this value.
                break; // Exits the current loop or switch.

            case SlideOrientationMode.Auto: // Handles this switch case.
            default: // Handles any unmatched switch case.
                if (slide.image != null) // Checks this condition before continuing.
                { // Starts this code block.
                    // Auto-detect from sprite pixel rect
                    isPortrait = slide.image.rect.height > slide.image.rect.width; // Updates this value.
                } // Ends this code block.
                break; // Exits the current loop or switch.
        } // Ends this code block.

        rt.sizeDelta = isPortrait ? portraitSize : landscapeSize; // Updates this value.

        UpdateCloseButton(); // Runs this line of UI logic.
    } // Ends this code block.

    private void UpdateButtonState() // Declares a method used by this UI script.
    { // Starts this code block.
        // Prev disabled at first slide (unless looping via buttons)
        prevButton.interactable = loop ? true : currentIndex > 0; // Updates this value.

        if (!loop) // Checks this condition before continuing.
        { // Starts this code block.
            // Next disabled at last slide
            nextButton.interactable = currentIndex < slides.Length - 1; // Updates this value.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            // If looping, always allow
            prevButton.interactable = true; // Updates this value.
            nextButton.interactable = true; // Updates this value.
        } // Ends this code block.
    } // Ends this code block.

    // ---------- AUTOPLAY ----------

    private void StartAutoPlay() // Declares a method used by this UI script.
    { // Starts this code block.
        if (slides.Length == 0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        // Stop any existing autoplay coroutine so we restart delay
        if (autoPlayRoutine != null) // Checks this condition before continuing.
            StopCoroutine(autoPlayRoutine); // Stops a Unity coroutine.

        autoPlayRoutine = StartCoroutine(AutoPlayRoutine()); // Updates this value.
    } // Ends this code block.

    private IEnumerator AutoPlayRoutine() // Declares a method used by this UI script.
    { // Starts this code block.
        while (true) // Repeats this block while the condition stays true.
        { // Starts this code block.
            // Get this slide's duration; fall back to default if not set or <= 0
            float duration = slides[currentIndex].duration; // Creates a local value used by the following UI logic.
            if (duration <= 0f) // Checks this condition before continuing.
                duration = defaultSlideDuration; // Updates this value.

            // IMPORTANT: Use real-time so it still runs if Time.timeScale == 0
            yield return new WaitForSecondsRealtime(duration); // Pauses this coroutine until the yielded condition finishes.

            // Move to next slide
            if (currentIndex < slides.Length - 1) // Checks this condition before continuing.
            { // Starts this code block.
                currentIndex++; // Runs this line of UI logic.
                UpdateImage(); // Runs this line of UI logic.
                UpdateButtonState(); // Runs this line of UI logic.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                if (loop) // Checks this condition before continuing.
                { // Starts this code block.
                    currentIndex = 0; // Updates this value.
                    UpdateImage(); // Runs this line of UI logic.
                    UpdateButtonState(); // Runs this line of UI logic.
                } // Ends this code block.
                else // Runs when the previous condition was false.
                { // Starts this code block.
                    // End of slideshow, stop autoplay
                    autoPlayRoutine = null; // Updates this value.
                    yield break; // Pauses this coroutine until the yielded condition finishes.
                } // Ends this code block.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private void HandleCloseButtonClicked() // Declares a method used by this UI script.
    { // Starts this code block.
        if (slides == null || slides.Length == 0 || currentIndex != slides.Length - 1) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (!ShouldUseSharedCloseGate()) // Checks this condition before continuing.
        { // Starts this code block.
            CompleteClose(); // Runs this line of UI logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        int currentRound = GetCurrentCloseRound(); // Creates a local value used by the following UI logic.
        if (currentRound <= 0) // Checks this condition before continuing.
        { // Starts this code block.
            pendingCloseClick = true; // Updates this value.
            RefreshCloseGateUI(); // Runs this line of UI logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        pendingCloseClick = false; // Updates this value.

        if (!HasLocalPlayerClickedClose()) // Checks this condition before continuing.
        { // Starts this code block.
            SetLocalPlayerCloseClicked(true); // Runs this line of UI logic.
        } // Ends this code block.

        RefreshCloseGateUI(); // Runs this line of UI logic.

        if (GetExpectedClosePlayerCount() <= 1) // Checks this condition before continuing.
        { // Starts this code block.
            MarkComicClosedForEveryone(); // Runs this line of UI logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        TryCompleteSharedCloseGate(); // Runs this line of UI logic.
    } // Ends this code block.

    private bool ShouldUseSharedCloseGate() // Declares a method used by this UI script.
    { // Starts this code block.
        return requireAllPlayersToClickClose && PhotonNetwork.IsConnected && PhotonNetwork.InRoom; // Returns this result to the caller.
    } // Ends this code block.

    private void RefreshCloseGateUI() // Declares a method used by this UI script.
    { // Starts this code block.
        if (closeButton == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        bool isLastSlide = slides != null && slides.Length > 0 && currentIndex == slides.Length - 1; // Creates a local value used by the following UI logic.
        if (!isLastSlide) // Checks this condition before continuing.
        { // Starts this code block.
            RestoreDefaultCloseButtonText(); // Runs this line of UI logic.
            closeButton.interactable = true; // Updates this value.
            return; // Returns this result to the caller.
        } // Ends this code block.

        if (!ShouldUseSharedCloseGate()) // Checks this condition before continuing.
        { // Starts this code block.
            RestoreDefaultCloseButtonText(); // Runs this line of UI logic.
            closeButton.interactable = true; // Updates this value.
            return; // Returns this result to the caller.
        } // Ends this code block.

        int currentRound = GetCurrentCloseRound(); // Creates a local value used by the following UI logic.
        if (currentRound <= 0) // Checks this condition before continuing.
        { // Starts this code block.
            if (pendingCloseClick) // Checks this condition before continuing.
                SetCloseButtonText("Waiting for remaining players to close"); // Runs this line of UI logic.
            else // Runs when the previous condition was false.
                RestoreDefaultCloseButtonText(); // Runs this line of UI logic.

            closeButton.interactable = !pendingCloseClick; // Updates this value.
            return; // Returns this result to the caller.
        } // Ends this code block.

        int remainingPlayers = GetRemainingPlayersCount(); // Creates a local value used by the following UI logic.
        bool localClicked = HasLocalPlayerClickedClose(); // Creates a local value used by the following UI logic.

        if (remainingPlayers <= 0) // Checks this condition before continuing.
        { // Starts this code block.
            SetCloseButtonText("Closing..."); // Runs this line of UI logic.
            closeButton.interactable = false; // Updates this value.
            return; // Returns this result to the caller.
        } // Ends this code block.

        if (localClicked) // Checks this condition before continuing.
        { // Starts this code block.
            SetCloseButtonText($"Waiting for ({remainingPlayers} players to close)"); // Runs this line of UI logic.
            closeButton.interactable = false; // Updates this value.
            return; // Returns this result to the caller.
        } // Ends this code block.

        RestoreDefaultCloseButtonText(); // Runs this line of UI logic.
        closeButton.interactable = true; // Updates this value.
    } // Ends this code block.

    private void CompleteClose() // Declares a method used by this UI script.
    { // Starts this code block.
        Time.timeScale = 1f; // Updates this value.
        gameObject.SetActive(false); // Runs this line of UI logic.

        Transform parent = transform.parent; // Creates a local value used by the following UI logic.
        if (parent != null) // Checks this condition before continuing.
        { // Starts this code block.
            bool anyComicStillOpen = false; // Creates a local value used by the following UI logic.
            for (int i = 0; i < parent.childCount; i++) // Repeats this block for a counted loop.
            { // Starts this code block.
                Transform child = parent.GetChild(i); // Creates a local value used by the following UI logic.
                if (child != null && child != transform && child.gameObject.activeSelf && child.GetComponent<SlideshowUI>() != null) // Checks this condition before continuing.
                { // Starts this code block.
                    anyComicStillOpen = true; // Updates this value.
                    break; // Exits the current loop or switch.
                } // Ends this code block.
            } // Ends this code block.

            if (!anyComicStillOpen) // Checks this condition before continuing.
                parent.gameObject.SetActive(false); // Runs this line of UI logic.
        } // Ends this code block.

        ClosePanelAndResetCamera.ResetCameraAndCursor(); // Runs this line of UI logic.
        TryAdvancePuzzleStageOnClose(); // Runs this line of UI logic.
        TryLeaveRoomAndLoadLobbyOnClose(); // Runs this line of UI logic.
    } // Ends this code block.

    private void TryAdvancePuzzleStageOnClose() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!advancePuzzleStageOnClose || PuzzleStateManager.Instance == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        PuzzleStateManager.Instance.BroadcastStageTransition(closeTargetStage); // Runs this line of UI logic.
    } // Ends this code block.

    private void TryLeaveRoomAndLoadLobbyOnClose() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!leaveRoomAndLoadLobbyOnClose || hasStartedRoomTransition) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        hasStartedRoomTransition = true; // Updates this value.
        GameManager.LeaveRoomAndLoadScene(lobbySceneName); // Runs this line of UI logic.
    } // Ends this code block.

    private void ResetLocalCloseState() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate()) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        SetLocalPlayerCloseClicked(false); // Runs this line of UI logic.
    } // Ends this code block.

    private void BeginCloseGateRoundIfNeeded() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate() || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        int nextRound = GetCurrentCloseRound() + 1; // Creates a local value used by the following UI logic.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable // Runs this line of UI logic.
        { // Starts this code block.
            [comicClosedPropertyKey] = false, // Adds an attribute that changes how Unity or C# treats the next declaration.
            [comicCloseRoundPropertyKey] = nextRound // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of UI logic.
    } // Ends this code block.

    private void MarkComicClosedForEveryone() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
        { // Starts this code block.
            CompleteClose(); // Runs this line of UI logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable // Runs this line of UI logic.
        { // Starts this code block.
            [comicClosedPropertyKey] = true // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of UI logic.
    } // Ends this code block.

    private void TryCompleteSharedCloseGate() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate()) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (GetRemainingPlayersCount() == 0 && HasAnyPlayerClickedClose()) // Checks this condition before continuing.
            MarkComicClosedForEveryone(); // Runs this line of UI logic.
    } // Ends this code block.

    private void SetLocalPlayerCloseClicked(bool clicked) // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.LocalPlayer == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        int currentRound = GetCurrentCloseRound(); // Creates a local value used by the following UI logic.
        int clickedRound = clicked && currentRound > 0 ? currentRound : -1; // Creates a local value used by the following UI logic.

        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable // Runs this line of UI logic.
        { // Starts this code block.
            [comicClosePropertyKey] = clickedRound // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of UI logic.
    } // Ends this code block.

    private bool HasLocalPlayerClickedClose() // Declares a method used by this UI script.
    { // Starts this code block.
        return HasPlayerClickedClose(PhotonNetwork.LocalPlayer); // Returns this result to the caller.
    } // Ends this code block.

    private bool HasPlayerClickedClose(Player player) // Declares a method used by this UI script.
    { // Starts this code block.
        if (player == null || string.IsNullOrEmpty(comicClosePropertyKey)) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        int currentRound = GetCurrentCloseRound(); // Creates a local value used by the following UI logic.
        if (currentRound <= 0) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        if (player.CustomProperties != null && // Checks this condition before continuing.
            player.CustomProperties.TryGetValue(comicClosePropertyKey, out object value) && // Runs this line of UI logic.
            value is int clickedRound) // Runs this line of UI logic.
        { // Starts this code block.
            return clickedRound == currentRound; // Returns this result to the caller.
        } // Ends this code block.

        return false; // Returns this result to the caller.
    } // Ends this code block.

    private int GetRemainingPlayersCount() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate()) // Checks this condition before continuing.
            return 0; // Returns this result to the caller.

        int remaining = 0; // Creates a local value used by the following UI logic.
        Player[] players = PhotonNetwork.PlayerList; // Runs this line of UI logic.
        for (int i = 0; i < players.Length; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            if (!ShouldExpectPlayerClose(players[i])) // Checks this condition before continuing.
                continue; // Skips to the next loop iteration.

            if (!HasPlayerClickedClose(players[i])) // Checks this condition before continuing.
                remaining++; // Runs this line of UI logic.
        } // Ends this code block.

        return remaining; // Returns this result to the caller.
    } // Ends this code block.

    private int GetExpectedClosePlayerCount() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate()) // Checks this condition before continuing.
            return 0; // Returns this result to the caller.

        int count = 0; // Creates a local value used by the following UI logic.
        Player[] players = PhotonNetwork.PlayerList; // Runs this line of UI logic.
        for (int i = 0; i < players.Length; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            if (ShouldExpectPlayerClose(players[i])) // Checks this condition before continuing.
                count++; // Runs this line of UI logic.
        } // Ends this code block.

        return count; // Returns this result to the caller.
    } // Ends this code block.

    private bool ShouldExpectPlayerClose(Player player) // Declares a method used by this UI script.
    { // Starts this code block.
        return player != null && !player.IsInactive; // Returns this result to the caller.
    } // Ends this code block.

    private string BuildComicClosePropertyKey() // Declares a method used by this UI script.
    { // Starts this code block.
        string rawId = string.IsNullOrWhiteSpace(comicId) ? BuildStableComicPath() : comicId; // Creates a local value used by the following UI logic.
        rawId = rawId.Trim(); // Updates this value.

        char[] chars = rawId.ToCharArray(); // Runs this line of UI logic.
        for (int i = 0; i < chars.Length; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            if (!char.IsLetterOrDigit(chars[i])) // Checks this condition before continuing.
                chars[i] = '_'; // Updates this value.
        } // Ends this code block.

        return "ComicClose_" + new string(chars); // Returns this result to the caller.
    } // Ends this code block.

    private string BuildStableComicPath() // Declares a method used by this UI script.
    { // Starts this code block.
        System.Collections.Generic.Stack<string> parts = new System.Collections.Generic.Stack<string>(); // Runs this line of UI logic.
        Transform current = transform; // Creates a local value used by the following UI logic.

        while (current != null) // Repeats this block while the condition stays true.
        { // Starts this code block.
            parts.Push(current.name); // Runs this line of UI logic.
            current = current.parent; // Updates this value.
        } // Ends this code block.

        return string.Join("/", parts.ToArray()); // Returns this result to the caller.
    } // Ends this code block.

    private int GetCurrentCloseRound() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.CurrentRoom == null || string.IsNullOrEmpty(comicCloseRoundPropertyKey)) // Checks this condition before continuing.
            return 0; // Returns this result to the caller.

        if (PhotonNetwork.CurrentRoom.CustomProperties != null && // Checks this condition before continuing.
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(comicCloseRoundPropertyKey, out object value) && // Runs this line of UI logic.
            value is int round) // Runs this line of UI logic.
        { // Starts this code block.
            return round; // Returns this result to the caller.
        } // Ends this code block.

        return 0; // Returns this result to the caller.
    } // Ends this code block.

    private void SetCloseButtonText(string value) // Declares a method used by this UI script.
    { // Starts this code block.
        if (closeButtonLabel != null) // Checks this condition before continuing.
        { // Starts this code block.
            closeButtonLabel.text = value; // Updates this value.
            ResizeCloseButtonToText(); // Runs this line of UI logic.
        } // Ends this code block.
    } // Ends this code block.

    private void RestoreDefaultCloseButtonText() // Declares a method used by this UI script.
    { // Starts this code block.
        if (closeButtonLabel != null) // Checks this condition before continuing.
            SetCloseButtonText(defaultCloseButtonText); // Runs this line of UI logic.
    } // Ends this code block.

    private void ResizeCloseButtonToText() // Declares a method used by this UI script.
    { // Starts this code block.
        if (closeButtonRect == null || closeButtonLabel == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        closeButtonLabel.ForceMeshUpdate(); // Runs this line of UI logic.

        float targetWidth = Mathf.Max(closeButtonMinSize.x, closeButtonMaxWidth); // Creates a local value used by the following UI logic.
        float wrappedTextWidth = Mathf.Max(1f, targetWidth - closeButtonTextPadding.x); // Creates a local value used by the following UI logic.
        Vector2 preferredValues = closeButtonLabel.GetPreferredValues(closeButtonLabel.text, wrappedTextWidth, 0f); // Creates a local value used by the following UI logic.

        float targetHeight = Mathf.Max( // Creates a local value used by the following UI logic.
            closeButtonMinSize.y, // Runs this line of UI logic.
            preferredValues.y + closeButtonTextPadding.y); // Runs this line of UI logic.

        closeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth); // Runs this line of UI logic.
        closeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight); // Runs this line of UI logic.
    } // Ends this code block.

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate() || changedProps == null || string.IsNullOrEmpty(comicClosePropertyKey)) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (!changedProps.ContainsKey(comicClosePropertyKey)) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        RefreshCloseGateUI(); // Runs this line of UI logic.
        TryCompleteSharedCloseGate(); // Runs this line of UI logic.
    } // Ends this code block.

    public override void OnPlayerEnteredRoom(Player newPlayer) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        RefreshCloseGateUI(); // Runs this line of UI logic.
    } // Ends this code block.

    public override void OnPlayerLeftRoom(Player otherPlayer) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        RefreshCloseGateUI(); // Runs this line of UI logic.

        TryCompleteSharedCloseGate(); // Runs this line of UI logic.
    } // Ends this code block.

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate() || propertiesThatChanged == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (!string.IsNullOrEmpty(comicCloseRoundPropertyKey) && propertiesThatChanged.ContainsKey(comicCloseRoundPropertyKey)) // Checks this condition before continuing.
        { // Starts this code block.
            ResetLocalCloseState(); // Runs this line of UI logic.

            if (pendingCloseClick) // Checks this condition before continuing.
            { // Starts this code block.
                pendingCloseClick = false; // Updates this value.
                SetLocalPlayerCloseClicked(true); // Runs this line of UI logic.
            } // Ends this code block.

            RefreshCloseGateUI(); // Runs this line of UI logic.
            TryCompleteSharedCloseGate(); // Runs this line of UI logic.
        } // Ends this code block.

        if (!string.IsNullOrEmpty(comicClosedPropertyKey) && // Checks this condition before continuing.
            propertiesThatChanged.TryGetValue(comicClosedPropertyKey, out object value) && // Runs this line of UI logic.
            value is bool isClosed && isClosed) // Runs this line of UI logic.
        { // Starts this code block.
            CompleteClose(); // Runs this line of UI logic.
        } // Ends this code block.
    } // Ends this code block.

    private bool HasAnyPlayerClickedClose() // Declares a method used by this UI script.
    { // Starts this code block.
        if (!ShouldUseSharedCloseGate()) // Checks this condition before continuing.
            return false; // Returns this result to the caller.

        Player[] players = PhotonNetwork.PlayerList; // Runs this line of UI logic.
        for (int i = 0; i < players.Length; i++) // Repeats this block for a counted loop.
        { // Starts this code block.
            if (!ShouldExpectPlayerClose(players[i])) // Checks this condition before continuing.
                continue; // Skips to the next loop iteration.

            if (HasPlayerClickedClose(players[i])) // Checks this condition before continuing.
                return true; // Returns this result to the caller.
        } // Ends this code block.

        return false; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
