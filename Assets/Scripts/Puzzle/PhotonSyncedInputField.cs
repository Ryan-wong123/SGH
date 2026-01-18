using UnityEngine;
using Photon.Pun;
using TMPro;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(TMP_InputField))]
public class PhotonSyncedTMPInputField : MonoBehaviourPun
{
    [SerializeField] private TMP_InputField input;

    private bool suppressCallbacks;

    // LWW ordering per-sender
    private int localSeq = 0;
    private readonly System.Collections.Generic.Dictionary<int, int> lastSeqByActor =
        new System.Collections.Generic.Dictionary<int, int>();

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

    private void OnLocalValueChanged(string value)
    {
        if (suppressCallbacks) return;

        if (!PhotonNetwork.InRoom)
            return;

        localSeq++;
        int actor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;

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
}
