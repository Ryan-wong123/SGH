using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(TMP_Dropdown))]
public class PhotonSyncedTMPDropdown : MonoBehaviourPun
{
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

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnLocalValueChanged);
    }

    private void OnLocalValueChanged(int value)
    {
        if (suppressCallbacks)
            return;

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

        photonView.RPC(nameof(RPC_SetDropdownValue), RpcTarget.Others, value);
    }

    public void SetBackgroundColorAndSync(Color color)
    {
        ApplyBackgroundColor(color);

        if (!PhotonNetwork.InRoom)
            return;

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
}
