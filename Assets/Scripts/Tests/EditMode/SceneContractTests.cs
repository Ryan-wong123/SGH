using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SGH.Tests
{
    // Preview scenes inspect Inspector wiring without replacing or saving the user's scene.
    public class SceneContractTests : GameTestFixture
    {
        private Scene game;
        private Scene lobby;

        [OneTimeSetUp]
        public void OpenScenes()
        {
            game = EditorSceneManager.OpenPreviewScene("Assets/Scenes/GameScene.unity");
            lobby = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Lobby.unity");
        }

        [OneTimeTearDown]
        public void CloseScenes()
        {
            if (game.IsValid()) EditorSceneManager.ClosePreviewScene(game);
            if (lobby.IsValid()) EditorSceneManager.ClosePreviewScene(lobby);
        }

        private Component[] All(Scene scene, string type)
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren(RuntimeType(type), true)).ToArray();
        }

        private Component Only(string type)
        {
            Component[] matches = All(game, type);
            Assert.That(matches.Length, Is.EqualTo(1), type + " must have exactly one configured instance");
            return matches[0];
        }

        [Test]
        public void EnabledBuildScenesMatchJourney()
        {
            Assert.That(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path),
                Is.EqualTo(new[] { "Assets/Scenes/Lobby.unity", "Assets/Scenes/GameScene.unity" }));
        }

        [TestCase(1, "symptomsCardManager")]
        [TestCase(2, "bearCardManager")]
        [TestCase(3, "pictureGridManager")]
        [TestCase(4, "activity2PuzzleManager")]
        [TestCase(5, "wordSearchManager")]
        [TestCase(6, "diaryPuzzleManager")]
        [TestCase(7, "risktableManager")]
        [TestCase(8, "prescriptionManager")]
        [TestCase(9, "crosswordManager")]
        [TestCase(10, "insulinPuzzleManager")]
        [TestCase(11, "deathLapsesManager")]
        public void EveryStageHasPanelAndPuzzleHandler(int stage, string managerField)
        {
            Component state = Only("PuzzleStateManager");
            Assert.That(Get(state, "Puzzle" + stage + "Panel") as GameObject, Is.Not.Null);
            object manager = Get(state, managerField);
            Assert.That(manager as Component, Is.Not.Null);
            Assert.That(RuntimeType("IPuzzle").IsInstanceOfType(manager), Is.True);
        }

        [Test]
        public void CountdownAndTransitionDelayMatchRequirements()
        {
            Component timer = Only("GameTimerManager");
            Assert.That(Get(timer, "countdownDuration"), Is.EqualTo(900f));
            IList warnings = (IList)Get(timer, "warnings");
            Assert.That(warnings.Cast<object>().Select(w => (float)Get(w, "thresholdSeconds")), Is.EquivalentTo(new[] { 300f, 120f }));
            Assert.That(Get(Only("PuzzleStateManager"), "delayTransition"), Is.EqualTo(2f));
            Assert.That(Get(timer, "timerText") as UnityEngine.Object, Is.Not.Null);
        }

        [TestCase("BearCardManager", "required", 10)]
        [TestCase("Activity2PuzzleManager", "required", 20)]
        [TestCase("InsulinPuzzleManager", "required", 9)]
        [TestCase("DeathLapsesManager", "requiredAnswer1", 5)]
        [TestCase("DeathLapsesManager", "requiredAnswer2", 4)]
        public void CardThresholdsMatchConfiguredRules(string type, string field, int count)
        {
            Assert.That(Get(Only(type), field), Is.EqualTo(count));
        }

        [Test]
        public void PictureGridHasTwentyUniqueButtonsAndExactAnswerIndices()
        {
            Component manager = Only("PictureGridManager");
            IList buttons = (IList)Get(manager, "pictureButtons");
            Assert.That(buttons.Count, Is.EqualTo(20));
            Assert.That(buttons.Cast<object>().Distinct().Count(), Is.EqualTo(20));
            Assert.That(buttons.Cast<UnityEngine.Object>().All(b => b != null), Is.True);
            Assert.That((IList)Get(manager, "correctIndexes"), Is.EquivalentTo(new[] { 0, 1, 7, 8, 17, 19 }));
        }

        [Test]
        public void DiaryHasAllEightFieldsAndThreePages()
        {
            Component diary = Only("DiaryPuzzleManager");
            Assert.That(((GameObject[])Get(diary, "stagePanels")).Length, Is.EqualTo(3));
            IList answers = (IList)Get(diary, "answers");
            Assert.That(answers.Count, Is.EqualTo(8));
            string[][] expected = { new[] { "32" }, new[] { "24", "8", "8", "8" }, new[] { "40", "20", "40" } };
            for (int stage = 1; stage <= 3; stage++)
            {
                object[] page = answers.Cast<object>().Where(a => (int)Get(a, "stage") == stage).ToArray();
                Assert.That(page.Select(a => ((string[])Get(a, "accepted"))[0]), Is.EqualTo(expected[stage - 1]));
                foreach (object entry in page) Assert.That(Get(entry, "field") as UnityEngine.Object, Is.Not.Null);
            }
        }

        [Test]
        public void RiskTableHasAllSixDropdownReferencesAndExpectedIndices()
        {
            IList entries = (IList)Get(Only("RiskTable"), "dropdowns");
            Assert.That(entries.Count, Is.EqualTo(6));
            Assert.That(entries.Cast<object>().Select(e => (int)Get(e, "correctIndex")), Is.EqualTo(new[] { 1, 2, 1, 1, 1, 1 }));
            foreach (object entry in entries) Assert.That(Get(entry, "dropdown") as UnityEngine.Object, Is.Not.Null,
                "Null dropdowns are skipped by the validator and can produce false success");
        }

        [Test]
        public void GeneratedPuzzlesHaveCorrectDimensionsAndCellPrefabs()
        {
            foreach (string type in new[] { "CrosswordManager", "WordSearchManager" })
            {
                Component manager = Only(type);
                Assert.That(Get(manager, "gridSize"), Is.EqualTo(type == "CrosswordManager" ? 13 : 21));
                var prefab = (GameObject)Get(manager, "cellPrefab");
                Assert.That(prefab, Is.Not.Null);
                Assert.That(prefab.GetComponent(RuntimeType(type == "CrosswordManager" ? "CrosswordCell" : "LetterCell")), Is.Not.Null);
            }
        }

        [Test]
        public void SyringeEntryHasBothChestAndDedicatedTrigger()
        {
            Component state = Only("PuzzleStateManager");
            Assert.That(Get(state, "puzzle10SyringeTrigger") as Component, Is.Not.Null);
            Assert.That(Get(state, "puzzle10ChestLid") as Component, Is.Not.Null);
            Assert.That(Get(Only("InsulinPuzzleManager"), "correctSyringeIndex"), Is.EqualTo(1));
        }

        [Test]
        public void GuidanceHasExactNamedRoutesAndNoMissingWaypoints()
        {
            IList paths = (IList)Get(Only("ObjectivePointer"), "paths");
            string[] names = { "Canteen", "Physician Room", "Pharmacy", "Nurse Education Room", "Morturary" };
            int[] counts = { 7, 4, 8, 6, 6 };
            for (int i = 0; i < names.Length; i++)
            {
                object[] matches = paths.Cast<object>().Where(p => (string)Get(p, "id") == names[i]).ToArray();
                Assert.That(matches.Length, Is.EqualTo(1), names[i]);
                var waypoints = (Transform[])Get(matches[0], "waypoints");
                Assert.That(waypoints.Length, Is.EqualTo(counts[i]), names[i]);
                Assert.That(waypoints.All(w => w != null), Is.True, names[i]);
            }
        }

        [Test]
        public void KeypadCombinationsMatchClueRewards()
        {
            Assert.That(All(game, "NavKeypad.Keypad").Select(k => (int)Get(k, "keypadCombo")),
                Is.EquivalentTo(new[] { 325, 154, 477, 28, 135 }));
        }

        [Test]
        public void EndingExitsLocallyAndOpeningAdvancesToFirstPuzzle()
        {
            Component[] comics = All(game, "SlideshowUI");
            Component[] endings = comics.Where(c => (bool)Get(c, "leaveRoomAndLoadLobbyOnClose")).ToArray();
            Assert.That(endings.Length, Is.EqualTo(1));
            Assert.That(Get(endings[0], "requireAllPlayersToClickClose"), Is.False);
            Assert.That(((IList)Get(endings[0], "slides")).Count, Is.EqualTo(2));
            Assert.That(Get(endings[0], "lobbySceneName"), Is.EqualTo("Lobby"));
            Component[] openings = comics.Where(c => (bool)Get(c, "advancePuzzleStageOnClose")).ToArray();
            Assert.That(openings.Length, Is.EqualTo(1));
            Assert.That(System.Convert.ToInt32(Get(openings[0], "closeTargetStage")), Is.EqualTo(1));
            Assert.That(((IList)Get(openings[0], "slides")).Count, Is.EqualTo(9));
        }

        [Test]
        public void ComicsUseRegularClueCloseButton()
        {
            object close = Get(Only("ClueUI"), "closeButton");
            Assert.That(close as Button, Is.Not.Null);
            foreach (Component comic in All(game, "SlideshowUI"))
                Assert.That(Get(comic, "closeButton"), Is.SameAs(close), comic.name);
        }

        [Test]
        public void AvatarsAndSpawnPointsAreAssignedInRoleOrder()
        {
            Component manager = Only("GameManager");
            var prefabs = (GameObject[])Get(manager, "characterPrefabs");
            Assert.That(prefabs.Select(p => p.name), Is.EqualTo(new[] { "Doctor", "Nurse", "Therapist" }));
            Assert.That(((Transform[])Get(manager, "spawnPoints")).Length, Is.EqualTo(4));
            foreach (GameObject prefab in prefabs)
            {
                Assert.That(prefab.GetComponent(RuntimeType("PlayerController")), Is.Not.Null);
                Assert.That(prefab.GetComponent<CharacterController>(), Is.Not.Null);
                Assert.That(prefab.CompareTag("Player"), Is.True);
            }
        }

        [Test]
        public void NoMissingMonoBehaviourScriptsInEnabledScenes()
        {
            foreach (Scene scene in new[] { game, lobby })
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero,
                            scene.name + "/" + child.name);
        }

        [Test]
        public void RoomListHasLayoutToPreventOverlappingEntries()
        {
            foreach (Component join in All(lobby, "JoinRoomManager"))
            {
                object content = Get(join, "contentPanel");
                GameObject go = content is GameObject obj ? obj : ((Component)content).gameObject;
                Assert.That(go.GetComponent<LayoutGroup>(), Is.Not.Null);
            }
        }
    }
}
