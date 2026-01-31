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

    [Header("Audio (Stage Enter)")]
    [Tooltip("If false, stage enter audio will NOT play on the very first apply on each client (prevents late-join buffered RPC from playing audio).")]
    [SerializeField] private bool playEnterAudioOnInitialApply = false;

    [Header("Per-Stage Directives")]
    [SerializeField] private List<StageDirective> directives = new();

    private PuzzleStateManager.PuzzleStage? lastStage;
    private float _nextPoll;

    // Used to suppress audio on the very first stage apply (late join / buffered RPC scenario)
    private bool _hasAppliedStageOnce = false;

    [Serializable]
    public class StatusTextDirective
    {
        [Tooltip("TMP text element to update for this stage.")]
        public TMP_Text target;

        [Tooltip("Template supports {n} and optional {date}. (Legacy supported: {date+X}, {date-X})")]
        [TextArea(2, 4)]
        public string template = "";

        [Header("Date options")]
        [Tooltip("If false, date tokens will be removed/ignored (no date shown).")]
        public bool useDate = true;

        [Tooltip("Date offset in days applied to {date}. Use negative for past dates.")]
        public int dateOffsetDays = 0;

        [Tooltip("Date format used for {date}. Select from dropdown in Inspector (custom drawer).")]
        [DateFormat]
        public string dateFormat = "dd MMM yyyy";
    }


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

        [Header("Status Text Directives (optional, multiple targets)")]
        public List<StatusTextDirective> statusTextDirectives = new();

        [Header("Stage Enter Audio (optional)")]
        [Tooltip("AudioSource used to play audio when entering this stage.")]
        public AudioSource enterAudioSource;

        [Tooltip("AudioClip to play on stage enter. If null, will use enterAudioSource.clip.")]
        public AudioClip enterAudioClip;

        [Range(0f, 1f)]
        [Tooltip("Volume used when playing enter audio.")]
        public float enterAudioVolume = 1f;

        [Tooltip("If true, PlayOneShot is used (does not overwrite the AudioSource's clip). If false, sets clip then Play().")]
        public bool usePlayOneShot = true;

        [Tooltip("Optional local hooks (animations, sounds, etc.). Not networked.")]
        [Header("Invoke functions")]
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

        // Queue texts for this stage
        string[] queueTexts = CollectQueueTexts(newDirective, stage);

        // multiple status text outputs for this stage
        string[] statusTexts = CollectStatusTexts(newDirective, stage);

        var target = bufferStageRpcForLateJoiners ? RpcTarget.AllBuffered : RpcTarget.All;

        photonView.RPC(nameof(RPC_ApplyStageEffects),
            target,
            (int)stage,
            deactivatePaths,
            activatePaths,
            queueTexts,
            statusTexts);
    }

    [PunRPC]
    private void RPC_ApplyStageEffects(int stageInt, string[] deactivatePaths, string[] activatePaths, string[] queueTexts, string[] statusTexts)
    {
        bool isInitialApply = !_hasAppliedStageOnce;
        _hasAppliedStageOnce = true;

        // 1) Stop all board flashes before applying new stage
        StopAllBoardFlashes();

        // 2) Toggle objects by path
        ToggleByPaths(deactivatePaths, false);
        ToggleByPaths(activatePaths, true);

        // 3) Apply queue texts + flash for this stage
        var stage = (PuzzleStateManager.PuzzleStage)stageInt;
        var directive = FindDirective(stage);
        ApplyQueueTextsAndFlash(directive, queueTexts, stage);

        // 4) Apply status texts for this stage (multiple targets)
        ApplyStatusTexts(directive, statusTexts, stage);

        // 5) Play enter audio on stage enter
        PlayStageEnterAudio(directive, isInitialApply);

        // Local-only hooks
        directive?.onEnter?.Invoke();
    }

    private void ApplyForEveryone(PuzzleStateManager.PuzzleStage? previous, PuzzleStateManager.PuzzleStage stage)
    {
        bool isInitialApply = !_hasAppliedStageOnce;
        _hasAppliedStageOnce = true;

        StopAllBoardFlashes();

        var prevDirective = previous.HasValue ? FindDirective(previous.Value) : null;
        var newDirective = FindDirective(stage);
        if (newDirective == null) return;

        if (prevDirective != null)
            foreach (var go in prevDirective.deactivateOnExit)
                if (go) go.SetActive(false);

        foreach (var go in newDirective.activateOnEnter)
            if (go) go.SetActive(true);

        // Queue texts local
        string[] queueTexts = CollectQueueTexts(newDirective, stage);
        ApplyQueueTextsAndFlash(newDirective, queueTexts, stage);

        // Status texts local
        string[] statusTexts = CollectStatusTexts(newDirective, stage);
        ApplyStatusTexts(newDirective, statusTexts, stage);

        // Play enter audio on stage enter (local mode too)
        PlayStageEnterAudio(newDirective, isInitialApply);

        newDirective.onEnter?.Invoke();
    }

    // ----------------------------
    // NEW: Stage Enter Audio
    // ----------------------------

    private void PlayStageEnterAudio(StageDirective directive, bool isInitialApply)
    {
        if (directive == null) return;

        // Prevent buffered RPC / late joiners from hearing audio immediately (unless enabled)
        if (isInitialApply && !playEnterAudioOnInitialApply)
            return;

        if (directive.enterAudioSource == null) return;

        // Decide which clip to use:
        // - If directive.enterAudioClip is set, use that.
        // - Else fall back to directive.enterAudioSource.clip.
        AudioClip clipToPlay = directive.enterAudioClip != null
            ? directive.enterAudioClip
            : directive.enterAudioSource.clip;

        if (clipToPlay == null)
        {
            Debug.LogWarning($"[StorylineDirector] Stage {directive.stage} has no AudioClip (enterAudioClip and AudioSource.clip are both null).");
            return;
        }

        if (directive.usePlayOneShot)
        {
            directive.enterAudioSource.PlayOneShot(clipToPlay, directive.enterAudioVolume);
        }
        else
        {
            // Set clip then play
            directive.enterAudioSource.clip = clipToPlay;
            directive.enterAudioSource.volume = directive.enterAudioVolume;
            directive.enterAudioSource.Play();
        }
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

    // ----------------------------
    // Status text helpers (NEW)
    // ----------------------------

    private string[] CollectStatusTexts(StageDirective directive, PuzzleStateManager.PuzzleStage stage)
    {
        if (directive == null || directive.statusTextDirectives == null || directive.statusTextDirectives.Count == 0)
            return Array.Empty<string>();

        var result = new List<string>(directive.statusTextDirectives.Count);

        for (int i = 0; i < directive.statusTextDirectives.Count; i++)
        {
            var s = directive.statusTextDirectives[i];
            if (s == null || s.target == null)
            {
                result.Add("");
                continue;
            }

            string msg = s.template ?? "";

            // Allow {n}
            int n = GetPuzzleNumber(stage);
            msg = msg.Replace("{n}", n.ToString());

            if (s.useDate)
            {
                // ✅ If user didn't include any date token, auto-add it
                // - Empty template becomes "{date}"
                // - "Todays date:" becomes "Todays date: {date}"
                if (!msg.Contains("{date", StringComparison.Ordinal))
                {
                    msg = msg.TrimEnd();
                    msg = string.IsNullOrEmpty(msg) ? "{date}" : (msg + " {date}");
                }

                string fmt = string.IsNullOrEmpty(s.dateFormat) ? "dd MMM yyyy" : s.dateFormat;
                DateTime baseDate = DateTime.Now.AddDays(s.dateOffsetDays);

                msg = ReplaceDateTokens(msg, fmt, baseDate);
            }
            else
            {
                msg = RemoveDateTokens(msg);
            }

            result.Add(msg);
        }

        return result.ToArray();
    }

    private static string RemoveDateTokens(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        if (!input.Contains("{date")) return input;

        string output = input;

        // Remove simple token
        output = output.Replace("{date}", "");

        // Remove legacy tokens like {date+2}, {date-5}
        int safety = 50;
        while (safety-- > 0)
        {
            int start = output.IndexOf("{date", StringComparison.Ordinal);
            if (start < 0) break;

            int end = output.IndexOf("}", start, StringComparison.Ordinal);
            if (end < 0) break;

            string token = output.Substring(start, end - start + 1);

            // only remove if it matches the legacy token patterns
            if (token.StartsWith("{date+", StringComparison.Ordinal) || token.StartsWith("{date-", StringComparison.Ordinal))
            {
                output = ReplaceFirst(output, token, "");
            }
            else
            {
                // If it's some weird token, stop to avoid infinite loop
                break;
            }
        }

        return output;
    }

    private void ApplyStatusTexts(StageDirective directive, string[] statusTexts, PuzzleStateManager.PuzzleStage stage)
    {
        if (directive == null || directive.statusTextDirectives == null) return;
        if (statusTexts == null) return;

        for (int i = 0; i < directive.statusTextDirectives.Count && i < statusTexts.Length; i++)
        {
            var s = directive.statusTextDirectives[i];
            if (s == null || s.target == null) continue;

            if (string.IsNullOrEmpty(statusTexts[i])) continue;

            s.target.text = statusTexts[i];
        }
    }

    private static string ReplaceDateTokens(string input, string dateFormat, DateTime baseDate)
    {
        if (string.IsNullOrEmpty(input)) return "";

        if (!input.Contains("{date")) return input;

        string output = input;

        // Primary token {date} uses baseDate (NOW + offsetDays)
        string baseStr = baseDate.ToString(dateFormat);
        output = output.Replace("{date}", baseStr);

        int safety = 50;
        while (safety-- > 0)
        {
            int start = output.IndexOf("{date", StringComparison.Ordinal);
            if (start < 0) break;

            int end = output.IndexOf("}", start, StringComparison.Ordinal);
            if (end < 0) break;

            string token = output.Substring(start, end - start + 1);

            if (token == "{date}")
            {
                output = ReplaceFirst(output, token, baseStr);
                continue;
            }

            int offsetDays = 0;
            bool parsed = false;

            if (token.StartsWith("{date+", StringComparison.Ordinal) && token.EndsWith("}"))
            {
                string num = token.Substring(6, token.Length - 7);
                parsed = int.TryParse(num, out offsetDays);
            }
            else if (token.StartsWith("{date-", StringComparison.Ordinal) && token.EndsWith("}"))
            {
                string num = token.Substring(6, token.Length - 7);
                parsed = int.TryParse(num, out offsetDays);
                offsetDays = -offsetDays;
            }

            if (parsed)
            {
                string d = baseDate.AddDays(offsetDays).ToString(dateFormat);
                output = ReplaceFirst(output, token, d);
            }
            else
            {
                break;
            }
        }

        return output;
    }

    private static string ReplaceFirst(string text, string search, string replace)
    {
        int pos = text.IndexOf(search, StringComparison.Ordinal);
        if (pos < 0) return text;
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }
}
