using UnityEngine;

namespace MiniMart.Characters
{
    public enum NetState { Ready, Thrown, Cooldown }

    /// <summary>Simple state machine for the player's net per Architecture Spec Section 6.</summary>
    public class NetTool : MonoBehaviour
    {
        public NetState State = NetState.Ready;
        public float throwDuration = 0.3f;
        public float cooldownDuration = 1.5f;
        private float timer;
        private GameObject netVisual;

        public void Throw()
        {
            if (State != NetState.Ready) return;
            State = NetState.Thrown;
            timer = throwDuration;

            // Spawn a visual net (semi-transparent sphere)
            netVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = netVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            netVisual.transform.SetParent(transform, false);
            netVisual.transform.localPosition = new Vector3(0, 0.5f, 1.5f); // Throw in front
            netVisual.transform.localScale = Vector3.zero;

            var mr = netVisual.GetComponent<MeshRenderer>();
            // Shader.Find can return null in device builds — never pass it straight
            // into new Material() (ArgumentNullException: shader).
            var standard = Shader.Find("Standard");
            Material mat;
            if (standard != null)
            {
                mat = new Material(standard);
                // Set Standard shader to Transparent mode
                mat.SetFloat("_Mode", 3f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            else
            {
                mat = MiniMart.Engine.PrimitiveFactory.NewColoredMaterial(Color.white);
            }
            mat.color = new Color(0f, 0.8f, 1f, 0.4f); // Cyan semi-transparent
            if (mr != null) mr.material = mat;
        }

        private void Update()
        {
            if (State == NetState.Ready) return;
            timer -= Time.deltaTime;

            if (State == NetState.Thrown && netVisual != null)
            {
                // Smoothly scale up the net visual
                float progress = Mathf.Clamp01((throwDuration - timer) / throwDuration);
                float currentScale = Mathf.Lerp(0f, 4f, progress); // Expands up to size 4
                netVisual.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            }

            if (timer > 0f) return;

            if (State == NetState.Thrown)
            {
                if (netVisual != null)
                {
                    Destroy(netVisual);
                    netVisual = null;
                }
                State = NetState.Cooldown;
                timer = cooldownDuration;
            }
            else if (State == NetState.Cooldown)
            {
                State = NetState.Ready;
            }
        }
    }
}
