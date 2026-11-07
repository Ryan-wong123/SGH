using System;
using System.Collections;
using NUnit.Framework;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace SGH.Tests
{
    // One real PUN client running production flow + an independent Realtime peer.
    // The peer leaves the room for real; Photon elects the PUN client as master.
    public class LivePunRecoveryTests : GameTestFixture
    {
        private LoadBalancingClient peer;
        private Component flow;
        private object previousFlow;
        private object previousLogger;
        private string version;

        [SetUp]
        public void CaptureRuntimeState()
        {
            previousFlow = Get(RuntimeType("PuzzleStateManager"), "Instance");
            previousLogger = Get(RuntimeType("GameSessionCsvLogger"), "Instance");
            version = PhotonNetwork.GameVersion;
            flow = null; peer = null;
        }

        private IEnumerator Until(Func<bool> condition, string message, float timeout = 45)
        {
            float due = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < due) { peer?.Service(); yield return null; }
            Assert.That(condition(), Is.True, message);
        }

        private IEnumerator Connect(int stage)
        {
            string app = LiveTestConfig.Get("SGH_PHOTON_APP_ID");
            Assert.That(Guid.TryParse(app, out _), Is.True, "Dedicated SGH_PHOTON_APP_ID required");
            Assert.That(PhotonNetwork.IsConnected, Is.False);
            Set(RuntimeType("GameSessionCsvLogger"), "<Instance>k__BackingField", null);
            Call(RuntimeType("SessionSmtpCredentials"), "Clear");
            PhotonNetwork.GameVersion = "sgh-live-pun-v1";
            var settings = new AppSettings
            {
                AppIdRealtime = app, AppVersion = PhotonNetwork.NetworkingClient.AppVersion,
                FixedRegion = LiveTestConfig.Region
            };
            peer = new LoadBalancingClient();
            peer.AuthValues = new AuthenticationValues("sgh-owner-" + Guid.NewGuid().ToString("N"));
            Assert.That(peer.ConnectUsingSettings(settings), Is.True);
            yield return Until(() => peer.State == ClientState.ConnectedToMasterServer, "owner connection");
            string name = "sgh-pun-ci-" + Guid.NewGuid().ToString("N");
            Assert.That(peer.OpCreateRoom(new EnterRoomParams { RoomName = name, RoomOptions = new RoomOptions { MaxPlayers = 2, PlayerTtl = 60000, EmptyRoomTtl = 2000 } }), Is.True);
            yield return Until(() => peer.InRoom, "owner creates room");
            PhotonNetwork.AuthValues = new AuthenticationValues("sgh-pun-" + Guid.NewGuid().ToString("N"));
            settings.AppVersion = "sgh-live-pun-v1"; // PUN adds its own version suffix.
            Assert.That(PhotonNetwork.ConnectUsingSettings(settings), Is.True);
            yield return Until(() => PhotonNetwork.IsConnectedAndReady, "PUN connection");
            Assert.That(PhotonNetwork.JoinRoom(name), Is.True);
            yield return Until(() => PhotonNetwork.InRoom && peer.CurrentRoom.PlayerCount == 2, "PUN joins owner");
            Assert.That(PhotonNetwork.IsMasterClient, Is.False);
            flow = Component("PuzzleStateManager");
            var view = flow.gameObject.AddComponent<PhotonView>(); view.ViewID = 991;
            Set(flow, "CurrentStage", Enum.ToObject(RuntimeType("PuzzleStateManager+PuzzleStage"), stage));
            Set(RuntimeType("PuzzleStateManager"), "Instance", flow);
            for (int i = 1; i <= 11; i++) Set(flow, "Puzzle" + i + "Panel", Object("Panel" + i));
            Call(flow, "InitStagePanels");
            PhotonNetwork.AddCallbackTarget(flow);
        }

        protected override void BeforeDestroyRoot()
        {
            if (flow != null) PhotonNetwork.RemoveCallbackTarget(flow);
            // Never-activated views do not receive OnDestroy. NUnit destroys the
            // hierarchy before Unity's coroutine teardown starts on this runner.
            if (Root != null)
                foreach (var view in Root.GetComponentsInChildren<PhotonView>(true))
                    PhotonNetwork.LocalCleanPhotonView(view);
        }

        [UnityTearDown]
        public IEnumerator CleanupLiveRoom()
        {
            Set(RuntimeType("PuzzleStateManager"), "Instance", previousFlow);
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom(false);
            peer?.Disconnect(); PhotonNetwork.Disconnect();
            float due = Time.realtimeSinceStartup + 10;
            while ((PhotonNetwork.IsConnected || peer?.IsConnected == true) && Time.realtimeSinceStartup < due) { peer?.Service(); yield return null; }
            if (version != null) PhotonNetwork.GameVersion = version;
            Set(RuntimeType("GameSessionCsvLogger"), "<Instance>k__BackingField", previousLogger);
            Assert.That(PhotonNetwork.GetPhotonView(991), Is.Null, "Inactive fixture view must be released before coroutine teardown");
            Assert.That(PhotonNetwork.IsConnected || peer?.IsConnected == true, Is.False, "Both test clients disconnect between cases");
        }

        [UnityTest]
        public IEnumerator FixtureViewIsReleasedBeforeCoroutineTeardown()
        {
            Assert.That(PhotonNetwork.IsConnected, Is.False);
            var view = Object("Inactive registered fixture view").AddComponent<PhotonView>();
            view.ViewID = 991;
            Assert.That(PhotonNetwork.GetPhotonView(991), Is.SameAs(view));
            // The real NUnit/UTF teardown order must release this never-awakened
            // view before CleanupLiveRoom checks the registry, without Cloud IDs.
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstDragDefersSlotAcceptanceUntilRealRemoteOwnershipTransfer()
        {
            yield return Connect(1);
            var ui = new GameObject("Synthetic drag canvas", typeof(RectTransform)); ui.SetActive(false);
            var canvas = ui.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var home = new GameObject("Home", typeof(RectTransform)); home.transform.SetParent(ui.transform, false);
            var target = new GameObject("Target", typeof(RectTransform)); target.transform.SetParent(ui.transform, false);
            var slot = target.AddComponent(RuntimeType("DropSlot"));
            var cardObject = new GameObject("Synthetic card", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardObject.transform.SetParent(home.transform, false);
            var view = cardObject.AddComponent<PhotonView>(); view.OwnershipTransfer = OwnershipOption.Request; view.ViewID = 1001;
            var card = cardObject.AddComponent(RuntimeType("DraggableCard")); Set(card, "canvas", canvas); Set(card, "pathRoot", ui.transform);
            var events = new GameObject("Synthetic pointer", typeof(EventSystem));
            bool requested = false;
            Action<ExitGames.Client.Photon.EventData> ownerListener = e => { if (e.Code == 209) requested = true; };
            peer.EventReceived += ownerListener;
            try
            {
                ui.SetActive(true); yield return null;
                Assert.That(view.OwnerActorNr, Is.EqualTo(peer.LocalPlayer.ActorNumber));
                var pointer = new PointerEventData(events.GetComponent<EventSystem>()) { pointerDrag = cardObject, position = new Vector2(100, 100) };
                Call(card, "OnBeginDrag", pointer);
                Assert.That(cardObject.transform.parent, Is.EqualTo(ui.transform), "First gesture previews immediately");
                Call(card, "OnEndDrag", pointer); Call(slot, "OnDrop", pointer);
                yield return Until(() => requested, "remote owner receives actual ownership request");
                Assert.That(cardObject.transform.parent, Is.EqualTo(ui.transform), "Unconfirmed ownership cannot commit the slot");
                Assert.That(peer.OpRaiseEvent(210, new[] { view.ViewID, PhotonNetwork.LocalPlayer.ActorNumber },
                    new RaiseEventOptions { Receivers = ReceiverGroup.Others }, ExitGames.Client.Photon.SendOptions.SendReliable), Is.True);
                yield return Until(() => view.IsMine && cardObject.transform.parent == target.transform, "production callback completes deferred drop");
                yield return Until(() => peer.CurrentRoom.CustomProperties.ContainsKey("CardParentPath_1001"), "authoritative layout reaches remote client");
                Assert.That(Get(card, "waitingForOwnership"), Is.False);
                Assert.That(Get(card, "waitingToFinalizeDrop"), Is.False);
            }
            finally
            {
                peer.EventReceived -= ownerListener;
                UnityEngine.Object.Destroy(ui); UnityEngine.Object.Destroy(events);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProductionMasterCallbackRepairsOrphanLockAndCompletesPendingStage()
        {
            yield return Connect(1);
            peer.CurrentRoom.SetCustomProperties(new Hashtable
            {
                ["PuzzleLocked"] = true, ["LockOwner"] = peer.LocalPlayer.ActorNumber,
                ["Round"] = 1, ["PendingAdvanceStage"] = 1, ["PendingAdvanceAt"] = PhotonNetwork.Time + .2d
            });
            yield return Until(() => PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("PendingAdvanceAt"), "pending transition replication");
            yield return new WaitForSecondsRealtime(.3f); peer.Disconnect();
            yield return Until(() => PhotonNetwork.IsMasterClient, "master election");
            yield return Until(() => Convert.ToInt32(Get(flow, "CurrentStage")) == 2, "production master callback advances stage");
            yield return Until(() => Equals(PhotonNetwork.CurrentRoom.CustomProperties["PuzzleLocked"], false), "orphan lock repair");
            Assert.That(PhotonNetwork.CurrentRoom.CustomProperties["LockOwner"], Is.EqualTo(-1));
            Assert.That(PhotonNetwork.CurrentRoom.CustomProperties["PendingAdvanceStage"], Is.EqualTo(-1));
        }

        [UnityTest]
        public IEnumerator ProductionInsulinDeadlineResumesAfterMasterMigrationAndResetWins()
        {
            yield return Connect(10);
            var insulin = Component("InsulinPuzzleManager");
            Set(insulin, "syringeDropdown", Dropdown(1)); Set(insulin, "panel1", Object("Part1"));
            var part2 = Object("Part2"); part2.SetActive(false); Set(insulin, "panel2", part2);
            Set(flow, "insulinPuzzleManager", insulin);
            var snapshot = (Hashtable)Call(RuntimeType("InsulinRoomState"), "Snapshot", 4, 1, PhotonNetwork.Time + .2d);
            snapshot["Round"] = 1; snapshot["PuzzleLocked"] = true; snapshot["LockOwner"] = peer.LocalPlayer.ActorNumber;
            peer.CurrentRoom.SetCustomProperties(snapshot);
            yield return Until(() => PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SGH_Insulin_Part"), "insulin snapshot");
            yield return new WaitForSecondsRealtime(.3f); peer.Disconnect();
            yield return Until(() => PhotonNetwork.IsMasterClient && (int)Get(insulin, "CurrentPart") == 2, "production recovery to part two");
            Call(insulin, "ResetPuzzle");
            yield return Until(() => Equals(PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_Revision"], 5), "reset revision");
            Call(flow, "ProcessPendingInsulinAdvance");
            Assert.That(Get(insulin, "CurrentPart"), Is.EqualTo(1));
            Assert.That(PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_AdvanceAt"], Is.EqualTo(0d));
        }

        [UnityTest]
        public IEnumerator RealInactiveActorCannotBlockProductionClueCloseVoteCount()
        {
            yield return Connect(3);
            var clue = Component("ClueUI");
            Set(clue, "mainRoot", Object("Clue root")); Set(clue, "_currentPanel", Object("Clue page"));
            Set(clue, "closeButton", Object("Close").AddComponent<Button>());
            Call(clue, "EnsureCloseGateKeys", "synthetic");
            string round = (string)Get(clue, "_clueCloseRoundPropertyKey");
            peer.CurrentRoom.SetCustomProperties(new Hashtable { [round] = 1 });
            yield return Until(() => PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(round), "vote round");
            Assert.That(Call(clue, "GetRemainingPlayersCount"), Is.EqualTo(2));
            Call(clue, "SetLocalPlayerCloseClicked", true);
            yield return Until(() => (int)Call(clue, "GetRemainingPlayersCount") == 1, "local vote");
            peer.Disconnect();
            yield return Until(() => PhotonNetwork.CurrentRoom.GetPlayer(1).IsInactive, "inactive peer");
            Assert.That(Call(clue, "GetRemainingPlayersCount"), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator UnauthorizedSyringeRpcIsRejectedByActualMasterHandler()
        {
            yield return Connect(10);
            var insulin = Component("InsulinPuzzleManager");
            Set(insulin, "syringeDropdown", Dropdown(1)); Set(insulin, "panel1", Object("Part1"));
            var page2 = Object("Part2"); page2.SetActive(false); Set(insulin, "panel2", page2);
            Set(flow, "insulinPuzzleManager", insulin);
            peer.Disconnect(); yield return Until(() => PhotonNetwork.IsMasterClient && !peer.IsConnected, "master election");
            Assert.That(peer.ReconnectAndRejoin(), Is.True); yield return Until(() => peer.InRoom, "non-master rejoin");
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                ["PuzzleLocked"] = true, ["LockOwner"] = PhotonNetwork.LocalPlayer.ActorNumber,
                ["SGH_Insulin_Revision"] = 0, ["SGH_Insulin_Part"] = 1, ["SGH_Insulin_AdvanceAt"] = 0d
            });
            yield return Until(() => Equals(peer.CurrentRoom.CustomProperties["LockOwner"], PhotonNetwork.LocalPlayer.ActorNumber), "owned attempt");
            int received = 0;
            Action<ExitGames.Client.Photon.EventData> observer = e => { if (e.Code == 200) received++; };
            PhotonNetwork.NetworkingClient.EventReceived += observer;
            try
            {
                // PUN's actual RPC wire format, authored by the unrelated client.
                var rpc = new Hashtable { [(byte)0] = 991, [(byte)2] = PhotonNetwork.ServerTimestamp,
                    [(byte)3] = "RPC_RequestInsulinAdvance", [(byte)4] = new object[] { 1, 0 } };
                Assert.That(peer.OpRaiseEvent(200, rpc, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, ExitGames.Client.Photon.SendOptions.SendReliable), Is.True);
                yield return Until(() => received == 1, "actual unauthorized RPC dispatch");
                Assert.That(PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_AdvanceAt"], Is.EqualTo(0d));
                Call(flow, "RequestInsulinPart2Transition", 1);
                yield return Until(() => (double)PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_AdvanceAt"] > 0, "authorized owner can still schedule");
            }
            finally { PhotonNetwork.NetworkingClient.EventReceived -= observer; }
        }

        // ---- Insulin hardening: real master, real second client, real RPCs ----

        private void SetUpInsulin()
        {
            var insulin = Component("InsulinPuzzleManager");
            Set(insulin, "syringeDropdown", Dropdown(1)); Set(insulin, "panel1", Object("Part1"));
            var page2 = Object("Part2"); page2.SetActive(false); Set(insulin, "panel2", page2);
            Set(flow, "insulinPuzzleManager", insulin);
        }

        // The PUN client becomes the real master and the independent client rejoins as a non-master player.
        private IEnumerator ConnectWithPunAsMaster()
        {
            yield return Connect(10);
            SetUpInsulin();
            peer.Disconnect(); yield return Until(() => PhotonNetwork.IsMasterClient && !peer.IsConnected, "master election");
            Assert.That(peer.ReconnectAndRejoin(), Is.True); yield return Until(() => peer.InRoom, "non-master rejoin");
            int peerActor = peer.LocalPlayer.ActorNumber;
            yield return Until(() => PhotonNetwork.CurrentRoom.GetPlayer(peerActor) != null && !PhotonNetwork.CurrentRoom.GetPlayer(peerActor).IsInactive,
                "peer is an active player in the master's room view");
        }

        // PUN's actual RPC wire format, authored by the independent client.
        private bool SendRpcFromPeer(string method, params object[] arguments)
        {
            var rpc = new Hashtable { [(byte)0] = 991, [(byte)2] = PhotonNetwork.ServerTimestamp,
                [(byte)3] = method, [(byte)4] = arguments };
            return peer.OpRaiseEvent(200, rpc, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
                ExitGames.Client.Photon.SendOptions.SendReliable);
        }

        private static Hashtable InsulinRoom(bool locked, int owner, double advanceAt)
        {
            return new Hashtable
            {
                ["PuzzleLocked"] = locked, ["LockOwner"] = owner,
                ["SGH_Insulin_Revision"] = 0, ["SGH_Insulin_Part"] = 1, ["SGH_Insulin_AdvanceAt"] = advanceAt
            };
        }

        private IEnumerator Settle(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) { peer?.Service(); yield return null; }
        }

        [UnityTest]
        public IEnumerator InsulinRequestFromARealPeerOnAFreeLockIsAcceptedAndRepeatsDoNotExtendIt()
        {
            yield return ConnectWithPunAsMaster();
            int peerActor = peer.LocalPlayer.ActorNumber;
            PhotonNetwork.CurrentRoom.SetCustomProperties(InsulinRoom(false, -1, 0d));
            yield return Until(() => Equals(peer.CurrentRoom.CustomProperties["LockOwner"], -1), "free lock visible to the peer");

            Assert.That(SendRpcFromPeer("RPC_RequestInsulinAdvance", 1, 0), Is.True);
            yield return Until(() => Equals(PhotonNetwork.CurrentRoom.CustomProperties["LockOwner"], peerActor) &&
                (double)PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_AdvanceAt"] > 0,
                "the master takes the free lock for the peer and schedules the deadline");
            double due = (double)PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_AdvanceAt"];

            for (int i = 0; i < 20; i++) Assert.That(SendRpcFromPeer("RPC_RequestInsulinAdvance", 1, 0), Is.True);
            yield return Settle(.4f);
            var properties = PhotonNetwork.CurrentRoom.CustomProperties;
            Assert.That(Equals(properties["SGH_Insulin_Part"], 2) || (double)properties["SGH_Insulin_AdvanceAt"] == due, Is.True,
                "Repeated requests must not extend or replace the deadline");
            Assert.That(properties["LockOwner"], Is.EqualTo(peerActor));
        }

        // The ordering that used to drop the request: the syringe request is handled before the lock request.
        [UnityTest]
        public IEnumerator InsulinRequestArrivingBeforeTheLockRequestIsNotDropped()
        {
            yield return ConnectWithPunAsMaster();
            int peerActor = peer.LocalPlayer.ActorNumber;
            PhotonNetwork.CurrentRoom.SetCustomProperties(InsulinRoom(false, -1, 0d));
            yield return Until(() => Equals(peer.CurrentRoom.CustomProperties["LockOwner"], -1), "free lock visible to the peer");

            Assert.That(SendRpcFromPeer("RPC_RequestInsulinAdvance", 1, 0), Is.True);
            Assert.That(SendRpcFromPeer("RPC_RequestLockFromMaster", peerActor), Is.True); // arrives second, in the same order as sent
            yield return Until(() => Equals(PhotonNetwork.CurrentRoom.CustomProperties["LockOwner"], peerActor) &&
                (double)PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_AdvanceAt"] > 0,
                "a syringe request that beat the lock request is still accepted");
            yield return Settle(.3f);
            Assert.That(PhotonNetwork.CurrentRoom.CustomProperties["PuzzleLocked"], Is.EqualTo(true));
            Assert.That(PhotonNetwork.CurrentRoom.CustomProperties["LockOwner"], Is.EqualTo(peerActor), "The late lock request must not undo the lock");
        }

        [UnityTest]
        public IEnumerator InsulinRequestFromARealPeerIsRejectedWhileTheMasterHoldsTheLock()
        {
            yield return ConnectWithPunAsMaster();
            int masterActor = PhotonNetwork.LocalPlayer.ActorNumber;
            PhotonNetwork.CurrentRoom.SetCustomProperties(InsulinRoom(true, masterActor, 0d));
            yield return Until(() => Equals(peer.CurrentRoom.CustomProperties["LockOwner"], masterActor), "lock held by the master");

            for (int i = 0; i < 5; i++) Assert.That(SendRpcFromPeer("RPC_RequestInsulinAdvance", 1, 0), Is.True);
            yield return Settle(.5f);
            Assert.That(PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_AdvanceAt"], Is.EqualTo(0d));
            Assert.That(PhotonNetwork.CurrentRoom.CustomProperties["LockOwner"], Is.EqualTo(masterActor), "A held lock is never taken over");
        }

        // The PUN client is a non-master player here and the independent client is the (passive) master.
        // The fixture keeps its objects inactive so no lifecycle callbacks run, so the production resend loop
        // is stepped here directly instead of being hosted by the flow object's StartCoroutine.
        [UnityTest]
        public IEnumerator NonMasterClientResendsTheInsulinRequestUntilTheRoomAcknowledgesIt()
        {
            yield return Connect(10);
            SetUpInsulin();
            int punActor = PhotonNetwork.LocalPlayer.ActorNumber;
            peer.CurrentRoom.SetCustomProperties(InsulinRoom(true, punActor, 0d));
            yield return Until(() => Equals(PhotonNetwork.CurrentRoom.CustomProperties["LockOwner"], punActor) &&
                PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("SGH_Insulin_Revision"), "the PUN client sees that it owns the attempt");

            int requests = 0;
            Action<ExitGames.Client.Photon.EventData> observer = e =>
            {
                if (e.Code == 200 && e.CustomData is Hashtable data && data.ContainsKey((byte)0) && Equals(data[(byte)0], 991)) requests++;
            };
            peer.EventReceived += observer;
            try
            {
                Call(flow, "RequestInsulinPart2Transition", 1);
                yield return Until(() => requests >= 1, "the first request reaches the master", 20);

                // The passive master never answers, so the loop must keep resending.
                var resend = (IEnumerator)Call(flow, "ResendInsulinAdvanceUntilAcknowledged", 1, 0);
                bool acknowledged = false;
                int steps = 0;
                while (resend.MoveNext())
                {
                    Assert.That(++steps, Is.LessThan(20), "The resend loop must stop on its own");
                    yield return Settle(2.1f); // one resend interval, servicing the peer
                    if (!acknowledged && requests >= 3)
                    {
                        // Acknowledge the way the master does: the snapshot gains a deadline.
                        peer.CurrentRoom.SetCustomProperties(InsulinRoom(true, punActor, PhotonNetwork.Time + 600d));
                        yield return Until(() => (double)PhotonNetwork.CurrentRoom.CustomProperties["SGH_Insulin_AdvanceAt"] > 0, "acknowledgement reaches the PUN client");
                        acknowledged = true;
                    }
                }
                Assert.That(acknowledged, Is.True, "The request was resent until the test acknowledged it");
                int atEnd = requests;
                yield return Settle(2.5f);
                Assert.That(requests, Is.EqualTo(atEnd), "No resend once the room acknowledges the request");
            }
            finally { peer.EventReceived -= observer; }
        }
    }
}
