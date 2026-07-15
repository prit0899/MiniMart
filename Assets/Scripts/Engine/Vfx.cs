using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Thin wrapper over the Cartoon FX Remaster prefabs staged in
    /// Assets/Resources/VFX/. Every effect is optional: a missing prefab
    /// just no-ops (with the existing Emote/AudioFx feedback still firing),
    /// so the game degrades gracefully if the pack is removed.
    /// CFXR prefabs self-destruct when done, but a safety Destroy is added
    /// for any that loop.
    /// </summary>
    public static class Vfx
    {
        private static bool loaded;
        private static GameObject poofPrefab, fireworkPrefab, starsPrefab;

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            poofPrefab     = Resources.Load<GameObject>("VFX/PurchasePoof");
            fireworkPrefab = Resources.Load<GameObject>("VFX/LevelUpFirework");
            starsPrefab    = Resources.Load<GameObject>("VFX/TutorialStars");
        }

        private static void Spawn(GameObject prefab, Vector3 pos, float lifeSeconds)
        {
            if (prefab == null) return;
            var go = Object.Instantiate(prefab, pos, Quaternion.identity);
            Object.Destroy(go, lifeSeconds); // safety net for looping variants
        }

        /// <summary>White cartoon poof — new building/pad purchased.</summary>
        public static void Poof(Vector3 pos) { Load(); Spawn(poofPrefab, pos + Vector3.up * 0.5f, 4f); }

        /// <summary>Firework burst above the player — store level-up.</summary>
        public static void LevelUp(Vector3 pos) { Load(); Spawn(fireworkPrefab, pos + Vector3.up * 2.5f, 6f); }

        /// <summary>Gentle falling stars — tutorial completion.</summary>
        public static void Stars(Vector3 pos) { Load(); Spawn(starsPrefab, pos + Vector3.up * 2f, 6f); }
    }
}
