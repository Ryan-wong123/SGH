using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SGH.Tests
{
    [Category("RegressionFixes")]
    public class PhotonProtocolRegressionTests : GameTestFixture
    {
        private static Type PhotonType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
        }

        private static object Message(byte code, object payload)
        {
            Type type = PhotonType("ExitGames.Client.Photon.EventData");
            object message = Activator.CreateInstance(type);
            Set(message, "Code", code);
            // Photon ParameterCode.Data=245. Supplying the actual wire envelope
            // exercises receivers rather than only testing a helper validator.
            object parameters = Activator.CreateInstance(type.GetField("Parameters", Members).FieldType);
            parameters.GetType().GetProperty("Item").SetValue(parameters, payload, new object[] { (byte)245 });
            Set(message, "Parameters", parameters);
            return message;
        }

        [Test]
        public void AllApplicationEventCodesAreUniqueAndRetiredCodesAreUnused()
        {
            byte[] codes = new[] { "Analytics", "ClueCloseRemaining", "CrosswordText", "CrosswordResult", "WordSearchState" }
                .Select(name => (byte)Get(RuntimeType("SghPhotonEvents"), name)).ToArray();
            Assert.That(codes.Distinct().Count(), Is.EqualTo(5));
            Assert.That(codes.All(code => code > 43 && code < 200), Is.True);
        }

        [TestCase("GameSessionCsvLogger", "GameplayLogEventCode")]
        [TestCase("ClueUI", "ClueCloseRemainingEventCode")]
        [TestCase("CrosswordManager", "CrosswordTextSyncEventCode")]
        [TestCase("CrosswordManager", "CrosswordResultSyncEventCode")]
        [TestCase("WordSearchManager", "WordSearchCellStateEventCode")]
        public void ReceiversIgnoreMalformedAndOtherProtocolPayloads(string type, string codeField)
        {
            object receiver = Component(type);
            byte code = (byte)Get(RuntimeType(type), codeField);
            object[] analytics = { 1, "synthetic", 1, "Synthetic A", 3, 1, 2d, "answer" };
            object[] clue = { "ClueClose_synthetic", 1, 1 };
            object[] crossword = { 0, 0, "a", 1, 1 };
            object[] cells = { 1, 1, 3 };
            foreach (object payload in new object[] { null, "wrong", new object[0], new object[] { null, null, null },
                new object[] { "x", "y", "state" }, analytics, clue, crossword, cells,
                new object[] { 0, 0, "too long", 1, 1 },
                new object[] { 1, "synthetic", 1, "Synthetic A", 3, 1, double.NaN, "answer" } })
                Assert.DoesNotThrow(() => Call(receiver, "OnEvent", Message(code, payload)));
            foreach (byte retired in new byte[] { 41, 42, 43 })
                Assert.DoesNotThrow(() => Call(receiver, "OnEvent", Message(retired, analytics)));
            Assert.DoesNotThrow(() => Call(receiver, "OnEvent", new object[] { null }));
        }

        [Test]
        public void AnalyticsAcceptsOnlyItsTypedFiniteEightFieldPayload()
        {
            object logger = Component("GameSessionCsvLogger");
            byte code = (byte)Get(RuntimeType("SghPhotonEvents"), "Analytics");
            object[] payload = { 1, "synthetic", 1, "Synthetic A", 3, 1, 2d, "answer" };
            Call(logger, "OnEvent", Message(code, payload));
            Assert.That(((IDictionary)Get(logger, "teamStatsByKey")).Count, Is.EqualTo(1));
            payload[6] = double.PositiveInfinity;
            Call(logger, "OnEvent", Message(code, payload));
            foreach (object row in ((IDictionary)Get(logger, "teamStatsByKey")).Values)
                Assert.That(Get(row, "CorrectAnswers"), Is.EqualTo(1));
        }
    }
}
