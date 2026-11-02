using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SGH.Tests
{
    public class AnalyticsLifecycleTests : GameTestFixture
    {
        private string directory;
        private object logger;

        [SetUp]
        public void SyntheticExportOnly()
        {
            var network = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Photon.Pun.PhotonNetwork")).First(t => t != null);
            Assert.That(Get(network, "InRoom"), Is.False, "CSV fixtures must run outside gameplay rooms so direct SMTP is unreachable");
            directory = Path.GetFullPath("TestResults/synthetic-csv/" + Guid.NewGuid().ToString("N"));
            logger = Component("GameSessionCsvLogger");
            Set(logger, "csvDirectory", (Func<string>)(() => directory));
            Set(logger, "sessionPlayedAt", new DateTime(2026, 11, 2));
        }

        [TearDown]
        public void RemoveOwnedSyntheticFiles()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        private void Answer(int stage, int part, bool correct, string answer, int actor = 1)
        {
            Call(logger, "ApplyEvent", 1, "synthetic-room", actor, "Synthetic,\"Player\"", stage,
                correct, 5d, "__PUZZLE_PART=" + part + "__|" + answer);
        }

        [Test]
        public void RealCsvExportHasParseableSectionsCountsEscapingAndExclusions()
        {
            Answer(3, 0, false, "wrong,\"quoted\"\nline"); Answer(3, 0, true, "accepted");
            Answer(4, 2, true, "15|4", 2); Answer(2, 0, true, "excluded");
            Call(logger, "WriteCsvIfNeeded");
            string text = File.ReadAllText(Directory.GetFiles(directory, "*.csv").Single());
            var records = Parse(text);
            int team = records.FindIndex(r => r.SequenceEqual(new[] { "TEAM_SUBMISSIONS" }));
            Assert.That(records[0], Is.EqualTo(new[] { "INDIVIDUAL_PLAYER_INPUTS" }));
            Assert.That(team, Is.GreaterThan(2));
            var teamRows = records.Skip(team + 2).Where(r => r.Length > 1).ToArray();
            Assert.That(teamRows.Length, Is.EqualTo(2));
            Assert.That(teamRows.Any(r => r.Contains("Synthetic,\"Player\"")), Is.True);
            Assert.That(teamRows.Any(r => r.Contains("2") && r.Contains("1")), Is.True);
            Assert.That(records.Take(team).Count(r => r.Length > 3 && r[3] == "4"), Is.Zero,
                "Puzzle 4 part 2 must not create an individual row");
            Assert.That(teamRows.Any(r => r.Length > 3 && r[3] == "2"), Is.False);
        }

        [Test]
        public void FailedWriteCanRetryAndSuccessfulExportIsIdempotent()
        {
            Answer(3, 0, true, "synthetic");
            Set(logger, "csvWriter", (Action<string, string>)((p, c) => throw new IOException("Synthetic disk failure")));
            Assert.Throws<IOException>(() => Call(logger, "WriteCsvIfNeeded"));
            Assert.That(Get(logger, "hasWrittenCsv"), Is.False);
            int writes = 0;
            Set(logger, "csvWriter", (Action<string, string>)((p, c) => { writes++; File.WriteAllText(p, c); }));
            Call(logger, "WriteCsvIfNeeded"); Call(logger, "WriteCsvIfNeeded");
            Assert.That(writes, Is.EqualTo(1)); Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(1));
        }

        [Test]
        public void EarlyLeaveFlushesSyntheticRowsWithoutMailCredentials()
        {
            Answer(3, 0, false, "synthetic early leave");
            Call(logger, "OnLeftRoom");
            Assert.That(Directory.GetFiles(directory, "*.csv").Length, Is.EqualTo(1));
        }

        [Test]
        public void SecondRoomMustNotRetainFirstRoomAnalytics()
        {
            Answer(3, 0, true, "first-session-only");
            Call(logger, "WriteCsvIfNeeded");
            ((IDictionary)Get(logger, "localAttemptStartTimes"))["old-attempt"] = 5d;
            Call(logger, "OnJoinedRoom");
            Assert.That(((IDictionary)Get(logger, "teamStatsByKey")).Count, Is.Zero);
            Assert.That(((IDictionary)Get(logger, "statsByKey")).Count, Is.Zero);
            Assert.That(((IDictionary)Get(logger, "localAttemptStartTimes")).Count, Is.Zero);
            Assert.That(Get(logger, "hasWrittenCsv"), Is.False);
            Answer(7, 0, true, "second-session-only"); Call(logger, "WriteCsvIfNeeded");
            var exports = Directory.GetFiles(directory, "*.csv");
            Assert.That(exports.Length, Is.EqualTo(2), "Rapid sessions must not overwrite each other's CSV");
            Assert.That(exports.Count(p => File.ReadAllText(p).Contains("first-session-only")), Is.EqualTo(1));
            Assert.That(exports.Count(p => File.ReadAllText(p).Contains("second-session-only")), Is.EqualTo(1));
        }

        [Test]
        public void SameActorRejoinPreservesCurrentSessionRowsAndExportIdentity()
        {
            var network = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Photon.Pun.PhotonNetwork")).First(t => t != null);
            var player = Get(network, "LocalPlayer");
            var rejoined = player.GetType().GetProperty("HasRejoined");
            object previous = rejoined.GetValue(player);
            Answer(3, 0, true, "synthetic-resume"); string id = (string)Get(logger, "sessionExportId");
            try
            {
                rejoined.SetValue(player, true); Call(logger, "OnJoinedRoom");
                Assert.That(((IDictionary)Get(logger, "teamStatsByKey")).Count, Is.EqualTo(1));
                Assert.That(Get(logger, "sessionExportId"), Is.EqualTo(id));
            }
            finally { rejoined.SetValue(player, previous); }
        }

        // Independent CSV parser, including quoted commas, escaped quotes and embedded newlines.
        private static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>(); var fields = new List<string>(); var cell = new StringBuilder(); bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"') { if (quoted && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else quoted = !quoted; }
                else if (c == ',' && !quoted) { fields.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n' && !quoted) { fields.Add(cell.ToString().TrimEnd('\r')); rows.Add(fields.ToArray()); fields.Clear(); cell.Clear(); }
                else cell.Append(c);
            }
            Assert.That(quoted, Is.False, "Unterminated CSV quote"); return rows;
        }
    }
}
