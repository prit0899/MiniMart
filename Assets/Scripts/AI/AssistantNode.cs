using UnityEngine;
using MiniMart.Core;
using MiniMart.Production;
using MiniMart.Runtime;

namespace MiniMart.AI
{
    /// <summary>
    /// Assistant Node (new map spec §Agriculture East). A small yellow robot
    /// that sits on the corn field and auto-harvests corn on a timer, dropping
    /// stock straight into the Corn storage. Cheaper than a full purple worker:
    /// unlocked with a $250 purchase pad, no upgrade tracks, no speed level.
    /// Visual: small yellow harvester box with a spinning cone on top.
    /// </summary>
    public class AssistantNode : MonoBehaviour
    {
        public CornField cornField;
        [Tooltip("Seconds between auto-harvest ticks.")]
        public float HarvestIntervalSeconds = 2.5f;
        [Tooltip("Corn cobs pulled per tick (capped by CornField.TotalRipe).")]
        public int PerTick = 2;

        private float timer;
        private Transform coneTop;

        private void Start()
        {
            BuildVisual();
        }

        private void BuildVisual()
        {
            var yellow = new Color(0.98f, 0.78f, 0.20f);
            var chassis = new Color(0.85f, 0.65f, 0.15f);
            var wheels = new Color(0.15f, 0.15f, 0.18f);

            Engine.PrimitiveFactory.Part(PrimitiveType.Cube, transform,
                new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.6f, 1.2f), yellow);
            Engine.PrimitiveFactory.Part(PrimitiveType.Cube, transform,
                new Vector3(0f, 0.15f, 0f), new Vector3(0.95f, 0.15f, 1.25f), chassis);

            // Wheels
            Engine.PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3(-0.5f, 0.18f,  0.45f), new Vector3(0.30f, 0.10f, 0.30f), wheels);
            Engine.PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3( 0.5f, 0.18f,  0.45f), new Vector3(0.30f, 0.10f, 0.30f), wheels);
            Engine.PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3(-0.5f, 0.18f, -0.45f), new Vector3(0.30f, 0.10f, 0.30f), wheels);
            Engine.PrimitiveFactory.Part(PrimitiveType.Cylinder, transform,
                new Vector3( 0.5f, 0.18f, -0.45f), new Vector3(0.30f, 0.10f, 0.30f), wheels);

            // Spinning cone antenna (turns to show it's "thinking")
            var coneGO = new GameObject("Cone");
            coneGO.transform.SetParent(transform, false);
            coneGO.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            coneTop = coneGO.transform;
            var cone = Engine.PrimitiveFactory.Part(PrimitiveType.Cube, coneGO.transform,
                Vector3.zero, new Vector3(0.15f, 0.35f, 0.15f), Color.white);
            cone.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void Update()
        {
            if (coneTop != null)
                coneTop.Rotate(Vector3.up, 90f * Time.deltaTime);

            var gm = GameManager.Instance;
            if (gm == null || cornField == null) return;
            var inv = gm.Inventory;
            if (inv == null) return;

            timer += Time.deltaTime;
            if (timer < HarvestIntervalSeconds) return;
            timer = 0f;

            if (cornField.TotalRipe() <= 0) return;
            var stock = inv.Stocks.TryGetValue(ItemType.Corn, out var s) ? s : null;
            if (stock == null || stock.SpaceLeft <= 0) return;

            int want = Mathf.Min(PerTick, stock.SpaceLeft);
            int got = cornField.Harvest(want);
            if (got > 0) inv.Deposit(ItemType.Corn, got);
        }
    }
}
