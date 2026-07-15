using UnityEngine;
using MiniMart.Engine;
using MiniMart.Core;
using MiniMart.Catalog;

namespace MiniMart.Characters
{
    public class DeliveryVan : MonoBehaviour
    {
        public PhoneOrder Order;
        public Transform PickupSpot;
        public Transform SpawnSpot;
        
        private bool isDrivingIn;
        private bool isDrivingOut;
        private float driveProgress;
        
        private GameObject uiBubble;
        private TextMesh textMesh;

        public void Init(PhoneOrder order, Transform pickup, Transform spawn)
        {
            Order = order;
            PickupSpot = pickup;
            SpawnSpot = spawn;
            
            transform.position = spawn.position;
            isDrivingIn = true;
            
            BuildUI();
            RefreshUI();
        }

        private void BuildUI()
        {
            uiBubble = new GameObject("OrderBubble");
            uiBubble.transform.SetParent(transform, false);
            uiBubble.transform.localPosition = new Vector3(0, 3f, 0);
            
            textMesh = uiBubble.AddComponent<TextMesh>();
            textMesh.fontSize = 40;
            textMesh.characterSize = 0.05f;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                textMesh.font = font;
                var mr = uiBubble.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            uiBubble.AddComponent<Billboard>();
        }

        public void RefreshUI()
        {
            if (Order == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("WANTED:");
            foreach(var kv in Order.Items)
                sb.AppendLine($"{kv.Value}x {kv.Key}");
            
            if (!Order.IsExpired && !Order.IsFulfilled)
                sb.AppendLine($"Time: {Mathf.CeilToInt(Order.TimeRemaining)}s");
            textMesh.text = sb.ToString();
        }

        public bool Needs(ItemType item)
        {
            return Order != null && !Order.IsFulfilled && !Order.IsExpired && Order.Items.ContainsKey(item) && Order.Items[item] > 0;
        }

        public void LoadItem(ItemType item, int amount)
        {
            if (!Needs(item)) return;
            Order.Items[item] -= amount;
            if (Order.Items[item] <= 0)
                Order.Items.Remove(item);

            // Fully loaded: pay out and close the order (drives the van home too,
            // since Update watches Order.IsFulfilled).
            if (Order.Items.Count == 0)
                GameManager.Instance?.PhoneOrderManager?.CompleteByVan(Order);
        }

        private void Update()
        {
            RefreshUI();

            // Cancelled-order fast path: if the order was dismissed / expired / fulfilled
            // WHILE the van was still driving in, turn it around immediately from wherever
            // it currently is. Previously we only checked this in the idle "else" branch,
            // so a dismissed truck kept driving all the way to the pickup spot first,
            // leaving the impression that the truck "didn't disappear."
            if (!isDrivingOut && (Order == null || Order.IsExpired || Order.IsFulfilled || Order.IsDismissed))
            {
                isDrivingIn = false;
                isDrivingOut = true;
                // Preserve where we currently are along the drive-in path so the
                // return-leg lerp starts from the van's real position, not from
                // PickupSpot (which would visually teleport it forward first).
                driveProgress = 0f;
                returnStart = transform.position;
                if (uiBubble != null) uiBubble.SetActive(false);
            }

            if (isDrivingIn)
            {
                driveProgress += Time.deltaTime * 0.5f;
                transform.position = Vector3.Lerp(SpawnSpot.position, PickupSpot.position, driveProgress);
                transform.rotation = Quaternion.LookRotation(PickupSpot.position - SpawnSpot.position);
                if (driveProgress >= 1f)
                {
                    isDrivingIn = false;
                    driveProgress = 0f;
                }
            }
            else if (isDrivingOut)
            {
                driveProgress += Time.deltaTime * 0.5f;
                var from = returnStart == default ? PickupSpot.position : returnStart;
                transform.position = Vector3.Lerp(from, SpawnSpot.position, driveProgress);
                transform.rotation = Quaternion.LookRotation(SpawnSpot.position - from);
                if (driveProgress >= 1f)
                {
                    Destroy(gameObject);
                }
            }
        }

        private Vector3 returnStart;
    }
}
