using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public static class AStar
    {
        struct Node
        {
            public Vector2Int pos;
            public int g;
            public int h;
            public Vector2Int parent;
            public bool visited;
        }

        static int Heuristic(Vector2Int a, Vector2Int b, bool diag)
        {
            if (diag)
            {
                int dx = Mathf.Abs(a.x - b.x);
                int dy = Mathf.Abs(a.y - b.y);
                int min = Mathf.Min(dx, dy);
                int max = Mathf.Max(dx, dy);
                return 14 * min + 10 * (max - min);
            }
            else
            {
                return 10 * (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y));
            }
        }

        public static List<Vector2Int> FindPath(bool[,] walkable, Vector2Int start, Vector2Int goal, bool allowDiagonals = false)
        {
            int w = walkable.GetLength(0);
            int h = walkable.GetLength(1);
            if (start.x < 0 || start.x >= w || start.y < 0 || start.y >= h) return null;
            if (goal.x < 0 || goal.x >= w || goal.y < 0 || goal.y >= h) return null;
            if (!walkable[start.x, start.y] || !walkable[goal.x, goal.y]) return null;

            Node[,] nodes = new Node[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    nodes[x, y].pos = new Vector2Int(x, y);
                    nodes[x, y].g = int.MaxValue;
                    nodes[x, y].h = Heuristic(nodes[x, y].pos, goal, allowDiagonals);
                    nodes[x, y].visited = false;
                }

            List<Vector2Int> open = new List<Vector2Int>();
            nodes[start.x, start.y].g = 0;
            open.Add(start);

            int[,] dirs = allowDiagonals ? new int[,] { {1,0},{-1,0},{0,1},{0,-1},{1,1},{1,-1},{-1,1},{-1,-1} } : new int[,] { {1,0},{-1,0},{0,1},{0,-1} };

            while (open.Count > 0)
            {
                // find node with lowest f = g + h
                int bestIndex = 0;
                int bestF = nodes[open[0].x, open[0].y].g + nodes[open[0].x, open[0].y].h;
                for (int i = 1; i < open.Count; i++)
                {
                    var p = open[i];
                    int f = nodes[p.x, p.y].g + nodes[p.x, p.y].h;
                    if (f < bestF)
                    {
                        bestF = f;
                        bestIndex = i;
                    }
                }

                Vector2Int current = open[bestIndex];
                if (current == goal)
                {
                    // reconstruct path
                    List<Vector2Int> path = new List<Vector2Int>();
                    Vector2Int cur = goal;
                    while (cur != start)
                    {
                        path.Add(cur);
                        cur = nodes[cur.x, cur.y].parent;
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                open.RemoveAt(bestIndex);
                nodes[current.x, current.y].visited = true;

                int dirCount = dirs.GetLength(0);
                for (int i = 0; i < dirCount; i++)
                {
                    int nx = current.x + dirs[i, 0];
                    int ny = current.y + dirs[i, 1];
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (!walkable[nx, ny]) continue;
                    if (nodes[nx, ny].visited) continue;

                    int moveCost = (Mathf.Abs(dirs[i,0]) + Mathf.Abs(dirs[i,1]) == 2) ? 14 : 10;
                    int tentativeG = nodes[current.x, current.y].g + moveCost;
                    if (tentativeG < nodes[nx, ny].g)
                    {
                        nodes[nx, ny].g = tentativeG;
                        nodes[nx, ny].parent = current;
                        // add to open if not already
                        bool inOpen = false;
                        for (int k = 0; k < open.Count; k++) if (open[k].x == nx && open[k].y == ny) { inOpen = true; break; }
                        if (!inOpen) open.Add(new Vector2Int(nx, ny));
                    }
                }
            }

            // no path
            return null;
        }
    }
}
