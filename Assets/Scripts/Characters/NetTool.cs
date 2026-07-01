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

        public void Throw()
        {
            if (State != NetState.Ready) return;
            State = NetState.Thrown;
            timer = throwDuration;
        }

        private void Update()
        {
            if (State == NetState.Ready) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (State == NetState.Thrown)
            {
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
