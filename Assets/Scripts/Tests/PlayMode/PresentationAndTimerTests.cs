using System;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SGH.Tests
{
    public class PresentationAndTimerTests : GameTestFixture
    {
        private float savedTimeScale;
        private CursorLockMode savedCursorLock;
        private bool savedCursorVisible;

        [SetUp]
        public void SavePresentationState()
        {
            savedTimeScale = Time.timeScale;
            savedCursorLock = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
        }

        [TearDown]
        public void RestorePresentationState()
        {
            Time.timeScale = savedTimeScale;
            Cursor.lockState = savedCursorLock;
            Cursor.visible = savedCursorVisible;
        }

        [UnityTest]
        public IEnumerator PopupQueueRunsInOrderWhileScaledTimeIsPaused()
        {
            Component popup = Component("CluePopupUI");
            CanvasGroup panel = Object("Popup").AddComponent<CanvasGroup>();
            TMP_Text message = Object("Message").AddComponent<TextMeshProUGUI>();
            Set(popup, "panel", panel); Set(popup, "message", message);
            Set(popup, "fadeInDuration", 0.01f); Set(popup, "holdDuration", 0.1f); Set(popup, "fadeOutDuration", 0.01f);
            object previous = Get(RuntimeType("CluePopupUI"), "Instance");
            Set(RuntimeType("CluePopupUI"), "Instance", null);
            try
            {
                Root.SetActive(true);
                Time.timeScale = 0f;
                Call(popup, "Show", "First synthetic message"); Call(popup, "Show", "Second synthetic message");
                yield return null;
                Assert.That(message.text, Is.EqualTo("First synthetic message"));
                float deadline = Time.realtimeSinceStartup + 3f;
                while (message.text != "Second synthetic message" && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(message.text, Is.EqualTo("Second synthetic message"));
                while ((bool)Get(popup, "isShowing") && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(Get(popup, "isShowing"), Is.False);
                Assert.That(panel.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Root.SetActive(false);
                Set(RuntimeType("CluePopupUI"), "Instance", previous);
            }
        }

        [UnityTest]
        public IEnumerator RegularClueTakesSharedCloseAfterSlideshowAndInvokesCallbackOnce()
        {
            Component clue = Component("ClueUI");
            GameObject main = Object("Clue");
            GameObject regular = Object("Regular clue"); regular.transform.SetParent(main.transform, false);
            Button close = Object("Close").AddComponent<Button>(); close.transform.SetParent(main.transform, false);
            Component slideshow = Component("SlideshowUI"); slideshow.transform.SetParent(main.transform, false);
            slideshow.gameObject.SetActive(false);
            Set(slideshow, "displayImage", Object("Comic image").AddComponent<Image>());
            Set(slideshow, "prevButton", Object("Previous").AddComponent<Button>());
            Set(slideshow, "nextButton", Object("Next").AddComponent<Button>());
            Set(slideshow, "closeButton", close);
            Set(slideshow, "requireAllPlayersToClickClose", false);
            Set(slideshow, "slides", Array.CreateInstance(RuntimeType("SlideshowUI+SlideData"), 2));
            Set(clue, "mainRoot", main); Set(clue, "closeButton", close);
            var panels = (IDictionary)Get(clue, "_panels"); panels.Add("synthetic-clue", regular);
            object previous = Get(RuntimeType("ClueUI"), "Instance");
            object previousSlide = Get(RuntimeType("SlideshowUI"), "activeInstance");
            Set(RuntimeType("ClueUI"), "Instance", null);
            int callbacks = 0;
            try
            {
                Root.SetActive(true);
                // Awake rebuilds the dictionary from serialized data.
                panels.Add("synthetic-clue", regular);
                main.SetActive(true); slideshow.gameObject.SetActive(true);
                yield return null;
                Assert.That(close.gameObject.activeSelf, Is.False, "Comic Close appears only on its last slide");
                Assert.That(Call(clue, "ShowById", "synthetic-clue", (Action)(() => callbacks++)), Is.True);
                Assert.That(slideshow.gameObject.activeSelf, Is.False);
                Assert.That(close.gameObject.activeSelf, Is.True);
                Assert.That(close.interactable, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                close.onClick.Invoke(); close.onClick.Invoke();
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(main.activeSelf, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                Root.SetActive(false);
                Set(RuntimeType("ClueUI"), "Instance", previous);
                Set(RuntimeType("SlideshowUI"), "activeInstance", previousSlide);
            }
        }

        [UnityTest]
        public IEnumerator PhotonDeadlineCountdownContinuesWhileScaledTimeIsPaused()
        {
            Component timer = Component("GameTimerManager");
            TMP_Text text = Object("Time").AddComponent<TextMeshProUGUI>(); Set(timer, "timerText", text);
            // SyncTimer accepts an absolute Photon timestamp. Offline Photon.Time still advances.
            Type network = AppDomain.CurrentDomain.GetAssemblies()[0].GetType("Photon.Pun.PhotonNetwork");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                network = network ?? assembly.GetType("Photon.Pun.PhotonNetwork");
            Assert.That(Get(network, "IsConnected"), Is.False, "Run in the isolated Unity test scene");
            bool savedOfflineMode = (bool)Get(network, "OfflineMode");
            network.GetProperty("OfflineMode").SetValue(null, true);
            try
            {
                // Disconnected Photon has no advancing server clock. Its explicit
                // offline mode provides a local timestamp without joining a room.
                yield return null;
                double now = (double)Get(network, "Time");
                Call(timer, "SyncTimer", (float)now + 1f);
                Time.timeScale = 0f;
                float timeout = Time.realtimeSinceStartup + 5f;
                while ((bool)Get(timer, "timerRunning") && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                    Call(timer, "Update");
                }
                Assert.That(Get(timer, "timerRunning"), Is.False);
                Assert.That(text.text, Is.EqualTo("00:00"));
            }
            finally { network.GetProperty("OfflineMode").SetValue(null, savedOfflineMode); }
        }

        [UnityTest, Category("KnownGap"), Explicit("Reset does not currently cancel the insulin part-1 delay. Expected failure until durable/cancellable recovery is implemented.")]
        public IEnumerator InsulinResetDuringDelayMustNotReopenPartTwo()
        {
            Component insulin = Component("InsulinPuzzleManager");
            GameObject first = Object("Part1"), second = Object("Part2"); second.SetActive(false);
            Set(insulin, "panel1", first); Set(insulin, "panel2", second); Set(insulin, "syringeDropdown", Dropdown(1));
            Root.SetActive(true);
            Call(insulin, "SubmitSyringeAnswer");
            Call(insulin, "ResetPuzzle");
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.That(second.activeSelf, Is.False);
            Assert.That(first.activeSelf, Is.True);
        }
    }
}
