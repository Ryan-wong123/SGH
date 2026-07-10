using System.Collections; // Imports IEnumerator for Unity coroutines.
using UnityEngine; // Imports Unity engine types such as MonoBehaviour, Vector3, Mathf, Time, and Debug.

namespace NavKeypad // Groups keypad classes under the NavKeypad namespace.
{ // Opens the NavKeypad namespace body.
    public class KeypadButton : MonoBehaviour // Defines a physical keypad button component.
    { // Opens the KeypadButton class body.
        [Header("Value")] // Groups the button value field in the Inspector.
        [SerializeField] private string value; // Stores the value sent to the keypad when this button is pressed.

        [Header("Button Animation Settings")] // Groups button animation fields in the Inspector.
        [SerializeField] private float bttnspeed = 0.1f; // Stores how long each press or release movement takes.
        [SerializeField] private float moveDist = 0.0025f; // Stores how far the button moves when pressed.
        [SerializeField] private float buttonPressedTime = 0.1f; // Stores how long the button stays pressed before releasing.

        [Header("Component References")] // Groups component reference fields in the Inspector.
        [SerializeField] private Keypad keypad; // Stores the keypad that receives this button's value.

        private bool moving; // Tracks whether the button press animation is currently running.

        public void PressButton() // Handles a player pressing this keypad button.
        { // Opens the PressButton method body.
            if (moving) return; // Ignores presses while the button animation is already running.
            if (keypad == null) // Checks whether this button has no keypad assigned.
            { // Opens the missing-keypad block.
                Debug.LogWarning($"{name}: Keypad reference missing."); // Logs a warning that the button cannot send input.
                return; // Stops because there is no keypad to receive the value.
            } // Closes the missing-keypad block.

            keypad.AddInput(value); // Sends this button's value to the keypad.
            StartCoroutine(MoveSmooth()); // Starts the button press animation.
        } // Closes the PressButton method body.

        private IEnumerator MoveSmooth() // Animates the button moving in and then back out.
        { // Opens the MoveSmooth coroutine body.
            moving = true; // Marks the button as currently animating.

            Vector3 startPos = transform.localPosition; // Stores the button's starting local position.
            Vector3 endPos = transform.localPosition + new Vector3(0, 0, moveDist); // Calculates the pressed-in local position.

            float elapsedTime = 0; // Resets the press-in animation timer.
            while (elapsedTime < bttnspeed) // Continues until the press-in animation duration has elapsed.
            { // Opens the press-in loop body.
                elapsedTime += Time.deltaTime; // Advances the timer by the current frame time.
                float t = Mathf.Clamp01(elapsedTime / bttnspeed); // Converts elapsed time into a clamped interpolation value.
                transform.localPosition = Vector3.Lerp(startPos, endPos, t); // Moves the button toward the pressed-in position.
                yield return null; // Waits one frame before continuing the animation.
            } // Closes the press-in loop body.

            transform.localPosition = endPos; // Snaps exactly to the pressed-in position.
            yield return new WaitForSeconds(buttonPressedTime); // Keeps the button pressed for the configured time.

            startPos = transform.localPosition; // Stores the pressed-in position as the release animation start.
            endPos = transform.localPosition - new Vector3(0, 0, moveDist); // Calculates the released local position.

            elapsedTime = 0; // Resets the release animation timer.
            while (elapsedTime < bttnspeed) // Continues until the release animation duration has elapsed.
            { // Opens the release loop body.
                elapsedTime += Time.deltaTime; // Advances the timer by the current frame time.
                float t = Mathf.Clamp01(elapsedTime / bttnspeed); // Converts elapsed time into a clamped interpolation value.
                transform.localPosition = Vector3.Lerp(startPos, endPos, t); // Moves the button back toward the released position.
                yield return null; // Waits one frame before continuing the animation.
            } // Closes the release loop body.

            transform.localPosition = endPos; // Snaps exactly to the released position.
            moving = false; // Marks the button as ready to be pressed again.
        } // Closes the MoveSmooth coroutine body.
    } // Closes the KeypadButton class body.
} // Closes the NavKeypad namespace body.
