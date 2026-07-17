#if UNITY_EDITOR
using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Marker-driven gameplay capture for the README (owner: "may can add
    /// gameplay video if possible"). Trigger: Logs/gifcapture.marker at scene
    /// load (spawned by SceneBootstrapper, marker consumed).
    ///
    /// Protocol: warm up 12s so the store is lively, then save a Game-view
    /// screenshot to Logs/frames/ every 0.5s for 80 frames (~40s of play),
    /// then drop Logs/autostop.marker. An external script assembles the GIF
    /// with PIL. Editor-only; ships nothing.
    /// </summary>
    public class GifCaptureSession : MonoBehaviour
    {
        private const int FrameCount = 80;
        private const float Interval = 0.5f;
        private const float WarmupSeconds = 12f;

        private int taken;
        private float nextShotAt = WarmupSeconds;
        private string dir;

        private void Start()
        {
            dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Logs/frames");
            System.IO.Directory.CreateDirectory(dir);
            foreach (var f in System.IO.Directory.GetFiles(dir, "*.png"))
                System.IO.File.Delete(f);
        }

        private void Update()
        {
            if (taken >= FrameCount) return;

            // The offline-earnings popup sits mid-screen until COLLECT is
            // pressed — a bot never presses UI, so a whole capture came back
            // with "WELCOME BACK!" over every frame. Dismiss it before shooting.
            if (Time.timeSinceLevelLoad < WarmupSeconds)
            {
                var go = GameObject.Find("CollectOfflineButton");
                var btn = go != null ? go.GetComponent<UnityEngine.UI.Button>() : null;
                if (btn != null) btn.onClick.Invoke();
            }

            if (Time.timeSinceLevelLoad < nextShotAt) return;
            nextShotAt = Time.timeSinceLevelLoad + Interval;
            ScreenCapture.CaptureScreenshot($"{dir}/frame_{taken:D4}.png", 1);
            taken++;
            if (taken >= FrameCount)
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.GetFullPath(Application.dataPath + "/../Logs/autostop.marker"), "");
            }
        }
    }
}
#endif
