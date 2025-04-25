using UnityEngine;
using TMPro;
using Photon.Pun;

public class GameTimerManager : MonoBehaviourPunCallbacks
{
    [Header("Timer Settings")]
    [SerializeField] private float countdownDuration = 60f;

    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    private float timerEndTime;
    private bool timerRunning = false;
    private void Awake()
    {
        SetupPhotonView();
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
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            timerRunning = false;
            // Timer has ended — trigger event if needed
            Debug.Log("Timer ended!");
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
}
