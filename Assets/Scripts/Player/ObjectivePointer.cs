using UnityEngine;

public class ObjectivePointer : MonoBehaviour
{
    public static ObjectivePointer Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;        // your local player transform
    [SerializeField] private Transform target;        // current objective
    [SerializeField] private RectTransform arrowUI;   // the UI Image RectTransform

    [Header("Settings")]
    [SerializeField] private float edgePadding = 50f; // distance from screen edge

    private Camera cam;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    private void Update()
    {
        if (!target)
        {
            arrowUI?.gameObject.SetActive(false);
            return;
        }
        else
        {
            arrowUI?.gameObject.SetActive(true);
        }

            Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        // If target is behind camera, flip it so arrow still points roughly correctly
        if (screenPos.z < 0)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        // Clamp position to screen edges (with padding)
        float clampedX = Mathf.Clamp(screenPos.x, edgePadding, Screen.width - edgePadding);
        float clampedY = Mathf.Clamp(screenPos.y, edgePadding, Screen.height - edgePadding);

        arrowUI.position = new Vector3(clampedX, clampedY, 0f);

        // Compute direction on the horizontal plane from player to target
        Vector3 worldDir = target.position - player.position;
        worldDir.y = 0;

        if (worldDir.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
            // Rotate arrow so it points towards the objective
            arrowUI.rotation = Quaternion.Euler(0, 0, -angle);
        }

        arrowUI.gameObject.SetActive(true);
    }

    // Call this when you change objectives
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log($"target for arrow set to {target.name}");
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
        Debug.Log($"player for arrow set to {player.name}");
    }
    public void SetCamera(Camera newCam)
    {
        cam = newCam;
    }

}


