using UnityEngine;
using TMPro;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

/// <summary>
/// Flash a free-form string (e.g., "154-> room") on a world-space TMP label,
/// with optional cube emission pulse and optional Photon sync.
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
    [SerializeField] private Color baseTextColor;
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
    }

    void Update()
    {
        // Flashing alpha
        if (label != null)
        {
            float t = 0.5f * (Mathf.Sin(Time.time * flashSpeed * Mathf.PI * 2f) + 1f);
            float a = Mathf.Lerp(alphaRange.x, alphaRange.y, t);
            var c = baseTextColor;
            c.a = a;
            label.color = c;
        }

        // Emission pulse
        if (pulseEmission && cubeRenderer != null)
        {
            float t = 0.5f * (Mathf.Sin(Time.time * emissionSpeed * Mathf.PI * 2f) + 1f);
            float intensity = Mathf.Lerp(emissionRange.x, emissionRange.y, t);

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

    // ---------- Public API ----------
    /// <summary>Sets any free-form text (symbols allowed) and updates the label.</summary>
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

    /// <summary>
    /// Convenience method if you sometimes want to push a number.
    /// Set paddingDigits = 0 for raw number; e.g., 7 → "7".
    /// Set paddingDigits = 3 for "007", etc. You can also add a suffix/prefix.
    /// </summary>
    public void SetNumber(int number, int paddingDigits = 0, string prefix = "", string suffix = "")
    {
        string core = paddingDigits > 0 ? number.ToString($"D{Mathf.Max(1, paddingDigits)}") : number.ToString();
        SetDisplayText($"{prefix}{core}{suffix}");
    }

#if PHOTON_UNITY_NETWORKING
    // ---------- Photon ----------
    [PunRPC]
    private void RPC_SetDisplayText(string text)
    {
        displayText = text ?? "";
        RedrawImmediate();
    }

    /// <summary>
    /// Optional state sync (backup to RPC buffer).
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
            stream.SendNext(displayText ?? "");
        else
        {
            displayText = (string)stream.ReceiveNext();
            RedrawImmediate();
        }
    }
#endif
}
