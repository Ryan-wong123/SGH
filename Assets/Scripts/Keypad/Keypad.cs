using Photon.Pun;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace NavKeypad
{
    public class Keypad : MonoBehaviourPun
    {
        [Header("Events")]
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
        [SerializeField] private Color screenNormalColor = new Color(0.98f, 0.50f, 0.032f, 1f); //orangy
        [SerializeField] private Color screenDeniedColor = new Color(1f, 0f, 0f, 1f); //red
        [SerializeField] private Color screenGrantedColor = new Color(0f, 0.62f, 0.07f); //greenish
        [Header("SoundFx")]
        [SerializeField] private AudioClip buttonClickedSfx;
        [SerializeField] private AudioClip accessDeniedSfx;
        [SerializeField] private AudioClip accessGrantedSfx;
        [Header("Component References")]
        [SerializeField] private Renderer panelMesh;
        [SerializeField] private TMP_Text keypadDisplayText;
        [SerializeField] private AudioSource audioSource;


        private string currentInput;
        private bool displayingResult = false;
        private bool accessWasGranted = false;

        [SerializeField] private string keyName;
        [SerializeField] private PhotonView photonView;

        private void Awake()
        {

            ClearInput();
            panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity);
        }


        //Gets value from pressedbutton
        public void AddInput(string input)
        {
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
                    if (currentInput != null && currentInput.Length == 9) // 9 max passcode size 
                    {
                        return;
                    }
                    currentInput += input;
                    keypadDisplayText.text = currentInput;
                    break;
            }

        }
        private void Backspace()
        {
            if (!string.IsNullOrEmpty(currentInput))
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                keypadDisplayText.text = currentInput;
            }
        }

        public void CheckCombo()
        {
            if (int.TryParse(currentInput, out var currentKombo))
            {
                bool granted = currentKombo == keypadCombo;
                if (!displayingResult)
                {
                    StartCoroutine(DisplayResultRoutine(granted));
                }
            }
            else
            {
                Debug.LogWarning("Couldn't process input for some reason..");
            }

        }

        //mainly for animations 
        private IEnumerator DisplayResultRoutine(bool granted)
        {
            displayingResult = true;

            if (granted) AccessGranted();
            else AccessDenied();

            yield return new WaitForSeconds(displayResultTime);
            displayingResult = false;
            if (granted) yield break;
            ClearInput();
            panelMesh.material.SetVector("_EmissionColor", screenNormalColor * screenIntensity);

        }

        private void AccessDenied()
        {
            keypadDisplayText.text = accessDeniedText;
            panelMesh.material.SetVector("_EmissionColor", screenDeniedColor * screenIntensity);
            audioSource.PlayOneShot(accessDeniedSfx);
        }

        private void ClearInput()
        {
            currentInput = "";
            keypadDisplayText.text = currentInput;
        }

        private void AccessGranted()
        {
            accessWasGranted = true;
            keypadDisplayText.text = accessGrantedText;
            panelMesh.material.SetVector("_EmissionColor", screenGrantedColor * screenIntensity);
            audioSource.PlayOneShot(accessGrantedSfx);

            Debug.Log("access granted");

            photonView.RPC(nameof(RPC_AddSharedInventoryItem), RpcTarget.AllBuffered, keyName);
        }

        [PunRPC]
        public void RPC_AddSharedInventoryItem(string itemName)
        {
            StartCoroutine(DelayedAddItem(itemName));
        }

        IEnumerator DelayedAddItem(string itemName)
        {
            while (InventorySystem.Instance == null || InventoryUIManager.Instance == null)
                yield return null;

            Debug.Log($"added {itemName}");
            InventorySystem.Instance.AddItem(itemName);
            InventoryUIManager.Instance.UpdateInventoryUI();
        }
    }
}