using System.Collections.Generic;
using UnityEngine;

namespace MiniMart.Map
{
    /// <summary>
    /// Simplified navigation mesh built from the walkability grid.
    ///
    /// The old system ran A* directly on grid cells with 4-way neighbours, so every
    /// path was a Manhattan staircase and workers turned in hard 90° corners. This
    /// replaces that with the standard approach:
    ///
    ///   1. BAKE      — merge walkable cells into a small set of maximal convex
    ///                  rectangles ("walkable polygons"). A 100x82 grid collapses
    ///                  from ~8000 cells into a few dozen polys.
    ///   2. LINK      — polys that share an edge get a "portal": the world-space
    ///                  segment you can walk through between them.
    ///   3. A*        — search the poly graph (a few dozen nodes, not thousands).
    ///   4. FUNNEL    — string-pull the portal corridor (Simple Stupid Funnel
    ///                  Algorithm) to get the true shortest path inside it. This is
    ///                  what produces natural diagonal lines that hug corners
    ///                  instead of axis-aligned zig-zags.
    ///
    /// The result is a short list of real corner points, which the steering layer
    /// then follows smoothly (see Characters/Steering.cs).
    /// </summary>
    public class NavMesh : MonoBehaviour
    {
        public static NavMesh Instance { get; private set; }

        private class Poly
        {
            public int xMin, xMax, zMin, zMax;      // cell-space bounds (inclusive)
            public Vector3 Center;
            public readonly List<int> Links = new List<int>();       // neighbour poly ids
            public readonly List<(Vector3 a, Vector3 b)> Portals = new List<(Vector3, Vector3)>();
        }

        private readonly List<Poly> polys = new List<Poly>();
        private int[,] cellToPoly;                  // -1 when unwalkable
        private GridPathfinder grid;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Bakes the mesh from the grid. Call AFTER all walls are stamped in.</summary>
        public void Bake(GridPathfinder pathfinder)
        {
            grid = pathfinder;
            polys.Clear();

            int w = grid.GridWidth, h = grid.GridHeight;
            cellToPoly = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    cellToPoly[x, z] = -1;

            // ── 1. Greedy maximal-rectangle merge ─────────────────────────────
            for (int z = 0; z < h; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (cellToPoly[x, z] != -1 || !Walkable(x, z)) continue;

                    // Grow right as far as we can.
                    int xMax = x;
                    while (xMax + 1 < w && cellToPoly[xMax + 1, z] == -1 && Walkable(xMax + 1, z))
                        xMax++;

                    // Grow up while the whole row [x..xMax] is free.
                    int zMax = z;
                    while (zMax + 1 < h && RowFree(x, xMax, zMax + 1))
                        zMax++;

                    int id = polys.Count;
                    var p = new Poly { xMin = x, xMax = xMax, zMin = z, zMax = zMax };
                    p.Center = CellCornerToWorld(
                        (x + xMax + 1) * 0.5f, (z + zMax + 1) * 0.5f);
                    polys.Add(p);

                    for (int cx = x; cx <= xMax; cx++)
                        for (int cz = z; cz <= zMax; cz++)
                            cellToPoly[cx, cz] = id;
                }
            }

            // ── 2. Link neighbours + build portals ────────────────────────────
            // Two polys are linked when their cell rects touch along an edge; the
            // portal is the overlapping span of that shared edge, in world space.
            for (int i = 0; i < polys.Count; i++)
            {
                for (int j = i + 1; j < polys.Count; j++)
                {
                    var A = polys[i];
                    var B = polys[j];

                    // Vertical shared edge (A left of B, or B left of A).
                    if (A.xMax + 1 == B.xMin || B.xMax + 1 == A.xMin)
                    {
                        int zLo = Mathf.Max(A.zMin, B.zMin);
                        int zHi = Mathf.Min(A.zMax, B.zMax);
                        if (zHi < zLo) continue;                     // no overlap
                        float edgeX = (A.xMax + 1 == B.xMin) ? A.xMax + 1 : B.xMax + 1;
                        var a = CellCornerToWorld(edgeX, zLo);
                        var b = CellCornerToWorld(edgeX, zHi + 1);
                        Link(i, j, a, b);
                    }
                    // Horizontal shared edge.
                    else if (A.zMax + 1 == B.zMin || B.zMax + 1 == A.zMin)
                    {
                        int xLo = Mathf.Max(A.xMin, B.xMin);
                        int xHi = Mathf.Min(A.xMax, B.xMax);
                        if (xHi < xLo) continue;
                        float edgeZ = (A.zMax + 1 == B.zMin) ? A.zMax + 1 : B.zMax + 1;
                        var a = CellCornerToWorld(xLo, edgeZ);
                        var b = CellCornerToWorld(xHi + 1, edgeZ);
                        Link(i, j, a, b);
                    }
                }
            }

            Debug.Log($"[NavMesh] Baked {polys.Count} walkable polygons from {w}x{h} cells.");
        }

        private void Link(int i, int j, Vector3 a, Vector3 b)
        {
            // Pull the portal in slightly at both ends so agents (which have a body
            // radius) don't clip the corner they're rounding.
            Vector3 dir = (b - a);
            float len = dir.magnitude;
            if (len > 0.0001f)
            {
                float inset = Mathf.Min(AgentRadius, len * 0.35f);
                dir /= len;
                a += dir * inset;
                b -= dir * inset;
            }
            polys[i].Links.Add(j); polys[i].Portals.Add((a, b));
            polys[j].Links.Add(i); polys[j].Portals.Add((a, b));
        }

        /// <summary>Body radius used to inset portals so agents round corners cleanly.</summary>
        public const float AgentRadius = 0.35f;

        private bool Walkable(int x, int z) => grid.IsWalkableCell(x, z);

        private bool RowFree(int xFrom, int xTo, int z)
        {
            for (int x = xFrom; x <= xTo; x++)
                if (cellToPoly[x, z] != -1 || !Walkable(x, z)) return false;
            return true;
        }

        /// <summary>Cell-corner (float, so .5 gives a cell centre) → world.</summary>
        private Vector3 CellCornerToWorld(float cx, float cz) =>
            new Vector3(grid.GridOrigin.x + cx * grid.CellSize,
                        0f,
                        grid.GridOrigin.z + cz * grid.CellSize);

        private int PolyAt(Vector3 world)
        {
            if (cellToPoly == null) return -1;
            var c = grid.WorldToCell(world);
            if (c.x < 0 || c.y < 0 || c.x >= grid.GridWidth || c.y >= grid.GridHeight) return -1;
            int id = cellToPoly[c.x, c.y];
            if (id != -1) return id;

            // Standing on/inside geometry (a rack, a counter): snap to the nearest
            // walkable poly so we can still path away from it.
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < polys.Count; i++)
            {
                float d = (polys[i].Center - world).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Smooth path from start to goal: A* across polygons, then funnelled into
        /// real corner points. Returns null when there's no route.
        /// </summary>
        public List<Vector3> FindPath(Vector3 start, Vector3 goal)
        {
            if (polys.Count == 0) return null;

            start.y = 0; goal.y = 0;
            int s = PolyAt(start), g = PolyAt(goal);
            if (s < 0 || g < 0) return null;

            if (s == g) return new List<Vector3> { goal };   // straight shot

            var corridor = AStar(s, g);
            if (corridor == null) return null;

            var portals = BuildPortalList(corridor, start, goal);
            var path = Funnel(portals);

            // The funnel's first point is the start itself — drop it.
            if (path.Count > 0 && (path[0] - start).sqrMagnitude < 0.0004f)
                path.RemoveAt(0);
            if (path.Count == 0) path.Add(goal);
            return path;
        }

        // ── A* over the polygon graph ────────────────────────────────────────
        private List<int> AStar(int start, int goal)
        {
            int n = polys.Count;
            var came = new int[n];
            var gScore = new float[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++) { came[i] = -1; gScore[i] = float.MaxValue; }

            gScore[start] = 0f;
            var open = new List<int> { start };

            while (open.Count > 0)
            {
                // Small graph (dozens of nodes) — a linear scan is faster than a heap.
                int best = 0;
                float bestF = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    int id = open[i];
                    float f = gScore[id] + Vector3.Distance(polys[id].Center, polys[goal].Center);
                    if (f < bestF) { bestF = f; best = i; }
                }

                int cur = open[best];
                if (cur == goal)
                {
                    var route = new List<int>();
                    for (int at = goal; at != -1; at = came[at]) route.Add(at);
                    route.Reverse();
                    return route;
                }

                open.RemoveAt(best);
                closed[cur] = true;

                var p = polys[cur];
                for (int k = 0; k < p.Links.Count; k++)
                {
                    int nb = p.Links[k];
                    if (closed[nb]) continue;

                    // Cost through the portal we'd actually cross, not centre-to-centre.
                    var (pa, pb) = p.Portals[k];
                    Vector3 mid = (pa + pb) * 0.5f;
                    float tentative = gScore[cur]
                                    + Vector3.Distance(p.Center, mid)
                                    + Vector3.Distance(mid, polys[nb].Center);

                    if (tentative < gScore[nb])
                    {
                        came[nb] = cur;
                        gScore[nb] = tentative;
                        if (!open.Contains(nb)) open.Add(nb);
                    }
                }
            }
            return null;
        }

        // ── Portal corridor, oriented left/right along travel direction ──────
        private List<(Vector3 left, Vector3 right)> BuildPortalList(
            List<int> corridor, Vector3 start, Vector3 goal)
        {
            var list = new List<(Vector3, Vector3)> { (start, start) };

            for (int i = 0; i < corridor.Count - 1; i++)
            {
                var from = polys[corridor[i]];
                int toId = corridor[i + 1];
                int k = from.Links.IndexOf(toId);
                if (k < 0) continue;

                var (a, b) = from.Portals[k];
                Vector3 dir = polys[toId].Center - from.Center;

                // Whichever endpoint is counter-clockwise of the travel direction is
                // the "left" one. Getting this wrong turns the funnel inside out.
                float ca = Cross(dir, a - from.Center);
                float cb = Cross(dir, b - from.Center);
                list.Add(ca > cb ? (a, b) : (b, a));
            }

            list.Add((goal, goal));
            return list;
        }

        /// <summary>2D cross product on the XZ plane. &gt;0 means b is CCW (left) of a.</summary>
        private static float Cross(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;

        /// <summary>Cross of (p1-p0) x (p2-p0) — sign tells which side p2 falls on.</summary>
        private static float Cross(Vector3 p0, Vector3 p1, Vector3 p2) =>
            (p1.x - p0.x) * (p2.z - p0.z) - (p1.z - p0.z) * (p2.x - p0.x);

        // ── Simple Stupid Funnel Algorithm (Mikko Mononen) ───────────────────
        // Walks the corridor keeping a left/right "funnel" from the current corner.
        // Whenever the funnel would turn inside out, the offending side becomes a
        // real corner of the path. Output is the shortest route through the corridor.
        private static List<Vector3> Funnel(List<(Vector3 left, Vector3 right)> portals)
        {
            var pts = new List<Vector3>();
            if (portals.Count == 0) return pts;

            Vector3 apex = portals[0].left;
            Vector3 left = apex, right = apex;
            int apexIdx = 0, leftIdx = 0, rightIdx = 0;

            pts.Add(apex);

            for (int i = 1; i < portals.Count; i++)
            {
                Vector3 l = portals[i].left;
                Vector3 r = portals[i].right;

                // Tighten the RIGHT side.
                if (Cross(apex, right, r) >= 0f)
                {
                    if (apex == right || Cross(apex, left, r) < 0f)
                    {
                        right = r; rightIdx = i;
                    }
                    else
                    {
                        // Right crossed over left → left is a genuine corner.
                        pts.Add(left);
                        apex = left; apexIdx = leftIdx;
                        left = apex; right = apex;
                        leftIdx = apexIdx; rightIdx = apexIdx;
                        i = apexIdx;
                        continue;
                    }
                }

                // Tighten the LEFT side.
                if (Cross(apex, left, l) <= 0f)
                {
                    if (apex == left || Cross(apex, right, l) > 0f)
                    {
                        left = l; leftIdx = i;
                    }
                    else
                    {
                        pts.Add(right);
                        apex = right; apexIdx = rightIdx;
                        left = apex; right = apex;
                        leftIdx = apexIdx; rightIdx = apexIdx;
                        i = apexIdx;
                        continue;
                    }
                }
            }

            Vector3 end = portals[portals.Count - 1].left;
            if (pts.Count == 0 || (pts[pts.Count - 1] - end).sqrMagnitude > 0.0001f)
                pts.Add(end);

            return pts;
        }

#if UNITY_EDITOR
        /// <summary>Draws the baked polygons + portals in the Scene view.</summary>
        private void OnDrawGizmosSelected()
        {
            if (polys == null) return;
            foreach (var p in polys)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.25f);
                Vector3 lo = CellCornerToWorld(p.xMin, p.zMin);
                Vector3 hi = CellCornerToWorld(p.xMax + 1, p.zMax + 1);
                Vector3 size = hi - lo; size.y = 0.05f;
                Gizmos.DrawCube(lo + size * 0.5f, size);

                Gizmos.color = Color.yellow;
                foreach (var (a, b) in p.Portals) Gizmos.DrawLine(a, b);
            }
        }
#endif
    }
}
