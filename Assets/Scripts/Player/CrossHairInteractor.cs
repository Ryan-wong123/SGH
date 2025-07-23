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
            HandleInteractable(hit);
            if (Input.GetMouseButtonDown(0)) // LMB
            {
                // Try to press keypad button
                var button = hit.collider.GetComponent<NavKeypad.KeypadButton>();
                if (button != null)
                {
                    button.PressButton();
                    return; // stop here if we clicked a button
                }
            }
        }
        else
        {
            ClearInteraction();
        }
    }

    private void HandleInteractable(RaycastHit hit)
    {
        IInteractable interactable = hit.collider.GetComponent<IInteractable>();
        IHighlightable highlightable = hit.collider.GetComponent<IHighlightable>();

        if (interactable != null)
        {
            if (currentTarget != interactable)
            {
                currentTarget = interactable;

                if (interactionLabel != null)
                {
                    interactionLabel.gameObject.SetActive(true);
                    interactionLabel.text = interactable.GetInteractionPrompt();
                }

                // Update highlight
                if (currentHighlight != highlightable)
                {
                    currentHighlight?.Unhighlight();
                    currentHighlight = highlightable;
                    currentHighlight?.Highlight();
                }
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