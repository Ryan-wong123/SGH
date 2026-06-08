using UnityEngine; // Imports a namespace used by this script.
using UnityEngine.Events; // Imports a namespace used by this script.
using UnityEngine.EventSystems; // Imports a namespace used by this script.

public class InputClickButton : MonoBehaviour, IPointerClickHandler // Declares this type.
{ // Starts this code block.

    [Header("Trigger Button")] // Adds a labeled section in the Unity Inspector.
    public PointerEventData.InputButton triggerButton = PointerEventData.InputButton.Left; // Declares a field, property, or member used by this script.

    [Header("What to call when triggered")] // Adds a labeled section in the Unity Inspector.
    public UnityEvent onTriggered; // no params, fully generic

    public void OnPointerClick(PointerEventData eventData) // Declares a method used by this script.
    { // Starts this code block.
        if (eventData.button == triggerButton) // Checks this condition before continuing.
        { // Starts this code block.
            onTriggered.Invoke(); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.
} // Ends this code block.
