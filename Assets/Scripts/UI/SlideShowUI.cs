using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class SlideshowUI : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private Image displayImage;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    [Header("Multiplayer Close Gate")]
    [SerializeField] private bool requireAllPlayersToClickClose = true;
    [SerializeField] private string comicId;

    [Header("Stage Transition")]
    [SerializeField] private bool advancePuzzleStageOnClose;
    [SerializeField] private PuzzleStateManager.PuzzleStage closeTargetStage = PuzzleStateManager.PuzzleStage.Puzzle1;

    [Header("Room Transition")]
    [SerializeField] private bool leaveRoomAndLoadLobbyOnClose;
    [SerializeField] private string lobbySceneName = "Lobby";

    public enum SlideOrientationMode
    {
        Auto,
        ForcePortrait,
        ForceLandscape
    }

    [System.Serializable]
    public struct SlideData
    {
        public Sprite image;
        public float duration; // how long this slide stays (seconds)
        public SlideOrientationMode orientationMode;
    }

    [Header("Slides")]
    [SerializeField] private SlideData[] slides;

    private int currentIndex = 0;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip onEnableClip;

    [Header("Autoplay")]
    [SerializeField] private bool autoPlay = false;
    [SerializeField] private bool loop = false;
    [SerializeField] private float defaultSlideDuration = 2f;

    [Header("Close Button Auto Size")]
    [SerializeField] private Vector2 closeButtonTextPadding = new Vector2(48f, 20f);
    [SerializeField] private float closeButtonMaxWidth = 260f;

    private Coroutine autoPlayRoutine;
    private TMP_Text closeButtonLabel;
    private string defaultCloseButtonText = "Close";
    private RectTransform closeButtonRect;
    private Vector2 closeButtonMinSize;
    private string comicClosePropertyKey;
    private string comicClosedPropertyKey;
    private string comicCloseRoundPropertyKey;
    private bool closeListenerRegistered;
    private bool pendingCloseClick;
    private bool hasStartedRoomTransition;
    private static SlideshowUI activeInstance;

    [Header("Image Layout")]
    [SerializeField] private Vector2 portraitSize = new Vector2(600, 900);
    [SerializeField] private Vector2 landscapeSize = new Vector2(900, 600);
    [SerializeField] private bool preserveAspect = true;

    public static bool ShouldSuppressLegacyCloseHandlers()
    {
        return activeInstance != null && activeInstance.isActiveAndEnabled;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        activeInstance = this;
        currentIndex = 0;
        EnsureCloseButtonSetup();
        pendingCloseClick = false;
        hasStartedRoomTransition = false;
        ResetLocalCloseState();
        BeginCloseGateRoundIfNeeded();

        // Play audio when panel is enabled
        if (audioSource && onEnableClip)
        {
            audioSource.clip = onEnableClip;
            audioSource.Play();
        }

        UpdateImage();
        UpdateButtonState();

        // Start autoplay (if enabled and we have slides)
        if (autoPlay && slides.Length > 0)
            StartAutoPlay();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (activeInstance == this)
            activeInstance = null;

        if (closeButton != null && closeListenerRegistered)
        {
            closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
            closeListenerRegistered = false;
        }

        RestoreDefaultCloseButtonText();

        // Stop audio
        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();

        // Stop autoplay coroutine
        if (autoPlayRoutine != null)
        {
            StopCoroutine(autoPlayRoutine);
            autoPlayRoutine = null;
        }

    }

    private void Start()
    {
        if (slides.Length == 0)
        {
            Debug.LogWarning("No slides assigned!");
            return;
        }

        EnsureCloseButtonSetup();

        // Hook up buttons
        prevButton.onClick.AddListener(ShowPrevImage);
        nextButton.onClick.AddListener(ShowNextImage);

        // Initial button state
        UpdateCloseButton();
        UpdateButtonState();
    }

    private void EnsureCloseButtonSetup()
    {
        if (closeButton == null)
            return;

        closeButtonLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
        closeButtonRect = closeButton.GetComponent<RectTransform>();
        if (closeButtonRect != null && closeButtonMinSize == Vector2.zero)
            closeButtonMinSize = closeButtonRect.rect.size;

        if (closeButtonLabel != null && string.IsNullOrWhiteSpace(defaultCloseButtonText))
            defaultCloseButtonText = closeButtonLabel.text;
        else if (closeButtonLabel != null)
            defaultCloseButtonText = closeButtonLabel.text;

        comicClosePropertyKey = BuildComicClosePropertyKey();
        comicClosedPropertyKey = comicClosePropertyKey + "_Done";
        comicCloseRoundPropertyKey = comicClosePropertyKey + "_Round";

        if (!closeListenerRegistered)
        {
            closeButton.onClick.AddListener(HandleCloseButtonClicked);
            closeListenerRegistered = true;
        }
    }

    private void ShowPrevImage()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateImage();
            UpdateButtonState();

            // Reset autoplay timer after manual navigation
            if (autoPlay)
                StartAutoPlay();
        }
        else if (loop && slides.Length > 0)
        {
            // Optional: if loop enabled, allow prev wrap-around
            currentIndex = slides.Length - 1;
            UpdateImage();
            UpdateButtonState();

            if (autoPlay)
                StartAutoPlay();
        }
    }
    private void UpdateCloseButton()
    {
        bool isLastSlide = currentIndex == slides.Length - 1;
        closeButton.gameObject.SetActive(isLastSlide);
        RefreshCloseGateUI();
    }

    private void ShowNextImage()
    {
        if (currentIndex < slides.Length - 1)
        {
            currentIndex++;
            UpdateImage();
            UpdateButtonState();

            // Reset autoplay timer after manual navigation
            if (autoPlay)
                StartAutoPlay();
        }
        else if (loop && slides.Length > 0)
        {
            // Optional: if loop enabled, allow next wrap-around
            currentIndex = 0;
            UpdateImage();
            UpdateButtonState();

            if (autoPlay)
                StartAutoPlay();
        }
    }

    private void UpdateImage()
    {
        SlideData slide = slides[currentIndex];

        displayImage.sprite = slide.image;
        displayImage.preserveAspect = preserveAspect;

        RectTransform rt = displayImage.rectTransform;

        bool isPortrait = false;

        switch (slide.orientationMode)
        {
            case SlideOrientationMode.ForcePortrait:
                isPortrait = true;
                break;

            case SlideOrientationMode.ForceLandscape:
                isPortrait = false;
                break;

            case SlideOrientationMode.Auto:
            default:
                if (slide.image != null)
                {
                    // Auto-detect from sprite pixel rect
                    isPortrait = slide.image.rect.height > slide.image.rect.width;
                }
                break;
        }

        rt.sizeDelta = isPortrait ? portraitSize : landscapeSize;

        UpdateCloseButton();
    }

    private void UpdateButtonState()
    {
        // Prev disabled at first slide (unless looping via buttons)
        prevButton.interactable = loop ? true : currentIndex > 0;

        if (!loop)
        {
            // Next disabled at last slide
            nextButton.interactable = currentIndex < slides.Length - 1;
        }
        else
        {
            // If looping, always allow
            prevButton.interactable = true;
            nextButton.interactable = true;
        }
    }

    // ---------- AUTOPLAY ----------

    private void StartAutoPlay()
    {
        if (slides.Length == 0)
            return;

        // Stop any existing autoplay coroutine so we restart delay
        if (autoPlayRoutine != null)
            StopCoroutine(autoPlayRoutine);

        autoPlayRoutine = StartCoroutine(AutoPlayRoutine());
    }

    private IEnumerator AutoPlayRoutine()
    {
        while (true)
        {
            // Get this slide's duration; fall back to default if not set or <= 0
            float duration = slides[currentIndex].duration;
            if (duration <= 0f)
                duration = defaultSlideDuration;

            // IMPORTANT: Use real-time so it still runs if Time.timeScale == 0
            yield return new WaitForSecondsRealtime(duration);

            // Move to next slide
            if (currentIndex < slides.Length - 1)
            {
                currentIndex++;
                UpdateImage();
                UpdateButtonState();
            }
            else
            {
                if (loop)
                {
                    currentIndex = 0;
                    UpdateImage();
                    UpdateButtonState();
                }
                else
                {
                    // End of slideshow, stop autoplay
                    autoPlayRoutine = null;
                    yield break;
                }
            }
        }
    }

    private void HandleCloseButtonClicked()
    {
        if (slides == null || slides.Length == 0 || currentIndex != slides.Length - 1)
            return;

        if (!ShouldUseSharedCloseGate())
        {
            CompleteClose();
            return;
        }

        int currentRound = GetCurrentCloseRound();
        if (currentRound <= 0)
        {
            pendingCloseClick = true;
            RefreshCloseGateUI();
            return;
        }

        pendingCloseClick = false;

        if (!HasLocalPlayerClickedClose())
        {
            SetLocalPlayerCloseClicked(true);
        }

        RefreshCloseGateUI();

        if (GetExpectedClosePlayerCount() <= 1)
        {
            MarkComicClosedForEveryone();
            return;
        }

        TryCompleteSharedCloseGate();
    }

    private bool ShouldUseSharedCloseGate()
    {
        return requireAllPlayersToClickClose && PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
    }

    private void RefreshCloseGateUI()
    {
        if (closeButton == null)
            return;

        bool isLastSlide = slides != null && slides.Length > 0 && currentIndex == slides.Length - 1;
        if (!isLastSlide)
        {
            RestoreDefaultCloseButtonText();
            closeButton.interactable = true;
            return;
        }

        if (!ShouldUseSharedCloseGate())
        {
            RestoreDefaultCloseButtonText();
            closeButton.interactable = true;
            return;
        }

        int currentRound = GetCurrentCloseRound();
        if (currentRound <= 0)
        {
            if (pendingCloseClick)
                SetCloseButtonText("Waiting for remaining players to close");
            else
                RestoreDefaultCloseButtonText();

            closeButton.interactable = !pendingCloseClick;
            return;
        }

        int remainingPlayers = GetRemainingPlayersCount();
        bool localClicked = HasLocalPlayerClickedClose();

        if (remainingPlayers <= 0)
        {
            SetCloseButtonText("Closing...");
            closeButton.interactable = false;
            return;
        }

        if (localClicked)
        {
            SetCloseButtonText($"Waiting for ({remainingPlayers} players to close)");
            closeButton.interactable = false;
            return;
        }

        RestoreDefaultCloseButtonText();
        closeButton.interactable = true;
    }

    private void CompleteClose()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);

        Transform parent = transform.parent;
        if (parent != null)
        {
            bool anyComicStillOpen = false;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child != transform && child.gameObject.activeSelf && child.GetComponent<SlideshowUI>() != null)
                {
                    anyComicStillOpen = true;
                    break;
                }
            }

            if (!anyComicStillOpen)
                parent.gameObject.SetActive(false);
        }

        ClosePanelAndResetCamera.ResetCameraAndCursor();
        TryAdvancePuzzleStageOnClose();
        TryLeaveRoomAndLoadLobbyOnClose();
    }

    private void TryAdvancePuzzleStageOnClose()
    {
        if (!advancePuzzleStageOnClose || PuzzleStateManager.Instance == null)
            return;

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            return;

        PuzzleStateManager.Instance.BroadcastStageTransition(closeTargetStage);
    }

    private void TryLeaveRoomAndLoadLobbyOnClose()
    {
        if (!leaveRoomAndLoadLobbyOnClose || hasStartedRoomTransition)
            return;

        hasStartedRoomTransition = true;
        GameManager.LeaveRoomAndLoadScene(lobbySceneName);
    }

    private void ResetLocalCloseState()
    {
        if (!ShouldUseSharedCloseGate())
            return;

        SetLocalPlayerCloseClicked(false);
    }

    private void BeginCloseGateRoundIfNeeded()
    {
        if (!ShouldUseSharedCloseGate() || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        int nextRound = GetCurrentCloseRound() + 1;

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            [comicClosedPropertyKey] = false,
            [comicCloseRoundPropertyKey] = nextRound
        });
    }

    private void MarkComicClosedForEveryone()
    {
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.CurrentRoom == null)
        {
            CompleteClose();
            return;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            [comicClosedPropertyKey] = true
        });
    }

    private void TryCompleteSharedCloseGate()
    {
        if (!ShouldUseSharedCloseGate())
            return;

        if (GetRemainingPlayersCount() == 0 && HasAnyPlayerClickedClose())
            MarkComicClosedForEveryone();
    }

    private void SetLocalPlayerCloseClicked(bool clicked)
    {
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.LocalPlayer == null)
            return;

        int currentRound = GetCurrentCloseRound();
        int clickedRound = clicked && currentRound > 0 ? currentRound : -1;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            [comicClosePropertyKey] = clickedRound
        });
    }

    private bool HasLocalPlayerClickedClose()
    {
        return HasPlayerClickedClose(PhotonNetwork.LocalPlayer);
    }

    private bool HasPlayerClickedClose(Player player)
    {
        if (player == null || string.IsNullOrEmpty(comicClosePropertyKey))
            return false;

        int currentRound = GetCurrentCloseRound();
        if (currentRound <= 0)
            return false;

        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(comicClosePropertyKey, out object value) &&
            value is int clickedRound)
        {
            return clickedRound == currentRound;
        }

        return false;
    }

    private int GetRemainingPlayersCount()
    {
        if (!ShouldUseSharedCloseGate())
            return 0;

        int remaining = 0;
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (!ShouldExpectPlayerClose(players[i]))
                continue;

            if (!HasPlayerClickedClose(players[i]))
                remaining++;
        }

        return remaining;
    }

    private int GetExpectedClosePlayerCount()
    {
        if (!ShouldUseSharedCloseGate())
            return 0;

        int count = 0;
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (ShouldExpectPlayerClose(players[i]))
                count++;
        }

        return count;
    }

    private bool ShouldExpectPlayerClose(Player player)
    {
        return player != null && !player.IsInactive;
    }

    private string BuildComicClosePropertyKey()
    {
        string rawId = string.IsNullOrWhiteSpace(comicId) ? BuildStableComicPath() : comicId;
        rawId = rawId.Trim();

        char[] chars = rawId.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }

        return "ComicClose_" + new string(chars);
    }

    private string BuildStableComicPath()
    {
        System.Collections.Generic.Stack<string> parts = new System.Collections.Generic.Stack<string>();
        Transform current = transform;

        while (current != null)
        {
            parts.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", parts.ToArray());
    }

    private int GetCurrentCloseRound()
    {
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.CurrentRoom == null || string.IsNullOrEmpty(comicCloseRoundPropertyKey))
            return 0;

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(comicCloseRoundPropertyKey, out object value) &&
            value is int round)
        {
            return round;
        }

        return 0;
    }

    private void SetCloseButtonText(string value)
    {
        if (closeButtonLabel != null)
        {
            closeButtonLabel.text = value;
            ResizeCloseButtonToText();
        }
    }

    private void RestoreDefaultCloseButtonText()
    {
        if (closeButtonLabel != null)
            SetCloseButtonText(defaultCloseButtonText);
    }

    private void ResizeCloseButtonToText()
    {
        if (closeButtonRect == null || closeButtonLabel == null)
            return;

        closeButtonLabel.ForceMeshUpdate();

        float targetWidth = Mathf.Max(closeButtonMinSize.x, closeButtonMaxWidth);
        float wrappedTextWidth = Mathf.Max(1f, targetWidth - closeButtonTextPadding.x);
        Vector2 preferredValues = closeButtonLabel.GetPreferredValues(closeButtonLabel.text, wrappedTextWidth, 0f);

        float targetHeight = Mathf.Max(
            closeButtonMinSize.y,
            preferredValues.y + closeButtonTextPadding.y);

        closeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        closeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!ShouldUseSharedCloseGate() || changedProps == null || string.IsNullOrEmpty(comicClosePropertyKey))
            return;

        if (!changedProps.ContainsKey(comicClosePropertyKey))
            return;

        RefreshCloseGateUI();
        TryCompleteSharedCloseGate();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshCloseGateUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshCloseGateUI();

        TryCompleteSharedCloseGate();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (!ShouldUseSharedCloseGate() || propertiesThatChanged == null)
            return;

        if (!string.IsNullOrEmpty(comicCloseRoundPropertyKey) && propertiesThatChanged.ContainsKey(comicCloseRoundPropertyKey))
        {
            ResetLocalCloseState();

            if (pendingCloseClick)
            {
                pendingCloseClick = false;
                SetLocalPlayerCloseClicked(true);
            }

            RefreshCloseGateUI();
            TryCompleteSharedCloseGate();
        }

        if (!string.IsNullOrEmpty(comicClosedPropertyKey) &&
            propertiesThatChanged.TryGetValue(comicClosedPropertyKey, out object value) &&
            value is bool isClosed && isClosed)
        {
            CompleteClose();
        }
    }

    private bool HasAnyPlayerClickedClose()
    {
        if (!ShouldUseSharedCloseGate())
            return false;

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (!ShouldExpectPlayerClose(players[i]))
                continue;

            if (HasPlayerClickedClose(players[i]))
                return true;
        }

        return false;
    }
}
