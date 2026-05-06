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

    public GameObject unreadBadge; // Badge panel with TMP_Text inside
    public TMP_Text unreadText;
    private int unreadCount = 0;
    private GameObject fallbackCanvas;
    private TMP_Text fallbackChannelText;
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

        isVisible = !isVisible;
        chatPanel.SetActive(isVisible);

        if (isVisible)
        {
            ResetUnreadCount();
        }
    }

    void SetupDropdown()
    {
        if (channelDropdown == null)
        {
            if (fallbackChannelText != null)
            {
                fallbackChannelText.text = currentChannel;
            }

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
            AddChatMessage($"<i><color=grey>Joined {channel} chat</color></i>");
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
            AddChatMessage($"<b><color=#00ffff>[{channelName}]</color> <color=#00ffcc>{senders[i]}</color></b>: {messages[i]}");

            if (!isVisible)
            {
                unreadCount++;
                UpdateUnreadUI();
            }
        }
    }
    private void UpdateUnreadUI()
    {
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
            AddChatMessage("<i><color=grey>Chat is still connecting. Try again in a moment.</color></i>");
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
        UpdateActiveChannel(false);
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
    }

    private void SendSubmittedText(string _)
    {
        SendFromInputField();
    }

    private void EnsureUi()
    {
        if (inputField != null && sendButton != null && chatContent != null && chatPanel != null)
        {
            if (fallbackCanvas != null && !IsUsingFallbackUi())
            {
                Destroy(fallbackCanvas);
                fallbackCanvas = null;
                fallbackChannelText = null;
            }

            return;
        }

        if (fallbackCanvas != null)
        {
            return;
        }

        CreateFallbackUi();
    }

    private bool IsUsingFallbackUi()
    {
        if (fallbackCanvas == null)
        {
            return false;
        }

        Transform fallbackTransform = fallbackCanvas.transform;
        return (inputField != null && inputField.transform.IsChildOf(fallbackTransform))
            || (sendButton != null && sendButton.transform.IsChildOf(fallbackTransform))
            || (chatContent != null && chatContent.IsChildOf(fallbackTransform))
            || (chatPanel != null && chatPanel.transform.IsChildOf(fallbackTransform));
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

        if (hasStarted)
        {
            EnsureUi();
            SetupUiBindings();
            SetupDropdown();
        }
    }

    private void CreateFallbackUi()
    {
        EnsureEventSystem();

        fallbackCanvas = new GameObject("Runtime Chat Canvas");
        DontDestroyOnLoad(fallbackCanvas);

        Canvas canvas = fallbackCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        fallbackCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        fallbackCanvas.AddComponent<GraphicRaycaster>();

        chatPanel = CreateRect("Chat Panel", fallbackCanvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(260f, 180f), new Vector2(140f, 110f));
        Image panelImage = chatPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject channelLabel = CreateText("Channel Label", chatPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-20f, 24f), new Vector2(0f, -14f), 16);
        fallbackChannelText = channelLabel.GetComponent<TMP_Text>();
        fallbackChannelText.alignment = TextAlignmentOptions.Center;

        GameObject viewport = CreateRect("Messages", chatPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-16f, -76f), new Vector2(0f, 18f));
        RectMask2D mask = viewport.AddComponent<RectMask2D>();
        mask.padding = Vector4.zero;

        GameObject content = CreateRect("Content", viewport.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 4f;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        chatContent = content.transform;

        GameObject input = CreateRect("Chat Input", chatPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-72f, 28f), new Vector2(-36f, 8f));
        Image inputImage = input.AddComponent<Image>();
        inputImage.color = Color.white;
        inputField = input.AddComponent<TMP_InputField>();
        TMP_Text inputText = CreateText("Text", input.transform, Vector2.zero, Vector2.one, new Vector2(-16f, -8f), Vector2.zero, 14).GetComponent<TMP_Text>();
        TMP_Text placeholder = CreateText("Placeholder", input.transform, Vector2.zero, Vector2.one, new Vector2(-16f, -8f), Vector2.zero, 14).GetComponent<TMP_Text>();
        placeholder.text = "Type message...";
        placeholder.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;

        GameObject send = CreateRect("Send Button", chatPanel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(56f, 28f), new Vector2(-34f, 8f));
        Image sendImage = send.AddComponent<Image>();
        sendImage.color = new Color(0.1f, 0.55f, 0.7f, 1f);
        sendButton = send.AddComponent<Button>();
        TMP_Text sendText = CreateText("Text", send.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 14).GetComponent<TMP_Text>();
        sendText.text = "Send";
        sendText.alignment = TextAlignmentOptions.Center;

        unreadBadge = CreateRect("Unread Badge", fallbackCanvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(276f, 202f));
        Image badgeImage = unreadBadge.AddComponent<Image>();
        badgeImage.color = new Color(0.85f, 0.1f, 0.1f, 1f);
        unreadText = CreateText("Text", unreadBadge.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 14).GetComponent<TMP_Text>();
        unreadText.alignment = TextAlignmentOptions.Center;
        unreadBadge.SetActive(false);
    }

    private static GameObject CreateFallbackMessageObject(Transform parent)
    {
        GameObject messageObject = new GameObject("Chat Message");
        messageObject.transform.SetParent(parent, false);
        TMP_Text text = messageObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        return messageObject;
    }

    private static GameObject CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, float fontSize)
    {
        GameObject textObject = CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, anchoredPosition);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        return textObject;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        GameObject rectObject = new GameObject(name);
        rectObject.transform.SetParent(parent, false);
        RectTransform rect = rectObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        return rectObject;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        DontDestroyOnLoad(eventSystemObject);
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
