using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrosswordCell : MonoBehaviour
{
    public TMP_InputField inputField;
    public Vector2Int position;
    public bool isActive;

    public void SetLetter(char c)
    {
        inputField.text = c.ToString();
    }

    public char GetLetter()
    {
        return string.IsNullOrEmpty(inputField.text) ? ' ' : inputField.text[0];
    }

    public void SetInteractable(bool interactable)
    {
        inputField.interactable = interactable;
        inputField.GetComponent<Image>().enabled = interactable;
    }

}
