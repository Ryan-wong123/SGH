using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SGH.Tests
{
    [InitializeOnLoad]
    public static class RunGameTests
    {
        private static TestRunnerApi runner;
        private const string PendingRun = "SGH.Tests.PendingRun";

        static RunGameTests()
        {
            // GameCI v4.3.2 enables instrumentation on launch. Disable it before
            // entering Play Mode when requested by the CI fixture workflow.
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sgh-disable-coverage") >= 0)
                UnityEngine.TestTools.Coverage.enabled = false;

            // Play Mode reloads the scripting domain. Register again after reload,
            // and keep only the pending result filename in editor SessionState.
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new Results());
        }

        [MenuItem("SGH/Tests/Run Edit Mode suite")]
        public static void EditMode() { Run(TestMode.EditMode); }

        [MenuItem("SGH/Tests/Run Play Mode suite")]
        public static void PlayMode() { Run(TestMode.PlayMode); }

        [MenuItem("SGH/Tests/Run fixed-issue Edit Mode regressions")]
        public static void EditDiagnostics() { Run(TestMode.EditMode, true); }

        [MenuItem("SGH/Tests/Run fixed-issue Play Mode regressions")]
        public static void PlayDiagnostics() { Run(TestMode.PlayMode, true); }

        // Also callable with Unity -runTests -testPlatform EditMode/PlayMode.
        private static void Run(TestMode mode, bool diagnostics = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                Debug.LogWarning("Wait until Unity is idle in Edit Mode before running SGH tests.");
                return;
            }
            SessionState.SetString(PendingRun, "sgh-" + mode + (diagnostics ? "-fixes" : "") + ".xml");
            runner.Execute(new ExecutionSettings(new Filter { testMode = mode,
                categoryNames = new[] { diagnostics ? "RegressionFixes" : "!KnownGap" }, assemblyNames = new[]
            {
                mode == TestMode.EditMode ? "SGH.Tests.EditMode" : "SGH.Tests.PlayMode"
            } }));
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                string filename = SessionState.GetString(PendingRun, "");
                if (string.IsNullOrEmpty(filename)) return;
                SessionState.EraseString(PendingRun);
                string directory = Path.GetFullPath("TestResults");
                Directory.CreateDirectory(directory);
                TestRunnerApi.SaveResultToFile(result, Path.Combine(directory, filename));
                Debug.Log($"SGH tests: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped. Results: {directory}");
            }
        }
    }
}
