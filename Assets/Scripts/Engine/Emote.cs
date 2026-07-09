using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// One-shot floating emote graphic that rises and fades above a character —
    /// the reference game's transaction/mood feedback. Instead of ":)" text we
    /// build a small round face (yellow disc with eyes + mouth), a heart, an
    /// angry brow, or a question dot — matching the reference iconography.
    /// </summary>
    public class Emote : MonoBehaviour
    {
        public enum Kind { Happy, Heart, Angry, SoldOut, Trash }

        private float life;
        private const float Lifetime = 1.3f;
        private const float RiseSpeed = 0.9f;
        private GameObject face;

        public static void Spawn(Vector3 position, Kind kind)
        {
            var go = new GameObject($"Emote_{kind}");
            go.transform.position = position + new Vector3(0, 2.1f, 0);
            var e = go.AddComponent<Emote>();
            e.face = BuildFace(go.transform, kind);
            go.AddComponent<Billboard>();
        }

        // Legacy text-based path kept for callers that pass a raw symbol (e.g. the
        // bin dumps still call Spawn(pos, "x", grey)). Maps common symbols to a Kind.
        public static void Spawn(Vector3 position, string symbol, Color _color)
        {
            Kind k = symbol switch
            {
                ":)" => Kind.Happy,
                "<3" => Kind.Heart,
                ">:(" => Kind.Angry,
                "?"  => Kind.SoldOut,
                _    => Kind.Trash,
            };
            Spawn(position, k);
        }

        private static GameObject BuildFace(Transform parent, Kind kind)
        {
            var root = new GameObject("Face");
            root.transform.SetParent(parent, false);

            switch (kind)
            {
                case Kind.Happy:
                {
                    // Yellow disc background.
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        Vector3.zero, new Vector3(0.7f, 0.7f, 0.08f),
                        new Color(1f, 0.85f, 0.15f));
                    // Two black eyes.
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        new Vector3(-0.15f, 0.08f, -0.06f), new Vector3(0.10f, 0.10f, 0.05f), Color.black);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        new Vector3( 0.15f, 0.08f, -0.06f), new Vector3(0.10f, 0.10f, 0.05f), Color.black);
                    // Smile (thin flat cube).
                    PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                        new Vector3(0f, -0.15f, -0.06f), new Vector3(0.32f, 0.06f, 0.05f), Color.black);
                    break;
                }
                case Kind.Heart:
                {
                    // Two red spheres + diamond tail = heart silhouette.
                    var red = new Color(1f, 0.35f, 0.55f);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        new Vector3(-0.15f, 0.12f, 0f), new Vector3(0.45f, 0.45f, 0.1f), red);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        new Vector3( 0.15f, 0.12f, 0f), new Vector3(0.45f, 0.45f, 0.1f), red);
                    var tail = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                        new Vector3(0f, -0.15f, 0f), new Vector3(0.42f, 0.42f, 0.1f), red);
                    tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    break;
                }
                case Kind.Angry:
                {
                    // Red disc with angry V-brows.
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        Vector3.zero, new Vector3(0.7f, 0.7f, 0.08f),
                        new Color(1f, 0.30f, 0.20f));
                    var browL = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                        new Vector3(-0.16f, 0.14f, -0.06f), new Vector3(0.22f, 0.05f, 0.04f), Color.black);
                    browL.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
                    var browR = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                        new Vector3( 0.16f, 0.14f, -0.06f), new Vector3(0.22f, 0.05f, 0.04f), Color.black);
                    browR.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
                    // Small frown.
                    PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                        new Vector3(0f, -0.16f, -0.06f), new Vector3(0.28f, 0.06f, 0.04f), Color.black);
                    break;
                }
                case Kind.SoldOut:
                {
                    // Grey disc with a white question dot (readable at distance).
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        Vector3.zero, new Vector3(0.7f, 0.7f, 0.08f),
                        new Color(0.78f, 0.78f, 0.78f));
                    PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                        new Vector3(0f, 0.06f, -0.06f), new Vector3(0.10f, 0.28f, 0.05f), Color.white);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        new Vector3(0f, -0.22f, -0.06f), new Vector3(0.12f, 0.12f, 0.05f), Color.white);
                    break;
                }
                default: // Trash / generic "x"
                {
                    PrimitiveFactory.Part(PrimitiveType.Sphere, root.transform,
                        Vector3.zero, new Vector3(0.6f, 0.6f, 0.08f),
                        new Color(0.75f, 0.75f, 0.75f));
                    var barA = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                        new Vector3(0f, 0f, -0.06f), new Vector3(0.42f, 0.08f, 0.04f), Color.black);
                    barA.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    var barB = PrimitiveFactory.Part(PrimitiveType.Cube, root.transform,
                        new Vector3(0f, 0f, -0.06f), new Vector3(0.42f, 0.08f, 0.04f), Color.black);
                    barB.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                    break;
                }
            }
            return root;
        }

        private void Update()
        {
            life += Time.deltaTime;
            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

            // Fade + gentle shrink at end.
            if (face != null)
            {
                float t = Mathf.Clamp01(life / Lifetime);
                float alpha = 1f - t;
                float scale = 1f - t * 0.15f;
                face.transform.localScale = new Vector3(scale, scale, scale);

                // Cheap fade: reduce material alpha on every renderer we own.
                foreach (var r in face.GetComponentsInChildren<MeshRenderer>())
                {
                    var m = r.material;
                    var c = m.color;
                    c.a = alpha;
                    m.color = c;
                }
            }

            if (life >= Lifetime) Destroy(gameObject);
        }

        // Convenience wrappers so existing call sites (`Emote.Happy(pos)`) still work.
        public static void Happy(Vector3 pos)   => Spawn(pos, Kind.Happy);
        public static void Heart(Vector3 pos)   => Spawn(pos, Kind.Heart);
        public static void Angry(Vector3 pos)   => Spawn(pos, Kind.Angry);
        public static void SoldOut(Vector3 pos) => Spawn(pos, Kind.SoldOut);
    }
}
