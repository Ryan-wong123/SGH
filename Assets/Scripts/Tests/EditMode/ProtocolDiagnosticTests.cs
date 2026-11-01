using NUnit.Framework;

namespace SGH.Tests
{
    public class ProtocolDiagnosticTests : GameTestFixture
    {
        [Test, Category("KnownGap"), Explicit("Event 41 currently collides. This test should fail until every sender/receiver is migrated.")]
        public void AnalyticsAndCrosswordMustUseDifferentEventCodes()
        {
            Assert.That(Get(RuntimeType("GameSessionCsvLogger"), "GameplayLogEventCode"),
                Is.Not.EqualTo(Get(RuntimeType("CrosswordManager"), "CrosswordTextSyncEventCode")));
        }

        [Test, Category("KnownGap"), Explicit("Event 42 currently collides. This test should fail until every sender/receiver is migrated.")]
        public void ClueVotesAndWordSearchMustUseDifferentEventCodes()
        {
            Assert.That(Get(RuntimeType("ClueUI"), "ClueCloseRemainingEventCode"),
                Is.Not.EqualTo(Get(RuntimeType("WordSearchManager"), "WordSearchCellStateEventCode")));
        }
    }
}
