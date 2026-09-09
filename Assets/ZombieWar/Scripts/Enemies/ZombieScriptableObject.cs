using UnityEngine;

[CreateAssetMenu(fileName = "ZombieData", menuName = "Zombie War/Zombie")]
public class ZombieScriptableObject : ScriptableObject
{
    [SerializeField, Min(1f)] private float m_maxHealth = 100f;
    [SerializeField, Min(0.01f)] private float m_moveSpeed = 2f;
    [SerializeField, Min(0f)] private float m_damage = 10f;
    [SerializeField, Min(0.01f)] private float m_attackInterval = 1f;
    [SerializeField, Min(0.01f)] private float m_attackRange = 1.5f;

    public float MaxHealth => m_maxHealth;
    public float MoveSpeed => m_moveSpeed;
    public float Damage => m_damage;
    public float AttackInterval => m_attackInterval;
    public float AttackRange => m_attackRange;
}
