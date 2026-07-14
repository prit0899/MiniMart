using System.Collections.Generic;
using UnityEngine;
using MiniMart.Map;

namespace MiniMart.Characters
{
    /// <summary>
    /// Classic Reynolds steering behaviours, on the XZ plane.
    ///
    /// Every function returns a STEERING FORCE (not a position): the difference
    /// between where the agent wants to be going and where it is going. The caller
    /// sums the forces it cares about, clamps the total, and integrates it into
    /// velocity. That accumulation is what makes movement look organic — agents
    /// bank into turns and ease out of them instead of snapping to a new heading.
    /// </summary>
    public static class Steering
    {
        /// <summary>Head straight at a target at full speed.</summary>
        public static Vector3 Seek(Vector3 pos, Vector3 velocity, Vector3 target, float maxSpeed)
        {
            Vector3 desired = target - pos;
            desired.y = 0f;
            if (desired.sqrMagnitude < 1e-6f) return Vector3.zero;
            desired = desired.normalized * maxSpeed;
            return desired - velocity;
        }

        /// <summary>Seek, but ease to a stop inside slowRadius so we don't overshoot
        /// and jitter around the goal (the "arrival" behaviour).</summary>
        public static Vector3 Arrive(Vector3 pos, Vector3 velocity, Vector3 target,
                                     float maxSpeed, float slowRadius)
        {
            Vector3 to = target - pos;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 1e-4f) return -velocity;

            float speed = dist < slowRadius ? maxSpeed * (dist / slowRadius) : maxSpeed;
            Vector3 desired = to / dist * speed;
            return desired - velocity;
        }

        /// <summary>Run away from a point (used to scatter from a thief / crowded till).</summary>
        public static Vector3 Flee(Vector3 pos, Vector3 velocity, Vector3 threat,
                                   float maxSpeed, float panicRadius)
        {
            Vector3 away = pos - threat;
            away.y = 0f;
            float d = away.magnitude;
            if (d > panicRadius || d < 1e-4f) return Vector3.zero;
            Vector3 desired = away / d * maxSpeed;
            return desired - velocity;
        }

        /// <summary>Push apart from nearby agents so workers flow around each other
        /// instead of grinding through one another. Falls off with distance.</summary>
        public static Vector3 Separation(Vector3 pos, IReadOnlyList<CharacterBase> others,
                                         CharacterBase self, float radius, float maxSpeed)
        {
            Vector3 push = Vector3.zero;
            int count = 0;

            for (int i = 0; i < others.Count; i++)
            {
                var o = others[i];
                if (o == null || o == self) continue;

                Vector3 diff = pos - o.transform.position;
                diff.y = 0f;
                float d = diff.magnitude;
                if (d > radius || d < 1e-4f) continue;

                push += diff / d * (1f - d / radius);   // closer ⇒ stronger
                count++;
            }

            if (count == 0) return Vector3.zero;
            push /= count;
            if (push.sqrMagnitude < 1e-6f) return Vector3.zero;
            return push.normalized * maxSpeed;
        }

        /// <summary>
        /// Obstacle avoidance against the world: probe ahead with three "feelers"
        /// (centre + two angled whiskers). If a feeler lands on an unwalkable cell,
        /// steer away from it. This keeps agents off walls and around furniture
        /// smoothly, rather than bumping and sliding along at right angles.
        /// </summary>
        public static Vector3 AvoidWalls(Vector3 pos, Vector3 velocity,
                                         GridPathfinder grid, float maxSpeed,
                                         float lookAhead = 1.4f)
        {
            if (grid == null) return Vector3.zero;

            Vector3 fwd = velocity;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) return Vector3.zero;
            fwd.Normalize();

            Vector3 avoid = Vector3.zero;

            // Centre feeler (full length) + whiskers at ±35° (shorter).
            ProbeFeeler(pos, fwd, lookAhead, grid, ref avoid);
            ProbeFeeler(pos, Quaternion.Euler(0f,  35f, 0f) * fwd, lookAhead * 0.6f, grid, ref avoid);
            ProbeFeeler(pos, Quaternion.Euler(0f, -35f, 0f) * fwd, lookAhead * 0.6f, grid, ref avoid);

            if (avoid.sqrMagnitude < 1e-6f) return Vector3.zero;
            return avoid.normalized * maxSpeed - velocity;
        }

        private static void ProbeFeeler(Vector3 pos, Vector3 dir, float len,
                                        GridPathfinder grid, ref Vector3 avoid)
        {
            Vector3 tip = pos + dir * len;
            if (grid.IsWalkableWorld(tip)) return;

            // Blocked: push back along the feeler, away from the obstruction.
            Vector3 back = pos - tip;
            back.y = 0f;
            if (back.sqrMagnitude > 1e-6f) avoid += back.normalized;
        }

        /// <summary>Clamp a force to a maximum magnitude.</summary>
        public static Vector3 Clamp(Vector3 force, float max)
        {
            float sq = force.sqrMagnitude;
            if (sq > max * max && sq > 1e-6f) return force / Mathf.Sqrt(sq) * max;
            return force;
        }
    }
}
