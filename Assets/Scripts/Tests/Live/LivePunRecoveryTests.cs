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
            previousLogger = Get(RuntimeType("GameSessionCsvLogger"), "Instance");
            Set(RuntimeType("GameSessionCsvLogger"), "<Instance>k__BackingField", null);
            Call(RuntimeType("SessionSmtpCredentials"), "Clear");
            version = PhotonNetwork.GameVersion;
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
            previousFlow = Get(RuntimeType("PuzzleStateManager"), "Instance");
            flow = Component("PuzzleStateManager");
            var view = flow.gameObject.AddComponent<PhotonView>(); view.ViewID = 991;
            Set(flow, "CurrentStage", Enum.ToObject(RuntimeType("PuzzleStateManager+PuzzleStage"), stage));
            Set(RuntimeType("PuzzleStateManager"), "Instance", flow);
            for (int i = 1; i <= 11; i++) Set(flow, "Puzzle" + i + "Panel", Object("Panel" + i));
            Call(flow, "InitStagePanels");
            PhotonNetwork.AddCallbackTarget(flow);
        }

        [UnityTearDown]
        public IEnumerator CleanupLiveRoom()
        {
            if (flow != null) PhotonNetwork.RemoveCallbackTarget(flow);
            // These fixtures stay inactive, so Unity never calls PhotonView.OnDestroy.
            // Release their explicitly registered IDs before the base fixture destroys them.
            foreach (var view in Root.GetComponentsInChildren<PhotonView>(true))
                PhotonNetwork.LocalCleanPhotonView(view);
            Set(RuntimeType("PuzzleStateManager"), "Instance", previousFlow);
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom(false);
            peer?.Disconnect(); PhotonNetwork.Disconnect();
            float due = Time.realtimeSinceStartup + 10;
            while (PhotonNetwork.IsConnected && Time.realtimeSinceStartup < due) { peer?.Service(); yield return null; }
            if (version != null) PhotonNetwork.GameVersion = version;
            Set(RuntimeType("GameSessionCsvLogger"), "<Instance>k__BackingField", previousLogger);
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
    }
}
