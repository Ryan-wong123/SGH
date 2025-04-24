using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using Photon.Pun;

public class CrosshairInteractor : MonoBehaviourPun
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private GameObject interactionUIPanel; // assign puzzle/door UI
    private TextMeshProUGUI interactionLabel; // optional: display "Press E to Interact"

    private IInteractable currentTarget;

    private void Start()
    {
        if (!photonView.IsMine) return;

        interactionLabel = GameObject.Find("Cross Hair Text").GetComponent<TextMeshProUGUI>();
        interactionLabel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                currentTarget = interactable;
                interactionLabel.gameObject.SetActive(true);
                interactionLabel.text = interactable.GetInteractionPrompt();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    currentTarget.Interact();
                }
            }
        }
        else
        {
            currentTarget = null;
            interactionLabel.gameObject.SetActive(false);
        }
    }

}