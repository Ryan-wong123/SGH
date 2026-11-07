using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SGH.Tests
{
    public class GridAndInteractionTests : GameTestFixture
    {
        [TestCase("correct", true)]
        [TestCase("upper", true)]
        [TestCase("missing", false)]
        [TestCase("wrong", false)]
        [TestCase("empty", false)]
        public void CrosswordChecksEveryActiveLetter(string mode, bool expected)
        {
            Component manager = Component("CrosswordManager");
            var solution = (char[,])Get(manager, "solution");
            Array grid = Array.CreateInstance(RuntimeType("CrosswordCell"), 13, 13);
            bool changed = false;
            for (int y = 0; y < 13; y++)
                for (int x = 0; x < 13; x++)
                {
                    if (solution[y, x] == '\0') continue;
                    Component cell = Component("CrosswordCell");
                    string value = mode == "upper" ? char.ToUpperInvariant(solution[y, x]).ToString() : solution[y, x].ToString();
                    if (mode == "empty") value = "";
                    if (!changed && (mode == "missing" || mode == "wrong")) { value = mode == "missing" ? "" : "?"; changed = true; }
                    Set(cell, "inputField", Input(value)); grid.SetValue(cell, y, x);
                }
            Set(manager, "grid", grid);
            Assert.That(Call(manager, "IsSolved"), Is.EqualTo(expected));
            if (!expected) Assert.That((string)Call(manager, "GetWrongAnswerSnapshot"), Does.Contain("expected"));
        }

        private Component WordSearch(out Array grid)
        {
            Component manager = Component("WordSearchManager"); Set(manager, "gridSize", 21);
            var solution = (char[,])Get(manager, "solution");
            grid = Array.CreateInstance(RuntimeType("LetterCell"), 21, 21);
            for (int y = 0; y < 21; y++)
                for (int x = 0; x < 21; x++)
                {
                    Component cell = Component("LetterCell");
                    Set(cell, "letterText", Object("Letter").AddComponent<TextMeshProUGUI>());
                    Set(cell, "backgroundImage", cell.gameObject.AddComponent<Image>());
                    Call(cell, "Init", manager, new Vector2Int(x, y));
                    Call(cell, "SetPlayable", x > 0 && y > 0);
                    Call(cell, "SetLetter", solution[y, x] == '\0' ? 'X' : solution[y, x]);
                    grid.SetValue(cell, x, y);
                }
            Set(manager, "grid", grid);
            return manager;
        }

        [TestCase("correct", true)]
        [TestCase("missing", false)]
        [TestCase("extra", false)]
        [TestCase("wrong", false)]
        [TestCase("empty", false)]
        public void WordSearchRequiresAllSevenWordsWithoutExtras(string mode, bool expected)
        {
            Component manager = WordSearch(out Array grid);
            var positions = new HashSet<Vector2Int>();
            foreach (string word in new[] { "FLUOROQUINOLONES", "SULFONYLUREAS", "LINEZOLID", "AZOLES", "ELDERLY", "IMPAIRED", "REDUCTION" })
            {
                object[] args = { word, null };
                Assert.That(Call(manager, "TryFindWordPositions", args), Is.True, word);
                foreach (Vector2Int position in (IEnumerable)args[1]) positions.Add(position);
            }
            bool changed = false;
            foreach (Vector2Int p in positions)
            {
                if (mode == "empty") continue;
                if (!changed && mode == "missing") { changed = true; continue; }
                object cell = grid.GetValue(p.x, p.y);
                if (!changed && mode == "wrong") { Call(cell, "SetLetter", '?'); changed = true; }
                Call(manager, "ToggleCell", cell);
            }
            if (mode == "extra") Call(manager, "ToggleCell", grid.GetValue(2, 2));
            Assert.That(Call(manager, "IsSolved"), Is.EqualTo(expected));
        }

        [Test]
        public void WordSearchDragAddsThenRemovesAndIgnoresHeaders()
        {
            Component manager = WordSearch(out Array grid);
            object header = grid.GetValue(0, 1), a = grid.GetValue(1, 1), b = grid.GetValue(2, 1);
            Call(manager, "BeginDrag", header); Assert.That(Get(header, "IsSelected"), Is.False);
            Call(manager, "BeginDrag", a); Call(manager, "DragOver", b); Call(manager, "EndDrag");
            Assert.That(Get(a, "IsSelected"), Is.True); Assert.That(Get(b, "IsSelected"), Is.True);
            Call(manager, "BeginDrag", a); Call(manager, "DragOver", b); Call(manager, "EndDrag");
            Assert.That(Get(a, "IsSelected"), Is.False); Assert.That(Get(b, "IsSelected"), Is.False);
        }

        [Test]
        public void WordSearchFillerIsDeterministicForTheSameSceneAndDimensions()
        {
            Component manager = WordSearch(out Array grid);
            var solution = (char[,])Get(manager, "solution");
            Call(grid.GetValue(2, 2), "SetLetter", ' ');
            Call(manager, "FillRandomLetters"); char first = (char)Call(grid.GetValue(2, 2), "GetLetter");
            Call(grid.GetValue(2, 2), "SetLetter", ' ');
            Call(manager, "FillRandomLetters");
            Assert.That(Call(grid.GetValue(2, 2), "GetLetter"), Is.EqualTo(first));
            Assert.That(Call(grid.GetValue(12, 1), "GetLetter"), Is.EqualTo(solution[1, 12]));
        }

        [Test]
        public void CardHierarchyPathsDisambiguateDuplicateSiblingNames()
        {
            Component card = Component("DraggableCard"); Set(card, "pathRoot", Root.transform);
            GameObject first = Object("Slot"), second = Object("Slot");
            GameObject nested = Object("Nested"); nested.transform.SetParent(second.transform);
            foreach (Transform target in new[] { first.transform, second.transform, nested.transform, Root.transform })
            {
                string path = (string)Call(card, "GetPathFromRoot", target);
                Assert.That(Call(card, "FindByPath", path), Is.SameAs(target));
            }
            Assert.That(Call(card, "GetPathFromRoot", second.transform), Is.EqualTo("Slot|1"));
            Assert.That(Call(card, "FindByPath", "Missing|0"), Is.Null);
        }

        [Test]
        public void KeypadLimitsNineDigitsBackspacesAndIgnoresInputAfterGrant()
        {
            Component keypad = Component("NavKeypad.Keypad"); Call(keypad, "Awake");
            for (int i = 0; i < 12; i++) Call(keypad, "AddInput", "1");
            Assert.That(Get(keypad, "currentInput"), Is.EqualTo("111111111"));
            Call(keypad, "AddInput", "back"); Assert.That(Get(keypad, "currentInput"), Is.EqualTo("11111111"));
            Call(keypad, "ClearInput"); Call(keypad, "AddInput", "back");
            Assert.That(Get(keypad, "currentInput"), Is.Empty);
            Set(keypad, "accessWasGranted", true); Call(keypad, "AddInput", "2");
            Assert.That(Get(keypad, "currentInput"), Is.Empty);
        }

        [Test]
        public void KeypadDenialClearsForRetryButGrantLatches()
        {
            Component keypad = Component("NavKeypad.Keypad");
            Set(keypad, "keypadDisplayText", Object("Display").AddComponent<TextMeshProUGUI>());
            Call(keypad, "Awake"); Call(keypad, "AddInput", "123");
            var denial = (IEnumerator)Call(keypad, "DisplayResultRoutine", false);
            Assert.That(denial.MoveNext(), Is.True); Assert.That(Get(keypad, "displayingResult"), Is.True);
            Assert.That(denial.MoveNext(), Is.False); Assert.That(Get(keypad, "currentInput"), Is.Empty);
            Call(keypad, "AddInput", "325");
            var grant = (IEnumerator)Call(keypad, "DisplayResultRoutine", true);
            Assert.That(grant.MoveNext(), Is.True); Assert.That(grant.MoveNext(), Is.False);
            Assert.That(Get(keypad, "accessWasGranted"), Is.True);
            Assert.That(((TMP_Text)Get(keypad, "keypadDisplayText")).text, Is.EqualTo("Granted"));
        }

        [Test]
        public void GuidanceRejectsOutOfOrderArrivalAndCompletesInSequence()
        {
            Component pointer = Component("ObjectivePointer");
            object path = Nested("ObjectivePointer", "WaypointPath");
            Transform[] points = { Object("A").transform, Object("B").transform, Object("C").transform };
            Set(path, "id", "Canteen"); Set(path, "waypoints", points);
            Array paths = Array.CreateInstance(path.GetType(), 1); paths.SetValue(path, 0); Set(pointer, "paths", paths);
            Call(pointer, "StartPath", "Canteen"); Call(pointer, "ReachWaypoint", "Canteen", 2);
            Assert.That(Get(pointer, "currentTarget"), Is.SameAs(points[0]));
            Call(pointer, "ReachWaypoint", "other", 0); Assert.That(Get(pointer, "currentTarget"), Is.SameAs(points[0]));
            Call(pointer, "ReachWaypoint", "Canteen", 0); Assert.That(Get(pointer, "currentTarget"), Is.SameAs(points[1]));
            Call(pointer, "ReachWaypoint", "Canteen", 1); Call(pointer, "ReachWaypoint", "Canteen", 2);
            Assert.That(Get(pointer, "currentTarget"), Is.Null); Assert.That(Get(pointer, "IsActive"), Is.False);
        }

        [Test]
        public void InventoryAndCluesDeduplicateAndCluesCanReset()
        {
            Component inventory = Component("InventorySystem");
            Call(inventory, "AddItem", "Synthetic key"); Call(inventory, "AddItem", "Synthetic key");
            Assert.That(Call(inventory, "HasItem", "Synthetic key"), Is.True);
            Assert.That(Call(inventory, "HasItem", "Other key"), Is.False);
            Assert.That((IEnumerable)Call(inventory, "GetAllItems"), Is.EquivalentTo(new[] { "Synthetic key" }));
            Component clues = Component("ClueSystem");
            Assert.That(Call(clues, "AddClue", ""), Is.False);
            Assert.That(Call(clues, "AddClue", "synthetic-clue"), Is.True);
            Assert.That(Call(clues, "AddClue", "synthetic-clue"), Is.False);
            Call(clues, "ClearAllClues"); Assert.That(Call(clues, "HasClue", "synthetic-clue"), Is.False);
        }
    }
}
