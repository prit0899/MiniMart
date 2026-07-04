using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Physical pile of cash on/near a counter (reference flow: money stacks on the till,
    /// the player walks over them to collect). Checkout no longer auto-banks revenue —
    /// it grows one of these instead. Collecting grants the cash AND the store XP.
    /// </summary>
    public class MoneyStack : MonoBehaviour
    {
        public float Value { get; private set; }

        private TextMesh label;
        private Transform visual;
        private float bobT;

        /// <summary>Add revenue at a spot: merges into a nearby existing stack or spawns one.</summary>
        public static void SpawnOrMerge(Vector3 position, float amount)
        {
            if (amount <= 0f) return;

            foreach (var existing in Object.FindObjectsByType<MoneyStack>(FindObjectsSortMode.None))
            {
                if (Vector3.Distance(existing.transform.position, position) < 2f)
                {
                    existing.Add(amount);
                    return;
                }
            }

            var go = new GameObject("MoneyStack");
            go.transform.position = position;
            go.AddComponent<MoneyStack>().Add(amount);
        }

        private void Awake()
        {
            // Pile of flat green "bills".
            visual = new GameObject("Pile").transform;
            visual.SetParent(transform, false);
            var green = new Color(0.25f, 0.75f, 0.30f);
            var dark  = new Color(0.18f, 0.55f, 0.22f);
            PrimitiveFactory.Part(PrimitiveType.Cube, visual, new Vector3(0, 0.07f, 0),      new Vector3(0.55f, 0.14f, 0.34f), green);
            PrimitiveFactory.Part(PrimitiveType.Cube, visual, new Vector3(0.08f, 0.2f, 0.05f), new Vector3(0.55f, 0.12f, 0.34f), dark);
            PrimitiveFactory.Part(PrimitiveType.Cube, visual, new Vector3(-0.05f, 0.31f, -0.04f), new Vector3(0.5f, 0.1f, 0.3f), green);

            var badgeGO = new GameObject("Value");
            badgeGO.transform.SetParent(transform, false);
            badgeGO.transform.localPosition = new Vector3(0, 1.0f, 0);
            label = badgeGO.AddComponent<TextMesh>();
            label.fontSize = 40;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.6f, 1f, 0.6f);
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                label.font = font;
                var mr = badgeGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            badgeGO.AddComponent<Billboard>();
        }

        public void Add(float amount)
        {
            Value += amount;
            if (label != null) label.text = $"${Value:F0}";
        }

        private void Update()
        {
            // Gentle bob so the pile reads as collectible.
            bobT += Time.deltaTime * 3f;
            if (visual != null)
                visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(bobT)) * 0.06f, 0);

            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;

            if (Vector3.Distance(gm.Player.transform.position, transform.position) < 1.4f)
            {
                gm.Economy.Deposit(Value);
                gm.AddStoreXp(Mathf.Max(1, Mathf.CeilToInt(Value)));
                Destroy(gameObject);
            }
        }
    }
}
