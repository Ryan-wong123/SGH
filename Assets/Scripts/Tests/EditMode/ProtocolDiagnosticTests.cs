using NUnit.Framework;

namespace SGH.Tests
{
    [Category("RegressionFixes")]
    public class ProtocolDiagnosticTests : GameTestFixture
    {
        [Test]
        public void AnalyticsAndCrosswordMustUseDifferentEventCodes()
        {
            Assert.That(Get(RuntimeType("GameSessionCsvLogger"), "GameplayLogEventCode"),
                Is.Not.EqualTo(Get(RuntimeType("CrosswordManager"), "CrosswordTextSyncEventCode")));
        }

        [Test]
        public void ClueVotesAndWordSearchMustUseDifferentEventCodes()
        {
            Assert.That(Get(RuntimeType("ClueUI"), "ClueCloseRemainingEventCode"),
                Is.Not.EqualTo(Get(RuntimeType("WordSearchManager"), "WordSearchCellStateEventCode")));
        }
    }
}
