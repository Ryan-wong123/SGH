using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PickupItem : MonoBehaviourPun, IInteractable, IHighlightable
{
    [Header("Pickup Info")]
    [SerializeField] private string itemName = "Key";

    private Renderer rend;
    private Material material;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float emissionIntensity = 1.5f;

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    [SerializeField] private float highlightScaleMultiplier = 1.1f;
    [SerializeField] private float scaleSpeed = 5f;
    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        SetupPhotonView();
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            material = rend.material;
        }
        originalScale = transform.localScale;
    }

    void SetupPhotonView()
    {
        // Ensure PhotonView exists
        PhotonView view = GetComponent<PhotonView>();
        if (view == null)
        {
            view = gameObject.AddComponent<PhotonView>();
        }

        // Set Ownership Transfer to Fixed (scene object)
        view.OwnershipTransfer = OwnershipOption.Fixed;

        // Set Synchronization method (choose one)
        view.Synchronization = ViewSynchronization.UnreliableOnChange; // or .ReliableOnChange

        // Ensure this scene object is registered at runtime
        if (PhotonNetwork.IsConnectedAndReady && view.ViewID == 0)
        {
            PhotonNetwork.RegisterPhotonView(view);
            Debug.Log($"Registered scene object with ViewID: {view.ViewID}");
        }
    }

    public void Interact()
    {
        photonView.RPC(nameof(SyncItemPickup), RpcTarget.AllBuffered, itemName);
        photonView.RPC(nameof(RequestDestroy), RpcTarget.MasterClient);

    }

    [PunRPC]
    void SyncItemPickup(string itemName)
    {
        StartCoroutine(SafeSync(itemName));
    }

    System.Collections.IEnumerator SafeSync(string itemName)
    {
        while (InventorySystem.Instance == null || InventoryUIManager.Instance == null)
        {
            Debug.Log("[Sync] Still null...");
            yield return null;
        }

        Debug.Log($"Adding {itemName}");

        InventorySystem.Instance.AddItem(itemName);
        InventoryUIManager.Instance.UpdateInventoryUI();
    }

    [PunRPC]
    void RequestDestroy()
    {
        // Only MasterClient will execute this
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
    public string GetInteractionPrompt()
    {
        return $"Press E to pick up {itemName}";
    }

    public void Highlight()
    {
        if (material == null) return;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", highlightColor * emissionIntensity);
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale * highlightScaleMultiplier));
    }

    public void Unhighlight()
    {
        if (material == null) return;
        material.SetColor("_EmissionColor", Color.black);
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale));
    }
    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
            yield return null;
        }
        transform.localScale = targetScale;
    }
}
