using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class ExampleAStar : MonoBehaviour
{
    public int width = 20;
    public int height = 12;
    public Vector2Int start = new Vector2Int(0, 0);
    public Vector2Int goal = new Vector2Int(19, 11);
    public bool allowDiagonals = true;

    private bool[,] grid;
    private List<Vector2Int> path;

    void Start()
    {
        BuildGrid();
        path = AStar.FindPath(grid, start, goal, allowDiagonals);
        if (path == null) Debug.Log("No path found");
    }

    void BuildGrid()
    {
        grid = new bool[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = true;

        // sample obstacles: create a wall with a gap
        for (int x = 3; x < 17; x++)
        {
            grid[x, 6] = false;
        }
        grid[10, 6] = true; // gap

        // some random blocks
        grid[5, 2] = false;
        grid[6, 2] = false;
        grid[7, 2] = false;
        grid[12, 9] = false;
        grid[13, 9] = false;
    }

    void OnDrawGizmos()
    {
        if (grid == null)
        {
            // draw empty grid preview in editor
            Gizmos.color = Color.gray;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Gizmos.DrawWireCube(new Vector3(x + 0.5f, 0, y + 0.5f), new Vector3(1, 0.01f, 1));

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(start.x + 0.5f, 0, start.y + 0.5f), 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(new Vector3(goal.x + 0.5f, 0, goal.y + 0.5f), 0.2f);
            return;
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (!grid[x, y]) Gizmos.color = Color.black;
                else Gizmos.color = Color.white;
                Gizmos.DrawCube(new Vector3(x + 0.5f, 0, y + 0.5f), new Vector3(1, 0.02f, 1));
            }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(new Vector3(start.x + 0.5f, 0.1f, start.y + 0.5f), 0.25f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(goal.x + 0.5f, 0.1f, goal.y + 0.5f), 0.25f);

        if (path != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = new Vector3(path[i].x + 0.5f, 0.15f, path[i].y + 0.5f);
                Vector3 b = new Vector3(path[i + 1].x + 0.5f, 0.15f, path[i + 1].y + 0.5f);
                Gizmos.DrawLine(a, b);
                Gizmos.DrawSphere(a, 0.08f);
            }
        }
    }
}
