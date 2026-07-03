using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using MiniMart;
using MiniMart.Characters;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string ScreenshotPathKey = "PlayModeTest.ScreenshotPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = 90; // Wait 1.5 seconds

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            if (state == "WaitingForCompile")
            {
                EditorApplication.delayCall += () => {
                    SessionState.SetString(StateKey, "EnteringPlayMode");
                    EditorApplication.isPlaying = true;
                };
            }
            else if (state == "InPlayMode" && EditorApplication.isPlaying)
            {
                EditorApplication.update += Tick;
            }
        }

        private static int _frameCount = 0;
        private static bool _hasRun = false;
        private static Vector3 _playerStart;
        private static bool _capturedScreenshot = false;

        private static void Tick()
        {
            _frameCount++;
            
            if (_frameCount == 5)
            {
                var p = Object.FindAnyObjectByType<PlayerController>();
                if (p != null) _playerStart = p.transform.position;
            }

            if (_frameCount == 10)
            {
                // Simulate input
                var p = Object.FindAnyObjectByType<PlayerController>();
                if (p != null) p.transform.position += new Vector3(1, 0, 1);
            }

            if (_frameCount >= WaitFrames - 1 && !_capturedScreenshot)
            {
                ScreenCapture.CaptureScreenshot("PlayModeTest_Final.png");
                SessionState.SetString(ScreenshotPathKey, "PlayModeTest_Final.png");
                _capturedScreenshot = true;
                return;
            }

            if (_frameCount < WaitFrames) return;
            if (_hasRun) return;
            _hasRun = true;

            string resultJson = GetResult();
            SessionState.SetString(ResultKey, resultJson);
            SessionState.SetString(StateKey, "Done");
            Debug.Log(SentinelLog);
            EditorApplication.isPlaying = false;
        }

        private static string GetResult()
        {
            var p = Object.FindAnyObjectByType<PlayerController>();
            bool moved = p != null && (p.transform.position - _playerStart).sqrMagnitude > 0.1f;
            
            var characters = Object.FindObjectsByType<CharacterBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            bool hasNPCs = characters.Length > 1;

            var res = new TestResult {
                success = moved && hasNPCs,
                playerMoved = moved,
                npcCount = characters.Length,
                screenshotPath = "PlayModeTest_Final.png"
            };
            return JsonUtility.ToJson(res);
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath)) AssetDatabase.DeleteAsset(scriptPath);
            SessionState.EraseString(StateKey);
        }

        [System.Serializable]
        private class TestResult {
            public bool success;
            public bool playerMoved;
            public int npcCount;
            public string screenshotPath;
        }
    }
}