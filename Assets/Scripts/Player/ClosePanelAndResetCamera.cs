using UnityEngine; // Gives this script access to Unity engine types like MonoBehaviour, Cursor, and CursorLockMode.
using Photon.Pun; // Gives this script access to Photon PUN networking types if needed by this component.

public class ClosePanelAndResetCamera : MonoBehaviour // Defines a Unity component that can close a panel and restore player camera control.
{ // Starts the ClosePanelAndResetCamera class body.

    public void OnCloseClicked() // Runs when the close button is clicked.
    { // Starts the close-click handler.
        if (SlideshowUI.ShouldSuppressLegacyCloseHandlers()) // Checks whether newer slideshow UI logic should handle the close action instead.
            return; // Stops here so the legacy close behavior does not also run.

        ResetCameraAndCursor(); // Restores the camera view and locks/hides the cursor for gameplay.
    } // Ends the close-click handler.

    public static void ResetCameraAndCursor() // Provides shared logic for resetting the local camera and cursor state.
    { // Starts the camera-and-cursor reset method.
        if (PlayerController.Local != null) // Makes sure there is a local player controller before using it.
            PlayerController.Local.ResetCameraView(); // Resets the local player's camera back to its gameplay view.

        Cursor.lockState = CursorLockMode.Locked; // Locks the cursor so mouse movement controls the camera again.
        Cursor.visible = false; // Hides the cursor during gameplay.
    } // Ends the camera-and-cursor reset method.

} // Ends the ClosePanelAndResetCamera class.
