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
    private IHighlightable currentHighlight;

    private void Start()
    {
        if (!photonView.IsMine) return;

        interactionLabel = GameObject.Find("Cross Hair Text").GetComponent<TextMeshProUGUI>();
        interactionLabel?.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            IHighlightable highlightable = hit.collider.GetComponent<IHighlightable>();

            if (interactable != null)
            {
                currentTarget = interactable;
                if (interactionLabel != null)
                {
                    interactionLabel.gameObject.SetActive(true);
                    interactionLabel.text = interactable.GetInteractionPrompt();
                }

                // Handle highlighting
                if (highlightable != null && currentHighlight != highlightable)
                {
                    if (currentHighlight != null) currentHighlight.Unhighlight();
                    currentHighlight = highlightable;
                    currentHighlight.Highlight();
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    currentTarget.Interact();
                }
            }
            else
            {
                ClearInteraction();
            }
        }
        else
        {
            ClearInteraction();
        }
    }

    private void ClearInteraction()
    {
        currentTarget = null;
        if (interactionLabel != null)
        {
            interactionLabel.gameObject.SetActive(false);
        }

        if (currentHighlight != null)
        {
            currentHighlight.Unhighlight();
            currentHighlight = null;
        }
    }

}