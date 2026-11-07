using Photon.Chat; // Imports a namespace used by this script.
using ExitGames.Client.Photon; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.
using Photon.Realtime; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using UnityEngine.SceneManagement; // Imports a namespace used by this script.

public class ChatManager : MonoBehaviourPunCallbacks, IChatClientListener // Declares the Photon-backed chat manager component.
{ // Starts this block.
    public static ChatManager Instance { get; private set; } // Stores the shared ChatManager singleton instance.

    private const string GlobalChannel = "Global"; // Defines the fallback channel used outside active rooms.

    private ChatClient chatClient; // Holds the Photon Chat client connection.

    [Header("UI References")] // Groups related inspector fields in Unity.
    public TMP_InputField inputField; // References the chat text input field.
    public Button sendButton; // References the button that sends typed chat text.
    public Transform chatContent; // Content panel inside ScrollView // References the scroll-view content that receives message rows.
    public GameObject chatMessagePrefab; // Prefab with TMP_Text component // References the optional prefab used for new chat messages.
    public TMP_Dropdown channelDropdown; // References the dropdown that displays the active channel.

    [Header("Settings")] // Groups related inspector fields in Unity.
    private string userName; // Stores the Photon nickname used for chat authentication.
    private string currentChannel; // Tracks the channel currently selected for messages.
    public string chatAppId = "41ecc5f0-2661-4f08-9c6b-3567bc7d495d"; // Stores the Photon Chat AppId used to connect.
    private bool isVisible = true; // Tracks whether the expanded chat UI is currently visible.
    public GameObject chatPanel; // References the root chat UI panel.
    [SerializeField] private Color messageTextColor = new Color(0.12f, 0.12f, 0.12f, 1f); // Exposes this private setting in the Unity inspector.

    public GameObject unreadBadge; // Badge panel with TMP_Text inside // References the unread-message badge object.
    public TMP_Text unreadText; // References the unread count label.
    public Button collapseButton; // References the chat expand/collapse button.
    private int unreadCount = 0; // Stores how many messages arrived while chat was hidden.
    private readonly HashSet<string> subscribedChannels = new HashSet<string>(); // Remembers channels already subscribed to.
    private bool hasStarted; // Tracks whether Start has completed.
    private bool isProxy; // Marks duplicate scene instances that delegate to the singleton.

    private void Awake() // Runs Unity initialization before Start.
    { // Starts this block.
        if (Instance != null && Instance != this) // Checks whether this condition requires special handling.
        { // Starts this block.
            Instance.AbsorbSceneReferences(this); // Delegates this call to the singleton instance.
            isProxy = true; // Marks this duplicate as a proxy object.
            enabled = false; // Disables the duplicate proxy component.
            return; // Leaves the current method early.
        } // Ends this block.

        // GameScene puts ChatManager on the same object as scene-owned gameplay
        // managers. Persist only Chat, never that entire object and stale scene references.
        if (GetComponents<MonoBehaviour>().Length > 1)
        {
            var host = new GameObject(nameof(ChatManager));
            var persistent = host.AddComponent<ChatManager>();
            persistent.chatAppId = chatAppId;
            persistent.AbsorbSceneReferences(this);
            isProxy = true;
            enabled = false;
            return;
        }

        Instance = this; // Registers this component as the singleton instance.
        DontDestroyOnLoad(gameObject); // Keeps this manager alive when Unity changes scenes.
    } // Ends this block.

    void Start() // Runs Unity setup when the component starts.
    { // Starts this block.
        hasStarted = true; // Records that runtime setup has begun.
        EnsureUi(); // Ensures chat UI references are ready before use.
        SetupUiBindings(); // Refreshes the button and input event bindings.
        UpdateActiveChannel(true); // Refreshes which chat channel should be active.
        ApplyLobbyDefaultVisibility(); // Applies the lobby visibility rule.
        TryConnect(); // Attempts to connect once prerequisites are present.
    } // Ends this block.

    void Update() // Runs once per frame to keep chat serviced.
    { // Starts this block.
        if (chatClient == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            TryConnect(); // Attempts to connect once prerequisites are present.
        } // Ends this block.

        chatClient?.Service(); // Lets Photon Chat process network events this frame.
    } // Ends this block.

    public void ToggleChatPanel() // Toggles the expanded chat panel state.
    { // Starts this block.
        if (isProxy && Instance != null && Instance != this) // Checks whether this condition requires special handling.
        { // Starts this block.
            Instance.ToggleChatPanel(); // Delegates this call to the singleton instance.
            return; // Leaves the current method early.
        } // Ends this block.

        EnsureUi(); // Ensures chat UI references are ready before use.

        SetChatVisible(!isVisible); // Changes the chat panel visibility state.

        if (isVisible) // Checks whether this condition requires special handling.
        { // Starts this block.
            ResetUnreadCount(); // Clears hidden-message notifications.
        } // Ends this block.
    } // Ends this block.

    void SetupDropdown() // Configures the channel dropdown UI.
    { // Starts this block.
        if (channelDropdown == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        channelDropdown.ClearOptions(); // Updates the channel dropdown control.
        channelDropdown.AddOptions(new List<string> { currentChannel }); // Updates the channel dropdown control.
        channelDropdown.SetValueWithoutNotify(0); // Updates the channel dropdown control.
        channelDropdown.interactable = true; // Updates the channel dropdown control.
    } // Ends this block.

    void OnChannelChanged(int index) // Handles channel dropdown changes.
    { // Starts this block.
        UpdateActiveChannel(false); // Refreshes which chat channel should be active.
    } // Ends this block.

    public override void OnConnected() // Handles the Photon Realtime connection callback.
    { // Starts this block.
        Debug.Log("Connected to Photon Chat"); // Writes diagnostic information to the Unity console.
        SubscribeToCurrentChannel(); // Ensures the active channel is subscribed.
    } // Ends this block.

    public void OnDisconnected() => Debug.Log("Disconnected from Photon Chat"); // Logs when Photon Chat disconnects.

    public void OnChatStateChange(ChatState state) { } // Receives Photon Chat state changes.

    public void OnSubscribed(string[] channels, bool[] results) // Handles successful channel subscriptions.
    { // Starts this block.
        foreach (string channel in channels) // Loops through each item in this collection.
        { // Starts this block.
            subscribedChannels.Add(channel); // Updates the set of subscribed channels.
            AddChatMessage($"<i><color=#5A5A5A>Joined {channel} chat</color></i>"); // Adds a formatted status or chat line to the chat UI.
        } // Ends this block.
    } // Ends this block.

    public void OnUnsubscribed(string[] channels) // Handles channel unsubscribe notifications.
    { // Starts this block.
        foreach (string channel in channels) // Loops through each item in this collection.
        { // Starts this block.
            subscribedChannels.Remove(channel); // Updates the set of subscribed channels.
        } // Ends this block.
    } // Ends this block.

    public void OnGetMessages(string channelName, string[] senders, object[] messages) // Handles incoming public chat messages.
    { // Starts this block.
        if (channelName != currentChannel) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        for (int i = 0; i < senders.Length; i++) // Loops through indexed items in this collection.
        { // Starts this block.
            AddChatMessage($"<b><color=#006D9C>[{channelName}]</color> <color=#007C73>{senders[i]}</color></b>: {messages[i]}"); // Adds a formatted status or chat line to the chat UI.

            if (!isVisible) // Checks whether this condition requires special handling.
            { // Starts this block.
                unreadCount++; // Counts this hidden incoming message as unread.
                UpdateUnreadUI(); // Refreshes the unread badge UI.
            } // Ends this block.
        } // Ends this block.
    } // Ends this block.
    private void UpdateUnreadUI() // Refreshes the unread badge display.
    { // Starts this block.
        if (unreadBadge == null || unreadText == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        if (unreadCount > 0) // Checks whether this condition requires special handling.
        { // Starts this block.
            unreadBadge.SetActive(true); // Shows or hides the unread badge.
            unreadText.text = unreadCount.ToString(); // Displays the unread count as text.
        } // Ends this block.
        else // Runs when the previous condition is false.
        { // Starts this block.
            unreadBadge.SetActive(false); // Shows or hides the unread badge.
        } // Ends this block.
    } // Ends this block.

    private void ResetUnreadCount() // Clears unread-message state.
    { // Starts this block.
        unreadCount = 0; // Resets the hidden-message counter.
        UpdateUnreadUI(); // Refreshes the unread badge UI.
    } // Ends this block.


    public void OnPrivateMessage(string sender, object message, string channelName) { } // Receives private chat messages if enabled later.

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { } // Receives Photon Chat user status updates.

    public void OnUserSubscribed(string channel, string user) // Handles another user joining a channel.
    { // Starts this block.
        AddChatMessage($"<i><color=grey>{user} joined {channel}</color></i>"); // Adds a formatted status or chat line to the chat UI.
    } // Ends this block.

    public void OnUserUnsubscribed(string channel, string user) // Handles another user leaving a channel.
    { // Starts this block.
        AddChatMessage($"<i><color=grey>{user} left {channel}</color></i>"); // Adds a formatted status or chat line to the chat UI.
    } // Ends this block.

    public void DebugReturn(DebugLevel level, string message) // Receives Photon Chat debug output.
    { // Starts this block.
        Debug.Log($"Photon Chat Debug: {message}"); // Writes diagnostic information to the Unity console.
    } // Ends this block.

    public void SendFromInputField() // Sends the current input field text to chat.
    { // Starts this block.
        if (isProxy && Instance != null && Instance != this) // Checks whether this condition requires special handling.
        { // Starts this block.
            Instance.SendFromInputField(); // Delegates this call to the singleton instance.
            return; // Leaves the current method early.
        } // Ends this block.

        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        if (chatClient == null || !chatClient.CanChat || string.IsNullOrEmpty(currentChannel)) // Checks whether this condition requires special handling.
        { // Starts this block.
            AddChatMessage("<i><color=#5A5A5A>Chat is still connecting. Try again in a moment.</color></i>"); // Adds a formatted status or chat line to the chat UI.
            return; // Leaves the current method early.
        } // Ends this block.

        chatClient.PublishMessage(currentChannel, inputField.text.Trim()); // Sends the trimmed input text to Photon Chat.
        inputField.text = ""; // Updates the chat input text.
        inputField.ActivateInputField(); // Returns keyboard focus to the chat input.
    } // Ends this block.

    private void AddChatMessage(string message) // Creates and fills a UI row for a chat message.
    { // Starts this block.
        EnsureUi(); // Ensures chat UI references are ready before use.

        GameObject msgObj = chatMessagePrefab != null // Creates or stores a GameObject reference.
            ? Instantiate(chatMessagePrefab, chatContent) // Uses the assigned prefab when one exists.
            : CreateFallbackMessageObject(chatContent); // Falls back to a generated text row when no prefab exists.

        TMP_Text tmp = msgObj.GetComponent<TMP_Text>(); // Creates or stores a TextMeshPro text reference.
        if (tmp != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            tmp.color = messageTextColor; // Configures the TextMeshPro message display.
            tmp.text = message; // Configures the TextMeshPro message display.
        } // Ends this block.
    } // Ends this block.

    public override void OnJoinedRoom() // Handles joining a Photon room.
    { // Starts this block.
        UpdateActiveChannel(false); // Refreshes which chat channel should be active.
    } // Ends this block.

    public override void OnLeftRoom() // Handles leaving a Photon room.
    { // Starts this block.
        UpdateActiveChannel(false); // Refreshes which chat channel should be active.
    } // Ends this block.

    public override void OnDisconnected(DisconnectCause cause) // Handles Photon Realtime disconnection.
    { // Starts this block.
        subscribedChannels.Clear(); // Updates the set of subscribed channels.
    } // Ends this block.

    public override void OnEnable() // Subscribes to Unity events when enabled.
    { // Starts this block.
        base.OnEnable(); // Runs the base Photon enable behavior.
        SceneManager.activeSceneChanged += OnActiveSceneChanged; // Updates the active-scene-changed event subscription.
    } // Ends this block.

    public override void OnDisable() // Unsubscribes from Unity events when disabled.
    { // Starts this block.
        SceneManager.activeSceneChanged -= OnActiveSceneChanged; // Updates the active-scene-changed event subscription.
        base.OnDisable(); // Runs the base Photon disable behavior.
    } // Ends this block.

    private void OnActiveSceneChanged(Scene previousScene, Scene nextScene) // Refreshes chat wiring after a scene switch.
    { // Starts this block.
        EnsureUi(); // Ensures chat UI references are ready before use.
        SetupUiBindings(); // Refreshes the button and input event bindings.
        UpdateActiveChannel(false); // Refreshes which chat channel should be active.
        ApplyLobbyDefaultVisibility(); // Applies the lobby visibility rule.
    } // Ends this block.

    private void TryConnect() // Attempts to create and connect the Photon Chat client.
    { // Starts this block.
        if (chatClient != null || string.IsNullOrEmpty(chatAppId)) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        string photonName = PhotonNetwork.NickName; // Reads the Photon nickname for chat login.
        if (string.IsNullOrWhiteSpace(photonName)) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        userName = photonName; // Stores the chat username.
        chatClient = new ChatClient(this); // Creates the Photon Chat client.
        chatClient.Connect(chatAppId, "1.0", new Photon.Chat.AuthenticationValues(userName)); // Starts the Photon Chat connection.
    } // Ends this block.

    private void UpdateActiveChannel(bool force) // Chooses and subscribes to the correct active channel.
    { // Starts this block.
        string nextChannel = GetActiveChannel(); // Computes the next channel name.

        if (!force && currentChannel == nextChannel) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        string previousChannel = currentChannel; // Remembers the old channel before switching.
        currentChannel = nextChannel; // Stores the newly selected channel.
        SetupDropdown(); // Refreshes the dropdown after the active channel changes.

        if (chatClient != null && chatClient.CanChat) // Checks whether this condition requires special handling.
        { // Starts this block.
            if (!string.IsNullOrEmpty(previousChannel) && previousChannel != currentChannel) // Checks whether this condition requires special handling.
            { // Starts this block.
                chatClient.Unsubscribe(new[] { previousChannel }); // Leaves the previous Photon Chat channel.
            } // Ends this block.

            SubscribeToCurrentChannel(); // Ensures the active channel is subscribed.
        } // Ends this block.
    } // Ends this block.

    private void SubscribeToCurrentChannel() // Subscribes Photon Chat to the current channel.
    { // Starts this block.
        if (chatClient == null || !chatClient.CanChat || string.IsNullOrEmpty(currentChannel) || subscribedChannels.Contains(currentChannel)) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        chatClient.Subscribe(new[] { currentChannel }); // Subscribes to the active Photon Chat channel.
    } // Ends this block.

    private string GetActiveChannel() // Calculates which channel should be active.
    { // Starts this block.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            return GlobalChannel; // Returns the computed value to the caller.
        } // Ends this block.

        bool isWaitingRoomCreator = PhotonNetwork.IsMasterClient && SceneManager.GetActiveScene().name == "Lobby"; // Detects lobby hosts that should stay in global chat.
        return isWaitingRoomCreator ? GlobalChannel : PhotonNetwork.CurrentRoom.Name; // Returns the computed value to the caller.
    } // Ends this block.

    private void SetupUiBindings() // Connects UI controls to ChatManager methods.
    { // Starts this block.
        if (sendButton != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            sendButton.onClick.RemoveListener(SendFromInputField); // Updates the send button click listener.
            sendButton.onClick.AddListener(SendFromInputField); // Updates the send button click listener.
        } // Ends this block.

        if (inputField != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            EnsureInputFieldViewport(); // Prepares the TMP input viewport.
            inputField.onSubmit.RemoveListener(SendSubmittedText); // Updates the input submit listener.
            inputField.onSubmit.AddListener(SendSubmittedText); // Updates the input submit listener.
        } // Ends this block.

        if (channelDropdown != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            channelDropdown.onValueChanged.RemoveListener(OnChannelChanged); // Updates the channel dropdown control.
            channelDropdown.onValueChanged.AddListener(OnChannelChanged); // Updates the channel dropdown control.
        } // Ends this block.

        if (unreadBadge != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            unreadBadge.SetActive(unreadCount > 0); // Shows or hides the unread badge.
        } // Ends this block.

        if (collapseButton != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            collapseButton.onClick.RemoveListener(ToggleChatPanel); // Prevents duplicate collapse click handlers.
            collapseButton.onClick.AddListener(ToggleChatPanel); // Connects the collapse button to the toggle method.
        } // Ends this block.

        EnsureCollapsedChatButton(); // Ensures collapse-button setup stays available.
        ApplyChatVisibility(); // Applies the current chat visibility to the scene.
    } // Ends this block.

    private void SendSubmittedText(string _) // Routes input-submit events through the send method.
    { // Starts this block.
        SendFromInputField(); // Sends submitted input through the same button path.
    } // Ends this block.

    private void EnsureUi() // Makes sure required chat UI references are available.
    { // Starts this block.
        AutoWireMissingSceneReferences(); // Attempts automatic UI lookup in the active scene.

        if (inputField != null && sendButton != null && chatContent != null && chatPanel != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        Debug.LogWarning("ChatManager is missing scene UI references. Assign the manual chat box UI in the inspector."); // Writes a warning to the Unity console.
    } // Ends this block.

    private void AutoWireMissingSceneReferences() // Finds missing scene UI references by name.
    { // Starts this block.
        if (chatPanel == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            Transform chatBox = FindSceneTransform("Chat Box"); // Creates or stores a Transform reference.
            if (chatBox != null) // Checks whether this condition requires special handling.
            { // Starts this block.
                chatPanel = chatBox.gameObject; // Stores the chat panel GameObject.
            } // Ends this block.
        } // Ends this block.

        if (chatPanel == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        inputField ??= FindChildComponent<TMP_InputField>(chatPanel.transform, "Input Field Chat"); // Auto-fills the chat input reference if missing.
        sendButton ??= FindChildComponent<Button>(chatPanel.transform, "Send Button"); // Auto-fills the send button reference if missing.
        channelDropdown ??= FindChildComponent<TMP_Dropdown>(chatPanel.transform, "Channel"); // Auto-fills the channel dropdown reference if missing.
        collapseButton ??= FindChildComponent<Button>(chatPanel.transform, "Collapsible Button"); // Auto-fills the collapse button reference if missing.

        if (unreadBadge == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            Transform badge = FindChild(chatPanel.transform, "Notification Badge"); // Creates or stores a Transform reference.
            unreadBadge = badge != null ? badge.gameObject : null; // Stores the unread badge GameObject.
        } // Ends this block.

        unreadText ??= unreadBadge != null ? unreadBadge.GetComponentInChildren<TMP_Text>(true) : null; // Auto-fills the unread text reference if missing.

        if (chatContent == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            ScrollRect scrollRect = FindChildComponent<ScrollRect>(chatPanel.transform, "Scroll View"); // Creates or stores a ScrollRect reference.
            chatContent = scrollRect != null ? scrollRect.content : null; // Stores the message content transform.
        } // Ends this block.
    } // Ends this block.

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component // Finds a named child and returns its requested component.
    { // Starts this block.
        Transform child = FindChild(root, childName); // Creates or stores a Transform reference.
        return child != null ? child.GetComponent<T>() : null; // Returns the computed value to the caller.
    } // Ends this block.

    private static Transform FindSceneTransform(string objectName) // Searches the active scene for a named transform.
    { // Starts this block.
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) // Loops through each item in this collection.
        { // Starts this block.
            if (root.name == objectName) // Checks whether this condition requires special handling.
            { // Starts this block.
                return root.transform; // Returns the computed value to the caller.
            } // Ends this block.

            Transform child = FindChild(root.transform, objectName); // Creates or stores a Transform reference.
            if (child != null) // Checks whether this condition requires special handling.
            { // Starts this block.
                return child; // Returns the computed value to the caller.
            } // Ends this block.
        } // Ends this block.

        return null; // Reports that no matching object was found.
    } // Ends this block.

    private static Transform FindChild(Transform root, string childName) // Recursively searches children by name.
    { // Starts this block.
        if (root == null) // Checks whether this condition requires special handling.
        { // Starts this block.
            return null; // Reports that no matching object was found.
        } // Ends this block.

        for (int i = 0; i < root.childCount; i++) // Loops through indexed items in this collection.
        { // Starts this block.
            Transform child = root.GetChild(i); // Creates or stores a Transform reference.
            if (child.name == childName) // Checks whether this condition requires special handling.
            { // Starts this block.
                return child; // Returns the computed value to the caller.
            } // Ends this block.

            Transform nestedChild = FindChild(child, childName); // Creates or stores a Transform reference.
            if (nestedChild != null) // Checks whether this condition requires special handling.
            { // Starts this block.
                return nestedChild; // Returns the computed value to the caller.
            } // Ends this block.
        } // Ends this block.

        return null; // Reports that no matching object was found.
    } // Ends this block.

    private void AbsorbSceneReferences(ChatManager sceneManager) // Copies scene references from a duplicate manager into the singleton.
    { // Starts this block.
        inputField = sceneManager.inputField ?? inputField; // Keeps the existing input unless the scene duplicate has one.
        sendButton = sceneManager.sendButton ?? sendButton; // Keeps the existing send button unless the scene duplicate has one.
        chatContent = sceneManager.chatContent ?? chatContent; // Stores the message content transform.
        chatMessagePrefab = sceneManager.chatMessagePrefab ?? chatMessagePrefab; // Keeps the existing message prefab unless the scene duplicate has one.
        channelDropdown = sceneManager.channelDropdown ?? channelDropdown; // Keeps the existing dropdown unless the scene duplicate has one.
        chatPanel = sceneManager.chatPanel ?? chatPanel; // Stores the chat panel GameObject.
        unreadBadge = sceneManager.unreadBadge ?? unreadBadge; // Stores the unread badge GameObject.
        unreadText = sceneManager.unreadText ?? unreadText; // Keeps the existing unread label unless the scene duplicate has one.
        collapseButton = sceneManager.collapseButton ?? collapseButton; // Keeps the existing collapse button unless the scene duplicate has one.

        if (hasStarted) // Checks whether this condition requires special handling.
        { // Starts this block.
            EnsureUi(); // Ensures chat UI references are ready before use.
            SetupUiBindings(); // Refreshes the button and input event bindings.
            SetupDropdown(); // Refreshes the dropdown after the active channel changes.
            ApplyLobbyDefaultVisibility(); // Applies the lobby visibility rule.
        } // Ends this block.
    } // Ends this block.

    private void ApplyLobbyDefaultVisibility() // Applies the lobby-specific default hidden state.
    { // Starts this block.
        if (SceneManager.GetActiveScene().name == "Lobby") // Checks whether this condition requires special handling.
        { // Starts this block.
            SetChatVisible(false); // Changes the chat panel visibility state.
        } // Ends this block.
    } // Ends this block.

    private void SetChatVisible(bool visible) // Stores and applies the requested visibility state.
    { // Starts this block.
        isVisible = visible; // Stores the requested expanded/collapsed state.
        ApplyChatVisibility(); // Applies the current chat visibility to the scene.
    } // Ends this block.

    private void EnsureInputFieldViewport() // Ensures TMP has a viewport for input rendering.
    { // Starts this block.
        if (inputField == null || inputField.textViewport != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            return; // Leaves the current method early.
        } // Ends this block.

        inputField.textViewport = inputField.GetComponent<RectTransform>(); // Updates the chat input text.
    } // Ends this block.

    private void ApplyChatVisibility() // Shows or hides chat UI elements.
    { // Starts this block.
        if (chatPanel != null) // Checks whether this condition requires special handling.
        { // Starts this block.
            if (collapseButton != null && collapseButton.transform.IsChildOf(chatPanel.transform)) // Checks whether this condition requires special handling.
            { // Starts this block.
                chatPanel.SetActive(true); // Shows or hides the root chat panel.
                Transform collapseTransform = collapseButton.transform; // Creates or stores a Transform reference.
                Transform unreadTransform = unreadBadge != null ? unreadBadge.transform : null; // Creates or stores a Transform reference.

                for (int i = 0; i < chatPanel.transform.childCount; i++) // Loops through indexed items in this collection.
                { // Starts this block.
                    Transform child = chatPanel.transform.GetChild(i); // Creates or stores a Transform reference.
                    if (IsAlwaysActiveChatControl(child, collapseTransform, unreadTransform)) // Checks whether this condition requires special handling.
                    { // Starts this block.
                        continue; // Leaves always-active controls untouched.
                    } // Ends this block.

                    child.gameObject.SetActive(isVisible); // Shows or hides this chat panel child.
                } // Ends this block.
            } // Ends this block.
            else // Runs when the previous condition is false.
            { // Starts this block.
                chatPanel.SetActive(isVisible); // Shows or hides the root chat panel.
            } // Ends this block.
        } // Ends this block.
    } // Ends this block.

    private bool IsAlwaysActiveChatControl(Transform child, Transform collapseTransform, Transform unreadTransform) // Identifies controls that should remain visible when collapsed.
    { // Starts this block.
        return IsControlOrAncestor(child, inputField != null ? inputField.transform : null) // Returns the computed value to the caller.
            || IsControlOrAncestor(child, sendButton != null ? sendButton.transform : null) // Keeps the send button visible when collapsed.
            || IsControlOrAncestor(child, channelDropdown != null ? channelDropdown.transform : null) // Keeps the channel dropdown visible when collapsed.
            || IsControlOrAncestor(child, collapseTransform) // Keeps the collapse control visible when collapsed.
            || IsControlOrAncestor(child, unreadTransform); // Keeps the unread badge visible when collapsed.
    } // Ends this block.

    private static bool IsControlOrAncestor(Transform possibleAncestor, Transform control) // Checks whether one transform contains another control.
    { // Starts this block.
        return possibleAncestor != null // Returns the computed value to the caller.
            && control != null // Requires a control transform to compare against.
            && (possibleAncestor == control || control.IsChildOf(possibleAncestor)); // Accepts either the control itself or one of its parents.
    } // Ends this block.

    private void EnsureCollapsedChatButton() // Keeps a placeholder for scene-authored collapse controls.
    { // Starts this block.
        // The collapsed/open affordance is authored in each scene now.
    } // Ends this block.

    private static GameObject CreateFallbackMessageObject(Transform parent) // Builds a basic message row when no prefab is assigned.
    { // Starts this block.
        GameObject messageObject = new GameObject("Chat Message"); // Creates or stores a GameObject reference.
        messageObject.transform.SetParent(parent, false); // Configures the fallback message GameObject.
        TMP_Text text = messageObject.AddComponent<TextMeshProUGUI>(); // Creates or stores a TextMeshPro text reference.
        text.fontSize = 14; // Configures the fallback message text component.
        text.color = new Color(0.12f, 0.12f, 0.12f, 1f); // Configures the fallback message text component.
        text.textWrappingMode = TextWrappingModes.Normal; // Configures the fallback message text component.
        return messageObject; // Returns the computed value to the caller.
    } // Ends this block.

} // Ends this block.

