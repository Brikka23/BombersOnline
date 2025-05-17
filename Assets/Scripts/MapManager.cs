using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

public class MapManager : MonoBehaviourPun
{
    public static MapManager Instance;

    [Header("Tilemaps")]
    public Tilemap floorTilemap;
    public Tilemap destructibleTilemap;
    public Tilemap indestructibleTilemap;

    [Header("Destructible Prefab")]
    public GameObject destructibleEffectPrefab;

    [Header("Items")]
    [Range(0f, 1f)] public float itemSpawnChance = 0.2f;
    public GameObject[] spawnableItems;

    private Dictionary<Vector3Int, TileBase> initialDestructible;

    private Dictionary<Vector3Int, BombBehaviour> bombsOnMap = new Dictionary<Vector3Int, BombBehaviour>();


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CacheInitialMap();
        }
        else Destroy(gameObject);
    }

    void CacheInitialMap()
    {
        initialDestructible = new Dictionary<Vector3Int, TileBase>();
        foreach (var pos in destructibleTilemap.cellBounds.allPositionsWithin)
            if (destructibleTilemap.HasTile(pos))
                initialDestructible[pos] = destructibleTilemap.GetTile(pos);
    }

    public Vector3Int GetCellPosition(Vector3 worldPos)
        => floorTilemap.WorldToCell(worldPos);

    public Vector3 GetCellCenterWorld(Vector3Int cell)
        => floorTilemap.GetCellCenterWorld(cell);

    public bool IsDestructible(Vector3Int cell)
        => destructibleTilemap.HasTile(cell);

    public bool IsIndestructible(Vector3Int cell)
        => indestructibleTilemap.HasTile(cell);

    public bool TryPlaceBomb(Vector3Int cell, BombBehaviour bb)
    {
        if (indestructibleTilemap.HasTile(cell) ||
            destructibleTilemap.HasTile(cell) ||
            bombsOnMap.ContainsKey(cell))
            return false;

        bombsOnMap[cell] = bb;
        return true;
    }

    public void RemoveBombAt(Vector3Int cell)
    {
        bombsOnMap.Remove(cell);
    }

    public void ResetMapToInitialState()
    {
        destructibleTilemap.ClearAllTiles();
        foreach (var kv in initialDestructible)
            destructibleTilemap.SetTile(kv.Key, kv.Value);
        bombsOnMap.Clear();
    }

    public void RegisterBomb(Vector3Int cell, BombBehaviour bb)
    {
        bombsOnMap[cell] = bb;
    }

    public bool HasBomb(Vector3Int cell)
    {
        return bombsOnMap.ContainsKey(cell);
    }

    [PunRPC]
    public void DestroyTileAtPosition(int x, int y, int z)
    {
        var cell = new Vector3Int(x, y, z);
        if (!destructibleTilemap.HasTile(cell)) return;

        destructibleTilemap.SetTile(cell, null);
        Vector3 w = destructibleTilemap.GetCellCenterWorld(cell);

        if (PhotonNetwork.InRoom)
            PhotonNetwork.InstantiateRoomObject(destructibleEffectPrefab.name, w, Quaternion.identity);
        else
            Instantiate(destructibleEffectPrefab, w, Quaternion.identity);

        if (spawnableItems.Length > 0 && Random.value < itemSpawnChance)
        {
            var prefab = spawnableItems[Random.Range(0, spawnableItems.Length)];
            if (PhotonNetwork.InRoom)
                PhotonNetwork.InstantiateRoomObject(prefab.name, w, Quaternion.identity);
            else
            {
                var go = Instantiate(prefab, w, Quaternion.identity);
                var ip = go.GetComponent<ItemPickup>();
                if (ip != null) ip.isOfflineMode = true;
            }
        }
    }
}
