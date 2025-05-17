using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombController : MonoBehaviourPun
{
    public struct BombInfo { public Vector3Int cell; public float explodeTime; }
    public static List<BombInfo> PendingBombs = new List<BombInfo>();

    [Header("Mode")]
    public bool isOfflineMode = false;

    [Header("Bomb")]
    public KeyCode inputKey = KeyCode.LeftShift;
    public GameObject bombPrefab;
    public float bombFuseTime = 3f;
    public int bombAmount = 1;
    public int bombsRemaining;

    [Header("Explosion")]
    public GameObject explosionPrefab;
    public float explosionDuration = 1f;
    public int explosionRadius = 1;

    void OnEnable() => bombsRemaining = bombAmount;

    void Update()
    {
        bool isLocal = isOfflineMode || photonView.IsMine;
        if (!isLocal || bombsRemaining <= 0) return;
        if (Input.GetKeyDown(inputKey))
            StartCoroutine(PlaceBomb());
    }

    public IEnumerator PlaceBomb()
    {
        var map = MapManager.Instance;
        Vector3Int cell = map.indestructibleTilemap.WorldToCell(transform.position);

        if (map.HasBomb(cell))
            yield break;

        Vector3 world = map.indestructibleTilemap.GetCellCenterWorld(cell);
        float explodeTime = Time.time + bombFuseTime;

        if (isOfflineMode)
            PendingBombs.Add(new BombInfo { cell = cell, explodeTime = explodeTime });

        GameObject bomb = isOfflineMode
            ? Instantiate(bombPrefab, world, Quaternion.identity)
            : PhotonNetwork.Instantiate(bombPrefab.name, world, Quaternion.identity);

        var bb = bomb.GetComponent<BombBehaviour>();
        if (bb != null)
        {
            bb.controller = this;
            bb.cell = cell;
            MapManager.Instance.RegisterBomb(cell, bb);
        }

        bombsRemaining--;
        yield return new WaitForSeconds(bombFuseTime);

        if (bomb == null) yield break;

        MapManager.Instance.RemoveBombAt(cell);

        if (isOfflineMode)
        {
            ExplodeLocal(bomb, cell);
            bombsRemaining++;
        }
        else
        {
            photonView.RPC(
                nameof(ExplodeNetwork),
                RpcTarget.MasterClient,
                cell.x, cell.y, cell.z,
                bomb.GetComponent<PhotonView>().ViewID
            );
            PhotonNetwork.Destroy(bomb);
            bombsRemaining++;
        }
    }

    [PunRPC]
    void ExplodeNetwork(int x, int y, int z, int bombViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Vector3Int origin = new Vector3Int(x, y, z);

        MapManager.Instance.RemoveBombAt(origin);

        var cells = new List<Vector3Int> { origin };
        foreach (var dir in new[]{ Vector3Int.up, Vector3Int.down,
                                   Vector3Int.left,Vector3Int.right})
            CollectExplosionCells(cells, origin, dir);

        var xs = cells.Select(c => c.x).ToArray();
        var ys = cells.Select(c => c.y).ToArray();
        var zs = cells.Select(c => c.z).ToArray();

        photonView.RPC(nameof(CreateExplosionsNetwork),
                        RpcTarget.All, xs, ys, zs);

        foreach (var c in cells)
            if (MapManager.Instance.destructibleTilemap.HasTile(c))
                MapManager.Instance.photonView.RPC(
                    "DestroyTileAtPosition",
                    RpcTarget.All,
                    c.x, c.y, c.z
                );
    }

    [PunRPC]
    void CreateExplosionsNetwork(int[] xs, int[] ys, int[] zs)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            var cell = new Vector3Int(xs[i], ys[i], zs[i]);
            var pos = MapManager.Instance.destructibleTilemap.GetCellCenterWorld(cell);
            var obj = PhotonNetwork.Instantiate(explosionPrefab.name, pos, Quaternion.identity);
            var fx = obj.GetComponent<Explosion>();
            fx?.SetActiveRenderer(fx.middle);
            fx?.DestroyAfter(explosionDuration);
        }
    }

    void ExplodeLocal(GameObject bombObj, Vector3Int bombCell)
    {
        Destroy(bombObj);

        PendingBombs.RemoveAll(info => info.cell == bombCell);

        var cells = new List<Vector3Int> { bombCell };
        foreach (var dir in new[]{ Vector3Int.up, Vector3Int.down,
                                   Vector3Int.left,Vector3Int.right})
            CollectExplosionCells(cells, bombCell, dir);

        foreach (var c in cells)
        {
            var pos = MapManager.Instance.destructibleTilemap.GetCellCenterWorld(c);
            var obj = Instantiate(explosionPrefab, pos, Quaternion.identity);
            var fx = obj.GetComponent<Explosion>();
            fx?.SetActiveRenderer(fx.middle);
            fx?.DestroyAfter(explosionDuration);

            if (MapManager.Instance.destructibleTilemap.HasTile(c))
                MapManager.Instance.DestroyTileAtPosition(c.x, c.y, c.z);
        }
    }

    void CollectExplosionCells(List<Vector3Int> cells,
        Vector3Int origin, Vector3Int dir)
    {
        for (int i = 1; i <= explosionRadius; i++)
        {
            var c = origin + dir * i;
            if (MapManager.Instance.indestructibleTilemap.HasTile(c)) break;
            cells.Add(c);
            if (MapManager.Instance.destructibleTilemap.HasTile(c)) break;
        }
    }

    public void AddBomb() { bombAmount++; bombsRemaining++; }
    public void IncreaseExplosionRadius(int amt) { explosionRadius += amt; }
}
