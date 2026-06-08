using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.EventSystems; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using UnityEngine.UI; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.

[RequireComponent(typeof(PhotonView))] // Adds an attribute that changes how Unity or C# treats the next declaration.
public class LetterCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler // Declares this type.
{ // Starts this code block.
    public enum ResultVisualState // Declares this type.
    { // Starts this code block.
        None = 0, // Updates this value.
        Correct = 1, // Updates this value.
        Wrong = 2 // Updates this value.
    } // Ends this code block.

    [Header("UI")] // Adds a labeled section in the Unity Inspector.
    public TMP_Text letterText; // Declares a field, property, or member used by this script.
    public Image backgroundImage; // Declares a field, property, or member used by this script.

    [HideInInspector] public Vector2Int gridPosition; // Adds an attribute that changes how Unity or C# treats the next declaration.
    [HideInInspector] public bool IsPlayable { get; private set; } // Adds an attribute that changes how Unity or C# treats the next declaration.

    private WordSearchManager game; // Declares a field, property, or member used by this script.

    // Visual state
    private bool isSelected = false; // Declares a field, property, or member used by this script.
    private ResultVisualState resultState = ResultVisualState.None; // Declares a field, property, or member used by this script.

    // Photon
    private PhotonView pv; // Declares a field, property, or member used by this script.

    // Prevent feedback loops when applying remote updates
    private bool suppressNet; // Declares a field, property, or member used by this script.

    // ---- Init from manager ----
    public void Init(WordSearchManager mgr, Vector2Int pos) // Declares a method used by this script.
    { // Starts this code block.
        game = mgr; // Updates this value.
        gridPosition = pos; // Updates this value.
    } // Ends this code block.

    private void Awake() // Declares a method used by this script.
    { // Starts this code block.
        pv = GetComponent<PhotonView>(); // Updates this value.
    } // Ends this code block.

    public void SetPlayable(bool playable) // Declares a method used by this script.
    { // Starts this code block.
        IsPlayable = playable; // Updates this value.
        if (backgroundImage != null) backgroundImage.raycastTarget = playable; // Checks this condition before continuing.

        // (Optional) If you want playable state synced too, uncomment:
        // if (pv != null && PhotonNetwork.InRoom && !suppressNet)
        //     pv.RPC(nameof(RPC_SetPlayable), RpcTarget.Others, playable);
    } // Ends this code block.

    // ---- Content ----
    public void SetLetter(char c) => letterText.text = c.ToString(); // Declares a method used by this script.
    public void SetLetterString(string s) => letterText.text = s; // Declares a method used by this script.
    public char GetLetter() => letterText.text.Length > 0 ? letterText.text[0] : ' '; // Declares a method used by this script.

    // ---- Visuals ----
    public bool IsSelected => isSelected; // Declares a field, property, or member used by this script.
    public ResultVisualState CurrentResultState => resultState; // Declares a field, property, or member used by this script.

    public void ApplySharedState(bool selected, ResultVisualState result) // Declares a method used by this script.
    { // Starts this code block.
        suppressNet = true; // Updates this value.

        isSelected = selected; // Updates this value.
        resultState = result; // Updates this value.

        if (backgroundImage != null) // Checks this condition before continuing.
        { // Starts this code block.
            switch (resultState) // Chooses behavior based on this value.
            { // Starts this code block.
                case ResultVisualState.Correct: // Handles this switch case.
                    backgroundImage.color = Color.green; // Updates this value.
                    break; // Exits the current loop or switch.
                case ResultVisualState.Wrong: // Handles this switch case.
                    backgroundImage.color = Color.red; // Updates this value.
                    break; // Exits the current loop or switch.
                default: // Handles any unmatched switch case.
                    backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white; // Updates this value.
                    break; // Exits the current loop or switch.
            } // Ends this code block.
        } // Ends this code block.

        suppressNet = false; // Updates this value.
    } // Ends this code block.

    public void SetSelectedVisual(bool selected) // Declares a method used by this script.
    { // Starts this code block.
        isSelected = selected; // Updates this value.
        if (resultState != ResultVisualState.None) return; // keep green/red until cleared
        if (backgroundImage != null) // Checks this condition before continuing.
            backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white; // yellow or white

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet) // Checks this condition before continuing.
        { // Starts this code block.
            pv.RPC(nameof(RPC_SetSelectedVisual), RpcTarget.Others, selected); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public void SetResultCorrect() // Declares a method used by this script.
    { // Starts this code block.
        resultState = ResultVisualState.Correct; // Updates this value.
        if (backgroundImage != null) backgroundImage.color = Color.green; // Checks this condition before continuing.

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet) // Checks this condition before continuing.
        { // Starts this code block.
            pv.RPC(nameof(RPC_SetResultCorrect), RpcTarget.Others); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public void SetResultWrong() // Declares a method used by this script.
    { // Starts this code block.
        resultState = ResultVisualState.Wrong; // Updates this value.
        if (backgroundImage != null) backgroundImage.color = Color.red; // Checks this condition before continuing.

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet) // Checks this condition before continuing.
        { // Starts this code block.
            pv.RPC(nameof(RPC_SetResultWrong), RpcTarget.Others); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public void ClearResult() // Declares a method used by this script.
    { // Starts this code block.
        resultState = ResultVisualState.None; // Updates this value.
        // Re-apply selection coloring
        if (backgroundImage != null) // Checks this condition before continuing.
            backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white; // Updates this value.

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet) // Checks this condition before continuing.
        { // Starts this code block.
            pv.RPC(nameof(RPC_ClearResult), RpcTarget.Others); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public void DisableCellUI() // Declares a method used by this script.
    { // Starts this code block.
        if (backgroundImage != null) // Checks this condition before continuing.
        { // Starts this code block.
            backgroundImage.enabled = false; // Updates this value.
            backgroundImage.raycastTarget = false; // Updates this value.
        } // Ends this code block.

        // ✅ Sync to others
        if (pv != null && PhotonNetwork.InRoom && !suppressNet) // Checks this condition before continuing.
        { // Starts this code block.
            pv.RPC(nameof(RPC_DisableCellUI), RpcTarget.Others); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    public void SetTextColor(Color color) // Declares a method used by this script.
    { // Starts this code block.
        if (letterText != null) letterText.color = color; // Checks this condition before continuing.

        // ✅ Sync to others (send floats because Color is not always safe in RPC across setups)
        if (pv != null && PhotonNetwork.InRoom && !suppressNet) // Checks this condition before continuing.
        { // Starts this code block.
            pv.RPC(nameof(RPC_SetTextColor), RpcTarget.Others, color.r, color.g, color.b, color.a); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.

    // ─────────────────────────────
    // RPC RECEIVERS (remote applies)
    // ─────────────────────────────
    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetSelectedVisual(bool selected) // Declares a method used by this script.
    { // Starts this code block.
        suppressNet = true; // Updates this value.
        SetSelectedVisual_NoNet(selected); // Runs this line of puzzle logic.
        suppressNet = false; // Updates this value.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetResultCorrect() // Declares a method used by this script.
    { // Starts this code block.
        suppressNet = true; // Updates this value.
        SetResultCorrect_NoNet(); // Runs this line of puzzle logic.
        suppressNet = false; // Updates this value.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetResultWrong() // Declares a method used by this script.
    { // Starts this code block.
        suppressNet = true; // Updates this value.
        SetResultWrong_NoNet(); // Runs this line of puzzle logic.
        suppressNet = false; // Updates this value.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_ClearResult() // Declares a method used by this script.
    { // Starts this code block.
        suppressNet = true; // Updates this value.
        ClearResult_NoNet(); // Runs this line of puzzle logic.
        suppressNet = false; // Updates this value.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_DisableCellUI() // Declares a method used by this script.
    { // Starts this code block.
        suppressNet = true; // Updates this value.
        DisableCellUI_NoNet(); // Runs this line of puzzle logic.
        suppressNet = false; // Updates this value.
    } // Ends this code block.

    [PunRPC] // Adds an attribute that changes how Unity or C# treats the next declaration.
    private void RPC_SetTextColor(float r, float g, float b, float a) // Declares a method used by this script.
    { // Starts this code block.
        suppressNet = true; // Updates this value.
        if (letterText != null) letterText.color = new Color(r, g, b, a); // Checks this condition before continuing.
        suppressNet = false; // Updates this value.
    } // Ends this code block.

    // Optional playable sync if you enable it above:
    // [PunRPC]
    // private void RPC_SetPlayable(bool playable)
    // {
    //     suppressNet = true;
    //     SetPlayable(playable);
    //     suppressNet = false;
    // }

    // ─────────────────────────────
    // Local-only helpers (same logic, no RPC)
    // ─────────────────────────────
    private void SetSelectedVisual_NoNet(bool selected) // Declares a method used by this script.
    { // Starts this code block.
        isSelected = selected; // Updates this value.
        if (resultState != ResultVisualState.None) return; // Checks this condition before continuing.
        if (backgroundImage != null) // Checks this condition before continuing.
            backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white; // Updates this value.
    } // Ends this code block.

    private void SetResultCorrect_NoNet() // Declares a method used by this script.
    { // Starts this code block.
        resultState = ResultVisualState.Correct; // Updates this value.
        if (backgroundImage != null) backgroundImage.color = Color.green; // Checks this condition before continuing.
    } // Ends this code block.

    private void SetResultWrong_NoNet() // Declares a method used by this script.
    { // Starts this code block.
        resultState = ResultVisualState.Wrong; // Updates this value.
        if (backgroundImage != null) backgroundImage.color = Color.red; // Checks this condition before continuing.
    } // Ends this code block.

    private void ClearResult_NoNet() // Declares a method used by this script.
    { // Starts this code block.
        resultState = ResultVisualState.None; // Updates this value.
        if (backgroundImage != null) // Checks this condition before continuing.
            backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white; // Updates this value.
    } // Ends this code block.

    private void DisableCellUI_NoNet() // Declares a method used by this script.
    { // Starts this code block.
        if (backgroundImage != null) // Checks this condition before continuing.
        { // Starts this code block.
            backgroundImage.enabled = false; // Updates this value.
            backgroundImage.raycastTarget = false; // Updates this value.
        } // Ends this code block.
    } // Ends this code block.

    // ---- Pointer input ----
    public void OnPointerDown(PointerEventData eventData) // Declares a method used by this script.
    { // Starts this code block.
        if (!IsPlayable || eventData.button != PointerEventData.InputButton.Left) return; // Checks this condition before continuing.
        game.BeginDrag(this); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void OnPointerEnter(PointerEventData eventData) // Declares a method used by this script.
    { // Starts this code block.
        if (!IsPlayable) return; // Checks this condition before continuing.
        if (Input.GetMouseButton(0)) // Checks this condition before continuing.
            game.DragOver(this); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void OnPointerUp(PointerEventData eventData) // Declares a method used by this script.
    { // Starts this code block.
        if (!IsPlayable || eventData.button != PointerEventData.InputButton.Left) return; // Checks this condition before continuing.
        game.EndDrag(); // Runs this line of puzzle logic.
    } // Ends this code block.
} // Ends this code block.
