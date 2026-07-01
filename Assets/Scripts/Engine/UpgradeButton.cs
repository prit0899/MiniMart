using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniMart.Characters;
using MiniMart.Production;

namespace MiniMart.UI
{
    /// <summary>
    /// Generic upgrade button row. Wire one per upgradeable entity in the Shop/Management screen.
    /// </summary>
    public class UpgradeButton : MonoBehaviour
    {
        public TextMeshProUGUI NameLabel;
        public TextMeshProUGUI LevelLabel;
        public TextMeshProUGUI CostLabel;
        public Button UpgradeBtn;

        // Supply exactly one of these.
        public CharacterBase TargetCharacter;
        public Machine TargetMachine;
        public HenCoop TargetHenCoop;

        private void Awake() => UpgradeBtn?.onClick.AddListener(DoUpgrade);

        private void Update() => RefreshDisplay();

        private void RefreshDisplay()
        {
            if (TargetCharacter != null)
            {
                int lvl = TargetCharacter.Level;
                int maxLvl = TargetCharacter.Curve?.MaxLevel ?? lvl;
                int cost = TargetCharacter.Curve?.CostForNextLevel(lvl) ?? -1;
                Set(TargetCharacter.Role.ToString(), lvl, maxLvl, cost);
            }
            else if (TargetMachine != null)
            {
                int lvl = TargetMachine.Level;
                int cost = TargetMachine.TryUpgrade(out _) ? 0 : -1; // just peek cost via a dry run isn't ideal; keep simple
                Set(TargetMachine.Type.ToString(), lvl, 4, -1);
            }
        }

        private void Set(string name, int lvl, int maxLvl, int cost)
        {
            if (NameLabel != null)  NameLabel.text  = name;
            if (LevelLabel != null) LevelLabel.text = $"Lv {lvl}/{maxLvl}";
            if (CostLabel != null)  CostLabel.text  = cost < 0 ? "MAX" : $"${cost}";
            if (UpgradeBtn != null) UpgradeBtn.interactable = cost >= 0
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
                if (!TargetMachine.TryUpgrade(out int cost) || !eco.TrySpend(cost)) return;
            }
            else if (TargetHenCoop != null)
            {
                // Peek cost from HenCoop directly
                if (!TargetHenCoop.TryUpgrade(out int cost) || !eco.TrySpend(cost)) return;
            }
        }
    }
}
