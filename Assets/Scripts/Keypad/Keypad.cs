using Photon.Pun; // Imports Photon networking types such as MonoBehaviourPun, PhotonNetwork, and PunRPC.
using System.Collections; // Imports IEnumerator for Unity coroutines.
using TMPro; // Imports TextMeshPro text types such as TMP_Text.
using UnityEngine; // Imports Unity engine types such as Color, Renderer, AudioClip, AudioSource, and Debug.

namespace NavKeypad // Groups keypad classes under the NavKeypad namespace.
{ // Opens the NavKeypad namespace body.
    public class Keypad : MonoBehaviourPun // Defines a networked keypad controller.
    { // Opens the Keypad class body.
        [Header("Combination Code (9 Numbers Max)")] // Groups the keypad combination field in the Inspector.
        [SerializeField] private int keypadCombo = 12345; // Stores the numeric code that grants access.

        [Header("Settings")] // Groups result text settings in the Inspector.
        [SerializeField] private string accessGrantedText = "Granted"; // Stores the text shown when the code is correct.
        [SerializeField] private string accessDeniedText = "Denied"; // Stores the text shown when the code is wrong.

        [Header("Visuals")] // Groups display visual settings in the Inspector.
        [SerializeField] private float displayResultTime = 1f; // Stores how long the granted or denied result stays on screen.
        [Range(0, 5)] // Restricts the screen intensity slider range in the Inspector.
        [SerializeField] private float screenIntensity = 2.5f; // Stores the emission brightness multiplier for the keypad screen.

        [Header("Colors")] // Groups screen color settings in the Inspector.
        [SerializeField] private Color screenNormalColor = new Color(0.98f, 0.50f, 0.032f, 1f); // Stores the normal screen emission color.
        [SerializeField] private Color screenDeniedColor = new Color(1f, 0f, 0f, 1f); // Stores the denied screen emission color.
        [SerializeField] private Color screenGrantedColor = new Color(0f, 0.62f, 0.07f); // Stores the granted screen emission color.

        [Header("SoundFx")] // Groups keypad sound effect fields in the Inspector.
        [SerializeField] private AudioClip buttonClickedSfx; // Stores the sound played when any keypad button is clicked.
        [SerializeField] private AudioClip accessDeniedSfx; // Stores the sound played when access is denied.
        [SerializeField] private AudioClip accessGrantedSfx; // Stores the sound played when access is granted.

        [Header("Component References")] // Groups required component references in the Inspector.
        [SerializeField] private Renderer panelMesh; // Stores the renderer used to change the keypad screen emission.
        [SerializeField] private TMP_Text keypadDisplayText; // Stores the text component used for keypad input and results.
        [SerializeField] private AudioSource audioSource; // Stores the audio source used to play keypad sound effects.

        [Header("Unlock Target")] // Groups door unlock targets in the Inspector.
        [SerializeField] private DoorInteractable[] doorsToUnlock; // Stores every door that should unlock when access is granted.

        [Header("Shared Inventory Key (optional)")] // Groups the optional shared inventory key field in the Inspector.
        [SerializeField] private string keyName; // Stores the optional inventory item name granted after successful access.

        private string currentInput; // Stores the digits currently entered into the keypad.
        private bool displayingResult = false; // Tracks whether the keypad is currently showing a granted or denied result.
        private bool accessWasGranted = false; // Tracks whether access has already been granted.

        private void Awake() // Runs Unity initialization before the first frame.
        { // Opens the Awake method body.
            ClearInput(); // Clears the keypad input and display at startup.
            if (panelMesh != null) // Checks whether the screen renderer is assigned.
                panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity); // Sets the keypad screen to the normal emission color.
        } // Closes the Awake method body.

        public void AddInput(string input) // Receives input from a keypad button.
        { // Opens the AddInput method body.
            if (audioSource != null && buttonClickedSfx != null) // Checks whether a click sound can be played.
                audioSource.PlayOneShot(buttonClickedSfx); // Plays the keypad button click sound.

            if (displayingResult || accessWasGranted) return; // Ignores input while showing a result or after access is granted.

            switch (input) // Branches based on the keypad button value.
            { // Opens the switch body.
                case "enter": // Handles the enter button.
                    CheckCombo(); // Checks whether the entered code is correct.
                    break; // Stops processing this switch case.
                case "back": // Handles the backspace button.
                    Backspace(); // Removes the last entered digit.
                    break; // Stops processing this switch case.
                default: // Handles number or other normal input buttons.
                    if (currentInput != null && currentInput.Length == 9) return; // Stops when the input already has the maximum of nine characters.
                    currentInput += input; // Appends the pressed button value to the current input.
                    if (keypadDisplayText != null) keypadDisplayText.text = currentInput; // Updates the keypad display text if assigned.
                    break; // Stops processing this switch case.
            } // Closes the switch body.
        } // Closes the AddInput method body.

        private void Backspace() // Removes the last character from the keypad input.
        { // Opens the Backspace method body.
            if (!string.IsNullOrEmpty(currentInput)) // Checks whether there is input to remove.
            { // Opens the non-empty input block.
                currentInput = currentInput.Substring(0, currentInput.Length - 1); // Removes the final character from the input string.
                if (keypadDisplayText != null) keypadDisplayText.text = currentInput; // Updates the keypad display text if assigned.
            } // Closes the non-empty input block.
        } // Closes the Backspace method body.

        public void CheckCombo() // Checks the current input against the configured keypad code.
        { // Opens the CheckCombo method body.
            if (!int.TryParse(currentInput, out var currentKombo)) // Attempts to convert the entered input into an integer.
            { // Opens the invalid-input block.
                Debug.LogWarning("Couldn't process keypad input."); // Logs a warning when the input cannot be parsed.
                return; // Stops because the code cannot be checked.
            } // Closes the invalid-input block.

            bool granted = currentKombo == keypadCombo; // Stores whether the entered code matches the configured code.

            if (displayingResult) return; // Stops if a result is already being displayed.

            if (PhotonNetwork.IsConnected) // Checks whether the game is connected to Photon.
            { // Opens the networked result block.
                // Master decides and broadcasts result to prevent repeated grant requests. // Explains why the check is routed through the master client.
                photonView.RPC(nameof(RPC_RequestCheck), RpcTarget.MasterClient, granted); // Sends the requested result to the master client.
            } // Closes the networked result block.
            else // Handles offline play.
            { // Opens the offline result block.
                StartCoroutine(DisplayResultRoutine(granted)); // Shows the result locally.
            } // Closes the offline result block.
        } // Closes the CheckCombo method body.

        [PunRPC] // Allows Photon to call this method through RPC.
        private void RPC_RequestCheck(bool granted, PhotonMessageInfo info) // Receives a keypad result request on the master client.
        { // Opens the result request RPC method body.
            if (!PhotonNetwork.IsMasterClient) return; // Ignores the request if this client is not the master client.

            // If already granted, ignore further requests. // Explains the repeat-grant guard.
            if (accessWasGranted) return; // Stops if access has already been granted.

            photonView.RPC(nameof(RPC_ShowResult), RpcTarget.AllBuffered, granted); // Broadcasts the result to all current and future clients.
        } // Closes the result request RPC method body.

        [PunRPC] // Allows Photon to call this method through RPC.
        private void RPC_ShowResult(bool granted) // Receives the granted or denied result from the network.
        { // Opens the show result RPC method body.
            if (!displayingResult) // Checks whether no result is already being displayed.
                StartCoroutine(DisplayResultRoutine(granted)); // Starts the result display routine.
        } // Closes the show result RPC method body.

        private IEnumerator DisplayResultRoutine(bool granted) // Shows the granted or denied state for a short duration.
        { // Opens the result display coroutine body.
            displayingResult = true; // Marks that the keypad is currently displaying a result.

            if (granted) AccessGranted(); // Runs granted behavior when the code was correct.
            else AccessDenied(); // Runs denied behavior when the code was wrong.

            yield return new WaitForSeconds(displayResultTime); // Waits for the configured result display duration.

            displayingResult = false; // Marks that the result display has finished.

            if (granted) yield break; // Stops without clearing the display when access was granted.

            ClearInput(); // Clears the denied input after the result display ends.
            if (panelMesh != null) // Checks whether the screen renderer is assigned.
                panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity); // Restores the normal screen emission color.
        } // Closes the result display coroutine body.

        private void AccessDenied() // Applies the denied result visuals and sound.
        { // Opens the AccessDenied method body.
            if (keypadDisplayText != null) keypadDisplayText.text = accessDeniedText; // Shows the denied text on the keypad display.
            if (panelMesh != null) panelMesh.material.SetVector("_EmissionColor", screenDeniedColor * screenIntensity); // Sets the screen emission to the denied color.
            if (audioSource != null && accessDeniedSfx != null) audioSource.PlayOneShot(accessDeniedSfx); // Plays the denied sound effect if assigned.
        } // Closes the AccessDenied method body.

        private void ClearInput() // Clears the current keypad input.
        { // Opens the ClearInput method body.
            currentInput = ""; // Resets the input string to empty.
            if (keypadDisplayText != null) keypadDisplayText.text = currentInput; // Updates the keypad display text if assigned.
        } // Closes the ClearInput method body.

        private void AccessGranted() // Applies the granted result, unlocks targets, and optionally grants an inventory item.
        { // Opens the AccessGranted method body.
            accessWasGranted = true; // Records that access has been granted.

            if (keypadDisplayText != null) keypadDisplayText.text = accessGrantedText; // Shows the granted text on the keypad display.
            if (panelMesh != null) panelMesh.material.SetVector("_EmissionColor", screenGrantedColor * screenIntensity); // Sets the screen emission to the granted color.
            if (audioSource != null && accessGrantedSfx != null) audioSource.PlayOneShot(accessGrantedSfx); // Plays the granted sound effect if assigned.

            Debug.Log("access granted"); // Logs successful keypad access.

            // Unlock/open door for everyone through each DoorInteractable. // Explains the door unlock loop.
            // Unlock/open all doors linked to this keypad. // Notes that all configured doors are processed.
            if (doorsToUnlock != null) // Checks whether any door targets are assigned.
            { // Opens the door-target array block.
                foreach (var d in doorsToUnlock) // Loops through each configured door target.
                { // Opens the door loop body.
                    if (d != null) // Checks whether this door reference is assigned.
                        d.UnlockFromKeypad(); // Unlocks the door through its keypad API.
                } // Closes the door loop body.
            } // Closes the door-target array block.


            // Optional: grant shared key item for everyone. // Explains the optional shared inventory grant.
            if (!string.IsNullOrEmpty(keyName)) // Checks whether a shared inventory item name was configured.
                photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, keyName); // Broadcasts the inventory item grant to all current and future clients.
        } // Closes the AccessGranted method body.

        [PunRPC] // Allows Photon to call this method through RPC.
        public void RPC_AddSharedInventoryItem(string itemName) // Receives a request to add a shared inventory item.
        { // Opens the shared inventory RPC method body.
            StartCoroutine(DelayedAddItem(itemName)); // Starts a coroutine that waits for inventory systems before adding the item.
        } // Closes the shared inventory RPC method body.

        private IEnumerator DelayedAddItem(string itemName) // Waits until inventory services exist, then adds the item.
        { // Opens the delayed add item coroutine body.
            while (InventorySystem.Instance == null || InventoryUIManager.Instance == null) // Waits while either inventory system dependency is missing.
                yield return null; // Waits one frame before checking again.

            InventorySystem.Instance.AddItem(itemName); // Adds the shared item to the inventory system.
            InventoryUIManager.Instance.UpdateInventoryUI(); // Refreshes the inventory UI after adding the item.
        } // Closes the delayed add item coroutine body.
    } // Closes the Keypad class body.
} // Closes the NavKeypad namespace body.
