using System.Collections.Generic;
using UnityEngine;
using MiniMart.Characters;
using MiniMart.Production;

namespace MiniMart.Engine
{
    /// <summary>
    /// Plays the game like a human tester: walks via the same tap-to-move
    /// pathfinding a player uses, harvests whatever is ripe, stocks shelves by
    /// proximity (PlayerInteraction does the work), collects money stacks,
    /// stands on purchase pads until they complete, and rides the travel pad
    /// to MegaMart. Never teleports, never grants cash, never calls dev APIs —
    /// if the bot stalls, a player stalls.
    ///
    /// Everything notable lands in the static Log with a sim-time stamp so a
    /// probe can read the whole diary after stepping time forward.
    /// </summary>
    public class TesterBot : MonoBehaviour
    {
        public static readonly List<string> Log = new List<string>();
        public static void Note(string s) => Log.Add($"[{Time.timeSinceLevelLoad:F0}s] {s}");

        // Unattended-run support: the diary is flushed to Logs/testerbot_run.txt
        // every few seconds so an external QA process can follow the run without
        // any editor scripting hooks.
        private static string DiaryPath =>
            System.IO.Path.GetFullPath(Application.dataPath + "/../Logs/testerbot_run.txt");
        private float nextFlushAt;

        private void FlushDiary(MiniMart.GameManager gm)
        {
            try
            {
                string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                // Production-chain snapshot: with the eat-tomato hen, eggs (and so the
                // stove and oven) starve if nobody feeds her — surface the whole chain
                // in the STATUS line so a starvation stall is visible in the diary.
                string chain = "";
                var hen = FindAnyObjectByType<Production.HenCoop>();
                if (hen != null) chain += $" hen[t{hen.TomatoQueued}/e{hen.EggReady}]";
                foreach (var m in FindObjectsByType<Production.Machine>(FindObjectsSortMode.None))
                {
                    if (m.Type == Catalog.MachineType.Oven)
                        chain += $" oven[{m.InputQueued}+{m.InputQueued2}->{m.OutputReady}]";
                    else if (m.Type == Catalog.MachineType.Stove)
                        chain += $" stove[{m.InputQueued}->{m.OutputReady}]";
                    else if (m.Type == Catalog.MachineType.Mill)
                        chain += $" mill[{m.InputQueued}->{m.OutputReady}]";
                    else if (m.Type == Catalog.MachineType.Blender)
                        chain += $" blend[{m.InputQueued}->{m.OutputReady}]";
                }
                var inv = gm.Inventory;
                if (inv != null)
                    chain += $" store[tom{inv.CountOf(Core.ItemType.Tomato)} egg{inv.CountOf(Core.ItemType.Egg)} whe{inv.CountOf(Core.ItemType.Wheat)} flr{inv.CountOf(Core.ItemType.WheatFlour)}]";

                // Where is everyone / is money reaching the floor? (Static-world
                // stalls need position evidence, not just economy counters.)
                if (player != null)
                {
                    Vector3 pp = player.transform.position;
                    chain += $" bot({pp.x:F0},{pp.z:F0})carry{player.CarryCount}";
                }
                chain += $" cash$={FindObjectsByType<MoneyStack>(FindObjectsSortMode.None).Length}";
                chain += $" buyers={FindObjectsByType<MiniMart.AI.Buyer>(FindObjectsSortMode.None).Length}";
                string shelves = "";
                foreach (var sh in FindObjectsByType<ShopShelf>(FindObjectsSortMode.None))
                    if (sh.gameObject.activeInHierarchy) shelves += $"{sh.Item.ToString().Substring(0, 3)}{sh.Count} ";
                chain += $" shelf[{shelves.TrimEnd()}]";

                var lines = new List<string>(Log.Count + 1)
                {
                    $"STATUS L{gm.StoreLevel} ${gm.Economy.PlayerCash:F0} xp={gm.StoreXp}/{gm.XpToNextLevel} scene={scene} t={Time.timeSinceLevelLoad:F0}s real={Time.realtimeSinceStartup:F0}s{chain}"
                };
                lines.AddRange(Log);
                System.IO.File.WriteAllLines(DiaryPath, lines);
            }
            catch { /* diary must never break the run */ }
        }

        private PlayerController player;
        private float decideAt;
        private float lastCash;
        private float lastProgressAt;
        private int lastLevel;
        private Vector3 holdPos;
        private float holdUntil;

        private void Start()
        {
            // Survive travel between marts, like the player's thumbs do.
            DontDestroyOnLoad(gameObject);
            // Accelerated soak: all game logic is dt-based, so 3x wall speed
            // changes nothing about the simulation, only how long QA waits.
            Time.timeScale = 3f;
            player = FindAnyObjectByType<PlayerController>();
            var gm = MiniMart.GameManager.Instance;
            lastCash = gm != null ? gm.Economy.PlayerCash : 0f;
            lastLevel = gm != null ? gm.StoreLevel : 1;
            lastProgressAt = Time.timeSinceLevelLoad;
            Note($"BOT START scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} L{lastLevel} ${lastCash:F0}");
        }

        private void Update()
        {
            var gm = MiniMart.GameManager.Instance;
            if (player == null) // destroyed by a scene load - reattach
            {
                player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    holdUntil = 0f; decideAt = 0f;
                    lastProgressAt = Time.timeSinceLevelLoad;   // per-scene clock reset
                    Note($"BOT REATTACH scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                }
            }
            if (gm == null || player == null) return;

            if (Time.unscaledTime >= nextFlushAt)
            {
                nextFlushAt = Time.unscaledTime + 5f;
                FlushDiary(gm);
            }
            CheckWedged();

            // Thief visibility for the QA report: note spawns and outcomes.
            int thievesNow = FindObjectsByType<MiniMart.AI.Thief>(FindObjectsSortMode.None).Length;
            if (thievesNow > lastThieves) Note($"THIEF spawned (active={thievesNow})");
            else if (thievesNow < lastThieves) Note($"THIEF gone (caught or escaped; active={thievesNow})");
            lastThieves = thievesNow;

            // Milestones + stall detection.
            if (gm.StoreLevel != lastLevel)
            {
                Note($"LEVEL {lastLevel} -> {gm.StoreLevel} (cash ${gm.Economy.PlayerCash:F0})");
                lastLevel = gm.StoreLevel;
                lastProgressAt = Time.timeSinceLevelLoad;
            }
            if (Mathf.Abs(gm.Economy.PlayerCash - lastCash) > 0.5f)
            {
                lastCash = gm.Economy.PlayerCash;
                lastProgressAt = Time.timeSinceLevelLoad;
            }
            if (Time.timeSinceLevelLoad - lastProgressAt > 90f)
            {
                Note($"STALL 90s: no cash/level progress at L{gm.StoreLevel} ${gm.Economy.PlayerCash:F0} carry={player.CarryCount}");
                lastProgressAt = Time.timeSinceLevelLoad; // report once per stall window
            }

            // While "standing on a pad", stay put (dwell mechanics need stillness).
            if (Time.timeSinceLevelLoad < holdUntil)
            {
                player.StopCurrentTask();
                player.transform.position = holdPos; // hold still against drift
                return;
            }

            if (Time.timeSinceLevelLoad < decideAt) return;
            decideAt = Time.timeSinceLevelLoad + 0.6f;

            // Priority 1: money on the floor.
            var stack = FindAnyObjectByType<MoneyStack>();
            if (stack != null) { Go(stack.transform.position); return; }

            // Priority 2: buy the cheapest visible, affordable pad.
            PurchasePad pad = null;
            foreach (var p in FindObjectsByType<PurchasePad>(FindObjectsSortMode.None))
            {
                // Check MinLevel directly: on the first frame after a scene load
                // the pads haven't hidden their locked children yet, so the
                // visibility check alone briefly sees every pad.
                if (p.MinLevel > gm.StoreLevel) continue;
                if (p.transform.childCount == 0 || !p.transform.GetChild(0).gameObject.activeSelf) continue;
                if (p.Cost > gm.Economy.PlayerCash) continue;
                if (pad == null || p.Cost < pad.Cost) pad = p;
            }
            if (pad != null)
            {
                Note($"BUYING pad '{pad.Label}' (${pad.Cost:F0})");
                Hold(pad.transform.position, 6f + pad.Cost / 40f);
                return;
            }

            // Priority 2b: with no level-unlock pad affordable right now, spend
            // spare cash on the cheapest player upgrade (carry / speed / crop) —
            // a real player does this, and it exercises the upgrade loop so we
            // can confirm the pads actually help. Upgrades are finite (5 levels
            // each), so this naturally stops once everything is maxed.
            UpgradePad up = null;
            foreach (var u in FindObjectsByType<UpgradePad>(FindObjectsSortMode.None))
            {
                int c = u.NextCost;
                if (c < 0 || c > gm.Economy.PlayerCash) continue;
                if (up == null || u.NextCost < up.NextCost) up = u;
            }
            if (up != null)
            {
                Note($"UPGRADING '{up.name}' (${up.NextCost})");
                Hold(up.transform.position, 3f);
                return;
            }

            // Priority 3: at L6+ in Mart 1 with nothing left to buy here, take the
            // savings to MegaMart — that's where every remaining unlock lives.
            var travel = FindAnyObjectByType<SceneTransition>();
            bool inMart1 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game";
            // Fare is $500; bring seed money too — travelling at ~$500 strands the
            // bot (and a player) in a mart with zero income sources ($9, nothing
            // to buy, nothing to sell — observed live).
            if (travel != null && inMart1 && gm.StoreLevel >= 6 && gm.Economy.PlayerCash >= 660f)
            {
                Note("TRAVELLING to MegaMart");
                Hold(travel.transform.position, 4f);
                return;
            }

            // Priority 3b: stranded in MegaMart — broke, nothing carried, nothing
            // ripe, no money on the floor. A real player takes the return pad back
            // to Mart 1 to earn; so does the bot now.
            if (travel != null && !inMart1 && gm.Economy.PlayerCash < 60f
                && player.CarryCount == 0)
            {
                Note("RETURNING to Mart 1 to earn (broke in MegaMart)");
                Hold(travel.transform.position, 4f);
                return;
            }

            // Priority 4: carrying something -> stand at its shelf (auto-stocks).
            if (player.CarryCount > 0)
            {
                var pi = player.GetComponent<PlayerInteraction>();
                var items = pi != null ? pi.GetCarriedItems() : null;
                if (items != null && items.Count > 0)
                {
                    var shelfGO = GameObject.Find($"Shelf_{items[0]}");
                    if (shelfGO != null && shelfGO.activeInHierarchy) { Hold(shelfGO.transform.position, 3f); return; }
                    // No shelf for it (raw ingredient) -> its machine/trough will
                    // take it by proximity; try the matching processor.
                    string target = items[0] switch
                    {
                        Core.ItemType.Wheat => "WheatMill",
                        Core.ItemType.Milk  => "Dairy",
                        Core.ItemType.Herb  => "LeafProcessor",
                        Core.ItemType.Corn  => "CornProcessor",
                        _ => null,
                    };
                    if (target != null)
                    {
                        var m = GameObject.Find(target);
                        if (m != null && m.activeInHierarchy) { Hold(m.transform.position, 3f); return; }
                    }
                }
            }

            // Priority 5: harvest the richest ripe source that's active.
            Vector3? best = null; int bestRipe = 0; string bestName = "";
            void Consider(string name, Component c, int ripe)
            {
                if (c == null || !c.gameObject.activeInHierarchy || ripe <= bestRipe) return;
                best = c.transform.position; bestRipe = ripe; bestName = name;
            }
            Consider("tomato", FindAnyObjectByType<TomatoFarm>(), FindAnyObjectByType<TomatoFarm>()?.TotalRipe() ?? 0);
            Consider("wheat",  FindAnyObjectByType<WheatFarm>(),  FindAnyObjectByType<WheatFarm>()?.ReadyCount() ?? 0);
            Consider("egg",    FindAnyObjectByType<HenCoop>(),    FindAnyObjectByType<HenCoop>()?.TotalEggsReady() ?? 0);
            Consider("corn",   FindAnyObjectByType<CornField>(),  FindAnyObjectByType<CornField>()?.TotalRipe() ?? 0);
            Consider("apple",  FindAnyObjectByType<AppleOrchard>(), FindAnyObjectByType<AppleOrchard>()?.TotalRipe() ?? 0);
            Consider("herb",   FindAnyObjectByType<HerbPatch>(),  FindAnyObjectByType<HerbPatch>()?.TotalRipe() ?? 0);
            Consider("milk",   FindAnyObjectByType<CowPen>(),     FindAnyObjectByType<CowPen>()?.TotalMilkReady() ?? 0);
            if (best.HasValue) { Hold(best.Value, 2.5f); return; }

            // Nothing to do: collect from any ready machine output by loitering
            // at the processing strip, else wander to the store to man the till.
            var counter = gm.Counters != null && gm.Counters.Count > 0 ? gm.Counters[0] : null;
            if (counter != null) Go(counter.transform.position);
        }

        private void OnDestroy()
        {
            // Leaving play mode must never strand the editor at 3x.
            Time.timeScale = 1f;
        }

        private int lastThieves;
        private Vector3 lastPos;
        private float lastMoveAt;

        /// <summary>Physically-stuck detector: we have somewhere to be but haven't
        /// moved half a unit in 45s. Logs once per wedge window with coordinates.</summary>
        private void CheckWedged()
        {
            if (player == null) return;
            Vector3 pp = player.transform.position;
            if ((pp - lastPos).sqrMagnitude > 0.25f)
            {
                lastPos = pp;
                lastMoveAt = Time.timeSinceLevelLoad;
                return;
            }
            if (player.HasMoveTarget && Time.timeSinceLevelLoad - lastMoveAt > 45f)
            {
                Note($"BOT_WEDGED at ({pp.x:F1},{pp.z:F1}) carry={player.CarryCount} — has target but no movement 45s");
                lastMoveAt = Time.timeSinceLevelLoad;
            }
        }

        private void Go(Vector3 pos) => player.SetTarget(pos);

        private void Hold(Vector3 pos, float seconds)
        {
            player.SetTarget(pos);
            // Approximate walk time then hold still on the spot.
            float walkTime = Vector3.Distance(player.transform.position, pos) / Mathf.Max(1f, player.CurrentSpeed);
            holdPos = pos;
            holdUntil = Time.timeSinceLevelLoad + walkTime + seconds;
        }
    }
}
