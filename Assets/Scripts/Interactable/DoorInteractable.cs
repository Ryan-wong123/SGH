using Photon.Pun;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Door Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private string keyName = "Key";

    [Header("Leaf Settings")]
    [SerializeField] private bool isLeftDoor = false;     // Left leaf swings negative
    [SerializeField] private float openAngle = 90f;       // Degrees to swing open
    [Tooltip("Optional. If not set, a hinge is auto-created at the door edge.")]
    [SerializeField] private Transform hinge;

    [Header("Animation")]
    [SerializeField] private bool smooth = false;
    [SerializeField] private float smoothSpeed = 360f;    // deg/sec

    private HighlightEffect highlightEffect;
    private Transform highlightRoot;

    private bool isOpen = false;

    // Cached rotations
    private Quaternion closedRotLocal;
    private Quaternion openRotLocal;

    private PhotonView pv;

    private void Awake()
    {
        // Make the whole door interactable by raycasts (your layer mask must include this)
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        SetLayerRecursive(transform, gameObject.layer);

        SetupPhotonView();
        EnsureHingeExistsAndIsParented();

        if (hinge == null)
        {
            Debug.LogError($"{name}: Hinge could not be found/created. Assign one manually.");
            return;
        }

        // Cache closed pose as authored
        closedRotLocal = hinge.localRotation;

        // Direction: left swings negative, right swings positive
        float dir = isLeftDoor ? -1f : 1f;

        // If the transform hierarchy is mirrored, flip direction
        if (IsMirrored(hinge)) dir *= -1f;

        openRotLocal = closedRotLocal * Quaternion.Euler(0f, dir * openAngle, 0f);

        // ---- Highlight setup ----
        if (highlightEffect == null)
        {
            Transform searchRoot = highlightRoot != null ? highlightRoot : transform;
            highlightEffect = searchRoot.GetComponent<HighlightEffect>();
            if (highlightEffect == null)
                highlightEffect = searchRoot.GetComponentInChildren<HighlightEffect>(true);
        }
    }
    static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursive(t.GetChild(i), layer);
    }

    // ---------------- Photon setup ----------------

    void SetupPhotonView()
    {
        pv = GetComponent<PhotonView>();
        if (pv == null) pv = gameObject.AddComponent<PhotonView>();

        // Scene object ownership
        pv.OwnershipTransfer = OwnershipOption.Fixed;

        // Optional observer (kept from your original)
        if (GetComponent<PhotonTransformView>() == null)
        {
            var transformView = gameObject.AddComponent<PhotonTransformView>();
            pv.ObservedComponents.Add(transformView);
        }

        pv.Synchronization = ViewSynchronization.UnreliableOnChange;

        if (PhotonNetwork.IsConnectedAndReady && pv.ViewID == 0)
        {
            PhotonNetwork.RegisterPhotonView(pv);
            Debug.Log($"Registered scene object with ViewID: {pv.ViewID}");
        }
    }
    private void OnDisable()
    {
        // If we get disabled while highlighted, reset safely
        highlightEffect?.Unhighlight();
    }

    // ---------------- IInteractable ----------------

    public void Interact()
    {
        if (isLocked)
        {
            Debug.Log("Door is locked.");
            return;
        }

        if (requiresKey && !InventorySystem.Instance.HasItem(keyName))
        {
            Debug.Log($"Door requires a {keyName}");
            return;
        }

        // Network-safe toggle: broadcast target state so everyone lands exactly on the same angle
        bool targetState = !isOpen;

        if (pv != null && PhotonNetwork.IsConnected)
        {
            if (pv.IsMine)
                pv.RPC(nameof(RPC_SetOpen), RpcTarget.AllBuffered, targetState);
            else
                Debug.Log("PhotonView is not mine; waiting for owner to toggle.");
        }
        else
        {
            // Offline/local mode
            SetOpenLocal(targetState);
        }
    }

    public string GetInteractionPrompt()
    {
        if (isLocked) return "This door is locked.";
        if (requiresKey && !InventorySystem.Instance.HasItem(keyName)) return $"Door requires a {keyName}";
        return isOpen ? "Press E to close door" : "Press E to open door";
    }

    // ---------------- IHighlightable ----------------
    public void Highlight() => highlightEffect?.Highlight();
    public void Unhighlight() => highlightEffect?.Unhighlight();

    // ---------------- Network sync ----------------

    [PunRPC]
    void RPC_SetOpen(bool open)
    {
        SetOpenLocal(open);
    }

    // ---------------- Core open/close logic ----------------

    void SetOpenLocal(bool open)
    {
        if (hinge == null) return;

        isOpen = open;
        Quaternion target = isOpen ? openRotLocal : closedRotLocal;

        if (!smooth)
        {
            hinge.localRotation = target;
            return;
        }

        // Smoothly rotate to target
        StopAllCoroutines();
        StartCoroutine(RotateTo(hinge, target, smoothSpeed));
    }

    System.Collections.IEnumerator RotateTo(Transform t, Quaternion target, float degPerSec)
    {
        while (Quaternion.Angle(t.localRotation, target) > 0.1f)
        {
            float step = degPerSec * Time.deltaTime;
            t.localRotation = Quaternion.RotateTowards(t.localRotation, target, step);
            yield return null;
        }
        t.localRotation = target;
    }

    // ---------------- Hinge setup helpers ----------------

    void EnsureHingeExistsAndIsParented()
    {
        if (hinge != null)
        {
            if (transform.parent != hinge)
                ReparentWithoutMoving(transform, hinge);
            return;
        }

        // Try find an existing child/parent named "Hinge"
        Transform found = null;
        if (transform.parent != null) found = transform.parent.Find("Hinge");
        if (found == null) found = transform.Find("Hinge");

        if (found != null)
        {
            hinge = found;
            if (transform.parent != hinge)
                ReparentWithoutMoving(transform, hinge);
            return;
        }

        // Auto-create hinge at door edge using Renderer bounds
        var rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning($"{name}: No Renderer found to infer bounds; cannot auto-create hinge.");
            return;
        }

        Bounds b = rend.bounds; // world-space bounds
        Vector3 edge = b.center;
        edge.x = isLeftDoor ? b.min.x : b.max.x; // use X edge for left/right

        var go = new GameObject("Hinge");
        hinge = go.transform;
        hinge.position = edge;
        hinge.rotation = transform.rotation; // share same orientation
        hinge.localScale = Vector3.one;

        // Put hinge beside door in hierarchy
        hinge.SetParent(transform.parent, worldPositionStays: true);

        // Make door a child of hinge without moving it
        ReparentWithoutMoving(transform, hinge);
    }

    static void ReparentWithoutMoving(Transform child, Transform newParent)
    {
        Vector3 wp = child.position;
        Quaternion wr = child.rotation;
        child.SetParent(newParent, worldPositionStays: false);
        child.position = wp;
        child.rotation = wr;
        // Assume uniform scales for clean rotation around hinge
    }

    static bool IsMirrored(Transform t)
    {
        // Negative determinant => mirrored (handedness flip) somewhere in hierarchy
        return t.localToWorldMatrix.determinant < 0f;
    }


}
