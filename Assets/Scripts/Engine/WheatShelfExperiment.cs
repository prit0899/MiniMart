#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using MiniMart.Characters;

namespace MiniMart.Engine
{
    /// <summary>
    /// Marker-driven QA experiment (owner repro): "wheat shelf is filled MAX,
    /// buyers roaming, no one takes even one".
    ///
    /// Trigger: Logs/wheattest.marker at scene load (spawned by SceneBootstrapper).
    /// Protocol:
    ///   t=20s   let the store warm up and buyers stream in, then:
    ///           wheat shelf -> MAX, every other shelf -> 0, wheat storage -> 0
    ///           (so the shelver can't refill it and every take is a BUYER take)
    ///   t=20-140s  sample the shelf every 5s to Logs/wheattest_result.txt
    ///   t=140s  verdict line + autostop.marker (play exits itself)
    /// </summary>
    public class WheatShelfExperiment : MonoBehaviour
    {
        private ShopShelf wheat;
        private int startCount = -1;
        private bool armed, done;
        private float nextSample;
        private readonly List<string> log = new List<string>();

        private string OutPath =>
            System.IO.Path.GetFullPath(Application.dataPath + "/../Logs/wheattest_result.txt");

        private void Update()
        {
            float t = Time.timeSinceLevelLoad;

            if (!armed && t >= 20f)
            {
                armed = true;
                var inv = GameManager.Instance?.Inventory;
                foreach (var s in FindObjectsByType<ShopShelf>(FindObjectsSortMode.None))
                {
                    if (!s.gameObject.activeInHierarchy) continue;
                    if (s.Item == Core.ItemType.Wheat) { wheat = s; s.Count = s.Capacity; }
                    else s.Count = 0;
                }
                if (inv != null)
                {
                    // Zero the wheat storage so the shelver cannot top the shelf up:
                    // every unit that leaves the shelf was taken by a BUYER.
                    while (inv.CountOf(Core.ItemType.Wheat) > 0)
                        inv.Withdraw(Core.ItemType.Wheat, 1);
                }
                if (wheat == null) { Finish("NO ACTIVE WHEAT SHELF — experiment invalid"); return; }
                startCount = wheat.Count;
                log.Add($"SETUP t={t:F0}s wheat={startCount}/{wheat.Capacity} others=0 storage(wheat)=0 " +
                        $"buyers={FindObjectsByType<MiniMart.AI.Buyer>(FindObjectsSortMode.None).Length}");
                Flush();
            }

            if (!armed || done || wheat == null) return;

            if (t >= nextSample)
            {
                nextSample = t + 5f;
                int buyers = FindObjectsByType<MiniMart.AI.Buyer>(FindObjectsSortMode.None).Length;
                log.Add($"t={t:F0}s wheat={wheat.Count}/{wheat.Capacity} taken={startCount - wheat.Count} buyers={buyers}");
                Flush();
            }

            if (t >= 140f)
            {
                int taken = startCount - wheat.Count;
                Finish(taken > 0
                    ? $"PASS — buyers took {taken} wheat from the MAX shelf in 120 sim-s"
                    : "FAIL — zero wheat taken from a MAX shelf with buyers present");
            }
        }

        private void Finish(string verdict)
        {
            done = true;
            log.Add("VERDICT: " + verdict);
            Flush();
            System.IO.File.WriteAllText(
                System.IO.Path.GetFullPath(Application.dataPath + "/../Logs/autostop.marker"), "");
        }

        private void Flush()
        {
            try { System.IO.File.WriteAllLines(OutPath, log); } catch { }
        }
    }
}
#endif
