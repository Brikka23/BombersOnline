using UnityEngine;

[CreateAssetMenu(menuName = "AI/BotSettings")]
public class BotSettings : ScriptableObject
{
    [Header("�����")]
    [Tooltip("�������� ����� ��������� �� (���)")]
    public float reactionDelay = 1f;

    [Range(0f, 1f), Tooltip("����������� �������� ����� ��� �������������")]
    public float bombChance = 0.3f;

    [Tooltip("������ ����������� ������ (� �������)")]
    public int detectionRadius = 5;

    [Tooltip("��������� ��������")]
    public float speedMultiplier = 1f;

    [Range(0f, 1f), Tooltip("����������� ������������� �� ���������� �����")]
    public float breakWallChance = 0.2f;
}
