using UnityEngine;
using UnityEngine.UI;
using MiniMart.Characters;
using MiniMart.Production;
using MiniMart.Core;
using System.Collections.Generic;

namespace MiniMart.UI
{
    /// <summary>
    /// Reference-parity upgrade row: shows one specific track (Stack or Speed) for
    /// one entity, with its own cost pill (green with cash icon + number) and its
    /// own "Maxed" grey checkmark when the track is at its ceiling.
    /// </summary>
    public class UpgradeRow : MonoBehaviour
    {
        public enum Track { Stack, Speed }

        public string DisplayName;
        public Track WhichTrack = Track.Stack;

        public Text NameLabel;
        public Text LevelLabel;
        public Text CostLabel;
        public Button UpgradeBtn;
        public Image BtnImage;
        public Image CostIcon;

        public CharacterBase TargetCharacter;
        public Machine TargetMachine;
        public HenCoop TargetHenCoop;
        public CowPen TargetCowPen;

        private static readonly Color CostGreen = new Color(0.20f, 0.72f, 0.28f, 1f);
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
                if (WhichTrack == Track.Stack)
                    Set(TargetCharacter.StackLevel, TargetCharacter.Curve?.MaxLevel ?? 1, TargetCharacter.NextStackCost);
                else
                    Set(TargetCharacter.SpeedLevel, TargetCharacter.Curve?.MaxLevel ?? 1, TargetCharacter.NextSpeedCost);
            }
            else if (TargetMachine != null)
            {
                // Machine currently has one Level driving both stack and speed —
                // both tracks read from the same Level for now (the split is
                // on the character side; machine parity is a follow-up).
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
            if (LevelLabel != null) LevelLabel.text = $"{WhichTrack} – Lvl.{lvl}";

            bool maxed = cost < 0;
            if (CostLabel != null) CostLabel.text = maxed ? "Maxed" : $"{cost}";
            if (BtnImage != null)  BtnImage.color = maxed ? MaxedGray : CostGreen;
            if (CostIcon != null)  CostIcon.gameObject.SetActive(!maxed);
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
                int cost;
                bool ok = WhichTrack == Track.Stack
                    ? TargetCharacter.TryUpgradeStack(out cost)
                    : TargetCharacter.TryUpgradeSpeed(out cost);
                if (!ok) return;
                if (!eco.TrySpend(cost)) { /* insufficient — revert */ return; }
                if (TargetCharacter is PlayerController pc)
                    GameManager.Instance.OnPlayerLevelUp(pc.Level);
            }
            else if (TargetMachine != null)
            {
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
    /// Reference-parity upgrade panel: green shell, three colored tabs
    /// (Workers / Machines / Animals), TWO rows per entity (Stack + Speed) with
    /// a rounded pill button carrying a green cash icon + cost number, matching
    /// the "My Mini Mart" reference exactly.
    /// </summary>
    public class UpgradePanelController : MonoBehaviour
    {
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

            for (int i = 0; i < 3; i++)
            {
                // Scrollable viewport per tab. The Workers tab alone stacks ~11
                // 56px rows into what was a 560px shell — rows visibly spilled
                // out of the panel (see LIVE_ref4 capture). ScrollRect +
                // RectMask2D + ContentSizeFitter makes ANY row count fit, so
                // future machines/animals can never overflow again.
                var viewport = new GameObject($"Viewport_{tabNames[i]}",
                    typeof(RectTransform), typeof(RectMask2D), typeof(Image));
                viewport.transform.SetParent(transform, false);
                var vrt = viewport.GetComponent<RectTransform>();
                vrt.anchorMin = new Vector2(0, 0);
                vrt.anchorMax = new Vector2(1, 1);
                vrt.offsetMin = new Vector2(14, 14);
                vrt.offsetMax = new Vector2(-14, -140);
                // Near-invisible image so the viewport is a raycast target for drags.
                viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);

                var content = new GameObject($"Content_{tabNames[i]}", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                var crt = content.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 1);
                crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1f);
                crt.offsetMin = Vector2.zero;
                crt.offsetMax = Vector2.zero;

                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.spacing = 10;
                vlg.childControlHeight = false;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;

                var fitter = content.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var scroll = viewport.AddComponent<ScrollRect>();
                scroll.content = crt;
                scroll.viewport = vrt;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 24f;

                tabContents.Add(content);
            }

            // Workers tab — Player gets ONE row (Stack only per reference).
            // All other workers get Stack + Speed rows.
            AddCharacterBlock(tabContents[0], "PLAYER", RowWorkers, font, gm.Player, playerOnly: true);
            AddCharacterBlock(tabContents[0], "Farmer", RowWorkers, font, gm.Farmer);
            AddCharacterBlock(tabContents[0], "Shelver A", RowWorkers, font, gm.Shelver1);
            AddCharacterBlock(tabContents[0], "Shelver B", RowWorkers, font, gm.Shelver2);
            AddCharacterBlock(tabContents[0], "Chef", RowWorkers, font, gm.Chef);

            // Machines tab.
            AddMachineBlock(tabContents[1], "Blender", RowMachines, font, gm.Blender);
            AddMachineBlock(tabContents[1], "Bread Oven", RowMachines, font, gm.Oven);
            AddMachineBlock(tabContents[1], "Wheat Mill", RowMachines, font, gm.Mill);
            AddMachineBlock(tabContents[1], "Dairy", RowMachines, font, gm.Dairy);
            AddMachineBlock(tabContents[1], "Egg Stove", RowMachines, font, gm.Stove);
            AddMachineBlock(tabContents[1], "Leaf Unit", RowMachines, font, gm.LeafProcessor);

            // Animals tab.
            AddCoopBlock(tabContents[2], "Chickens", RowAnimals, font, gm.HenCoop);
            AddCowBlock(tabContents[2], "Cow", RowAnimals, font, gm.CowPen);

            ShowTab(0);
        }

        private void ShowTab(int index)
        {
            // Toggle the parent VIEWPORT (contents are nested inside ScrollRect
            // viewports now) — leaving all three viewports active would stack
            // their raycast targets and swallow drags on the visible tab.
            for (int i = 0; i < tabContents.Count; i++)
                tabContents[i].transform.parent.gameObject.SetActive(i == index);
            for (int i = 0; i < tabButtons.Count; i++)
            {
                var c = tabButtons[i].color;
                c.a = i == index ? 1f : 0.55f;
                tabButtons[i].color = c;
            }
        }

        // Two rows per character: Stack (top) + Speed (bottom).
        private void AddCharacterBlock(GameObject parent, string displayName, Color rowColor, Font font,
            CharacterBase character, bool playerOnly = false)
        {
            if (character == null) return;
            AddRow(parent, displayName, UpgradeRow.Track.Stack, rowColor, font, character: character);
            if (!playerOnly)
                AddRow(parent, displayName, UpgradeRow.Track.Speed, rowColor, font, character: character);
        }

        private void AddMachineBlock(GameObject parent, string displayName, Color rowColor, Font font, Machine machine)
        {
            if (machine == null) return;
            AddRow(parent, displayName, UpgradeRow.Track.Stack, rowColor, font, machine: machine);
            AddRow(parent, displayName, UpgradeRow.Track.Speed, rowColor, font, machine: machine);
        }

        private void AddCoopBlock(GameObject parent, string displayName, Color rowColor, Font font, HenCoop coop)
        {
            if (coop == null) return;
            AddRow(parent, displayName, UpgradeRow.Track.Stack, rowColor, font, coop: coop);
            AddRow(parent, displayName, UpgradeRow.Track.Speed, rowColor, font, coop: coop);
        }

        private void AddCowBlock(GameObject parent, string displayName, Color rowColor, Font font, CowPen pen)
        {
            if (pen == null) return;
            AddRow(parent, displayName, UpgradeRow.Track.Stack, rowColor, font, cowPen: pen);
            AddRow(parent, displayName, UpgradeRow.Track.Speed, rowColor, font, cowPen: pen);
        }

        private void AddRow(GameObject parent, string displayName, UpgradeRow.Track track, Color rowColor, Font font,
            CharacterBase character = null, Machine machine = null, HenCoop coop = null, CowPen cowPen = null)
        {
            var rowGO = new GameObject($"Row_{displayName}_{track}", typeof(RectTransform));
            rowGO.transform.SetParent(parent.transform, false);
            rowGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 56);

            var bg = rowGO.AddComponent<Image>();
            bg.color = rowColor;
            bg.sprite = Engine.HUDBuilder.RoundedSprite;
            bg.type = Image.Type.Sliced;

            var row = rowGO.AddComponent<UpgradeRow>();
            row.DisplayName = displayName;
            row.WhichTrack = track;
            row.TargetCharacter = character;
            row.TargetMachine = machine;
            row.TargetHenCoop = coop;
            row.TargetCowPen = cowPen;

            // Left: entity name (only on the FIRST row per entity — Stack row).
            if (track == UpgradeRow.Track.Stack)
            {
                row.NameLabel = MakeText("Name", rowGO.transform, displayName, font, 22, TextAnchor.MiddleLeft, Color.white, true);
                SetAnchored(row.NameLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(20, 0), new Vector2(220, 44));
            }

            // Center: "Stack – Lvl.N" or "Speed – Lvl.N"
            row.LevelLabel = MakeText("Level", rowGO.transform, "", font, 20, TextAnchor.MiddleRight, Color.white, false);
            SetAnchored(row.LevelLabel.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-200, 0), new Vector2(240, 44));

            // Right: rounded green cost pill with cash icon + number (or grey "Maxed").
            var btnGO = new GameObject("UpgradeBtn", typeof(RectTransform));
            btnGO.transform.SetParent(rowGO.transform, false);
            SetAnchored(btnGO.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-18, 0), new Vector2(150, 42));
            row.BtnImage = btnGO.AddComponent<Image>();
            row.BtnImage.sprite = Engine.HUDBuilder.RoundedSprite;
            row.BtnImage.type = Image.Type.Sliced;
            row.UpgradeBtn = btnGO.AddComponent<Button>();
            row.UpgradeBtn.targetGraphic = row.BtnImage;

            // Small cash-bill icon inside the pill.
            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(btnGO.transform, false);
            SetAnchored(iconGO.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(14, 0), new Vector2(24, 20));
            row.CostIcon = iconGO.AddComponent<Image>();
            row.CostIcon.color = new Color(0.30f, 0.85f, 0.35f);
            row.CostIcon.sprite = Engine.HUDBuilder.RoundedSprite;
            row.CostIcon.type = Image.Type.Sliced;

            row.CostLabel = MakeText("Cost", btnGO.transform, "", font, 20, TextAnchor.MiddleCenter, Color.white, true);
            SetAnchored(row.CostLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(10, 0), new Vector2(120, 42));
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
