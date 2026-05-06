using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class ClueEntry
{
    [Tooltip("Unique ID for this clue (e.g., 'clue_diary_page1')")]
    public string id;

    [Tooltip("Panel GameObject to toggle for this clue (child of mainRoot). Optional if using image detail view only.")]
    public GameObject panelRoot;
}

public class ClueUI : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static ClueUI Instance; // singleton for easy access from ClueSystem

    [Header("Main Root UI Panel (container for classic panels)")]
    [SerializeField] private GameObject mainRoot;

    [Header("Clue Panels (id → panel) - optional")]
    [SerializeField] private List<ClueEntry> clues = new();

    [Header("General Close Button (inside mainRoot)")]
    [SerializeField] private Button closeButton;

    [Header("Unread Badge (on your Clue button)")]
    [Tooltip("Red dot (or container) that shows the unread number.")]
    [SerializeField] private GameObject unreadBadgeRoot;
    [SerializeField] private TMP_Text unreadText; // number label

    [Header("Optional: extra badge panel to auto-close")]
    [Tooltip("If you have a small dropdown/panel tied to the badge, assign it so we can SetActive(false).")]
    [SerializeField] private GameObject badgePanelRoot;

    [Header("Detail Image View")]
    [Tooltip("Panel that contains a full-size Image (and its own Close button).")]
    [SerializeField] private GameObject detailPanelRoot;
    [SerializeField] private Image detailImage;
    [SerializeField] private Button detailCloseButton;
    [SerializeField] private GameObject[] comics;

    [Header("Multiplayer Close Gate")]
    [SerializeField] private bool requireAllPlayersToClickClose = true;
    [SerializeField] private string clueCloseId = "SharedClue";

    [Header("Close Button Auto Size")]
    [SerializeField] private Vector2 closeButtonTextPadding = new Vector2(48f, 20f);
    [SerializeField] private float closeButtonMaxWidth = 260f;

    private readonly Dictionary<string, GameObject> _panels = new();
    private GameObject _currentPanel;
    private Action _onClosed;
    private string _clueClosePropertyKey;
    private string _clueClosedPropertyKey;
    private string _clueCloseRoundPropertyKey;
    private string _clueRemainingPropertyKey;
    private TMP_Text _closeButtonLabel;
    private string _defaultCloseButtonText = "Close";
    private RectTransform _closeButtonRect;
    private Vector2 _closeButtonMinSize;
    private bool _pendingCloseClick;
    private bool _allowSharedCloseHide;
    private int _broadcastRemainingPlayers = -1;
    private const byte ClueCloseRemainingEventCode = 42;

    [Header("Image Layout")]
    [SerializeField] private Vector2 portraitSize = new Vector2(600, 900);
    [SerializeField] private Vector2 landscapeSize = new Vector2(900, 600);
    // unread counter
    private int _unreadCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Map IDs to panels, hide them all
        _panels.Clear();
        foreach (var c in clues)
        {
            if (c == null || string.IsNullOrWhiteSpace(c.id) || c.panelRoot == null) continue;

            if (_panels.ContainsKey(c.id))
                Debug.LogWarning($"Duplicate clue id '{c.id}' in ClueUI.");
            else
                _panels.Add(c.id, c.panelRoot);

            c.panelRoot.SetActive(false);
        }

        if (mainRoot != null) mainRoot.SetActive(false);

        if (closeButton != null)
        {
            _closeButtonLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
            _closeButtonRect = closeButton.GetComponent<RectTransform>();
            if (_closeButtonRect != null)
                _closeButtonMinSize = _closeButtonRect.rect.size;

            if (_closeButtonLabel != null)
                _defaultCloseButtonText = _closeButtonLabel.text;

            closeButton.onClick = new Button.ButtonClickedEvent();
            closeButton.onClick.AddListener(() => Hide(true));
        }

        if (detailPanelRoot != null)
            detailPanelRoot.SetActive(false);

        if (detailCloseButton != null)
            detailCloseButton.onClick.AddListener(HideImage);

        UpdateUnreadUI(); // ensure correct initial state
    }

    /// <summary>Show the panel for a clue by its id (classic per-clue panel flow).</summary>
    public bool ShowById(string id, Action onClosed = null)
    {
        if (string.IsNullOrWhiteSpace(id) || !_panels.TryGetValue(id, out var panel))
        {
            Debug.LogWarning($"ClueUI: No panel found for id '{id}'.");
            return false;
        }

        // Hide any currently visible panel first
        if (_currentPanel != null)
            _currentPanel.SetActive(false);

        _currentPanel = panel;
        _onClosed = onClosed;
        _pendingCloseClick = false;
        _allowSharedCloseHide = false;
        _broadcastRemainingPlayers = -1;
        EnsureCloseGateKeys(id);

        if (mainRoot != null) mainRoot.SetActive(true);
        _currentPanel.SetActive(true);
        RefreshCloseButtonState();
        BeginCloseGateRoundIfNeeded();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        // Reset unread when opened
        ResetUnreadCount();

        return true;
    }

    /// <summary>Hide the current clue panel + root. If userAction=true, invoke onClosed callback.</summary>
    public void Hide(bool userAction)
    {
        if (SlideshowUI.ShouldSuppressLegacyCloseHandlers())
            return;

        if (ShouldUseSharedCloseGate())
        {
            HandleSharedCloseClicked();
            return;
        }

        HideNow(userAction);
    }

    private void HideNow(bool userAction)
    {
        _allowSharedCloseHide = true;
        _pendingCloseClick = false;
        RestoreDefaultCloseButtonText();
        if (closeButton != null)
            closeButton.interactable = true;

        if (_currentPanel != null)
            _currentPanel.SetActive(false);

        if (mainRoot != null)
            mainRoot.SetActive(false);

        Time.timeScale = 1f;

        if (userAction && _onClosed != null)
        {
            var cb = _onClosed;
            _onClosed = null;
            cb.Invoke();
        }

        _currentPanel = null;

        // Reset unread
        ResetUnreadCount();
        // Force-close any optional badge panel
        if (badgePanelRoot != null && badgePanelRoot.activeSelf)
            badgePanelRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!ShouldUseSharedCloseGate() || _allowSharedCloseHide)
            return;

        if (mainRoot != null && !mainRoot.activeSelf)
            mainRoot.SetActive(true);

        if (_currentPanel != null && !_currentPanel.activeSelf)
            _currentPanel.SetActive(true);

        RefreshCloseButtonState();
    }

    private void HandleSharedCloseClicked()
    {
        int currentRound = GetCurrentCloseRound();
        if (currentRound <= 0)
        {
            _pendingCloseClick = true;
            RefreshCloseButtonState();
            return;
        }

        _pendingCloseClick = false;

        if (!HasLocalPlayerClickedClose())
        {
            SetLocalPlayerCloseClicked(true);
            _pendingCloseClick = true;
        }

        RefreshSharedCloseGateState();
    }

    private bool ShouldUseSharedCloseGate()
    {
        return requireAllPlayersToClickClose && PhotonNetwork.IsConnected && PhotonNetwork.InRoom && _currentPanel != null;
    }

    private void EnsureCloseGateKeys(string clueId)
    {
        string rawId = string.IsNullOrWhiteSpace(clueCloseId) ? "SharedClue" : clueCloseId.Trim();
        if (!string.IsNullOrWhiteSpace(clueId))
            rawId += "_" + clueId.Trim();

        char[] chars = rawId.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }

        _clueClosePropertyKey = "ClueClose_" + new string(chars);
        _clueClosedPropertyKey = _clueClosePropertyKey + "_Done";
        _clueCloseRoundPropertyKey = _clueClosePropertyKey + "_Round";
        _clueRemainingPropertyKey = _clueClosePropertyKey + "_Remaining";
    }

    private void BeginCloseGateRoundIfNeeded()
    {
        if (!ShouldUseSharedCloseGate() || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        int nextRound = GetCurrentCloseRound() + 1;

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            [_clueClosedPropertyKey] = false,
            [_clueCloseRoundPropertyKey] = nextRound,
            [_clueRemainingPropertyKey] = PhotonNetwork.PlayerList.Length
        });
    }

    private void RefreshCloseButtonState()
    {
        if (closeButton == null)
            return;

        if (!ShouldUseSharedCloseGate())
        {
            RestoreDefaultCloseButtonText();
            closeButton.interactable = true;
            return;
        }

        int currentRound = GetCurrentCloseRound();
        if (currentRound <= 0)
        {
            int setupRemainingPlayers = _broadcastRemainingPlayers >= 0
                ? _broadcastRemainingPlayers
                : PhotonNetwork.PlayerList.Length;

            SetCloseButtonText($"Waiting for ({setupRemainingPlayers} players to close)");
            closeButton.interactable = !_pendingCloseClick;
            return;
        }

        int remainingPlayers = GetDisplayedRemainingPlayersCount();
        bool localClicked = HasLocalPlayerClickedClose() || _pendingCloseClick;

        if (remainingPlayers <= 0)
        {
            SetCloseButtonText("Closing...");
            closeButton.interactable = false;
            return;
        }

        SetCloseButtonText($"Waiting for ({remainingPlayers} players to close)");

        closeButton.interactable = !localClicked;
    }

    private void SetCloseButtonText(string value)
    {
        if (_closeButtonLabel != null)
        {
            _closeButtonLabel.text = value;
            ResizeCloseButtonToText();
        }
    }

    private void RestoreDefaultCloseButtonText()
    {
        SetCloseButtonText(_defaultCloseButtonText);
    }

    private void ResizeCloseButtonToText()
    {
        if (_closeButtonRect == null || _closeButtonLabel == null)
            return;

        _closeButtonLabel.ForceMeshUpdate();

        float targetWidth = Mathf.Max(_closeButtonMinSize.x, closeButtonMaxWidth);
        float wrappedTextWidth = Mathf.Max(1f, targetWidth - closeButtonTextPadding.x);
        Vector2 preferredValues = _closeButtonLabel.GetPreferredValues(_closeButtonLabel.text, wrappedTextWidth, 0f);

        float targetHeight = Mathf.Max(
            _closeButtonMinSize.y,
            preferredValues.y + closeButtonTextPadding.y);

        _closeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        _closeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }

    private void TryCompleteSharedCloseGate()
    {
        if (!ShouldUseSharedCloseGate())
            return;

        if (GetRemainingPlayersCount() == 0 && HasAnyPlayerClickedClose())
            MarkClueClosedForEveryone();
    }

    private void MarkClueClosedForEveryone()
    {
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.CurrentRoom == null)
        {
            HideNow(true);
            return;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            [_clueClosedPropertyKey] = true
        });
    }

    private void SetLocalPlayerCloseClicked(bool clicked)
    {
        if (!ShouldUseSharedCloseGate() || PhotonNetwork.LocalPlayer == null || PhotonNetwork.CurrentRoom == null)
            return;

        int currentRound = GetCurrentCloseRound();
        int clickedRound = clicked && currentRound > 0 ? currentRound : -1;
        int remainingPlayers = CalculateRemainingPlayersCountWithLocalVote(clickedRound);

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            [BuildPlayerClosePropertyKey(PhotonNetwork.LocalPlayer.ActorNumber)] = clickedRound,
            [_clueRemainingPropertyKey] = remainingPlayers
        });

        BroadcastRemainingPlayers(remainingPlayers);
    }

    private bool HasLocalPlayerClickedClose()
    {
        return HasPlayerClickedClose(PhotonNetwork.LocalPlayer);
    }

    private bool HasPlayerClickedClose(Player player)
    {
        if (player == null || string.IsNullOrEmpty(_clueClosePropertyKey))
            return false;

        int currentRound = GetCurrentCloseRound();
        if (currentRound <= 0)
            return false;

        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(BuildPlayerClosePropertyKey(player.ActorNumber), out object value) &&
            value is int clickedRound)
        {
            return clickedRound == currentRound;
        }

        return false;
    }

    private string BuildPlayerClosePropertyKey(int actorNumber)
    {
        return $"{_clueClosePropertyKey}_{actorNumber}";
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

    private int GetDisplayedRemainingPlayersCount()
    {
        if (_broadcastRemainingPlayers >= 0)
            return _broadcastRemainingPlayers;

        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties != null &&
            !string.IsNullOrEmpty(_clueRemainingPropertyKey) &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(_clueRemainingPropertyKey, out object value) &&
            value is int remaining)
        {
            return remaining;
        }

        return GetRemainingPlayersCount();
    }

    private int CalculateRemainingPlayersCountWithLocalVote(int localClickedRound)
    {
        if (!ShouldUseSharedCloseGate())
            return 0;

        int currentRound = GetCurrentCloseRound();
        int remaining = 0;
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];
            bool clicked = player == PhotonNetwork.LocalPlayer
                ? localClickedRound == currentRound
                : HasPlayerClickedClose(player);

            if (!clicked)
                remaining++;
        }

        return remaining;
    }

    private void BroadcastRemainingPlayers(int remainingPlayers)
    {
        if (!PhotonNetwork.InRoom || string.IsNullOrEmpty(_clueClosePropertyKey))
            return;

        object[] content =
        {
            _clueClosePropertyKey,
            GetCurrentCloseRound(),
            remainingPlayers
        };

        PhotonNetwork.RaiseEvent(
            ClueCloseRemainingEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != ClueCloseRemainingEventCode)
            return;

        if (!ShouldUseSharedCloseGate() || !(photonEvent.CustomData is object[] content) || content.Length < 3)
            return;

        string closeKey = content[0] as string;
        if (closeKey != _clueClosePropertyKey)
            return;

        if (!(content[1] is int round))
            return;

        if (!(content[2] is int remainingPlayers))
            return;

        int currentRound = GetCurrentCloseRound();
        if (currentRound > 0 && round != currentRound)
            return;

        _broadcastRemainingPlayers = remainingPlayers;

        if (HasLocalPlayerClickedClose())
            _pendingCloseClick = false;

        RefreshSharedCloseGateState();
    }

    private int GetCurrentCloseRound()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || string.IsNullOrEmpty(_clueCloseRoundPropertyKey))
            return 0;

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(_clueCloseRoundPropertyKey, out object value) &&
            value is int round)
        {
            return round;
        }

        return 0;
    }

    private bool HasAnyPlayerClickedClose()
    {
        if (!ShouldUseSharedCloseGate())
            return false;

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (HasPlayerClickedClose(players[i]))
                return true;
        }

        return false;
    }

    private void ResetLocalCloseState()
    {
        if (!ShouldUseSharedCloseGate())
            return;

        SetLocalPlayerCloseClicked(false);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!ShouldUseSharedCloseGate() || changedProps == null || string.IsNullOrEmpty(_clueClosePropertyKey))
            return;

        if (!changedProps.ContainsKey(_clueClosePropertyKey))
            return;

        if (targetPlayer == PhotonNetwork.LocalPlayer && HasLocalPlayerClickedClose())
            _pendingCloseClick = false;

        RefreshSharedCloseGateState();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshSharedCloseGateState();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshSharedCloseGateState();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!ShouldUseSharedCloseGate() || propertiesThatChanged == null)
            return;

        bool closeVoteChanged = !string.IsNullOrEmpty(_clueRemainingPropertyKey) &&
                                propertiesThatChanged.ContainsKey(_clueRemainingPropertyKey);
        foreach (object key in propertiesThatChanged.Keys)
        {
            if (key is string keyString && keyString.StartsWith(_clueClosePropertyKey + "_"))
            {
                closeVoteChanged = true;
                break;
            }
        }

        if (closeVoteChanged)
        {
            if (!string.IsNullOrEmpty(_clueRemainingPropertyKey) &&
                PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.CustomProperties != null &&
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(_clueRemainingPropertyKey, out object remainingValue) &&
                remainingValue is int remainingPlayers)
            {
                _broadcastRemainingPlayers = remainingPlayers;
            }

            if (HasLocalPlayerClickedClose())
                _pendingCloseClick = false;

            RefreshSharedCloseGateState();
        }

        if (!string.IsNullOrEmpty(_clueCloseRoundPropertyKey) && propertiesThatChanged.ContainsKey(_clueCloseRoundPropertyKey))
        {
            ResetLocalCloseState();

            if (_pendingCloseClick)
            {
                _pendingCloseClick = false;
                SetLocalPlayerCloseClicked(true);
                _pendingCloseClick = true;
            }

            RefreshSharedCloseGateState();
        }

        if (!string.IsNullOrEmpty(_clueClosedPropertyKey) &&
            propertiesThatChanged.TryGetValue(_clueClosedPropertyKey, out object value) &&
            value is bool isClosed && isClosed)
        {
            HideNow(true);
        }
    }

    private void RefreshSharedCloseGateState()
    {
        RefreshCloseButtonState();
        TryCompleteSharedCloseGate();
    }

    public bool HasClueId(string id) =>
        !string.IsNullOrWhiteSpace(id) && _panels.ContainsKey(id);

    // ---------- Image Detail View API ----------

    /// <summary>Show a full-size image in the detail panel (called by ClueSystem on tile click).</summary>
    public void ShowImage(Sprite sprite)
    {
        if (sprite == null || detailImage == null || detailPanelRoot == null)
        {
            Debug.LogWarning("[ClueUI] Missing detail image/panel or sprite.");
            return;
        }
        detailImage.gameObject.SetActive(true);
        detailImage.sprite = sprite;
        RectTransform rt = detailImage.rectTransform;

        float spriteAspect = sprite.rect.width / sprite.rect.height;

        float targetHeight = rt.sizeDelta.y;
        float targetWidth = targetHeight * spriteAspect;

        bool isPortrait = false;
        // Auto-detect from sprite pixel rect
        isPortrait = sprite.rect.height > sprite.rect.width;

        rt.sizeDelta = isPortrait ? portraitSize : landscapeSize;



        detailPanelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        ResetUnreadCount();
    }

    public void HideImage()
    {
        if (SlideshowUI.ShouldSuppressLegacyCloseHandlers())
            return;

        if (detailPanelRoot != null)
            detailPanelRoot.SetActive(false);
        foreach (var go in comics)
        {
            go.SetActive(false);
        }
        detailImage.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    // ---------- Unread badge API (called by ClueSystem) ----------

    public void IncrementUnread()
    {
        _unreadCount++;
        UpdateUnreadUI();
    }

    private void ResetUnreadCount()
    {
        _unreadCount = 0;
        UpdateUnreadUI();
    }

    private void UpdateUnreadUI()
    {
        bool show = _unreadCount > 0;

        if (unreadBadgeRoot != null)
            unreadBadgeRoot.SetActive(show);

        if (unreadText != null)
            unreadText.text = _unreadCount.ToString();
    }
}
