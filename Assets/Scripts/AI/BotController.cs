using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovementController), typeof(BombController))]
public class BotController : MonoBehaviour
{
    public BotSettings[] difficultySettings;
    [HideInInspector] public int difficultyIndex = 0;

    BotSettings settings;
    MovementController move;
    BombController bomb;
    Transform player;

    enum State { Patrol, SeekItem, Chase, BreakWall, Evade }
    State state = State.Patrol;

    List<Vector3Int> path;
    int pathIndex;
    float nextDecisionTime;
    float evacuateUntilTime;

    Vector3Int breakCell, breakApproach;
    GameObject targetItem;

    void Start()
    {
        settings = difficultySettings[Mathf.Clamp(difficultyIndex, 0, difficultySettings.Length - 1)];
        move = GetComponent<MovementController>();
        bomb = GetComponent<BombController>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        move.speed *= settings.speedMultiplier;
        nextDecisionTime = Time.time;
        evacuateUntilTime = 0f;
    }

    void Update()
    {
        if (state == State.Evade && Time.time < evacuateUntilTime)
        {
            if (path != null && pathIndex < path.Count) FollowPath();
            return;
        }
        if (state == State.Evade && Time.time >= evacuateUntilTime)
            state = State.Patrol;

        if (Time.time >= nextDecisionTime)
        {
            Decide();
            nextDecisionTime = Time.time + settings.reactionDelay;
        }
        FollowPath();
    }

    void Decide()
    {
        var myCell = MapManager.Instance.GetCellPosition(transform.position);

        var items = GameObject.FindGameObjectsWithTag("Item");
        float bestDist = float.MaxValue;
        targetItem = null;
        foreach (var it in items)
        {
            var cell = MapManager.Instance.GetCellPosition(it.transform.position);
            float d = Mathf.Abs(cell.x - myCell.x) + Mathf.Abs(cell.y - myCell.y);
            if (d < bestDist && d <= settings.detectionRadius)
            {
                bestDist = d;
                targetItem = it;
            }
        }
        if (targetItem)
        {
            state = State.SeekItem;
            var itCell = MapManager.Instance.GetCellPosition(targetItem.transform.position);
            path = PathfindingGrid.Instance.FindPath(myCell, itCell);
            pathIndex = 0;
            return;
        }

        if (IsInDanger(myCell))
        {
            StartBombEvade(myCell, bomb.explosionRadius);
            return;
        }

        var playerCell = MapManager.Instance.GetCellPosition(player.position);
        var chase = PathfindingGrid.Instance.FindPath(myCell, playerCell);
        if (chase != null)
        {
            int dist = chase.Count - 1;
            if (bomb.bombsRemaining > 0 && dist <= bomb.explosionRadius)
            {
                StartBombEvade(myCell, bomb.explosionRadius);
                return;
            }
            state = State.Chase;
            path = chase; pathIndex = 0;
            return;
        }

        var breakPath = PathfindingGrid.Instance.FindPathAllowBreak(myCell, playerCell);
        if (breakPath != null)
        {
            for (int i = 1; i < breakPath.Count; i++)
            {
                var c = breakPath[i];
                if (MapManager.Instance.IsDestructible(c))
                {
                    breakCell = c;
                    breakApproach = breakPath[i - 1];
                    path = breakPath.GetRange(0, i);
                    pathIndex = 0;
                    state = State.BreakWall;
                    return;
                }
            }
        }

        state = State.Patrol;
        path = new List<Vector3Int> { GetRandomCell() };
        pathIndex = 0;
    }

    void FollowPath()
    {
        if (path == null || pathIndex >= path.Count)
        {
            move.SetDirection(Vector2.zero, move.spriteRendererDown);
            return;
        }
        var cell = path[pathIndex];
        var world = MapManager.Instance.GetCellCenterWorld(cell);
        var dir = ((Vector2)world - (Vector2)transform.position);
        if (dir.magnitude < 0.1f)
        {
            if (state == State.BreakWall && cell == breakApproach)
            {
                StartBombEvade(cell, bomb.explosionRadius);
                return;
            }
            pathIndex++; return;
        }
        dir.Normalize();
        AnimatedSpriteRenderer spr =
            Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? dir.x > 0 ? move.spriteRendererRight : move.spriteRendererLeft
            : dir.y > 0 ? move.spriteRendererUp : move.spriteRendererDown;
        move.SetDirection(dir, spr);
    }

    void StartBombEvade(Vector3Int bombCell, int radius)
    {
        if (bomb.bombsRemaining <= 0) return;
        evacuateUntilTime = Time.time + bomb.bombFuseTime + bomb.explosionDuration;
        EvadeTo(bombCell, radius);
        StartCoroutine(bomb.PlaceBomb());
    }

    void EvadeTo(Vector3Int center, int radius)
    {
        state = State.Evade;
        Vector3Int target = center;
        var dirs = new[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        var b = MapManager.Instance.destructibleTilemap.cellBounds;
        foreach (var d in dirs)
        {
            var cand = center + d * (radius + 1);
            if (cand.x < b.xMin || cand.x >= b.xMax || cand.y < b.yMin || cand.y >= b.yMax) continue;
            if (MapManager.Instance.IsIndestructible(cand) || MapManager.Instance.IsDestructible(cand)) continue;
            if (PathfindingGrid.Instance.FindPath(center, cand) != null) { target = cand; break; }
        }
        if (target == center)
            foreach (var d in dirs)
            {
                var nb = center + d;
                if (!MapManager.Instance.IsIndestructible(nb) && !MapManager.Instance.IsDestructible(nb))
                { target = nb; break; }
            }
        var myCell = MapManager.Instance.GetCellPosition(transform.position);
        path = PathfindingGrid.Instance.FindPath(myCell, target);
        pathIndex = 0;
    }

    bool IsInDanger(Vector3Int at)
    {
        foreach (var info in BombController.PendingBombs)
        {
            if (info.explodeTime <= Time.time) continue;
            var b = info.cell; int R = bomb.explosionRadius;
            int dx = at.x - b.x, dy = at.y - b.y;
            if (dx != 0 && dy != 0) continue;
            if (Mathf.Abs(dx + dy) > R) continue;
            return true;
        }
        return false;
    }

    Vector3Int GetRandomCell()
    {
        var b = MapManager.Instance.destructibleTilemap.cellBounds;
        Vector3Int c;
        do
        {
            c = new Vector3Int(Random.Range(b.xMin, b.xMax), Random.Range(b.yMin, b.yMax), 0);
        } while (MapManager.Instance.IsIndestructible(c) || MapManager.Instance.IsDestructible(c));
        return c;
    }
}
