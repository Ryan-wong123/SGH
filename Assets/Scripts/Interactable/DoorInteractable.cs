using Photon.Pun;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Door Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private string keyName = "Key";

    [Header("Keypad Unlock Behaviour")]
    [SerializeField] private bool openOnUnlock = true;
    [SerializeField] private bool permanentlyDisableKeyRequirementOnUnlock = true;

    [Header("Leaf Settings")]
    [SerializeField] private bool isLeftDoor = false;
    [SerializeField] private float openAngle = 90f;
    [Tooltip("Optional. If not set, a hinge is auto-created at the door edge.")]
    [SerializeField] private Transform hinge;

    [Header("Animation")]
    [SerializeField] private bool smooth = false;
    [SerializeField] private float smoothSpeed = 360f;

    private HighlightEffect highlightEffect;
    private Transform highlightRoot;

    private bool isOpen = false;
    private Quaternion closedRotLocal;
    private Quaternion openRotLocal;

    private PhotonView pv;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        SetLayerRecursive(transform, gameObject.layer);

        SetupPhotonView();
        EnsureHingeExistsAndIsParented();

        if (hinge == null)
        {
            Debug.LogError($"{name}: Hinge could not be found/created. Assign one manually.");
            return;
        }

        closedRotLocal = hinge.localRotation;

        float dir = isLeftDoor ? -1f : 1f;
        if (IsMirrored(hinge)) dir *= -1f;

        openRotLocal = closedRotLocal * Quaternion.Euler(0f, dir * openAngle, 0f);

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

    void SetupPhotonView()
    {
        pv = GetComponent<PhotonView>();
        if (pv == null) pv = gameObject.AddComponent<PhotonView>();

        pv.OwnershipTransfer = OwnershipOption.Fixed;

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

        if (requiresKey && (InventorySystem.Instance == null || !InventorySystem.Instance.HasItem(keyName)))
        {
            Debug.Log("Use keypad to unlock door");
            return;
        }

        bool targetState = !isOpen;

        if (pv != null && PhotonNetwork.IsConnected)
        {
            // Scene object ownership fixed; allow Master to toggle, or allow anyone by directly RPCing AllBuffered
            pv.RPC(nameof(RPC_SetOpen), RpcTarget.AllBuffered, targetState);
        }
        else
        {
            SetOpenLocal(targetState);
        }
    }

    public string GetInteractionPrompt()
    {
        if (isLocked) return "This door is locked.";
        if (requiresKey && (InventorySystem.Instance == null || !InventorySystem.Instance.HasItem(keyName))) return "Use keypad to unlock door";
        return isOpen ? "Press E to close door" : "Press E to open door";
    }

    // ---------------- IHighlightable ----------------
    public void Highlight() => highlightEffect?.Highlight();
    public void Unhighlight() => highlightEffect?.Unhighlight();

    // ---------------- Keypad API ----------------

    // Called by Keypad when correct PIN entered
    public void UnlockFromKeypad()
    {
        if (pv != null && PhotonNetwork.IsConnected)
        {
            pv.RPC(nameof(RPC_KeypadUnlock), RpcTarget.AllBuffered, true);
        }
        else
        {
            ApplyKeypadUnlockLocal(true);
        }
    }

    [PunRPC]
    private void RPC_KeypadUnlock(bool unlocked)
    {
        ApplyKeypadUnlockLocal(unlocked);
    }

    private void ApplyKeypadUnlockLocal(bool unlocked)
    {
        isLocked = !unlocked;
        if (permanentlyDisableKeyRequirementOnUnlock)
            requiresKey = false;

        if (unlocked && openOnUnlock)
            SetOpenLocal(true);
    }

    // ---------------- Network sync (open/close) ----------------

    [PunRPC]
    void RPC_SetOpen(bool open)
    {
        SetOpenLocal(open);
    }

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

    // ---------------- Hinge helpers ----------------

    void EnsureHingeExistsAndIsParented()
    {
        if (hinge != null)
        {
            if (transform.parent != hinge)
                ReparentWithoutMoving(transform, hinge);
            return;
        }

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

        var rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning($"{name}: No Renderer found to infer bounds; cannot auto-create hinge.");
            return;
        }

        Bounds b = rend.bounds;
        Vector3 edge = b.center;
        edge.x = isLeftDoor ? b.min.x : b.max.x;

        var go = new GameObject("Hinge");
        hinge = go.transform;
        hinge.position = edge;
        hinge.rotation = transform.rotation;
        hinge.localScale = Vector3.one;

        hinge.SetParent(transform.parent, worldPositionStays: true);
        ReparentWithoutMoving(transform, hinge);
    }

    static void ReparentWithoutMoving(Transform child, Transform newParent)
    {
        Vector3 wp = child.position;
        Quaternion wr = child.rotation;
        child.SetParent(newParent, worldPositionStays: false);
        child.position = wp;
        child.rotation = wr;
    }

    static bool IsMirrored(Transform t) => t.localToWorldMatrix.determinant < 0f;
}
