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

    private void Awake()
    {
        SetupPhotonView();
        timeWarningPanel.SetActive(false);
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
            photonView.RPC(nameof(ShowTimeWarning), RpcTarget.All);
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
        photonView.RPC(nameof(SyncTimer), RpcTarget.AllBuffered, timerEndTime);
    }

    [PunRPC]
    void SyncTimer(float endTime)
    {
        timerEndTime = endTime;
        timerRunning = true;
    }

    [PunRPC]
    void ShowTimeWarning()
    {
        if (timeWarningPanel != null)
        {
            timeWarningPanel.SetActive(true);
        }
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
