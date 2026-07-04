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
                transform.position = Vector3.Lerp(PickupSpot.position, SpawnSpot.position, driveProgress);
                transform.rotation = Quaternion.LookRotation(SpawnSpot.position - PickupSpot.position);
                if (driveProgress >= 1f)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                if (Order == null || Order.IsExpired || Order.IsFulfilled)
                {
                    isDrivingOut = true;
                    if (uiBubble != null) uiBubble.SetActive(false);
                }
            }
        }
    }
}
