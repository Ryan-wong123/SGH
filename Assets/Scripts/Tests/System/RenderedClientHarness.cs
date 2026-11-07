using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using static SGH.Tests.GameTestFixture;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace SGH.Tests
{
    // Included only in automation builds containing test assemblies. The production
    // player never starts this harness. Both processes render the authored scenes.
    public class RenderedClientHarness : MonoBehaviour
    {
        [Serializable] private class Config { public string appId; public string chatAppId; public string region; }
        [Serializable] private class Report
        {
            public string role, status, error;
            public int sessions, observedStages, clickX, clickY;
            public bool keyboard, mouse, click, avatars, chat;
        }
        private Report report;
        private string directory, room, chatApp;
        private bool host, finished;
        private int session;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!Environment.GetCommandLineArgs().Contains("-sgh-rendered")) return;
            var go = new GameObject("SGH rendered automation");
            DontDestroyOnLoad(go); go.AddComponent<RenderedClientHarness>();
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, name);
            if (i < 0 || i + 1 >= args.Length) throw new InvalidOperationException("Missing automation argument " + name);
            return args[i + 1];
        }

        private void Start()
        {
            directory = Path.GetFullPath(Arg("-sgh-results")); Directory.CreateDirectory(directory);
            host = Arg("-sgh-role") == "host"; room = Arg("-sgh-room");
            report = new Report { role = host ? "host" : "guest", status = "starting" };
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
            SceneManager.sceneLoaded += Isolate;
            StartCoroutine(Guard(Run()));
        }

        private void Save() => File.WriteAllText(Path.Combine(directory, report.role + ".json"), JsonUtility.ToJson(report, true));
        private void Fail(string error)
        {
            if (finished) return;
            finished = true; report.status = "failed"; report.error = error; Save(); Application.Quit(1);
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Fail(message);
        }
        private IEnumerator Guard(IEnumerator routine)
        {
            while (!finished)
            {
                object current;
                try { if (!routine.MoveNext()) yield break; current = routine.Current; }
                catch (Exception e) { Fail(e.ToString()); yield break; }
                if (current is IEnumerator nested) yield return Guard(nested);
                else yield return current;
            }
        }
        private static Component[] All(string type) => FindObjectsByType(RuntimeType(type), FindObjectsInactive.Include, FindObjectsSortMode.None).Cast<Component>().ToArray();
        private void Isolate(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                if (RuntimeType("Network").IsAssignableFrom(component.GetType())) component.enabled = false;
                if (component.GetType().Name == "ChatManager")
                {
                    Set(component, "chatAppId", chatApp);
                    if (!(bool)Get(component, "isProxy")) component.enabled = true;
                }
            }
            var chat = Get(RuntimeType("ChatManager"), "Instance");
            if (chat != null) Set(chat, "chatAppId", chatApp);
            Call(RuntimeType("SessionSmtpCredentials"), "Clear");
            var logger = Get(RuntimeType("GameSessionCsvLogger"), "Instance");
            if (logger != null) Set(logger, "hasWrittenCsv", true);
        }
        private IEnumerator Until(Func<bool> condition, string description, float seconds = 90)
        {
            float due = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < due && !finished) yield return null;
            Assert.That(condition(), Is.True, description);
        }
        private static void CloseComic(bool opening)
        {
            var comic = All("SlideshowUI").First(c => (bool)Get(c, opening ? "advancePuzzleStageOnClose" : "leaveRoomAndLoadLobbyOnClose"));
            comic.gameObject.SetActive(true);
            for (int i = 0; i < ((Array)Get(comic, "slides")).Length; i++) Call(comic, "ShowNextImage");
            ((Button)Get(comic, "closeButton")).onClick.Invoke();
        }
        private bool Prop(string key) => PhotonNetwork.InRoom && Equals(PhotonNetwork.CurrentRoom.CustomProperties[key], true);
        private void Publish(string key) => PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { [key] = true });
        private string Key(string name) => "SGH_TEST_Rendered_" + session + "_" + name;

        private IEnumerator Run()
        {
            var config = JsonUtility.FromJson<Config>(File.ReadAllText(Arg("-sgh-config")));
            Assert.That(Guid.TryParse(config.appId, out _), Is.True, "Dedicated test App ID required");
            Assert.That(Guid.TryParse(config.chatAppId, out _), Is.True, "Dedicated Chat test App ID required");
            chatApp = config.chatAppId;
            // Also fence production reconnect helpers: a default-settings retry
            // must stay in the dedicated test application, never production.
            var fallback = PhotonNetwork.PhotonServerSettings.AppSettings;
            fallback.AppIdRealtime = config.appId; fallback.AppIdChat = config.chatAppId;
            fallback.AppVersion = "sgh-rendered-v1"; fallback.FixedRegion = (string.IsNullOrWhiteSpace(config.region) || config.region.Trim().ToLowerInvariant() == "sg" ? "asia" : config.region.Trim().ToLowerInvariant());
            PhotonNetwork.AuthValues = new AuthenticationValues("sgh-rendered-" + report.role + "-" + Guid.NewGuid().ToString("N"));
            PhotonNetwork.NickName = "Synthetic " + report.role + "-" + room;
            yield return SceneManager.LoadSceneAsync("Lobby");
            PhotonNetwork.AutomaticallySyncScene = true;
            Assert.That(PhotonNetwork.ConnectUsingSettings(new AppSettings
            {
                AppIdRealtime = config.appId, AppVersion = "sgh-rendered-v1", FixedRegion = (string.IsNullOrWhiteSpace(config.region) || config.region.Trim().ToLowerInvariant() == "sg" ? "asia" : config.region.Trim().ToLowerInvariant())
            }), Is.True);
            yield return Until(() => PhotonNetwork.IsConnectedAndReady, "connect dedicated test application");
            yield return Until(() => Get(Get(RuntimeType("ChatManager"), "Instance"), "chatClient") != null &&
                (bool)Get(Get(Get(RuntimeType("ChatManager"), "Instance"), "chatClient"), "CanChat"), "real scene Chat connection");
            Assert.That(Get(Get(RuntimeType("ChatManager"), "Instance"), "currentChannel"), Is.EqualTo("Global"));
            for (session = 1; session <= 2; session++)
            {
                if (host)
                {
                    Assert.That(PhotonNetwork.CreateRoom(room + "-" + session, new RoomOptions { MaxPlayers = 2, PlayerTtl = 60000 }), Is.True);
                    yield return Until(() => PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount == 2, "two real players join");
                    PhotonNetwork.LoadLevel("GameScene");
                }
                else
                {
                    // Room creation is asynchronous. JoinRandomOrCreate would split clients into different rooms.
                    bool waiting = false;
                    Action<short, string> failed = (code, message) => waiting = false;
                    var listener = new JoinFailure(failed); PhotonNetwork.AddCallbackTarget(listener);
                    float deadline = Time.realtimeSinceStartup + 90;
                    try
                    {
                        while (!PhotonNetwork.InRoom && Time.realtimeSinceStartup < deadline)
                        {
                            if (!waiting && PhotonNetwork.IsConnectedAndReady)
                            { waiting = PhotonNetwork.JoinRoom(room + "-" + session); }
                            yield return new WaitForSecondsRealtime(.5f);
                        }
                    }
                    finally { PhotonNetwork.RemoveCallbackTarget(listener); }
                    Assert.That(PhotonNetwork.InRoom, Is.True, "guest joins host room");
                }
                yield return Until(() => SceneManager.GetActiveScene().name == "GameScene", "both render GameScene");
                yield return Until(() => All("PlayerController").Count(c => c.GetComponent<PhotonView>() != null &&
                    c.GetComponent<PhotonView>().InstantiationId > 0 && c.GetComponent<PhotonView>().OwnerActorNr > 0) == 2, "two authored network avatars spawn");
                var local = All("PlayerController").Single(c => c.GetComponent<PhotonView>() != null &&
                    c.GetComponent<PhotonView>().InstantiationId > 0 && c.GetComponent<PhotonView>().IsMine);
                report.avatars = true;
                Publish(Key("ready_" + report.role));
                yield return Until(() => Prop(Key("ready_host")) && Prop(Key("ready_guest")), "scene-ready barrier");
                CloseComic(true);
                yield return Until(() => Convert.ToInt32(Get(Get(RuntimeType("PuzzleStateManager"), "Instance"), "CurrentStage")) == 1, "both opening votes");
                if (session == 1)
                {
                    // Both windows share one X11 focus and pointer. Probe one client at a time
                    // (host, then guest) so an idle window never loses focus mid-keypress/click
                    // and a failure is reported by the client that actually failed.
                    if (!host) yield return Until(() => Prop(Key("input_host")), "host finishes its physical input probe first", 300);
                    yield return ProbeInput(local);
                }
                Publish(Key("input_" + report.role));
                yield return Until(() => Prop(Key("input_host")) && Prop(Key("input_guest")), "both physical input probes", 300);
                yield return ProbeSceneChat();
                yield return new SceneJourneyTests().DriveCurrentGameSceneToLobby(false, PhotonNetwork.LocalPlayer.ActorNumber, host, stage =>
                {
                    report.observedStages++;
                    ScreenCapture.CaptureScreenshot(Path.Combine(directory, report.role + "-session" + session + "-stage" + stage + ".png"));
                });
                if (!host)
                {
                    yield return Until(() => PhotonNetwork.CurrentRoom.PlayerCount == 1, "host exits independently before guest");
                    Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GameScene"));
                    CloseComic(false);
                    yield return Until(() => SceneManager.GetActiveScene().name == "Lobby" && !PhotonNetwork.InRoom, "guest independent ending exit");
                }
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
                yield return Until(() => (string)Get(Get(RuntimeType("ChatManager"), "Instance"), "currentChannel") == "Global", "Chat returns to Lobby channel");
                report.sessions = session; report.status = "between-sessions"; Save();
                yield return Until(() => PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom, "return to master server");
            }
            finished = true; report.status = "passed"; Save(); PhotonNetwork.Disconnect(); Application.Quit(0);
        }

        internal static Button ResolveChatToggleButton(Component chat)
        {
            Assert.That(chat, Is.Not.Null, "Chat singleton must exist before probing its scene controls");
            var assigned = (Button)Get(chat, "collapseButton");
            if (assigned != null && assigned.gameObject.scene == SceneManager.GetActiveScene()) return assigned;
            // GameScene authors its toggle as a persistent UnityEvent on a
            // separate button, without assigning the optional collapseButton field.
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.gameObject.scene != SceneManager.GetActiveScene()) continue;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    if (button.onClick.GetPersistentMethodName(i) == "ToggleChatPanel" &&
                        button.onClick.GetPersistentTarget(i) is Component target && target.GetType() == RuntimeType("ChatManager"))
                        return button;
            }
            Assert.Fail("Active scene has no authored Chat toggle button");
            return null;
        }

        private IEnumerator ProbeInput(Component local)
        {
            Time.timeScale = 1;
            Vector3 position = local.transform.position;
            var camera = local.GetComponentInChildren<Camera>(true);
            Quaternion rotation = camera.transform.rotation;
            report.status = "input-ready"; Save();
            yield return Until(() => Input.GetKey(KeyCode.W), "OS keyboard W reaches game", 120);
            yield return new WaitForSecondsRealtime(1);
            Assert.That(Vector3.Distance(position, local.transform.position), Is.GreaterThan(.05f), "actual PlayerController movement");
            report.keyboard = true; report.status = "mouse-ready"; Save();
            yield return Until(() => Quaternion.Angle(rotation, camera.transform.rotation) > 1, "OS mouse moves player camera", 30);
            report.keyboard = report.mouse = true; report.status = "cursor-ready"; Save();
            yield return Until(() => Cursor.lockState == CursorLockMode.None, "OS Q unlocks the gameplay cursor", 30);
            var chat = Get(RuntimeType("ChatManager"), "Instance");
            var button = ResolveChatToggleButton((Component)chat); Assert.That(button.IsActive(), Is.True, "Authored Chat toggle must be active");
            var rect = (RectTransform)button.transform; var canvas = button.GetComponentInParent<Canvas>();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            report.clickX = Mathf.RoundToInt(point.x); report.clickY = Screen.height - Mathf.RoundToInt(point.y);
            Assert.That(report.clickX, Is.InRange(0, Screen.width - 1)); Assert.That(report.clickY, Is.InRange(0, Screen.height - 1));
            bool before = (bool)Get(chat, "isVisible"); report.status = "click-ready"; Save();
            yield return Until(() => (bool)Get(chat, "isVisible") != before, "OS pointer click reaches the actual Chat Button listener", 30);
            report.click = true; report.status = "input-passed"; Save();
        }

        private IEnumerator ProbeSceneChat()
        {
            var chat = Get(RuntimeType("ChatManager"), "Instance");
            string channel = PhotonNetwork.CurrentRoom.Name;
            yield return Until(() => (string)Get(chat, "currentChannel") == channel &&
                ((System.Collections.IEnumerable)Get(chat, "subscribedChannels")).Cast<string>().Contains(channel), "scene Chat subscribes to gameplay room");
            string incoming = "synthetic-chat-" + session + "-" + (host ? "guest" : "host");
            bool Received() => ((Transform)Get(chat, "chatContent")).GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains(incoming));
            if (!host)
            {
                if ((bool)Get(chat, "isVisible")) Call(chat, "ToggleChatPanel");
                Publish(Key("chat_guest_hidden"));
                yield return Until(() => Received() && (int)Get(chat, "unreadCount") > 0, "real unread message in collapsed scene Chat");
                Call(chat, "ToggleChatPanel"); Assert.That(Get(chat, "unreadCount"), Is.EqualTo(0));
            }
            else
            {
                if (!(bool)Get(chat, "isVisible")) Call(chat, "ToggleChatPanel");
                yield return Until(() => Prop(Key("chat_guest_hidden")), "guest hides Chat");
            }
            ((TMP_InputField)Get(chat, "inputField")).text = "synthetic-chat-" + session + "-" + report.role;
            ((Button)Get(chat, "sendButton")).onClick.Invoke();
            yield return Until(Received, "message reaches other rendered client's Chat UI");
            report.chat = true; Publish(Key("chat_done_" + report.role));
            yield return Until(() => Prop(Key("chat_done_host")) && Prop(Key("chat_done_guest")), "both Chat UI probes finish");
        }

        private sealed class JoinFailure : IMatchmakingCallbacks
        {
            private readonly Action<short, string> failed;
            public JoinFailure(Action<short, string> failed) { this.failed = failed; }
            public void OnJoinRoomFailed(short code, string message) => failed(code, message);
            public void OnFriendListUpdate(System.Collections.Generic.List<FriendInfo> friends) { }
            public void OnCreatedRoom() { }
            public void OnCreateRoomFailed(short code, string message) { }
            public void OnJoinedRoom() { }
            public void OnJoinRandomFailed(short code, string message) { }
            public void OnLeftRoom() { }
        }
    }
}
