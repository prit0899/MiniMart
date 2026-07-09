using System.Collections.Generic;
using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Physical pile of cash on/near a counter — reference flow: money stacks on the
    /// till and the player walks over them to collect. Rendered as a 3D grid of
    /// stacked green "bills" (thin flat cubes tiled in rows/columns) that visibly
    /// GROWS as revenue accumulates — the reference silhouette. Collecting grants
    /// cash + store XP.
    /// </summary>
    public class MoneyStack : MonoBehaviour
    {
        public float Value { get; private set; }

        private TextMesh label;
        private Transform pile;
        private readonly List<GameObject> bills = new List<GameObject>();
        private float bobT;

        // Layout: rows of 3 bills wide x 3 deep, up to 5 layers tall.
        // Each visible bill represents ~$5 of revenue on the counter.
        private const int RowW = 3;
        private const int RowD = 3;
        private const int MaxLayers = 5;
        private const float DollarsPerBill = 5f;
        private const float BillW = 0.32f;
        private const float BillD = 0.20f;
        private const float BillH = 0.06f;

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
            pile = new GameObject("Pile").transform;
            pile.SetParent(transform, false);

            var labelGO = new GameObject("Value");
            labelGO.transform.SetParent(transform, false);
            labelGO.transform.localPosition = new Vector3(0, 1.4f, 0);
            label = labelGO.AddComponent<TextMesh>();
            label.fontSize = 40;
            label.characterSize = 0.075f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.15f, 0.5f, 0.15f);
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                label.font = font;
                var mr = labelGO.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            labelGO.AddComponent<Billboard>();
        }

        public void Add(float amount)
        {
            Value += amount;
            RebuildPile();
            if (label != null) label.text = $"${Value:F0}";
        }

        private void RebuildPile()
        {
            int desired = Mathf.Clamp(
                Mathf.CeilToInt(Value / DollarsPerBill),
                1,
                RowW * RowD * MaxLayers);

            var green = new Color(0.25f, 0.72f, 0.30f);
            var dark  = new Color(0.16f, 0.52f, 0.22f);
            while (bills.Count < desired)
            {
                int i = bills.Count;
                int layer = i / (RowW * RowD);
                int inLayer = i % (RowW * RowD);
                int col = inLayer % RowW;
                int row = inLayer / RowW;

                Color c = (i % 2 == 0) ? green : dark;
                float xJitter = ((row + layer) % 2 == 0) ? 0.02f : -0.02f;

                var bill = PrimitiveFactory.Part(PrimitiveType.Cube, pile,
                    new Vector3(
                        (col - 1) * BillW * 1.05f + xJitter,
                        BillH * 0.5f + layer * BillH * 1.05f,
                        (row - 1) * BillD * 1.05f),
                    new Vector3(BillW, BillH, BillD),
                    c);
                bill.name = $"Bill_{i}";
                bills.Add(bill);
            }
            for (int i = 0; i < bills.Count; i++)
                bills[i].SetActive(i < desired);
        }

        private void Update()
        {
            bobT += Time.deltaTime * 3f;
            if (pile != null)
                pile.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(bobT)) * 0.05f, 0);

            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null) return;

            if (Vector3.Distance(gm.Player.transform.position, transform.position) < 1.4f)
            {
                gm.Economy.Deposit(Value);
                gm.AddStoreXp(Mathf.Max(1, Mathf.CeilToInt(Value)));
                AudioFx.Coin();
                Destroy(gameObject);
            }
        }
    }
}
