using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TMP_Text tipText;

    [Header("Config")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private string[] loadingTips;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        loadingPanel.SetActive(false);
    }

    public void StartFadeAndLoad(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        loadingPanel.SetActive(true);
        tipText.text = loadingTips[Random.Range(0, loadingTips.Length)];
        yield return StartCoroutine(FadeIn());

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(sceneName);

        // While loading, show progress bar
        while (PhotonNetwork.LevelLoadingProgress < 1f)
        {
            progressBarFill.fillAmount = PhotonNetwork.LevelLoadingProgress;
            yield return null;
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null) return;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        loadingPanel.SetActive(false); // hide loading UI when done
    }
}
