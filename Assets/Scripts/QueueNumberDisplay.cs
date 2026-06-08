using UnityEngine; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
#if PHOTON_UNITY_NETWORKING // Starts a conditional compilation block.
using Photon.Pun; // Imports a namespace used by this script.
#endif // Ends the conditional compilation block.

/// <summary>
/// World-space TMP "queue board".
/// - Shows any free-form text (e.g., "154-> room").
/// - Flashes only when isFlash == true (solid otherwise).
/// - Optional cube emission pulse tied to isFlash.
/// - Photon sync for displayText and isFlash.
/// </summary>
public class QueueNumberDisplay : MonoBehaviour // Declares this type.
#if PHOTON_UNITY_NETWORKING // Starts a conditional compilation block.
    , IPunObservable // Runs this line of logic.
#endif // Ends the conditional compilation block.
{ // Starts this code block.
    [Header("UI")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private TextMeshProUGUI label;          // World-space TMP on the cube face

    [Header("Displayed Text")] // Adds a labeled section in the Unity Inspector.
    [Tooltip("Type anything here (e.g., \"154-> room\"). This is shown exactly as-is.")] // Shows helper text for this field in the Unity Inspector.
    [TextArea][SerializeField] private string displayText = "154-> room"; // Adds an attribute that changes how Unity or C# treats the next declaration.

    [Header("Flash")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] public bool isFlash = false;            // <— director toggles this
    [Tooltip("How fast the label flashes.")] // Shows helper text for this field in the Unity Inspector.
    [SerializeField] private float flashSpeed = 2.5f; // Exposes this private field in the Unity Inspector.
    [Tooltip("Minimum and maximum alpha for the flashing text.")] // Shows helper text for this field in the Unity Inspector.
    [SerializeField] private Vector2 alphaRange = new Vector2(0.25f, 1f); // Exposes this private field in the Unity Inspector.

    [Header("Optional: Cube Glow Pulse")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private Renderer cubeRenderer; // Exposes this private field in the Unity Inspector.
    [Tooltip("Enable if your material has Emission on.")] // Shows helper text for this field in the Unity Inspector.
    [SerializeField] private bool pulseEmission = true; // Exposes this private field in the Unity Inspector.
    [Tooltip("How fast the emission pulses.")] // Shows helper text for this field in the Unity Inspector.
    [SerializeField] private float emissionSpeed = 2f; // Exposes this private field in the Unity Inspector.
    [Tooltip("Emission intensity range.")] // Shows helper text for this field in the Unity Inspector.
    [SerializeField] private Vector2 emissionRange = new Vector2(0.1f, 1.5f); // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color emissionColor = Color.white; // Exposes this private field in the Unity Inspector.

    // Internal
    [SerializeField] private Color baseTextColor = Color.white; // Exposes this private field in the Unity Inspector.
    private MaterialPropertyBlock mpb; // Declares a field, property, or member used by this script.

#if PHOTON_UNITY_NETWORKING // Starts a conditional compilation block.
    private PhotonView pv; // Declares a field, property, or member used by this script.
#endif // Ends the conditional compilation block.

    // ---------- Unity ----------
    void Awake() // Declares a method used by this script.
    { // Starts this code block.
        if (label != null) baseTextColor = label.color; // Checks this condition before continuing.
        if (cubeRenderer != null) mpb = new MaterialPropertyBlock(); // Checks this condition before continuing.
#if PHOTON_UNITY_NETWORKING // Starts a conditional compilation block.
        pv = GetComponent<PhotonView>(); // Updates this value.
#endif // Ends the conditional compilation block.
        RedrawImmediate(); // Runs this line of logic.
        ApplyFlashVisuals(initial: true); // Runs this line of logic.
    } // Ends this code block.

    void Update() // Declares a method used by this script.
    { // Starts this code block.
        // Text alpha flash ONLY when isFlash is true
        if (label != null) // Checks this condition before continuing.
        { // Starts this code block.
            if (isFlash) // Checks this condition before continuing.
            { // Starts this code block.
                float t = 0.5f * (Mathf.Sin(Time.unscaledTime * flashSpeed * Mathf.PI * 2f) + 1f); // Creates a local value used by the following logic.
                float a = Mathf.Lerp(alphaRange.x, alphaRange.y, t); // Creates a local value used by the following logic.
                var c = baseTextColor; c.a = a; // Creates a local value used by the following logic.
                label.color = c; // Updates this value.
            } // Ends this code block.
            else // Runs when the previous condition was false.
            { // Starts this code block.
                // Keep solid alpha when not flashing
                var c = baseTextColor; c.a = 1f; // Creates a local value used by the following logic.
                if (label.color.a != 1f) label.color = c; // Checks this condition before continuing.
            } // Ends this code block.
        } // Ends this code block.

        // Emission pulse follows isFlash (on when flashing, 0 when not)
        if (pulseEmission && cubeRenderer != null) // Checks this condition before continuing.
        { // Starts this code block.
            float intensity = 0f; // Creates a local value used by the following logic.
            if (isFlash) // Checks this condition before continuing.
            { // Starts this code block.
                float t = 0.5f * (Mathf.Sin(Time.unscaledTime * emissionSpeed * Mathf.PI * 2f) + 1f); // Creates a local value used by the following logic.
                intensity = Mathf.Lerp(emissionRange.x, emissionRange.y, t); // Updates this value.
            } // Ends this code block.

            cubeRenderer.GetPropertyBlock(mpb); // Runs this line of logic.
            mpb.SetColor("_EmissionColor", emissionColor * intensity); // Runs this line of logic.
            cubeRenderer.SetPropertyBlock(mpb); // Runs this line of logic.
        } // Ends this code block.
    } // Ends this code block.

    private void RedrawImmediate() // Declares a method used by this script.
    { // Starts this code block.
        if (label != null) // Checks this condition before continuing.
            label.text = displayText ?? ""; // Updates this value.
    } // Ends this code block.

    private void ApplyFlashVisuals(bool initial = false) // Declares a method used by this script.
    { // Starts this code block.
        // Ensure the label matches the isFlash state immediately
        if (label == null) return; // Checks this condition before continuing.
        if (!isFlash || initial) // Checks this condition before continuing.
        { // Starts this code block.
            var c = baseTextColor; c.a = isFlash ? c.a : 1f; // Creates a local value used by the following logic.
            label.color = c; // Updates this value.
        } // Ends this code block.
    } // Ends this code block.

    // ---------- Public API ----------
    public void SetDisplayText(string text) // Declares a method used by this script.
    { // Starts this code block.
#if PHOTON_UNITY_NETWORKING // Starts a conditional compilation block.
        if (pv != null && pv.IsMine) // Checks this condition before continuing.
        { // Starts this code block.
            pv.RPC(nameof(RPC_SetDisplayText), RpcTarget.AllBuffered, text ?? ""); // Runs this line of logic.
            return; // Returns this result to the caller.
        } // Ends this code block.
#endif // Ends the conditional compilation block.
        displayText = text ?? ""; // Updates this value.
        RedrawImmediate(); // Runs this line of logic.
    } // Ends this code block.

    public void SetNumber(int number, int paddingDigits = 0, string prefix = "", string suffix = "") // Declares a method used by this script.
    { // Starts this code block.
        string core = paddingDigits > 0 ? number.ToString($"D{Mathf.Max(1, paddingDigits)}") : number.ToString(); // Creates a local value used by the following logic.
        SetDisplayText($"{prefix}{core}{suffix}"); // Runs this line of logic.
    } // Ends this code block.

    public void SetFlash(bool on) // Declares a method used by this script.
    { // Starts this code block.
#if PHOTON_UNITY_NETWORKING // Starts a conditional compilation block.
        if (pv != null && pv.IsMine) // Checks this condition before continuing.
        { // Starts this code block.
            pv.RPC(nameof(RPC_SetFlash), RpcTarget.AllBuffered, on); // Runs this line of logic.
            return; // Returns this result to the caller.
        } // Ends this code block.
#endif // Ends the conditional compilation block.
        isFlash = on; // Updates this value.
        ApplyFlashVisuals(); // Runs this line of logic.
    } // Ends this code block.

#if PHOTON_UNITY_NETWORKING // Starts a conditional compilation block.
    // ---------- Photon ----------
    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetDisplayText(string text) // Declares a method used by this script.
    { // Starts this code block.
        displayText = text ?? ""; // Updates this value.
        RedrawImmediate(); // Runs this line of logic.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetFlash(bool on) // Declares a method used by this script.
    { // Starts this code block.
        isFlash = on; // Updates this value.
        ApplyFlashVisuals(); // Runs this line of logic.
    } // Ends this code block.

    /// <summary>Optional state sync (backup to RPC buffer).</summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) // Declares a method used by this script.
    { // Starts this code block.
        if (stream.IsWriting) // Checks this condition before continuing.
        { // Starts this code block.
            stream.SendNext(displayText ?? ""); // Runs this line of logic.
            stream.SendNext(isFlash); // Runs this line of logic.
        } // Ends this code block.
        else // Runs when the previous condition was false.
        { // Starts this code block.
            displayText = (string)stream.ReceiveNext(); // Updates this value.
            isFlash = (bool)stream.ReceiveNext(); // Updates this value.
            RedrawImmediate(); // Runs this line of logic.
            ApplyFlashVisuals(); // Runs this line of logic.
        } // Ends this code block.
    } // Ends this code block.
#endif // Ends the conditional compilation block.
} // Ends this code block.
