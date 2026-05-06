using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(TMP_Dropdown))]
public class PhotonSyncedTMPDropdown : MonoBehaviourPun
{
    private const string ValuePropertyPrefix = "TMPDropdownValue_";
    private const string ColorPropertyPrefix = "TMPDropdownColor_";

    [SerializeField] private TMP_Dropdown dropdown;

    // Prevent loop when we set value from network
    private bool suppressCallbacks;
    private Graphic backgroundGraphic;

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        backgroundGraphic = dropdown != null
            ? dropdown.targetGraphic as Graphic
            : GetComponent<Graphic>();

        dropdown.onValueChanged.AddListener(OnLocalValueChanged);
    }

    private void OnEnable()
    {
        ApplyPersistedRoomState();
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnLocalValueChanged);
    }

    private void OnLocalValueChanged(int value)
    {
        if (suppressCallbacks)
            return;

        PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable
        {
            [BuildValueKey()] = value
        });

        photonView.RPC(nameof(RPC_SetDropdownValue), RpcTarget.Others, value);
    }

    [PunRPC]
    private void RPC_SetDropdownValue(int value)
    {
        value = Mathf.Clamp(value, 0, dropdown.options.Count - 1);

        suppressCallbacks = true;
        dropdown.value = value;
        dropdown.RefreshShownValue();
        suppressCallbacks = false;
    }

    public void SetValueAndSync(int value)
    {
        value = Mathf.Clamp(value, 0, dropdown.options.Count - 1);

        suppressCallbacks = true;
        dropdown.value = value;
        dropdown.RefreshShownValue();
        suppressCallbacks = false;

        PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable
        {
            [BuildValueKey()] = value
        });

        photonView.RPC(nameof(RPC_SetDropdownValue), RpcTarget.Others, value);
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
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || dropdown == null)
            return;

        Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;

        string valueKey = BuildValueKey();
        if (properties.ContainsKey(valueKey) && properties[valueKey] is int value)
        {
            value = Mathf.Clamp(value, 0, dropdown.options.Count - 1);

            suppressCallbacks = true;
            dropdown.value = value;
            dropdown.RefreshShownValue();
            suppressCallbacks = false;
        }

        string colorKey = BuildColorKey();
        if (properties.ContainsKey(colorKey) && TryDecodeColor(properties[colorKey], out Color color))
            ApplyBackgroundColor(color);
    }

    private string BuildValueKey() => $"{ValuePropertyPrefix}{photonView.ViewID}";
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
