using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class BombBehaviour : MonoBehaviour
{
    [HideInInspector] public Vector3Int cell;
    [HideInInspector] public BombController controller;

    private CircleCollider2D col;
    private List<GameObject> initialEntities = new List<GameObject>();
    private bool barrierClosed;

    void Awake()
    {
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        barrierClosed = false;
    }

    void Start()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, col.radius);
        foreach (var hit in hits)
            if (hit.GetComponent<MovementController>() != null)
                initialEntities.Add(hit.gameObject);

        if (initialEntities.Count == 0)
        {
            col.isTrigger = false;
            barrierClosed = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (barrierClosed) return;
        if (other.GetComponent<MovementController>() != null)
        {
            initialEntities.Remove(other.gameObject);
            if (initialEntities.Count == 0)
            {
                col.isTrigger = false;
                barrierClosed = true;
            }
        }
    }
}
