using UnityEngine;

public class PuzzleTrigger : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Puzzle Panel")]
    [SerializeField] private GameObject puzzleUIPanel;
    [SerializeField] private GameObject closePanelUI;

    private Renderer rend;
    private Material material;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float emissionIntensity = 1.5f;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            material = rend.material;
        }
    }

    public void Interact()
    {
        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
            closePanelUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Register this puzzle with the SidebarMenuManager
            FindObjectOfType<SidebarMenuManager>().SetActivePuzzle(this);
        }
    }

    public string GetInteractionPrompt()
    {
        return "Press E to start puzzle";
    }

    public void ClosePuzzle()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        puzzleUIPanel.SetActive(false); // hide this specific panel
    }

    public void Highlight()
    {
        if (material == null) return;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", highlightColor * emissionIntensity);
    }

    public void Unhighlight()
    {
        if (material == null) return;
        material.SetColor("_EmissionColor", Color.black);
    }
}
