using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Linq;

public class CrosshairInteractor : MonoBehaviourPun
{
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [Tooltip("Layers that contain interactable things (doors, keypad buttons, etc.).")]
    [SerializeField] private LayerMask interactableLayer = ~0; // default to everything if not set
    [Tooltip("If true, we try to click keypad buttons even if a door is slightly in front.")]
    [SerializeField] private bool preferKeypadButtons = true;

    [Header("UI")]
    [SerializeField] private GameObject interactionUIPanel; // optional container
    private TextMeshProUGUI interactionLabel;

    // State
    private IInteractable currentTarget;
    private IHighlightable currentHighlight;

    void Start()
    {
        if (!photonView.IsMine) return;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                Debug.LogError($"{name}: playerCamera is not assigned and no MainCamera found.");
        }

        var labelObj = GameObject.Find("Cross Hair Text");
        if (labelObj != null)
        {
            interactionLabel = labelObj.GetComponent<TextMeshProUGUI>();
            if (interactionLabel != null) interactionLabel.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!photonView.IsMine || playerCamera == null) return;

        var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Grab all hits so we can prioritize KeypadButton first
        var hits = Physics.RaycastAll(ray, interactDistance, interactableLayer, QueryTriggerInteraction.Collide);
        if (hits.Length == 0)
        {
            ClearInteraction();
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Resolve the best target from the hits
        var (button, interactable, highlightable, hitTransform) = ResolveBestTarget(hits, preferKeypadButtons);

        if (interactable != null || button != null)
        {
            // Update UI + highlight
            UpdateInteractionUIAndHighlight(interactable, highlightable);

            // Click (LMB) → KeypadButton first, else Interact()
            if (Input.GetMouseButtonDown(0) && button != null)
            {
                button.PressButton();
                return;
            }

            // Press E → Interact() on doors/other interactables
            if (Input.GetKeyDown(KeyCode.E) && interactable != null)
            {
                interactable.Interact();
            }
        }
        else
        {
            ClearInteraction();
        }
    }

    // ---------- Helpers ----------

    private (NavKeypad.KeypadButton button, IInteractable interactable, IHighlightable highlightable, Transform hitT)
        ResolveBestTarget(RaycastHit[] hits, bool preferButtons)
    {
        NavKeypad.KeypadButton foundButton = null;
        IInteractable foundInteractable = null;
        IHighlightable foundHighlightable = null;
        Transform hitT = null;

        // 1) If we prefer buttons, search for the nearest KeypadButton in parent chain first
        if (preferButtons)
        {
            foreach (var h in hits)
            {
                var b = h.collider.GetComponent<NavKeypad.KeypadButton>() ??
                        h.collider.GetComponentInParent<NavKeypad.KeypadButton>();
                if (b != null)
                {
                    foundButton = b;
                    // Also find highlight on the same object chain for feedback
                    foundHighlightable = h.collider.GetComponent<IHighlightable>() ??
                                         h.collider.GetComponentInParent<IHighlightable>();
                    hitT = h.collider.transform;
                    break;
                }
            }
        }

        // 2) If no button (or not preferring), resolve to nearest IInteractable in parent chain
        if (foundButton == null)
        {
            foreach (var h in hits)
            {
                var inter = h.collider.GetComponentInParent<IInteractable>();
                if (inter != null)
                {
                    foundInteractable = inter;

                    // Prefer highlight on the exact collider hit; fall back to parent
                    foundHighlightable = h.collider.GetComponent<IHighlightable>() ??
                                         h.collider.GetComponentInParent<IHighlightable>();
                    hitT = h.collider.transform;
                    break;
                }
            }
        }

        return (foundButton, foundInteractable, foundHighlightable, hitT);
    }

    private void UpdateInteractionUIAndHighlight(IInteractable interactable, IHighlightable highlightable)
    {
        // PROMPT
        if (interactable != null)
        {
            if (interactionLabel != null)
            {
                interactionLabel.gameObject.SetActive(true);
                interactionLabel.text = interactable.GetInteractionPrompt();
            }

            // Update current target tracking (for prompt refresh)
            if (!ReferenceEquals(currentTarget, interactable))
                currentTarget = interactable;
            else if (interactionLabel != null)
                interactionLabel.text = currentTarget.GetInteractionPrompt();
        }
        else
        {
            // No interactable → hide label
            if (interactionLabel != null)
                interactionLabel.gameObject.SetActive(false);

            currentTarget = null;
        }

        // HIGHLIGHT
        if (!ReferenceEquals(currentHighlight, highlightable))
        {
            currentHighlight?.Unhighlight();
            currentHighlight = highlightable;
            currentHighlight?.Highlight();
        }
    }

    private void ClearInteraction()
    {
        currentTarget = null;

        if (interactionLabel != null)
            interactionLabel.gameObject.SetActive(false);

        if (currentHighlight != null)
        {
            currentHighlight.Unhighlight();
            currentHighlight = null;
        }
    }
}
