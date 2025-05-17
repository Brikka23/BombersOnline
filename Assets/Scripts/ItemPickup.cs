using Photon.Pun;
using UnityEngine;

public class ItemPickup : MonoBehaviourPun
{
    public enum ItemType { ExtraBomb, BlastRadius, SpeedIncrease }

    [Header("Mode")]
    public bool isOfflineMode = false;

    [Header("Type")]
    public ItemType type;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bc = other.GetComponent<BombController>();
        if (bc == null) return;

        if (isOfflineMode)
        {
            ApplyItemEffect((int)type, other.gameObject);
            Destroy(gameObject);
        }
        else if (photonView.IsMine)
        {
            int viewID = other.GetComponent<PhotonView>()?.ViewID ?? 0;
            photonView.RPC(nameof(RPC_Apply), RpcTarget.All, (int)type, viewID);
            photonView.RPC(nameof(RPC_Destroy), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Apply(int typeIndex, int viewID)
    {
        GameObject target = null;
        if (isOfflineMode)
            target = GameObject.FindWithTag("Player");
        else if (viewID != 0)
            target = PhotonView.Find(viewID)?.gameObject;
        if (target == null) return;

        var bc = target.GetComponent<BombController>();
        var mc = target.GetComponent<MovementController>();
        switch ((ItemType)typeIndex)
        {
            case ItemType.ExtraBomb: bc?.AddBomb(); break;
            case ItemType.BlastRadius: bc?.IncreaseExplosionRadius(1); break;
            case ItemType.SpeedIncrease: mc?.IncreaseSpeed(1f); break;
        }
    }

    [PunRPC]
    public void RPC_Destroy()
    {
        try
        {
            if (PhotonNetwork.InRoom && photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
            else
                Destroy(gameObject);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ItemPickup] RPC_Destroy failed: {ex.Message}");
            Destroy(gameObject);
        }
    }

    void ApplyItemEffect(int typeIndex, GameObject target)
    {
        var bc = target.GetComponent<BombController>();
        var mc = target.GetComponent<MovementController>();
        switch ((ItemType)typeIndex)
        {
            case ItemType.ExtraBomb: bc?.AddBomb(); break;
            case ItemType.BlastRadius: bc?.IncreaseExplosionRadius(1); break;
            case ItemType.SpeedIncrease: mc?.IncreaseSpeed(1f); break;
        }
    }
}
