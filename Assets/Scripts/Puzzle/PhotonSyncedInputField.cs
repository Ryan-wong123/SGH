using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(TMP_InputField))]
public class PhotonSyncedTMPInputField : MonoBehaviourPun
{
    private const string TextPropertyPrefix = "TMPInputText_";
    private const string ColorPropertyPrefix = "TMPInputColor_";

    [SerializeField] private TMP_InputField input;

    private bool suppressCallbacks;
    private Graphic backgroundGraphic;

    // LWW ordering per-sender
    private int localSeq = 0;
    private readonly System.Collections.Generic.Dictionary<int, int> lastSeqByActor =
        new System.Collections.Generic.Dictionary<int, int>();

    private void Awake()
    {
        if (input == null)
            input = GetComponent<TMP_InputField>();

        backgroundGraphic = input != null && input.image != null
            ? input.image
            : GetComponent<Graphic>();

        input.onValueChanged.AddListener(OnLocalValueChanged);
    }

    private void OnEnable()
    {
        ApplyPersistedRoomState();
    }

    private void OnDestroy()
    {
        input.onValueChanged.RemoveListener(OnLocalValueChanged);
    }

    private void OnLocalValueChanged(string value)
    {
        if (suppressCallbacks)
            return;

        if (!PhotonNetwork.InRoom)
            return;

        localSeq++;
        int actor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;

        PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable
        {
            [BuildTextKey()] = value ?? string.Empty
        });

        // Everyone can type; broadcast changes to others.
        photonView.RPC(nameof(RPC_SetTextLWW), RpcTarget.Others, value, actor, localSeq);
    }

    [PunRPC]
    private void RPC_SetTextLWW(string text, int actor, int seq)
    {
        // Ignore out-of-order updates from the same actor
        if (lastSeqByActor.TryGetValue(actor, out int last) && seq <= last)
            return;

        lastSeqByActor[actor] = seq;

        suppressCallbacks = true;
        input.text = text;
        suppressCallbacks = false;
    }

    public void SetBackgroundColorAndSync(Color color)
    {
        ApplyBackgroundColor(color);

        if (!PhotonNetwork.InRoom)
            return;

        PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable
        {
            [BuildColorKey()] = EncodeColor(color)
        });

        photonView.RPC(nameof(RPC_SetBackgroundColor), RpcTarget.Others, color.r, color.g, color.b, color.a);
    }

    public void ResetBackgroundColorAndSync()
    {
        SetBackgroundColorAndSync(Color.white);
    }

    [PunRPC]
    private void RPC_SetBackgroundColor(float r, float g, float b, float a)
    {
        ApplyBackgroundColor(new Color(r, g, b, a));
    }

    private void ApplyBackgroundColor(Color color)
    {
        if (backgroundGraphic != null)
            backgroundGraphic.color = color;
    }

    private void ApplyPersistedRoomState()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || input == null)
            return;

        Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;

        string textKey = BuildTextKey();
        if (properties.ContainsKey(textKey))
        {
            suppressCallbacks = true;
            input.text = properties[textKey] as string ?? string.Empty;
            suppressCallbacks = false;
        }

        string colorKey = BuildColorKey();
        if (properties.ContainsKey(colorKey) && TryDecodeColor(properties[colorKey], out Color color))
            ApplyBackgroundColor(color);
    }

    private string BuildTextKey() => $"{TextPropertyPrefix}{photonView.ViewID}";
    private string BuildColorKey() => $"{ColorPropertyPrefix}{photonView.ViewID}";

    private static float[] EncodeColor(Color color) => new[] { color.r, color.g, color.b, color.a };

    private static bool TryDecodeColor(object value, out Color color)
    {
        if (value is float[] rgba && rgba.Length >= 4)
        {
            color = new Color(rgba[0], rgba[1], rgba[2], rgba[3]);
            return true;
        }

        if (value is Color storedColor)
        {
            color = storedColor;
            return true;
        }

        color = Color.white;
        return false;
    }
}
