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

        /// <summary>Device-safe colored material. Never throws, even if shader lookup fails.</summary>
        public static Material NewColoredMaterial(Color color)
        {
            var baseMat = StdMat;
            var m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            m.color = color;
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
        public static GameObject BuildCharacter(GameObject go, Color bodyColor,
            bool chefHat = false, bool isPlayer = false)
        {
            var pivot = new GameObject("Visual");
            pivot.transform.SetParent(go.transform, false);
            var pt = pivot.transform;

            // Body
            Part(PrimitiveType.Capsule, pt, new Vector3(0, 0.5f, 0), new Vector3(0.45f, 0.5f, 0.45f), bodyColor);
            // Head
            Part(PrimitiveType.Sphere, pt, new Vector3(0, 1.15f, 0), new Vector3(0.38f, 0.38f, 0.38f), Color.white);
            // Eyes
            Part(PrimitiveType.Sphere, pt, new Vector3(-0.08f, 1.2f, 0.15f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.1f, 0.1f, 0.1f));
            Part(PrimitiveType.Sphere, pt, new Vector3(0.08f, 1.2f, 0.15f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.1f, 0.1f, 0.1f));

            if (chefHat)
            {
                // Tall white chef hat
                Part(PrimitiveType.Cylinder, pt, new Vector3(0, 1.5f, 0), new Vector3(0.28f, 0.18f, 0.28f), Color.white);
            }

            if (isPlayer)
            {
                // Backpack
                Part(PrimitiveType.Cube, pt, new Vector3(0, 0.7f, -0.22f), new Vector3(0.3f, 0.35f, 0.15f), new Color(0.3f, 0.5f, 0.9f));
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

        // ─── Store Furniture ─────────────────────────────────────────────────

        /// <summary>Tall shelf unit with backboard and items on 3 levels.</summary>
        public static void ShelfUnit(GameObject go, Color itemColor)
        {
            FloorMat(go.transform, 1.8f, 1.4f);
            // Backboard
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0, 0.72f, -0.35f), new Vector3(1.3f, 1.45f, 0.08f), WOOD);
            // Side panels
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(-0.65f, 0.72f, 0), new Vector3(0.06f, 1.45f, 0.6f), WOOD);
            Part(PrimitiveType.Cube, go.transform,
                new Vector3(0.65f, 0.72f, 0), new Vector3(0.06f, 1.45f, 0.6f), WOOD);
            // 3 shelf boards + items
            for (int i = 0; i < 3; i++)
            {
                float y = 0.18f + i * 0.42f;
                // Shelf board
                Part(PrimitiveType.Cube, go.transform,
                    new Vector3(0, y, 0), new Vector3(1.3f, 0.06f, 0.6f), WOOD);
                // Items on shelf (cylinders like cans/jars)
                for (int j = 0; j < 4; j++)
                {
                    float x = -0.4f + j * 0.28f;
                    Part(PrimitiveType.Cylinder, go.transform,
                        new Vector3(x, y + 0.16f, 0), new Vector3(0.1f, 0.1f, 0.1f), itemColor);
                }
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
                case Core.ItemType.TomatoKetchup:  return new Color(0.25f, 0.72f, 0.35f);
                case Core.ItemType.Wheat:          return new Color(0.92f, 0.82f, 0.45f);
                case Core.ItemType.WheatFlour:     return new Color(0.96f, 0.93f, 0.88f);
                case Core.ItemType.Bread:          return new Color(0.82f, 0.62f, 0.32f);
                case Core.ItemType.Milk:           return new Color(0.95f, 0.97f, 1f);
                case Core.ItemType.Cheese:         return new Color(1f, 0.83f, 0.25f);
                default:                           return Color.white;
            }
        }

        /// <summary>Raw items use crate display, processed items use shelf unit.</summary>
        public static bool IsRawItem(MiniMart.Core.ItemType item)
        {
            return item == Core.ItemType.Egg
                || item == Core.ItemType.Tomato
                || item == Core.ItemType.Wheat
                || item == Core.ItemType.Milk;
        }
    }
}
