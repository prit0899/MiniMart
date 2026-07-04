using UnityEngine;
using MiniMart.Characters;

namespace MiniMart.UI
{
    /// <summary>
    /// Attaches to any character that carries items. Renders a stack of item icons above
    /// their sprite to show CarryCount / CarryCapacity. Uses simple GameObject pooling.
    /// </summary>
    public class CarryVisual : MonoBehaviour
    {
        public GameObject ItemIconPrefab;
        public Vector3 StackOffset = new Vector3(0f, 0.6f, 0f);
        public float IconSpacing = 0.25f;

        private CharacterBase character;
        private GameObject[] icons = new GameObject[24]; // player carry reaches 44; visual clamps here
        private int lastCount = -1;
        private Color lastColor = Color.clear;
        private TextMesh maxBadge; // reference-style "MAX" over a full stack

        private void Awake()
        {
            character = GetComponent<CharacterBase>();
            // Pre-instantiate max icons (hidden).
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
                    icons[i].transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); // small item boxes
                    
                    var col = icons[i].GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    
                    var mr = icons[i].GetComponent<MeshRenderer>();
                    if (mr != null)
                        mr.material = Engine.PrimitiveFactory.NewColoredMaterial(Color.white);
                }
                icons[i].SetActive(false);
            }

            // "MAX" badge shown when the carry stack is full (reference UI).
            var badgeGO = new GameObject("MaxBadge");
            badgeGO.transform.SetParent(transform, false);
            maxBadge = badgeGO.AddComponent<TextMesh>();
            maxBadge.text = "MAX";
            maxBadge.fontSize = 44;
            maxBadge.characterSize = 0.08f;
            maxBadge.anchor = TextAnchor.MiddleCenter;
            maxBadge.alignment = TextAlignment.Center;
            maxBadge.color = new Color(1f, 0.35f, 0.25f);
            var font = Engine.HUDBuilder.UIFont;
            if (font != null)
            {
                maxBadge.font = font;
                var bmr = badgeGO.GetComponent<MeshRenderer>();
                if (bmr != null) bmr.material = font.material;
            }
            badgeGO.AddComponent<MiniMart.Billboard>();
            badgeGO.SetActive(false);
        }

        private void LateUpdate()
        {
            if (character == null) return;
            int count = Mathf.Min(character.CarryCount, icons.Length);
            if (count == lastCount && character.CarryColor == lastColor) return;
            lastCount = count;
            lastColor = character.CarryColor;

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null) continue;
                bool show = i < count;
                icons[i].SetActive(show);
                if (show)
                {
                    icons[i].transform.localPosition = StackOffset + Vector3.up * (i * IconSpacing);
                    var mr = icons[i].GetComponent<MeshRenderer>();
                    if (mr != null)
                        mr.material.color = character.CarryColor;
                }
            }

            if (maxBadge != null)
            {
                bool full = character.CarryCapacity > 0 && character.CarryCount >= character.CarryCapacity;
                maxBadge.gameObject.SetActive(full);
                if (full)
                    maxBadge.transform.localPosition = StackOffset + Vector3.up * (count * IconSpacing + 0.5f);
            }
        }
    }
}
