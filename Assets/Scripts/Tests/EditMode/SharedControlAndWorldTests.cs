using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SGH.Tests
{
    public class SharedControlAndWorldTests : GameTestFixture
    {
        [Test]
        public void InputReplicationRejectsDuplicateAndOutOfOrderSequencesPerActor()
        {
            TMP_InputField input = Input();
            Component sync = input.gameObject.AddComponent(RuntimeType("PhotonSyncedTMPInputField"));
            Call(sync, "Awake");
            Call(sync, "RPC_SetTextLWW", "new", 1, 2);
            Call(sync, "RPC_SetTextLWW", "old", 1, 1);
            Call(sync, "RPC_SetTextLWW", "duplicate", 1, 2);
            Assert.That(input.text, Is.EqualTo("new"));
            Call(sync, "RPC_SetTextLWW", "other actor", 2, 1);
            Assert.That(input.text, Is.EqualTo("other actor"));
            Assert.That(Get(sync, "suppressCallbacks"), Is.False);
            Assert.That(Get(sync, "localSeq"), Is.EqualTo(0), "Remote updates must not echo as local input");
        }

        [TestCase(-10, 0)] [TestCase(1, 1)] [TestCase(99, 2)]
        public void DropdownReplicationClampsAndSuppressesLocalCallbacks(int incoming, int expected)
        {
            TMP_Dropdown dropdown = Dropdown();
            Component sync = dropdown.gameObject.AddComponent(RuntimeType("PhotonSyncedTMPDropdown"));
            Call(sync, "Awake");
            Call(sync, "RPC_SetDropdownValue", incoming);
            Assert.That(dropdown.value, Is.EqualTo(expected));
            Assert.That(Get(sync, "suppressCallbacks"), Is.False);
        }

        [TestCase("DraggableCard")]
        [TestCase("PhotonSyncedTMPInputField")]
        [TestCase("PhotonSyncedTMPDropdown")]
        public void SharedColorsRoundTripAndRejectMalformedPayloads(string type)
        {
            Color color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            Type owner = RuntimeType(type);
            object encoded = Call(owner, "EncodeColor", color);
            object[] decode = { encoded, null };
            Assert.That(Call(owner, "TryDecodeColor", decode), Is.True);
            Assert.That(decode[1], Is.EqualTo(color));
            foreach (object bad in new object[] { null, "wrong", new float[] { 1f, 2f }, new int[] { 1, 2, 3, 4 } })
                Assert.That(Call(owner, "TryDecodeColor", new object[] { bad, null }), Is.False);
        }

        [TestCase(false, 0)] [TestCase(true, 0)]
        [TestCase(false, 1)] [TestCase(true, 1)]
        [TestCase(false, 2)] [TestCase(true, 2)]
        public void WordSearchSelectionAndResultEncodeIndependently(bool selected, int result)
        {
            Type manager = RuntimeType("WordSearchManager");
            object state = Enum.ToObject(RuntimeType("LetterCell+ResultVisualState"), result);
            object encoded = Call(manager, "EncodeCellState", selected, state);
            object[] args = { encoded, null, null };
            Call(manager, "DecodeCellState", args);
            Assert.That(args[1], Is.EqualTo(selected)); Assert.That(Convert.ToInt32(args[2]), Is.EqualTo(result));
        }

        [TestCase("ClueUI", "ShouldExpectPlayerClose")]
        [TestCase("SlideshowUI", "ShouldExpectPlayerClose")]
        [TestCase("PuzzleStateManager", "ShouldExpectPlayerTurn")]
        public void InactivePlayersDoNotBlockCloseVotesOrTurns(string type, string method)
        {
            Type playerType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Photon.Realtime.Player")).First(t => t != null);
            object player = Activator.CreateInstance(playerType, Members, null, new object[] { "Synthetic player", 12, false }, null);
            Component manager = Component(type);
            Assert.That(Call(manager, method, new object[] { null }), Is.False);
            Assert.That(Call(manager, method, player), Is.True);
            playerType.GetProperty("IsInactive").SetValue(player, true);
            Assert.That(Call(manager, method, player), Is.False);
        }

        [Test]
        public void DoorHardLockAndMissingKeyDenyUntilKeypadUnlock()
        {
            Component door = Component("DoorInteractable");
            Transform hinge = Object("Hinge").transform;
            Set(door, "hinge", hinge); Set(door, "closedRotLocal", Quaternion.identity);
            Set(door, "openRotLocal", Quaternion.Euler(0, 90, 0));
            Set(door, "isLocked", true); Set(door, "requiresKey", true);
            Call(door, "Interact"); Assert.That(Get(door, "HasBeenOpened"), Is.False);
            Set(door, "isLocked", false); Call(door, "Interact");
            Assert.That(Get(door, "HasBeenOpened"), Is.False);
            Call(door, "UnlockFromKeypad");
            Assert.That(Get(door, "isLocked"), Is.False); Assert.That(Get(door, "requiresKey"), Is.False);
            Assert.That(Get(door, "HasBeenOpened"), Is.True);
            Assert.That(Quaternion.Angle(hinge.localRotation, Quaternion.Euler(0, 90, 0)), Is.LessThan(0.01f));
            Call(door, "Interact"); Assert.That(Get(door, "isOpen"), Is.False);
            Assert.That(Get(door, "HasBeenOpened"), Is.True, "Chest entry uses historical opening, not current lid angle");
        }

        [Test]
        public void ComicTriggerRequiresPlayerAndActivatesOnlyOnce()
        {
            Component comic = Component("TriggerComic");
            GameObject target = Object("Comic target"); target.SetActive(false);
            Set(comic, "objectsToActivate", new[] { target });
            BoxCollider other = Object("Other collider").AddComponent<BoxCollider>();
            Call(comic, "OnTriggerEnter", other); Assert.That(target.activeSelf, Is.False);
            other.tag = "Player"; Call(comic, "OnTriggerEnter", other); Assert.That(target.activeSelf, Is.True);
            target.SetActive(false); Call(comic, "OnTriggerEnter", other); Assert.That(target.activeSelf, Is.False);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CharacterSelectionHighlightsExactlyOneRole(int index)
        {
            Component selection = Component("CharacterSelection");
            Button[] buttons = Enumerable.Range(0, 3).Select(i => { GameObject go = Object("Role " + i); go.AddComponent<Image>(); return go.AddComponent<Button>(); }).ToArray();
            Set(selection, "characterButtons", buttons); Call(selection, "UpdateButtonColors", index);
            for (int i = 0; i < buttons.Length; i++)
                Assert.That(buttons[i].GetComponent<Image>().color, Is.EqualTo(i == index ? Color.green : Color.white));
        }

        [Test]
        public void SidebarSwitchesPanelsAndBlockerAndClosesAll()
        {
            Component sidebar = Component("SidebarMenuManager");
            GameObject a = Object("Inventory"), b = Object("Clues"), blocker = Object("Blocker");
            a.SetActive(false); b.SetActive(false); blocker.SetActive(false);
            Set(sidebar, "popupBlocker", blocker);
            Call(sidebar, "TogglePopup", a); Assert.That(a.activeSelf && blocker.activeSelf, Is.True);
            Call(sidebar, "TogglePopup", b); Assert.That(a.activeSelf, Is.False); Assert.That(b.activeSelf, Is.True);
            Call(sidebar, "TogglePopup", b); Assert.That(b.activeSelf || blocker.activeSelf, Is.False);
            Call(sidebar, "TogglePopup", a); Call(sidebar, "CloseAllPopups");
            Assert.That(a.activeSelf || b.activeSelf || blocker.activeSelf, Is.False);
        }
    }
}
