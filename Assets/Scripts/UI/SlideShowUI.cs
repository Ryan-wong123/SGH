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

    private Coroutine autoPlayRoutine;
    private TMP_Text closeButtonLabel;
    private string defaultCloseButtonText = "Close";
    private string comicClosePropertyKey;
    private bool closeListenerRegistered;
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
        closeButton.gameObject.SetActive(false);

        // Hook up buttons
        prevButton.onClick.AddListener(ShowPrevImage);
        nextButton.onClick.AddListener(ShowNextImage);

        // Initial button state
        UpdateButtonState();
    }

    private void EnsureCloseButtonSetup()
    {
        if (closeButton == null)
            return;

        closeButtonLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
        if (closeButtonLabel != null && string.IsNullOrWhiteSpace(defaultCloseButtonText))
            defaultCloseButtonText = closeButtonLabel.text;
        else if (closeButtonLabel != null)
            defaultCloseButtonText = closeButtonLabel.text;

        comicClosePropertyKey = BuildComicClosePropertyKey();

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

        if (!HasLocalPlayerClickedClose())
        {
            SetLocalPlayerCloseClicked(true);
        }

        RefreshCloseGateUI();

        if (GetRemainingPlayersCount() == 0)
        {
            CompleteClose();
        }
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

        int remainingPlayers = GetRemainingPlayersCount();
        bool localClicked = HasLocalPlayerClickedClose();

        if (remainingPlayers <= 0)
        {
            SetCloseButtonText(defaultCloseButtonText);
            closeButton.interactable = true;
            return;
        }

        SetCloseButtonText(localClicked
            ? $"Waiting ({remainingPlayers} left)"
            : $"Close ({remainingPlayers} left)");

        closeButton.interactable = !localClicked;
    }

    private void CompleteClose()
    {
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
    }

    private void SetLocalPlayerCloseClicked(bool clicked)
    {
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.LocalPlayer == null)
            return;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            [comicClosePropertyKey] = clicked
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

        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(comicClosePropertyKey, out object value) &&
            value is bool clicked)
        {
            return clicked;
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
            if (!HasPlayerClickedClose(players[i]))
                remaining++;
        }

        return remaining;
    }

    private string BuildComicClosePropertyKey()
    {
        string rawId = string.IsNullOrWhiteSpace(comicId) ? gameObject.name : comicId;
        rawId = rawId.Trim();

        char[] chars = rawId.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }

        return "ComicClose_" + new string(chars);
    }

    private void SetCloseButtonText(string value)
    {
        if (closeButtonLabel != null)
            closeButtonLabel.text = value;
    }

    private void RestoreDefaultCloseButtonText()
    {
        if (closeButtonLabel != null)
            closeButtonLabel.text = defaultCloseButtonText;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (!ShouldUseSharedCloseGate() || changedProps == null || string.IsNullOrEmpty(comicClosePropertyKey))
            return;

        if (!changedProps.ContainsKey(comicClosePropertyKey))
            return;

        RefreshCloseGateUI();

        if (HasLocalPlayerClickedClose() && GetRemainingPlayersCount() == 0)
            CompleteClose();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshCloseGateUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshCloseGateUI();

        if (ShouldUseSharedCloseGate() && HasLocalPlayerClickedClose() && GetRemainingPlayersCount() == 0)
            CompleteClose();
    }
}
