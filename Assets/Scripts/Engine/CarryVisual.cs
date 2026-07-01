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
        private GameObject[] icons = new GameObject[10];
        private int lastCount = -1;

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
                    {
                        mr.material = new Material(Shader.Find("Standard"));
                        mr.material.color = Color.white;
                    }
                }
                icons[i].SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (character == null) return;
            int count = Mathf.Min(character.CarryCount, icons.Length);
            if (count == lastCount) return;
            lastCount = count;

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null) continue;
                bool show = i < count;
                icons[i].SetActive(show);
                if (show)
                    icons[i].transform.localPosition = StackOffset + Vector3.up * (i * IconSpacing);
            }
        }
    }
}
