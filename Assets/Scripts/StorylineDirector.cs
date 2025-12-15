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

    [Tooltip("If true, stage effects are buffered so late joiners receive the latest stage toggles.")]
    [SerializeField] private bool bufferStageRpcForLateJoiners = true;

    [Header("Per-Stage Directives")]
    [SerializeField] private List<StageDirective> directives = new();

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

        [Tooltip("Scene objects set active = true when entering this stage.")]
        public List<GameObject> activateOnEnter = new();

        [Tooltip("Scene objects set active = false when exiting this stage.")]
        public List<GameObject> deactivateOnExit = new();

        [Header("Queue Number Displays (optional, per-display text)")]
        public List<QueueDisplayDirective> queueDisplayDirectives = new();

        [Tooltip("Optional local hooks (animations, sounds, etc.). Not networked.")]
        public UnityEvent onEnter;
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

        // Convert to stable hierarchy paths (works even if inactive)
        string[] deactivatePaths = CollectHierarchyPaths(prevDirective?.deactivateOnExit);
        string[] activatePaths = CollectHierarchyPaths(newDirective?.activateOnEnter);

        string[] texts = CollectQueueTexts(newDirective, stage);

        var target = bufferStageRpcForLateJoiners ? RpcTarget.AllBuffered : RpcTarget.All;

        photonView.RPC(nameof(RPC_ApplyStageEffects),
            target,
            (int)stage,
            deactivatePaths,
            activatePaths,
            texts);
    }

    [PunRPC]
    private void RPC_ApplyStageEffects(int stageInt, string[] deactivatePaths, string[] activatePaths, string[] texts)
    {
        // 1) Stop all board flashes before applying new stage
        StopAllBoardFlashes();

        // 2) Toggle objects by path
        ToggleByPaths(deactivatePaths, false);
        ToggleByPaths(activatePaths, true);

        // 3) Apply texts + turn on flash for this stage
        var stage = (PuzzleStateManager.PuzzleStage)stageInt;
        var directive = FindDirective(stage);
        ApplyQueueTextsAndFlash(directive, texts, stage);

        // Local-only hooks
        directive?.onEnter?.Invoke();
    }

    private void ApplyForEveryone(PuzzleStateManager.PuzzleStage? previous, PuzzleStateManager.PuzzleStage stage)
    {
        StopAllBoardFlashes();

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

    // ----------------------------
    // Path-based helpers
    // ----------------------------

    private static string[] CollectHierarchyPaths(List<GameObject> list)
    {
        if (list == null || list.Count == 0) return Array.Empty<string>();

        List<string> paths = new();
        foreach (var go in list)
        {
            if (!go) continue;
            paths.Add(GetHierarchyPath(go.transform));
        }
        return paths.ToArray();
    }

    // Example: "Environment/Room1/DoorBlocker"
    private static string GetHierarchyPath(Transform t)
    {
        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }

    // Find including inactive
    private static GameObject FindByHierarchyPathIncludingInactive(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // The first segment is a root object name
        var split = path.Split('/');
        if (split.Length == 0) return null;

        // Find matching root (including inactive)
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;

        foreach (var r in roots)
        {
            if (r.name == split[0])
            {
                current = r.transform;
                break;
            }
        }
        if (current == null) return null;

        // Traverse children by name, including inactive
        for (int i = 1; i < split.Length; i++)
        {
            current = FindChildByNameIncludingInactive(current, split[i]);
            if (current == null) return null;
        }

        return current.gameObject;
    }

    private static Transform FindChildByNameIncludingInactive(Transform parent, string childName)
    {
        // Search recursively through all children (includes inactive automatically)
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;

            var deeper = FindChildByNameIncludingInactive(child, childName);
            if (deeper != null) return deeper;
        }
        return null;
    }

    private static void ToggleByPaths(string[] paths, bool active)
    {
        if (paths == null) return;

        foreach (var p in paths)
        {
            var go = FindByHierarchyPathIncludingInactive(p);
            if (go != null)
            {
                go.SetActive(active);
            }
            else
            {
                Debug.LogWarning($"[StorylineDirector] Could not find GameObject by path: {p}");
            }
        }
    }

    // ----------------------------
    // Your existing queue helpers
    // ----------------------------

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

            if (q.display != null) q.display.text = texts[i];

            if (q.flashBoardWhenShown && q.board != null)
                q.board.SetFlash(true);
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
}
