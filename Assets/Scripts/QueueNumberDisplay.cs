using UnityEngine;
using TMPro;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// World-space TMP "queue board".
/// - Shows any free-form text (e.g., "154-> room").
/// - Flashes only when isFlash == true (solid otherwise).
/// - Optional cube emission pulse tied to isFlash.
/// - Photon sync for displayText and isFlash.
/// </summary>
public class QueueNumberDisplay : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
    , IPunObservable
#endif
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI label;          // World-space TMP on the cube face

    [Header("Displayed Text")]
    [Tooltip("Type anything here (e.g., \"154-> room\"). This is shown exactly as-is.")]
    [TextArea][SerializeField] private string displayText = "154-> room";

    [Header("Flash")]
    [SerializeField] public bool isFlash = false;            // <— director toggles this
    [Tooltip("How fast the label flashes.")]
    [SerializeField] private float flashSpeed = 2.5f;
    [Tooltip("Minimum and maximum alpha for the flashing text.")]
    [SerializeField] private Vector2 alphaRange = new Vector2(0.25f, 1f);

    [Header("Optional: Cube Glow Pulse")]
    [SerializeField] private Renderer cubeRenderer;
    [Tooltip("Enable if your material has Emission on.")]
    [SerializeField] private bool pulseEmission = true;
    [Tooltip("How fast the emission pulses.")]
    [SerializeField] private float emissionSpeed = 2f;
    [Tooltip("Emission intensity range.")]
    [SerializeField] private Vector2 emissionRange = new Vector2(0.1f, 1.5f);
    [SerializeField] private Color emissionColor = Color.white;

    // Internal
    [SerializeField] private Color baseTextColor = Color.white;
    private MaterialPropertyBlock mpb;

#if PHOTON_UNITY_NETWORKING
    private PhotonView pv;
#endif

    // ---------- Unity ----------
    void Awake()
    {
        if (label != null) baseTextColor = label.color;
        if (cubeRenderer != null) mpb = new MaterialPropertyBlock();
#if PHOTON_UNITY_NETWORKING
        pv = GetComponent<PhotonView>();
#endif
        RedrawImmediate();
        ApplyFlashVisuals(initial: true);
    }

    void Update()
    {
        // Text alpha flash ONLY when isFlash is true
        if (label != null)
        {
            if (isFlash)
            {
                float t = 0.5f * (Mathf.Sin(Time.unscaledTime * flashSpeed * Mathf.PI * 2f) + 1f);
                float a = Mathf.Lerp(alphaRange.x, alphaRange.y, t);
                var c = baseTextColor; c.a = a;
                label.color = c;
            }
            else
            {
                // Keep solid alpha when not flashing
                var c = baseTextColor; c.a = 1f;
                if (label.color.a != 1f) label.color = c;
            }
        }

        // Emission pulse follows isFlash (on when flashing, 0 when not)
        if (pulseEmission && cubeRenderer != null)
        {
            float intensity = 0f;
            if (isFlash)
            {
                float t = 0.5f * (Mathf.Sin(Time.unscaledTime * emissionSpeed * Mathf.PI * 2f) + 1f);
                intensity = Mathf.Lerp(emissionRange.x, emissionRange.y, t);
            }

            cubeRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", emissionColor * intensity);
            cubeRenderer.SetPropertyBlock(mpb);
        }
    }

    private void RedrawImmediate()
    {
        if (label != null)
            label.text = displayText ?? "";
    }

    private void ApplyFlashVisuals(bool initial = false)
    {
        // Ensure the label matches the isFlash state immediately
        if (label == null) return;
        if (!isFlash || initial)
        {
            var c = baseTextColor; c.a = isFlash ? c.a : 1f;
            label.color = c;
        }
    }

    // ---------- Public API ----------
    public void SetDisplayText(string text)
    {
#if PHOTON_UNITY_NETWORKING
        if (pv != null && pv.IsMine)
        {
            pv.RPC(nameof(RPC_SetDisplayText), RpcTarget.AllBuffered, text ?? "");
            return;
        }
#endif
        displayText = text ?? "";
        RedrawImmediate();
    }

    public void SetNumber(int number, int paddingDigits = 0, string prefix = "", string suffix = "")
    {
        string core = paddingDigits > 0 ? number.ToString($"D{Mathf.Max(1, paddingDigits)}") : number.ToString();
        SetDisplayText($"{prefix}{core}{suffix}");
    }

    public void SetFlash(bool on)
    {
#if PHOTON_UNITY_NETWORKING
        if (pv != null && pv.IsMine)
        {
            pv.RPC(nameof(RPC_SetFlash), RpcTarget.AllBuffered, on);
            return;
        }
#endif
        isFlash = on;
        ApplyFlashVisuals();
    }

#if PHOTON_UNITY_NETWORKING
    // ---------- Photon ----------
    [PunRPC]
    private void RPC_SetDisplayText(string text)
    {
        displayText = text ?? "";
        RedrawImmediate();
    }

    [PunRPC]
    private void RPC_SetFlash(bool on)
    {
        isFlash = on;
        ApplyFlashVisuals();
    }

    /// <summary>Optional state sync (backup to RPC buffer).</summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(displayText ?? "");
            stream.SendNext(isFlash);
        }
        else
        {
            displayText = (string)stream.ReceiveNext();
            isFlash = (bool)stream.ReceiveNext();
            RedrawImmediate();
            ApplyFlashVisuals();
        }
    }
#endif
}
