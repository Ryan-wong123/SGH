using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SGH.Tests
{
    public class CardPuzzleTests : GameTestFixture
    {
        private object savedState;
        private object savedSlots;
        private Component state;

        [SetUp]
        public void ConfigureIsolatedPuzzleState()
        {
            savedState = Get(RuntimeType("PuzzleStateManager"), "Instance");
            savedSlots = Get(RuntimeType("DropSlot"), "allDropSlots");
            Set(RuntimeType("DropSlot"), "allDropSlots", Activator.CreateInstance(savedSlots.GetType()));
            state = Component("PuzzleStateManager");
            // Do not activate the persistent network manager. Only the validators need it.
            Set(RuntimeType("PuzzleStateManager"), "Instance", state);
        }

        [TearDown]
        public void RestorePuzzleState()
        {
            Set(RuntimeType("PuzzleStateManager"), "Instance", savedState);
            Set(RuntimeType("DropSlot"), "allDropSlots", savedSlots);
            Time.timeScale = 1f;
        }

        private void Stage(int value)
        {
            Set(state, "CurrentStage", Enum.ToObject(RuntimeType("PuzzleStateManager+PuzzleStage"), value));
        }

        private Component Card(Component slot, string answer)
        {
            GameObject go = Object("Card");
            go.transform.SetParent(slot.transform, false);
            go.AddComponent<Image>(); go.AddComponent<CanvasGroup>();
            Component card = go.AddComponent(RuntimeType("DraggableCard"));
            Set(card, "symptomName", answer); Set(card, "stepName", answer); Set(card, "lapsesAnswer", answer);
            Call(card, "Awake");
            return card;
        }

        private Component Slot(string type, string answer, bool filled = true)
        {
            Component slot = Component(type);
            ((IList)Get(RuntimeType("DropSlot"), "allDropSlots")).Add(slot);
            if (type == "Activity1DropSlot" || type == "Activity1_2DropSlot")
                Set(slot, "correctAnswers", new System.Collections.Generic.List<string> { answer });
            else if (type == "Activity2DropSlot" || type == "Activity6DropSlot") Set(slot, "stepName", answer);
            if (filled) Card(slot, answer);
            return slot;
        }

        private void EnableValidator(Component manager)
        {
            // Enable the validator alone; its parent stays inactive until dependencies are ready.
            manager.transform.SetParent(null);
            manager.gameObject.SetActive(true);
        }

        [UnityTest]
        public IEnumerator SymptomsRequireEveryRegisteredSlotAndRejectWrongOrMissingCard()
        {
            Stage(1);
            Component manager = Component("SymptomsCardManager");
            Component first = Slot("Activity1DropSlot", "sweating");
            Component second = Slot("Activity1DropSlot", "shaking", false);
            EnableValidator(manager);
            try
            {
                Assert.That(Call(manager, "IsSolved"), Is.False);
                Component card = Card(second, "wrong");
                Assert.That(Call(manager, "IsSolved"), Is.False);
                Set(card, "symptomName", "shaking");
                Assert.That(Call(manager, "IsSolved"), Is.True);
                Assert.That(first.transform.childCount, Is.GreaterThan(0));
                yield return null;
            }
            finally { UnityEngine.Object.DestroyImmediate(manager.gameObject); }
        }

        [UnityTest]
        public IEnumerator PaletteThresholdCountsOnlyFirstCorrectCardPerSlot()
        {
            Stage(2);
            Component manager = Component("BearCardManager"); Set(manager, "required", 10);
            for (int i = 0; i < 9; i++) Slot("Activity1_2DropSlot", "symptom " + i);
            Component last = Slot("Activity1_2DropSlot", "last", false);
            EnableValidator(manager);
            try
            {
                Assert.That(Call(manager, "IsSolved"), Is.False);
                Card(last, "last");
                Assert.That(Call(manager, "IsSolved"), Is.True);
                Slot("Activity1_2DropSlot", "extra", false);
                Assert.That(Call(manager, "IsSolved"), Is.True, "Palette uses a threshold rather than all slots");
                yield return null;
            }
            finally { UnityEngine.Object.DestroyImmediate(manager.gameObject); }
        }

        [UnityTest]
        public IEnumerator SurvivalCardsRequireTwentyAndPartTwoDoesNotCompleteFromPartOne()
        {
            Stage(4);
            Component manager = Component("Activity2PuzzleManager"); Set(manager, "required", 20);
            GameObject part1 = Object("Part 1"), part2 = Object("Part 2"); part2.SetActive(false);
            Set(manager, "panel1", part1); Set(manager, "panel2", part2);
            Set(manager, "input1", Input("15")); Set(manager, "input2", Input("4"));
            for (int i = 0; i < 19; i++) Slot("Activity2DropSlot", "Step " + i);
            EnableValidator(manager);
            try
            {
                Assert.That(Call(manager, "SubmitActivity2Stage").ToString(), Is.EqualTo("Incorrect"));
                Slot("Activity2DropSlot", "last");
                Assert.That(Call(manager, "SubmitActivity2Stage").ToString(), Is.EqualTo("Advanced"));
                Assert.That(Get(manager, "CurrentPart"), Is.EqualTo(1));
                Call(manager, "GoToPart2");
                Assert.That(part1.activeSelf, Is.False); Assert.That(part2.activeSelf, Is.True);
                Assert.That(Call(manager, "SubmitActivity2Stage").ToString(), Is.EqualTo("Completed"));
                yield return null;
            }
            finally { UnityEngine.Object.DestroyImmediate(manager.gameObject); }
        }

        [UnityTest]
        public IEnumerator InsulinRequiresNineCorrectCardsAndResetRestoresSyringePage()
        {
            Component manager = Component("InsulinPuzzleManager"); Set(manager, "required", 9);
            GameObject part1 = Object("Part 1"), part2 = Object("Part 2");
            Set(manager, "panel1", part1); Set(manager, "panel2", part2);
            Set(manager, "syringeDropdown", Dropdown(1));
            for (int i = 0; i < 8; i++) Slot("Activity6DropSlot", "Step " + i);
            EnableValidator(manager);
            try
            {
                Assert.That(Call(manager, "IsSolved"), Is.False);
                Slot("Activity6DropSlot", "last");
                Assert.That(Call(manager, "IsSolved"), Is.True);
                Call(manager, "GoToPart2");
                Call(manager, "ResetPuzzle");
                Assert.That(Get(manager, "CurrentPart"), Is.EqualTo(1));
                Assert.That(part1.activeSelf, Is.True); Assert.That(part2.activeSelf, Is.False);
                yield return null;
            }
            finally { UnityEngine.Object.DestroyImmediate(manager.gameObject); }
        }

        [UnityTest]
        public IEnumerator LapsesStoreFiveAnswersThenRequireFourLinkedMatches()
        {
            Component manager = Component("DeathLapsesManager");
            Set(manager, "requiredAnswer1", 5); Set(manager, "requiredAnswer2", 4);
            Set(manager, "answer2Cards", Object("Part2Cards")); Set(manager, "answer2Slots", Object("Part2Slots"));
            var first = new System.Collections.Generic.List<Component>();
            for (int i = 0; i < 5; i++)
            {
                Component slot = Slot("Activity9DropSlot", " Answer " + i + " "); slot.tag = "Stage1Slot"; first.Add(slot);
            }
            EnableValidator(manager);
            try
            {
                Assert.That(Call(manager, "SubmitDeathLapsesStage").ToString(), Is.EqualTo("Advanced"));
                Assert.That(Get(first[0], "stage1Answer"), Is.EqualTo("answer 0"));
                Call(manager, "GoToStage2");
                Assert.That(Call(manager, "SubmitDeathLapsesStage").ToString(), Is.EqualTo("Incorrect"));
                for (int i = 0; i < 4; i++)
                {
                    Component slot = Slot("Activity9DropSlot", "ANSWER " + i); slot.tag = "Stage2Slot";
                    Set(slot, "linkedStage1Slot", first[i]);
                }
                Assert.That(Call(manager, "SubmitDeathLapsesStage").ToString(), Is.EqualTo("Completed"));
                Call(manager, "ResetPuzzle"); Assert.That(Get(manager, "CurrentStage"), Is.EqualTo(1));
                yield return null;
            }
            finally { UnityEngine.Object.DestroyImmediate(manager.gameObject); }
        }
    }
}
