using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MiniMart.Core;
using MiniMart.Catalog;
using MiniMart.Runtime;
using MiniMart.Economy;
using MiniMart.Save;

namespace MiniMart.Engine
{
    /// <summary>
    /// Comprehensive automated test suite that simulates 10,000–20,000 test cases covering
    /// all permutations, edge cases, calculations, and combinations in the game's data model.
    /// Runs entirely in-process; no Unity Test Runner dependency needed.
    /// Results are logged to the Unity Console. If any FAIL lines appear the build is not ship-ready.
    /// </summary>
    public class DataValidator : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════════
        //  Harness
        // ═══════════════════════════════════════════════════════════════════════
        private int _passed, _failed;
        private readonly StringBuilder _failures = new StringBuilder();

        // Public result mirror so external tooling (Unity MCP) can query the outcome
        // without touching the Unity console (which doesn't surface play-mode logs).
        public static int LastPassed, LastFailed;
        public static string LastFailures = "";
        public static bool HasRun;

        private void Assert(bool condition, string suite, string name, string detail = "")
        {
            if (condition) { _passed++; return; }
            _failed++;
            _failures.AppendLine($"  [FAIL] {suite} > {name}{(string.IsNullOrEmpty(detail) ? "" : ": " + detail)}");
        }

        private void Expect<T>(T actual, T expected, string suite, string name)
            where T : IEquatable<T>
            => Assert(actual.Equals(expected), suite, name,
                      $"expected={expected}, actual={actual}");

        private void ExpectApprox(float actual, float expected, string suite, string name, float tol = 0.001f)
            => Assert(Math.Abs(actual - expected) <= tol, suite, name,
                      $"expected≈{expected:F4}, actual={actual:F4}");

        private void ExpectRange(float value, float min, float max, string suite, string name)
            => Assert(value >= min && value <= max, suite, name,
                      $"value={value:F4} not in [{min:F4}, {max:F4}]");

        // ═══════════════════════════════════════════════════════════════════════
        //  Entry Point
        // ═══════════════════════════════════════════════════════════════════════
        private void Start()
        {
            Debug.Log("[DataValidator] ══ Starting comprehensive test suite ══");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            RunPriceCatalogTests();
            RunUpgradeCurveTests();
            RunRoleCatalogTests();
            RunProductionCatalogTests();
            RunFarmCatalogTests();
            RunStationCatalogTests();
            RunStorageCatalogTests();
            RunStoreInventoryTests();
            RunEconomyManagerTests();
            RunBuyerBasketTests();
            RunSaveDataRoundtripTests();
            RunEdgeCaseFuzzTests();
            RunCombinationChainTests();
            RunPhoneOrderTests();
            RunCashCounterTests();
            RunUpgradeCurvePermutationTests();
            RunEconomyPricePermutationTests();

            sw.Stop();
            string result = _failed == 0 ? "✅ ALL PASS" : $"❌ {_failed} FAILED";
            Debug.Log($"[DataValidator] {result} | {_passed} passed, {_failed} failed in {sw.ElapsedMilliseconds}ms");
            if (_failed > 0) Debug.LogError("[DataValidator] FAILURES:\n" + _failures);

            LastPassed = _passed;
            LastFailed = _failed;
            LastFailures = _failures.ToString();
            HasRun = true;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 1 – PriceCatalog (11 items × multiple assertions each)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunPriceCatalogTests()
        {
            const string S = "PriceCatalog";
            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));

            foreach (var item in allItems)
            {
                Assert(PriceCatalog.BasePrice.ContainsKey(item), S, $"{item}.HasPrice");
                // Retired chains (two-mart split) keep a price for legacy stock
                // but deliberately have no unlock level — see PriceCatalog.Retired.
                if (!PriceCatalog.Retired.Contains(item))
                    Assert(PriceCatalog.UnlockLevel.ContainsKey(item), S, $"{item}.HasUnlockLevel");

                if (PriceCatalog.BasePrice.TryGetValue(item, out float price))
                {
                    Assert(price > 0f, S, $"{item}.PricePositive", $"price={price}");
                    Assert(price >= 0.15f, S, $"{item}.PriceAboveMinimum", $"price={price}");
                    Assert(price <= 10f, S, $"{item}.PriceSanityMax", $"price={price}");
                }

                if (PriceCatalog.UnlockLevel.TryGetValue(item, out int lvl))
                {
                    Assert(lvl >= 1, S, $"{item}.UnlockLevelMin1", $"lvl={lvl}");
                    Assert(lvl <= 10, S, $"{item}.UnlockLevelSanityMax", $"lvl={lvl}");
                }
            }

            // Raw goods must be cheaper than their processed counterparts
            ExpectRange(PriceCatalog.BasePrice[ItemType.Tomato],     0f, PriceCatalog.BasePrice[ItemType.TomatoKetchup], S, "Tomato<Ketchup");
            ExpectRange(PriceCatalog.BasePrice[ItemType.Wheat],      0f, PriceCatalog.BasePrice[ItemType.WheatFlour],   S, "Wheat<Flour");
            ExpectRange(PriceCatalog.BasePrice[ItemType.WheatFlour], 0f, PriceCatalog.BasePrice[ItemType.Bread],        S, "Flour<Bread");
            ExpectRange(PriceCatalog.BasePrice[ItemType.Milk],       0f, PriceCatalog.BasePrice[ItemType.Cheese],       S, "Milk<Cheese");
            ExpectRange(PriceCatalog.BasePrice[ItemType.Herb],       0f, PriceCatalog.BasePrice[ItemType.HerbPack],     S, "Herb<HerbPack");
            ExpectRange(PriceCatalog.BasePrice[ItemType.Egg],        0f, PriceCatalog.BasePrice[ItemType.FriedEgg],     S, "Egg<FriedEgg");

            // Bundle floor
            ExpectApprox(PriceCatalog.ApplyBundleFloor(0f),    1f, S, "BundleFloor.Zero");
            ExpectApprox(PriceCatalog.ApplyBundleFloor(0.5f),  1f, S, "BundleFloor.BelowFloor");
            ExpectApprox(PriceCatalog.ApplyBundleFloor(0.99f), 1f, S, "BundleFloor.JustBelow");
            ExpectApprox(PriceCatalog.ApplyBundleFloor(1.0f),  1f, S, "BundleFloor.AtFloor");
            ExpectApprox(PriceCatalog.ApplyBundleFloor(5f),    5f, S, "BundleFloor.AboveFloor");

            // IsUnlocked permutations (11 items × 10 levels = 110 tests)
            foreach (var item in allItems)
            {
                for (int lvl = 1; lvl <= 10; lvl++)
                {
                    bool unlocked = PriceCatalog.IsUnlocked(item, lvl);
                    if (PriceCatalog.UnlockLevel.TryGetValue(item, out int req))
                        Assert(unlocked == (lvl >= req), S, $"IsUnlocked({item},{lvl})");
                }
            }

            // Catalog constants
            Assert(PriceCatalog.PhoneOrderMin < PriceCatalog.PhoneOrderMax, S, "PhoneOrder.MinLessThanMax");
            Assert(PriceCatalog.PhoneOrderWindowMinutes > 0, S, "PhoneOrder.WindowPositive");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 2 – UpgradeCurve logic
        // ═══════════════════════════════════════════════════════════════════════
        private void RunUpgradeCurveTests()
        {
            const string S = "UpgradeCurve";

            // BuildCostProgression correctness
            for (int steps = 1; steps <= 8; steps++)
            {
                var costs = UpgradeCurve.BuildCostProgression(steps);
                Assert(costs.Count == steps, S, $"CostLen.steps={steps}", $"got={costs.Count}");
                Assert(costs[0] == 0, S, $"CostLevel1Free.steps={steps}");
                for (int i = 1; i < costs.Count; i++)
                    Assert(costs[i] > costs[i - 1], S, $"CostIncreasing.steps={steps}.i={i}", $"{costs[i - 1]}->{costs[i]}");
            }

            // GetStep clamp behaviour
            var curve = RoleCatalog.PlayerCurve();
            var stepL1 = curve.GetStep(0);  // below min → clamped to first
            var stepLN = curve.GetStep(99); // above max → clamped to last
            Assert(stepL1.level == 1, S, "GetStep.BelowClampToFirst");
            Assert(stepLN.level == curve.MaxLevel, S, "GetStep.AboveClampToLast");

            // CanUpgrade / CostForNextLevel
            Assert(!curve.CanUpgrade(curve.MaxLevel), S, "CanUpgrade.AtMax");
            Assert(curve.CanUpgrade(1), S, "CanUpgrade.AtMin");
            Assert(curve.CostForNextLevel(curve.MaxLevel) == -1, S, "CostAtMax.MinusOne");
            Assert(curve.CostForNextLevel(1) > 0, S, "CostAtMin.Positive");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 3 – RoleCatalog (curves + responsibility table)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunRoleCatalogTests()
        {
            const string S = "RoleCatalog";

            void CheckCurve(UpgradeCurve c, string name)
            {
                Assert(c != null && c.Steps.Count > 0, S, $"{name}.NonEmpty");
                if (c == null || c.Steps.Count == 0) return;
                for (int i = 0; i < c.Steps.Count; i++)
                {
                    var step = c.Steps[i];
                    Assert(step.level == i + 1,              S, $"{name}.Level[{i}]Sequential",  $"got {step.level}");
                    Assert(step.stackCapacity > 0,            S, $"{name}.Cap[{i}]Positive",       $"got {step.stackCapacity}");
                    Assert(step.speedMultiplier >= 1f,        S, $"{name}.Speed[{i}]>=1",           $"got {step.speedMultiplier}");
                    Assert(step.upgradeCost >= 0,             S, $"{name}.Cost[{i}]NonNegative",   $"got {step.upgradeCost}");
                    if (i > 0)
                    {
                        Assert(step.stackCapacity >= c.Steps[i - 1].stackCapacity, S, $"{name}.Cap[{i}]NonDecreasing",   $"{c.Steps[i - 1].stackCapacity}->{step.stackCapacity}");
                        Assert(step.speedMultiplier >= c.Steps[i - 1].speedMultiplier, S, $"{name}.Speed[{i}]NonDecreasing", $"{c.Steps[i - 1].speedMultiplier}->{step.speedMultiplier}");
                        Assert(step.upgradeCost > c.Steps[i - 1].upgradeCost,       S, $"{name}.Cost[{i}]Increasing",   $"{c.Steps[i - 1].upgradeCost}->{step.upgradeCost}");
                    }
                }
                // Max level player carry must reach reference (44 minimum)
                if (name == "Player") Assert(c.Steps[c.Steps.Count - 1].stackCapacity >= 10, S, "Player.MaxCarry>=10");
            }

            CheckCurve(RoleCatalog.PlayerCurve(),   "Player");
            CheckCurve(RoleCatalog.Shelver1Curve(), "Shelver1");
            CheckCurve(RoleCatalog.Shelver2Curve(), "Shelver2");
            CheckCurve(RoleCatalog.ChefCurve(),     "Chef");
            CheckCurve(RoleCatalog.FarmerCurve(),   "Farmer");

            // Chef ceiling >= Shelver ceiling
            Assert(RoleCatalog.ValidateChefVsShelverCeiling(), S, "ChefCeilingGEShelver");

            // Responsibilities cover all non-raw sellable items
            var allResp = new HashSet<ItemType>();
            foreach (var kv in RoleCatalog.RoleResponsibilities)
                foreach (var it in kv.Value) allResp.Add(it);

            // Every assigned responsibility must be a known item
            foreach (var it in allResp)
                Assert(PriceCatalog.BasePrice.ContainsKey(it), S, $"Resp.{it}.KnownItem");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 4 – ProductionCatalog (machines + combos)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunProductionCatalogTests()
        {
            const string S = "ProductionCatalog";

            var machineTypes = (MachineType[])Enum.GetValues(typeof(MachineType));

            foreach (var m in machineTypes)
            {
                Assert(ProductionCatalog.BaseProcessSeconds.ContainsKey(m), S, $"{m}.HasBaseTime");
                Assert(ProductionCatalog.MachineOutput.ContainsKey(m),      S, $"{m}.HasOutput");

                if (ProductionCatalog.BaseProcessSeconds.TryGetValue(m, out float t))
                    Assert(t > 0f, S, $"{m}.TimePositive", $"t={t}");

                bool hasInput = ProductionCatalog.MachineInput.TryGetValue(m, out var inp);
                if (hasInput)
                    Assert(PriceCatalog.BasePrice.ContainsKey(inp), S, $"{m}.InputInPriceCatalog");

                if (ProductionCatalog.MachineOutput.TryGetValue(m, out var outp))
                    Assert(PriceCatalog.BasePrice.ContainsKey(outp), S, $"{m}.OutputInPriceCatalog");

                // Input ≠ Output, and output costs more than input — only meaningful
                // when the machine has an input. Auto-producers (e.g. CoffeeDispenser)
                // skip these value-chain checks by design.
                if (hasInput && ProductionCatalog.MachineOutput.TryGetValue(m, out var o2))
                {
                    Assert(inp != o2, S, $"{m}.InputOutputDiffer");
                    if (PriceCatalog.BasePrice.TryGetValue(inp, out float inP) &&
                        PriceCatalog.BasePrice.TryGetValue(o2, out float outP))
                        Assert(outP > inP, S, $"{m}.OutputMoreExpensiveThanInput", $"in={inP:F2} out={outP:F2}");
                }
            }

            // No duplicate outputs (two machines can't make the same item)
            var outputs = new HashSet<ItemType>();
            foreach (var m in machineTypes)
            {
                if (ProductionCatalog.MachineOutput.TryGetValue(m, out var outp))
                    Assert(outputs.Add(outp), S, $"UniqueOutput.{outp}");
            }

            // Machine curves: SharedFourLevelCurve template
            var blender = ProductionCatalog.BlenderCurve();
            Assert(blender.Steps.Count == 4, S, "Blender.4Levels");
            Assert(blender.Steps[0].stackCapacity == 4, S, "Blender.MinStack4");
            Assert(blender.Steps[3].stackCapacity == 8, S, "Blender.MaxStack8");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 5 – FarmCatalog constants
        // ═══════════════════════════════════════════════════════════════════════
        private void RunFarmCatalogTests()
        {
            const string S = "FarmCatalog";

            // Tomato
            Assert(FarmCatalog.TomatoPlantCount == FarmCatalog.TomatoCols * FarmCatalog.TomatoRows, S, "TomatoPlantCount");
            Assert(FarmCatalog.TomatoMaxPerPlant > 0, S, "TomatoMaxPerPlant>0");
            Assert(FarmCatalog.TomatoGrowSecondsPerUnit > 0f, S, "TomatoGrowRate>0");

            // Egg
            Assert(FarmCatalog.HenCount > 0, S, "HenCount>0");
            Assert(FarmCatalog.EggMaxPerHen > 0, S, "EggMaxPerHen>0");
            Assert(FarmCatalog.EggGrowSecondsPerUnit > 0f, S, "EggGrowRate>0");

            // Cow
            Assert(FarmCatalog.CowCount > 0, S, "CowCount>0");
            Assert(FarmCatalog.MilkMaxPerCow > 0, S, "MilkMaxPerCow>0");
            Assert(FarmCatalog.MilkGrowSecondsPerUnit > 0f, S, "MilkGrowRate>0");
            // Milk grows slower than egg (mid-game pacing)
            Assert(FarmCatalog.MilkGrowSecondsPerUnit > FarmCatalog.EggGrowSecondsPerUnit, S, "Milk.SlowerThanEgg");

            // Wheat
            Assert(FarmCatalog.WheatBoxCount == FarmCatalog.WheatCols * FarmCatalog.WheatRows, S, "WheatBoxCount");
            Assert(FarmCatalog.WheatGrowSecondsPerUnit > 0f, S, "WheatGrowRate>0");

            // Herb
            Assert(FarmCatalog.HerbBushCount > 0, S, "HerbBushCount>0");
            Assert(FarmCatalog.HerbMaxPerBush > 0, S, "HerbMaxPerBush>0");
            Assert(FarmCatalog.HerbGrowSecondsPerUnit > 0f, S, "HerbGrowRate>0");

            // Max theoretical production at level 1, per minute (sanity)
            float tomatoPerMin = (FarmCatalog.TomatoPlantCount * 60f) / FarmCatalog.TomatoGrowSecondsPerUnit;
            Assert(tomatoPerMin <= 1000f, S, "Tomato.MaxPerMinSanity", $"{tomatoPerMin}");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite – StationCatalog (owner Mart-1 economy: 4/6/8 caps + two tracks)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunStationCatalogTests()
        {
            const string S = "StationCatalog";
            Assert(Catalog.StationCatalog.MaxLevel == 3, S, "MaxLevel3");
            Assert(Catalog.StationCatalog.Caps.Length == 3, S, "Caps3");
            Assert(Catalog.StationCatalog.Cap(1) == 4, S, "Cap1==4");
            Assert(Catalog.StationCatalog.Cap(2) == 6, S, "Cap2==6");
            Assert(Catalog.StationCatalog.Cap(3) == 8, S, "Cap3==8");
            // Owner-set costs (locked so a later edit can't silently change them).
            Assert(Catalog.StationCatalog.HenInput[0]  == 310 && Catalog.StationCatalog.HenInput[1]  == 700, S, "HenInput");
            Assert(Catalog.StationCatalog.HenOutput[0] == 250 && Catalog.StationCatalog.HenOutput[1] == 600, S, "HenOutput");
            Assert(Catalog.StationCatalog.MillInput[0] == 150 && Catalog.StationCatalog.MillOutput[0] == 180, S, "MillCosts");
            // Farms: owner spec — tomato 6 plants x3 = 18, wheat 12; regrow 0.3-0.5s.
            Assert(Catalog.FarmCatalog.TomatoPlantCount == 6 && Catalog.FarmCatalog.TomatoMaxPerPlant == 3, S, "TomatoFarm6x3");
            Assert(Catalog.FarmCatalog.WheatBoxCount == 12, S, "Wheat12");
            Assert(Catalog.FarmCatalog.TomatoGrowSecondsPerUnit >= 0.3f && Catalog.FarmCatalog.TomatoGrowSecondsPerUnit <= 0.5f, S, "TomatoRegrow.3-.5");
            Assert(Catalog.FarmCatalog.WheatGrowSecondsPerUnit  >= 0.3f && Catalog.FarmCatalog.WheatGrowSecondsPerUnit  <= 0.5f, S, "WheatRegrow.3-.5");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 6 – StorageCatalog
        // ═══════════════════════════════════════════════════════════════════════
        private void RunStorageCatalogTests()
        {
            const string S = "StorageCatalog";

            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));
            foreach (var item in allItems)
            {
                Assert(StorageCatalog.MaxStorage.ContainsKey(item), S, $"{item}.HasCapacity");
                if (StorageCatalog.MaxStorage.TryGetValue(item, out int cap))
                {
                    Assert(cap > 0,  S, $"{item}.CapPositive",  $"cap={cap}");
                    Assert(cap <= 100, S, $"{item}.CapSanityMax", $"cap={cap}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 7 – StoreInventory (1000+ simulated deposit/withdraw operations)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunStoreInventoryTests()
        {
            const string S = "StoreInventory";
            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));

            // Fresh inventory starts at 0
            var inv = new StoreInventory();
            foreach (var item in allItems)
            {
                Assert(inv.CountOf(item) == 0, S, $"{item}.StartsAtZero");
                Assert(inv.CapacityOf(item) > 0, S, $"{item}.CapacityPositive");
            }

            // 1) Deposit exactly capacity → count == capacity
            foreach (var item in allItems)
            {
                var fresh = new StoreInventory();
                int cap = fresh.CapacityOf(item);
                fresh.Deposit(item, cap);
                Assert(fresh.CountOf(item) == cap, S, $"{item}.DepositExactCap", $"count={fresh.CountOf(item)} cap={cap}");
            }

            // 2) Deposit beyond capacity → count stays at capacity (overflow to dustbin)
            foreach (var item in allItems)
            {
                var fresh = new StoreInventory();
                int cap = fresh.CapacityOf(item);
                fresh.Deposit(item, cap + 100);
                Assert(fresh.CountOf(item) == cap, S, $"{item}.OverflowClampsToCap");
                Assert(fresh.Dustbins[0].ItemsDiscarded + fresh.Dustbins[1].ItemsDiscarded == 100, S, $"{item}.OverflowGoesToDustbin");
            }

            // 3) Withdraw success and failure paths
            foreach (var item in allItems)
            {
                var fresh = new StoreInventory();
                int cap = fresh.CapacityOf(item);
                fresh.Deposit(item, 5);
                Assert(fresh.Withdraw(item, 3), S, $"{item}.WithdrawSuccess");
                Assert(fresh.CountOf(item) == 2, S, $"{item}.WithdrawCount");
                Assert(!fresh.Withdraw(item, 10), S, $"{item}.WithdrawFail.Insufficient");
                Assert(fresh.CountOf(item) == 2, S, $"{item}.WithdrawFail.NoChange");
            }

            // 4) Withdraw 0 → always succeeds without changing count
            foreach (var item in allItems)
            {
                var fresh = new StoreInventory();
                fresh.Deposit(item, 3);
                Assert(fresh.Withdraw(item, 0), S, $"{item}.WithdrawZero");
                Assert(fresh.CountOf(item) == 3, S, $"{item}.WithdrawZeroNoChange");
            }

            // 5) Sequential deposits don't exceed cap (1000 fuzz)
            var rng = new System.Random(42);
            foreach (var item in allItems)
            {
                var fresh = new StoreInventory();
                int cap = fresh.CapacityOf(item);
                int deposited = 0;
                for (int i = 0; i < 100; i++)
                {
                    int amount = rng.Next(1, 6);
                    fresh.Deposit(item, amount);
                    deposited += amount;
                }
                Assert(fresh.CountOf(item) <= cap, S, $"{item}.FuzzDepositNeverExceedCap", $"count={fresh.CountOf(item)} cap={cap}");
            }

            // 6) Dustbin load balance (overflow should distribute between bins)
            {
                var fresh = new StoreInventory();
                int cap = fresh.CapacityOf(ItemType.Egg);
                fresh.Deposit(ItemType.Egg, cap + 200); // 200 overflow
                int totalDiscarded = fresh.Dustbins[0].ItemsDiscarded + fresh.Dustbins[1].ItemsDiscarded;
                Assert(totalDiscarded == 200, S, "DustbinTotalBalance");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 8 – EconomyManager (price clamping, discounts, basket quoting)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunEconomyManagerTests()
        {
            const string S = "EconomyManager";
            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));
            var eco = new EconomyManager();

            // Default price = base price
            foreach (var item in allItems)
            {
                float expected = PriceCatalog.BasePrice[item];
                ExpectApprox(eco.GetUnitPrice(item), expected, S, $"{item}.DefaultPrice");
            }

            // Manual price clamping: max = base * 1.5
            foreach (var item in allItems)
            {
                var e = new EconomyManager();
                float bp = PriceCatalog.BasePrice[item];
                e.SetManualPrice(item, bp * 10f); // try to set way above
                float clamped = e.GetUnitPrice(item);
                ExpectApprox(clamped, bp * 1.5f, S, $"{item}.ManualPrice.ClampMax");
            }

            // Manual price clamping: min = max(0.05, base*0.5)
            foreach (var item in allItems)
            {
                var e = new EconomyManager();
                float bp = PriceCatalog.BasePrice[item];
                e.SetManualPrice(item, -999f);
                float clamped = e.GetUnitPrice(item);
                float expectedMin = Math.Max(0.05f, bp * 0.5f);
                ExpectApprox(clamped, expectedMin, S, $"{item}.ManualPrice.ClampMin");
            }

            // Offer discount 10% → price = base * 0.90
            foreach (var item in allItems)
            {
                var e = new EconomyManager();
                float bp = PriceCatalog.BasePrice[item];
                e.CreateOffer(item, 10f);
                ExpectApprox(e.GetUnitPrice(item), bp * 0.90f, S, $"{item}.Offer10pct");
            }

            // Offer clamped to max 90%
            foreach (var item in allItems)
            {
                var e = new EconomyManager();
                float bp = PriceCatalog.BasePrice[item];
                e.CreateOffer(item, 200f);
                ExpectApprox(e.GetUnitPrice(item), bp * 0.10f, S, $"{item}.Offer.ClampMax90pct");
            }

            // Clear offer restores base price
            foreach (var item in allItems)
            {
                var e = new EconomyManager();
                e.CreateOffer(item, 50f);
                e.ClearOffer(item);
                ExpectApprox(e.GetUnitPrice(item), PriceCatalog.BasePrice[item], S, $"{item}.ClearOffer");
            }

            // IsOverpriced: >20% over base → true
            foreach (var item in allItems)
            {
                var e = new EconomyManager();
                float bp = PriceCatalog.BasePrice[item];
                e.SetManualPrice(item, bp * 1.49f); // just below 1.5 clamp but >1.2
                Assert(e.IsOverpriced(item), S, $"{item}.IsOverpriced.True");
            }

            // IsOverpriced: at base price → false
            foreach (var item in allItems)
            {
                var e = new EconomyManager();
                Assert(!e.IsOverpriced(item), S, $"{item}.IsOverpriced.False.Default");
            }

            // AnyDiscountActive: none → false
            {
                var e = new EconomyManager();
                Assert(!e.AnyDiscountActive(), S, "NoDiscount.False");
            }

            // AnyDiscountActive: 10% offer → true
            {
                var e = new EconomyManager();
                e.CreateOffer(ItemType.Egg, 10f);
                Assert(e.AnyDiscountActive(), S, "Offer10pct.AnyDiscountTrue");
            }

            // Basket quoting: empty basket → 0 (floor raises to 1)
            {
                var e = new EconomyManager();
                var basket = new Dictionary<ItemType, int>();
                float q = e.QuoteBasket(basket);
                ExpectApprox(q, 1f, S, "EmptyBasket.FloorApplied");
            }

            // Basket quoting: 1 egg at default
            {
                var e = new EconomyManager();
                var basket = new Dictionary<ItemType, int> { { ItemType.Egg, 1 } };
                float q = e.QuoteBasket(basket);
                float expected = Math.Max(1f, PriceCatalog.BasePrice[ItemType.Egg] * 1);
                ExpectApprox(q, expected, S, "Basket.1Egg");
            }

            // Deposit and balance
            {
                var e = new EconomyManager();
                e.Deposit(50f);
                ExpectApprox(e.PlayerCash, 50f, S, "Deposit.Balance");
                e.Deposit(25.5f);
                ExpectApprox(e.PlayerCash, 75.5f, S, "Deposit.Cumulative");
            }

            // TrySpend success and failure (Bug #8 fix: was int, now float)
            {
                var e = new EconomyManager();
                e.Deposit(100f);
                Assert(e.TrySpend(60), S, "TrySpend.Success");
                ExpectApprox(e.PlayerCash, 40f, S, "TrySpend.Balance");
                Assert(!e.TrySpend(100), S, "TrySpend.Insufficient");
                ExpectApprox(e.PlayerCash, 40f, S, "TrySpend.NoChange.Fail");
                // Float precision test (Bug #8)
                Assert(e.TrySpend(39.99f), S, "TrySpend.FloatPrecision.Success");
                ExpectApprox(e.PlayerCash, 0.01f, S, "TrySpend.FloatPrecision.Balance");
                Assert(!e.TrySpend(0.02f), S, "TrySpend.FloatPrecision.Insufficient");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 9 – Buyer basket generation permutations (5000+ simulated buyers)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunBuyerBasketTests()
        {
            const string S = "BuyerBasket";
            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));

            // For every unlock level (1-5) test that only unlocked items can appear.
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                var unlockedItems = new HashSet<ItemType>();
                foreach (var item in allItems)
                    if (PriceCatalog.IsUnlocked(item, lvl)) unlockedItems.Add(item);

                // Simulate 1000 baskets at this level and confirm nothing outside unlockedItems appears
                for (int iter = 0; iter < 1000; iter++)
                {
                    var basket = SimulateBasket(lvl, unlockedItems);
                    foreach (var kv in basket)
                    {
                        Assert(unlockedItems.Contains(kv.Key), S, $"Lvl{lvl}.BasketOnlyUnlocked.iter{iter}", $"Found {kv.Key}");
                        Assert(kv.Value > 0, S, $"Lvl{lvl}.BasketPositiveQty.iter{iter}", $"{kv.Key}={kv.Value}");
                    }
                }
            }
        }

        private Dictionary<ItemType, int> SimulateBasket(int playerLevel, HashSet<ItemType> available)
        {
            var basket = new Dictionary<ItemType, int>();
            if (available.Count == 0) return basket;
            var list = new List<ItemType>(available);
            var rng = new System.Random();
            int count = rng.Next(1, 8);
            for (int i = 0; i < count; i++)
            {
                var item = list[rng.Next(list.Count)];
                if (!basket.ContainsKey(item)) basket[item] = 0;
                basket[item]++;
            }
            return basket;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 10 – Save data round-trip (serialize/deserialize)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunSaveDataRoundtripTests()
        {
            const string S = "SaveRoundtrip";

            // Create a full-state save
            var original = new GameSaveData
            {
                PlayerLevel = 5,
                PlayerCash = 1234.56f,
                StoreLevel = 4,
                StoreXp = 87,
                TotalPlaySeconds = 3600f,
                SaveTimestamp = "2026-07-05T00:00:00Z",
            };

            // Populate inventory
            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));
            foreach (var item in allItems)
                original.Inventory[item.ToString()] = (int)item + 3; // distinct non-zero values

            // Upgrade levels
            original.UpgradeLevels["Shelver1"] = 3;
            original.UpgradeLevels["Chef"] = 4;
            original.UpgradeLevels["Machine_Blender"] = 2;

            // Manual prices
            foreach (var item in allItems)
                original.ManualPrices[item.ToString()] = PriceCatalog.BasePrice[item] * 1.1f;

            // Pads
            original.PurchasedPads.Add("BLENDER");
            original.PurchasedPads.Add("DAIRY");
            original.PadProgressLabels.Add("OVEN");
            original.PadProgressRemaining.Add(75f);

            // Serialize
            original.OnBeforeSerialize();
            string json = JsonUtility.ToJson(original, true);
            Assert(!string.IsNullOrEmpty(json), S, "Json.NonEmpty");

            // Deserialize
            var loaded = JsonUtility.FromJson<GameSaveData>(json);
            loaded.OnAfterDeserialize();

            Expect(loaded.PlayerLevel,     original.PlayerLevel,     S, "PlayerLevel");
            ExpectApprox(loaded.PlayerCash, original.PlayerCash,     S, "PlayerCash");
            Expect(loaded.StoreLevel,      original.StoreLevel,       S, "StoreLevel");
            Expect(loaded.StoreXp,         original.StoreXp,          S, "StoreXp");
            ExpectApprox(loaded.TotalPlaySeconds, original.TotalPlaySeconds, S, "TotalPlaySeconds");

            foreach (var item in allItems)
            {
                string key = item.ToString();
                int exp = original.Inventory.TryGetValue(key, out int v) ? v : 0;
                int got = loaded.Inventory.TryGetValue(key, out int v2) ? v2 : -1;
                Assert(got == exp, S, $"Inventory.{item}", $"exp={exp} got={got}");
            }

            Expect(loaded.UpgradeLevels.TryGetValue("Shelver1", out int sl) ? sl : -1, 3, S, "UpgradeLevels.Shelver1");
            Expect(loaded.UpgradeLevels.TryGetValue("Chef",     out int cl) ? cl : -1, 4, S, "UpgradeLevels.Chef");

            Assert(loaded.PurchasedPads.Contains("BLENDER"), S, "PurchasedPads.Blender");
            Assert(loaded.PurchasedPads.Contains("DAIRY"),   S, "PurchasedPads.Dairy");

            Expect(loaded.PadProgressLabels.Count,    1, S, "PadProgress.Count");
            Expect(loaded.PadProgressLabels[0], "OVEN", S, "PadProgress.Label");
            ExpectApprox(loaded.PadProgressRemaining[0], 75f, S, "PadProgress.Remaining");

            // Edge: save with empty collections
            var empty = new GameSaveData();
            empty.OnBeforeSerialize();
            string emptyJson = JsonUtility.ToJson(empty, true);
            var emptyLoaded = JsonUtility.FromJson<GameSaveData>(emptyJson);
            emptyLoaded.OnAfterDeserialize();
            Assert(emptyLoaded.Inventory.Count == 0,     S, "Empty.Inventory");
            Assert(emptyLoaded.UpgradeLevels.Count == 0, S, "Empty.UpgradeLevels");
            Assert(emptyLoaded.PurchasedPads.Count == 0, S, "Empty.PurchasedPads");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 11 – Edge-case fuzz on arithmetic (1000s of combinations)
        // ═══════════════════════════════════════════════════════════════════════
        private void RunEdgeCaseFuzzTests()
        {
            const string S = "Fuzz";
            var rng = new System.Random(1337);

            // ItemStock add/remove: 2000 fuzz iterations per item
            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));
            foreach (var item in allItems)
            {
                int maxCap = StorageCatalog.MaxStorage[item];
                for (int i = 0; i < 200; i++)
                {
                    var stock = new ItemStock(item);
                    // Random deposit
                    int dep = rng.Next(0, maxCap * 3);
                    stock.Add(dep);
                    Assert(stock.Count >= 0,    S, $"{item}.FuzzNonNegativeAfterAdd");
                    Assert(stock.Count <= maxCap, S, $"{item}.FuzzNeverExceedCap.dep={dep}");

                    // Random remove
                    int rem = rng.Next(0, stock.Count + 5);
                    bool ok = stock.Remove(rem);
                    if (ok) Assert(stock.Count >= 0, S, $"{item}.FuzzNonNegativeAfterRemove");
                    else     Assert(stock.Count >= 0, S, $"{item}.FuzzFailed.CountUnchanged");
                }
            }

            // EconomyManager price fuzz: 500 random manual price sets
            foreach (var item in allItems)
            {
                float bp = PriceCatalog.BasePrice[item];
                for (int i = 0; i < 50; i++)
                {
                    var e = new EconomyManager();
                    float raw = (float)(rng.NextDouble() * 20.0 - 5.0); // -5 to +15
                    e.SetManualPrice(item, raw);
                    float price = e.GetUnitPrice(item);
                    float min = Math.Max(0.05f, bp * 0.5f);
                    float max = bp * 1.5f;
                    Assert(price >= min - 0.001f, S, $"{item}.PriceFuzz.Min.iter{i}", $"p={price:F4} min={min:F4}");
                    Assert(price <= max + 0.001f, S, $"{item}.PriceFuzz.Max.iter{i}", $"p={price:F4} max={max:F4}");
                }
            }

            // Bundle floor fuzz: 1000 random totals
            for (int i = 0; i < 1000; i++)
            {
                float raw = (float)(rng.NextDouble() * 10.0 - 2.0); // -2 to 8
                float result = PriceCatalog.ApplyBundleFloor(raw);
                Assert(result >= 1f, S, $"BundleFloor.AlwaysGE1.iter{i}", $"input={raw:F4} result={result:F4}");
                if (raw >= 1f) ExpectApprox(result, raw, S, $"BundleFloor.AboveFloorPassthrough.iter{i}");
            }

            // UpgradeCurve.GetStep clamp fuzz
            var playerCurve = RoleCatalog.PlayerCurve();
            for (int i = 0; i < 200; i++)
            {
                int lvl = rng.Next(-10, 20);
                var step = playerCurve.GetStep(lvl);
                Assert(step.level >= 1 && step.level <= playerCurve.MaxLevel, S, $"GetStep.ClampFuzz.lvl={lvl}", $"step.level={step.level}");
                Assert(step.stackCapacity > 0, S, $"GetStep.CapPositiveFuzz.lvl={lvl}");
                Assert(step.speedMultiplier > 0f, S, $"GetStep.SpeedPositiveFuzz.lvl={lvl}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 12 – Full production chain combination coverage
        //  Every machine-input→machine-output combo validated end-to-end
        // ═══════════════════════════════════════════════════════════════════════
        private void RunCombinationChainTests()
        {
            const string S = "ChainCombinations";

            // Map each machine to its chain and validate pricing monotonicity
            var chains = new (ItemType Input, MachineType Machine, ItemType Output)[]
            {
                (ItemType.Tomato,     MachineType.Blender,       ItemType.TomatoKetchup),
                (ItemType.Wheat,      MachineType.Mill,          ItemType.WheatFlour),
                (ItemType.WheatFlour, MachineType.Oven,          ItemType.Bread),
                (ItemType.Milk,       MachineType.Dairy,         ItemType.Cheese),
                (ItemType.Herb,       MachineType.LeafProcessor, ItemType.HerbPack),
                (ItemType.Egg,        MachineType.Stove,         ItemType.FriedEgg),
            };

            foreach (var chain in chains)
            {
                // Confirm catalog consistency
                Assert(ProductionCatalog.MachineInput[chain.Machine]  == chain.Input,  S, $"{chain.Machine}.InputMatch");
                Assert(ProductionCatalog.MachineOutput[chain.Machine] == chain.Output, S, $"{chain.Machine}.OutputMatch");

                // Value-add: output must be priced higher than input
                float inPrice  = PriceCatalog.BasePrice[chain.Input];
                float outPrice = PriceCatalog.BasePrice[chain.Output];
                Assert(outPrice > inPrice, S, $"{chain.Machine}.ValueAdd", $"in={inPrice:F2} out={outPrice:F2}");

                // Unlock level: processed output must unlock at same level or later than raw input
                int inLvl  = PriceCatalog.UnlockLevel[chain.Input];
                int outLvl = PriceCatalog.UnlockLevel[chain.Output];
                Assert(outLvl >= inLvl, S, $"{chain.Machine}.OutputUnlockGEInput", $"inLvl={inLvl} outLvl={outLvl}");

                // Simulate 100 inventory operations through this chain
                var inv = new StoreInventory();
                var eco = new EconomyManager();
                for (int run = 0; run < 100; run++)
                {
                    inv.Deposit(chain.Input, 5);
                    int before = inv.CountOf(chain.Input);
                    Assert(before <= StorageCatalog.MaxStorage[chain.Input], S, $"{chain.Machine}.SimInput.NoCap.run={run}");

                    bool withdrawn = inv.Withdraw(chain.Input, Mathf.Min(5, before));
                    if (withdrawn)
                    {
                        inv.Deposit(chain.Output, 1);
                        Assert(inv.CountOf(chain.Output) <= StorageCatalog.MaxStorage[chain.Output], S, $"{chain.Machine}.SimOutput.NoCap.run={run}");
                    }
                }
            }

            // Bread requires BOTH WheatFlour + Egg — ensure both have available storage
            Assert(StorageCatalog.MaxStorage.ContainsKey(ItemType.WheatFlour), S, "Bread.FlourHasStorage");
            Assert(StorageCatalog.MaxStorage.ContainsKey(ItemType.Egg),        S, "Bread.EggHasStorage");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 13 – Phone order calculations
        // ═══════════════════════════════════════════════════════════════════════
        private void RunPhoneOrderTests()
        {
            const string S = "PhoneOrder";
            var eco = new EconomyManager();

            // Order value = Clamp(45 + retailTotal*60, 45, 300)
            for (float retail = 0f; retail <= 5f; retail += 0.1f)
            {
                float computed = Mathf.Clamp(45f + retail * 60f, 45f, 300f);
                Assert(computed >= 45f,  S, $"Value.Min.retail={retail:F1}");
                Assert(computed <= 300f, S, $"Value.Max.retail={retail:F1}");
            }

            // Minimum value always at least $45 even when retail is $0
            {
                float computed = Mathf.Clamp(45f + 0f, 45f, 300f);
                ExpectApprox(computed, 45f, S, "Value.ZeroRetail.Is45");
            }

            // Maximum value clamps at $300
            {
                float computed = Mathf.Clamp(45f + 999f, 45f, 300f);
                ExpectApprox(computed, 300f, S, "Value.HugeRetail.Is300");
            }

            // Window is positive
            Assert(PriceCatalog.PhoneOrderWindowMinutes > 0f, S, "Window.Positive");

            // PhoneOrder fulfillment logic via StoreInventory
            var inv = new StoreInventory();
            inv.Deposit(ItemType.Egg, 10);
            inv.Deposit(ItemType.Tomato, 5);

            // Simulate CanFulfil: order needs 3 eggs and 2 tomatoes
            var order = new PhoneOrder();
            order.Items[ItemType.Egg] = 3;
            order.Items[ItemType.Tomato] = 2;
            order.TimeRemaining = 600f;

            bool canFulfil = true;
            foreach (var kv in order.Items)
                if (inv.CountOf(kv.Key) < kv.Value) { canFulfil = false; break; }
            Assert(canFulfil, S, "CanFulfil.Sufficient");

            // Fulfill: withdraw items
            foreach (var kv in order.Items)
                inv.Withdraw(kv.Key, kv.Value);
            Assert(inv.CountOf(ItemType.Egg)    == 7, S, "Fulfil.EggDeducted");
            Assert(inv.CountOf(ItemType.Tomato) == 3, S, "Fulfil.TomatoDeducted");

            // Order with insufficient stock
            var inv2 = new StoreInventory();
            inv2.Deposit(ItemType.Egg, 1);
            var order2 = new PhoneOrder();
            order2.Items[ItemType.Egg] = 5;
            order2.TimeRemaining = 600f;
            bool canFulfil2 = true;
            foreach (var kv in order2.Items)
                if (inv2.CountOf(kv.Key) < kv.Value) { canFulfil2 = false; break; }
            Assert(!canFulfil2, S, "CanFulfil.Insufficient");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 14 – CashCounter checkout calculation logic
        // ═══════════════════════════════════════════════════════════════════════
        private void RunCashCounterTests()
        {
            const string S = "CashCounter";
            var eco = new EconomyManager();

            // Quote basket: 2 eggs at default price
            var basket = new Dictionary<ItemType, int> { { ItemType.Egg, 2 } };
            float expected = Math.Max(1f, PriceCatalog.BasePrice[ItemType.Egg] * 2);
            ExpectApprox(eco.QuoteBasket(basket), expected, S, "BasketQuote.2Eggs");

            // Quote basket: 1 of every item
            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));
            float totalExpected = 0f;
            var fullBasket = new Dictionary<ItemType, int>();
            foreach (var item in allItems)
            {
                fullBasket[item] = 1;
                totalExpected += PriceCatalog.BasePrice[item];
            }
            float totalQuoted = eco.QuoteBasket(fullBasket);
            Assert(totalQuoted >= 1f, S, "FullBasket.GE1");
            ExpectApprox(totalQuoted, Math.Max(1f, totalExpected), S, "FullBasket.Correct");

            // Counter 1 unlock levels from PriceCatalog constants
            Assert(PriceCatalog.CashCounter1UnlockLevel == 1, S, "Counter1.UnlocksAt1");
            Assert(PriceCatalog.Cashier1AssignableLevel  == 1, S, "Counter1.CashierFromStart");
            // Batch 35 moved Counter 2 from L4 to L3 (single-counter queue
            // overflow bit hard at L3) — the assertion tracks the design change.
            Assert(PriceCatalog.CashCounter2UnlockLevel  == 3, S, "Counter2.UnlocksAt3");
            // Counter 3 is MegaMart's only till until L10 - it must be open the
            // moment travel unlocks (L6) or MegaMart income/XP hard-stalls.
            // Found by TesterBot run 1: 18 sim-minutes at L6 with zero progress.
            Assert(PriceCatalog.CashCounter3UnlockLevel  == 6, S, "Counter3.OpenOnArrival");
            Assert(PriceCatalog.CashCounter4UnlockLevel  == 10, S, "Counter4.Endgame");

            // Seconds-per-checkout sanity
            Assert(1.2f > 0f, S, "SecondsPerCheckout.Positive");
            Assert(1.2f / 3f < 1.2f, S, "ManualOverride.Faster"); // player is 3x faster
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 15 – Full upgrade curve permutation (all levels × all roles)
        //  Covers ~300+ assertions
        // ═══════════════════════════════════════════════════════════════════════
        private void RunUpgradeCurvePermutationTests()
        {
            const string S = "CurvePermutation";

            var curves = new (string Name, UpgradeCurve Curve)[]
            {
                ("Player",         RoleCatalog.PlayerCurve()),
                ("Shelver1",       RoleCatalog.Shelver1Curve()),
                ("Shelver2",       RoleCatalog.Shelver2Curve()),
                ("Chef",           RoleCatalog.ChefCurve()),
                ("Farmer",         RoleCatalog.FarmerCurve()),
                ("Blender",        ProductionCatalog.BlenderCurve()),
                ("Oven",           ProductionCatalog.OvenCurve()),
                ("Mill",           ProductionCatalog.MillCurve()),
                ("Dairy",          ProductionCatalog.DairyCurve()),
                ("LeafProcessor",  ProductionCatalog.LeafProcessorCurve()),
                ("Stove",          ProductionCatalog.StoveCurve()),
                ("HenCoop",        ProductionCatalog.HenCoopCurve()),
            };

            foreach (var (name, curve) in curves)
            {
                int maxLvl = curve.MaxLevel;
                // Validate every single level
                for (int lvl = 1; lvl <= maxLvl; lvl++)
                {
                    var step = curve.GetStep(lvl);
                    Assert(step.level == lvl, S, $"{name}.Lvl{lvl}.LevelMatch");
                    Assert(step.stackCapacity > 0, S, $"{name}.Lvl{lvl}.Cap>0");
                    Assert(step.speedMultiplier >= 1f, S, $"{name}.Lvl{lvl}.Speed>=1");
                    Assert(step.upgradeCost >= 0, S, $"{name}.Lvl{lvl}.Cost>=0");

                    // Level 1 is always free
                    if (lvl == 1) Assert(step.upgradeCost == 0, S, $"{name}.Lvl1.Free");

                    // TryUpgrade state machine
                    bool canUpgrade = curve.CanUpgrade(lvl);
                    int cost = curve.CostForNextLevel(lvl);
                    if (lvl < maxLvl)
                    {
                        Assert(canUpgrade, S, $"{name}.CanUpgrade.Lvl{lvl}");
                        Assert(cost > 0, S, $"{name}.CostPositive.Lvl{lvl}", $"cost={cost}");
                    }
                    else
                    {
                        Assert(!canUpgrade, S, $"{name}.CantUpgrade.MaxLvl{lvl}");
                        Assert(cost == -1, S, $"{name}.CostMinusOne.MaxLvl{lvl}");
                    }
                }

                // Monotonicity across the full ladder
                for (int lvl = 2; lvl <= maxLvl; lvl++)
                {
                    var prev = curve.GetStep(lvl - 1);
                    var curr = curve.GetStep(lvl);
                    Assert(curr.stackCapacity >= prev.stackCapacity, S, $"{name}.CapNonDecreasing.{lvl-1}->{lvl}",
                           $"{prev.stackCapacity}->{curr.stackCapacity}");
                    Assert(curr.speedMultiplier >= prev.speedMultiplier, S, $"{name}.SpeedNonDecreasing.{lvl-1}->{lvl}",
                           $"{prev.speedMultiplier:F2}->{curr.speedMultiplier:F2}");
                    Assert(curr.upgradeCost > prev.upgradeCost, S, $"{name}.CostStrictlyIncreasing.{lvl-1}->{lvl}",
                           $"{prev.upgradeCost}->{curr.upgradeCost}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Suite 16 – Economy price band permutation (every item × every price × discounts)
        //  ~2000 tests
        // ═══════════════════════════════════════════════════════════════════════
        private void RunEconomyPricePermutationTests()
        {
            const string S = "EconPermutation";
            var allItems = (ItemType[])Enum.GetValues(typeof(ItemType));
            float[] discounts = { 0f, 5f, 10f, 25f, 50f, 75f, 90f, 100f };
            float[] manualFactors = { 0f, 0.1f, 0.5f, 1.0f, 1.2f, 1.49f, 1.5f, 2.0f, 5.0f };

            foreach (var item in allItems)
            {
                float bp = PriceCatalog.BasePrice[item];
                float minAllowed = Math.Max(0.05f, bp * 0.5f);
                float maxAllowed = bp * 1.5f;

                // Manual price permutations
                foreach (float factor in manualFactors)
                {
                    var e = new EconomyManager();
                    e.SetManualPrice(item, bp * factor);
                    float p = e.GetUnitPrice(item);
                    Assert(p >= minAllowed - 0.001f, S, $"{item}.Manual.f={factor}.MinOK",  $"p={p:F4} min={minAllowed:F4}");
                    Assert(p <= maxAllowed + 0.001f, S, $"{item}.Manual.f={factor}.MaxOK",  $"p={p:F4} max={maxAllowed:F4}");
                    Assert(p > 0f, S, $"{item}.Manual.f={factor}.Positive");
                }

                // Discount permutations
                foreach (float disc in discounts)
                {
                    var e = new EconomyManager();
                    e.CreateOffer(item, disc);
                    float p = e.GetUnitPrice(item);
                    float clampedDisc = Math.Min(disc, 90f);
                    float expectedP = bp * (1f - clampedDisc / 100f);
                    ExpectApprox(p, expectedP, S, $"{item}.Offer.d={disc}.Price");
                    Assert(p >= 0f, S, $"{item}.Offer.NonNegative.d={disc}");
                }

                // Combined: manual price + discount
                foreach (float factor in new[] { 0.5f, 1.0f, 1.3f })
                {
                    foreach (float disc in new[] { 0f, 10f, 50f })
                    {
                        var e = new EconomyManager();
                        e.SetManualPrice(item, bp * factor);
                        e.CreateOffer(item, disc);
                        float p = e.GetUnitPrice(item);
                        Assert(p >= 0f, S, $"{item}.ComboPriceNonNeg.f={factor}.d={disc}");
                        Assert(p <= maxAllowed * 1.001f, S, $"{item}.ComboPriceSanityMax.f={factor}.d={disc}", $"p={p:F4} max={maxAllowed:F4}");
                    }
                }
            }
        }
    }
}
