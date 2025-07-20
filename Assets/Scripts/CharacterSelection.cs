using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button[] characterButtons;

    [Header("Highlight Colors")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color normalColor = Color.white;

    private int currentSelectedIndex = -1;

    public void SetCharacter(int characterIndex)
    {
        Hashtable properties = new Hashtable
        {
            { "CharacterIndex", characterIndex }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        Debug.Log("Selected Character Index: " + characterIndex);
        UpdateButtonColors(characterIndex);
    }

    private void UpdateButtonColors(int selectedIndex)
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            var image = characterButtons[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }

        currentSelectedIndex = selectedIndex;
    }
}
