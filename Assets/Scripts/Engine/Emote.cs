using UnityEngine;

namespace MiniMart.Engine
{
    /// <summary>
    /// One-shot floating emote (":)", "&lt;3", "?", "&gt;:(") that rises and fades above a
    /// character — the reference game's transaction/mood feedback.
    /// </summary>
    public class Emote : MonoBehaviour
    {
        private TextMesh text;
        private float life;
        private const float Lifetime = 1.3f;
        private const float RiseSpeed = 0.9f;

        public static void Spawn(Vector3 position, string symbol, Color color)
        {
            var go = new GameObject("Emote");
            go.transform.position = position + new Vector3(0, 2.1f, 0);
            var e = go.AddComponent<Emote>();

            var t = go.AddComponent<TextMesh>();
            t.text = symbol;
            t.fontSize = 52;
            t.characterSize = 0.09f;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = color;
            var font = HUDBuilder.UIFont;
            if (font != null)
            {
                t.font = font;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = font.material;
            }
            go.AddComponent<Billboard>();
            e.text = t;
        }

        private void Update()
        {
            life += Time.deltaTime;
            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);
            if (text != null)
            {
                var c = text.color;
                c.a = Mathf.Clamp01(1f - life / Lifetime);
                text.color = c;
            }
            if (life >= Lifetime) Destroy(gameObject);
        }

        // Convenience palettes for the common moods.
        public static void Happy(Vector3 pos)   => Spawn(pos, ":)", new Color(1f, 0.9f, 0.2f));
        public static void Heart(Vector3 pos)   => Spawn(pos, "<3", new Color(1f, 0.4f, 0.6f));
        public static void Angry(Vector3 pos)   => Spawn(pos, ">:(", new Color(1f, 0.3f, 0.2f));
        public static void SoldOut(Vector3 pos) => Spawn(pos, "?", new Color(0.8f, 0.8f, 0.8f));
    }
}
