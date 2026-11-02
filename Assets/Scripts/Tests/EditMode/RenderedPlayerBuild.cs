using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SGH.Tests
{
    public static class RenderedPlayerBuild
    {
        public static void Linux() => Build(BuildTarget.StandaloneLinux64, "TestResults/RenderedPlayer/SGH");
        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "TestResults/RenderedPlayer/SGH.exe");
        private static void Build(BuildTarget target, string output)
        {
            const string scenePath = "Assets/Scripts/Tests/System/RenderedBootstrap.unity";
            if (!File.Exists(scenePath))
            {
                var bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(bootstrap, scenePath);
            }
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath }.Concat(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)).ToArray(),
                target = target, locationPathName = output,
                options = BuildOptions.Development | BuildOptions.IncludeTestAssemblies
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Rendered automation build failed");
        }
    }
}
