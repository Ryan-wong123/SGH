using UnityEngine; // Imports Unity engine types such as MonoBehaviour, GameObject, and Sprite.
using UnityEngine.UI; // Imports Unity UI types such as Image.

public class CardPopupManager : MonoBehaviour // Declares the component that controls the card popup UI.
{ // Starts the CardPopupManager class body.
    public static CardPopupManager Instance; // Stores the shared CardPopupManager singleton instance.

    [Header("Popup UI")] // Groups popup UI references in the Unity inspector.
    public GameObject popupPanel; // References the root popup panel GameObject.
    public Image cardImageDisplay; // References the UI image that displays the selected card sprite.

    private void Awake() // Runs before Start to initialize the singleton and popup state.
    { // Starts the Awake method body.
        if (Instance == null) // Checks whether no CardPopupManager singleton exists yet.
            Instance = this; // Registers this component as the singleton instance.
        else // Runs when another CardPopupManager already exists.
            Destroy(gameObject); // Destroys this duplicate popup manager.

        popupPanel.SetActive(false); // Hides the popup panel at startup.
    } // Ends the Awake method body.

    public void ShowCard(Sprite cardSprite) // Shows the popup with the requested card sprite.
    { // Starts the ShowCard method body.
        popupPanel.SetActive(true); // Makes the popup panel visible.
        cardImageDisplay.sprite = cardSprite; // Displays the provided card sprite in the popup image.
    } // Ends the ShowCard method body.

    public void HidePopup() // Hides the card popup panel.
    { // Starts the HidePopup method body.
        popupPanel.SetActive(false); // Makes the popup panel invisible.
    } // Ends the HidePopup method body.
} // Ends the CardPopupManager class body.
