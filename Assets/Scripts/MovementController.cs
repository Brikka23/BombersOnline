using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviourPun, IPunObservable
{
    [Header("Mode")]
    public bool isOfflineMode = false;

    [Header("Movement")]
    public float speed = 5f;

    [Header("Input Keys")]
    public KeyCode inputUp = KeyCode.W;
    public KeyCode inputDown = KeyCode.S;
    public KeyCode inputLeft = KeyCode.A;
    public KeyCode inputRight = KeyCode.D;

    [Header("Sprites")]
    public AnimatedSpriteRenderer spriteRendererUp;
    public AnimatedSpriteRenderer spriteRendererDown;
    public AnimatedSpriteRenderer spriteRendererLeft;
    public AnimatedSpriteRenderer spriteRendererRight;
    public AnimatedSpriteRenderer spriteRendererDeath;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.down;
    private AnimatedSpriteRenderer activeSprite;
    private PhotonView pv;
    private PhotonView gmPV;
    private Vector2 netPos, netDir;
    private bool deathNotified = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pv = photonView;
        activeSprite = spriteRendererDown;
        if (!isOfflineMode && !pv.IsMine)
            rb.isKinematic = true;
    }

    void Start()
    {
        if (!isOfflineMode)
        {
            if (GameManager.Instance != null)
                gmPV = GameManager.Instance.GetComponent<PhotonView>();
            else
                Debug.LogError("GameManager.Instance отсутствует");

            if (pv.IsMine)
                PhotonNetwork.LocalPlayer.SetCustomProperties(
                    new Hashtable { { "IsAlive", true } }
                );
        }
    }

    void Update()
    {
        bool local = isOfflineMode || pv.IsMine;
        if (local) HandleInput();
        else SyncAnimation();
    }

    void FixedUpdate()
    {
        bool local = isOfflineMode || pv.IsMine;
        if (local)
            rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
        else
        {
            rb.position = Vector2.MoveTowards(rb.position, netPos, speed * Time.fixedDeltaTime);
            direction = netDir;
        }
    }

    void HandleInput()
    {
        if (Input.GetKey(inputUp)) SetDirection(Vector2.up, spriteRendererUp);
        else if (Input.GetKey(inputDown)) SetDirection(Vector2.down, spriteRendererDown);
        else if (Input.GetKey(inputLeft)) SetDirection(Vector2.left, spriteRendererLeft);
        else if (Input.GetKey(inputRight)) SetDirection(Vector2.right, spriteRendererRight);
        else SetDirection(Vector2.zero, activeSprite);
    }

    public void SetDirection(Vector2 dir, AnimatedSpriteRenderer spr)
    {
        direction = dir;
        spriteRendererUp.enabled = spr == spriteRendererUp;
        spriteRendererDown.enabled = spr == spriteRendererDown;
        spriteRendererLeft.enabled = spr == spriteRendererLeft;
        spriteRendererRight.enabled = spr == spriteRendererRight;
        activeSprite = spr;
        activeSprite.SetIdle(dir == Vector2.zero);
    }

    void SyncAnimation()
    {
        spriteRendererUp.enabled = activeSprite == spriteRendererUp;
        spriteRendererDown.enabled = activeSprite == spriteRendererDown;
        spriteRendererLeft.enabled = activeSprite == spriteRendererLeft;
        spriteRendererRight.enabled = activeSprite == spriteRendererRight;
        activeSprite.SetIdle(direction == Vector2.zero);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
            DeathSequence();
    }

    public void DeathSequence()
    {
        bool local = isOfflineMode || pv.IsMine;
        if (!local) return;

        if (isOfflineMode)
            RPC_Death();
        else
            pv.RPC(nameof(RPC_Death), RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_Death()
    {
        enabled = false;
        GetComponent<BombController>().enabled = false;
        spriteRendererUp.enabled = false;
        spriteRendererDown.enabled = false;
        spriteRendererLeft.enabled = false;
        spriteRendererRight.enabled = false;
        spriteRendererDeath.enabled = true;

        Invoke(nameof(OnDeathComplete), 1.25f);
    }

    void OnDeathComplete()
    {
        if (deathNotified) return;
        deathNotified = true;

        if (!isOfflineMode && pv.IsMine && gmPV != null)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(
                new Hashtable { { "IsAlive", false } }
            );
            gmPV.RPC(
                nameof(GameManager.NotifyPlayerDeath),
                RpcTarget.MasterClient,
                PhotonNetwork.LocalPlayer.ActorNumber
            );
        }

        gameObject.SetActive(false);
    }

    public void IncreaseSpeed(float amount) => speed += amount;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (isOfflineMode) return;

        if (stream.IsWriting)
        {
            stream.SendNext(rb.position);
            stream.SendNext(direction);
            int idx = activeSprite == spriteRendererUp ? 0
                    : activeSprite == spriteRendererDown ? 1
                    : activeSprite == spriteRendererLeft ? 2 : 3;
            stream.SendNext(idx);
        }
        else
        {
            netPos = (Vector2)stream.ReceiveNext();
            netDir = (Vector2)stream.ReceiveNext();
            int si = (int)stream.ReceiveNext();
            activeSprite = si == 0 ? spriteRendererUp
                          : si == 1 ? spriteRendererDown
                          : si == 2 ? spriteRendererLeft
                                     : spriteRendererRight;
        }
    }
}
