using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace SGH.Tests
{
    public class SessionAndAnalyticsTests : GameTestFixture
    {
        [SetUp]
        public void ClearSyntheticCredentials() { Call(RuntimeType("SessionSmtpCredentials"), "Clear"); }

        [TearDown]
        public void RemoveSyntheticCredentials() { Call(RuntimeType("SessionSmtpCredentials"), "Clear"); }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("   ", false)]
        [TestCase("not-an-email", false)]
        [TestCase("Test Person <sender@example.invalid>", false)]
        [TestCase(" sender@example.invalid ", true)]
        public void EmailValidationRejectsInvalidAndDisplayNameForms(string value, bool expected)
        {
            foreach (string method in new[] { "TrySetSenderEmail", "TrySetRecipientEmail" })
            {
                object[] args = { value, null };
                Assert.That(Call(RuntimeType("SessionSmtpCredentials"), method, args), Is.EqualTo(expected));
                Assert.That(args[1] == null, Is.EqualTo(expected));
            }
        }

        [Test]
        public void CredentialsRequireAllThreeValuesNormalizePasswordAndClear()
        {
            Type type = RuntimeType("SessionSmtpCredentials");
            Assert.That(Call(type, "TryGetCredentials", new object[] { null, null, null }), Is.False);
            Call(type, "TrySetSenderEmail", new object[] { " sender@example.invalid ", null });
            Call(type, "TrySetAppPassword", new object[] { " synthetic test password ", null });
            Assert.That(Call(type, "TryGetCredentials", new object[] { null, null, null }), Is.False);
            Call(type, "TrySetRecipientEmail", new object[] { "recipient@example.invalid", null });
            object[] result = { null, null, null };
            Assert.That(Call(type, "TryGetCredentials", result), Is.True);
            Assert.That(result, Is.EqualTo(new[] { "sender@example.invalid", "synthetictestpassword", "recipient@example.invalid" }));
            Call(type, "Clear");
            Assert.That(Call(type, "TryGetCredentials", result), Is.False);
            Assert.That(result, Is.All.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void EmptyAppPasswordIsRejected(string password)
        {
            Assert.That(Call(RuntimeType("SessionSmtpCredentials"), "TrySetAppPassword", new object[] { password, null }), Is.False);
        }

        [TestCase("https://example.invalid/api/gameplay-csv", true, true)]
        [TestCase("http://localhost:5177/api/gameplay-csv", true, true)]
        [TestCase("http://127.0.0.1:5177/api/gameplay-csv", true, true)]
        [TestCase("http://example.invalid/api/gameplay-csv", true, false)]
        [TestCase("ftp://example.invalid/data", true, false)]
        [TestCase("relative/path", true, false)]
        [TestCase("", true, false)]
        [TestCase("https://example.invalid/api/gameplay-csv", false, false)]
        public void BackendSettingsRequireHttpsExceptEditorLoopback(string url, bool enabled, bool expected)
        {
            var settings = ScriptableObject.CreateInstance(RuntimeType("SmtpEmailSettings"));
            try
            {
                Set(settings, "emailApiUrl", url); Set(settings, "enableSending", enabled);
                object[] args = { null };
                Assert.That(Call(settings, "IsConfigured", args), Is.EqualTo(expected));
                Assert.That(args[0] == null, Is.EqualTo(expected));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [TestCase(1, 0, false)]
        [TestCase(3, 0, false)]
        [TestCase(4, 1, false)]
        [TestCase(4, 2, true)]
        [TestCase(5, 0, true)]
        [TestCase(6, 1, true)]
        [TestCase(6, 2, true)]
        [TestCase(6, 3, true)]
        [TestCase(7, 0, false)]
        [TestCase(8, 0, true)]
        [TestCase(9, 0, true)]
        [TestCase(10, 1, false)]
        [TestCase(10, 2, false)]
        [TestCase(11, 1, false)]
        [TestCase(11, 2, false)]
        public void IndividualAnalyticsExclusionsArePreserved(int stage, int part, bool suppressed)
        {
            Assert.That(Call(RuntimeType("GameSessionCsvLogger"), "ShouldSuppressIndividualSubmissionInputs", stage, part), Is.EqualTo(suppressed));
        }

        [TestCase(null, "")]
        [TestCase("plain", "plain")]
        [TestCase("a,b", "\"a,b\"")]
        [TestCase("a\"b", "\"a\"\"b\"")]
        [TestCase("a\nb", "\"a\nb\"")]
        [TestCase("a\rb", "\"a\rb\"")]
        public void CsvEscapesSyntheticCells(string value, string expected)
        {
            var builder = new StringBuilder();
            Call(RuntimeType("CsvStringBuilderExtensions"), "AppendCsv", builder, value);
            Assert.That(builder.ToString(), Is.EqualTo(expected));
        }

        [TestCase(-10d, "'00:00")]
        [TestCase(0d, "'00:00")]
        [TestCase(59d, "'00:59")]
        [TestCase(60d, "'01:00")]
        [TestCase(900d, "'15:00")]
        [TestCase(3600d, "'60:00")]
        public void CsvDurationIsNonnegativeSpreadsheetText(double seconds, string expected)
        {
            Assert.That(Call(RuntimeType("GameSessionCsvLogger"), "FormatMinutesAndSeconds", seconds), Is.EqualTo(expected));
        }

        [TestCase(4)] [TestCase(6)] [TestCase(10)] [TestCase(11)]
        public void AnalyticsDropsUntaggedMultipartEvents(int stage)
        {
            var logger = Component("GameSessionCsvLogger");
            Call(logger, "ApplyEvent", 1, "synthetic-room", 1, "Synthetic A", stage, false, 1d, "wrong");
            Assert.That(((IDictionary)Get(logger, "teamStatsByKey")).Count, Is.Zero);
            Assert.That(((IDictionary)Get(logger, "statsByKey")).Count, Is.Zero);
        }

        [Test]
        public void AnalyticsExcludesPaletteAndKeepsFirstSuccessfulSnapshot()
        {
            var logger = Component("GameSessionCsvLogger");
            Call(logger, "ApplyEvent", 1, "synthetic-room", 1, "Synthetic A", 2, true, 1d, "ignored");
            Assert.That(((IDictionary)Get(logger, "teamStatsByKey")).Count, Is.Zero);
            foreach (string snapshot in new[] { "28", "changed" })
                Call(logger, "ApplyEvent", 1, "synthetic-room", 1, "Synthetic A", 8, true, 1d, snapshot);
            IDictionary rows = (IDictionary)Get(logger, "teamStatsByKey");
            Assert.That(rows.Count, Is.EqualTo(1));
            foreach (object row in rows.Values)
            {
                Assert.That(Get(row, "CorrectAnswers"), Is.EqualTo(2));
                Assert.That(Get(row, "ExpectedCorrectAnswers"), Is.EqualTo("28"));
            }
        }

        [Test]
        public void AnalyticsSeparatesSubmittersAndPartsAndClampsNegativeTime()
        {
            var logger = Component("GameSessionCsvLogger");
            foreach (int actor in new[] { 1, 2 })
                foreach (int part in new[] { 1, 2 })
                    Call(logger, "ApplyEvent", 1, "synthetic-room", actor, "Synthetic " + actor, 10, false, -2d,
                        "__PUZZLE_PART=" + part + "__|wrong");
            IDictionary rows = (IDictionary)Get(logger, "teamStatsByKey");
            Assert.That(rows.Count, Is.EqualTo(4));
            foreach (object row in rows.Values)
            {
                Assert.That(Get(row, "WrongAnswers"), Is.EqualTo(1));
                Assert.That(Get(row, "TimeTakenSeconds"), Is.EqualTo(0d));
            }
        }

        [Test]
        public void CreatedRoomsKeepRejoinLifetimeAndLobbyMetadataWithoutCredentials()
        {
            object options = Call(RuntimeType("CreateRoomManager"), "BuildRoomOptions", (byte)4, "ABC234");
            Assert.That(Get(options, "MaxPlayers"), Is.EqualTo(4));
            Assert.That(Get(options, "PlayerTtl"), Is.EqualTo(600000));
            Assert.That(Get(options, "EmptyRoomTtl"), Is.EqualTo(300000));
            Assert.That(Get(options, "PublishUserId"), Is.True);
            var properties = (IDictionary)Get(options, "CustomRoomProperties");
            Assert.That(properties.Count, Is.EqualTo(2));
            Assert.That(properties["RoomPassword"], Is.EqualTo("ABC234"));
            Assert.That(properties["GameStarted"], Is.False);
        }

        [Test]
        public void GeneratedRoomPasswordsUseConfiguredAlphabetAndLength()
        {
            UnityEngine.Random.State saved = UnityEngine.Random.state;
            try
            {
                Type type = RuntimeType("CreateRoomManager");
                string alphabet = (string)Get(type, "PasswordCharacters");
                for (int i = 0; i < 100; i++)
                {
                    string password = (string)Call(type, "GenerateRoomPassword");
                    Assert.That(password.Length, Is.EqualTo(6));
                    foreach (char c in password) Assert.That(alphabet.Contains(c.ToString()), Is.True);
                }
            }
            finally { UnityEngine.Random.state = saved; }
        }
    }
}
