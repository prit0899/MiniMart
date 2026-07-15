using UnityEngine;

namespace UMA
{
    /// <summary>
    /// Unity 6000.5 made Object.GetInstanceID() error-obsolete in favour of
    /// GetEntityId() (EntityId, with implicit int conversions). This extension
    /// gives the UMA code one stable call site that compiles on both API
    /// generations; added as part of the MiniMart Unity-6000.5 compat pass.
    /// </summary>
    internal static class UnityIdCompat
    {
        public static int GetStableInstanceId(this Object obj)
        {
#if UNITY_6000_5_OR_NEWER
            // The EntityId->int implicit cast is itself error-obsolete in
            // 6000.5, so use GetHashCode(): equal EntityIds hash equally,
            // which is all the UMA callers need — they only use this value
            // as a session-local identity key (HashSet membership in
            // UMAGeneratorBase.CreatedAvatars, duplicate-asset detection in
            // DynamicUMADnaAsset), never as a persistent or reversible ID.
            return obj.GetEntityId().GetHashCode();
#else
            return obj.GetInstanceID();
#endif
        }
    }
}
