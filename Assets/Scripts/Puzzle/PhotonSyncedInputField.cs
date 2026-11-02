using Photon.Pun; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using Hashtable = ExitGames.Client.Photon.Hashtable; // Imports a namespace used by this script.

[RequireComponent(typeof(PhotonView))] // Adds an attribute that changes how Unity or C# treats the next declaration.
[RequireComponent(typeof(TMP_InputField))] // Adds an attribute that changes how Unity or C# treats the next declaration.
public class PhotonSyncedTMPInputField : MonoBehaviourPun // Declares this type.
{ // Starts this code block.
    private const string TextPropertyPrefix = "TMPInputText_"; // Declares a field, property, or member used by this script.
    private const string ColorPropertyPrefix = "TMPInputColor_"; // Declares a field, property, or member used by this script.

    [SerializeField] private TMP_InputField input; // Exposes this private field in the Unity Inspector.

    private bool suppressCallbacks; // Declares a field, property, or member used by this script.
    private Graphic backgroundGraphic; // Declares a field, property, or member used by this script.

    // LWW ordering per-sender
    private int localSeq = 0; // Declares a field, property, or member used by this script.
    private readonly System.Collections.Generic.Dictionary<int, int> lastSeqByActor = // Declares a field, property, or member used by this script.
        new System.Collections.Generic.Dictionary<int, int>(); // Runs this line of puzzle logic.

    private void Awake() // Declares a method used by this script.
    { // Starts this code block.
        if (input == null) // Checks this condition before continuing.
            input = GetComponent<TMP_InputField>(); // Updates this value.

        backgroundGraphic = input != null && input.image != null // Updates this value.
            ? input.image // Runs this line of puzzle logic.
            : GetComponent<Graphic>(); // Runs this line of puzzle logic.

        input.onValueChanged.AddListener(OnLocalValueChanged); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnEnable() // Declares a method used by this script.
    { // Starts this code block.
        ApplyPersistedRoomState(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnDestroy() // Declares a method used by this script.
    { // Starts this code block.
        input.onValueChanged.RemoveListener(OnLocalValueChanged); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnLocalValueChanged(string value) // Declares a method used by this script.
    { // Starts this code block.
        if (suppressCallbacks) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        GameSessionCsvLogger.Instance?.RecordLocalPlayerInput(gameObject.name, value);

        if (!PhotonNetwork.InRoom || photonView.ViewID <= 0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        localSeq++; // Runs this line of puzzle logic.
        int actor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1; // Creates a local value used by the following logic.

        PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildTextKey()] = value ?? string.Empty // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.

        // Everyone can type; broadcast changes to others.
        photonView.RPC(nameof(RPC_SetTextLWW), RpcTarget.Others, value, actor, localSeq); // Runs this line of puzzle logic.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetTextLWW(string text, int actor, int seq) // Declares a method used by this script.
    { // Starts this code block.
        // Ignore out-of-order updates from the same actor
        if (lastSeqByActor.TryGetValue(actor, out int last) && seq <= last) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        lastSeqByActor[actor] = seq; // Updates this value.

        suppressCallbacks = true; // Updates this value.
        input.text = text; // Updates this value.
        suppressCallbacks = false; // Updates this value.
    } // Ends this code block.

    public void SetBackgroundColorAndSync(Color color) // Declares a method used by this script.
    { // Starts this code block.
        ApplyBackgroundColor(color); // Runs this line of puzzle logic.

        if (!PhotonNetwork.InRoom || photonView.ViewID <= 0) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildColorKey()] = EncodeColor(color) // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.

        photonView.RPC(nameof(RPC_SetBackgroundColor), RpcTarget.Others, color.r, color.g, color.b, color.a); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void ResetBackgroundColorAndSync() // Declares a method used by this script.
    { // Starts this code block.
        SetBackgroundColorAndSync(Color.white); // Runs this line of puzzle logic.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetBackgroundColor(float r, float g, float b, float a) // Declares a method used by this script.
    { // Starts this code block.
        ApplyBackgroundColor(new Color(r, g, b, a)); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyBackgroundColor(Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (backgroundGraphic != null) // Checks this condition before continuing.
            backgroundGraphic.color = color; // Updates this value.
    } // Ends this code block.

    private void ApplyPersistedRoomState() // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom || photonView.ViewID <= 0 || PhotonNetwork.CurrentRoom == null || input == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties; // Creates a local value used by the following logic.

        string textKey = BuildTextKey(); // Creates a local value used by the following logic.
        if (properties.ContainsKey(textKey)) // Checks this condition before continuing.
        { // Starts this code block.
            suppressCallbacks = true; // Updates this value.
            input.text = properties[textKey] as string ?? string.Empty; // Updates this value.
            suppressCallbacks = false; // Updates this value.
        } // Ends this code block.

        string colorKey = BuildColorKey(); // Creates a local value used by the following logic.
        if (properties.ContainsKey(colorKey) && TryDecodeColor(properties[colorKey], out Color color)) // Checks this condition before continuing.
            ApplyBackgroundColor(color); // Runs this line of puzzle logic.
    } // Ends this code block.

    private string BuildTextKey() => $"{TextPropertyPrefix}{photonView.ViewID}"; // Declares a method used by this script.
    private string BuildColorKey() => $"{ColorPropertyPrefix}{photonView.ViewID}"; // Declares a method used by this script.

    private static float[] EncodeColor(Color color) => new[] { color.r, color.g, color.b, color.a }; // Declares a method used by this script.

    private static bool TryDecodeColor(object value, out Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (value is float[] rgba && rgba.Length >= 4) // Checks this condition before continuing.
        { // Starts this code block.
            color = new Color(rgba[0], rgba[1], rgba[2], rgba[3]); // Creates a local value used by the following logic.
            return true; // Returns this result to the caller.
        } // Ends this code block.

        if (value is Color storedColor) // Checks this condition before continuing.
        { // Starts this code block.
            color = storedColor; // Creates a local value used by the following logic.
            return true; // Returns this result to the caller.
        } // Ends this code block.

        color = Color.white; // Creates a local value used by the following logic.
        return false; // Returns this result to the caller.
    } // Ends this code block.
} // Ends this code block.
