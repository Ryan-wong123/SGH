using UnityEngine;
using Photon.Pun;
using TMPro;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(TMP_Dropdown))]
public class PhotonSyncedTMPDropdown : MonoBehaviourPun
{
    [SerializeField] private TMP_Dropdown dropdown;

    // Prevent loop when we set value from network
    private bool suppressCallbacks;

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        dropdown.onValueChanged.AddListener(OnLocalValueChanged);
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnLocalValueChanged);
    }

    private void OnLocalValueChanged(int value)
    {
        if (suppressCallbacks) return;

        // Broadcast to everyone else
        photonView.RPC(nameof(RPC_SetDropdownValue), RpcTarget.Others, value);
    }

    [PunRPC]
    private void RPC_SetDropdownValue(int value)
    {
        // Clamp just in case options differ
        value = Mathf.Clamp(value, 0, dropdown.options.Count - 1);

        suppressCallbacks = true;
        dropdown.value = value;

        // If you want the label to refresh immediately:
        dropdown.RefreshShownValue();

        suppressCallbacks = false;
    }

    // Optional: call this if you set dropdown.value via script and want to sync it too
    public void SetValueAndSync(int value)
    {
        value = Mathf.Clamp(value, 0, dropdown.options.Count - 1);

        suppressCallbacks = true;
        dropdown.value = value;
        dropdown.RefreshShownValue();
        suppressCallbacks = false;

        photonView.RPC(nameof(RPC_SetDropdownValue), RpcTarget.Others, value);
    }
}
