using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class LetterCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    [Header("UI")]
    public TMP_Text letterText;
    public Image backgroundImage;

    [HideInInspector] public Vector2Int gridPosition;
    [HideInInspector] public bool IsPlayable { get; private set; }

    private WordSearchManager game;

    // Visual state
    private bool isSelected = false;
    private bool hasResult = false; // true when green/red applied

    // ---- Init from manager ----
    public void Init(WordSearchManager mgr, Vector2Int pos)
    {
        game = mgr;
        gridPosition = pos;
    }

    public void SetPlayable(bool playable)
    {
        IsPlayable = playable;
        if (backgroundImage != null) backgroundImage.raycastTarget = playable;
    }

    // ---- Content ----
    public void SetLetter(char c) => letterText.text = c.ToString();
    public void SetLetterString(string s) => letterText.text = s;
    public char GetLetter() => letterText.text.Length > 0 ? letterText.text[0] : ' ';

    // ---- Visuals ----
    public void SetSelectedVisual(bool selected)
    {
        isSelected = selected;
        if (hasResult) return; // keep green/red until cleared
        backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white; // yellow or white
    }

    public void SetResultCorrect()
    {
        hasResult = true;
        backgroundImage.color = Color.green;
    }

    public void SetResultWrong()
    {
        hasResult = true;
        backgroundImage.color = Color.red;
    }

    public void ClearResult()
    {
        hasResult = false;
        // Re-apply selection coloring
        backgroundImage.color = isSelected ? new Color(1f, 0.95f, 0.4f) : Color.white;
    }

    public void DisableCellUI()
    {
        backgroundImage.enabled = false;
        backgroundImage.raycastTarget = false;
    }

    public void SetTextColor(Color color) => letterText.color = color;

    // ---- Pointer input ----
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPlayable || eventData.button != PointerEventData.InputButton.Left) return;
        game.BeginDrag(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsPlayable) return;
        if (Input.GetMouseButton(0))
            game.DragOver(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsPlayable || eventData.button != PointerEventData.InputButton.Left) return;
        game.EndDrag();
    }
}
