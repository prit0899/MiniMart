using UnityEngine;
using MiniMart.Characters;

namespace MiniMart.UI
{
    /// <summary>
    /// Reference-parity carry stack: a single-file TALL COLUMN of colored cubes rising
    /// straight up above the character's head — the silhouette-defining feature of
    /// "My Mini Mart"-style games (previously we drew a 2x2xN pyramid, which read
    /// wrong: the reference stack is thin and towers). "MAX" pill appears above the
    /// stack once carry == capacity.
    /// </summary>
    public class CarryVisual : MonoBehaviour
    {
        public GameObject ItemIconPrefab;
        public Vector3 StackOffset = new Vector3(0f, 1.7f, 0f);
        public float IconSpacing = 0.22f;   // vertical gap per cube (single column)
        public float IconSize    = 0.32f;   // cube edge size; tuned so 15+ items still fit on-screen

        private CharacterBase character;
        // Player carry reaches 44 at max level — pool that many icons.
        private GameObject[] icons = new GameObject[44];
        private Core.ItemType[] iconTypes = new Core.ItemType[44];
        private int lastCount = -1;
        private Color lastColor = Color.clear;

        // Reference-style "MAX" pill (black rounded chip with white text).
        private GameObject maxPill;
        private TextMesh maxLabel;

        private void Awake()
        {
            character = GetComponent<CharacterBase>();

            for (int i = 0; i < icons.Length; i++)
            {
                if (ItemIconPrefab != null)
                {
                    icons[i] = Instantiate(ItemIconPrefab, transform);
                }
                else
                {
                    icons[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    icons[i].transform.SetParent(transform, false);
                    icons[i].transform.localScale = new Vector3(IconSize, IconSize, IconSize);

                    var col = icons[i].GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    var mr = icons[i].GetComponent<MeshRenderer>();
                    if (mr != null)
                        mr.material = Engine.PrimitiveFactory.NewColoredMaterial(Color.white);
                }
                icons[i].SetActive(false);
                iconTypes[i] = (Core.ItemType)(-1);
            }

            BuildMaxPill();
        }

        private void BuildMaxPill()
        {
            maxPill = new GameObject("MaxPill");
            maxPill.transform.SetParent(transform, false);

            // Black rounded background chip (approximated by a flat dark cube).
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "PillBack";
            back.transform.SetParent(maxPill.transform, false);
            back.transform.localScale = new Vector3(0.85f, 0.28f, 0.05f);
            back.transform.localPosition = Vector3.zero;
            Destroy(back.GetComponent<Collider>());
            var backMR = back.GetComponent<MeshRenderer>();
            if (backMR != null)
                backMR.material = Engine.PrimitiveFactory.NewColoredMaterial(new Color(0.08f, 0.08f, 0.08f, 0.95f));

            // White "MAX" text over the chip.
            var textGO = new GameObject("MaxText");
            textGO.transform.SetParent(maxPill.transform, false);
            textGO.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            maxLabel = textGO.AddComponent<TextMesh>();
            maxLabel.text = "MAX";
            maxLabel.fontSize = 40;
            maxLabel.characterSize = 0.075f;
            maxLabel.anchor = TextAnchor.MiddleCenter;
            maxLabel.alignment = TextAlignment.Center;
            maxLabel.color = Color.white;
            var font = Engine.HUDBuilder.UIFont;
            if (font != null)
            {
                maxLabel.font = font;
                var tmr = textGO.GetComponent<MeshRenderer>();
                if (tmr != null) tmr.material = font.material;
            }

            maxPill.AddComponent<MiniMart.Billboard>();
            maxPill.SetActive(false);
        }

        private void LateUpdate()
        {
            if (character == null) return;
            int count = Mathf.Min(character.CarryCount, icons.Length);

            // Build per-item color list for the player (mixed stack support).
            // For NPCs (no PlayerInteraction), fall back to the single CarryColor.
            System.Collections.Generic.List<Core.ItemType> itemList = null;
            var pi = GetComponent<PlayerInteraction>();
            if (pi != null)
                itemList = pi.GetCarriedItems();

            // Detect whether we need to redraw: count changed, color changed, or
            // the item composition changed (for mixed stacks).
            bool needsRedraw = count != lastCount || character.CarryColor != lastColor;
            if (!needsRedraw && itemList != null)
            {
                // Quick hash: if the list length differs from lastCount we already
                // know we need to redraw. Otherwise skip — the full per-frame
                // comparison is cheap enough at ≤44 items.
                needsRedraw = true; // always redraw when player has a mixed stack
            }
            if (!needsRedraw) return;

            lastCount = count;
            lastColor = character.CarryColor;

            // Single tall column: cube i sits at StackOffset + (0, i*spacing, 0).
            for (int i = 0; i < icons.Length; i++)
            {
                bool show = i < count;
                if (!show)
                {
                    if (icons[i] != null) icons[i].SetActive(false);
                    continue;
                }

                // If we know the exact item (Player), and the mesh is missing or out of date:
                if (itemList != null && i < itemList.Count)
                {
                    Core.ItemType type = itemList[i];
                    if (iconTypes[i] != type || icons[i] == null)
                    {
                        if (icons[i] != null) Destroy(icons[i]);
                        icons[i] = Engine.PrimitiveFactory.ItemMesh(type, transform, Vector3.zero, IconSize * 2f);
                        iconTypes[i] = type;
                    }
                }
                else if (icons[i] == null) // fallback for NPCs without itemList
                {
                    icons[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    icons[i].transform.SetParent(transform, false);
                    icons[i].transform.localScale = new Vector3(IconSize, IconSize, IconSize);
                    Destroy(icons[i].GetComponent<Collider>());
                    var mat = Engine.PrimitiveFactory.NewColoredMaterial(Color.white);
                    icons[i].GetComponent<MeshRenderer>().material = mat;
                    iconTypes[i] = (Core.ItemType)(-1);
                }

                icons[i].SetActive(true);
                icons[i].transform.localPosition = StackOffset + new Vector3(0f, i * IconSpacing, 0f);

                // Slight wobble for cubes (skip wobble on complex meshes to avoid distortion, or apply lightly)
                float jitter = 1f + Mathf.Sin(i * 0.9f) * 0.05f;
                if (itemList == null || i >= itemList.Count)
                {
                    icons[i].transform.localScale = new Vector3(IconSize * jitter, IconSize, IconSize * jitter);
                    var mr = icons[i].GetComponent<MeshRenderer>();
                    if (mr != null) mr.material.color = character.CarryColor;
                }
                else
                {
                    // ItemMesh is already scaled, just apply local rotation jitter for juice
                    icons[i].transform.localRotation = Quaternion.Euler(0f, i * 15f + Mathf.Sin(Time.time * 2f + i) * 10f, 0f);
                }
            }

            if (maxPill != null)
            {
                bool full = character.CarryCapacity > 0 && character.CarryCount >= character.CarryCapacity;
                maxPill.SetActive(full);
                if (full)
                {
                    // Sit above the top item
                    maxPill.transform.localPosition =
                        StackOffset + new Vector3(0f, count * IconSpacing + 0.35f, 0f);
                }
            }
        }
    }
}
