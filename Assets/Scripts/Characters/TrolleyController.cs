using UnityEngine;
using MiniMart.Core;
using MiniMart.AI;

namespace MiniMart.Characters
{
    /// <summary>
    /// Trolley logic. A Buyer auto-attaches this component when their basket reaches 5+ items.
    /// The trolley prefab is a child GameObject with its own SpriteRenderer; this script keeps
    /// it aligned and toggles it based on BagType.
    /// </summary>
    public class TrolleyController : MonoBehaviour
    {
        [Header("Renderers — assign both in inspector")]
        public SpriteRenderer HandCarrySprite; // the buyer's hand-carry pose sprite
        public SpriteRenderer TrolleySprite;   // trolley overlay sprite

        private Buyer buyer;

        private void Awake() => buyer = GetComponent<Buyer>();

        private void Update()
        {
            if (buyer == null) return;
            bool trolley = buyer.BagType == BagType.Trolley;
            if (TrolleySprite != null) TrolleySprite.enabled = trolley;
            if (HandCarrySprite != null) HandCarrySprite.enabled = !trolley;
        }
    }
}
