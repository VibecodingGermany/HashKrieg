using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nova.Editor
{
    /// <summary>
    /// Command-line player build entry points (G0-B platform baseline).
    /// Usage:
    ///   Unity -batchmode -projectPath <repo> -executeMethod Nova.Editor.BuildScript.BuildWindows64 -quit
    ///   Unity -batchmode -projectPath <repo> -executeMethod Nova.Editor.BuildScript.BuildMacOSArm64 -quit
    ///   Unity -batchmode -projectPath <repo> -executeMethod Nova.Editor.BuildScript.BuildLinux64 -quit
    /// The scene list always comes from EditorBuildSettings; the build fails
    /// closed when no enabled scene is configured.
    /// </summary>
    public static class BuildScript
    {
        private const string CompanyName = "Project Nova";
        private const string ProductName = "Project Nova";

        public static void BuildWindows64()
        {
            Build(
                BuildTarget.StandaloneWindows64,
                "Builds/Windows64/ProjectNova.exe");
        }

        public static void BuildMacOSArm64()
        {
            Build(
                BuildTarget.StandaloneOSX,
                "Builds/MacOSArm64/ProjectNova.app");
        }

        public static void BuildLinux64()
        {
            Build(
                BuildTarget.StandaloneLinux64,
                "Builds/Linux64/ProjectNova.x86_64");
        }

        private static void Build(BuildTarget target, string locationPathName)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "EditorBuildSettings contains no enabled scene; cannot build.");
            }

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                locationPathName = locationPathName,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Build for {target} failed: {report.summary.result} " +
                    $"({report.summary.totalErrors} errors)");
            }

            Debug.Log(
                $"Build for {target} succeeded: {locationPathName} " +
                $"({report.summary.totalSize} bytes)");
        }
    }
}
