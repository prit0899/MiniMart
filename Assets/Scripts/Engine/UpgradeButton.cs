using UnityEngine;
using UnityEngine.UI;
using MiniMart.Characters;
using MiniMart.Production;
using MiniMart.Core;
using System.Collections.Generic;

namespace MiniMart.UI
{
    public class UpgradeRow : MonoBehaviour
    {
        public string DisplayName;
        public Text NameLabel;
        public Text LevelLabel;
        public Text CostLabel;
        public Button UpgradeBtn;
        public Image BtnImage;

        public CharacterBase TargetCharacter;
        public Machine TargetMachine;
        public HenCoop TargetHenCoop;
        public CowPen TargetCowPen;

        private static readonly Color CostGreen = new Color(0.35f, 0.78f, 0.28f, 1f);
        private static readonly Color MaxedGray = new Color(0.74f, 0.74f, 0.74f, 1f);

        private void Start()
        {
            if (UpgradeBtn != null) UpgradeBtn.onClick.AddListener(DoUpgrade);
        }

        private void Update() => RefreshDisplay();

        private void RefreshDisplay()
        {
            if (TargetCharacter != null)
            {
                int lvl = TargetCharacter.Level;
                int maxLvl = TargetCharacter.Curve?.MaxLevel ?? lvl;
                int cost = TargetCharacter.Curve?.CostForNextLevel(lvl) ?? -1;
                Set(lvl, maxLvl, cost);
            }
            else if (TargetMachine != null)
            {
                Set(TargetMachine.Level, 4, TargetMachine.NextUpgradeCost);
            }
            else if (TargetHenCoop != null)
            {
                Set(TargetHenCoop.Level, 4, TargetHenCoop.NextUpgradeCost);
            }
            else if (TargetCowPen != null)
            {
                Set(TargetCowPen.Level, 4, TargetCowPen.NextUpgradeCost);
            }
        }

        private void Set(int lvl, int maxLvl, int cost)
        {
            if (NameLabel != null)  NameLabel.text  = DisplayName;
            if (LevelLabel != null) LevelLabel.text = $"Stack+Speed - Lvl.{lvl}";

            bool maxed = cost < 0;
            if (CostLabel != null) CostLabel.text = maxed ? "Maxed" : $"$ {cost}";
            if (BtnImage != null)  BtnImage.color = maxed ? MaxedGray : CostGreen;
            if (UpgradeBtn != null) UpgradeBtn.interactable = !maxed
                && GameManager.Instance != null
                && GameManager.Instance.Economy.PlayerCash >= cost;
        }

        private void DoUpgrade()
        {
            var eco = GameManager.Instance?.Economy;
            if (eco == null) return;

            if (TargetCharacter != null)
            {
                int cost = TargetCharacter.Curve?.CostForNextLevel(TargetCharacter.Level) ?? -1;
                if (cost < 0 || !eco.TrySpend(cost)) return;
                TargetCharacter.TryUpgrade(out _);
                if (TargetCharacter is PlayerController pc)
                    GameManager.Instance.OnPlayerLevelUp(pc.Level);
            }
            else if (TargetMachine != null)
            {
                // Spend first — TryUpgrade mutates, so it must only run after payment succeeds.
                int cost = TargetMachine.NextUpgradeCost;
                if (cost < 0 || !eco.TrySpend(cost)) return;
                TargetMachine.TryUpgrade(out _);
            }
            else if (TargetHenCoop != null)
            {
                int cost = TargetHenCoop.NextUpgradeCost;
                if (cost < 0 || !eco.TrySpend(cost)) return;
                TargetHenCoop.TryUpgrade(out _);
            }
            else if (TargetCowPen != null)
            {
                int cost = TargetCowPen.NextUpgradeCost;
                if (cost < 0 || !eco.TrySpend(cost)) return;
                TargetCowPen.TryUpgrade(out _);
            }
        }
    }

    /// <summary>
    /// Reference-style upgrade panel (My Mini Mart): green shell, three colored tabs
    /// (Workers / Machines / Animals), one rounded row bar per upgradeable entity with
    /// its name, current level, and a green cost button that turns grey "Maxed".
    /// </summary>
    public class UpgradePanelController : MonoBehaviour
    {
        // Palette lifted from the reference screenshots.
        private static readonly Color TabWorkers  = new Color(0.55f, 0.33f, 0.75f, 1f);
        private static readonly Color TabMachines = new Color(0.25f, 0.55f, 0.90f, 1f);
        private static readonly Color TabAnimals  = new Color(0.94f, 0.60f, 0.15f, 1f);
        private static readonly Color RowWorkers  = new Color(0.64f, 0.48f, 0.84f, 1f);
        private static readonly Color RowMachines = new Color(0.47f, 0.72f, 0.95f, 1f);
        private static readonly Color RowAnimals  = new Color(0.96f, 0.70f, 0.40f, 1f);

        private readonly List<GameObject> tabContents = new List<GameObject>();
        private readonly List<Image> tabButtons = new List<Image>();

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var font = Engine.HUDBuilder.UIFont;

            // Tab bar under the title.
            string[] tabNames = { "Workers", "Machines", "Animals" };
            Color[] tabColors = { TabWorkers, TabMachines, TabAnimals };
            float tabWidth = 215f;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var tabGO = new GameObject($"Tab_{tabNames[i]}", typeof(RectTransform));
                tabGO.transform.SetParent(transform, false);
                var rt = tabGO.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2((i - 1) * (tabWidth + 8f), -84f);
                rt.sizeDelta = new Vector2(tabWidth, 44f);

                var img = tabGO.AddComponent<Image>();
                img.color = tabColors[i];
                tabButtons.Add(img);

                var btn = tabGO.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => ShowTab(index));

                var label = MakeText($"{tabNames[i]}_Label", tabGO.transform, tabNames[i], font, 22, TextAnchor.MiddleCenter, Color.white, true);
                Stretch(label.rectTransform);
            }

            // One scrollable-feel content column per tab (plain vertical layout; row counts are small).
            for (int i = 0; i < 3; i++)
            {
                var content = new GameObject($"Content_{tabNames[i]}", typeof(RectTransform));
                content.transform.SetParent(transform, false);
                var crt = content.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 0);
                crt.anchorMax = new Vector2(1, 1);
                crt.offsetMin = new Vector2(14, 14);
                crt.offsetMax = new Vector2(-14, -140);

                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.spacing = 12;
                vlg.childControlHeight = false;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;

                tabContents.Add(content);
            }

            // Workers tab.
            AddRow(tabContents[0], "PLAYER", RowWorkers, font, character: gm.Player);
            AddRow(tabContents[0], "Farmer", RowWorkers, font, character: gm.Farmer);
            AddRow(tabContents[0], "Shelver A", RowWorkers, font, character: gm.Shelver1);
            AddRow(tabContents[0], "Shelver B", RowWorkers, font, character: gm.Shelver2);
            AddRow(tabContents[0], "Chef", RowWorkers, font, character: gm.Chef);

            // Machines tab.
            AddRow(tabContents[1], "Blender", RowMachines, font, machine: gm.Blender);
            AddRow(tabContents[1], "Bread Oven", RowMachines, font, machine: gm.Oven);
            AddRow(tabContents[1], "Wheat Mill", RowMachines, font, machine: gm.Mill);
            AddRow(tabContents[1], "Dairy", RowMachines, font, machine: gm.Dairy);

            // Animals tab.
            AddRow(tabContents[2], "Hen Coop", RowAnimals, font, coop: gm.HenCoop);
            AddRow(tabContents[2], "Cow Pen", RowAnimals, font, cowPen: gm.CowPen);

            ShowTab(0);
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < tabContents.Count; i++)
                tabContents[i].SetActive(i == index);
            for (int i = 0; i < tabButtons.Count; i++)
            {
                var c = tabButtons[i].color;
                c.a = i == index ? 1f : 0.55f; // dim inactive tabs
                tabButtons[i].color = c;
            }
        }

        private void AddRow(GameObject parent, string displayName, Color rowColor, Font font,
            CharacterBase character = null, Machine machine = null, HenCoop coop = null, CowPen cowPen = null)
        {
            if (character == null && machine == null && coop == null && cowPen == null) return;

            var rowGO = new GameObject($"Row_{displayName}", typeof(RectTransform));
            rowGO.transform.SetParent(parent.transform, false);
            rowGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 64);

            var bg = rowGO.AddComponent<Image>();
            bg.color = rowColor;

            var row = rowGO.AddComponent<UpgradeRow>();
            row.DisplayName = displayName;
            row.TargetCharacter = character;
            row.TargetMachine = machine;
            row.TargetHenCoop = coop;
            row.TargetCowPen = cowPen;

            // Name (left).
            row.NameLabel = MakeText("Name", rowGO.transform, displayName, font, 22, TextAnchor.MiddleLeft, Color.white, true);
            SetAnchored(row.NameLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(20, 0), new Vector2(220, 44));

            // Level (right of center).
            row.LevelLabel = MakeText("Level", rowGO.transform, "", font, 20, TextAnchor.MiddleRight, Color.white, false);
            SetAnchored(row.LevelLabel.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-190, 0), new Vector2(240, 44));

            // Cost / Maxed button (right).
            var btnGO = new GameObject("UpgradeBtn", typeof(RectTransform));
            btnGO.transform.SetParent(rowGO.transform, false);
            SetAnchored(btnGO.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-18, 0), new Vector2(150, 46));
            row.BtnImage = btnGO.AddComponent<Image>();
            row.UpgradeBtn = btnGO.AddComponent<Button>();
            row.UpgradeBtn.targetGraphic = row.BtnImage;

            row.CostLabel = MakeText("Cost", btnGO.transform, "", font, 20, TextAnchor.MiddleCenter, Color.white, true);
            Stretch(row.CostLabel.rectTransform);
        }

        // ─── small uGUI helpers ─────────────────────────────────────────────

        private static Text MakeText(string name, Transform parent, string content, Font font,
            int size, TextAnchor align, Color color, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = content;
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = size;
            t.resizeTextMinSize = Mathf.Max(10, size / 2);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
