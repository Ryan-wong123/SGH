using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerTagDisplay : MonoBehaviourPunCallbacks
{
    private const string PlayerTagPropertyKey = "PlayerTag";

    [Header("Tag Visuals")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private int fontSize = 4;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private bool hideForLocalOwner = true;

    private Camera targetCamera;
    private TextMeshPro textMesh;
    private Transform tagTransform;
    private Vector3 resolvedOffset;

    private void Awake()
    {
        CreateTagObject();
        resolvedOffset = CalculateOffsetFromRenderers();
        UpdateTagText();
        ApplyLocalVisibility();
    }

    private void LateUpdate()
    {
        if (tagTransform == null)
        {
            return;
        }

        tagTransform.position = transform.position + resolvedOffset;

        Camera cam = GetTargetCamera();
        if (cam == null)
        {
            return;
        }

        Vector3 direction = tagTransform.position - cam.transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            tagTransform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (photonView.Owner == null || targetPlayer != photonView.Owner || changedProps == null)
        {
            return;
        }

        if (changedProps.ContainsKey(PlayerTagPropertyKey))
        {
            UpdateTagText();
        }
    }

    private void CreateTagObject()
    {
        if (tagTransform != null)
        {
            return;
        }

        GameObject tagObject = new GameObject("PlayerTag");
        tagObject.transform.SetParent(null, false);

        textMesh = tagObject.AddComponent<TextMeshPro>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.enableWordWrapping = false;
        textMesh.outlineWidth = 0.2f;
        textMesh.outlineColor = Color.black;
        textMesh.text = string.Empty;

        tagTransform = tagObject.transform;
        tagTransform.localScale = Vector3.one * 0.2f;
    }

    private Vector3 CalculateOffsetFromRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Vector3(worldOffset.x, 2.25f + worldOffset.y, worldOffset.z);
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        float height = Mathf.Max(1.5f, combinedBounds.max.y - transform.position.y);
        return new Vector3(worldOffset.x, height + worldOffset.y, worldOffset.z);
    }

    private void UpdateTagText()
    {
        if (textMesh == null)
        {
            return;
        }

        textMesh.text = GetPlayerTag();
    }

    private string GetPlayerTag()
    {
        Player owner = photonView.Owner;
        if (owner == null)
        {
            return "Player";
        }

        if (owner.CustomProperties != null &&
            owner.CustomProperties.TryGetValue(PlayerTagPropertyKey, out object tagValue) &&
            tagValue is string tag &&
            !string.IsNullOrWhiteSpace(tag))
        {
            return tag.Trim();
        }

        if (!string.IsNullOrWhiteSpace(owner.NickName))
        {
            return owner.NickName.Trim();
        }

        return $"Player {owner.ActorNumber}";
    }

    private void ApplyLocalVisibility()
    {
        if (textMesh == null)
        {
            return;
        }

        textMesh.enabled = !(hideForLocalOwner && photonView.IsMine);
    }

    private Camera GetTargetCamera()
    {
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
        {
            return targetCamera;
        }

        targetCamera = Camera.main;
        if (targetCamera != null && targetCamera.isActiveAndEnabled)
        {
            return targetCamera;
        }

        Camera[] cameras = FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].isActiveAndEnabled)
            {
                targetCamera = cameras[i];
                return targetCamera;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (tagTransform != null)
        {
            Destroy(tagTransform.gameObject);
        }
    }
}
