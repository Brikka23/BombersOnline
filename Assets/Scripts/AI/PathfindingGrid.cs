using System.Collections.Generic;
using UnityEngine;


public class PathfindingGrid : MonoBehaviour
{
    public static PathfindingGrid Instance;

    private void Awake() => Instance = this;

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target)
        => FindPathInternal(start, target, allowDestructible: false);

    public List<Vector3Int> FindPathAllowBreak(Vector3Int start, Vector3Int target)
        => FindPathInternal(start, target, allowDestructible: true);

    private List<Vector3Int> FindPathInternal(Vector3Int start, Vector3Int target, bool allowDestructible)
    {
        var open = new List<Node>();
        var closed = new HashSet<Vector3Int>();
        open.Add(new Node(start, null, 0, Heuristic(start, target)));

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.fCost != b.fCost ? a.fCost - b.fCost : a.hCost - b.hCost);
            var cur = open[0];
            open.RemoveAt(0);
            closed.Add(cur.pos);

            if (cur.pos == target)
                return Retrace(cur);

            foreach (var nb in GetNeighbors(cur.pos, allowDestructible))
            {
                if (closed.Contains(nb)) continue;
                int gNew = cur.gCost + 1;
                var exist = open.Find(n => n.pos == nb);
                if (exist == null)
                {
                    open.Add(new Node(nb, cur, gNew, Heuristic(nb, target)));
                }
                else if (gNew < exist.gCost)
                {
                    exist.gCost = gNew;
                    exist.parent = cur;
                }
            }
        }
        return null;
    }

    private List<Vector3Int> Retrace(Node end)
    {
        var path = new List<Vector3Int>();
        var cur = end;
        while (cur != null)
        {
            path.Add(cur.pos);
            cur = cur.parent;
        }
        path.Reverse();
        return path;
    }

    private int Heuristic(Vector3Int a, Vector3Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private IEnumerable<Vector3Int> GetNeighbors(Vector3Int c, bool allowDestructible)
    {
        var dirs = new[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        foreach (var d in dirs)
        {
            var nb = c + d;
            if (MapManager.Instance.IsIndestructible(nb)) continue;
            if (!allowDestructible && MapManager.Instance.IsDestructible(nb)) continue;
            yield return nb;
        }
    }

    private class Node
    {
        public Vector3Int pos;
        public Node parent;
        public int gCost, hCost;
        public int fCost => gCost + hCost;
        public Node(Vector3Int p, Node par, int g, int h) { pos = p; parent = par; gCost = g; hCost = h; }
    }
}
