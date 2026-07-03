using UnityEngine;
using MiniMart.Characters;
using MiniMart.Core;

namespace MiniMart.Engine
{
    /// <summary>
    /// Adds a procedural bounce and wobble animation to characters when they are moving.
    /// This gives the game a much livelier, "juicy" feel without needing complex rigging.
    /// </summary>
    public class WobbleAnimator : MonoBehaviour
    {
        public float bounceSpeed = 15f;
        public float bounceHeight = 0.3f;
        public float wobbleAngle = 10f;
        public float wobbleSpeed = 10f;

        private CharacterBase character;
        private Transform visualPivot;
        
        private float animationTime;
        private Vector3 startLocalPos;
        private Quaternion startLocalRot;

        private void Awake()
        {
            character = GetComponent<CharacterBase>();
            
            // Find the child that holds the visual primitives
            // We assume it's the first child, or we create a pivot
            if (transform.childCount > 0)
            {
                visualPivot = transform.GetChild(0);
                startLocalPos = visualPivot.localPosition;
                startLocalRot = visualPivot.localRotation;
            }
        }

        private void Update()
        {
            if (visualPivot == null || character == null)
            {
                if (transform.childCount > 0)
                {
                    visualPivot = transform.GetChild(0);
                    startLocalPos = visualPivot.localPosition;
                    startLocalRot = visualPivot.localRotation;
                }
                return;
            }

            // CharacterState.Walking is what we look for, or if they have a target
            // Wait, CharacterBase sets state to Walking. When arrived, sets to Idle.
            // Check if they are actually moving based on position changes.
            bool isMoving = (character.State == CharacterState.Walking);

            if (isMoving)
            {
                animationTime += Time.deltaTime;
                
                // Sine wave for vertical bounce (absolute so they stay above ground)
                float yOffset = Mathf.Abs(Mathf.Sin(animationTime * bounceSpeed)) * bounceHeight;
                visualPivot.localPosition = startLocalPos + new Vector3(0, yOffset, 0);

                // Cosine wave for side-to-side wobble
                float zRot = Mathf.Cos(animationTime * wobbleSpeed) * wobbleAngle;
                visualPivot.localRotation = startLocalRot * Quaternion.Euler(0, 0, zRot);
            }
            else
            {
                // Smoothly return to idle pose
                animationTime = 0f;
                visualPivot.localPosition = Vector3.Lerp(visualPivot.localPosition, startLocalPos, Time.deltaTime * 10f);
                visualPivot.localRotation = Quaternion.Slerp(visualPivot.localRotation, startLocalRot, Time.deltaTime * 10f);
            }
        }
    }
}
