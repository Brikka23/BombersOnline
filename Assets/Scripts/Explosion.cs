using Photon.Pun;
using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviourPun
{
    public AnimatedSpriteRenderer start;
    public AnimatedSpriteRenderer middle;
    public AnimatedSpriteRenderer end;

    private void Awake()
    {
        start.enabled = false;
        middle.enabled = false;
        end.enabled = false;
    }

    public void SetActiveRenderer(AnimatedSpriteRenderer renderer)
    {
        start.enabled = renderer == start;
        middle.enabled = renderer == middle;
        end.enabled = renderer == end;
    }

    public void SetDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x);
        transform.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
    }

    public void DestroyAfter(float seconds)
    {
        StartCoroutine(DestroyAfterDelay(seconds));
    }

    private IEnumerator DestroyAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        try
        {
            if (PhotonNetwork.InRoom && photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
            else
                Destroy(gameObject);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Explosion] Destroy failed: {ex.Message}");
            Destroy(gameObject);
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            MovementController movementController = other.GetComponent<MovementController>();
            if (movementController != null)
            {
                movementController.DeathSequence();
            }
        }
    }
}
