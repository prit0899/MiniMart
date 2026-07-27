using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// Builds compound 3D shapes from Unity primitives to match the
    /// "My Mini Mall" visual style. Every method is static and self-contained.
    /// </summary>
    public static class PrimitiveFactory
    {
        // ─── Shared material cache ───────────────────────────────────────────
        // "Standard" is guaranteed by GraphicsSettings > Always Included Shaders; the chain
        // below keeps device builds alive even if that list is ever edited.
        public static Shader SafeShader =>
            Shader.Find("Standard")
            ?? Shader.Find("Legacy Shaders/Diffuse")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("UI/Default");

        private static Material _stdMat;
        private static Material StdMat
        {
            get
            {
                if (_stdMat == null)
                {
                    var shader = SafeShader;
                    if (shader != null) _stdMat = new Material(shader);
                }
                return _stdMat;
            }
        }

        // Shared-material cache: the game builds thousands of primitives and a
        // fresh Material per Part() meant hundreds of unique materials — a real
        // draw-call/memory cost on mobile. One material per distinct color is
        // enough. Callers that later tint a renderer do so through the
        // `renderer.material` accessor, which auto-clones per renderer, so the
        // shared instances can never be mutated by gameplay code.
        private static readonly System.Collections.Generic.Dictionary<Color32, Material> matCache =
            new System.Collections.Generic.Dictionary<Color32, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMatCache() => matCache.Clear(); // domain-reload safety

        /// <summary>Device-safe colored material — cached per color. Never throws,
        /// even if shader lookup fails.</summary>
        public static Material NewColoredMaterial(Color color)
        {
            Color32 key = color;
            if (matCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var baseMat = StdMat;
            var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            m.color = color;
            matCache[key] = m;
            return m;
        }

        private static Texture2D _softCircleTex;
        /// <summary>Radial-gradient blob texture so particles render as soft puffs,
        /// not hard white squares (untextured particle quads read as blocky cubes
        /// in every playtest capture).</summary>
        private static Texture2D SoftCircleTex
        {
            get
            {
                if (_softCircleTex == null)
                {
                    const int size = 64;
                    _softCircleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    _softCircleTex.wrapMode = TextureWrapMode.Clamp;
                    var px = new Color32[size * size];
                    float half = (size - 1) * 0.5f;
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                            // Smooth falloff: solid core, feathered edge.
                            float a = Mathf.Clamp01(1f - d);
                            a = a * a * (3f - 2f * a); // smoothstep
                            px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                        }
                    _softCircleTex.SetPixels32(px);
                    _softCircleTex.Apply();
                }
                return _softCircleTex;
            }
        }

        /// <summary>Soft translucent material for steam/smoke particles.</summary>
        public static Material NewParticleMaterial()
        {
            var shader = Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Hidden/InternalErrorShader");
            var m = new Material(shader);
            m.color = new Color(1f, 1f, 1f, 0.6f);
            m.mainTexture = SoftCircleTex;
            return m;
        }

        /// <summary>Create a primitive, strip collider, parent, position, scale, color it.</summary>
        public static GameObject Part(PrimitiveType type, Transform parent,
            Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = type.ToString();
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.material = NewColoredMaterial(color);
            return go;
        }

        // ─── Colors ──────────────────────────────────────────────────────────
        static readonly Color GRASS       = new Color(0.55f, 0.78f, 0.38f);
        static readonly Color STORE_FLOOR = new Color(0.92f, 0.82f, 0.65f);
        static readonly Color WALL_COLOR  = new Color(0.88f, 0.85f, 0.80f);
        static readonly Color WOOD        = new Color(0.55f, 0.38f, 0.18f);
        static readonly Color FENCE       = new Color(0.72f, 0.48f, 0.28f);
        static readonly Color DIRT        = new Color(0.50f, 0.35f, 0.20f);
        static readonly Color TEAL        = new Color(0.45f, 0.88f, 0.82f);
        static readonly Color FLOOR_MAT   = new Color(0.72f, 0.72f, 0.72f);

        /// <summary>Cow pen — a blocky white-and-black cow on a grass patch (reference dairy).</summary>
        public static void CowPen(GameObject owner)
        {
            var t = owner.transform;
            var white = new Color(0.96f, 0.96f, 0.96f);
            var black = new Color(0.15f, 0.15f, 0.15f);
            var pink  = new Color(0.95f, 0.65f, 0.7f);

            // Body + patches
            Part(PrimitiveType.Cube, t, new Vector3(0, 0.55f, 0), new Vector3(1.3f, 0.7f, 0.8f), white);
            Part(PrimitiveType.Cube, t, new Vector3(0.3f, 0.75f, 0.28f), new Vector3(0.4f, 0.35f, 0.3f), black);
            Part(PrimitiveType.Cube, t, new Vector3(-0.35f, 0.5f, -0.28f), new Vector3(0.35f, 0.4f, 0.3f), black);
            // Head + snout
            Part(PrimitiveType.Cube, t, new Vector3(0.8f, 0.85f, 0), new Vector3(0.45f, 0.4f, 0.45f), white);
            Part(PrimitiveType.Cube, t, new Vector3(1.02f, 0.75f, 0), new Vector3(0.2f, 0.18f, 0.3f), pink);
            // Legs
            Part(PrimitiveType.Cube, t, new Vector3(0.45f, 0.15f, 0.25f), new Vector3(0.15f, 0.3f, 0.15f), white);
            Part(PrimitiveType.Cube, t, new Vector3(0.45f, 0.15f, -0.25f), new Vector3(0.15f, 0.3f, 0.15f), white);
            Part(PrimitiveType.Cube, t, new Vector3(-0.45f, 0.15f, 0.25f), new Vector3(0.15f, 0.3f, 0.15f), white);
            Part(PrimitiveType.Cube, t, new Vector3(-0.45f, 0.15f, -0.25f), new Vector3(0.15f, 0.3f, 0.15f), white);
            // Milk pail
            Part(PrimitiveType.Cylinder, t, new Vector3(-0.9f, 0.18f, 0.4f), new Vector3(0.3f, 0.18f, 0.3f), new Color(0.7f, 0.75f, 0.8f));
        }

        /// <summary>Storage depot — the visible drop-off point where the player and farmer
        /// deposit harvested goods (a pallet with stacked crates).</summary>
        public static void StorageDepot(GameObject owner)
        {
            var wood = new Color(0.62f, 0.44f, 0.24f);
            var crate = new Color(0.78f, 0.60f, 0.34f);
            // Pallet
            Part(PrimitiveType.Cube, owner.transform, new Vector3(0, 0.06f, 0), new Vector3(1.7f, 0.12f, 1.7f), wood);
            // Crates
            Part(PrimitiveType.Cube, owner.transform, new Vector3(-0.4f, 0.42f, -0.3f), new Vector3(0.6f, 0.6f, 0.6f), crate);
            Part(PrimitiveType.Cube, owner.transform, new Vector3(0.42f, 0.42f, 0.25f), new Vector3(0.6f, 0.6f, 0.6f), crate);
            Part(PrimitiveType.Cube, owner.transform, new Vector3(-0.35f, 1.0f, -0.25f), new Vector3(0.55f, 0.55f, 0.55f), wood);
        }

        /// <summary>Delivery van that arrives to pick up phone orders.</summary>
        public static void VanVisual(GameObject owner)
        {
            var bodyColor = new Color(0.9f, 0.9f, 0.9f);
            var windowColor = new Color(0.3f, 0.7f, 0.9f);
            var wheelColor = new Color(0.1f, 0.1f, 0.1f);
            
            // Main body
            Part(PrimitiveType.Cube, owner.transform, new Vector3(0, 0.8f, 0), new Vector3(1.6f, 1.2f, 3.2f), bodyColor);
            // Cab
            Part(PrimitiveType.Cube, owner.transform, new Vector3(0, 0.6f, 2f), new Vector3(1.6f, 0.8f, 1f), bodyColor);
            // Windshield
            Part(PrimitiveType.Cube, owner.transform, new Vector3(0, 0.85f, 1.8f), new Vector3(1.5f, 0.6f, 0.8f), windowColor);
            
            // Wheels
            Part(PrimitiveType.Sphere, owner.transform, new Vector3(0.85f, 0.35f, 1.5f), new Vector3(0.7f, 0.7f, 0.7f), wheelColor);
            Part(PrimitiveType.Sphere, owner.transform, new Vector3(-0.85f, 0.35f, 1.5f), new Vector3(0.7f, 0.7f, 0.7f), wheelColor);
            Part(PrimitiveType.Sphere, owner.transform, new Vector3(0.85f, 0.35f, -1f), new Vector3(0.7f, 0.7f, 0.7f), wheelColor);
            Part(PrimitiveType.Sphere, owner.transform, new Vector3(-0.85f, 0.35f, -1f), new Vector3(0.7f, 0.7f, 0.7f), wheelColor);
        }

        /// <summary>Shopping trolley pushed in front of a buyer carrying 5+ items (GDD 8.2).</summary>
        public static GameObject Trolley(GameObject owner)
        {
            var pivot = new GameObject("Trolley");
            pivot.transform.SetParent(owner.transform, false);
            pivot.transform.localPosition = new Vector3(0, 0, 0.55f);

            var metal = new Color(0.74f, 0.78f, 0.84f);
            var wheel = new Color(0.20f, 0.20f, 0.22f);

            // Basket
            Part(PrimitiveType.Cube, pivot.transform, new Vector3(0, 0.42f, 0.28f), new Vector3(0.55f, 0.32f, 0.68f), metal);
            // Handle bar
            Part(PrimitiveType.Cube, pivot.transform, new Vector3(0, 0.68f, -0.12f), new Vector3(0.5f, 0.05f, 0.05f), metal);
            Part(PrimitiveType.Cube, pivot.transform, new Vector3(-0.22f, 0.55f, -0.06f), new Vector3(0.05f, 0.28f, 0.05f), metal);
            Part(PrimitiveType.Cube, pivot.transform, new Vector3(0.22f, 0.55f, -0.06f), new Vector3(0.05f, 0.28f, 0.05f), metal);
            // Wheels
            Part(PrimitiveType.Sphere, pivot.transform, new Vector3(-0.20f, 0.08f, 0.05f), Vector3.one * 0.13f, wheel);
            Part(PrimitiveType.Sphere, pivot.transform, new Vector3(0.20f, 0.08f, 0.05f), Vector3.one * 0.13f, wheel);
            Part(PrimitiveType.Sphere, pivot.transform, new Vector3(-0.20f, 0.08f, 0.52f), Vector3.one * 0.13f, wheel);
            Part(PrimitiveType.Sphere, pivot.transform, new Vector3(0.20f, 0.08f, 0.52f), Vector3.one * 0.13f, wheel);
            return pivot;
        }

        // ─── Environment ─────────────────────────────────────────────────────

        /// <summary>Large outdoor grass plane.</summary>
        public static void GrassGround(Vector3 center, float sizeX, float sizeZ)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "GrassGround";
            go.transform.position = new Vector3(center.x, -0.02f, center.z);
            go.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(StdMat);
            mr.material.color = GRASS;
        }

        /// <summary>Tan store interior floor.</summary>
        public static void StoreFloor(Vector3 center, float sizeX, float sizeZ)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "StoreFloor";
            go.transform.position = new Vector3(center.x, -0.01f, center.z);
            go.transform.localScale = new Vector3(sizeX, 0.02f, sizeZ);
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(StdMat);
            mr.material.color = STORE_FLOOR;
        }

        /// <summary>Store wall segment.</summary>
        public static void Wall(string name, Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = new Vector3(pos.x, 0.8f, pos.z);
            go.transform.localScale = new Vector3(size.x, 1.6f, size.z);
            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(StdMat);
            mr.material.color = WALL_COLOR;
        }

        /// <summary>Floor mat underneath furniture.</summary>
        public static void FloorMat(Transform parent, float sizeX, float sizeZ)
        {
            Part(PrimitiveType.Cube, parent,
                new Vector3(0, 0.01f, 0), new Vector3(sizeX, 0.02f, sizeZ), FLOOR_MAT);
        }

        // ─── Fence ───────────────────────────────────────────────────────────

        /// <summary>Fence segment between two points with posts and horizontal rails.</summary>
        public static void FenceSegment(Vector3 start, Vector3 end)
        {
            var parent = new GameObject("Fence");

            // Posts at both ends
            FencePost(parent.transform, start);
            FencePost(parent.transform, end);

            // Two horizontal rails
            Vector3 mid = (start + end) * 0.5f;
            Vector3 dir = end - start;
            float len = dir.magnitude;
            bool alongX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);

            for (int i = 0; i < 2; i++)
            {
                float y = 0.2f + i * 0.22f;
                var rail = Part(PrimitiveType.Cube, parent.transform,
                    new Vector3(mid.x, y, mid.z),
                    new Vector3(alongX ? len : 0.06f, 0.06f, alongX ? 0.06f : len),
                    FENCE);
                rail.transform.SetParent(null);
                rail.transform.position = new Vector3(mid.x, y, mid.z);
                rail.transform.localScale = new Vector3(alongX ? len : 0.06f, 0.06f, alongX ? 0.06f : len);
                rail.transform.SetParent(parent.transform);
            }
        }

        private static void FencePost(Transform parent, Vector3 pos)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(post.GetComponent<Collider>());
            post.name = "FencePost";
            post.transform.SetParent(parent, false);
            post.transform.position = pos + Vector3.up * 0.3f;
            post.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
            var mr = post.GetComponent<MeshRenderer>();
            mr.material = new Material(StdMat);
            mr.material.color = FENCE;
        }

        // ─── Characters ──────────────────────────────────────────────────────

        /// <summary>Character with capsule body, sphere head, eyes, and optional details.</summary>
        static readonly Color Skin = new Color(0.99f, 0.83f, 0.66f);

        /// <summary>Reference-style low-poly "toy person": stubby legs + feet, a rounded
        /// torso in the character's colour, arms at the sides, a skin-tone head with hair
        /// and two eyes. Built entirely from smooth primitives so it reads as a little
        /// gingerbread character, not a bare capsule.</summary>
        /// <summary>Role variants of BuildCharacter — each gets reference-matching
        /// costume details (chef apron, farmer overalls+straw hat, cashier name tag,
        /// shopper hair variants) so characters aren't visually identical.</summary>
        public enum CharacterRole { Generic, Player, Chef, Farmer, Cashier, Shelver, Shopper }

        public static GameObject BuildCharacter(GameObject go, Color bodyColor,
            bool chefHat = false, bool isPlayer = false)
        {
            // Back-compat wrapper. Chef and player still work; NPCs go through Generic.
            var role = chefHat ? CharacterRole.Chef
                     : isPlayer ? CharacterRole.Player
                     : CharacterRole.Generic;
            return BuildCharacter(go, bodyColor, role);
        }

        // ── Optional 3D character-pack skins ─────────────────────────────────
        // Drop any humanoid prefab into Assets/Resources/Characters/ and it
        // replaces the primitive body automatically — no code changes needed:
        //   Characters/Player.prefab  → used for that specific role
        //   Characters/Chef.prefab, Farmer, Cashier, Shelver, Shopper, Generic
        //   Characters/Base.prefab    → fallback used for EVERY role
        // The prefab is tinted with the character's colour where its materials
        // allow, and scaled to the ~1.5-unit chibi height the game expects.
        private static bool skinLookupDone;
        private static readonly System.Collections.Generic.Dictionary<CharacterRole, GameObject> skinPrefabs =
            new System.Collections.Generic.Dictionary<CharacterRole, GameObject>();
        private static GameObject skinBasePrefab;

        private static GameObject SkinFor(CharacterRole role)
        {
            if (!skinLookupDone)
            {
                skinLookupDone = true;
                foreach (CharacterRole r in System.Enum.GetValues(typeof(CharacterRole)))
                {
                    var p = Resources.Load<GameObject>($"Characters/{r}");
                    if (p != null) skinPrefabs[r] = p;
                }
                skinBasePrefab = Resources.Load<GameObject>("Characters/Base");
            }
            return skinPrefabs.TryGetValue(role, out var exact) ? exact : skinBasePrefab;
        }

        public static GameObject BuildCharacter(GameObject go, Color bodyColor, CharacterRole role)
        {
            // If a character-pack skin exists, use it instead of the primitive
            // body. Must contain a MeshRenderer or SkinnedMeshRenderer (2D
            // sprites are rejected — they've broken the player before).
            var skin = SkinFor(role);
            if (skin != null &&
                (skin.GetComponentInChildren<MeshRenderer>() != null ||
                 skin.GetComponentInChildren<SkinnedMeshRenderer>() != null))
            {
                var visual = Object.Instantiate(skin, go.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;

                // Normalize to the ~1.5-unit character height the whole game
                // (camera, badges, carry stacks) is tuned around.
                var bounds = new Bounds(visual.transform.position, Vector3.zero);
                foreach (var r in visual.GetComponentsInChildren<Renderer>())
                    bounds.Encapsulate(r.bounds);
                if (bounds.size.y > 0.01f)
                    visual.transform.localScale *= 1.5f / bounds.size.y;

                // Tint the largest renderer's material with the role colour so
                // shoppers keep their palette variety even on a shared mesh.
                Renderer biggest = null; float best = 0f;
                foreach (var r in visual.GetComponentsInChildren<Renderer>())
                {
                    float v = r.bounds.size.sqrMagnitude;
                    if (v > best) { best = v; biggest = r; }
                }
                if (biggest != null && biggest.material != null && biggest.material.HasProperty("_Color"))
                    biggest.material.color = bodyColor;

                return visual;
            }

            // Reference "My Mini Mart" chibi silhouette: flat-topped rounded shirt,
            // oversized skin sphere head with no neck, tiny black dot eyes, tiny shoe
            // caps at base. Role-specific hat/apron overlays give each NPC a
            // distinguishing costume detail so they read like the reference cast.
            var pivot = new GameObject("Visual");
            pivot.transform.SetParent(go.transform, false);
            var pt = pivot.transform;

            // Farmer wears sturdy brown boots; everyone else gets dark shoe caps
            // in a shade of their body color.
            Color shoe = role == CharacterRole.Farmer
                ? new Color(0.35f, 0.20f, 0.10f)
                : new Color(bodyColor.r * 0.35f, bodyColor.g * 0.35f, bodyColor.b * 0.40f);
            Part(PrimitiveType.Cube, pt, new Vector3(-0.14f, 0.06f, 0.06f),
                new Vector3(0.20f, 0.12f, 0.30f), shoe);
            Part(PrimitiveType.Cube, pt, new Vector3( 0.14f, 0.06f, 0.06f),
                new Vector3(0.20f, 0.12f, 0.30f), shoe);

            // Body — flat-topped rounded shirt.
            Part(PrimitiveType.Cube, pt, new Vector3(0f, 0.45f, 0f),
                new Vector3(0.62f, 0.70f, 0.44f), bodyColor);
            var hem = new Color(bodyColor.r * 0.78f, bodyColor.g * 0.78f, bodyColor.b * 0.82f);
            Part(PrimitiveType.Cube, pt, new Vector3(0f, 0.14f, 0f),
                new Vector3(0.64f, 0.12f, 0.46f), hem);

            // ── Role-specific torso overlays ──
            switch (role)
            {
                case CharacterRole.Chef:
                {
                    // White apron: broad flat panel across the front of the shirt.
                    Part(PrimitiveType.Cube, pt, new Vector3(0f, 0.42f, 0.23f),
                        new Vector3(0.50f, 0.66f, 0.03f), new Color(0.98f, 0.98f, 0.95f));
                    // Apron neck-loop — thin dark strap.
                    Part(PrimitiveType.Cube, pt, new Vector3(0f, 0.80f, 0.24f),
                        new Vector3(0.06f, 0.10f, 0.03f), new Color(0.75f, 0.75f, 0.72f));
                    break;
                }
                case CharacterRole.Farmer:
                {
                    // Brown apron/overalls panel on the front.
                    var apron = new Color(0.52f, 0.34f, 0.18f);
                    Part(PrimitiveType.Cube, pt, new Vector3(0f, 0.42f, 0.23f),
                        new Vector3(0.52f, 0.60f, 0.03f), apron);
                    // Two shoulder straps.
                    Part(PrimitiveType.Cube, pt, new Vector3(-0.14f, 0.78f, 0.24f),
                        new Vector3(0.08f, 0.14f, 0.03f), apron);
                    Part(PrimitiveType.Cube, pt, new Vector3( 0.14f, 0.78f, 0.24f),
                        new Vector3(0.08f, 0.14f, 0.03f), apron);
                    break;
                }
                case CharacterRole.Cashier:
                {
                    // Small name-tag square on the chest.
                    Part(PrimitiveType.Cube, pt, new Vector3(0.17f, 0.55f, 0.23f),
                        new Vector3(0.14f, 0.10f, 0.02f), new Color(0.98f, 0.98f, 0.98f));
                    Part(PrimitiveType.Cube, pt, new Vector3(0.17f, 0.55f, 0.24f),
                        new Vector3(0.10f, 0.03f, 0.01f), new Color(0.15f, 0.15f, 0.20f));
                    break;
                }
                case CharacterRole.Shelver:
                {
                    // Stock-clerk vest — a thin colored panel echoing body color,
                    // slightly desaturated so it reads as an over-shirt.
                    var vest = new Color(bodyColor.r * 0.60f, bodyColor.g * 0.60f, bodyColor.b * 0.60f);
                    Part(PrimitiveType.Cube, pt, new Vector3(0f, 0.55f, 0.22f),
                        new Vector3(0.55f, 0.40f, 0.03f), vest);
                    break;
                }
            }

            // Head — skin sphere directly on top of the body.
            Part(PrimitiveType.Sphere, pt, new Vector3(0f, 1.05f, 0f),
                new Vector3(0.62f, 0.62f, 0.62f), Skin);

            // Eyes.
            Part(PrimitiveType.Sphere, pt, new Vector3(-0.13f, 1.08f, 0.25f),
                new Vector3(0.08f, 0.10f, 0.05f), new Color(0.06f, 0.06f, 0.08f));
            Part(PrimitiveType.Sphere, pt, new Vector3( 0.13f, 1.08f, 0.25f),
                new Vector3(0.08f, 0.10f, 0.05f), new Color(0.06f, 0.06f, 0.08f));

            // ── Role-specific hats ──
            switch (role)
            {
                case CharacterRole.Chef:
                    Part(PrimitiveType.Cylinder, pt, new Vector3(0f, 1.42f, 0f),
                        new Vector3(0.36f, 0.16f, 0.36f), Color.white);
                    Part(PrimitiveType.Sphere, pt, new Vector3(0f, 1.68f, 0f),
                        new Vector3(0.55f, 0.42f, 0.55f), Color.white);
                    break;

                case CharacterRole.Player:
                    // White ballcap with forward visor.
                    Part(PrimitiveType.Sphere, pt, new Vector3(0f, 1.36f, -0.02f),
                        new Vector3(0.60f, 0.32f, 0.60f), Color.white);
                    Part(PrimitiveType.Cube, pt, new Vector3(0f, 1.26f, 0.28f),
                        new Vector3(0.46f, 0.05f, 0.20f), Color.white);
                    break;

                case CharacterRole.Farmer:
                {
                    // Straw hat: wide flat tan brim + short domed crown.
                    var straw = new Color(0.92f, 0.78f, 0.42f);
                    Part(PrimitiveType.Cylinder, pt, new Vector3(0f, 1.36f, 0f),
                        new Vector3(0.92f, 0.03f, 0.92f), straw);
                    Part(PrimitiveType.Sphere, pt, new Vector3(0f, 1.42f, 0f),
                        new Vector3(0.52f, 0.28f, 0.52f), straw);
                    break;
                }

                case CharacterRole.Cashier:
                {
                    // Neat rounded hair-cap in a chestnut tone (reference cashiers
                    // read as short-haired NPCs).
                    var hair = new Color(0.42f, 0.28f, 0.18f);
                    Part(PrimitiveType.Sphere, pt, new Vector3(0f, 1.22f, -0.02f),
                        new Vector3(0.58f, 0.36f, 0.58f), hair);
                    break;
                }

                case CharacterRole.Shopper:
                {
                    // Shoppers get a color-varied hair cap: 3 preset palettes based
                    // on body color hue, so a crowd of shoppers reads as distinct
                    // people rather than one repeated NPC.
                    Color hair;
                    // Cheap "hash" of body color -> hair palette selection.
                    int bucket = Mathf.Abs((int)(bodyColor.r * 7 + bodyColor.g * 11 + bodyColor.b * 13)) % 3;
                    hair = bucket == 0 ? new Color(0.35f, 0.22f, 0.14f)                 // brown
                         : bucket == 1 ? new Color(0.15f, 0.15f, 0.18f)                 // black
                         :                new Color(0.92f, 0.72f, 0.24f);                // blonde
                    Part(PrimitiveType.Sphere, pt, new Vector3(0f, 1.22f, -0.02f),
                        new Vector3(0.58f, 0.36f, 0.58f), hair);
                    // Reference shoppers push a small wireframe shopping cart in front
                    // of them. We approximate with a grey basket + red handle + two dark
                    // wheels — pivot is the character body, so the cart floats forward
                    // (+Z) at hand height.
                    var cart = new GameObject("ShoppingCart");
                    cart.transform.SetParent(pt, false);
                    cart.transform.localPosition = new Vector3(0f, 0.05f, 0.55f);
                    var basket = new Color(0.85f, 0.85f, 0.88f);
                    var handle = new Color(0.85f, 0.30f, 0.28f);
                    var wheel  = new Color(0.15f, 0.15f, 0.18f);
                    // Basket body: shallow open cube (approx as thin box + interior).
                    Part(PrimitiveType.Cube, cart.transform, new Vector3(0f, 0.60f, 0f),
                        new Vector3(0.55f, 0.30f, 0.42f), basket);
                    // Basket interior lip (darker) — reads as an open box top.
                    Part(PrimitiveType.Cube, cart.transform, new Vector3(0f, 0.72f, 0f),
                        new Vector3(0.48f, 0.02f, 0.36f), new Color(0.55f, 0.55f, 0.58f));
                    // Handle bar (red, above and behind the basket toward the shopper).
                    Part(PrimitiveType.Cube, cart.transform, new Vector3(0f, 0.95f, -0.22f),
                        new Vector3(0.60f, 0.05f, 0.05f), handle);
                    // Handle uprights.
                    Part(PrimitiveType.Cube, cart.transform, new Vector3(-0.26f, 0.78f, -0.20f),
                        new Vector3(0.05f, 0.36f, 0.05f), handle);
                    Part(PrimitiveType.Cube, cart.transform, new Vector3( 0.26f, 0.78f, -0.20f),
                        new Vector3(0.05f, 0.36f, 0.05f), handle);
                    // Two wheels at the front bottom (visible from the isometric angle).
                    Part(PrimitiveType.Sphere, cart.transform, new Vector3(-0.20f, 0.40f, 0.18f),
                        new Vector3(0.12f, 0.12f, 0.12f), wheel);
                    Part(PrimitiveType.Sphere, cart.transform, new Vector3( 0.20f, 0.40f, 0.18f),
                        new Vector3(0.12f, 0.12f, 0.12f), wheel);
                    break;
                }

                default: // Generic NPC — color-echoed hair cap.
                {
                    var hair = new Color(bodyColor.r * 0.50f, bodyColor.g * 0.50f, bodyColor.b * 0.60f);
                    Part(PrimitiveType.Sphere, pt, new Vector3(0f, 1.22f, -0.02f),
                        new Vector3(0.58f, 0.36f, 0.58f), hair);
                    break;
                }
            }

            return pivot;
        }

        // ─── Farms ───────────────────────────────────────────────────────────

        /// <summary>Hen coop: circular dirt patch with eggs and a chicken.</summary>
        public static void HenCoop(GameObject go)
        {
            // Circular dirt patch
            Part(PrimitiveType.Cylinder, go.transform, new Vector3(0, 0.05f, 0), new Vector3(2.2f, 0.1f, 2.2f), DIRT);
            // Scatter eggs (off-white spheres)
            float[] angles = { 0, 55, 110, 170, 230, 300 };
            foreach (float deg in angles)
            {
                float rad = deg * Mathf.Deg2Rad;
                float r = 0.45f + (deg % 3) * 0.08f;
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(Mathf.Cos(rad) * r, 0.16f, Mathf.Sin(rad) * r),
                    new Vector3(0.16f, 0.13f, 0.2f),
                    new Color(1f, 0.97f, 0.88f));
            }
            // Chicken body
            var hen = Part(PrimitiveType.Sphere, go.transform,
                new Vector3(0.2f, 0.22f, 0.15f), new Vector3(0.28f, 0.22f, 0.35f), Color.white);
            // Chicken comb
            Part(PrimitiveType.Sphere, hen.transform,
                new Vector3(0, 0.5f, 0.4f), new Vector3(0.3f, 0.4f, 0.15f), Color.red);
        }

        /// <summary>Tomato farm: dirt rectangle with growing tomato plants.</summary>
        public static void TomatoFarm(GameObject go)
        {
            // Dirt patch
            Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.04f, 0), new Vector3(3.2f, 0.08f, 2.2f), DIRT);
            // Rows of tomato plants
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float x = -1.1f + col * 0.75f;
                    float z = -0.45f + row * 0.9f;
                    // Green stem
                    Part(PrimitiveType.Cylinder, go.transform,
                        new Vector3(x, 0.35f, z), new Vector3(0.06f, 0.3f, 0.06f), new Color(0.25f, 0.65f, 0.25f));
                    // Leaf
                    Part(PrimitiveType.Sphere, go.transform,
                        new Vector3(x, 0.55f, z), new Vector3(0.25f, 0.12f, 0.25f), new Color(0.3f, 0.72f, 0.3f));
                    // Tomato
                    Part(PrimitiveType.Sphere, go.transform,
                        new Vector3(x + 0.12f, 0.4f, z), new Vector3(0.2f, 0.2f, 0.2f), Color.red);
                }
            }
        }

        /// <summary>Wheat farm: brown grid with wheat stalks.</summary>
        public static void WheatFarm(GameObject go)
        {
            int rows = 5, cols = 5;
            float cell = 0.55f;
            float ox = -(cols - 1) * cell * 0.5f;
            float oz = -(rows - 1) * cell * 0.5f;
            // Brown tilled dirt grid
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.03f, 0),
                new Vector3(cols * cell + 0.2f, 0.06f, rows * cell + 0.2f),
                new Color(0.52f, 0.38f, 0.22f));
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float x = ox + c * cell;
                    float z = oz + r * cell;
                    // Individual dirt cell
                    Part(PrimitiveType.Cube, go.transform,
                        new Vector3(x, 0.06f, z),
                        new Vector3(cell - 0.08f, 0.02f, cell - 0.08f),
                        new Color(0.45f, 0.32f, 0.18f));
                    // Wheat stalk
                    Part(PrimitiveType.Cylinder, go.transform,
                        new Vector3(x, 0.28f, z), new Vector3(0.04f, 0.22f, 0.04f),
                        new Color(0.82f, 0.72f, 0.38f));
                    // Wheat head
                    Part(PrimitiveType.Sphere, go.transform,
                        new Vector3(x, 0.5f, z), new Vector3(0.1f, 0.14f, 0.1f),
                        new Color(0.92f, 0.82f, 0.48f));
                }
            }
        }

        // ─── Perimeter & decorative props (reference image parity) ──────────

        /// <summary>Simple low-poly tree — brown trunk cylinder + green sphere
        /// canopy. Reference image rings the entire map with trees.</summary>
        public static void Tree(Vector3 pos, float scale = 1f)
        {
            var trunk = new Color(0.42f, 0.28f, 0.16f);
            var leaf  = new Color(0.32f, 0.62f, 0.28f);
            var leafDark = new Color(0.22f, 0.48f, 0.20f);
            var go = new GameObject("Tree");
            go.transform.position = pos;
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3(0f, 0.55f * scale, 0f),
                new Vector3(0.35f * scale, 0.55f * scale, 0.35f * scale), trunk);
            Part(PrimitiveType.Sphere, go.transform,
                new Vector3(0f, 1.40f * scale, 0f),
                new Vector3(1.30f * scale, 1.30f * scale, 1.30f * scale), leaf);
            Part(PrimitiveType.Sphere, go.transform,
                new Vector3(-0.35f * scale, 1.55f * scale, 0.25f * scale),
                new Vector3(0.85f * scale, 0.85f * scale, 0.85f * scale), leafDark);
        }

        /// <summary>Ring of trees along the map perimeter. Called once at boot.</summary>
        public static void TreePerimeter(float minX, float maxX, float minZ, float maxZ, float spacing = 3.5f)
        {
            // Top and bottom edges
            for (float x = minX; x <= maxX; x += spacing)
            {
                Tree(new Vector3(x + Random.Range(-0.4f, 0.4f), 0f, maxZ + Random.Range(-0.3f, 0.3f)));
                Tree(new Vector3(x + Random.Range(-0.4f, 0.4f), 0f, minZ + Random.Range(-0.3f, 0.3f)));
            }
            // Left and right edges
            for (float z = minZ + spacing; z <= maxZ - spacing; z += spacing)
            {
                Tree(new Vector3(minX + Random.Range(-0.3f, 0.3f), 0f, z + Random.Range(-0.4f, 0.4f)));
                Tree(new Vector3(maxX + Random.Range(-0.3f, 0.3f), 0f, z + Random.Range(-0.4f, 0.4f)));
            }
        }

        /// <summary>Red barn / chicken coop building — reference §Livestock West.
        /// Wooden red walls, dark shingle roof, small door, white trim.</summary>
        public static void RedBarn(GameObject go)
        {
            var red    = new Color(0.72f, 0.20f, 0.18f);
            var redDk  = new Color(0.55f, 0.16f, 0.14f);
            var roof   = new Color(0.32f, 0.24f, 0.20f);
            var white  = new Color(0.94f, 0.92f, 0.88f);

            // Main box body
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.80f, 0f), new Vector3(2.2f, 1.6f, 1.8f), red);
            // Darker wood trim on the sides
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(-1.10f, 0.80f, 0f), new Vector3(0.05f, 1.55f, 1.75f), redDk);
            Part(PrimitiveType.Cube, go.transform,
                new Vector3( 1.10f, 0.80f, 0f), new Vector3(0.05f, 1.55f, 1.75f), redDk);
            // Peaked roof (two slabs meeting at the ridge)
            var roofL = Part(PrimitiveType.Cube, go.transform,
                new Vector3(-0.55f, 1.80f, 0f), new Vector3(1.35f, 0.10f, 2.0f), roof);
            roofL.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            var roofR = Part(PrimitiveType.Cube, go.transform,
                new Vector3( 0.55f, 1.80f, 0f), new Vector3(1.35f, 0.10f, 2.0f), roof);
            roofR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            // White door + door frame
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.55f, 0.92f), new Vector3(0.55f, 1.05f, 0.05f), white);
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.55f, 0.94f), new Vector3(0.06f, 1.05f, 0.03f), roof);
            // Small square window
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(-0.60f, 1.20f, 0.92f), new Vector3(0.30f, 0.30f, 0.05f), white);
        }

        /// <summary>Processing factory building — concrete-grey box with two
        /// smokestacks emitting steam. Sits in the middle strip of the map.</summary>
        public static void ProcessingFactory(GameObject go)
        {
            var concrete = new Color(0.70f, 0.70f, 0.72f);
            var dark     = new Color(0.45f, 0.45f, 0.48f);
            var metal    = new Color(0.55f, 0.55f, 0.58f);
            var gears    = new Color(0.62f, 0.58f, 0.30f);

            // Main body
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.9f, 0f), new Vector3(3.6f, 1.8f, 2.0f), concrete);
            // Darker base plinth
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.15f, 0f), new Vector3(3.8f, 0.30f, 2.2f), dark);
            // Two smokestacks
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3(-0.85f, 2.20f, 0f), new Vector3(0.40f, 0.80f, 0.40f), dark);
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3( 0.35f, 2.20f, 0f), new Vector3(0.40f, 0.80f, 0.40f), dark);
            // Chrome ring atop each stack
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3(-0.85f, 3.05f, 0f), new Vector3(0.45f, 0.05f, 0.45f), metal);
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3( 0.35f, 3.05f, 0f), new Vector3(0.45f, 0.05f, 0.45f), metal);
            // Gear silhouettes on the front face
            for (int i = 0; i < 3; i++)
            {
                float gx = -1.1f + i * 1.1f;
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(gx, 0.9f, 1.05f), new Vector3(0.55f, 0.05f, 0.55f), gears);
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(gx, 0.9f, 1.10f), new Vector3(0.25f, 0.02f, 0.25f), metal);
            }

            // Steam puffs from the stacks
            for (int i = 0; i < 2; i++)
            {
                var stackX = i == 0 ? -0.85f : 0.35f;
                var puffGO = new GameObject($"SteamPuff_{i}");
                puffGO.transform.SetParent(go.transform, false);
                puffGO.transform.localPosition = new Vector3(stackX, 3.25f, 0f);
                var ps = puffGO.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 1.5f;
                main.startSpeed = 0.7f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                main.startColor = new Color(1f, 1f, 1f, 0.55f);
                main.maxParticles = 30;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission;
                em.rateOverTime = 4f;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
                col.color = grad;
                // B8: also animate SIZE over lifetime — a puff that grows as it fades
                // reads as dissipating smoke instead of a static hard quad.
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(
                    1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.3f)));
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.material = NewParticleMaterial();
            }
        }

        /// <summary>Ground-painted directional flow arrow (reference has orange
        /// arrows from farm → processing and blue arrows from processing → shelves).
        /// Rotates around Y so `direction` points the arrow tip toward the target.</summary>
        public static void FlowArrow(Vector3 pos, float lengthX, float widthZ, float directionDeg, Color color)
        {
            var go = new GameObject("FlowArrow");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, directionDeg, 0f);

            // Shaft (a long thin cube)
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.015f, 0f), new Vector3(lengthX, 0.02f, widthZ), color);
            // Arrow head — a rotated square giving a diamond point
            var head = Part(PrimitiveType.Cube, go.transform,
                new Vector3(lengthX * 0.5f + widthZ * 0.5f, 0.02f, 0f),
                new Vector3(widthZ * 1.2f, 0.02f, widthZ * 1.2f), color);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        }

        /// <summary>Milk tray — silver rectangular pan the cow's milk collects
        /// in. Sits at the cow's feet in Livestock West.</summary>
        public static void MilkTray(GameObject go)
        {
            var silver = new Color(0.82f, 0.85f, 0.88f);
            var white  = new Color(0.98f, 0.98f, 1f);
            // Tray rim
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.10f, 0.6f), silver);
            // Milk surface (slightly recessed)
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.09f, 0f), new Vector3(0.75f, 0.02f, 0.48f), white);
        }

        /// <summary>Single tomato crop plot for the reference "purchasable plots"
        /// grid (Agriculture East). A brown dirt tile with a small tomato plant.</summary>
        public static void TomatoCropPlot(GameObject go, bool grown = true)
        {
            var dirt  = new Color(0.42f, 0.28f, 0.18f);
            var stalk = new Color(0.35f, 0.68f, 0.30f);
            var red   = new Color(0.90f, 0.15f, 0.10f);
            // Dirt bed
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.04f, 0f), new Vector3(1.4f, 0.08f, 1.4f), dirt);
            if (grown)
            {
                // Center stalk + a small cluster of tomatoes
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(0f, 0.35f, 0f), new Vector3(0.10f, 0.55f, 0.10f), stalk);
                Part(PrimitiveType.Sphere, go.transform, new Vector3(-0.18f, 0.45f, 0f), new Vector3(0.28f, 0.28f, 0.28f), red);
                Part(PrimitiveType.Sphere, go.transform, new Vector3( 0.18f, 0.45f, 0f), new Vector3(0.28f, 0.28f, 0.28f), red);
                Part(PrimitiveType.Sphere, go.transform, new Vector3( 0f, 0.65f, 0f),   new Vector3(0.30f, 0.30f, 0.30f), red);
            }
        }

        /// <summary>Dashed rectangle outline on the ground indicating "purchasable
        /// plots" (reference callout in the middle of the farm). Rendered as a
        /// series of small white cubes around the rectangle perimeter.</summary>
        public static void DashedRect(Vector3 center, float sizeX, float sizeZ, Color dashColor)
        {
            var go = new GameObject("DashedRect");
            go.transform.position = center;
            float dashLen = 0.5f;
            float dashGap = 0.35f;
            float stride = dashLen + dashGap;

            void EdgeDashes(float startX, float startZ, float endX, float endZ)
            {
                float dx = endX - startX;
                float dz = endZ - startZ;
                float len = Mathf.Sqrt(dx * dx + dz * dz);
                float ang = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
                int count = Mathf.Max(1, Mathf.FloorToInt(len / stride));
                for (int i = 0; i < count; i++)
                {
                    float t = (i + 0.5f) / count;
                    float x = Mathf.Lerp(startX, endX, t);
                    float z = Mathf.Lerp(startZ, endZ, t);
                    var dash = Part(PrimitiveType.Cube, go.transform,
                        new Vector3(x - center.x, 0.025f, z - center.z),
                        new Vector3(dashLen, 0.02f, 0.12f), dashColor);
                    dash.transform.localRotation = Quaternion.Euler(0f, -ang, 0f);
                }
            }
            float halfX = sizeX * 0.5f, halfZ = sizeZ * 0.5f;
            EdgeDashes(center.x - halfX, center.z - halfZ, center.x + halfX, center.z - halfZ);
            EdgeDashes(center.x + halfX, center.z - halfZ, center.x + halfX, center.z + halfZ);
            EdgeDashes(center.x + halfX, center.z + halfZ, center.x - halfX, center.z + halfZ);
            EdgeDashes(center.x - halfX, center.z + halfZ, center.x - halfX, center.z - halfZ);
        }

        // ─── Zone Floors & Dividers (new map spec) ───────────────────────────

        /// <summary>Pink Bakery/Café floor tile — reference §"Bakery & Café (Pink
        /// Floor)". Sits east of the beige aisle floor, marking the café zone
        /// where bread, cookies, milk fridge, and coffee dispenser live.</summary>
        public static void CafeFloor(Vector3 center, float sizeX, float sizeZ)
        {
            Part(PrimitiveType.Cube, null, new Vector3(center.x, 0.005f, center.z),
                new Vector3(sizeX, 0.01f, sizeZ), new Color(0.96f, 0.78f, 0.85f));
        }

        /// <summary>Dark asphalt road segment with a dashed centerline (reference
        /// map §divider between Supermarket Zone and Processing Area).</summary>
        public static void Road(Vector3 center, float sizeX, float sizeZ)
        {
            var asphalt = new Color(0.28f, 0.28f, 0.30f);
            Part(PrimitiveType.Cube, null, new Vector3(center.x, 0.008f, center.z),
                new Vector3(sizeX, 0.015f, sizeZ), asphalt);

            // Dashed centerline: white cubes down the middle of the road.
            int dashCount = Mathf.Max(3, Mathf.RoundToInt(sizeX / 2.5f));
            float spacing = sizeX / dashCount;
            for (int i = 0; i < dashCount; i++)
            {
                float x = center.x - sizeX * 0.5f + spacing * (i + 0.5f);
                Part(PrimitiveType.Cube, null, new Vector3(x, 0.02f, center.z),
                    new Vector3(spacing * 0.55f, 0.015f, 0.14f),
                    new Color(0.95f, 0.95f, 0.92f));
            }
        }

        /// <summary>Painted white parking rectangle for the delivery van, per
        /// reference "how pickup van.png" (visible white outline under the truck).</summary>
        public static void VanParkingSpot(Vector3 center)
        {
            var white = new Color(0.95f, 0.95f, 0.92f);
            // Outer white rectangle
            Part(PrimitiveType.Cube, null, new Vector3(center.x, 0.011f, center.z),
                new Vector3(3.6f, 0.01f, 6.0f), white);
            // Inner cut-out (grass) to make it read as painted lines, not a slab
            Part(PrimitiveType.Cube, null, new Vector3(center.x, 0.012f, center.z),
                new Vector3(3.2f, 0.012f, 5.6f), GRASS);
        }

        /// <summary>Small red waste bin — decorative store prop matching the
        /// reference's red bins dotted along the shop floor. Purely cosmetic; not
        /// stamped into the walk grid so it never blocks a path.</summary>
        public static void TrashBin(Vector3 pos)
        {
            var red  = new Color(0.86f, 0.26f, 0.24f);
            var dark = new Color(0.12f, 0.12f, 0.14f);
            var root = new GameObject("TrashBin");
            root.transform.position = pos;
            Part(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.35f, 0f),
                new Vector3(0.68f, 0.70f, 0.68f), red);                       // body
            Part(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.72f, 0f),
                new Vector3(0.78f, 0.06f, 0.78f), dark);                      // rim
            Part(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.70f, 0f),
                new Vector3(0.58f, 0.05f, 0.58f), new Color(0.05f, 0.05f, 0.06f)); // opening
        }

        /// <summary>Red-white candy-striped angled barrier at the store's west
        /// entrance, per reference "how buyer spawn.png". Purely decorative.</summary>
        public static void StripedGate(Vector3 basePos, float length = 5f)
        {
            var red   = new Color(0.90f, 0.30f, 0.30f);
            var white = new Color(0.95f, 0.95f, 0.92f);
            // Base container so we can angle it and stripe it.
            var root = new GameObject("StripedGate");
            root.transform.position = basePos;
            root.transform.rotation = Quaternion.Euler(0f, 30f, -35f);
            // Alternate red / white stripe slabs stacked along local +X to form
            // the candy-cane barrier.
            int stripes = 10;
            float stripeLen = length / stripes;
            for (int i = 0; i < stripes; i++)
            {
                var col = i % 2 == 0 ? red : white;
                var slab = Part(PrimitiveType.Cube, root.transform,
                    new Vector3(-length * 0.5f + stripeLen * (i + 0.5f), 1.4f, 0f),
                    new Vector3(stripeLen, 1.8f, 0.25f), col);
            }
            // Two support posts at either end
            var brown = new Color(0.55f, 0.38f, 0.18f);
            Part(PrimitiveType.Cube, root.transform, new Vector3(-length * 0.5f, 0.6f, 0f),
                new Vector3(0.25f, 1.2f, 0.25f), brown);
            Part(PrimitiveType.Cube, root.transform, new Vector3( length * 0.5f, 0.6f, 0f),
                new Vector3(0.25f, 1.2f, 0.25f), brown);
        }

        /// <summary>Corn field visual — rows of tall yellow-topped stalks planted
        /// in brown dirt rows. Sized to CornCols × CornRows from FarmCatalog.</summary>
        public static void CornField(GameObject go)
        {
            int cols = MiniMart.Catalog.FarmCatalog.CornCols;
            int rows = MiniMart.Catalog.FarmCatalog.CornRows;
            var dirt   = new Color(0.42f, 0.28f, 0.18f);
            var stalk  = new Color(0.35f, 0.68f, 0.30f);
            var cob    = new Color(0.98f, 0.82f, 0.24f);
            float spacing = 0.85f;

            // Brown dirt bed
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.03f, 0f),
                new Vector3(cols * spacing + 0.4f, 0.06f, rows * spacing + 0.4f), dirt);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float x = (c - (cols - 1) * 0.5f) * spacing;
                    float z = (r - (rows - 1) * 0.5f) * spacing;
                    // Green stalk
                    Part(PrimitiveType.Cube, go.transform,
                        new Vector3(x, 0.55f, z), new Vector3(0.10f, 1.0f, 0.10f), stalk);
                    // Yellow cob at the top
                    Part(PrimitiveType.Cylinder, go.transform,
                        new Vector3(x, 0.95f, z), new Vector3(0.14f, 0.18f, 0.14f), cob);
                    // Small leaves
                    var leafA = Part(PrimitiveType.Cube, go.transform,
                        new Vector3(x - 0.14f, 0.7f, z), new Vector3(0.28f, 0.06f, 0.12f), stalk);
                    leafA.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                    var leafB = Part(PrimitiveType.Cube, go.transform,
                        new Vector3(x + 0.14f, 0.7f, z), new Vector3(0.28f, 0.06f, 0.12f), stalk);
                    leafB.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                }
            }
        }

        /// <summary>Hay Feed Trough for the cow (Livestock West). A wooden box
        /// with piled golden hay inside.</summary>
        public static void HayFeedTrough(GameObject go)
        {
            var wood = new Color(0.55f, 0.38f, 0.22f);
            var hay  = new Color(0.92f, 0.82f, 0.40f);
            // Box body
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.18f, 0f), new Vector3(1.4f, 0.35f, 0.7f), wood);
            // Inner darker back panel
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.30f, -0.28f), new Vector3(1.35f, 0.30f, 0.05f),
                new Color(wood.r * 0.75f, wood.g * 0.75f, wood.b * 0.75f));
            // Piled hay
            for (int i = 0; i < 5; i++)
            {
                float ox = -0.5f + i * 0.25f;
                float oy = 0.42f + (i % 2) * 0.05f;
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(ox, oy, 0f), new Vector3(0.32f, 0.20f, 0.28f), hay);
            }
        }

        /// <summary>Coffee dispenser — a tall brown-and-chrome cabinet with a
        /// glass window showing a coffee pot. Pink-café standing feature.</summary>
        public static void CoffeeDispenser(GameObject go)
        {
            var body   = new Color(0.35f, 0.22f, 0.15f); // dark brown
            var chrome = new Color(0.80f, 0.82f, 0.85f);
            var glass  = new Color(0.55f, 0.75f, 0.95f, 1f);
            var potDark= new Color(0.15f, 0.10f, 0.08f);

            // Body cabinet
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.85f, 0f), new Vector3(1.2f, 1.7f, 0.9f), body);
            // Chrome trim at the top
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 1.72f, 0f), new Vector3(1.25f, 0.12f, 0.95f), chrome);
            // Glass dispenser window (front)
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 1.0f, 0.42f), new Vector3(0.7f, 0.55f, 0.06f), glass);
            // Coffee pot silhouette behind glass
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3(0f, 1.05f, 0.35f), new Vector3(0.28f, 0.40f, 0.28f), potDark);
            // Spout below the glass
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.55f, 0.42f), new Vector3(0.16f, 0.10f, 0.12f), chrome);
            // Drip tray
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.32f, 0.42f), new Vector3(0.5f, 0.06f, 0.20f), chrome);
        }

        // ─── Store Furniture ─────────────────────────────────────────────────

        /// <summary>Fridge / freezer unit for cold-chain items (milk, cheese, dairy).
        /// Reference: a boxy white cabinet with a blue glass-door front panel and
        /// visible product silhouettes behind the glass.</summary>
        public static void FridgeUnit(GameObject go, Color itemColor)
        {
            FloorMat(go.transform, 1.9f, 1.3f);

            var whiteBody   = new Color(0.94f, 0.95f, 0.97f);
            var glassBlue   = new Color(0.55f, 0.82f, 0.95f, 1f);
            var trim        = new Color(0.75f, 0.78f, 0.82f);

            // Main body
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.85f, -0.25f), new Vector3(1.4f, 1.7f, 0.55f), whiteBody);
            // Blue glass door
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.85f, 0.02f), new Vector3(1.25f, 1.5f, 0.05f), glassBlue);
            // Door frame trim
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 1.65f, 0.05f), new Vector3(1.35f, 0.08f, 0.06f), trim);
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.05f, 0.05f), new Vector3(1.35f, 0.08f, 0.06f), trim);
            // Handle
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0.55f, 0.85f, 0.07f), new Vector3(0.06f, 0.6f, 0.05f), trim);

            // Products behind the glass — 3 rows of the item color.
            for (int row = 0; row < 3; row++)
            {
                float y = 0.32f + row * 0.5f;
                for (int j = 0; j < 3; j++)
                {
                    float x = -0.35f + j * 0.35f;
                    Part(PrimitiveType.Cylinder, go.transform,
                        new Vector3(x, y, -0.1f), new Vector3(0.12f, 0.16f, 0.12f), itemColor);
                }
            }
        }

        /// <summary>Office desk with computer, chair, and bookshelf — reference has
        /// this in the top-right corner as an in-world CARRY/SPEED upgrade point.</summary>
        public static void OfficeDesk(GameObject go)
        {
            var deskWood = new Color(0.62f, 0.45f, 0.28f);
            var chairSeat = new Color(0.35f, 0.30f, 0.38f);
            var monitor = new Color(0.15f, 0.18f, 0.22f);
            var screen  = new Color(0.42f, 0.68f, 0.85f);
            var paper   = new Color(0.95f, 0.95f, 0.90f);
            var bookRed = new Color(0.78f, 0.25f, 0.22f);
            var bookBlu = new Color(0.28f, 0.55f, 0.78f);
            var bookGrn = new Color(0.30f, 0.65f, 0.32f);

            // Desk
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.42f, 0f), new Vector3(1.7f, 0.08f, 1.0f), deskWood);
            // Desk legs
            Part(PrimitiveType.Cube, go.transform, new Vector3(-0.75f, 0.20f,  0.4f), new Vector3(0.10f, 0.42f, 0.10f), deskWood);
            Part(PrimitiveType.Cube, go.transform, new Vector3( 0.75f, 0.20f,  0.4f), new Vector3(0.10f, 0.42f, 0.10f), deskWood);
            Part(PrimitiveType.Cube, go.transform, new Vector3(-0.75f, 0.20f, -0.4f), new Vector3(0.10f, 0.42f, 0.10f), deskWood);
            Part(PrimitiveType.Cube, go.transform, new Vector3( 0.75f, 0.20f, -0.4f), new Vector3(0.10f, 0.42f, 0.10f), deskWood);

            // Monitor (base + stand + screen)
            Part(PrimitiveType.Cube, go.transform, new Vector3(-0.4f, 0.50f, -0.25f), new Vector3(0.35f, 0.04f, 0.20f), monitor);
            Part(PrimitiveType.Cube, go.transform, new Vector3(-0.4f, 0.62f, -0.25f), new Vector3(0.05f, 0.22f, 0.05f), monitor);
            Part(PrimitiveType.Cube, go.transform, new Vector3(-0.4f, 0.85f, -0.25f), new Vector3(0.55f, 0.36f, 0.06f), monitor);
            Part(PrimitiveType.Cube, go.transform, new Vector3(-0.4f, 0.85f, -0.22f), new Vector3(0.48f, 0.30f, 0.03f), screen);
            // Papers stacked next to the monitor
            Part(PrimitiveType.Cube, go.transform, new Vector3(0.35f, 0.48f, -0.20f), new Vector3(0.30f, 0.02f, 0.24f), paper);
            Part(PrimitiveType.Cube, go.transform, new Vector3(0.30f, 0.50f, -0.24f), new Vector3(0.28f, 0.02f, 0.22f), paper);

            // Office chair
            Part(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.35f, 1.0f), new Vector3(0.55f, 0.08f, 0.5f), chairSeat);
            Part(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.65f, 1.25f), new Vector3(0.55f, 0.55f, 0.06f), chairSeat);
            Part(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.15f, 1.0f), new Vector3(0.06f, 0.35f, 0.06f), monitor);

            // Bookshelf next to the desk
            Part(PrimitiveType.Cube, go.transform, new Vector3(1.35f, 0.60f, 0f), new Vector3(0.30f, 1.2f, 1.0f), deskWood);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.30f + i * 0.35f;
                Part(PrimitiveType.Cube, go.transform, new Vector3(1.35f, y, -0.30f), new Vector3(0.24f, 0.26f, 0.08f), bookRed);
                Part(PrimitiveType.Cube, go.transform, new Vector3(1.35f, y, -0.15f), new Vector3(0.24f, 0.26f, 0.08f), bookBlu);
                Part(PrimitiveType.Cube, go.transform, new Vector3(1.35f, y,  0.00f), new Vector3(0.24f, 0.26f, 0.08f), bookGrn);
            }
        }

        /// <summary>Next-Mart expansion pad marker: a large tan tile plot with a
        /// delivery truck silhouette parked on it, indicating the future
        /// expansion area unlocked by paying the pad cost.</summary>
        public static void NextMartPreview(GameObject go)
        {
            var tan    = new Color(0.90f, 0.80f, 0.65f);
            var truck  = new Color(0.85f, 0.35f, 0.20f);
            var wheels = new Color(0.15f, 0.15f, 0.18f);
            var cab    = new Color(0.95f, 0.95f, 0.98f);

            // Tan floor tile (the "pending" store space)
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0f, 0.02f, 0f), new Vector3(8f, 0.04f, 10f), tan);

            // Delivery truck silhouette parked on it
            Part(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.55f, 0f), new Vector3(2.4f, 1.0f, 4.5f), truck);
            Part(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.65f, 2.0f), new Vector3(2.2f, 0.9f, 1.2f), cab);
            // Wheels
            Part(PrimitiveType.Cylinder, go.transform, new Vector3(-1.10f, 0.20f,  1.8f), new Vector3(0.45f, 0.20f, 0.45f), wheels);
            Part(PrimitiveType.Cylinder, go.transform, new Vector3( 1.10f, 0.20f,  1.8f), new Vector3(0.45f, 0.20f, 0.45f), wheels);
            Part(PrimitiveType.Cylinder, go.transform, new Vector3(-1.10f, 0.20f, -1.4f), new Vector3(0.45f, 0.20f, 0.45f), wheels);
            Part(PrimitiveType.Cylinder, go.transform, new Vector3( 1.10f, 0.20f, -1.4f), new Vector3(0.45f, 0.20f, 0.45f), wheels);
        }

        /// <summary>Reference-style shelf: a short, low crate with a warm wood body and
        /// a wide, clearly-visible row of chunky item shapes across the top. Reads
        /// as "a stand" from the tilted camera, not a solid colored tower.</summary>
        public static void ShelfUnit(GameObject go, Color itemColor)
        {
            FloorMat(go.transform, 1.8f, 1.4f);

            // Warm wooden base — chunky and short so items sit at hand height.
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.28f, 0), new Vector3(1.55f, 0.56f, 0.85f), WOOD);
            // A slightly lighter top rim so the crate reads with depth.
            var rim = new Color(WOOD.r + 0.10f, WOOD.g + 0.07f, WOOD.b + 0.03f);
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.58f, 0), new Vector3(1.62f, 0.06f, 0.92f), rim);

            // Two shallow angled "back panel" wings that echo the reference
            // display racks (bread/cookie/canned).
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.75f, -0.38f), new Vector3(1.55f, 0.32f, 0.06f), rim);

            // Product row: 6 chunky bricks packed in a 3×2 grid — reads as a
            // fully stocked shelf from a distance without stealing the palette.
            for (int r = 0; r < 2; r++)
            for (int c = 0; c < 3; c++)
            {
                float x = -0.44f + c * 0.44f;
                float z = -0.20f + r * 0.32f;
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(x, 0.72f, z), new Vector3(0.34f, 0.24f, 0.24f), itemColor);
            }
        }

        /// <summary>Wooden crate display box with items piled inside.</summary>
        public static void CrateDisplay(GameObject go, Color itemColor)
        {
            FloorMat(go.transform, 1.6f, 1.2f);
            // Crate body (open-top box)
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.3f, 0), new Vector3(1.0f, 0.6f, 0.7f), WOOD);
            // Front panel (slightly lower to show items)
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.2f, 0.38f), new Vector3(0.95f, 0.4f, 0.04f),
                new Color(WOOD.r + 0.08f, WOOD.g + 0.05f, WOOD.b + 0.02f));
            // Items piled inside
            float[] xOff = { -0.25f, 0f, 0.25f, -0.12f, 0.12f };
            float[] zOff = { -0.1f, 0.05f, -0.1f, 0.1f, 0f };
            for (int i = 0; i < 5; i++)
            {
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(xOff[i], 0.58f + i * 0.06f, zOff[i]),
                    new Vector3(0.18f, 0.18f, 0.18f), itemColor);
            }
        }

        /// <summary>Cash counter with teal body and register.</summary>
        public static void CashCounter(GameObject go)
        {
            FloorMat(go.transform, 2.6f, 1.6f);
            // Counter body
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.42f, 0), new Vector3(2.0f, 0.84f, 0.65f), TEAL);
            // Register screen (dark)
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(-0.5f, 0.92f, 0), new Vector3(0.3f, 0.2f, 0.18f), new Color(0.15f, 0.15f, 0.18f));
            // Register base
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(-0.5f, 0.85f, 0), new Vector3(0.4f, 0.04f, 0.35f), Color.gray);
            // Cash tray
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0.4f, 0.86f, 0), new Vector3(0.35f, 0.04f, 0.25f), new Color(0.2f, 0.2f, 0.2f));
        }

        /// <summary>Machine with type-specific details (Oven/Blender/Mill).</summary>
        public static void MachineVisual(GameObject go, string machineType)
        {
            FloorMat(go.transform, 2.0f, 1.6f);
            Color machBody = new Color(0.78f, 0.75f, 0.70f);

            // Main body
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.45f, 0), new Vector3(0.9f, 0.9f, 0.7f), machBody);

            if (machineType == "Oven")
            {
                // Oven door (dark glass)
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(0, 0.4f, 0.36f), new Vector3(0.7f, 0.55f, 0.02f), new Color(0.2f, 0.2f, 0.25f));
                // Handle
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(0, 0.62f, 0.38f), new Vector3(0.5f, 0.04f, 0.04f), new Color(0.6f, 0.6f, 0.6f));
                // Top burner plates
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(-0.2f, 0.91f, 0), new Vector3(0.25f, 0.01f, 0.25f), new Color(0.25f, 0.25f, 0.28f));
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(0.2f, 0.91f, 0), new Vector3(0.2f, 0.01f, 0.2f), new Color(0.25f, 0.25f, 0.28f));
            }
            else if (machineType == "Blender")
            {
                // Blender jar (cylinder)
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(0, 1.0f, 0), new Vector3(0.3f, 0.2f, 0.3f), new Color(0.8f, 0.85f, 0.95f));
                // Lid
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(0, 1.22f, 0), new Vector3(0.15f, 0.02f, 0.15f), new Color(0.3f, 0.3f, 0.3f));
            }
            else if (machineType == "Stove")
            {
                // Cooking station: dark top plate + frying pan + knob strip.
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(0, 0.91f, 0), new Vector3(0.85f, 0.04f, 0.62f), new Color(0.18f, 0.18f, 0.2f));
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(-0.12f, 0.97f, 0), new Vector3(0.42f, 0.03f, 0.42f), new Color(0.1f, 0.1f, 0.12f));
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(-0.12f, 1.0f, 0), new Vector3(0.3f, 0.015f, 0.3f), new Color(1f, 0.82f, 0.25f)); // "omelette"
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(0.3f, 0.97f, 0.05f), new Vector3(0.35f, 0.03f, 0.06f), new Color(0.55f, 0.55f, 0.58f)); // handle
            }
            else if (machineType == "LeafProcessor")
            {
                // Leaf processor: green hopper + press drum.
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(0, 1.05f, 0), new Vector3(0.6f, 0.35f, 0.5f), new Color(0.3f, 0.65f, 0.3f));
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(0.32f, 0.5f, 0.2f), new Vector3(0.22f, 0.18f, 0.22f), new Color(0.4f, 0.42f, 0.45f));
            }
            else // Mill
            {
                // Funnel top
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(0, 1.0f, 0), new Vector3(0.45f, 0.1f, 0.45f), new Color(0.65f, 0.58f, 0.48f));
                // Spout
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(0.4f, 0.35f, 0), new Vector3(0.2f, 0.12f, 0.15f), Color.gray);
            }
        }

        /// <summary>Dustbin with white body, red band, and lid.</summary>
        public static void Dustbin(GameObject go)
        {
            // Body
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3(0, 0.3f, 0), new Vector3(0.45f, 0.3f, 0.45f), Color.white);
            // Red band
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3(0, 0.38f, 0), new Vector3(0.47f, 0.06f, 0.47f), Color.red);
            // Lid
            Part(PrimitiveType.Cylinder, go.transform,
                new Vector3(0, 0.62f, 0), new Vector3(0.5f, 0.02f, 0.5f), new Color(0.88f, 0.88f, 0.88f));
        }

        /// <summary>Door frame with posts and lintel.</summary>
        public static void Door(GameObject go, bool isEntry)
        {
            Color doorCol = isEntry ? new Color(0.55f, 0.38f, 0.22f) : new Color(0.45f, 0.45f, 0.48f);
            // Frame posts
            Part(PrimitiveType.Cube, go.transform, new Vector3(-0.55f, 0.6f, 0), new Vector3(0.12f, 1.2f, 0.12f), doorCol);
            Part(PrimitiveType.Cube, go.transform, new Vector3(0.55f, 0.6f, 0), new Vector3(0.12f, 1.2f, 0.12f), doorCol);
            // Lintel
            Part(PrimitiveType.Cube, go.transform, new Vector3(0, 1.22f, 0), new Vector3(1.2f, 0.12f, 0.12f), doorCol);
            // Striped welcome mat for entry
            if (isEntry)
            {
                Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.02f, 0.4f), new Vector3(1.0f, 0.04f, 0.5f), new Color(0.9f, 0.3f, 0.3f));
                Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.03f, 0.4f), new Vector3(0.3f, 0.04f, 0.5f), Color.white);
            }
        }

        // ─── Item Color Mapping ──────────────────────────────────────────────

        /// <summary>Returns a color representing items for each product type.</summary>
        public static Color ItemColor(MiniMart.Core.ItemType item)
        {
            switch (item)
            {
                case Core.ItemType.Egg:            return new Color(1f, 0.97f, 0.88f);
                case Core.ItemType.Tomato:         return new Color(0.9f, 0.15f, 0.1f);
                case Core.ItemType.TomatoKetchup:  return new Color(0.82f, 0.12f, 0.10f); // deep red — tomato sauce, not green
                case Core.ItemType.Wheat:          return new Color(0.92f, 0.82f, 0.45f);
                case Core.ItemType.WheatFlour:     return new Color(0.96f, 0.93f, 0.88f);
                case Core.ItemType.Bread:          return new Color(0.82f, 0.62f, 0.32f);
                case Core.ItemType.Milk:           return new Color(0.95f, 0.97f, 1f);
                case Core.ItemType.Cheese:         return new Color(1f, 0.83f, 0.25f);
                case Core.ItemType.Herb:           return new Color(0.25f, 0.62f, 0.22f);
                case Core.ItemType.HerbPack:       return new Color(0.45f, 0.85f, 0.50f);
                case Core.ItemType.FriedEgg:       return new Color(1f, 0.78f, 0.15f);
                // New spec colours.
                case Core.ItemType.Corn:           return new Color(0.98f, 0.78f, 0.20f); // corn cob yellow
                case Core.ItemType.ProcessedCorn:  return new Color(0.85f, 0.55f, 0.15f); // canned/processed corn
                case Core.ItemType.Cookie:         return new Color(0.75f, 0.52f, 0.28f); // baked cookie brown
                case Core.ItemType.Coffee:         return new Color(0.32f, 0.20f, 0.12f); // dark coffee brown
                default:                           return Color.white;
            }
        }

        /// <summary>One distinct primitive silhouette per SKU, used by shelves,
        /// storage racks and any other stock display. Colour alone was the only
        /// differentiator before (identical tinted cubes) — weak at isometric
        /// distance and useless for colourblind players. Shape + colour together
        /// give every item a recognisable identity everywhere it appears.
        /// Returns a root GameObject so callers can toggle/destroy as one unit.</summary>
        public static GameObject ItemMesh(MiniMart.Core.ItemType item, Transform parent, Vector3 localPos, float s = 1f)
        {
            var root = new GameObject($"Item_{item}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            var t = root.transform;
            Color col = ItemColor(item);

            switch (item)
            {
                case Core.ItemType.Egg: // white ellipsoid
                    Part(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.30f, 0.40f, 0.30f) * s, col);
                    break;

                case Core.ItemType.Tomato: // red sphere + green stem nub
                    Part(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.38f, 0.34f, 0.38f) * s, col);
                    Part(PrimitiveType.Cube, t, new Vector3(0, 0.20f, 0) * s,
                        new Vector3(0.08f, 0.10f, 0.08f) * s, new Color(0.25f, 0.60f, 0.25f));
                    break;

                case Core.ItemType.TomatoKetchup: // slim red bottle + white cap
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.20f, 0.22f, 0.20f) * s, col);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, 0.28f, 0) * s,
                        new Vector3(0.10f, 0.07f, 0.10f) * s, new Color(0.95f, 0.95f, 0.92f));
                    break;

                case Core.ItemType.Bread: // tan loaf w/ lighter crust cap
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.46f, 0.24f, 0.28f) * s, col);
                    Part(PrimitiveType.Cube, t, new Vector3(0, 0.14f, 0) * s,
                        new Vector3(0.42f, 0.10f, 0.24f) * s, new Color(0.92f, 0.76f, 0.48f));
                    break;

                case Core.ItemType.Wheat: // sheaf of three thin stalks
                    Part(PrimitiveType.Cylinder, t, new Vector3(-0.08f, 0, 0) * s, new Vector3(0.07f, 0.24f, 0.07f) * s, col);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0.08f, 0, 0.03f) * s, new Vector3(0.07f, 0.22f, 0.07f) * s, col);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, 0.02f, -0.06f) * s, new Vector3(0.07f, 0.26f, 0.07f) * s, col);
                    break;

                case Core.ItemType.WheatFlour: // white sack w/ tied top
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.34f, 0.32f, 0.26f) * s, col);
                    Part(PrimitiveType.Sphere, t, new Vector3(0, 0.20f, 0) * s,
                        new Vector3(0.14f, 0.10f, 0.14f) * s, new Color(0.85f, 0.82f, 0.76f));
                    break;

                case Core.ItemType.Milk: // white carton w/ blue band
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.26f, 0.40f, 0.26f) * s, col);
                    Part(PrimitiveType.Cube, t, new Vector3(0, 0.06f, 0) * s,
                        new Vector3(0.28f, 0.10f, 0.28f) * s, new Color(0.35f, 0.55f, 0.90f));
                    break;

                case Core.ItemType.BottledMilk: // bottle: white cylinder + narrow neck
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.22f, 0.18f, 0.22f) * s, col);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, 0.24f, 0) * s, new Vector3(0.12f, 0.08f, 0.12f) * s, col);
                    break;

                case Core.ItemType.Cheese: // yellow wedge (45° rotated cube)
                {
                    var wedge = Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.36f, 0.36f, 0.30f) * s, col);
                    wedge.transform.localRotation = Quaternion.Euler(0, 0, 45f);
                    break;
                }

                case Core.ItemType.Cookie: // flat tan disc + chip dots
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.36f, 0.05f, 0.36f) * s, col);
                    Part(PrimitiveType.Sphere, t, new Vector3(0.08f, 0.05f, 0.05f) * s,
                        new Vector3(0.06f, 0.04f, 0.06f) * s, new Color(0.35f, 0.22f, 0.12f));
                    Part(PrimitiveType.Sphere, t, new Vector3(-0.09f, 0.05f, -0.04f) * s,
                        new Vector3(0.06f, 0.04f, 0.06f) * s, new Color(0.35f, 0.22f, 0.12f));
                    break;

                case Core.ItemType.CannedTomato: // grey tin + red label band
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.26f, 0.18f, 0.26f) * s,
                        new Color(0.75f, 0.75f, 0.78f));
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.27f, 0.08f, 0.27f) * s, col);
                    break;

                case Core.ItemType.Coffee: // dark cup + saucer
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, 0.06f, 0) * s, new Vector3(0.22f, 0.12f, 0.22f) * s, col);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, -0.06f, 0) * s,
                        new Vector3(0.32f, 0.02f, 0.32f) * s, new Color(0.92f, 0.90f, 0.86f));
                    break;

                case Core.ItemType.Apple: // red sphere, slightly taller than tomato, brown stem
                    Part(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.32f, 0.36f, 0.32f) * s, col);
                    Part(PrimitiveType.Cube, t, new Vector3(0, 0.22f, 0) * s,
                        new Vector3(0.05f, 0.10f, 0.05f) * s, new Color(0.45f, 0.30f, 0.15f));
                    break;

                case Core.ItemType.Corn: // yellow cob + green husk leaf
                    Part(PrimitiveType.Capsule, t, Vector3.zero, new Vector3(0.18f, 0.20f, 0.18f) * s, col);
                    Part(PrimitiveType.Cube, t, new Vector3(0.10f, -0.08f, 0) * s,
                        new Vector3(0.08f, 0.26f, 0.04f) * s, new Color(0.35f, 0.68f, 0.30f));
                    break;

                case Core.ItemType.Herb: // leafy green sprig
                    Part(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.26f, 0.18f, 0.26f) * s, col);
                    Part(PrimitiveType.Sphere, t, new Vector3(0.10f, 0.10f, 0.05f) * s, new Vector3(0.16f, 0.12f, 0.16f) * s, col);
                    break;

                case Core.ItemType.HerbPack: // green box w/ white wrap band
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.34f, 0.20f, 0.26f) * s, col);
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.10f, 0.22f, 0.28f) * s,
                        new Color(0.95f, 0.95f, 0.92f));
                    break;

                case Core.ItemType.FriedEgg: // white disc + yolk dome
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.34f, 0.03f, 0.34f) * s,
                        new Color(0.97f, 0.96f, 0.92f));
                    Part(PrimitiveType.Sphere, t, new Vector3(0, 0.05f, 0) * s, new Vector3(0.16f, 0.10f, 0.16f) * s, col);
                    break;

                case Core.ItemType.Dough: // pale rounded blob
                case Core.ItemType.CookieDough:
                    Part(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.34f, 0.24f, 0.34f) * s, col);
                    break;

                case Core.ItemType.ProcessedCorn: // tin w/ yellow band
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.26f, 0.18f, 0.26f) * s,
                        new Color(0.75f, 0.75f, 0.78f));
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.27f, 0.08f, 0.27f) * s, col);
                    break;

                default: // safety net for future items
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(0.34f, 0.34f, 0.34f) * s, col);
                    break;
            }
            return root;
        }

        /// <summary>Raw items use crate display, processed items use shelf unit.</summary>
        public static bool IsRawItem(MiniMart.Core.ItemType item)
        {
            return item == Core.ItemType.Egg
                || item == Core.ItemType.Tomato
                || item == Core.ItemType.Wheat
                || item == Core.ItemType.Milk
                || item == Core.ItemType.Herb
                || item == Core.ItemType.Corn;
        }

        /// <summary>Apple orchard: a row of small trees with red apple dots in the
        /// canopy, on a grass-toned root patch. Sized to AppleTreeCount.</summary>
        public static void AppleOrchard(GameObject go)
        {
            int count = MiniMart.Catalog.FarmCatalog.AppleTreeCount;
            var trunk  = new Color(0.48f, 0.32f, 0.16f);
            var canopy = new Color(0.30f, 0.62f, 0.26f);
            var apple  = new Color(0.90f, 0.15f, 0.10f);
            float spacing = 1.6f;

            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) * 0.5f) * spacing;
                Part(PrimitiveType.Cylinder, go.transform,
                    new Vector3(x, 0.45f, 0f), new Vector3(0.18f, 0.45f, 0.18f), trunk);
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(x, 1.15f, 0f), new Vector3(1.0f, 0.85f, 1.0f), canopy);
                // Three visible apples per canopy, offset so they read from the
                // isometric camera.
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(x - 0.25f, 1.05f, 0.38f), new Vector3(0.16f, 0.16f, 0.16f), apple);
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(x + 0.28f, 1.25f, 0.30f), new Vector3(0.16f, 0.16f, 0.16f), apple);
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(x + 0.02f, 0.95f, 0.42f), new Vector3(0.16f, 0.16f, 0.16f), apple);
            }
        }

        /// <summary>Herb patch: dark soil bed with leafy green bushes (reference herb/leaf zone).</summary>
        public static void HerbPatch(GameObject go)
        {
            // Soil bed
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.05f, 0), new Vector3(3.2f, 0.1f, 2.2f), new Color(0.32f, 0.24f, 0.16f));
            // Bushes (2x2)
            var leaf = new Color(0.25f, 0.62f, 0.22f);
            var leafLight = new Color(0.38f, 0.75f, 0.32f);
            for (int i = 0; i < 4; i++)
            {
                float bx = (i % 2 == 0) ? -0.8f : 0.8f;
                float bz = (i < 2) ? -0.55f : 0.55f;
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(bx, 0.35f, bz), new Vector3(0.75f, 0.55f, 0.75f), leaf);
                Part(PrimitiveType.Sphere, go.transform,
                    new Vector3(bx + 0.18f, 0.55f, bz - 0.1f), new Vector3(0.45f, 0.35f, 0.45f), leafLight);
            }
        }
    }
}
