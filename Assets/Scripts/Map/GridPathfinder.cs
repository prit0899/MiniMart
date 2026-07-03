using System.Collections.Generic;
using UnityEngine;

namespace MiniMart.Map
{
    /// <summary>
    /// Lightweight A* on a 2D boolean walkability grid.
    /// Characters request a path from their current cell to a target cell and get back a list
    /// of world-space waypoints to step through. Per Architecture Spec Section 5, simple lane
    /// routes are acceptable for v1; this A* gives us growth room.
    /// </summary>
    public class GridPathfinder : MonoBehaviour
    {
        public int GridWidth = 30;
        public int GridHeight = 20;
        public float CellSize = 1f;
        public Vector3 GridOrigin;

        private bool[,] walkable;

        public static GridPathfinder Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(int width, int height, float cellSize)
        {
            GridWidth = width;
            GridHeight = height;
            CellSize = cellSize;
            walkable = new bool[GridWidth, GridHeight];
            for (int x = 0; x < GridWidth; x++)
                for (int y = 0; y < GridHeight; y++)
                    walkable[x, y] = true;
        }

        public void SetWalkable(int x, int y, bool value)
        {
            if (InBounds(x, y)) walkable[x, y] = value;
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            int cx = Mathf.FloorToInt((world.x - GridOrigin.x) / CellSize);
            int cy = Mathf.FloorToInt((world.z - GridOrigin.z) / CellSize);
            return new Vector2Int(Mathf.Clamp(cx, 0, GridWidth - 1), Mathf.Clamp(cy, 0, GridHeight - 1));
        }

        public Vector3 CellToWorld(int x, int y) =>
            new Vector3(GridOrigin.x + x * CellSize + CellSize * 0.5f,
                        0f,
                        GridOrigin.z + y * CellSize + CellSize * 0.5f);

        /// <summary>Returns a list of world-space waypoints from start to goal, or empty list if no path.</summary>
        public List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld)
        {
            var start = WorldToCell(startWorld);
            var goal  = WorldToCell(goalWorld);
            if (!InBounds(goal.x, goal.y) || !walkable[goal.x, goal.y]) return new List<Vector3>();

            var open   = new List<AStarNode>();
            var closed = new HashSet<Vector2Int>();
            open.Add(new AStarNode(start, null, 0, Heuristic(start, goal)));

            while (open.Count > 0)
            {
                var current = LowestF(open);
                if (current.Pos == goal) return ReconstructPath(current);

                open.Remove(current);
                closed.Add(current.Pos);

                foreach (var n in Neighbors(current.Pos))
                {
                    if (closed.Contains(n) || !walkable[n.x, n.y]) continue;
                    float g = current.G + 1f;
                    var existing = open.Find(nd => nd.Pos == n);
                    if (existing == null)
                        open.Add(new AStarNode(n, current, g, g + Heuristic(n, goal)));
                    else if (g < existing.G)
                    { existing.G = g; existing.F = g + Heuristic(n, goal); existing.Parent = current; }
                }
            }
            return new List<Vector3>(); // no path
        }

        private float Heuristic(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private AStarNode LowestF(List<AStarNode> list)
        {
            AStarNode best = list[0];
            foreach (var n in list) if (n.F < best.F) best = n;
            return best;
        }

        private List<Vector3> ReconstructPath(AStarNode node)
        {
            var path = new List<Vector3>();
            while (node != null)
            {
                path.Add(CellToWorld(node.Pos.x, node.Pos.y));
                node = node.Parent;
            }
            path.Reverse();
            return path;
        }

        private IEnumerable<Vector2Int> Neighbors(Vector2Int p)
        {
            if (InBounds(p.x + 1, p.y)) yield return new Vector2Int(p.x + 1, p.y);
            if (InBounds(p.x - 1, p.y)) yield return new Vector2Int(p.x - 1, p.y);
            if (InBounds(p.x, p.y + 1)) yield return new Vector2Int(p.x, p.y + 1);
            if (InBounds(p.x, p.y - 1)) yield return new Vector2Int(p.x, p.y - 1);
        }

        private bool InBounds(int x, int y) => x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;

        private class AStarNode
        {
            public Vector2Int Pos; public AStarNode Parent; public float G; public float F;
            public AStarNode(Vector2Int p, AStarNode parent, float g, float f)
            { Pos = p; Parent = parent; G = g; F = f; }
        }
    }
}
