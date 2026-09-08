using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1f)] private float m_maxHealth = 100f;
    private float m_currentHealth;

    public float CurrentHealth => m_currentHealth;
    public float MaxHealth => m_maxHealth;
    public bool IsAlive => m_currentHealth > 0f;
    public bool IsDead => !IsAlive;
    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake() => RestoreFullHealth();

    public void RestoreFullHealth()
    {
        if (float.IsNaN(m_maxHealth) || float.IsInfinity(m_maxHealth) || m_maxHealth <= 0f)
            m_maxHealth = 100f;
        m_currentHealth = m_maxHealth;
        HealthChanged?.Invoke(m_currentHealth, m_maxHealth);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        float amount = damageInfo.amount;
        if (!IsAlive || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        m_currentHealth = Mathf.Max(0f, m_currentHealth - amount);
        HealthChanged?.Invoke(m_currentHealth, m_maxHealth);
        if (!IsAlive) Died?.Invoke();
    }
}
