using Photon.Pun;
using UnityEngine;

public class DestructibleEffect : MonoBehaviourPun
{
    public float duration = 1f;

    private void Start()
    {
        Invoke(nameof(DestroySelf), duration);
    }

    private void DestroySelf()
    {
        if (photonView != null)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
