using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;

public class GameTimerManager : MonoBehaviourPunCallbacks
{
    [Header("Timer Settings")]
    [SerializeField] private float countdownDuration = 60f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Warning UI")]
    [SerializeField] private GameObject timeWarningPanel;
    [SerializeField] private TMP_Text warningText;

    [Header("Warnings (add as many as you want)")]
    [SerializeField] private TimerWarning[] warnings;

    private float timerEndTime;
    private bool timerRunning = false;

    private RectTransform warningRect;
    private CanvasGroup warningCanvasGroup;

    // ---------------- STRUCT ----------------
    [System.Serializable]
    public struct TimerWarning
    {
        [Min(0f)] public float thresholdSeconds;
        [TextArea] public string message;
        public Color color;

        [Header("Animation")]
        [Min(0.1f)] public float slideDuration; // per-warning slide duration (flexible)

        [HideInInspector] public bool shown; // runtime only
    }
    // ----------------------------------------

    private void Awake()
    {
        SetupPhotonView();

        if (timeWarningPanel != null)
        {
            timeWarningPanel.SetActive(false);
            warningRect = timeWarningPanel.GetComponent<RectTransform>();

            warningCanvasGroup = timeWarningPanel.GetComponent<CanvasGroup>();
            if (warningCanvasGroup == null)
                warningCanvasGroup = timeWarningPanel.AddComponent<CanvasGroup>();
        }
    }

    void SetupPhotonView()
    {
        PhotonView view = GetComponent<PhotonView>();
        if (view == null) view = gameObject.AddComponent<PhotonView>();

        view.OwnershipTransfer = OwnershipOption.Fixed;
        view.Synchronization = ViewSynchronization.UnreliableOnChange;

        if (PhotonNetwork.IsConnectedAndReady && view.ViewID == 0)
        {
            PhotonNetwork.RegisterPhotonView(view);
            Debug.Log($"Registered scene object with ViewID: {view.ViewID}");
        }
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!timerRunning) return;

        float timeLeft = timerEndTime - (float)PhotonNetwork.Time;

        // Trigger warnings (master decides; everyone sees via RPC)
        if (PhotonNetwork.IsMasterClient)
        {
            CheckAndFireWarnings(timeLeft);
        }

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            timerRunning = false;
            Debug.Log("Timer ended!");
            PuzzleStateManager.Instance?.OnTimerExpired();
        }

        UpdateTimerUI(timeLeft);
    }

    private void CheckAndFireWarnings(float timeLeft)
    {
        if (warnings == null || warnings.Length == 0) return;

        for (int i = 0; i < warnings.Length; i++)
        {
            // Because TimerWarning is a struct (value type), copy -> edit -> write back
            TimerWarning w = warnings[i];

            if (!w.shown && timeLeft <= w.thresholdSeconds && timeLeft > 0f)
            {
                w.shown = true;
                warnings[i] = w;

                photonView.RPC(
                    nameof(ShowTimeWarning),
                    RpcTarget.All,
                    w.message,
                    w.color.r, w.color.g, w.color.b, w.color.a,
                    w.slideDuration
                );
            }
        }
    }

    void UpdateTimerUI(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void StartTimer()
    {
        timerEndTime = (float)PhotonNetwork.Time + countdownDuration;

        ResetWarningsLocal();

        photonView.RPC(nameof(SyncTimer), RpcTarget.AllBuffered, timerEndTime);
    }

    private void ResetWarningsLocal()
    {
        if (warnings == null) return;

        for (int i = 0; i < warnings.Length; i++)
        {
            TimerWarning w = warnings[i];
            w.shown = false;
            warnings[i] = w;
        }
    }

    [PunRPC]
    void SyncTimer(float endTime)
    {
        timerEndTime = endTime;
        timerRunning = true;

        ResetWarningsLocal();
    }

    [PunRPC]
    void ShowTimeWarning(string message, float colorR, float colorG, float colorB, float colorA, float slideDuration)
    {
        Debug.Log("showing warning message");
        if (timeWarningPanel == null || warningRect == null || warningCanvasGroup == null)
            return;

        timeWarningPanel.SetActive(true);

        Image img = timeWarningPanel.GetComponent<Image>();
        if (img != null) img.color = new Color(colorR, colorG, colorB, colorA);

        LeanTween.cancel(warningRect.gameObject);
        LeanTween.cancel(warningCanvasGroup.gameObject);

        float screenWidth = Screen.width;
        warningRect.anchoredPosition = new Vector2(screenWidth, warningRect.anchoredPosition.y);
        warningCanvasGroup.alpha = 1f;

        if (warningText != null)
            warningText.text = message;

        LeanTween.moveX(warningRect, -screenWidth, slideDuration)
            .setEase(LeanTweenType.easeInOutQuad);

        LeanTween.alphaCanvas(warningCanvasGroup, 0f, 1.5f)
            .setDelay(Mathf.Max(0f, slideDuration - 1.5f))
            .setOnComplete(() =>
            {
                timeWarningPanel.SetActive(false);
            });
    }

    [PunRPC]
    public void HideTimeWarning()
    {
        if (timeWarningPanel != null && timeWarningPanel.activeSelf)
            timeWarningPanel.SetActive(false);
    }
}
