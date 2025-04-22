using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;

    private CharacterController controller;
    private Animator animator;

    [SerializeField] private float verticalRotation = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (photonView.IsMine)
        {
            //fix camera jitter on local player
            PhotonTransformView photonTransform = GetComponent<PhotonTransformView>();
            if (photonTransform != null)
            {
                photonTransform.enabled = false;
            }

            playerCamera.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            cameraRoot.gameObject.SetActive(false); // hide other players' cameras
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        HandleMovement();
        HandleCameraLook();
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = cameraRoot.forward;
        Vector3 right = cameraRoot.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (forward * v + right * h) * moveSpeed;
        controller.SimpleMove(moveDir);

        bool isMoving = moveDir.magnitude > 0.1f;
        animator.SetBool("isWalking", isMoving);
    }

    void HandleCameraLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -85f, 85f);

        cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        transform.Rotate(Vector3.up * mouseX);
    }
}
