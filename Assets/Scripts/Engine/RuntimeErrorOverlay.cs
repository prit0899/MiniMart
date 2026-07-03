using UnityEngine;
using UnityEngine.UI;

namespace MiniMart.Engine
{
    /// <summary>
    /// On-device error readout. Counts every Error/Exception/Assert the game logs and shows
    /// the count plus the latest message at the bottom of the screen, so device runs can be
    /// diagnosed without an attached Xcode/adb console.
    /// </summary>
    public class RuntimeErrorOverlay : MonoBehaviour
    {
        private Text label;
        private int errorCount;
        private string lastError = "";
        private bool dirty;

        public void Bind(Text target) => label = target;

        private void OnEnable()  => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        // logMessageReceived can fire off the main thread; only note the error here
        // and touch UI objects from Update.
        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            errorCount++;
            lastError = condition;
            dirty = true;
        }

        private void Update()
        {
            if (!dirty || label == null) return;
            dirty = false;

            string msg = lastError.Length > 140 ? lastError.Substring(0, 140) : lastError;
            label.text = $"ERRORS {errorCount} | {msg}";
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
        }
    }
}
