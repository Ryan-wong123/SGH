using System.Collections;
using NUnit.Framework;

namespace SGH.Tests
{
    [Category("RegressionFixes")]
    public class InsulinRoomStateTests : GameTestFixture
    {
        private object Snapshot(int revision, int part, double due)
        {
            return Call(RuntimeType("InsulinRoomState"), "Snapshot", revision, part, due);
        }

        [Test]
        public void OnlyReadyCurrentRevisionCanScheduleAndPendingCannotBeExtended()
        {
            var type = RuntimeType("InsulinRoomState");
            Assert.That(Call(type, "CanSchedule", Snapshot(3, 1, 0d), 3), Is.True);
            Assert.That(Call(type, "CanSchedule", Snapshot(3, 1, 0d), 2), Is.False);
            Assert.That(Call(type, "CanSchedule", Snapshot(3, 1, 102d), 3), Is.False);
            Assert.That(Call(type, "CanSchedule", Snapshot(3, 2, 0d), 3), Is.False);
        }

        [TestCase(101.99d, false)]
        [TestCase(102d, true)]
        [TestCase(200d, true)]
        public void AbsoluteDeadlineSurvivesReplacementAuthority(double now, bool expected)
        {
            Assert.That(Call(RuntimeType("InsulinRoomState"), "IsDue", Snapshot(4, 1, 102d), now), Is.EqualTo(expected));
        }

        [Test]
        public void ResetInvalidatesOldRequestAndPendingCompletion()
        {
            var type = RuntimeType("InsulinRoomState");
            object reset = Snapshot(5, 1, 0d);
            Assert.That(Call(type, "CanSchedule", reset, 4), Is.False);
            Assert.That(Call(type, "IsDue", reset, 200d), Is.False);
            Assert.That(Call(type, "CanSchedule", reset, 5), Is.True);
        }

        [TestCase(-1, 1, 0d)]
        [TestCase(0, 0, 0d)]
        [TestCase(0, 3, 0d)]
        [TestCase(0, 2, 102d)]
        [TestCase(0, 1, -1d)]
        [TestCase(0, 1, double.NaN)]
        [TestCase(0, 1, double.PositiveInfinity)]
        public void MalformedSnapshotsCannotDriveTransitions(int revision, int part, double due)
        {
            Assert.That(Call(RuntimeType("InsulinRoomState"), "TryRead", new object[] { Snapshot(revision, part, due), null, null, null }), Is.False);
        }

        [Test]
        public void CompletedPartIsRestorableAndHasNoPendingDeadline()
        {
            var type = RuntimeType("InsulinRoomState");
            object[] args = { Snapshot(8, 2, 0d), null, null, null };
            Assert.That(Call(type, "TryRead", args), Is.True);
            Assert.That(args[1], Is.EqualTo(8)); Assert.That(args[2], Is.EqualTo(2)); Assert.That(args[3], Is.EqualTo(0d));
        }
    }
}
