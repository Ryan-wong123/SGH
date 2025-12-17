using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;

namespace NavKeypad
{
    public class Keypad : MonoBehaviourPun
    {
        [Header("Combination Code (9 Numbers Max)")]
        [SerializeField] private int keypadCombo = 12345;

        [Header("Settings")]
        [SerializeField] private string accessGrantedText = "Granted";
        [SerializeField] private string accessDeniedText = "Denied";

        [Header("Visuals")]
        [SerializeField] private float displayResultTime = 1f;
        [Range(0, 5)]
        [SerializeField] private float screenIntensity = 2.5f;

        [Header("Colors")]
        [SerializeField] private Color screenNormalColor = new Color(0.98f, 0.50f, 0.032f, 1f);
        [SerializeField] private Color screenDeniedColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private Color screenGrantedColor = new Color(0f, 0.62f, 0.07f);

        [Header("SoundFx")]
        [SerializeField] private AudioClip buttonClickedSfx;
        [SerializeField] private AudioClip accessDeniedSfx;
        [SerializeField] private AudioClip accessGrantedSfx;

        [Header("Component References")]
        [SerializeField] private Renderer panelMesh;
        [SerializeField] private TMP_Text keypadDisplayText;
        [SerializeField] private AudioSource audioSource;

        [Header("Unlock Target")]
        [SerializeField] private DoorInteractable[] doorsToUnlock;

        [Header("Shared Inventory Key (optional)")]
        [SerializeField] private string keyName;

        private string currentInput;
        private bool displayingResult = false;
        private bool accessWasGranted = false;

        private void Awake()
        {
            ClearInput();
            if (panelMesh != null)
                panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity);
        }

        public void AddInput(string input)
        {
            if (audioSource != null && buttonClickedSfx != null)
                audioSource.PlayOneShot(buttonClickedSfx);

            if (displayingResult || accessWasGranted) return;

            switch (input)
            {
                case "enter":
                    CheckCombo();
                    break;
                case "back":
                    Backspace();
                    break;
                default:
                    if (currentInput != null && currentInput.Length == 9) return;
                    currentInput += input;
                    if (keypadDisplayText != null) keypadDisplayText.text = currentInput;
                    break;
            }
        }

        private void Backspace()
        {
            if (!string.IsNullOrEmpty(currentInput))
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                if (keypadDisplayText != null) keypadDisplayText.text = currentInput;
            }
        }

        public void CheckCombo()
        {
            if (!int.TryParse(currentInput, out var currentKombo))
            {
                Debug.LogWarning("Couldn't process keypad input.");
                return;
            }

            bool granted = currentKombo == keypadCombo;

            if (displayingResult) return;

            if (PhotonNetwork.IsConnected)
            {
                // ✅ Master decides and broadcasts result (prevents double-grant spam)
                photonView.RPC(nameof(RPC_RequestCheck), RpcTarget.MasterClient, granted);
            }
            else
            {
                StartCoroutine(DisplayResultRoutine(granted));
            }
        }

        [PunRPC]
        private void RPC_RequestCheck(bool granted, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            // if already granted, ignore further requests
            if (accessWasGranted) return;

            photonView.RPC(nameof(RPC_ShowResult), RpcTarget.AllBuffered, granted);
        }

        [PunRPC]
        private void RPC_ShowResult(bool granted)
        {
            if (!displayingResult)
                StartCoroutine(DisplayResultRoutine(granted));
        }

        private IEnumerator DisplayResultRoutine(bool granted)
        {
            displayingResult = true;

            if (granted) AccessGranted();
            else AccessDenied();

            yield return new WaitForSeconds(displayResultTime);

            displayingResult = false;

            if (granted) yield break;

            ClearInput();
            if (panelMesh != null)
                panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity);
        }

        private void AccessDenied()
        {
            if (keypadDisplayText != null) keypadDisplayText.text = accessDeniedText;
            if (panelMesh != null) panelMesh.material.SetVector("_EmissionColor", screenDeniedColor * screenIntensity);
            if (audioSource != null && accessDeniedSfx != null) audioSource.PlayOneShot(accessDeniedSfx);
        }

        private void ClearInput()
        {
            currentInput = "";
            if (keypadDisplayText != null) keypadDisplayText.text = currentInput;
        }

        private void AccessGranted()
        {
            accessWasGranted = true;

            if (keypadDisplayText != null) keypadDisplayText.text = accessGrantedText;
            if (panelMesh != null) panelMesh.material.SetVector("_EmissionColor", screenGrantedColor * screenIntensity);
            if (audioSource != null && accessGrantedSfx != null) audioSource.PlayOneShot(accessGrantedSfx);

            Debug.Log("access granted");

            // ✅ Unlock/open door for everyone (buffered inside DoorInteractable)
            // Unlock/open all doors linked to this keypad
            if (doorsToUnlock != null)
            {
                foreach (var d in doorsToUnlock)
                {
                    if (d != null)
                        d.UnlockFromKeypad(); // (or UnlockAndOpenFromKeypad if you used that name)
                }
            }


            // ✅ Optional: grant shared key item for everyone (kept from your original)
            if (!string.IsNullOrEmpty(keyName))
                photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, keyName);
        }

        [PunRPC]
        public void RPC_AddSharedInventoryItem(string itemName)
        {
            StartCoroutine(DelayedAddItem(itemName));
        }

        private IEnumerator DelayedAddItem(string itemName)
        {
            while (InventorySystem.Instance == null || InventoryUIManager.Instance == null)
                yield return null;

            InventorySystem.Instance.AddItem(itemName);
            InventoryUIManager.Instance.UpdateInventoryUI();
        }
    }
}
