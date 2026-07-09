using System;
using UnityEngine;
using Genies.Sdk;

namespace MiniMart.Characters
{
    /// <summary>
    /// Swaps the player's primitive chibi body for the user's own Genies avatar
    /// (Genies Avatar SDK, Packages/com.genies.avatar-sdk.client).
    ///
    /// Load is async and fully defensive: while the avatar streams in — and on
    /// ANY failure (not logged in with no default available, no network, SDK
    /// not bootstrapped via Tools > Genies > SDK Bootstrap Wizard) — the
    /// existing primitive visual stays, so the game is always playable.
    /// Per the SDK sample, LoadAvatarAsync with User options serves a default
    /// avatar when nobody is logged in, so this generally works out of the box.
    /// </summary>
    public sealed class GeniesPlayerSkin : MonoBehaviour
    {
        private const float TargetHeight = 1.5f; // chibi height the camera/badges are tuned around

        private async void Start()
        {
            try
            {
                var avatar = await AvatarSdk.LoadAvatarAsync(new LoadAvatarOptions.User
                {
                    AvatarName = "PlayerAvatar",
                    Parent = transform,
                });

                if (avatar == null || avatar.Root == null)
                {
                    Debug.LogWarning("[GeniesPlayerSkin] Avatar load returned null — keeping primitive body.");
                    return;
                }

                var avatarGO = avatar.Root.gameObject;
                avatarGO.transform.localPosition = Vector3.zero;
                avatarGO.transform.localRotation = Quaternion.identity;

                // Normalize the avatar to the game's character height so the
                // follow camera, carry stacks, and world badges keep working.
                var bounds = new Bounds(avatarGO.transform.position, Vector3.zero);
                foreach (var r in avatarGO.GetComponentsInChildren<Renderer>())
                    bounds.Encapsulate(r.bounds);
                if (bounds.size.y > 0.01f)
                    avatarGO.transform.localScale *= TargetHeight / bounds.size.y;

                // Only hide the primitive body once the real avatar is standing.
                var primitive = transform.Find("Visual");
                if (primitive != null) primitive.gameObject.SetActive(false);

                Debug.Log("[GeniesPlayerSkin] Genies avatar loaded — primitive body replaced.");
            }
            catch (Exception ex)
            {
                // Never let avatar-cloud problems break the game loop.
                Debug.LogWarning($"[GeniesPlayerSkin] Avatar load failed ({ex.Message}) — keeping primitive body.");
            }
        }
    }
}
