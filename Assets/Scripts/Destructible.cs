using Photon.Pun;
using UnityEngine;

public class Destructible : MonoBehaviourPun
{
    public float destructionTime = 1f;
    [Range(0f, 1f)]
    public float itemSpawnChance = 0.2f;
    public GameObject[] spawnableItems;

    private void Start()
    {
        Invoke(nameof(DestroySelf), destructionTime);
    }

    private void DestroySelf()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            bool shouldSpawnItem = Random.value < itemSpawnChance;
            int itemIndex = -1;

            if (shouldSpawnItem && spawnableItems.Length > 0)
            {
                itemIndex = Random.Range(0, spawnableItems.Length);
            }

            photonView.RPC("DestroyDestructible", RpcTarget.AllBuffered, itemIndex);
        }
    }

    [PunRPC]
    private void DestroyDestructible(int itemIndex)
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (itemIndex != -1 && spawnableItems.Length > 0)
        {
            Vector3 spawnPosition = transform.position;
            PhotonNetwork.InstantiateRoomObject(spawnableItems[itemIndex].name, spawnPosition, Quaternion.identity);
        }
    }
}
