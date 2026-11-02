using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Photon.Realtime;
using Photon.Chat;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.TestTools;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace SGH.Tests
{
    // Opt-in assembly: real Cloud traffic, separate synthetic identities and rooms.
    // This tests transport + production protocol handlers, not two rendered avatars.
    public class LiveTransportTests : GameTestFixture
    {
        private readonly List<LoadBalancingClient> peers = new List<LoadBalancingClient>();
        private readonly List<ChatClient> chats = new List<ChatClient>();
        private string appId;

        [SetUp]
        public void RequireDedicatedTestApplication()
        {
            appId = LiveTestConfig.Get("SGH_PHOTON_APP_ID");
            Assert.That(Guid.TryParse(appId, out _), Is.True, "Set a dedicated SGH_PHOTON_APP_ID; production fallback is forbidden");
        }

        [UnityTearDown]
        public IEnumerator DisconnectSyntheticClients()
        {
            foreach (var chat in chats) chat.Disconnect();
            foreach (var peer in peers) peer.Disconnect();
            float deadline = Time.realtimeSinceStartup + 3;
            while (Time.realtimeSinceStartup < deadline)
            {
                Pump(); yield return null;
                if (peers.TrueForAll(p => !p.IsConnected) && chats.TrueForAll(c => c.State == ChatState.Disconnected)) break;
            }
            peers.Clear(); chats.Clear();
        }

        private void Pump()
        {
            foreach (var p in peers) p.Service();
            foreach (var c in chats) c.Service();
        }

        private IEnumerator Until(Func<bool> predicate, string description, float timeout = 45)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline) { Pump(); yield return null; }
            Assert.That(predicate(), Is.True, description + " timed out");
        }

        private LoadBalancingClient Client()
        {
            var client = new LoadBalancingClient(ConnectionProtocol.Tcp);
            peers.Add(client);
            client.AuthValues = new Photon.Realtime.AuthenticationValues("sgh-ci-" + Guid.NewGuid().ToString("N"));
            Assert.That(client.ConnectUsingSettings(new AppSettings
            {
                AppIdRealtime = appId, AppVersion = "sgh-automation-v1",
                FixedRegion = LiveTestConfig.Get("SGH_PHOTON_REGION") ?? "sg",
                Protocol = ConnectionProtocol.Tcp, EnableProtocolFallback = false
            }), Is.True);
            return client;
        }

        private IEnumerator Room(LoadBalancingClient a, LoadBalancingClient b)
        {
            yield return Until(() => a.State == ClientState.ConnectedToMasterServer && b.State == ClientState.ConnectedToMasterServer, "Cloud connection");
            string room = "sgh-ci-" + Guid.NewGuid().ToString("N");
            Assert.That(a.OpCreateRoom(new EnterRoomParams
            {
                RoomName = room, RoomOptions = new RoomOptions
                { MaxPlayers = 4, PlayerTtl = 60000, EmptyRoomTtl = 2000, PublishUserId = true }
            }), Is.True);
            yield return Until(() => a.InRoom, "room creation");
            Assert.That(b.OpJoinRoom(new EnterRoomParams { RoomName = room }), Is.True);
            yield return Until(() => b.InRoom && a.CurrentRoom.PlayerCount == 2, "second client join");
            Assert.That(a.LocalPlayer.ActorNumber, Is.Not.EqualTo(b.LocalPlayer.ActorNumber));
        }

        [UnityTest]
        public IEnumerator FourClientSharedStateBurstConvergesAndFullRoomRejectsFifthClient()
        {
            var a = Client(); var b = Client(); yield return Room(a, b);
            var c = Client(); var d = Client(); var extra = Client();
            yield return Until(() => c.IsConnectedAndReady && d.IsConnectedAndReady && extra.IsConnectedAndReady, "additional clients connect");
            foreach (var player in new[] { c, d })
            {
                Assert.That(player.OpJoinRoom(new EnterRoomParams { RoomName = a.CurrentRoom.Name }), Is.True);
                yield return Until(() => player.InRoom, "additional client joins");
            }
            yield return Until(() => new[] { a, b, c, d }.All(p => p.CurrentRoom.PlayerCount == 4), "four player membership");
            bool full = false;
            extra.OpResponseReceived += response => { if (response.ReturnCode == Photon.Realtime.ErrorCode.GameFull) full = true; };
            Assert.That(extra.OpJoinRoom(new EnterRoomParams { RoomName = a.CurrentRoom.Name }), Is.True);
            yield return Until(() => full, "server rejects capacity overflow");
            foreach (var player in new[] { a, b, c, d })
                for (int i = 0; i < 25; i++) player.CurrentRoom.SetCustomProperties(new Hashtable { ["SGH_TEST_Burst_" + player.LocalPlayer.ActorNumber] = i });
            yield return Until(() => new[] { a, b, c, d }.All(p => new[] { a, b, c, d }.All(writer =>
                Equals(p.CurrentRoom.CustomProperties["SGH_TEST_Burst_" + writer.LocalPlayer.ActorNumber], 24))), "all final inputs converge");
        }

        [UnityTest]
        public IEnumerator ConcurrentCompareAndSwapHasOneWinnerAndReplicatesToBothClients()
        {
            var a = Client(); var b = Client(); yield return Room(a, b);
            const string key = "SGH_TEST_RaceWinner";
            a.CurrentRoom.SetCustomProperties(new Hashtable { [key] = 0 });
            yield return Until(() => b.CurrentRoom.CustomProperties.ContainsKey(key), "initial state");
            a.CurrentRoom.SetCustomProperties(new Hashtable { [key] = a.LocalPlayer.ActorNumber }, new Hashtable { [key] = 0 });
            b.CurrentRoom.SetCustomProperties(new Hashtable { [key] = b.LocalPlayer.ActorNumber }, new Hashtable { [key] = 0 });
            yield return Until(() => (int)a.CurrentRoom.CustomProperties[key] != 0 &&
                Equals(a.CurrentRoom.CustomProperties[key], b.CurrentRoom.CustomProperties[key]), "converged CAS winner");
            int winner = (int)a.CurrentRoom.CustomProperties[key];
            Assert.That(winner == a.LocalPlayer.ActorNumber || winner == b.LocalPlayer.ActorNumber, Is.True);
            a.CurrentRoom.SetCustomProperties(new Hashtable { [key] = -99 }, new Hashtable { [key] = 0 });
            yield return Until(() => Time.realtimeSinceStartup > 0, "pump");
            for (int i = 0; i < 30; i++) { Pump(); yield return null; }
            Assert.That(a.CurrentRoom.CustomProperties[key], Is.EqualTo(winner), "stale CAS must not overwrite committed state");
        }

        [UnityTest]
        public IEnumerator PendingInsulinSnapshotSurvivesMasterDisconnectAndSameActorRejoin()
        {
            var a = Client(); var b = Client(); yield return Room(a, b);
            int actor = a.LocalPlayer.ActorNumber;
            var snapshot = (Hashtable)Call(RuntimeType("InsulinRoomState"), "Snapshot", 7, 1, 1d);
            a.CurrentRoom.SetCustomProperties(snapshot);
            yield return Until(() => b.CurrentRoom.CustomProperties.ContainsKey("SGH_Insulin_Revision"), "pending snapshot delivery");
            a.Disconnect();
            yield return Until(() => !a.IsConnected && b.CurrentRoom.MasterClientId == b.LocalPlayer.ActorNumber, "master migration");
            Assert.That(b.CurrentRoom.GetPlayer(actor).IsInactive, Is.True);
            Assert.That(Call(RuntimeType("InsulinRoomState"), "IsDue", b.CurrentRoom.CustomProperties, 2d), Is.True);
            b.CurrentRoom.SetCustomProperties((Hashtable)Call(RuntimeType("InsulinRoomState"), "Snapshot", 7, 2, 0d), snapshot);
            yield return Until(() => (int)b.CurrentRoom.CustomProperties["SGH_Insulin_Part"] == 2, "replacement authority commit");
            Assert.That(a.ReconnectAndRejoin(), Is.True);
            yield return Until(() => a.InRoom, "same actor rejoin");
            Assert.That(a.LocalPlayer.ActorNumber, Is.EqualTo(actor));
            Assert.That(a.CurrentRoom.CustomProperties["SGH_Insulin_Part"], Is.EqualTo(2));
            Assert.That(a.CurrentRoom.CustomProperties["SGH_Insulin_Revision"], Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator ResetGenerationRejectsAnOldDeadlineOverRealTransport()
        {
            var a = Client(); var b = Client(); yield return Room(a, b);
            var old = (Hashtable)Call(RuntimeType("InsulinRoomState"), "Snapshot", 1, 1, 100d);
            a.CurrentRoom.SetCustomProperties(old);
            yield return Until(() => b.CurrentRoom.CustomProperties.ContainsKey("SGH_Insulin_Revision"), "pending page");
            a.CurrentRoom.SetCustomProperties((Hashtable)Call(RuntimeType("InsulinRoomState"), "Snapshot", 2, 1, 0d));
            yield return Until(() => (int)b.CurrentRoom.CustomProperties["SGH_Insulin_Revision"] == 2, "reset delivery");
            b.CurrentRoom.SetCustomProperties((Hashtable)Call(RuntimeType("InsulinRoomState"), "Snapshot", 1, 2, 0d), old);
            for (int i = 0; i < 30; i++) { Pump(); yield return null; }
            Assert.That(a.CurrentRoom.CustomProperties["SGH_Insulin_Revision"], Is.EqualTo(2));
            Assert.That(b.CurrentRoom.CustomProperties["SGH_Insulin_Part"], Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ActualEventSenderCannotSpoofAnalyticsActor()
        {
            var a = Client(); var b = Client(); yield return Room(a, b);
            var logger = Component("GameSessionCsvLogger"); int received = 0;
            b.EventReceived += message => { if (message.Code == 61) { Call(logger, "OnEvent", message); received++; } };
            object[] payload = { 1, "synthetic-room", b.LocalPlayer.ActorNumber, "Synthetic", 3, 1, 0d, "answer" };
            Assert.That(a.OpRaiseEvent(61, payload, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable), Is.True);
            yield return Until(() => received == 1, "spoofed event delivery");
            Assert.That(((IDictionary)Get(logger, "teamStatsByKey")).Count, Is.Zero);
            payload[2] = a.LocalPlayer.ActorNumber;
            a.OpRaiseEvent(61, payload, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
            yield return Until(() => received == 2, "valid event delivery");
            Assert.That(((IDictionary)Get(logger, "teamStatsByKey")).Count, Is.EqualTo(1));
        }

        private sealed class ChatListener : IChatClientListener
        {
            public readonly List<string> Messages = new List<string>();
            public bool Subscribed;
            public void DebugReturn(DebugLevel level, string message) { }
            public void OnDisconnected() { }
            public void OnConnected() { }
            public void OnChatStateChange(ChatState state) { }
            public void OnGetMessages(string channel, string[] senders, object[] messages) { foreach (var m in messages) Messages.Add(m.ToString()); }
            public void OnPrivateMessage(string sender, object message, string channel) { }
            public void OnSubscribed(string[] channels, bool[] results) { Subscribed = Array.TrueForAll(results, r => r); }
            public void OnUnsubscribed(string[] channels) { }
            public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
            public void OnUserSubscribed(string channel, string user) { }
            public void OnUserUnsubscribed(string channel, string user) { }
        }

        [UnityTest]
        public IEnumerator ChatDeliveryAndReconnectAreIndependentOfRealtimeRoom()
        {
            string chatId = LiveTestConfig.Get("SGH_PHOTON_CHAT_APP_ID");
            Assert.That(Guid.TryParse(chatId, out _), Is.True, "Set dedicated SGH_PHOTON_CHAT_APP_ID");
            var a = Client(); var b = Client(); yield return Room(a, b);
            var left = new ChatListener(); var right = new ChatListener();
            var ca = new ChatClient(left); var cb = new ChatClient(right); chats.Add(ca); chats.Add(cb);
            string channel = "sgh-ci-" + Guid.NewGuid().ToString("N");
            Assert.That(ca.Connect(chatId, "sgh-ci", new Photon.Chat.AuthenticationValues("SyntheticA-" + channel)), Is.True);
            Assert.That(cb.Connect(chatId, "sgh-ci", new Photon.Chat.AuthenticationValues("SyntheticB-" + channel)), Is.True);
            yield return Until(() => ca.CanChat && cb.CanChat, "Chat connection");
            ca.Subscribe(new[] { channel }); cb.Subscribe(new[] { channel });
            yield return Until(() => left.Subscribed && right.Subscribed, "Chat subscriptions");
            ca.PublishMessage(channel, "synthetic-before");
            yield return Until(() => right.Messages.Contains("synthetic-before"), "Chat message");
            cb.Disconnect(); yield return Until(() => cb.State == ChatState.Disconnected, "Chat disconnect");
            Assert.That(a.InRoom && b.InRoom, Is.True, "Chat loss must not disconnect PUN transport");
            right.Subscribed = false;
            Assert.That(cb.Connect(chatId, "sgh-ci", new Photon.Chat.AuthenticationValues("SyntheticB-" + channel)), Is.True);
            yield return Until(() => cb.CanChat, "Chat reconnect"); cb.Subscribe(new[] { channel });
            yield return Until(() => right.Subscribed, "Chat resubscribe");
            ca.PublishMessage(channel, "synthetic-after");
            yield return Until(() => right.Messages.Contains("synthetic-after"), "post reconnect message");
        }
    }
}
