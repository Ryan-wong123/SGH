using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class CharacterSelection : MonoBehaviourPunCallbacks
{
    public void SetCharacter(int characterIndex)
    {
        Hashtable properties = new Hashtable
        {
            { "CharacterIndex", characterIndex }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
        Debug.Log("Selected Character Index: " + characterIndex);
    }
}
