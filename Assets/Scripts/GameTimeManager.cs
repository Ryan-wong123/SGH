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
        // Attach PhotonView if missing
        if (GetComponent<PhotonView>() == null)
        {
            gameObject.AddComponent<PhotonView>();
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
        Debug.Log($"Timer {timeLeft}");
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
