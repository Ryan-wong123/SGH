using Photon.Pun; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using Hashtable = ExitGames.Client.Photon.Hashtable; // Imports a namespace used by this script.

[RequireComponent(typeof(PhotonView))] // Adds an attribute that changes how Unity or C# treats the next declaration.
[RequireComponent(typeof(TMP_Dropdown))] // Adds an attribute that changes how Unity or C# treats the next declaration.
public class PhotonSyncedTMPDropdown : MonoBehaviourPun // Declares this type.
{ // Starts this code block.
    private const string ValuePropertyPrefix = "TMPDropdownValue_"; // Declares a field, property, or member used by this script.
    private const string ColorPropertyPrefix = "TMPDropdownColor_"; // Declares a field, property, or member used by this script.

    [SerializeField] private TMP_Dropdown dropdown; // Exposes this private field in the Unity Inspector.

    // Prevent loop when we set value from network
    private bool suppressCallbacks; // Declares a field, property, or member used by this script.
    private Graphic backgroundGraphic; // Declares a field, property, or member used by this script.

    private void Awake() // Declares a method used by this script.
    { // Starts this code block.
        if (dropdown == null) // Checks this condition before continuing.
            dropdown = GetComponent<TMP_Dropdown>(); // Updates this value.

        backgroundGraphic = dropdown != null // Updates this value.
            ? dropdown.targetGraphic as Graphic // Runs this line of puzzle logic.
            : GetComponent<Graphic>(); // Runs this line of puzzle logic.

        dropdown.onValueChanged.AddListener(OnLocalValueChanged); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnEnable() // Declares a method used by this script.
    { // Starts this code block.
        ApplyPersistedRoomState(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnDestroy() // Declares a method used by this script.
    { // Starts this code block.
        if (dropdown != null) // Checks this condition before continuing.
            dropdown.onValueChanged.RemoveListener(OnLocalValueChanged); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnLocalValueChanged(int value) // Declares a method used by this script.
    { // Starts this code block.
        if (suppressCallbacks) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        string selectedText = dropdown != null && value >= 0 && value < dropdown.options.Count
            ? dropdown.options[value].text
            : value.ToString();
        GameSessionCsvLogger.Instance?.RecordLocalPlayerInput(gameObject.name, selectedText);

        PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildValueKey()] = value // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.

        photonView.RPC(nameof(RPC_SetDropdownValue), RpcTarget.Others, value); // Runs this line of puzzle logic.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetDropdownValue(int value) // Declares a method used by this script.
    { // Starts this code block.
        value = Mathf.Clamp(value, 0, dropdown.options.Count - 1); // Updates this value.

        suppressCallbacks = true; // Updates this value.
        dropdown.value = value; // Updates this value.
        dropdown.RefreshShownValue(); // Runs this line of puzzle logic.
        suppressCallbacks = false; // Updates this value.
    } // Ends this code block.

    public void SetValueAndSync(int value) // Declares a method used by this script.
    { // Starts this code block.
        value = Mathf.Clamp(value, 0, dropdown.options.Count - 1); // Updates this value.

        suppressCallbacks = true; // Updates this value.
        dropdown.value = value; // Updates this value.
        dropdown.RefreshShownValue(); // Runs this line of puzzle logic.
        suppressCallbacks = false; // Updates this value.

        PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildValueKey()] = value // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.

        photonView.RPC(nameof(RPC_SetDropdownValue), RpcTarget.Others, value); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void SetBackgroundColorAndSync(Color color) // Declares a method used by this script.
    { // Starts this code block.
        ApplyBackgroundColor(color); // Runs this line of puzzle logic.

        if (!PhotonNetwork.InRoom) // Checks this condition before continuing.
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
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || dropdown == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties; // Creates a local value used by the following logic.

        string valueKey = BuildValueKey(); // Creates a local value used by the following logic.
        if (properties.ContainsKey(valueKey) && properties[valueKey] is int value) // Checks this condition before continuing.
        { // Starts this code block.
            value = Mathf.Clamp(value, 0, dropdown.options.Count - 1); // Updates this value.

            suppressCallbacks = true; // Updates this value.
            dropdown.value = value; // Updates this value.
            dropdown.RefreshShownValue(); // Runs this line of puzzle logic.
            suppressCallbacks = false; // Updates this value.
        } // Ends this code block.

        string colorKey = BuildColorKey(); // Creates a local value used by the following logic.
        if (properties.ContainsKey(colorKey) && TryDecodeColor(properties[colorKey], out Color color)) // Checks this condition before continuing.
            ApplyBackgroundColor(color); // Runs this line of puzzle logic.
    } // Ends this code block.

    private string BuildValueKey() => $"{ValuePropertyPrefix}{photonView.ViewID}"; // Declares a method used by this script.
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
