using System;
using System.Collections;
using NUnit.Framework;

namespace SGH.Tests
{
    public class AdversarialProtocolTests : GameTestFixture
    {
        [TestCase("GameSessionCsvLogger", "GameplayLogEventCode")]
        [TestCase("CrosswordManager", "CrosswordTextSyncEventCode")]
        [TestCase("CrosswordManager", "CrosswordResultSyncEventCode")]
        [TestCase("WordSearchManager", "WordSearchCellStateEventCode")]
        [TestCase("ClueUI", "ClueCloseRemainingEventCode")]
        public void SeededMalformedWirePayloadsNeverThrow(string type, string codeField)
        {
            var receiver = Component(type); byte code = (byte)Get(RuntimeType(type), codeField);
            var random = new Random(73491);
            object[] values = { null, -1, int.MinValue, int.MaxValue, "synthetic-malformed", double.NaN, double.PositiveInfinity, 0L, new object[0], false };
            for (int sample = 0; sample < 1000; sample++)
            {
                var payload = new object[random.Next(0, 12)];
                for (int i = 0; i < payload.Length; i++) payload[i] = values[random.Next(values.Length)];
                object message = Call(typeof(PhotonProtocolRegressionTests), "Message", code, payload);
                Assert.DoesNotThrow(() => Call(receiver, "OnEvent", message), type + " sample " + sample);
            }
        }

        [Test]
        public void ForgedAnalyticsActorIsRejectedBeforeAnyStateMutation()
        {
            var logger = Component("GameSessionCsvLogger");
            object[] payload = { 1, "synthetic-room", 22, "Synthetic claimed actor", 3, 1, 0d, "answer" };
            object message = Call(typeof(PhotonProtocolRegressionTests), "Message", (byte)61, payload);
            object parameters = Get(message, "Parameters");
            parameters.GetType().GetProperty("Item").SetValue(parameters, 11, new object[] { (byte)254 });
            Call(logger, "OnEvent", message);
            Assert.That(((IDictionary)Get(logger, "teamStatsByKey")).Count, Is.Zero);
            Assert.That(((IDictionary)Get(logger, "statsByKey")).Count, Is.Zero);
        }
    }
}
