using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SGH.Tests
{
    // Uses the real Lobby/GameScene and production validators/transitions.
    // Input is applied at component/event boundaries, not an OS mouse bot.
    // Separate assembly so scene loads never interfere with isolated PR fixtures.
    public class SceneJourneyTests : GameTestFixture
    {
        private readonly List<GameObject> initialRoots = new List<GameObject>();
        private bool configured;
        private static bool commandLineFence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void FenceColdEditorStartup()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-runTests") || !args.Contains("SGH.Tests.System") || args.Contains("-sgh-rendered")) return;
            SceneManager.sceneLoaded -= IsolateServices;
            SceneManager.sceneLoaded += IsolateServices;
            commandLineFence = true;
        }

        private static Component[] All(string type) => UnityEngine.Object.FindObjectsByType(RuntimeType(type), FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Cast<Component>().Where(c => c.gameObject.scene.IsValid()).ToArray();

        private static Component One(string type) => All(type).First(); // Persistent managers move to DontDestroyOnLoad.

        private static void IsolateServices(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var m in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (m == null) continue;
                if (RuntimeType("Network").IsAssignableFrom(m.GetType()) || m.GetType().Name == "ChatManager") m.enabled = false;
                if (m.GetType().Name == "ChatManager") Set(m, "chatAppId", "");
                if (m.GetType().Name == "GameSessionCsvLogger") Set(m, "hasWrittenCsv", true);
            }
            var logger = Get(RuntimeType("GameSessionCsvLogger"), "Instance");
            if (logger != null) Set(logger, "hasWrittenCsv", true); // No CSV or SMTP in scene journey.
            var chat = Get(RuntimeType("ChatManager"), "Instance") as MonoBehaviour;
            if (chat != null)
            {
                Set(chat, "chatAppId", ""); chat.enabled = false;
                var client = Get(chat, "chatClient"); if (client != null) Call(client, "Disconnect");
            }
        }

        private IEnumerator SetupJourney()
        {
            Assert.That(PhotonNetwork.IsConnected, Is.False, "Run system tests in an isolated process");
            UnityEngine.Object.DontDestroyOnLoad(Root);
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.scene.IsValid() && go.transform.parent == null) initialRoots.Add(go);
            Call(RuntimeType("SessionSmtpCredentials"), "Clear");
            IsolateServices(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            if (!configured && !commandLineFence) SceneManager.sceneLoaded += IsolateServices;
            configured = true;
            yield return SceneManager.LoadSceneAsync("Lobby");
            Assert.That(All("Network").Length, Is.GreaterThan(0));
            // Offline CreateRoom dispatches synchronously; drain deferred callback
            // removals before nested property callbacks can mutate that enumeration.
            Call(PhotonNetwork.NetworkingClient, "UpdateCallbackTargets");
            PhotonNetwork.OfflineMode = true; PhotonNetwork.NickName = "Synthetic journey";
            foreach (var network in All("Network")) PhotonNetwork.RemoveCallbackTarget(network);
            foreach (var chat in All("ChatManager")) PhotonNetwork.RemoveCallbackTarget(chat);
            Call(PhotonNetwork.NetworkingClient, "UpdateCallbackTargets");
            Assert.That(PhotonNetwork.CreateRoom("synthetic-journey-" + Guid.NewGuid().ToString("N")), Is.True);
            PhotonNetwork.LoadLevel("GameScene");
            yield return Until(() => SceneManager.GetActiveScene().name == "GameScene", "GameScene load", 120);
            yield return null;
        }

        // Same wait, but the failure message is built only when the wait fails, so it can
        // describe the room state at that moment (artifacts from CI are short-lived).
        private static IEnumerator Until(Func<bool> condition, Func<string> describe, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, describe());
        }

        private static string StageState(object flow, string what)
        {
            // Diagnostics must never replace the real failure, so swallow any error here.
            try
            {
                var parts = new List<string>();
                if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
                    foreach (System.Collections.DictionaryEntry entry in PhotonNetwork.CurrentRoom.CustomProperties)
                        if (entry.Key is string key && (key == "PuzzleLocked" || key == "LockOwner" || key.StartsWith("Pending") || key.StartsWith("SGH_Insulin_")))
                            parts.Add(key + "=" + entry.Value);
                return what + " [CurrentStage=" + Get(flow, "CurrentStage") + "; " + (parts.Count > 0 ? string.Join(", ", parts) : "no lock/pending state") + "]";
            }
            catch (Exception) { return what; }
        }

        private static IEnumerator Until(Func<bool> condition, string message, float seconds = 15)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, message);
        }

        [UnityTearDown]
        public IEnumerator CleanupJourney()
        {
            if (!configured) yield break;
            if (!commandLineFence) SceneManager.sceneLoaded -= IsolateServices;
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom(false);
            yield return null;
            PhotonNetwork.OfflineMode = false; Time.timeScale = 1;
            var empty = SceneManager.CreateScene("SGH system cleanup"); SceneManager.SetActiveScene(empty);
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go != null && go.scene.IsValid() && go.transform.parent == null && !initialRoots.Contains(go) &&
                    go != Root && !go.name.Contains("TestRunner")) UnityEngine.Object.Destroy(go);
            yield return null;
        }

        private static void CloseComic(Component comic)
        {
            comic.gameObject.SetActive(true);
            int count = ((Array)Get(comic, "slides")).Length;
            for (int i = 0; i < count; i++) Call(comic, "ShowNextImage");
            ((Button)Get(comic, "closeButton")).onClick.Invoke();
        }

        private static void PlaceCards(string slotType, GameObject panel, bool stageTwo = false)
        {
            var slots = panel.GetComponentsInChildren(RuntimeType(slotType), true).Cast<Component>().ToArray();
            var cards = panel.GetComponentsInChildren(RuntimeType("DraggableCard"), true).Cast<Component>().ToList();
            var used = new HashSet<Component>();
            foreach (var slot in slots)
            {
                if (slotType == "Activity9DropSlot" && slot.CompareTag(stageTwo ? "Stage1Slot" : "Stage2Slot")) continue;
                Component card = cards.FirstOrDefault(c => !used.Contains(c) &&
                    (slotType == "Activity9DropSlot" && !stageTwo || (bool)Call(slot, "IsCardCorrect", c)));
                if (card == null && slotType == "Activity1_2DropSlot") continue; // Fifteen authored destinations, nine bears.
                Assert.That(card, Is.Not.Null, "No matching authored card for " + slot.name);
                used.Add(card);
                // Use the production layout application (including synced state),
                // while avoiding dependence on desktop pointer coordinates.
                Call(card, "ApplyLayoutState", slot.transform, Vector2.zero, Vector2.one);
            }
        }

        private static void SolveInputs(int stage, Component flow, GameObject panel)
        {
            switch (stage)
            {
                case 1: PlaceCards("Activity1DropSlot", panel); break;
                case 2:
                    PlaceCards("Activity1_2DropSlot", panel);
                    Assert.That(panel.GetComponentsInChildren(RuntimeType("Activity1_2DropSlot"), true).Cast<Component>()
                        .Sum(s => ((IList)Call(s, "GetCardsInSlot")).Count), Is.EqualTo((int)Get(Get(flow, "bearCardManager"), "required")));
                    break;
                case 3:
                    var pictures = Get(flow, "pictureGridManager");
                    var buttons = (IList)Get(pictures, "pictureButtons");
                    foreach (int index in (IEnumerable)Get(pictures, "correctIndexes")) ((Button)buttons[index]).onClick.Invoke();
                    break;
                case 4:
                    var survival = Get(flow, "activity2PuzzleManager");
                    if ((int)Get(survival, "CurrentPart") == 1) PlaceCards("Activity2DropSlot", panel);
                    else { ((TMP_InputField)Get(survival, "input1")).text = "15"; ((TMP_InputField)Get(survival, "input2")).text = "4"; }
                    break;
                case 5:
                    var search = Get(flow, "wordSearchManager"); var grid = (Array)Get(search, "grid");
                    var positions = new HashSet<Vector2Int>();
                    foreach (string word in new[] { "FLUOROQUINOLONES", "SULFONYLUREAS", "LINEZOLID", "AZOLES", "ELDERLY", "IMPAIRED", "REDUCTION" })
                    {
                        object[] args = { word, null }; Assert.That(Call(search, "TryFindWordPositions", args), Is.True);
                        foreach (Vector2Int p in (IEnumerable)args[1]) positions.Add(p);
                    }
                    foreach (var p in positions) Call(search, "ToggleCell", grid.GetValue(p.x, p.y));
                    break;
                case 6:
                    var diary = Get(flow, "diaryPuzzleManager");
                    foreach (object answer in (IEnumerable)Get(diary, "answers"))
                        ((TMP_InputField)Get(answer, "field")).text = ((string[])Get(answer, "accepted"))[0];
                    break;
                case 7:
                    foreach (object answer in (IEnumerable)Get(Get(flow, "risktableManager"), "dropdowns"))
                        ((TMP_Dropdown)Get(answer, "dropdown")).value = (int)Get(answer, "correctIndex");
                    break;
                case 8: ((TMP_InputField)Get(Get(flow, "prescriptionManager"), "prescriptionAmount")).text = "28"; break;
                case 9:
                    var crossword = Get(flow, "crosswordManager"); var solution = (char[,])Get(crossword, "solution");
                    var cells = (Array)Get(crossword, "grid");
                    for (int y = 0; y < 13; y++) for (int x = 0; x < 13; x++) if (solution[y, x] != '\0')
                        ((TMP_InputField)Get(cells.GetValue(y, x), "inputField")).text = solution[y, x].ToString();
                    break;
                case 10: PlaceCards("Activity6DropSlot", panel); break;
                case 11: PlaceCards("Activity9DropSlot", panel, (int)Get(Get(flow, "deathLapsesManager"), "CurrentStage") == 2); break;
            }
        }

        [UnityTest, Category("Journey"), Timeout(600000)]
        public IEnumerator RealScenePuzzleChainAndIndependentEndingExit()
        {
            yield return SetupJourney();
            yield return DriveCurrentGameSceneToLobby();
        }

        public IEnumerator DriveCurrentGameSceneToLobby(bool closeOpening = true, int participatingActor = 0, bool closeEnding = true, Action<int> onStage = null)
        {
            var flow = One("PuzzleStateManager");
            if (closeOpening) CloseComic(All("SlideshowUI").First(c => (bool)Get(c, "advancePuzzleStageOnClose")));
            yield return Until(() => Convert.ToInt32(Get(flow, "CurrentStage")) == 1, "opening comic close advances stage");
            for (int stage = 1; stage <= 11; stage++)
            {
                Assert.That(Convert.ToInt32(Get(flow, "CurrentStage")), Is.EqualTo(stage));
                onStage?.Invoke(stage);
                if (participatingActor > 0 && participatingActor != (stage % 2 == 1 ? 1 : 2))
                {
                    Call(flow, "OpenCurrentStagePanel", stage % 2 == 1 ? 1 : 2);
                    var observedPanel = (GameObject)Get(flow, "Puzzle" + stage + "Panel");
                    // Passive wait for the partner's whole stage (stage 10 alone is a keypad, a 2s deadline and two
                    // parts). The partner's own steps have their own timeouts, so a stuck partner still fails first.
                    yield return Until(() => stage < 11 ? Convert.ToInt32(Get(flow, "CurrentStage")) == stage + 1 : !observedPanel.activeSelf, () => StageState(flow, "remote completion of stage " + stage), 180);
                    Time.timeScale = 1;
                    continue;
                }
                if (stage == 10)
                {
                    var lid = (Component)Get(flow, "puzzle10ChestLid");
                    var keypad = All("NavKeypad.Keypad").First(k => ((Array)Get(k, "doorsToUnlock")).Cast<object>().Contains(lid));
                    int code = (int)Get(keypad, "keypadCombo");
                    // Online, the master decides and broadcasts the result, so the denial only appears a
                    // round trip after CheckCombo. Wait for it to start, then to clear; otherwise the
                    // retry is appended to the uncleared wrong code and never forms a valid combo.
                    float keypadSeconds = participatingActor > 0 ? 60f : 15f;
                    Call(keypad, "AddInput", (code + 1).ToString()); Call(keypad, "CheckCombo");
                    yield return Until(() => (bool)Get(keypad, "displayingResult"), "keypad denial is shown", keypadSeconds);
                    yield return Until(() => !(bool)Get(keypad, "displayingResult"), "keypad denial clears for retry", keypadSeconds);
                    Assert.That(Get(lid, "HasBeenOpened"), Is.False, "Wrong code must not unlock the chest");
                    Call(keypad, "AddInput", code.ToString()); Call(keypad, "CheckCombo");
                    yield return Until(() => (bool)Get(lid, "HasBeenOpened"), "correct keypad opens prerequisite lid", keypadSeconds);
                }
                object[] attempt = { stage == 10 ? Get(flow, "puzzle10SyringeTrigger") : null, null };
                // The shared attempt lock is released by the master over the network, so a teammate's
                // lock can still be visible for a moment after the stage advances. The game tells the
                // player to try again; do the same, bounded, and fail with the final refusal reason.
                bool began = (bool)Call(flow, "TryBeginAttempt", attempt);
                for (float retryUntil = Time.realtimeSinceStartup + 20f; !began && Time.realtimeSinceStartup < retryUntil;)
                {
                    yield return new WaitForSecondsRealtime(.25f);
                    attempt[1] = null;
                    began = (bool)Call(flow, "TryBeginAttempt", attempt);
                }
                Assert.That(began, Is.True, "Start stage " + stage + ": " + attempt[1]);
                Call(flow, "OpenCurrentStagePanel", PhotonNetwork.LocalPlayer.ActorNumber);
                yield return null;
                var panel = (GameObject)Get(flow, "Puzzle" + stage + "Panel");
                int parts = stage == 4 || stage == 11 ? 2 : stage == 6 ? 3 : 1;
                if (stage == 10)
                {
                    var insulin = Get(flow, "insulinPuzzleManager");
                    ((TMP_Dropdown)Get(insulin, "syringeDropdown")).value = 1;
                    Call(insulin, "SubmitSyringeAnswer");
                    // Online: guest -> master request, a 2s durable deadline, then the master publishes part 2.
                    // If that first request is dropped (CI showed the master never scheduling it while the guest
                    // held the lock), do what a player does and press Submit again. The master's compare-and-swap
                    // ignores repeats, so this cannot extend the deadline. Offline never retries.
                    float insulinDue = Time.realtimeSinceStartup + (participatingActor > 0 ? 60f : 15f);
                    while ((int)Get(insulin, "CurrentPart") != 2 && Time.realtimeSinceStartup < insulinDue)
                    {
                        float retryAt = Mathf.Min(Time.realtimeSinceStartup + 3f, insulinDue);
                        while ((int)Get(insulin, "CurrentPart") != 2 && Time.realtimeSinceStartup < retryAt) yield return null;
                        if (participatingActor > 0 && (int)Get(insulin, "CurrentPart") != 2 && Time.realtimeSinceStartup < insulinDue)
                            Call(insulin, "SubmitSyringeAnswer");
                    }
                    Assert.That((int)Get(insulin, "CurrentPart"), Is.EqualTo(2), StageState(flow, "insulin durable deadline"));
                }
                for (int part = 0; part < parts; part++)
                {
                    if (participatingActor > 0)
                    {
                        var views = panel.GetComponentsInChildren(RuntimeType("DraggableCard"), false).Cast<Component>()
                            .Select(c => c.GetComponent<PhotonView>()).Where(v => v != null && v.ViewID > 0).ToArray();
                        foreach (var view in views)
                        {
                            Assert.That(view.OwnershipTransfer, Is.Not.EqualTo(OwnershipOption.Fixed), "Shared card must allow ownership transfer");
                            if (!view.IsMine) view.RequestOwnership();
                        }
                        yield return Until(() => views.All(v => v.IsMine), "solver owns active cards", 30);
                    }
                    SolveInputs(stage, flow, panel);
                    if (participatingActor > 0) yield return new WaitForSecondsRealtime(.25f);
                    Call(flow, "Submit"); yield return null;
                }
                // Two real clients (Photon latency, software rendering) need more than the offline 15s.
                float completionSeconds = participatingActor > 0 ? 90f : 15f;
                if (stage < 11) yield return Until(() => Convert.ToInt32(Get(flow, "CurrentStage")) == stage + 1, () => StageState(flow, "completion of stage " + stage), completionSeconds);
                else yield return Until(() => !panel.activeSelf, () => StageState(flow, "final puzzle closes"), completionSeconds);
                Time.timeScale = 1;
            }
            yield return Until(() => (bool)Call(Get(RuntimeType("InventorySystem"), "Instance"), "HasItem", "Time Crystal5"), "final crystal reward");
            foreach (string crystal in new[] { "Time Crystal1", "Time Crystal2", "Time Crystal3", "Time Crystal4", "Time Crystal5" })
                Assert.That(Call(Get(RuntimeType("InventorySystem"), "Instance"), "HasItem", crystal), Is.True, crystal);
            if (!closeEnding) yield break;
            CloseComic(All("SlideshowUI").First(c => (bool)Get(c, "leaveRoomAndLoadLobbyOnClose")));
            yield return Until(() => SceneManager.GetActiveScene().name == "Lobby" && !PhotonNetwork.InRoom, "independent ending exit");
        }

        [UnityTest, Category("Journey"), Timeout(600000)]
        public IEnumerator SecondFullJourneyInSameProcessStartsFresh()
        {
            yield return RealScenePuzzleChainAndIndependentEndingExit();
            PhotonNetwork.OfflineMode = false;
            yield return RealScenePuzzleChainAndIndependentEndingExit();
        }
    }
}
