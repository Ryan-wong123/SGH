using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(TMP_InputField))]
public class PhotonSyncedTMPInputField : MonoBehaviourPun, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TMP_InputField input;

    private int editActor = -1;
    private bool suppressCallbacks;

    private int LocalActor =>
        PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;

    private bool IsLocked => editActor != -1;
    private bool IsLockedByMe => editActor == LocalActor;

    private void Awake()
    {
        if (input == null)
            input = GetComponent<TMP_InputField>();

        input.onValueChanged.AddListener(OnLocalValueChanged);
    }

    private void OnDestroy()
    {
        input.onValueChanged.RemoveListener(OnLocalValueChanged);
    }

    // ─────────────────────────────
    // LOCAL TYPING
    // ─────────────────────────────
    private void OnLocalValueChanged(string value)
    {
        if (suppressCallbacks) return;
        if (!IsLockedByMe) return;

        photonView.RPC(nameof(RPC_SetText), RpcTarget.Others, value);
    }

    // ─────────────────────────────
    // LOCKING
    // ─────────────────────────────
    public void OnSelect(BaseEventData eventData)
    {
        if (!PhotonNetwork.InRoom) return;

        if (IsLocked && !IsLockedByMe)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        photonView.RPC(nameof(RPC_SetEditor), RpcTarget.All, LocalActor);

        if (!photonView.IsMine)
            photonView.RequestOwnership();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!PhotonNetwork.InRoom) return;

        if (IsLockedByMe)
        {
            photonView.RPC(nameof(RPC_ClearEditor), RpcTarget.All);
        }
    }

    // ─────────────────────────────
    // RPCs
    // ─────────────────────────────
    [PunRPC]
    private void RPC_SetEditor(int actor)
    {
        editActor = actor;
        input.readOnly = IsLocked && !IsLockedByMe;
    }

    [PunRPC]
    private void RPC_ClearEditor()
    {
        editActor = -1;
        input.readOnly = false;
    }

    [PunRPC]
    private void RPC_SetText(string text)
    {
        suppressCallbacks = true;
        input.text = text;
        suppressCallbacks = false;
    }

    // ─────────────────────────────
    // SAFETY: editor left room
    // ─────────────────────────────
    private void Update()
    {
        if (!PhotonNetwork.InRoom || !IsLocked) return;

        var p = PhotonNetwork.CurrentRoom.GetPlayer(editActor);
        if (p == null)
        {
            editActor = -1;
            input.readOnly = false;
        }
    }
}
