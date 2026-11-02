using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SGH.Tests
{
    public class ChatAndStabilityTests : GameTestFixture
    {
        [TestCase("Activity2PuzzleManager", "Activity2DropSlot")]
        [TestCase("InsulinPuzzleManager", "Activity6DropSlot")]
        public void DelayedCardFeedbackHandlesGlobalRegistryContainingOtherPuzzleSlots(string managerType, string slotType)
        {
            var manager = Component(managerType);
            var other = Component("Activity1DropSlot");
            var own = Component(slotType);
            Call(other, "Start"); Call(own, "Start");
            var slots = (System.Collections.IList)Get(RuntimeType("DropSlot"), "allDropSlots");
            Assert.That(slots.Contains(other) && slots.Contains(own), Is.True);
            var routine = (IEnumerator)Call(manager, "ResetIncorrectCardColorsAfterDelay", 0f);
            Assert.That(routine.MoveNext(), Is.True);
            Assert.DoesNotThrow(() => { Assert.That(routine.MoveNext(), Is.False); });
        }

        [UnityTest]
        public IEnumerator HighlightShutdownRestoresScaleWithoutStartingInactiveCoroutine()
        {
            var target = Object("Highlight shutdown");
            target.transform.localScale = new Vector3(2, 3, 4);
            var effect = target.AddComponent(RuntimeType("HighlightEffect"));
            Root.SetActive(true);
            Call(effect, "Highlight");
            yield return null;
            target.SetActive(false);
            Call(effect, "RestartScale", Vector3.one);
            Assert.That(target.transform.localScale, Is.EqualTo(new Vector3(2, 3, 4)));
        }

        private Component Chat()
        {
            var chat = Component("ChatManager");
            Set(chat, "chatAppId", ""); // No production connection from fixtures.
            Set(chat, "inputField", Input()); Set(chat, "sendButton", Object("Send").AddComponent<Button>());
            Set(chat, "chatContent", Object("Messages").transform); Set(chat, "chatPanel", Object("Chat panel"));
            Set(chat, "unreadBadge", Object("Badge")); Set(chat, "unreadText", Object("Unread").AddComponent<TextMeshProUGUI>());
            Set(chat, "currentChannel", "synthetic-room");
            return chat;
        }

        [Test]
        public void ChatIgnoresOtherChannelsAndClearsUnreadOnlyWhenOpened()
        {
            var chat = Chat(); Set(chat, "isVisible", false);
            Call(chat, "OnGetMessages", "other-room", new[] { "Synthetic A" }, new object[] { "ignore" });
            Assert.That(Get(chat, "unreadCount"), Is.EqualTo(0));
            Call(chat, "OnGetMessages", "synthetic-room", new[] { "Synthetic A", "Synthetic B" }, new object[] { "one", "two" });
            Assert.That(Get(chat, "unreadCount"), Is.EqualTo(2));
            Assert.That(((Transform)Get(chat, "chatContent")).childCount, Is.EqualTo(2));
            Call(chat, "ToggleChatPanel"); Assert.That(Get(chat, "unreadCount"), Is.Zero);
            Assert.That(((GameObject)Get(chat, "unreadBadge")).activeSelf, Is.False);
        }

        [Test]
        public void ChatUiRebindingDoesNotDuplicateCollapseListenersAcrossSessions()
        {
            var chat = Chat(); var button = Object("Collapse").AddComponent<Button>(); Set(chat, "collapseButton", button);
            for (int session = 0; session < 20; session++) Call(chat, "SetupUiBindings");
            Set(chat, "isVisible", true); button.onClick.Invoke();
            Assert.That(Get(chat, "isVisible"), Is.False, "One click must toggle exactly once after repeated rebinds");
        }

        [UnityTest]
        public IEnumerator RepeatedPopupOpenCloseDoesNotAccumulatePresentationObjects()
        {
            var popup = Component("CluePopupUI"); var panel = Object("Popup").AddComponent<CanvasGroup>();
            Set(popup, "panel", panel); Set(popup, "message", Object("Text").AddComponent<TextMeshProUGUI>());
            Set(popup, "fadeInDuration", 0f); Set(popup, "holdDuration", 0f); Set(popup, "fadeOutDuration", 0f);
            object saved = Get(RuntimeType("CluePopupUI"), "Instance"); Set(RuntimeType("CluePopupUI"), "Instance", null);
            try
            {
                Root.SetActive(true); int baseline = Root.GetComponentsInChildren<Transform>(true).Length;
                for (int i = 0; i < 50; i++)
                {
                    Call(popup, "Show", "synthetic-cycle-" + i);
                    float due = Time.realtimeSinceStartup + 2;
                    while ((bool)Get(popup, "isShowing") && Time.realtimeSinceStartup < due) yield return null;
                    Assert.That(Get(popup, "isShowing"), Is.False);
                }
                Assert.That(Root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(baseline));
            }
            finally { Root.SetActive(false); Set(RuntimeType("CluePopupUI"), "Instance", saved); }
        }

        [Test]
        public void EventSystemClickUsesRealButtonAndRestoresSidebarState()
        {
            var eventSystem = Object("Events").AddComponent<EventSystem>();
            var button = Object("Button").AddComponent<Button>(); int calls = 0;
            button.onClick.AddListener(() => calls++);
            Root.SetActive(true);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Assert.That(calls, Is.EqualTo(1));
        }
    }
}
