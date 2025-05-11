using Photon.Chat;
using ExitGames.Client.Photon;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class ChatManager : MonoBehaviour, IChatClientListener
{
    private ChatClient chatClient;

    [Header("UI References")]
    public TMP_InputField inputField;
    public Button sendButton;
    public Transform chatContent; // Content panel inside ScrollView
    public GameObject chatMessagePrefab; // Prefab with TMP_Text component
    public TMP_Dropdown channelDropdown;

    [Header("Settings")]
    private string userName;
    private string[] chatChannels;
    private string selectedChannel;
    public string chatAppId = "41ecc5f0-2661-4f08-9c6b-3567bc7d495d";

    void Start()
    {
        userName = PhotonNetwork.NickName;
        string roomChannel = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "Room";
        chatChannels = new string[] { "Global", roomChannel };
        selectedChannel = chatChannels[0]; // Default to Global

        chatClient = new ChatClient(this);
        chatClient.Connect(chatAppId, "1.0", new AuthenticationValues(userName));

        sendButton.onClick.AddListener(SendFromInputField);
        channelDropdown.onValueChanged.AddListener(OnChannelChanged);

        SetupDropdown();
    }

    void Update()
    {
        chatClient?.Service();
    }
    void SetupDropdown()
    {
        channelDropdown.ClearOptions();
        channelDropdown.AddOptions(new System.Collections.Generic.List<string>(chatChannels));
    }

    void OnChannelChanged(int index)
    {
        selectedChannel = chatChannels[index];
    }
    public void OnConnected()
    {
        Debug.Log("Connected to Photon Chat");
        chatClient.Subscribe(chatChannels);
    }

    public void OnDisconnected() => Debug.Log("Disconnected from Photon Chat");

    public void OnChatStateChange(ChatState state) { }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        foreach (string channel in channels)
        {
            AddChatMessage($"<i><color=grey>Subscribed to {channel}</color></i>");
        }
    }

    public void OnUnsubscribed(string[] channels) { }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            AddChatMessage($"<b><color=#00ffff>[{channelName}]</color> <color=#00ffcc>{senders[i]}</color></b>: {messages[i]}");
        }
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
        if (!string.IsNullOrEmpty(inputField.text))
        {
            chatClient.PublishMessage(selectedChannel, inputField.text);
            inputField.text = "";
        }
    }

    private void AddChatMessage(string message)
    {
        GameObject msgObj = Instantiate(chatMessagePrefab, chatContent);
        TMP_Text tmp = msgObj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = message;
        }
    }
}
