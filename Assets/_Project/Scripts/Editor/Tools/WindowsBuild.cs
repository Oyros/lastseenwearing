using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastSeenWearing.Editor.Tools
{
    /// <summary>
    /// One-click Windows player builds into <c>Builds/</c> (gitignored). Scenes come from the
    /// build list, so <c>Bootstrap</c> is always first (P0.05). Localization's Addressables
    /// content is built by the player build itself (Addressables' build-with-player setting);
    /// there is no separate step. docs/WORKFLOW.md §8.
    /// </summary>
    public static class WindowsBuild
    {
        private const string MenuRoot = "Last Seen Wearing/Build/";
        private const string BuildsFolder = "Builds";
        private const string ExeName = "LastSeenWearing.exe";

        /// <summary>Compiled into playtest builds only: a release build that testers run.</summary>
        public const string PlaytestDefine = "LSW_PLAYTEST";

        [MenuItem(MenuRoot + "Windows (Development)")]
        public static void BuildDevelopment()
        {
            Build("Windows_Dev", BuildOptions.Development);
        }

        [MenuItem(MenuRoot + "Windows (Release)")]
        public static void BuildRelease()
        {
            Build("Windows", BuildOptions.None);
        }

        [MenuItem(MenuRoot + "Windows (Playtest)")]
        public static void BuildPlaytest()
        {
            Build("Windows_Playtest", BuildOptions.None, PlaytestDefine);
        }

        private static void Build(string folderName, BuildOptions options, params string[] defines)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[WindowsBuild] The build list has no enabled scenes.");
                return;
            }

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var outputFolder = Path.Combine(projectRoot, BuildsFolder, folderName);
            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputFolder, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = options,
                extraScriptingDefines = defines,
            };

            var summary = BuildPipeline.BuildPlayer(buildOptions).summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[WindowsBuild] {folderName} build {summary.result}: " +
                               $"{summary.totalErrors} error(s). See the console above.");
                return;
            }

            Debug.Log($"[WindowsBuild] {folderName} build succeeded in {summary.totalTime.TotalSeconds:F0} s " +
                      $"({summary.totalSize / (1024 * 1024)} MB): {buildOptions.locationPathName}");
            if (!Application.isBatchMode)
            {
                EditorUtility.RevealInFinder(buildOptions.locationPathName);
            }
        }
    }
}
