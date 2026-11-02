using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SGH.Tests
{
    public class VisualAndSoakTests : GameTestFixture
    {
        private readonly List<GameObject> previous = new List<GameObject>();
        private float savedScale;

        [SetUp]
        public void CaptureOriginalObjects()
        {
            Assert.That(PhotonNetwork.IsConnected, Is.False);
            UnityEngine.Object.DontDestroyOnLoad(Root);
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.scene.IsValid() && go.transform.parent == null) previous.Add(go);
            savedScale = Time.timeScale;
            SceneManager.sceneLoaded += DisableConnections;
        }

        private static void DisableConnections(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects()) foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                if (RuntimeType("Network").IsAssignableFrom(component.GetType()) || component.GetType().Name == "ChatManager") component.enabled = false;
                if (component.GetType().Name == "ChatManager") Set(component, "chatAppId", "");
            }
            object logger = Get(RuntimeType("GameSessionCsvLogger"), "Instance");
            if (logger != null) Set(logger, "hasWrittenCsv", true);
            var chat = Get(RuntimeType("ChatManager"), "Instance") as MonoBehaviour;
            if (chat != null)
            {
                Set(chat, "chatAppId", ""); chat.enabled = false;
                var client = Get(chat, "chatClient"); if (client != null) Call(client, "Disconnect");
            }
        }

        [UnityTearDown]
        public IEnumerator CleanScenes()
        {
            SceneManager.sceneLoaded -= DisableConnections;
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom(false);
            yield return null;
            PhotonNetwork.OfflineMode = false; Time.timeScale = savedScale;
            var scene = SceneManager.CreateScene("Visual test cleanup"); SceneManager.SetActiveScene(scene);
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go != null && go.scene.IsValid() && go.transform.parent == null && !previous.Contains(go) && go != Root && !go.name.Contains("TestRunner"))
                    UnityEngine.Object.Destroy(go);
            yield return null;
        }

        private IEnumerator Load(string scene)
        {
            PhotonNetwork.OfflineMode = true; PhotonNetwork.CreateRoom("synthetic-visual");
            yield return SceneManager.LoadSceneAsync(scene); yield return null;
        }

        [UnityTest, Category("Visual"), Timeout(900000)]
        public IEnumerator LobbyControlsFitFiveAspectRatiosAndProduceScreenshots()
        {
            yield return Load("Lobby");
            yield return CaptureFiveRatios("Lobby");
        }

        [UnityTest, Category("Visual"), Timeout(900000)]
        public IEnumerator AuthoredPuzzlePagesFitFiveAspectRatiosAndProduceScreenshots()
        {
            yield return Load("GameScene");
            foreach (var director in UnityEngine.Object.FindObjectsByType(RuntimeType("StorylineDirector"), FindObjectsSortMode.None).Cast<MonoBehaviour>())
            { director.StopAllCoroutines(); director.enabled = false; }
            var comic = UnityEngine.Object.FindObjectsByType(RuntimeType("SlideshowUI"), FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Cast<Component>().First(c => (bool)Get(c, "advancePuzzleStageOnClose"));
            for (int i = 0; i < ((Array)Get(comic, "slides")).Length; i++) Call(comic, "ShowNextImage");
            ((Button)Get(comic, "closeButton")).onClick.Invoke();
            var flow = (Component)Get(RuntimeType("PuzzleStateManager"), "Instance");
            for (int stage = 1; stage <= 11; stage++)
            {
                Set(flow, "CurrentStage", Enum.ToObject(RuntimeType("PuzzleStateManager+PuzzleStage"), stage));
                var panel = (GameObject)Get(flow, "Puzzle" + stage + "Panel"); panel.SetActive(true);
                yield return CaptureFiveRatios("GameScene-stage" + stage + "-part1");
                if (stage == 4) Call(Get(flow, "activity2PuzzleManager"), "GoToPart2");
                if (stage == 6) Call(Get(flow, "diaryPuzzleManager"), "GoToStage", 2);
                if (stage == 10) Call(Get(flow, "insulinPuzzleManager"), "ApplyRoomPart", 2, false);
                if (stage == 11) Call(Get(flow, "deathLapsesManager"), "GoToStage2");
                if (stage == 4 || stage == 6 || stage == 10 || stage == 11)
                    yield return CaptureFiveRatios("GameScene-stage" + stage + "-part2");
                if (stage == 6)
                {
                    Call(Get(flow, "diaryPuzzleManager"), "GoToStage", 3);
                    yield return CaptureFiveRatios("GameScene-stage6-part3");
                }
                panel.SetActive(false);
            }
        }

        private IEnumerator CaptureFiveRatios(string label)
        {
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1920,1200), new Vector2Int(3440,1440), new Vector2Int(768,1024), new Vector2Int(1280,720) })
            {
                var cameraObject = Object("Snapshot camera"); var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.gray;
                camera.cullingMask = 1 << LayerMask.NameToLayer("UI"); // Capture screen UI without unrelated geometry at the temporary camera origin.
                var target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target;
                foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (canvas.renderMode == RenderMode.WorldSpace) continue;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                    var scaler = canvas.GetComponent<CanvasScaler>();
                    if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    {
                        scaler.enabled = false;
                        float x = Mathf.Log(size.x / scaler.referenceResolution.x, 2), y = Mathf.Log(size.y / scaler.referenceResolution.y, 2);
                        float logScale = scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand ? Mathf.Min(x, y) :
                            scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink ? Mathf.Max(x, y) : Mathf.Lerp(x, y, scaler.matchWidthOrHeight);
                        canvas.scaleFactor = Mathf.Pow(2, logScale);
                    }
                }
                yield return null; Canvas.ForceUpdateCanvases(); camera.Render();
                var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                var saved = RenderTexture.active; RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); pixels.Apply(); RenderTexture.active = saved;
                string directory = Path.GetFullPath("TestResults/System/screenshots"); Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, label + "-" + size.x + "x" + size.y + ".png"), pixels.EncodeToPNG());
                var failures = new List<string>(); int checkedButtons = 0;
                foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                {
                    if (!button.IsActive() || !button.interactable || button.GetComponentInParent<Canvas>() == null) continue;
                    var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners); checkedButtons++;
                    if (corners.Any(c => { var p = camera.WorldToViewportPoint(c); return p.x < -0.01f || p.x > 1.01f || p.y < -0.01f || p.y > 1.01f; })) failures.Add(button.name);
                }
                UnityEngine.Object.Destroy(pixels); camera.targetTexture = null; target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(cameraObject);
                Assert.That(checkedButtons, Is.GreaterThan(0), "No visible controls checked");
                Assert.That(failures, Is.Empty, size + " offscreen controls: " + string.Join(",", failures));
            }
        }

        [UnityTest, Category("Soak")]
        public IEnumerator RepeatedRealLobbyLayoutRemainsBounded()
        {
            yield return Load("Lobby");
            int cycles = int.TryParse(Environment.GetEnvironmentVariable("SGH_SOAK_CYCLES"), out int configured) ? configured : 2000;
            Assert.That(cycles, Is.InRange(100, 100000));
            int initialCount = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            GC.Collect(); long initialMemory = Profiler.GetTotalAllocatedMemoryLong();
            var timings = new List<float>();
            for (int i = 0; i < cycles; i++)
            {
                float began = Time.realtimeSinceStartup;
                Canvas.ForceUpdateCanvases(); yield return null;
                timings.Add((Time.realtimeSinceStartup - began) * 1000);
            }
            GC.Collect(); yield return null;
            int finalCount = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            long growth = Profiler.GetTotalAllocatedMemoryLong() - initialMemory; timings.Sort();
            float p99 = timings[Math.Min(timings.Count - 1, (int)(timings.Count * .99f))];
            Directory.CreateDirectory("TestResults/System");
            File.WriteAllText("TestResults/System/soak-metrics.json", JsonUtility.ToJson(new Metrics { cycles = cycles, objectGrowth = finalCount - initialCount, bytesGrowth = growth, frameP99Ms = p99 }, true));
            Assert.That(finalCount - initialCount, Is.LessThanOrEqualTo(5), "Unexpected persistent object growth");
            Assert.That(growth, Is.LessThan(64L * 1024 * 1024), "More than 64MiB retained during idle layout soak");
            Assert.That(p99, Is.LessThan(500f), "CI smoke budget exceeded; see metrics, not a production FPS qualification");
        }

        [Serializable] private class Metrics { public int cycles; public int objectGrowth; public long bytesGrowth; public float frameP99Ms; }
    }
}
