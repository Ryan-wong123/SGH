using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SGH.Tests
{
    // Real Photon offline room/property APIs, without cloud traffic or email.
    // This verifies flow integration; it is not a two-process transport test.
    [Category("RegressionFixes")]
    public class InsulinRecoveryTests : GameTestFixture
    {
        private Type network;
        private object room;
        private Component flow;
        private Component insulin;
        private object savedFlow;
        private object savedLogger;
        private bool savedOffline;

        private object Snapshot(int revision, int part, double due)
        {
            return Call(RuntimeType("InsulinRoomState"), "Snapshot", revision, part, due);
        }

        private void Publish(object snapshot)
        {
            Call(room, "SetCustomProperties", snapshot, null, null);
        }

        private IEnumerator CreateOfflineRoom()
        {
            network = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Photon.Pun.PhotonNetwork")).First(t => t != null);
            Assert.That(Get(network, "IsConnected"), Is.False);
            savedFlow = Get(RuntimeType("PuzzleStateManager"), "Instance");
            savedLogger = Get(RuntimeType("GameSessionCsvLogger"), "Instance");
            Set(RuntimeType("GameSessionCsvLogger"), "<Instance>k__BackingField", null);
            savedOffline = (bool)Get(network, "OfflineMode");
            network.GetProperty("OfflineMode").SetValue(null, true);
            Assert.That(Call(network, "CreateRoom", "synthetic-insulin", null, null, null), Is.True);
            yield return null;
            room = Get(network, "CurrentRoom");
            insulin = Component("InsulinPuzzleManager");
            Set(insulin, "syringeDropdown", Dropdown(1));
            Set(insulin, "panel1", Object("Part1"));
            GameObject part2 = Object("Part2"); part2.SetActive(false); Set(insulin, "panel2", part2);
            flow = Component("PuzzleStateManager");
            Set(flow, "CurrentStage", Enum.ToObject(RuntimeType("PuzzleStateManager+PuzzleStage"), 10));
            Set(flow, "insulinPuzzleManager", insulin);
            Set(RuntimeType("PuzzleStateManager"), "Instance", flow);
            Call(flow, "EnsureRoomProps");
            IDictionary properties = (IDictionary)Get(room, "CustomProperties");
            properties["PuzzleLocked"] = true;
            properties["LockOwner"] = Get(Get(network, "LocalPlayer"), "ActorNumber");
        }

        [UnityTearDown]
        public IEnumerator LeaveOfflineRoom()
        {
            Set(RuntimeType("PuzzleStateManager"), "Instance", savedFlow);
            if ((bool)Get(network, "InRoom")) Call(network, "LeaveRoom", false);
            yield return null;
            network.GetProperty("OfflineMode").SetValue(null, savedOffline);
            Set(RuntimeType("GameSessionCsvLogger"), "<Instance>k__BackingField", savedLogger);
        }

        [UnityTest]
        public IEnumerator CorrectSubmissionCreatesDeadlineAndDuplicateDoesNotExtendIt()
        {
            yield return CreateOfflineRoom();
            Call(insulin, "SubmitSyringeAnswer");
            object properties = Get(room, "CustomProperties");
            double due = (double)((IDictionary)properties)["SGH_Insulin_AdvanceAt"];
            Assert.That(due, Is.GreaterThan((double)Get(network, "Time")));
            Call(insulin, "SubmitSyringeAnswer");
            Assert.That(((IDictionary)Get(room, "CustomProperties"))["SGH_Insulin_AdvanceAt"], Is.EqualTo(due));
            Assert.That(Get(insulin, "CurrentPart"), Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResetRejectsOldSubmissionAndExpiredDeadlineCannotReopenPage()
        {
            yield return CreateOfflineRoom();
            Call(insulin, "SubmitSyringeAnswer");
            Call(insulin, "ResetPuzzle");
            int actor = (int)Get(Get(network, "LocalPlayer"), "ActorNumber");
            Call(flow, "ScheduleInsulinAdvance", 1, 0, actor);
            Call(flow, "ProcessPendingInsulinAdvance");
            Call(flow, "RefreshInsulinRoomState");
            IDictionary properties = (IDictionary)Get(room, "CustomProperties");
            Assert.That(properties["SGH_Insulin_Revision"], Is.EqualTo(1));
            Assert.That(properties["SGH_Insulin_AdvanceAt"], Is.EqualTo(0d));
            Assert.That(Get(insulin, "CurrentPart"), Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PendingSnapshotCompletesAfterLocalRecoveryAndPartTwoReapplies()
        {
            yield return CreateOfflineRoom();
            while ((double)Get(network, "Time") <= 0.01d) yield return null;
            double now = (double)Get(network, "Time");
            Publish(Snapshot(4, 1, now / 2d));
            Call(flow, "ClearInsulinLocalState"); // Simulate loss of local cached transition state.
            Call(flow, "ProcessPendingInsulinAdvance");
            Call(flow, "RefreshInsulinRoomState");
            Assert.That(Get(insulin, "CurrentPart"), Is.EqualTo(2));
            Call(insulin, "ApplyRoomPart", 1, false); // Simulate freshly restored UI.
            Call(flow, "ClearInsulinLocalState");
            Call(flow, "RefreshInsulinRoomState");
            Assert.That(Get(insulin, "CurrentPart"), Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator WrongOwnerAndWrongSelectionCannotSchedule()
        {
            yield return CreateOfflineRoom();
            int actor = (int)Get(Get(network, "LocalPlayer"), "ActorNumber");
            Call(flow, "ScheduleInsulinAdvance", 0, 0, actor);
            Call(flow, "ScheduleInsulinAdvance", 1, 0, actor + 100);
            Assert.That(((IDictionary)Get(room, "CustomProperties"))["SGH_Insulin_AdvanceAt"], Is.EqualTo(0d));
            yield return null;
        }
    }
}
