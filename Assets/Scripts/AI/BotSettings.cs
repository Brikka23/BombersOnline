using UnityEngine;

[CreateAssetMenu(menuName = "AI/BotSettings")]
public class BotSettings : ScriptableObject
{
    [Header("Общее")]
    [Tooltip("Задержка между решениями ИИ (сек)")]
    public float reactionDelay = 1f;

    [Range(0f, 1f), Tooltip("Вероятность закладки бомбы при преследовании")]
    public float bombChance = 0.3f;

    [Tooltip("Радиус обнаружения игрока (в клетках)")]
    public int detectionRadius = 5;

    [Tooltip("Множитель скорости")]
    public float speedMultiplier = 1f;

    [Range(0f, 1f), Tooltip("Вероятность переключиться на разрушение стены")]
    public float breakWallChance = 0.2f;
}
