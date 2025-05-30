using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections;

public class GameTimerManager : MonoBehaviourPunCallbacks
{
    [Header("Timer Settings")]
    [SerializeField] private float countdownDuration = 60f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    private float timerEndTime;
    private bool timerRunning = false;

    [SerializeField] private GameObject timeWarningPanel;
    [SerializeField] private float warningThreshold = 10f; // seconds
    private bool warningShown = false; 

    [SerializeField] private float warningThreshold2 = 5f; // seconds
    private bool warningShown2 = false;

    private RectTransform warningRect;
    private CanvasGroup warningCanvasGroup;
    [SerializeField] private float slideDuration = 10f; // shorter = faster

    [SerializeField] private TMP_Text warningText;

    private void Awake()
    {
        SetupPhotonView();
        if (timeWarningPanel != null)
        {
            timeWarningPanel.SetActive(false);
            warningRect = timeWarningPanel.GetComponent<RectTransform>();

            // Add a CanvasGroup for fade control if it doesn't exist
            warningCanvasGroup = timeWarningPanel.GetComponent<CanvasGroup>();
            if (warningCanvasGroup == null)
            {
                warningCanvasGroup = timeWarningPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    void SetupPhotonView()
    {
        // Ensure PhotonView exists
        PhotonView view = GetComponent<PhotonView>();
        if (view == null)
        {
            view = gameObject.AddComponent<PhotonView>();
        }

        // Set Ownership Transfer to Fixed (scene object)
        view.OwnershipTransfer = OwnershipOption.Fixed;

        // Set Synchronization method (choose one)
        view.Synchronization = ViewSynchronization.UnreliableOnChange; // or .ReliableOnChange

        // Ensure this scene object is registered at runtime
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

        if (timeLeft <= warningThreshold && !warningShown)
        {
            warningShown = true;
            photonView.RPC(nameof(ShowTimeWarning), RpcTarget.All, "Time’s almost up!");
        }


        if (timeLeft <= warningThreshold2 && !warningShown2)
        {
            warningShown2 = true;
            photonView.RPC(nameof(ShowTimeWarning), RpcTarget.All, "Puzzle is going to reset!");
        }


        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            timerRunning = false;
            // Timer has ended — trigger event if needed
            Debug.Log("Timer ended!");
            PuzzleStateManager.Instance?.OnTimerExpired();
        }

        UpdateTimerUI(timeLeft);
    }

    void UpdateTimerUI(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void StartTimer()
    {
        // Only master client starts timer and sends to others
        timerEndTime = (float)PhotonNetwork.Time + countdownDuration;
        warningShown = false;
        warningShown2 = false;
        photonView.RPC(nameof(SyncTimer), RpcTarget.AllBuffered, timerEndTime);
    }

    [PunRPC]
    void SyncTimer(float endTime)
    {
        timerEndTime = endTime;
        timerRunning = true;
        warningShown = false;
        warningShown2 = false;
    }

    [PunRPC]
    void ShowTimeWarning(string message)
    {
        Debug.Log("showing warning message");
        if (timeWarningPanel == null || warningRect == null || warningCanvasGroup == null)
            return;

        // Reactivate and reset
        timeWarningPanel.SetActive(true);

        // Kill any existing tweens
        LeanTween.cancel(warningRect.gameObject);
        LeanTween.cancel(warningCanvasGroup.gameObject);

        // Reset position and alpha
        float screenWidth = Screen.width;
        warningRect.anchoredPosition = new Vector2(screenWidth, warningRect.anchoredPosition.y);
        warningCanvasGroup.alpha = 1;

        // Update the text dynamically here
        if (warningText != null)
            warningText.text = message;

        // Move to left and fade
        LeanTween.moveX(warningRect, -screenWidth, slideDuration)
            .setEase(LeanTweenType.easeInOutQuad);

        LeanTween.alphaCanvas(warningCanvasGroup, 0f, 1.5f)
            .setDelay(slideDuration - 1.5f)
            .setOnComplete(() =>
            {
                timeWarningPanel.SetActive(false);
            });
    }

    [PunRPC]
    public void HideTimeWarning()
    {
        if (timeWarningPanel != null && timeWarningPanel.activeSelf)
        {
            timeWarningPanel.SetActive(false);
        }
    }
}
