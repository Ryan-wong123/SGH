using UnityEngine;
using UnityEngine.UI;

public class CardPopupManager : MonoBehaviour
{
    public static CardPopupManager Instance;

    [Header("Popup UI")]
    public GameObject popupPanel;
    public Image cardImageDisplay;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        popupPanel.SetActive(false);
    }

    public void ShowCard(Sprite cardSprite)
    {
        popupPanel.SetActive(true);
        cardImageDisplay.sprite = cardSprite;
    }

    public void HidePopup()
    {
        popupPanel.SetActive(false);
    }
}
