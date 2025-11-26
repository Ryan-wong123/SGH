using System;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class StorylineDirector : MonoBehaviourPunCallbacks
{
    [Header("Refs")]
    [SerializeField] private PuzzleStateManager puzzleStateManager;

    [Header("Behaviour")]
    [Tooltip("Only Master Client drives and broadcasts effects when using Photon.")]
    [SerializeField] private bool masterControls = true;

    [Tooltip("Polling interval (s) for detecting stage changes.")]
    [SerializeField] private float pollIntervalSeconds = 0.1f;

    [Header("Per-Stage Directives")]
    [SerializeField] private List<StageDirective> directives = new();

    public List<ComicStrip> comic = new List<ComicStrip>();

    private PuzzleStateManager.PuzzleStage? lastStage;
    private float _nextPoll;

    [Serializable]
    public class QueueDisplayDirective
    {
        [Tooltip("TMP text element to update for this stage.")]
        public TextMeshProUGUI display;

        [Tooltip("Queue board (QueueNumberDisplay) that should flash when this text is shown.")]
        public QueueNumberDisplay board;

        [Tooltip("Text applied to this display. Use {n} to inject puzzle number.")]
        public string text = "";

        [Header("Flash behavior")]
        [Tooltip("If true, board.isFlash will be turned ON when text is applied.")]
        public bool flashBoardWhenShown = true;
    }

    [Serializable]
    public class StageDirective
    {
        public PuzzleStateManager.PuzzleStage stage;

        [Tooltip("Objects set active = true when entering this stage.")]
        public List<GameObject> activateOnEnter = new();

        [Tooltip("Objects set active = false when exiting this stage.")]
        public List<GameObject> deactivateOnExit = new();

        [Header("Queue Number Displays (optional, per-display text)")]
        public List<QueueDisplayDirective> queueDisplayDirectives = new();

        [Tooltip("Optional local hooks (animations, sounds, etc.). Not networked.")]
        public UnityEvent onEnter;
    }
    [System.Serializable]
    public class ToggleObject
    {
        public GameObject target;

        [Tooltip("If true, once this object is disabled one time, it will NEVER be enabled again")]
        public bool disablePermanentlyAfterFirstDisable = false;

        [HideInInspector]
        public bool hasBeenPermanentlyDisabled = false;  // runtime state
    }

    [System.Serializable]
    public class ComicStrip
    {
        [Tooltip("Which puzzle state does this mapping apply to?")]
        public PuzzleStateManager.PuzzleStage stage;
        [Tooltip("Objects that turn ON when this state is active.")]
        public List<GameObject> objectsToEnable = new List<GameObject>();
    }

    private void Awake()
    {
        if (puzzleStateManager == null)
            puzzleStateManager = FindObjectOfType<PuzzleStateManager>();
    }

    private void Start()
    {
        TryDetectAndApply(true);
    }

    private void Update()
    {
        if (Time.unscaledTime >= _nextPoll)
        {
            _nextPoll = Time.unscaledTime + pollIntervalSeconds;
            TryDetectAndApply(false);
        }
    }

    private void TryDetectAndApply(bool force)
    {
        if (puzzleStateManager == null) return;

        var stage = puzzleStateManager.CurrentStage;
        if (!force && lastStage.HasValue && lastStage.Value == stage)
            return;

        var previousStage = lastStage;
        lastStage = stage;

        if (masterControls && PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            BroadcastApply(previousStage, stage);
        }
        else
        {
            ApplyForEveryone(previousStage, stage);
        }
    }

    private void BroadcastApply(PuzzleStateManager.PuzzleStage? previous, PuzzleStateManager.PuzzleStage stage)
    {
        var prevDirective = previous.HasValue ? FindDirective(previous.Value) : null;
        var newDirective = FindDirective(stage);

        int[] deactivateIds = CollectInstanceIDs(prevDirective?.deactivateOnExit);
        int[] activateIds = CollectInstanceIDs(newDirective?.activateOnEnter);

        string[] texts = CollectQueueTexts(newDirective, stage);

        photonView.RPC(nameof(RPC_ApplyStageEffects),
            RpcTarget.All,
            (int)stage,
            deactivateIds,
            activateIds,
            texts);
    }

    [PunRPC]
    private void RPC_ApplyStageEffects(int stageInt, int[] deactivateIds, int[] activateIds, string[] texts)
    {
        // 1) Stop all board flashes before applying new stage (ensures "flash until stage change")
        StopAllBoardFlashes();

        // 2) Toggle objects
        ToggleByInstanceIDs(deactivateIds, false);
        ToggleByInstanceIDs(activateIds, true);

        // 3) Apply texts + turn on flash for this stage
        var stage = (PuzzleStateManager.PuzzleStage)stageInt;
        var directive = FindDirective(stage);
        ApplyQueueTextsAndFlash(directive, texts, stage);
        ApplyComicState(stage);
        directive?.onEnter?.Invoke();
    }

    private void ApplyForEveryone(PuzzleStateManager.PuzzleStage? previous, PuzzleStateManager.PuzzleStage stage)
    {
        // 1) Stop all board flashes
        StopAllBoardFlashes();

        // show respective comic strip based on puzzle state number
        ApplyComicState(stage);

        var prevDirective = previous.HasValue ? FindDirective(previous.Value) : null;
        var newDirective = FindDirective(stage);
        if (newDirective == null) return;

        if (prevDirective != null)
            foreach (var go in prevDirective.deactivateOnExit)
                if (go) go.SetActive(false);

        foreach (var go in newDirective.activateOnEnter)
            if (go) go.SetActive(true);

        string[] texts = CollectQueueTexts(newDirective, stage);
        ApplyQueueTextsAndFlash(newDirective, texts, stage);

        newDirective.onEnter?.Invoke();
    }

    // ---- Core helpers ----
    private string[] CollectQueueTexts(StageDirective directive, PuzzleStateManager.PuzzleStage stage)
    {
        if (directive == null || directive.queueDisplayDirectives == null)
            return Array.Empty<string>();

        var result = new List<string>(directive.queueDisplayDirectives.Count);
        foreach (var q in directive.queueDisplayDirectives)
        {
            string txt = FormatQueueText(q.text, stage);
            result.Add(txt);
        }
        return result.ToArray();
    }

    private void ApplyQueueTextsAndFlash(StageDirective directive, string[] texts, PuzzleStateManager.PuzzleStage stage)
    {
        if (directive == null || directive.queueDisplayDirectives == null) return;

        for (int i = 0; i < directive.queueDisplayDirectives.Count && i < texts.Length; i++)
        {
            var q = directive.queueDisplayDirectives[i];

            // 1) Set the visible stage text
            if (q.display != null) q.display.text = texts[i];

            // 2) Flash ONLY the queue board while this stage is active
            if (q.flashBoardWhenShown && q.board != null)
            {
                q.board.SetFlash(true);   // keep flashing; will be turned off next stage
            }
        }
    }

    private void StopAllBoardFlashes()
    {
        if (directives == null) return;
        foreach (var d in directives)
        {
            if (d?.queueDisplayDirectives == null) continue;
            foreach (var q in d.queueDisplayDirectives)
                if (q?.board != null) q.board.SetFlash(false);
        }
    }

    private StageDirective FindDirective(PuzzleStateManager.PuzzleStage stage)
    {
        foreach (var d in directives)
            if (d.stage.Equals(stage))
                return d;
        return null;
    }

    private string FormatQueueText(string template, PuzzleStateManager.PuzzleStage stage)
    {
        if (string.IsNullOrEmpty(template)) return "";
        int n = GetPuzzleNumber(stage);
        return template.Replace("{n}", n.ToString());
    }

    private static int GetPuzzleNumber(PuzzleStateManager.PuzzleStage stage)
    {
        string name = stage.ToString();
        int i = name.Length - 1;
        if (i >= 0 && char.IsDigit(name[i]))
        {
            int end = i;
            while (i >= 0 && char.IsDigit(name[i])) i--;
            if (int.TryParse(name.Substring(i + 1, end - i), out int parsed))
                return parsed;
        }
        return (int)stage + 1;
    }

    private static int[] CollectInstanceIDs(List<GameObject> list)
    {
        if (list == null || list.Count == 0) return Array.Empty<int>();
        List<int> ids = new();
        foreach (var go in list)
            if (go != null) ids.Add(go.GetInstanceID());
        return ids.ToArray();
    }

    private static void ToggleByInstanceIDs(int[] ids, bool active)
    {
        if (ids == null) return;
        var all = FindObjectsOfType<GameObject>(true);
        var map = new HashSet<int>(ids);
        foreach (var go in all)
            if (map.Contains(go.GetInstanceID()))
                go.SetActive(active);
    }

    private void ApplyComicState(PuzzleStateManager.PuzzleStage newStage)
    {
        ClueUI.Instance.ShowClueMainPage();
        foreach (var entry in comic)
        {
            if (entry.stage == newStage)
            {
                foreach (var go in entry.objectsToEnable)
                    if (go) go.SetActive(true);
            }
        }
    }

}

