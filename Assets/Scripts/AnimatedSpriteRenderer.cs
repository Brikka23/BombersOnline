using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSpriteRenderer : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public Sprite idleSprite;
    public Sprite[] animationSprites;

    public float animationTime = 0.25f;
    private int animationFrame;

    public bool loop = true;
    public bool idle = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        spriteRenderer.enabled = true;
        InvokeRepeating(nameof(NextFrame), animationTime, animationTime);
    }

    private void OnDisable()
    {
        spriteRenderer.enabled = false;
        CancelInvoke(nameof(NextFrame));
    }

    private void NextFrame()
    {
        if (idle)
        {
            spriteRenderer.sprite = idleSprite;
            animationFrame = 0;
            return;
        }

        if (animationSprites.Length == 0)
        {
            Debug.LogWarning("No animation sprites assigned.");
            return;
        }

        animationFrame++;

        if (loop && animationFrame >= animationSprites.Length)
        {
            animationFrame = 0;
        }
        else if (!loop && animationFrame >= animationSprites.Length)
        {
            animationFrame = animationSprites.Length - 1;
            CancelInvoke(nameof(NextFrame));
        }

        if (animationFrame >= 0 && animationFrame < animationSprites.Length)
        {
            spriteRenderer.sprite = animationSprites[animationFrame];
        }
    }

    public void SetIdle(bool isIdle)
    {
        idle = isIdle;
        if (idle)
        {
            spriteRenderer.sprite = idleSprite;
            animationFrame = 0;
        }
    }
}
