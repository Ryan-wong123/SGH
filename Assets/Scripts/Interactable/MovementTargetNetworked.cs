using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class MovementTargetForward : MonoBehaviourPun, IPunObservable, IHighlightable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveDistance = 3f;   // how far to move forward (local)
    [SerializeField] private float moveSpeed = 2f;      // units per second (linear)

    // Runtime/networked state
    private bool isMoving = false;
    private bool hasReachedTarget = false;
    private Vector3 moveFrom;
    private Vector3 moveTo;
    private float moveDuration;
    private double moveStartTime;

    private HighlightEffect highlightEffect;

    private void Start()
    {
        highlightEffect = GetComponent<HighlightEffect>();
    }

    // ---- IInteractable ----
    public void Interact()
    {
        // Ignore if already moving or already completed the one-shot move
        if (isMoving || hasReachedTarget) return;         

        if (!photonView.AmOwner) photonView.RequestOwnership();

        Vector3 currentPos = transform.position;
        Vector3 targetPos = currentPos + transform.forward * moveDistance;

        float duration = moveSpeed > 0f
            ? Vector3.Distance(currentPos, targetPos) / moveSpeed
            : 0f;

        photonView.RPC(nameof(RPC_BeginMove), RpcTarget.All, currentPos, targetPos, Mathf.Max(0.0001f, duration), PhotonNetwork.Time);
    }

    [PunRPC]
    private void RPC_BeginMove(Vector3 from, Vector3 to, float duration, double startStamp)
    {
        if (hasReachedTarget) return; // already completed elsewhere

        moveFrom = from;
        moveTo = to;
        moveDuration = duration;
        moveStartTime = startStamp;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;

        double now = PhotonNetwork.Time;
        float t = (float)((now - moveStartTime) / moveDuration);

        if (t < 1f)
        {
            // strictly linear motion
            transform.position = Vector3.LerpUnclamped(moveFrom, moveTo, t);
        }
        else
        {
            transform.position = moveTo;
            isMoving = false;
            hasReachedTarget = true; // lock further interactions
        }
    }
 
    // ---- Minimal state sync for late joiners/snapshots ----
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isMoving);
            stream.SendNext(hasReachedTarget);
            stream.SendNext(transform.position);
        }
        else
        {
            isMoving = (bool)stream.ReceiveNext();
            hasReachedTarget = (bool)stream.ReceiveNext();
            Vector3 netPos = (Vector3)stream.ReceiveNext();

            if (!isMoving) transform.position = netPos; // snap if idle
        }
    }

    // ---- IHighlightable (using your HighlightEffect) ----
    public void Highlight() => highlightEffect?.Highlight();
    public void Unhighlight() => highlightEffect?.Unhighlight();
}
