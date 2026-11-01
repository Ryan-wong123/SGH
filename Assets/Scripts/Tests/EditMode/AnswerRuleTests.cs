using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SGH.Tests
{
    public class AnswerRuleTests : GameTestFixture
    {
        [TestCase("32", "32", false, true)]
        [TestCase(" 32 ", "32", false, true)]
        [TestCase("32.0", "32", false, true)]
        [TestCase("3.2e1", "32", false, true)]
        [TestCase("32.00001", "32", false, true)]
        [TestCase("32.01", "32", false, false)]
        [TestCase("", "32", false, false)]
        [TestCase(null, "32", false, false)]
        [TestCase("abc", "ABC", false, true)]
        [TestCase("abc", "ABC", true, false)]
        [TestCase("32,0", "32", false, false)]
        public void DiaryUsesDocumentedComparison(string value, string expected, bool sensitive, bool matches)
        {
            Assert.That(Call(RuntimeType("DiaryPuzzleManager"), "ValuesMatch", value, expected, sensitive, 0.0001f), Is.EqualTo(matches));
        }

        [TestCase("15", "4", true)]
        [TestCase(" 15", "4", false)]
        [TestCase("15", "4 ", false)]
        [TestCase("15.0", "4", false)]
        [TestCase("", "4", false)]
        [TestCase("15", "", false)]
        [TestCase("4", "15", false)]
        public void SurvivalCodeRequiresExactStrings(string a, string b, bool expected)
        {
            var manager = Component("Activity2PuzzleManager");
            Set(manager, "input1", Input(a));
            Set(manager, "input2", Input(b));
            Assert.That(Call(manager, "CheckActivity2Part2Correct"), Is.EqualTo(expected));
            Call(manager, "ResetActivity2Part2");
            Assert.That(((TMP_InputField)Get(manager, "input1")).text, Is.Empty);
            Assert.That(((TMP_InputField)Get(manager, "input2")).text, Is.Empty);
        }

        [TestCase("28", true)]
        [TestCase(" 28 ", true)]
        [TestCase("28.0", false)]
        [TestCase("028", false)]
        [TestCase("", false)]
        [TestCase("27", false)]
        public void PrescriptionTrimsButDoesNotCoerceNumbers(string answer, bool expected)
        {
            var manager = Component("PrescriptionManager");
            TMP_InputField input = Input(answer);
            Set(manager, "prescriptionAmount", input);
            Assert.That(Call(manager, "IsSolved"), Is.EqualTo(expected));
            Assert.That(input.image.color, Is.EqualTo(expected ? Color.green : Color.red));
            Call(manager, "ResetPuzzle");
            Assert.That(input.text, Is.Empty);
            Assert.That(input.image.color, Is.EqualTo(Color.white));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void RiskTableRequiresEveryConfiguredAnswer(int wrongIndex)
        {
            var manager = Component("RiskTable");
            IList entries = NestedList("RiskTable", "DropdownAnswer");
            int[] expected = { 1, 2, 1, 1, 1, 1 };
            for (int i = 0; i < expected.Length; i++)
            {
                object entry = Nested("RiskTable", "DropdownAnswer");
                Set(entry, "dropdown", Dropdown(i == wrongIndex ? 0 : expected[i]));
                Set(entry, "correctIndex", expected[i]);
                entries.Add(entry);
            }
            Set(manager, "dropdowns", entries);
            Assert.That(Call(manager, "IsSolved"), Is.EqualTo(wrongIndex < 0));
            Call(manager, "ResetPuzzle");
            foreach (object entry in entries)
            {
                var dropdown = (TMP_Dropdown)Get(entry, "dropdown");
                Assert.That(dropdown.value, Is.Zero);
                Assert.That(dropdown.targetGraphic.color, Is.EqualTo(Color.white));
            }
        }

        [Test]
        public void DiaryAdvancesOnlyThroughExplicitPageChangeAndResetsAllPages()
        {
            var manager = Component("DiaryPuzzleManager");
            GameObject[] panels = { Object("Page1"), Object("Page2"), Object("Page3") };
            Set(manager, "stagePanels", panels);
            IList answers = NestedList("DiaryPuzzleManager", "AnswerItem");
            string[][] values = { new[] { "32" }, new[] { "24", "8", "8", "8" }, new[] { "40", "20", "40" } };
            for (int page = 0; page < values.Length; page++)
                foreach (string value in values[page])
                {
                    object entry = Nested("DiaryPuzzleManager", "AnswerItem");
                    Set(entry, "field", Input(value));
                    Set(entry, "stage", page + 1);
                    Set(entry, "accepted", new[] { value });
                    answers.Add(entry);
                }
            Set(manager, "answers", answers);
            for (int page = 1; page <= 3; page++)
            {
                Call(manager, "GoToStage", page);
                for (int i = 0; i < panels.Length; i++) Assert.That(panels[i].activeSelf, Is.EqualTo(i == page - 1));
                Assert.That(Call(manager, "SubmitDiaryStage").ToString(), Is.EqualTo(page == 3 ? "Completed" : "Advanced"));
                Assert.That(Get(manager, "CurrentStage"), Is.EqualTo(page));
            }
            Call(manager, "ResetPuzzle");
            Assert.That(Get(manager, "CurrentStage"), Is.EqualTo(1));
            foreach (object entry in answers) Assert.That(((TMP_InputField)Get(entry, "field")).text, Is.Empty);
        }

        [Test]
        public void DiaryWrongAnswerDoesNotAdvanceAndSnapshotOmitsCorrectAnswers()
        {
            var manager = Component("DiaryPuzzleManager");
            Set(manager, "stagePanels", new[] { Object("Page1") });
            IList answers = NestedList("DiaryPuzzleManager", "AnswerItem");
            foreach (string value in new[] { "32", "99" })
            {
                object entry = Nested("DiaryPuzzleManager", "AnswerItem");
                Set(entry, "field", Input(value)); Set(entry, "stage", 1); Set(entry, "accepted", new[] { "32" });
                answers.Add(entry);
            }
            Set(manager, "answers", answers);
            Assert.That(Call(manager, "SubmitDiaryStage").ToString(), Is.EqualTo("Incorrect"));
            Assert.That(Get(manager, "CurrentStage"), Is.EqualTo(1));
            Assert.That(Call(manager, "GetWrongAnswerSnapshot"), Is.EqualTo("Question 2 = 99"));
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(-1, true)]
        [TestCase(6, false)]
        public void PictureGridRejectsEveryOmissionAndExtraSelection(int changed, bool expected)
        {
            var manager = Component("PictureGridManager");
            var buttons = new System.Collections.Generic.List<Button>();
            Set(manager, "correctIndexes", new System.Collections.Generic.List<int> { 0, 1, 7, 8, 17, 19 });
            int[] correct = { 0, 1, 7, 8, 17, 19 };
            for (int i = 0; i < 20; i++)
            {
                GameObject go = Object("Picture " + i);
                var image = go.AddComponent<Image>();
                buttons.Add(go.AddComponent<Button>());
                bool selected = System.Array.IndexOf(correct, i) >= 0;
                if (changed >= 0 && changed < 6 && i == correct[changed]) selected = false;
                if (changed == 6 && i == 2) selected = true;
                image.color = (Color)Get(manager, selected ? "selectedColor" : "defaultColor");
            }
            Set(manager, "pictureButtons", buttons);
            Assert.That(Call(manager, "IsSolved"), Is.EqualTo(expected));
            Call(manager, "ResetPuzzle");
            Assert.That(Call(manager, "IsSolved"), Is.False);
            foreach (Button button in buttons) Assert.That(button.interactable, Is.True);
        }
    }
}
