using UnityEditor;
using UnityEngine;

namespace MiniMart.EditorTools
{
    /// <summary>
    /// Marker-file remote control for unattended QA, complementing the TesterBot
    /// markers (Logs/testerbot.enabled / .freshrun). External tooling can't always
    /// reach the editor (Unity MCP gets revoked by plan entitlement; UI automation
    /// is fragile), so the shell drops a file and the editor obeys:
    ///
    ///   Logs/autoplay.marker  → enter Play mode (marker consumed)
    ///   Logs/autostop.marker  → exit Play mode  (marker consumed)
    ///
    /// Editor-only; ships nothing into builds.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoPlayMarker
    {
        private static double nextPoll;

        static AutoPlayMarker()
        {
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 2.0;   // cheap: every 2s

            string logs = System.IO.Path.GetFullPath(Application.dataPath + "/../Logs");
            string play = System.IO.Path.Combine(logs, "autoplay.marker");
            string stop = System.IO.Path.Combine(logs, "autostop.marker");

            // Never enter play with a compile pending/running: the session would
            // boot OLD code and any newly-added marker hooks silently no-op
            // (cost us a full experiment round once).
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            // iOS build request: run the iOS player build from THIS licensed
            // interactive editor (batchmode can't get a license handshake). The
            // build blocks the editor for its duration and writes its verdict to
            // Logs/ios_build_result.txt.
            string iosbuild = System.IO.Path.Combine(logs, "iosbuild.marker");
            if (!EditorApplication.isPlaying && System.IO.File.Exists(iosbuild))
            {
                System.IO.File.Delete(iosbuild);
                Debug.Log("[AutoPlayMarker] iosbuild.marker consumed — building iOS player.");
                MiniMart.EditorTools.CIBuild.BuildIOSInteractive();
                return;
            }

            if (!EditorApplication.isPlaying && System.IO.File.Exists(play))
            {
                System.IO.File.Delete(play);
                Debug.Log("[AutoPlayMarker] autoplay.marker consumed — entering Play mode.");
                EditorApplication.EnterPlaymode();
            }
            else if (EditorApplication.isPlaying && System.IO.File.Exists(stop))
            {
                System.IO.File.Delete(stop);
                Debug.Log("[AutoPlayMarker] autostop.marker consumed — exiting Play mode.");
                EditorApplication.ExitPlaymode();
            }
        }
    }
}
