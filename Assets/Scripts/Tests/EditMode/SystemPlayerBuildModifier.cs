using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.TestTools;

[assembly: TestPlayerBuildModifier(typeof(SGH.Tests.SystemPlayerBuildModifier))]

namespace SGH.Tests
{
    public class SystemPlayerBuildModifier : ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions options)
        {
            // UTF can include disabled stale build-list paths; preserve its generated test scene but omit nonexistent paths.
            options.scenes = options.scenes.Where(File.Exists)
                .Concat(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)).Distinct().ToArray();
            // System tests are built-player smoke tests, not IL2CPP qualification.
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) != ScriptingImplementation.Mono2x)
                throw new System.InvalidOperationException("The reflective system smoke suite requires the project's Mono standalone backend.");
            return options;
        }
    }
}
