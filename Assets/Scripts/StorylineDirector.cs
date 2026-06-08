using System; // Imports a namespace used by the storyline director.
using System.Collections.Generic; // Imports a namespace used by the storyline director.
using System.IO; // Imports a namespace used by the storyline director.
using Photon.Pun; // Imports a namespace used by the storyline director.
using TMPro; // Imports a namespace used by the storyline director.
using UnityEngine; // Imports a namespace used by the storyline director.
using UnityEngine.Events; // Imports a namespace used by the storyline director.
using static Unity.Burst.Intrinsics.X86.Avx; // Imports a namespace used by the storyline director.

public class StorylineDirector : MonoBehaviourPunCallbacks // Declares the component that applies story effects as puzzle stages change.
{ // Starts this block.
    [Header("Refs")] // Groups related storyline fields in the Unity inspector.
    [SerializeField] private PuzzleStateManager puzzleStateManager; // Exposes this private storyline field in the Unity inspector.
    [SerializeField] private CluePopupUI cluePopupUI; // Exposes this private storyline field in the Unity inspector.

    [Header("Behaviour")] // Groups related storyline fields in the Unity inspector.
    [Tooltip("Only Master Client drives and broadcasts effects when using Photon.")] // Shows guidance for this field in the Unity inspector.
    [SerializeField] private bool masterControls = true; // Exposes this private storyline field in the Unity inspector.

    [Tooltip("Polling interval (s) for detecting stage changes.")] // Shows guidance for this field in the Unity inspector.
    [SerializeField] private float pollIntervalSeconds = 0.1f; // Exposes this private storyline field in the Unity inspector.

    [Tooltip("If true, stage effects are buffered so late joiners receive the latest stage toggles.")] // Shows guidance for this field in the Unity inspector.
    [SerializeField] private bool bufferStageRpcForLateJoiners = true; // Exposes this private storyline field in the Unity inspector.

    [Header("Audio (Stage Enter)")] // Groups related storyline fields in the Unity inspector.
    [Tooltip("If false, stage enter audio will NOT play on the very first apply on each client (prevents late-join buffered RPC from playing audio).")] // Shows guidance for this field in the Unity inspector.
    [SerializeField] private bool playEnterAudioOnInitialApply = false; // Exposes this private storyline field in the Unity inspector.

    [Header("Popup (Stage Enter/Exit)")] // Groups related storyline fields in the Unity inspector.
    [Tooltip("If false, clue popup messages will NOT show on the very first apply on each client (prevents late-join buffered RPC from showing stale messages).")] // Shows guidance for this field in the Unity inspector.
    [SerializeField] private bool showPopupMessagesOnInitialApply = false; // Exposes this private storyline field in the Unity inspector.

    [Header("Per-Stage Directives")] // Groups related storyline fields in the Unity inspector.
    [SerializeField] private List<StageDirective> directives = new(); // Exposes this private storyline field in the Unity inspector.

    private PuzzleStateManager.PuzzleStage? lastStage; // Tracks the previously applied puzzle stage.
    private float _nextPoll; // Stores the next unscaled time when stage polling should run.

    // Used to suppress audio on the very first stage apply (late join / buffered RPC scenario)
    private bool _hasAppliedStageOnce = false; // Tracks whether this client has already applied any stage effects.

    [Serializable] // Allows Unity to serialize this directive type in the inspector.
    public class StatusTextDirective // Defines one status text update for a stage.
    { // Starts this block.
        [Tooltip("TMP text element to update for this stage.")] // Shows guidance for this field in the Unity inspector.
        public TMP_Text target; // Handles this part of the storyline stage flow.

        [Tooltip("Template supports {n} and optional {date}. (Legacy supported: {date+X}, {date-X})")] // Shows guidance for this field in the Unity inspector.
        [TextArea(2, 4)] // Gives this text field a larger editor box.
        public string template = ""; // Handles this part of the storyline stage flow.

        [Header("Date options")] // Groups related storyline fields in the Unity inspector.
        [Tooltip("If false, date tokens will be removed/ignored (no date shown).")] // Shows guidance for this field in the Unity inspector.
        public bool useDate = true; // Handles this part of the storyline stage flow.

        [Tooltip("Date offset in days applied to {date}. Use negative for past dates.")] // Shows guidance for this field in the Unity inspector.
        public int dateOffsetDays = 0; // Handles this part of the storyline stage flow.

        [Tooltip("Date format used for {date}. Select from dropdown in Inspector (custom drawer).")] // Shows guidance for this field in the Unity inspector.
        [DateFormat] // Uses the custom date-format drawer for this string.
        public string dateFormat = "dd MMM yyyy"; // Handles this part of the storyline stage flow.
    } // Ends this block.


    [Serializable] // Allows Unity to serialize this directive type in the inspector.
    public class QueueDisplayDirective // Defines one queue-board text update for a stage.
    { // Starts this block.
        [Tooltip("TMP text element to update for this stage.")] // Shows guidance for this field in the Unity inspector.
        public TextMeshProUGUI display; // Handles this part of the storyline stage flow.
        [Tooltip("Queue board (QueueNumberDisplay) that should flash when this text is shown.")] // Shows guidance for this field in the Unity inspector.
        public QueueNumberDisplay board; // Handles this part of the storyline stage flow.
        [Tooltip("Text applied to this display. Use {n} to inject puzzle number.")] // Shows guidance for this field in the Unity inspector.
        public string text = ""; // Handles this part of the storyline stage flow.
        [Header("Flash behavior")] // Groups related storyline fields in the Unity inspector.
        [Tooltip("If true, board.isFlash will be turned ON when text is applied.")] // Shows guidance for this field in the Unity inspector.
        public bool flashBoardWhenShown = true; // Handles this part of the storyline stage flow.
    } // Ends this block.

    [Serializable] // Allows Unity to serialize this directive type in the inspector.
    public class PopupMessageDirective // Defines one delayed popup message for a stage transition.
    { // Starts this block.
        [Tooltip("Popup message shown through CluePopupUI. Leave empty to disable.")] // Shows guidance for this field in the Unity inspector.
        [TextArea(2, 4)] // Gives this text field a larger editor box.
        public string message = ""; // Handles this part of the storyline stage flow.

        [Tooltip("Delay in seconds before the popup message is shown.")] // Shows guidance for this field in the Unity inspector.
        [Min(0f)] // Constrains this numeric field to a minimum value in the inspector.
        public float delaySeconds = 0f; // Handles this part of the storyline stage flow.
    } // Ends this block.

    [Serializable] // Allows Unity to serialize this directive type in the inspector.
    public class StageDirective // Defines all configured effects for one puzzle stage.
    { // Starts this block.
        public PuzzleStateManager.PuzzleStage stage; // Handles this part of the storyline stage flow.

        [Tooltip("Scene objects set active = true when entering this stage.")] // Shows guidance for this field in the Unity inspector.
        public List<GameObject> activateOnEnter = new(); // Handles this part of the storyline stage flow.

        [Tooltip("Scene objects set active = false when exiting this stage.")] // Shows guidance for this field in the Unity inspector.
        public List<GameObject> deactivateOnExit = new(); // Handles this part of the storyline stage flow.

        [Header("Disable components (keep object active)")] // Groups related storyline fields in the Unity inspector.
        [Tooltip("Objects where ALL components/scripts will be disabled on stage enter (object stays active).")] // Shows guidance for this field in the Unity inspector.
        public List<GameObject> disableAllComponentsOnEnter = new(); // Handles this part of the storyline stage flow.

        [Tooltip("If true, also disables components on all children.")] // Shows guidance for this field in the Unity inspector.
        public bool disableAlsoChildren = true; // Handles this part of the storyline stage flow.

        [Tooltip("Components to keep enabled even when disabling everything else (e.g., PhotonView).")] // Shows guidance for this field in the Unity inspector.
        public List<Component> keepEnabledComponents = new(); // Handles this part of the storyline stage flow.

        [Header("Queue Number Displays (optional, per-display text)")] // Groups related storyline fields in the Unity inspector.
        public List<QueueDisplayDirective> queueDisplayDirectives = new(); // Handles this part of the storyline stage flow.

        [Header("Status Text Directives (optional, multiple targets)")] // Groups related storyline fields in the Unity inspector.
        public List<StatusTextDirective> statusTextDirectives = new(); // Handles this part of the storyline stage flow.

        [Header("Clue Popup Messages (optional)")] // Groups related storyline fields in the Unity inspector.
        public PopupMessageDirective popupOnEnter = new(); // Handles this part of the storyline stage flow.
        public PopupMessageDirective popupOnExit = new(); // Handles this part of the storyline stage flow.

        [Header("Stage Enter Audio (optional)")] // Groups related storyline fields in the Unity inspector.
        [Tooltip("AudioSource used to play audio when entering this stage.")] // Shows guidance for this field in the Unity inspector.
        public AudioSource enterAudioSource; // Handles this part of the storyline stage flow.

        [Tooltip("AudioClip to play on stage enter. If null, will use enterAudioSource.clip.")] // Shows guidance for this field in the Unity inspector.
        public AudioClip enterAudioClip; // Handles this part of the storyline stage flow.

        [Range(0f, 1f)] // Constrains this numeric field to a slider range in the inspector.
        [Tooltip("Volume used when playing enter audio.")] // Shows guidance for this field in the Unity inspector.
        public float enterAudioVolume = 1f; // Handles this part of the storyline stage flow.

        [Tooltip("If true, PlayOneShot is used (does not overwrite the AudioSource's clip). If false, sets clip then Play().")] // Shows guidance for this field in the Unity inspector.
        public bool usePlayOneShot = true; // Handles this part of the storyline stage flow.

        [Tooltip("Optional local hooks (animations, sounds, etc.). Not networked.")] // Shows guidance for this field in the Unity inspector.
        [Header("Invoke functions")] // Groups related storyline fields in the Unity inspector.
        public UnityEvent onEnter; // Handles this part of the storyline stage flow.
    } // Ends this block.

    private void Awake() // Finds missing storyline dependencies before the game starts.
    { // Starts this block.
        if (puzzleStateManager == null) // Checks whether this storyline condition requires action.
            puzzleStateManager = FindObjectOfType<PuzzleStateManager>(); // Stores the puzzle state manager dependency.
    } // Ends this block.

    private void Start() // Applies the initial stage effects when the director starts.
    { // Starts this block.
        TryDetectAndApply(true); // Handles this part of the storyline stage flow.
    } // Ends this block.

    private void Update() // Polls for puzzle-stage changes at the configured interval.
    { // Starts this block.
        if (Time.unscaledTime >= _nextPoll) // Checks whether this storyline condition requires action.
        { // Starts this block.
            _nextPoll = Time.unscaledTime + pollIntervalSeconds; // Schedules the next stage poll.
            TryDetectAndApply(false); // Handles this part of the storyline stage flow.
        } // Ends this block.
    } // Ends this block.

    private void TryDetectAndApply(bool force) // Detects stage changes and applies or broadcasts their effects.
    { // Starts this block.
        if (puzzleStateManager == null) return; // Checks whether this storyline condition requires action.

        var stage = puzzleStateManager.CurrentStage; // Stores an inferred storyline value.
        if (!force && lastStage.HasValue && lastStage.Value == stage) // Checks whether this storyline condition requires action.
            return; // Leaves the current method early.

        var previousStage = lastStage; // Stores an inferred storyline value.
        lastStage = stage; // Updates the previously applied stage.

        if (masterControls && PhotonNetwork.IsConnected) // Checks whether this storyline condition requires action.
        { // Starts this block.
            if (!PhotonNetwork.IsMasterClient) return; // Checks whether this storyline condition requires action.
            BroadcastApply(previousStage, stage); // Handles this part of the storyline stage flow.
        } // Ends this block.
        else // Runs when the previous condition is false.
        { // Starts this block.
            ApplyForEveryone(previousStage, stage); // Applies this group of storyline effects.
        } // Ends this block.
    } // Ends this block.

    private void BroadcastApply(PuzzleStateManager.PuzzleStage? previous, PuzzleStateManager.PuzzleStage stage) // Collects stage effect data and sends it through Photon.
    { // Starts this block.
        var prevDirective = previous.HasValue ? FindDirective(previous.Value) : null; // Stores an inferred storyline value.
        var newDirective = FindDirective(stage); // Stores an inferred storyline value.

        // Convert to stable hierarchy paths (works even if inactive)
        string[] deactivatePaths = CollectHierarchyPaths(prevDirective?.deactivateOnExit); // Stores a batch of serialized storyline values.
        string[] activatePaths = CollectHierarchyPaths(newDirective?.activateOnEnter); // Stores a batch of serialized storyline values.

        // Queue texts for this stage
        string[] queueTexts = CollectQueueTexts(newDirective, stage); // Stores a batch of serialized storyline values.

        // multiple status text outputs for this stage
        string[] statusTexts = CollectStatusTexts(newDirective, stage); // Stores a batch of serialized storyline values.

        string exitPopupText = previous.HasValue ? CollectPopupMessage(prevDirective?.popupOnExit, previous.Value) : ""; // Stores formatted storyline text or object paths.
        float exitPopupDelay = previous.HasValue ? CollectPopupDelay(prevDirective?.popupOnExit) : 0f; // Stores a timing value for storyline effects.

        string enterPopupText = CollectPopupMessage(newDirective?.popupOnEnter, stage); // Stores formatted storyline text or object paths.
        float enterPopupDelay = CollectPopupDelay(newDirective?.popupOnEnter); // Stores a timing value for storyline effects.

        //disable get component paths
        string[] disableAllCompPaths = CollectHierarchyPaths(newDirective?.disableAllComponentsOnEnter); // Stores a batch of serialized storyline values.
        bool disableAlsoChildren = newDirective != null && newDirective.disableAlsoChildren; // Stores a storyline boolean state value.
        string[] keepEnabledCompPaths = CollectComponentPaths(newDirective); // helper below // Stores a batch of serialized storyline values.

        var target = bufferStageRpcForLateJoiners ? RpcTarget.AllBuffered : RpcTarget.All; // Stores an inferred storyline value.

        photonView.RPC(nameof(RPC_ApplyStageEffects), // Sends the stage effects to Photon clients.
            target, // Handles this part of the storyline stage flow.
            (int)stage, // Handles this part of the storyline stage flow.
            deactivatePaths, // Handles this part of the storyline stage flow.
            activatePaths, // Handles this part of the storyline stage flow.
            queueTexts, // Handles this part of the storyline stage flow.
            statusTexts, // Handles this part of the storyline stage flow.
            exitPopupText, // Handles this part of the storyline stage flow.
            exitPopupDelay, // Handles this part of the storyline stage flow.
            enterPopupText, // Handles this part of the storyline stage flow.
            enterPopupDelay, // Handles this part of the storyline stage flow.
            disableAllCompPaths, // Handles this part of the storyline stage flow.
            disableAlsoChildren, // Handles this part of the storyline stage flow.
            keepEnabledCompPaths // Handles this part of the storyline stage flow.
        ); // Handles this part of the storyline stage flow.
    } // Ends this block.

    [PunRPC] // Marks the next method as callable through Photon RPC.
    private void RPC_ApplyStageEffects(int stageInt, string[] deactivatePaths, string[] activatePaths, string[] queueTexts, string[] statusTexts, string exitPopupText, float exitPopupDelay, string enterPopupText, float enterPopupDelay, string[] disableAllCompPaths, bool disableAlsoChildren, string[] keepEnabledCompPaths) // Applies synchronized stage effects received through Photon.
    { // Starts this block.
        bool isInitialApply = !_hasAppliedStageOnce; // Stores a storyline boolean state value.
        _hasAppliedStageOnce = true; // Marks that this client has applied stage effects.

        // 1) Stop all board flashes before applying new stage
        StopAllBoardFlashes(); // Clears queue board flashing before new stage effects.

        // 2) Toggle objects by path
        ToggleByPaths(deactivatePaths, false); // Applies object visibility changes by path.
        ToggleByPaths(activatePaths, true); // Applies object visibility changes by path.

        // 3) Apply queue texts + flash for this stage
        var stage = (PuzzleStateManager.PuzzleStage)stageInt; // Stores an inferred storyline value.
        var directive = FindDirective(stage); // Stores an inferred storyline value.

        // Disable components 
        DisableAllComponentsByPaths(disableAllCompPaths, disableAlsoChildren, keepEnabledCompPaths); // Handles this part of the storyline stage flow.


        ApplyQueueTextsAndFlash(directive, queueTexts, stage); // Applies this group of storyline effects.

        // 4) Apply status texts for this stage (multiple targets)
        ApplyStatusTexts(directive, statusTexts, stage); // Applies this group of storyline effects.

        // 5) Play enter audio on stage enter
        PlayStageEnterAudio(directive, isInitialApply); // Handles this part of the storyline stage flow.

        // 6) Show clue popup messages tied to this transition
        ApplyPopupMessages(exitPopupText, exitPopupDelay, enterPopupText, enterPopupDelay, isInitialApply); // Applies this group of storyline effects.

        // Local-only hooks
        directive?.onEnter?.Invoke(); // Invokes configured stage event hooks.
    } // Ends this block.
    // Component path format: "Root/A/B/C|ComponentTypeName"
    // We use this so we can "keep enabled" specific components over the network.
    private static string MakeComponentPath(Component c) // Handles this part of the storyline stage flow.
    { // Starts this block.
        if (c == null) return ""; // Checks whether this storyline condition requires action.
        return GetHierarchyPath(c.transform) + "|" + c.GetType().AssemblyQualifiedName; // Returns the computed storyline value to the caller.
    } // Ends this block.

    private static (string goPath, string typeName) ParseComponentPath(string s) // Handles this part of the storyline stage flow.
    { // Starts this block.
        int idx = s.LastIndexOf('|'); // Stores a numeric storyline value.
        if (idx < 0) return (s, ""); // Checks whether this storyline condition requires action.
        return (s.Substring(0, idx), s.Substring(idx + 1)); // Returns the computed storyline value to the caller.
    } // Ends this block.

    private static string[] CollectComponentPaths(StageDirective directive) // Converts keep-enabled components into stable component paths.
    { // Starts this block.
        if (directive == null || directive.keepEnabledComponents == null || directive.keepEnabledComponents.Count == 0) // Checks whether this storyline condition requires action.
            return Array.Empty<string>(); // Returns the computed storyline value to the caller.

        var list = new List<string>(); // Stores an inferred storyline value.
        foreach (var c in directive.keepEnabledComponents) // Loops through configured storyline objects or directives.
        { // Starts this block.
            if (c == null) continue; // Checks whether this storyline condition requires action.
            list.Add(MakeComponentPath(c)); // Handles this part of the storyline stage flow.
        } // Ends this block.
        return list.ToArray(); // Returns the computed storyline value to the caller.
    } // Ends this block.

    private static void DisableAllComponentsByPaths(string[] goPaths, bool includeChildren, string[] keepEnabledComponentPaths) // Handles this part of the storyline stage flow.
    { // Starts this block.
        if (goPaths == null || goPaths.Length == 0) return; // Checks whether this storyline condition requires action.

        // Build a lookup of components we want to keep enabled
        var keepSet = new HashSet<(string goPath, string typeName)>(); // Stores an inferred storyline value.
        if (keepEnabledComponentPaths != null) // Checks whether this storyline condition requires action.
        { // Starts this block.
            foreach (var s in keepEnabledComponentPaths) // Loops through configured storyline objects or directives.
            { // Starts this block.
                if (string.IsNullOrEmpty(s)) continue; // Checks whether this storyline condition requires action.
                keepSet.Add(ParseComponentPath(s)); // Handles this part of the storyline stage flow.
            } // Ends this block.
        } // Ends this block.

        foreach (var p in goPaths) // Loops through configured storyline objects or directives.
        { // Starts this block.
            var go = FindByHierarchyPathIncludingInactive(p); // Stores an inferred storyline value.
            if (go == null) // Checks whether this storyline condition requires action.
            { // Starts this block.
                Debug.LogWarning($"[StorylineDirector] Could not find GameObject to disable components by path: {p}"); // Writes a storyline warning to the Unity console.
                continue; // Handles this part of the storyline stage flow.
            } // Ends this block.

            DisableAllComponents(go, includeChildren, keepSet); // Handles this part of the storyline stage flow.
        } // Ends this block.
    } // Ends this block.

    private static void DisableAllComponents( // Handles this part of the storyline stage flow.
        GameObject root, // Stores a scene object reference.
        bool includeChildren, // Stores a storyline boolean state value.
        HashSet<(string goPath, string typeName)> keepSet // Handles this part of the storyline stage flow.
    ) // Handles this part of the storyline stage flow.
    { // Starts this block.
        if (root == null) return; // Checks whether this storyline condition requires action.

        var transforms = includeChildren // Stores an inferred storyline value.
            ? root.GetComponentsInChildren<Transform>(true) // Handles this part of the storyline stage flow.
            : new[] { root.transform }; // Handles this part of the storyline stage flow.

        foreach (var t in transforms) // Loops through configured storyline objects or directives.
        { // Starts this block.
            string goPath = GetHierarchyPath(t); // Stores formatted storyline text or object paths.

            var comps = t.GetComponents<Component>(); // Stores an inferred storyline value.

            foreach (var c in comps) // Loops through configured storyline objects or directives.
            { // Starts this block.
                if (c == null) continue; // Checks whether this storyline condition requires action.

                // Never disable Transform
                if (c is Transform) continue; // Checks whether this storyline condition requires action.

                // ? Keep Mesh Renderers (object stays visible)
                if (c is MeshRenderer) continue; // Checks whether this storyline condition requires action.
                if (c is SkinnedMeshRenderer) continue; // Checks whether this storyline condition requires action.

                // Keep exceptions (PhotonView etc.)
                string typeName = c.GetType().AssemblyQualifiedName; // Stores formatted storyline text or object paths.
                if (keepSet != null && keepSet.Contains((goPath, typeName))) // Checks whether this storyline condition requires action.
                    continue; // Handles this part of the storyline stage flow.

                TrySetEnabled(c, false); // Handles this part of the storyline stage flow.

                if (c is ParticleSystem ps) // Checks whether this storyline condition requires action.
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Stops this configured effect.
            } // Ends this block.
        } // Ends this block.
    } // Ends this block.

    // Uses reflection so it works for MonoBehaviour, Renderer, Collider, etc.
    private static void TrySetEnabled(Component c, bool value) // Handles this part of the storyline stage flow.
    { // Starts this block.
        var type = c.GetType(); // Stores an inferred storyline value.
        var prop = type.GetProperty("enabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public); // Stores an inferred storyline value.
        if (prop != null && prop.PropertyType == typeof(bool) && prop.CanWrite) // Checks whether this storyline condition requires action.
        { // Starts this block.
            prop.SetValue(c, value); // Handles this part of the storyline stage flow.
        } // Ends this block.
    } // Ends this block.
    private void ApplyForEveryone(PuzzleStateManager.PuzzleStage? previous, PuzzleStateManager.PuzzleStage stage) // Applies stage effects locally without Photon RPC.
    { // Starts this block.
        bool isInitialApply = !_hasAppliedStageOnce; // Stores a storyline boolean state value.
        _hasAppliedStageOnce = true; // Marks that this client has applied stage effects.

        StopAllBoardFlashes(); // Clears queue board flashing before new stage effects.

        var prevDirective = previous.HasValue ? FindDirective(previous.Value) : null; // Stores an inferred storyline value.
        var newDirective = FindDirective(stage); // Stores an inferred storyline value.

        if (newDirective == null) return; // Checks whether this storyline condition requires action.

        if (prevDirective != null) // Checks whether this storyline condition requires action.
            foreach (var go in prevDirective.deactivateOnExit) // Loops through configured storyline objects or directives.
                if (go) go.SetActive(false); // Checks whether this storyline condition requires action.

        foreach (var go in newDirective.activateOnEnter) // Loops through configured storyline objects or directives.
            if (go) go.SetActive(true); // Checks whether this storyline condition requires action.

        // Queue texts local
        string[] queueTexts = CollectQueueTexts(newDirective, stage); // Stores a batch of serialized storyline values.
        ApplyQueueTextsAndFlash(newDirective, queueTexts, stage); // Applies this group of storyline effects.

        // Status texts local
        string[] statusTexts = CollectStatusTexts(newDirective, stage); // Stores a batch of serialized storyline values.
        ApplyStatusTexts(newDirective, statusTexts, stage); // Applies this group of storyline effects.

        // Play enter audio on stage enter (local mode too)
        PlayStageEnterAudio(newDirective, isInitialApply); // Handles this part of the storyline stage flow.

        string exitPopupText = previous.HasValue ? CollectPopupMessage(prevDirective?.popupOnExit, previous.Value) : ""; // Stores formatted storyline text or object paths.
        float exitPopupDelay = previous.HasValue ? CollectPopupDelay(prevDirective?.popupOnExit) : 0f; // Stores a timing value for storyline effects.
        string enterPopupText = CollectPopupMessage(newDirective.popupOnEnter, stage); // Stores formatted storyline text or object paths.
        float enterPopupDelay = CollectPopupDelay(newDirective.popupOnEnter); // Stores a timing value for storyline effects.
        ApplyPopupMessages(exitPopupText, exitPopupDelay, enterPopupText, enterPopupDelay, isInitialApply); // Applies this group of storyline effects.

        newDirective.onEnter?.Invoke(); // Invokes configured stage event hooks.
    } // Ends this block.

    // ----------------------------
    // NEW: Stage Enter Audio
    // ----------------------------

    private void PlayStageEnterAudio(StageDirective directive, bool isInitialApply) // Handles this part of the storyline stage flow.
    { // Starts this block.
        if (directive == null) return; // Checks whether this storyline condition requires action.

        // Prevent buffered RPC / late joiners from hearing audio immediately (unless enabled)
        if (isInitialApply && !playEnterAudioOnInitialApply) // Checks whether this storyline condition requires action.
            return; // Leaves the current method early.

        if (directive.enterAudioSource == null) return; // Checks whether this storyline condition requires action.

        // Decide which clip to use:
        // - If directive.enterAudioClip is set, use that.
        // - Else fall back to directive.enterAudioSource.clip.
        AudioClip clipToPlay = directive.enterAudioClip != null // Stores the audio clip chosen for this stage.
            ? directive.enterAudioClip // Handles this part of the storyline stage flow.
            : directive.enterAudioSource.clip; // Handles this part of the storyline stage flow.

        if (clipToPlay == null) // Checks whether this storyline condition requires action.
        { // Starts this block.
            Debug.LogWarning($"[StorylineDirector] Stage {directive.stage} has no AudioClip (enterAudioClip and AudioSource.clip are both null)."); // Writes a storyline warning to the Unity console.
            return; // Leaves the current method early.
        } // Ends this block.

        if (directive.usePlayOneShot) // Checks whether this storyline condition requires action.
        { // Starts this block.
            directive.enterAudioSource.PlayOneShot(clipToPlay, directive.enterAudioVolume); // Plays the stage audio without replacing the source clip.
        } // Ends this block.
        else // Runs when the previous condition is false.
        { // Starts this block.
            // Set clip then play
            directive.enterAudioSource.clip = clipToPlay; // Handles this part of the storyline stage flow.
            directive.enterAudioSource.volume = directive.enterAudioVolume; // Handles this part of the storyline stage flow.
            directive.enterAudioSource.Play(); // Plays the configured stage audio source.
        } // Ends this block.
    } // Ends this block.

    // ----------------------------
    // Path-based helpers
    // ----------------------------

    private static string[] CollectHierarchyPaths(List<GameObject> list) // Converts configured GameObjects into stable hierarchy paths.
    { // Starts this block.
        if (list == null || list.Count == 0) return Array.Empty<string>(); // Checks whether this storyline condition requires action.

        List<string> paths = new(); // Handles this part of the storyline stage flow.
        foreach (var go in list) // Loops through configured storyline objects or directives.
        { // Starts this block.
            if (!go) continue; // Checks whether this storyline condition requires action.
            paths.Add(GetHierarchyPath(go.transform)); // Handles this part of the storyline stage flow.
        } // Ends this block.
        return paths.ToArray(); // Returns the computed storyline value to the caller.
    } // Ends this block.

    // Example: "Environment/Room1/DoorBlocker"
    private static string GetHierarchyPath(Transform t) // Builds a hierarchy path for a scene object.
    { // Starts this block.
        var stack = new Stack<string>(); // Stores an inferred storyline value.
        while (t != null) // Handles this part of the storyline stage flow.
        { // Starts this block.
            stack.Push(t.name); // Handles this part of the storyline stage flow.
            t = t.parent; // Handles this part of the storyline stage flow.
        } // Ends this block.
        return string.Join("/", stack); // Returns the computed storyline value to the caller.
    } // Ends this block.

    // Find including inactive
    private static GameObject FindByHierarchyPathIncludingInactive(string path) // Finds a scene object by a stored hierarchy path.
    { // Starts this block.
        if (string.IsNullOrEmpty(path)) return null; // Checks whether this storyline condition requires action.

        // The first segment is a root object name
        var split = path.Split('/'); // Stores an inferred storyline value.
        if (split.Length == 0) return null; // Checks whether this storyline condition requires action.

        // Find matching root (including inactive)
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(); // Stores an inferred storyline value.
        Transform current = null; // Stores a transform reference.

        foreach (var r in roots) // Loops through configured storyline objects or directives.
        { // Starts this block.
            if (r.name == split[0]) // Checks whether this storyline condition requires action.
            { // Starts this block.
                current = r.transform; // Handles this part of the storyline stage flow.
                break; // Handles this part of the storyline stage flow.
            } // Ends this block.
        } // Ends this block.
        if (current == null) return null; // Checks whether this storyline condition requires action.

        // Traverse children by name, including inactive
        for (int i = 1; i < split.Length; i++) // Loops through indexed storyline data.
        { // Starts this block.
            current = FindChildByNameIncludingInactive(current, split[i]); // Handles this part of the storyline stage flow.
            if (current == null) return null; // Checks whether this storyline condition requires action.
        } // Ends this block.

        return current.gameObject; // Returns the computed storyline value to the caller.
    } // Ends this block.

    private static Transform FindChildByNameIncludingInactive(Transform parent, string childName) // Handles this part of the storyline stage flow.
    { // Starts this block.
        // Search recursively through all children (includes inactive automatically)
        foreach (Transform child in parent) // Loops through configured storyline objects or directives.
        { // Starts this block.
            if (child.name == childName) return child; // Checks whether this storyline condition requires action.

            var deeper = FindChildByNameIncludingInactive(child, childName); // Stores an inferred storyline value.
            if (deeper != null) return deeper; // Checks whether this storyline condition requires action.
        } // Ends this block.
        return null; // Reports that no directive or object was found.
    } // Ends this block.

    private static void ToggleByPaths(string[] paths, bool active) // Handles this part of the storyline stage flow.
    { // Starts this block.
        if (paths == null) return; // Checks whether this storyline condition requires action.

        foreach (var p in paths) // Loops through configured storyline objects or directives.
        { // Starts this block.
            var go = FindByHierarchyPathIncludingInactive(p); // Stores an inferred storyline value.
            if (go != null) // Checks whether this storyline condition requires action.
            { // Starts this block.
                go.SetActive(active); // Shows or hides this configured storyline object.
            } // Ends this block.
            else // Runs when the previous condition is false.
            { // Starts this block.
                Debug.LogWarning($"[StorylineDirector] Could not find GameObject by path: {p}"); // Writes a storyline warning to the Unity console.
            } // Ends this block.
        } // Ends this block.
    } // Ends this block.

    // ----------------------------
    // Your existing queue helpers
    // ----------------------------

    private string[] CollectQueueTexts(StageDirective directive, PuzzleStateManager.PuzzleStage stage) // Builds queue display strings for a stage.
    { // Starts this block.
        if (directive == null || directive.queueDisplayDirectives == null) // Checks whether this storyline condition requires action.
            return Array.Empty<string>(); // Returns the computed storyline value to the caller.

        var result = new List<string>(directive.queueDisplayDirectives.Count); // Stores an inferred storyline value.
        foreach (var q in directive.queueDisplayDirectives) // Loops through configured storyline objects or directives.
        { // Starts this block.
            string txt = FormatQueueText(q.text, stage); // Stores formatted storyline text or object paths.
            result.Add(txt); // Handles this part of the storyline stage flow.
        } // Ends this block.
        return result.ToArray(); // Returns the computed storyline value to the caller.
    } // Ends this block.

    private void ApplyQueueTextsAndFlash(StageDirective directive, string[] texts, PuzzleStateManager.PuzzleStage stage) // Applies queue text updates and flash settings.
    { // Starts this block.
        if (directive == null || directive.queueDisplayDirectives == null) return; // Checks whether this storyline condition requires action.

        for (int i = 0; i < directive.queueDisplayDirectives.Count && i < texts.Length; i++) // Loops through indexed storyline data.
        { // Starts this block.
            var q = directive.queueDisplayDirectives[i]; // Stores an inferred storyline value.

            if (q.display != null) q.display.text = texts[i]; // Checks whether this storyline condition requires action.

            if (q.flashBoardWhenShown && q.board != null) // Checks whether this storyline condition requires action.
                q.board.SetFlash(true); // Handles this part of the storyline stage flow.
        } // Ends this block.
    } // Ends this block.

    private void StopAllBoardFlashes() // Stops flashing on all configured queue boards.
    { // Starts this block.
        if (directives == null) return; // Checks whether this storyline condition requires action.
        foreach (var d in directives) // Loops through configured storyline objects or directives.
        { // Starts this block.
            if (d?.queueDisplayDirectives == null) continue; // Checks whether this storyline condition requires action.
            foreach (var q in d.queueDisplayDirectives) // Loops through configured storyline objects or directives.
                if (q?.board != null) q.board.SetFlash(false); // Checks whether this storyline condition requires action.
        } // Ends this block.
    } // Ends this block.

    private StageDirective FindDirective(PuzzleStateManager.PuzzleStage stage) // Finds the configured directive for a puzzle stage.
    { // Starts this block.
        foreach (var d in directives) // Loops through configured storyline objects or directives.
            if (d.stage.Equals(stage)) // Checks whether this storyline condition requires action.
                return d; // Returns the computed storyline value to the caller.
        return null; // Reports that no directive or object was found.
    } // Ends this block.

    private string FormatQueueText(string template, PuzzleStateManager.PuzzleStage stage) // Formats queue text placeholders for a puzzle stage.
    { // Starts this block.
        if (string.IsNullOrEmpty(template)) return ""; // Checks whether this storyline condition requires action.
        int n = GetPuzzleNumber(stage); // Stores a numeric storyline value.
        return template.Replace("{n}", n.ToString()); // Returns the computed storyline value to the caller.
    } // Ends this block.

    private static int GetPuzzleNumber(PuzzleStateManager.PuzzleStage stage) // Handles this part of the storyline stage flow.
    { // Starts this block.
        string name = stage.ToString(); // Stores formatted storyline text or object paths.
        int i = name.Length - 1; // Stores a numeric storyline value.
        if (i >= 0 && char.IsDigit(name[i])) // Checks whether this storyline condition requires action.
        { // Starts this block.
            int end = i; // Stores a numeric storyline value.
            while (i >= 0 && char.IsDigit(name[i])) i--; // Handles this part of the storyline stage flow.
            if (int.TryParse(name.Substring(i + 1, end - i), out int parsed)) // Checks whether this storyline condition requires action.
                return parsed; // Returns the computed storyline value to the caller.
        } // Ends this block.
        return (int)stage + 1; // Returns the computed storyline value to the caller.
    } // Ends this block.

    // ----------------------------
    // Status text helpers (NEW)
    // ----------------------------

    private string[] CollectStatusTexts(StageDirective directive, PuzzleStateManager.PuzzleStage stage) // Builds status text strings for a stage.
    { // Starts this block.
        if (directive == null || directive.statusTextDirectives == null || directive.statusTextDirectives.Count == 0) // Checks whether this storyline condition requires action.
            return Array.Empty<string>(); // Returns the computed storyline value to the caller.

        var result = new List<string>(directive.statusTextDirectives.Count); // Stores an inferred storyline value.

        for (int i = 0; i < directive.statusTextDirectives.Count; i++) // Loops through indexed storyline data.
        { // Starts this block.
            var s = directive.statusTextDirectives[i]; // Stores an inferred storyline value.
            if (s == null || s.target == null) // Checks whether this storyline condition requires action.
            { // Starts this block.
                result.Add(""); // Handles this part of the storyline stage flow.
                continue; // Handles this part of the storyline stage flow.
            } // Ends this block.

            string msg = s.template ?? ""; // Stores formatted storyline text or object paths.

            // Allow {n}
            int n = GetPuzzleNumber(stage); // Stores a numeric storyline value.
            msg = msg.Replace("{n}", n.ToString()); // Handles this part of the storyline stage flow.

            if (s.useDate) // Checks whether this storyline condition requires action.
            { // Starts this block.
                // ? If user didn't include any date token, auto-add it
                // - Empty template becomes "{date}"
                // - "Todays date:" becomes "Todays date: {date}"
                if (!msg.Contains("{date", StringComparison.Ordinal)) // Checks whether this storyline condition requires action.
                { // Starts this block.
                    msg = msg.TrimEnd(); // Handles this part of the storyline stage flow.
                    msg = string.IsNullOrEmpty(msg) ? "{date}" : (msg + " {date}"); // Handles this part of the storyline stage flow.
                } // Ends this block.

                string fmt = string.IsNullOrEmpty(s.dateFormat) ? "dd MMM yyyy" : s.dateFormat; // Stores formatted storyline text or object paths.
                DateTime baseDate = DateTime.Now.AddDays(s.dateOffsetDays); // Stores the date used for formatted storyline text.

                msg = ReplaceDateTokens(msg, fmt, baseDate); // Handles this part of the storyline stage flow.
            } // Ends this block.
            else // Runs when the previous condition is false.
            { // Starts this block.
                msg = RemoveDateTokens(msg); // Handles this part of the storyline stage flow.
            } // Ends this block.

            result.Add(msg); // Handles this part of the storyline stage flow.
        } // Ends this block.

        return result.ToArray(); // Returns the computed storyline value to the caller.
    } // Ends this block.

    private static string RemoveDateTokens(string input) // Handles this part of the storyline stage flow.
    { // Starts this block.
        if (string.IsNullOrEmpty(input)) return ""; // Checks whether this storyline condition requires action.
        if (!input.Contains("{date")) return input; // Checks whether this storyline condition requires action.

        string output = input; // Stores formatted storyline text or object paths.

        // Remove simple token
        output = output.Replace("{date}", ""); // Handles this part of the storyline stage flow.

        // Remove legacy tokens like {date+2}, {date-5}
        int safety = 50; // Stores a numeric storyline value.
        while (safety-- > 0) // Handles this part of the storyline stage flow.
        { // Starts this block.
            int start = output.IndexOf("{date", StringComparison.Ordinal); // Stores a numeric storyline value.
            if (start < 0) break; // Checks whether this storyline condition requires action.

            int end = output.IndexOf("}", start, StringComparison.Ordinal); // Stores a numeric storyline value.
            if (end < 0) break; // Checks whether this storyline condition requires action.

            string token = output.Substring(start, end - start + 1); // Stores formatted storyline text or object paths.

            // only remove if it matches the legacy token patterns
            if (token.StartsWith("{date+", StringComparison.Ordinal) || token.StartsWith("{date-", StringComparison.Ordinal)) // Checks whether this storyline condition requires action.
            { // Starts this block.
                output = ReplaceFirst(output, token, ""); // Handles this part of the storyline stage flow.
            } // Ends this block.
            else // Runs when the previous condition is false.
            { // Starts this block.
                // If it's some weird token, stop to avoid infinite loop
                break; // Handles this part of the storyline stage flow.
            } // Ends this block.
        } // Ends this block.

        return output; // Returns the computed storyline value to the caller.
    } // Ends this block.

    private void ApplyStatusTexts(StageDirective directive, string[] statusTexts, PuzzleStateManager.PuzzleStage stage) // Applies status text updates to configured TMP targets.
    { // Starts this block.
        if (directive == null || directive.statusTextDirectives == null) return; // Checks whether this storyline condition requires action.
        if (statusTexts == null) return; // Checks whether this storyline condition requires action.

        for (int i = 0; i < directive.statusTextDirectives.Count && i < statusTexts.Length; i++) // Loops through indexed storyline data.
        { // Starts this block.
            var s = directive.statusTextDirectives[i]; // Stores an inferred storyline value.
            if (s == null || s.target == null) continue; // Checks whether this storyline condition requires action.

            if (string.IsNullOrEmpty(statusTexts[i])) continue; // Checks whether this storyline condition requires action.

            s.target.text = statusTexts[i]; // Writes formatted text into a UI label.
        } // Ends this block.
    } // Ends this block.

    private string CollectPopupMessage(PopupMessageDirective directive, PuzzleStateManager.PuzzleStage stage) // Builds the popup message text for a stage transition.
    { // Starts this block.
        if (directive == null || string.IsNullOrWhiteSpace(directive.message)) // Checks whether this storyline condition requires action.
            return ""; // Returns the computed storyline value to the caller.

        return FormatQueueText(directive.message, stage); // Returns the computed storyline value to the caller.
    } // Ends this block.

    private static float CollectPopupDelay(PopupMessageDirective directive) // Reads the popup delay for a directive.
    { // Starts this block.
        if (directive == null) // Checks whether this storyline condition requires action.
            return 0f; // Returns the computed storyline value to the caller.

        return Mathf.Max(0f, directive.delaySeconds); // Returns the computed storyline value to the caller.
    } // Ends this block.

    private void ApplyPopupMessages(string exitPopupText, float exitPopupDelay, string enterPopupText, float enterPopupDelay, bool isInitialApply) // Shows delayed exit and enter popup messages when allowed.
    { // Starts this block.
        if (isInitialApply && !showPopupMessagesOnInitialApply) // Checks whether this storyline condition requires action.
            return; // Leaves the current method early.

        var popup = cluePopupUI != null ? cluePopupUI : CluePopupUI.Instance; // Stores an inferred storyline value.
        if (popup == null) // Checks whether this storyline condition requires action.
            return; // Leaves the current method early.

        if (!string.IsNullOrWhiteSpace(exitPopupText)) // Checks whether this storyline condition requires action.
            popup.ShowDelayed(exitPopupText, exitPopupDelay); // Handles this part of the storyline stage flow.

        if (!string.IsNullOrWhiteSpace(enterPopupText)) // Checks whether this storyline condition requires action.
            popup.ShowDelayed(enterPopupText, enterPopupDelay); // Handles this part of the storyline stage flow.
    } // Ends this block.

    private static string ReplaceDateTokens(string input, string dateFormat, DateTime baseDate) // Handles this part of the storyline stage flow.
    { // Starts this block.
        if (string.IsNullOrEmpty(input)) return ""; // Checks whether this storyline condition requires action.

        if (!input.Contains("{date")) return input; // Checks whether this storyline condition requires action.

        string output = input; // Stores formatted storyline text or object paths.

        // Primary token {date} uses baseDate (NOW + offsetDays)
        string baseStr = baseDate.ToString(dateFormat); // Stores formatted storyline text or object paths.
        output = output.Replace("{date}", baseStr); // Handles this part of the storyline stage flow.

        int safety = 50; // Stores a numeric storyline value.
        while (safety-- > 0) // Handles this part of the storyline stage flow.
        { // Starts this block.
            int start = output.IndexOf("{date", StringComparison.Ordinal); // Stores a numeric storyline value.
            if (start < 0) break; // Checks whether this storyline condition requires action.

            int end = output.IndexOf("}", start, StringComparison.Ordinal); // Stores a numeric storyline value.
            if (end < 0) break; // Checks whether this storyline condition requires action.

            string token = output.Substring(start, end - start + 1); // Stores formatted storyline text or object paths.

            if (token == "{date}") // Checks whether this storyline condition requires action.
            { // Starts this block.
                output = ReplaceFirst(output, token, baseStr); // Handles this part of the storyline stage flow.
                continue; // Handles this part of the storyline stage flow.
            } // Ends this block.

            int offsetDays = 0; // Stores a numeric storyline value.
            bool parsed = false; // Stores a storyline boolean state value.

            if (token.StartsWith("{date+", StringComparison.Ordinal) && token.EndsWith("}")) // Checks whether this storyline condition requires action.
            { // Starts this block.
                string num = token.Substring(6, token.Length - 7); // Stores formatted storyline text or object paths.
                parsed = int.TryParse(num, out offsetDays); // Handles this part of the storyline stage flow.
            } // Ends this block.
            else if (token.StartsWith("{date-", StringComparison.Ordinal) && token.EndsWith("}")) // Checks another storyline condition after the previous one failed.
            { // Starts this block.
                string num = token.Substring(6, token.Length - 7); // Stores formatted storyline text or object paths.
                parsed = int.TryParse(num, out offsetDays); // Handles this part of the storyline stage flow.
                offsetDays = -offsetDays; // Handles this part of the storyline stage flow.
            } // Ends this block.

            if (parsed) // Checks whether this storyline condition requires action.
            { // Starts this block.
                string d = baseDate.AddDays(offsetDays).ToString(dateFormat); // Stores formatted storyline text or object paths.
                output = ReplaceFirst(output, token, d); // Handles this part of the storyline stage flow.
            } // Ends this block.
            else // Runs when the previous condition is false.
            { // Starts this block.
                break; // Handles this part of the storyline stage flow.
            } // Ends this block.
        } // Ends this block.

        return output; // Returns the computed storyline value to the caller.
    } // Ends this block.

    private static string ReplaceFirst(string text, string search, string replace) // Handles this part of the storyline stage flow.
    { // Starts this block.
        int pos = text.IndexOf(search, StringComparison.Ordinal); // Stores a numeric storyline value.
        if (pos < 0) return text; // Checks whether this storyline condition requires action.
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length); // Returns the computed storyline value to the caller.
    } // Ends this block.
} // Ends this block.
