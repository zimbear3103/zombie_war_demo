using System;
using UnityEngine;

public class ZombieStats : MonoBehaviour, IDamageable
{
    [SerializeField] private ZombieScriptableObject m_zombieData;
    [SerializeField, Min(1f)] private float m_maxHealth = 100f;
    [SerializeField, Min(0.01f)] private float m_moveSpeed = 2f;
    [SerializeField, Min(0f)] private float m_damage = 10f;
    [SerializeField, Min(0.01f)] private float m_attackInterval = 1f;
    [SerializeField, Min(0.01f)] private float m_attackRange = 1.5f;

    private float m_runtimeMaxHealth;
    private float m_runtimeMoveSpeed;
    private float m_runtimeDamage;
    private float m_runtimeAttackInterval;
    private float m_runtimeAttackRange;
    private float m_currentHealth;
    private bool m_damageEnabled = true;
    private bool m_deathRaised;

    public float CurrentHealth => m_currentHealth;
    public float MaxHealth => m_runtimeMaxHealth;
    public float MoveSpeed => m_runtimeMoveSpeed;
    public float Damage => m_runtimeDamage;
    public float AttackInterval => m_runtimeAttackInterval;
    public float AttackRange => m_runtimeAttackRange;
    public bool IsAlive => m_currentHealth > 0f;
    public bool IsDead => !IsAlive;

    public event Action<float, float> HealthChanged;
    public event Action<DamageInfo> Damaged;
    public event Action Died;

    private void Awake()
    {
        RestoreFullHealth();
    }

    public void RestoreFullHealth()
    {
        m_runtimeMaxHealth = GetPositiveValue(m_zombieData != null ? m_zombieData.MaxHealth : m_maxHealth, 100f);
        m_runtimeMoveSpeed = GetPositiveValue(m_zombieData != null ? m_zombieData.MoveSpeed : m_moveSpeed, 2f);
        m_runtimeDamage = GetNonNegativeValue(m_zombieData != null ? m_zombieData.Damage : m_damage, 10f);
        m_runtimeAttackInterval = GetPositiveValue(m_zombieData != null ? m_zombieData.AttackInterval : m_attackInterval, 1f);
        m_runtimeAttackRange = GetPositiveValue(m_zombieData != null ? m_zombieData.AttackRange : m_attackRange, 1.5f);
        m_currentHealth = m_runtimeMaxHealth;
        m_deathRaised = false;
        HealthChanged?.Invoke(m_currentHealth, m_runtimeMaxHealth);
    }

    public void SetDamageEnabled(bool value)
    {
        m_damageEnabled = value;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        Debug.Log($"Zombie took damage: {damageInfo.amount} from {damageInfo.source?.name ?? "Unknown"} at {damageInfo.hitPoint}");
        float amount = damageInfo.amount;
        if (!m_damageEnabled || !isActiveAndEnabled || !IsAlive || !IsFinite(amount) || amount <= 0f)
            return;

        m_currentHealth = Mathf.Max(0f, m_currentHealth - amount);
        HealthChanged?.Invoke(m_currentHealth, MaxHealth);
        Damaged?.Invoke(damageInfo);

        if (!IsAlive && !m_deathRaised)
        {
            m_deathRaised = true;
            Died?.Invoke();
        }
    }

    private static float GetPositiveValue(float value, float fallback)
    {
        return IsFinite(value) && value > 0f ? value : fallback;
    }

    private static float GetNonNegativeValue(float value, float fallback)
    {
        return IsFinite(value) && value >= 0f ? value : fallback;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
