#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiniMart.EditorTools
{
    /// <summary>
    /// Headless iOS build entry point for device-readiness validation. Run from a
    /// shell (editor must be closed to release the project lock):
    ///
    ///   Unity -batchmode -quit -projectPath . -buildTarget iOS \
    ///         -executeMethod MiniMart.EditorTools.CIBuild.BuildIOS \
    ///         -logFile Logs/ios_build.log
    ///
    /// Produces an Xcode project under Build/iOS. Its whole point is to surface
    /// IL2CPP / AOT / platform-compile errors that the editor's Mono pass never
    /// sees — the real gap between "compiles in the editor" and "builds for the
    /// device". It does NOT sign or deploy (that needs the owner's Apple account
    /// + hardware).
    /// </summary>
    public static class CIBuild
    {
        private static readonly string[] Scenes = { "Assets/Game.unity", "Assets/MegaMart.unity" };
        private const string OutDir = "Build/iOS";

        /// <summary>Batchmode entry — exits the process with the build's status.</summary>
        public static void BuildIOS()
        {
            var result = RunBuild();
            EditorApplication.Exit(result ? 0 : 1);
        }

        /// <summary>
        /// Interactive-editor entry (triggered by Logs/iosbuild.marker). Runs the
        /// SAME build from inside the already-licensed editor — which sidesteps the
        /// headless license handshake that stops batchmode — and writes the verdict
        /// to Logs/ios_build_result.txt for an external monitor. Never exits the
        /// editor.
        /// </summary>
        public static void BuildIOSInteractive() => RunBuild();

        private static bool RunBuild()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = OutDir,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary s = report.summary;
            string head = $"[CIBuild] iOS build result={s.result} " +
                          $"errors={s.totalErrors} warnings={s.totalWarnings} " +
                          $"sizeBytes={s.totalSize} time={s.totalTime}";
            Debug.Log(head);

            var lines = new System.Collections.Generic.List<string> { head };
            if (s.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                    foreach (var msg in step.messages)
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            string e = $"[CIBuild] {step.name}: {msg.content}";
                            Debug.LogError(e);
                            lines.Add(e);
                        }
            }
            try
            {
                System.IO.File.WriteAllLines(
                    System.IO.Path.GetFullPath(Application.dataPath + "/../Logs/ios_build_result.txt"),
                    lines);
            }
            catch { /* result file is best-effort */ }
            return s.result == BuildResult.Succeeded;
        }
    }
}
#endif
