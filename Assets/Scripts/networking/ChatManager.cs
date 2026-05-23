using Photon.Chat;
using ExitGames.Client.Photon;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ChatManager : MonoBehaviourPunCallbacks, IChatClientListener
{
    public static ChatManager Instance { get; private set; }

    private const string GlobalChannel = "Global";

    private ChatClient chatClient;

    [Header("UI References")]
    public TMP_InputField inputField;
    public Button sendButton;
    public Transform chatContent; // Content panel inside ScrollView
    public GameObject chatMessagePrefab; // Prefab with TMP_Text component
    public TMP_Dropdown channelDropdown;

    [Header("Settings")]
    private string userName;
    private string currentChannel;
    public string chatAppId = "41ecc5f0-2661-4f08-9c6b-3567bc7d495d";
    private bool isVisible = true;
    public GameObject chatPanel;
    [SerializeField] private Color messageTextColor = new Color(0.12f, 0.12f, 0.12f, 1f);

    public GameObject unreadBadge; // Badge panel with TMP_Text inside
    public TMP_Text unreadText;
    public Button collapseButton;
    private int unreadCount = 0;
    private readonly HashSet<string> subscribedChannels = new HashSet<string>();
    private bool hasStarted;
    private bool isProxy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.AbsorbSceneReferences(this);
            isProxy = true;
            enabled = false;
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        hasStarted = true;
        EnsureUi();
        SetupUiBindings();
        UpdateActiveChannel(true);
        ApplyLobbyDefaultVisibility();
        TryConnect();
    }

    void Update()
    {
        if (chatClient == null)
        {
            TryConnect();
        }

        chatClient?.Service();
    }

    public void ToggleChatPanel()
    {
        if (isProxy && Instance != null && Instance != this)
        {
            Instance.ToggleChatPanel();
            return;
        }

        EnsureUi();

        SetChatVisible(!isVisible);

        if (isVisible)
        {
            ResetUnreadCount();
        }
    }

    void SetupDropdown()
    {
        if (channelDropdown == null)
        {
            return;
        }

        channelDropdown.ClearOptions();
        channelDropdown.AddOptions(new List<string> { currentChannel });
        channelDropdown.SetValueWithoutNotify(0);
        channelDropdown.interactable = false;
    }

    void OnChannelChanged(int index)
    {
        UpdateActiveChannel(false);
    }

    public override void OnConnected()
    {
        Debug.Log("Connected to Photon Chat");
        SubscribeToCurrentChannel();
    }

    public void OnDisconnected() => Debug.Log("Disconnected from Photon Chat");

    public void OnChatStateChange(ChatState state) { }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        foreach (string channel in channels)
        {
            subscribedChannels.Add(channel);
            AddChatMessage($"<i><color=#5A5A5A>Joined {channel} chat</color></i>");
        }
    }

    public void OnUnsubscribed(string[] channels)
    {
        foreach (string channel in channels)
        {
            subscribedChannels.Remove(channel);
        }
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        if (channelName != currentChannel)
        {
            return;
        }

        for (int i = 0; i < senders.Length; i++)
        {
            AddChatMessage($"<b><color=#006D9C>[{channelName}]</color> <color=#007C73>{senders[i]}</color></b>: {messages[i]}");

            if (!isVisible)
            {
                unreadCount++;
                UpdateUnreadUI();
            }
        }
    }
    private void UpdateUnreadUI()
    {
        if (unreadBadge == null || unreadText == null)
        {
            return;
        }

        if (unreadCount > 0)
        {
            unreadBadge.SetActive(true);
            unreadText.text = unreadCount.ToString();
        }
        else
        {
            unreadBadge.SetActive(false);
        }
    }

    private void ResetUnreadCount()
    {
        unreadCount = 0;
        UpdateUnreadUI();
    }


    public void OnPrivateMessage(string sender, object message, string channelName) { }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }

    public void OnUserSubscribed(string channel, string user)
    {
        AddChatMessage($"<i><color=grey>{user} joined {channel}</color></i>");
    }

    public void OnUserUnsubscribed(string channel, string user)
    {
        AddChatMessage($"<i><color=grey>{user} left {channel}</color></i>");
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        Debug.Log($"Photon Chat Debug: {message}");
    }

    public void SendFromInputField()
    {
        if (isProxy && Instance != null && Instance != this)
        {
            Instance.SendFromInputField();
            return;
        }

        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text))
        {
            return;
        }

        if (chatClient == null || !chatClient.CanChat || string.IsNullOrEmpty(currentChannel))
        {
            AddChatMessage("<i><color=#5A5A5A>Chat is still connecting. Try again in a moment.</color></i>");
            return;
        }

        chatClient.PublishMessage(currentChannel, inputField.text.Trim());
        inputField.text = "";
        inputField.ActivateInputField();
    }

    private void AddChatMessage(string message)
    {
        EnsureUi();

        GameObject msgObj = chatMessagePrefab != null
            ? Instantiate(chatMessagePrefab, chatContent)
            : CreateFallbackMessageObject(chatContent);

        TMP_Text tmp = msgObj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.color = messageTextColor;
            tmp.text = message;
        }
    }

    public override void OnJoinedRoom()
    {
        UpdateActiveChannel(false);
    }

    public override void OnLeftRoom()
    {
        UpdateActiveChannel(false);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        subscribedChannels.Clear();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    public override void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        base.OnDisable();
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        EnsureUi();
        SetupUiBindings();
        UpdateActiveChannel(false);
        ApplyLobbyDefaultVisibility();
    }

    private void TryConnect()
    {
        if (chatClient != null || string.IsNullOrEmpty(chatAppId))
        {
            return;
        }

        string photonName = PhotonNetwork.NickName;
        if (string.IsNullOrWhiteSpace(photonName))
        {
            return;
        }

        userName = photonName;
        chatClient = new ChatClient(this);
        chatClient.Connect(chatAppId, "1.0", new Photon.Chat.AuthenticationValues(userName));
    }

    private void UpdateActiveChannel(bool force)
    {
        string nextChannel = GetActiveChannel();

        if (!force && currentChannel == nextChannel)
        {
            return;
        }

        string previousChannel = currentChannel;
        currentChannel = nextChannel;
        SetupDropdown();

        if (chatClient != null && chatClient.CanChat)
        {
            if (!string.IsNullOrEmpty(previousChannel) && previousChannel != currentChannel)
            {
                chatClient.Unsubscribe(new[] { previousChannel });
            }

            SubscribeToCurrentChannel();
        }
    }

    private void SubscribeToCurrentChannel()
    {
        if (chatClient == null || !chatClient.CanChat || string.IsNullOrEmpty(currentChannel) || subscribedChannels.Contains(currentChannel))
        {
            return;
        }

        chatClient.Subscribe(new[] { currentChannel });
    }

    private string GetActiveChannel()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return GlobalChannel;
        }

        bool isWaitingRoomCreator = PhotonNetwork.IsMasterClient && SceneManager.GetActiveScene().name == "Lobby";
        return isWaitingRoomCreator ? GlobalChannel : PhotonNetwork.CurrentRoom.Name;
    }

    private void SetupUiBindings()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SendFromInputField);
            sendButton.onClick.AddListener(SendFromInputField);
        }

        if (inputField != null)
        {
            EnsureInputFieldViewport();
            inputField.onSubmit.RemoveListener(SendSubmittedText);
            inputField.onSubmit.AddListener(SendSubmittedText);
        }

        if (channelDropdown != null)
        {
            channelDropdown.onValueChanged.RemoveListener(OnChannelChanged);
            channelDropdown.onValueChanged.AddListener(OnChannelChanged);
        }

        if (unreadBadge != null)
        {
            unreadBadge.SetActive(unreadCount > 0);
        }

        if (collapseButton != null)
        {
            collapseButton.onClick.RemoveListener(ToggleChatPanel);
            collapseButton.onClick.AddListener(ToggleChatPanel);
        }

        EnsureCollapsedChatButton();
        ApplyChatVisibility();
    }

    private void SendSubmittedText(string _)
    {
        SendFromInputField();
    }

    private void EnsureUi()
    {
        AutoWireMissingSceneReferences();

        if (inputField != null && sendButton != null && chatContent != null && chatPanel != null)
        {
            return;
        }

        Debug.LogWarning("ChatManager is missing scene UI references. Assign the manual chat box UI in the inspector.");
    }

    private void AutoWireMissingSceneReferences()
    {
        if (chatPanel == null)
        {
            Transform chatBox = FindSceneTransform("Chat Box");
            if (chatBox != null)
            {
                chatPanel = chatBox.gameObject;
            }
        }

        if (chatPanel == null)
        {
            return;
        }

        inputField ??= FindChildComponent<TMP_InputField>(chatPanel.transform, "Input Field Chat");
        sendButton ??= FindChildComponent<Button>(chatPanel.transform, "Send Button");
        channelDropdown ??= FindChildComponent<TMP_Dropdown>(chatPanel.transform, "Channel");
        collapseButton ??= FindChildComponent<Button>(chatPanel.transform, "Collapsible Button");

        if (unreadBadge == null)
        {
            Transform badge = FindChild(chatPanel.transform, "Notification Badge");
            unreadBadge = badge != null ? badge.gameObject : null;
        }

        unreadText ??= unreadBadge != null ? unreadBadge.GetComponentInChildren<TMP_Text>(true) : null;

        if (chatContent == null)
        {
            ScrollRect scrollRect = FindChildComponent<ScrollRect>(chatPanel.transform, "Scroll View");
            chatContent = scrollRect != null ? scrollRect.content : null;
        }
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindChild(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                return root.transform;
            }

            Transform child = FindChild(root.transform, objectName);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindChild(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }

    private void AbsorbSceneReferences(ChatManager sceneManager)
    {
        inputField = sceneManager.inputField ?? inputField;
        sendButton = sceneManager.sendButton ?? sendButton;
        chatContent = sceneManager.chatContent ?? chatContent;
        chatMessagePrefab = sceneManager.chatMessagePrefab ?? chatMessagePrefab;
        channelDropdown = sceneManager.channelDropdown ?? channelDropdown;
        chatPanel = sceneManager.chatPanel ?? chatPanel;
        unreadBadge = sceneManager.unreadBadge ?? unreadBadge;
        unreadText = sceneManager.unreadText ?? unreadText;
        collapseButton = sceneManager.collapseButton ?? collapseButton;

        if (hasStarted)
        {
            EnsureUi();
            SetupUiBindings();
            SetupDropdown();
            ApplyLobbyDefaultVisibility();
        }
    }

    private void ApplyLobbyDefaultVisibility()
    {
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            SetChatVisible(false);
        }
    }

    private void SetChatVisible(bool visible)
    {
        if (!visible && inputField != null)
        {
            inputField.DeactivateInputField();

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == inputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        isVisible = visible;
        ApplyChatVisibility();
    }

    private void EnsureInputFieldViewport()
    {
        if (inputField == null || inputField.textViewport != null)
        {
            return;
        }

        inputField.textViewport = inputField.GetComponent<RectTransform>();
    }

    private void ApplyChatVisibility()
    {
        if (chatPanel != null)
        {
            if (collapseButton != null && collapseButton.transform.IsChildOf(chatPanel.transform))
            {
                chatPanel.SetActive(true);
                Transform collapseTransform = collapseButton.transform;
                Transform unreadTransform = unreadBadge != null ? unreadBadge.transform : null;

                for (int i = 0; i < chatPanel.transform.childCount; i++)
                {
                    Transform child = chatPanel.transform.GetChild(i);
                    if (child == collapseTransform || (unreadTransform != null && child == unreadTransform))
                    {
                        continue;
                    }

                    child.gameObject.SetActive(isVisible);
                }
            }
            else
            {
                chatPanel.SetActive(isVisible);
            }
        }
    }

    private void EnsureCollapsedChatButton()
    {
        // The collapsed/open affordance is authored in each scene now.
    }

    private static GameObject CreateFallbackMessageObject(Transform parent)
    {
        GameObject messageObject = new GameObject("Chat Message");
        messageObject.transform.SetParent(parent, false);
        TMP_Text text = messageObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
        return messageObject;
    }

}
