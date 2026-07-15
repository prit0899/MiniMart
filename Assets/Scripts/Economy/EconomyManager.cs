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

        /// <summary>Player-driven price adjustment, clamped to the GDD 6.1 band of ±50%
        /// around base price (with an absolute $0.05 floor). Previously unclamped — any
        /// code path could set arbitrary prices and break buyer elasticity.</summary>
        public void SetManualPrice(ItemType item, float newPrice)
        {
            float basePrice = PriceCatalog.BasePrice.TryGetValue(item, out var bp) ? bp : 1f;
            float min = System.Math.Max(0.05f, basePrice * 0.5f);
            float max = basePrice * 1.5f;
            manualPrices[item] = System.Math.Clamp(newPrice, min, max);
        }

        /// <summary>Player-driven promotional offer (Section 1: "create offers").</summary>
        public void CreateOffer(ItemType item, float discountPercent) =>
            activeOfferPercent[item] = System.Math.Clamp(discountPercent, 0f, 90f);

        public void ClearOffer(ItemType item) => activeOfferPercent.Remove(item);

        /// <summary>Manual price overrides, exposed read-only for the save system.</summary>
        public IReadOnlyDictionary<ItemType, float> ManualPrices => manualPrices;

        /// <summary>GDD 8.3: an item priced more than +20% over base risks buyer rejection.</summary>
        public bool IsOverpriced(ItemType item) =>
            PriceCatalog.BasePrice.TryGetValue(item, out var bp) && GetUnitPrice(item) > bp * 1.2f;

        /// <summary>GDD 8.3: any ≥10% discount (offer or manual price cut) boosts buyer traffic.</summary>
        public bool AnyDiscountActive()
        {
            foreach (var kv in activeOfferPercent)
                if (kv.Value >= 10f) return true;
            foreach (var kv in manualPrices)
                if (PriceCatalog.BasePrice.TryGetValue(kv.Key, out var bp) && kv.Value <= bp * 0.9f) return true;
            return false;
        }

        /// <summary>Computes a basket total and applies the $1 floor rule before charging.</summary>
        public float QuoteBasket(IDictionary<ItemType, int> basket)
        {
            float total = 0f;
            foreach (var kv in basket)
                total += GetUnitPrice(kv.Key) * kv.Value;
            return PriceCatalog.ApplyBundleFloor(total);
        }

        public void Deposit(float amount) => PlayerCash += amount;

        public bool TrySpend(float amount)
        {
            if (PlayerCash < amount) return false;
            PlayerCash -= amount;
            return true;
        }
    }
}
