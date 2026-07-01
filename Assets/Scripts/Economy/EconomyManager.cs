using System.Collections.Generic;
using MiniMart.Core;
using MiniMart.Catalog;

namespace MiniMart.Economy
{
    /// <summary>
    /// Owns current sell prices (base + manual overrides + active offers) and player cash.
    /// Enforces the design rule that no full basket/bundle ever totals under $1.
    /// </summary>
    public class EconomyManager
    {
        public float PlayerCash = 0f;

        private readonly Dictionary<ItemType, float> manualPrices = new Dictionary<ItemType, float>();
        private readonly Dictionary<ItemType, float> activeOfferPercent = new Dictionary<ItemType, float>();

        public float GetUnitPrice(ItemType item)
        {
            float price = manualPrices.TryGetValue(item, out var manual) ? manual : PriceCatalog.BasePrice[item];
            if (activeOfferPercent.TryGetValue(item, out var discount))
                price *= (1f - discount / 100f);
            return price;
        }

        /// <summary>Player-driven price adjustment (Section 1: "adjust prices").</summary>
        public void SetManualPrice(ItemType item, float newPrice) => manualPrices[item] = System.Math.Max(0.05f, newPrice);

        /// <summary>Player-driven promotional offer (Section 1: "create offers").</summary>
        public void CreateOffer(ItemType item, float discountPercent) =>
            activeOfferPercent[item] = System.Math.Clamp(discountPercent, 0f, 90f);

        public void ClearOffer(ItemType item) => activeOfferPercent.Remove(item);

        /// <summary>Computes a basket total and applies the $1 floor rule before charging.</summary>
        public float QuoteBasket(IDictionary<ItemType, int> basket)
        {
            float total = 0f;
            foreach (var kv in basket)
                total += GetUnitPrice(kv.Key) * kv.Value;
            return PriceCatalog.ApplyBundleFloor(total);
        }

        public void Deposit(float amount) => PlayerCash += amount;

        public bool TrySpend(int amount)
        {
            if (PlayerCash < amount) return false;
            PlayerCash -= amount;
            return true;
        }
    }
}
