using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Reference-flow standing upgrade pad (new map spec §"Upgrade & Hub Center").
    /// A green glowing disc on the grass — walk over it, cash drains, the target
    /// character/system levels up. Three pads sit at the hub: Player Speed,
    /// Carry Capacity, and Crop Speed. Unlike PurchasePad these don't spawn
    /// anything new — they call a supplied upgrade action once cost is paid.
    /// </summary>
    public class UpgradePad : MonoBehaviour
    {
        public enum Kind { PlayerSpeed, PlayerCarry, CropSpeed }

        public Kind PadKind;

        // Cost ladder for each pad kind (index by current level).
        private static readonly int[] SpeedCosts = { 50, 100, 200, 500 };
        private static readonly int[] CarryCosts = { 50, 100, 200, 500 };
        private static readonly int[] CropCosts  = { 75, 150, 300, 600 };

        private TextMesh label;
        private Transform arrowRing;
        private float bobT;
        private float dwell;
        private Vector3 lastPlayerPos;

        public static UpgradePad Create(Vector3 position, Kind kind)
        {
            var go = new GameObject($"UpgradePad_{kind}");
            go.transform.position = position;
            var pad = go.AddComponent<UpgradePad>();
            pad.PadKind = kind;
            return pad;
        }

        private void Start()
        {
            var glowGreen = new Color(0.55f, 0.95f, 0.45f, 0.9f);
            var darkGreen = new Color(0.22f, 0.55f, 0.24f);

            // Glowing ground disc — brighter than a purchase pad to signal "always available".
            PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3(0, 0.02f, 0), new Vector3(1.8f, 0.03f, 1.8f), glowGreen);
            PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3(0, 0.04f, 0), new Vector3(1.4f, 0.02f, 1.4f), new Color(0.75f, 1f, 0.6f, 0.8f));

            // Small ring of arrows around the disc (matches the "Upow-Spots" callout on the reference map).
            arrowRing = new GameObject("Ring").transform;
            arrowRing.SetParent(transform, false);
            arrowRing.localPosition = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                float ang = i * 90f;
                float rad = ang * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * 0.9f + new Vector3(0f, 0.4f, 0f);
                var arrow = PrimitiveFactory.Part(PrimitiveType.Cube, arrowRing,
                    pos, new Vector3(0.15f, 0.30f, 0.15f), darkGreen);
                arrow.transform.localRotation = Quaternion.Euler(0f, ang + 45f, 0f);
            }

            // Text label above the pad.
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            label = labelGO.AddComponent<TextMesh>();
            label.fontSize = 38;
            label.characterSize = 0.075f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.15f, 0.15f, 0.15f);
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                label.font = font;
                var mr = labelGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            labelGO.AddComponent<Billboard>();
            RefreshLabel();
        }

        private int GetCurrentLevel()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return 1;
            switch (PadKind)
            {
                case Kind.PlayerSpeed: return gm.Player.SpeedLevel;
                case Kind.PlayerCarry: return gm.Player.StackLevel;
                default: return gm.CropSpeedLevel;
            }
        }

        private int GetNextCost()
        {
            int lvl = GetCurrentLevel();
            int[] costs = PadKind switch
            {
                Kind.PlayerSpeed => SpeedCosts,
                Kind.PlayerCarry => CarryCosts,
                _                => CropCosts,
            };
            int idx = lvl - 1;
            if (idx < 0 || idx >= costs.Length) return -1;
            return costs[idx];
        }

        private string PadTitle => PadKind switch
        {
            Kind.PlayerSpeed => "PLAYER SPEED",
            Kind.PlayerCarry => "CARRY",
            _                => "CROP SPEED",
        };

        private void RefreshLabel()
        {
            if (label == null) return;
            int cost = GetNextCost();
            int lvl = GetCurrentLevel();
            label.text = cost < 0
                ? $"{PadTitle}\nMAX (Lv {lvl})"
                : $"{PadTitle}\nLv {lvl}  →  $ {cost}";
        }

        private void Update()
        {
            bobT += Time.deltaTime * 3f;
            if (arrowRing != null)
                arrowRing.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bobT)) * 0.15f, 0f);

            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null || gm.Economy == null) return;

            int nextCost = GetNextCost();
            if (nextCost < 0) { RefreshLabel(); return; }

            Vector3 playerPos = gm.Player.transform.position;
            if (Vector3.Distance(playerPos, transform.position) > 1.5f) { dwell = 0f; return; }

            if ((playerPos - lastPlayerPos).magnitude > 2.5f * Time.deltaTime)
                dwell = 0f;
            lastPlayerPos = playerPos;

            dwell += Time.deltaTime;
            if (dwell < 0.5f) return;

            // Charge the full cost when the player has stood long enough, then apply.
            if (gm.Economy.PlayerCash < nextCost) { RefreshLabel(); return; }
            gm.Economy.PlayerCash -= nextCost;
            dwell = 0f;

            switch (PadKind)
            {
                case Kind.PlayerSpeed:
                    gm.Player.ApplySpeedLevel(gm.Player.SpeedLevel + 1);
                    break;
                case Kind.PlayerCarry:
                    gm.Player.ApplyStackLevel(gm.Player.StackLevel + 1);
                    break;
                default:
                    gm.BumpCropSpeed();
                    break;
            }
            AudioFx.Purchase();
            RefreshLabel();
        }
    }
}
