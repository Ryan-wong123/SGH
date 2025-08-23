using UnityEngine;
using TMPro;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class QueueNumberDisplay : MonoBehaviour
#if PHOTON_UNITY_NETWORKING
    , IPunObservable
#endif
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI numberLabel;

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

    [SerializeField] private int currentNumber = 0;
    [SerializeField] private Color baseTextColor = Color.white;
    [SerializeField] private Color emissionColor = Color.white;
    private MaterialPropertyBlock mpb;

#if PHOTON_UNITY_NETWORKING
    private PhotonView pv;
#endif

    void Awake()
    {
        if (numberLabel != null)
            baseTextColor = numberLabel.color;

        if (cubeRenderer != null)
            mpb = new MaterialPropertyBlock();

#if PHOTON_UNITY_NETWORKING
        pv = GetComponent<PhotonView>();
#endif
        UpdateLabelImmediate();
    }

    void Update()
    {
        if (numberLabel != null)
        {
            // Flash the text alpha with a smooth ping-pong
            float t = 0.5f * (Mathf.Sin(Time.time * flashSpeed * Mathf.PI * 2f) + 1f);
            float a = Mathf.Lerp(alphaRange.x, alphaRange.y, t);
            var c = baseTextColor;
            c.a = a;
            numberLabel.color = c;
        }

        if (pulseEmission && cubeRenderer != null)
        {
            float t = 0.5f * (Mathf.Sin(Time.time * emissionSpeed * Mathf.PI * 2f) + 1f);
            float intensity = Mathf.Lerp(emissionRange.x, emissionRange.y, t);

            cubeRenderer.GetPropertyBlock(mpb);
            // Standard shader uses _EmissionColor. Ensure Emission is enabled on the material.
            mpb.SetColor("_EmissionColor", emissionColor * intensity);
            cubeRenderer.SetPropertyBlock(mpb);
        }
    }

    private void UpdateLabelImmediate()
    {
        if (numberLabel != null)
            numberLabel.text = currentNumber.ToString();

    }

    // ----- Public API -----
    public void SetQueueNumber(int number)
    {
#if PHOTON_UNITY_NETWORKING
        if (pv != null && pv.IsMine)
        {
            pv.RPC(nameof(RPC_SetQueueNumber), RpcTarget.AllBuffered, number);
        }
        else
#endif
        {
            currentNumber = Mathf.Max(0, number);
            UpdateLabelImmediate();
        }
    }

    public void Increment()
    {
        SetQueueNumber(currentNumber + 1);
    }

    public void Decrement()
    {
        SetQueueNumber(Mathf.Max(0, currentNumber - 1));
    }

#if PHOTON_UNITY_NETWORKING
    [PunRPC]
    private void RPC_SetQueueNumber(int number)
    {
        currentNumber = Mathf.Max(0, number);
        UpdateLabelImmediate();
    }

    // Optional: state sync for late joiners if you don't want to rely on RPC buffer.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
            stream.SendNext(currentNumber);
        else
        {
            currentNumber = (int)stream.ReceiveNext();
            UpdateLabelImmediate();
        }
    }
#endif
}
