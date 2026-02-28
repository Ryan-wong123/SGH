using UnityEngine;
using Photon.Pun;

public class ClosePanelAndResetCamera : MonoBehaviour
{

    public void OnCloseClicked()
    {

        if (PlayerController.Local != null)
            PlayerController.Local.ResetCameraView();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

}