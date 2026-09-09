using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour, IDamageable
{
    [SerializeField] private CharacterScriptableObject m_characterData;
    [SerializeField, Min(1f)] private float m_maxHealth = 100f;
    [SerializeField, Min(0f)] private float m_moveSpeed = 5f;
    private float m_runtimeMaxHealth;
    private float m_runtimeMoveSpeed;
    private float m_currentHealth;
    private bool m_damageEnabled = true;

    public float CurrentHealth => m_currentHealth;
    public float MaxHealth => m_runtimeMaxHealth;
    public float MoveSpeed => m_runtimeMoveSpeed;
    public bool IsAlive => m_currentHealth > 0f;
    public bool IsDead => !IsAlive;
    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake() => RestoreFullHealth();

    public void RestoreFullHealth()
    {
        float health = m_characterData != null ? m_characterData.MaxHealth : m_maxHealth;
        float speed = m_characterData != null ? m_characterData.MoveSpeed : m_moveSpeed;
        m_runtimeMaxHealth = IsFinite(health) && health > 0f ? health : 100f;
        m_runtimeMoveSpeed = IsFinite(speed) && speed >= 0f ? speed : 5f;
        m_currentHealth = m_runtimeMaxHealth;
        HealthChanged?.Invoke(m_currentHealth, m_runtimeMaxHealth);
    }

    public void SetDamageEnabled(bool value) => m_damageEnabled = value;

    public void TakeDamage(DamageInfo damageInfo)
    {
        float amount = damageInfo.amount;
        if (!m_damageEnabled || !isActiveAndEnabled || !IsAlive || amount <= 0f || !IsFinite(amount)) return;
        m_currentHealth = Mathf.Max(0f, m_currentHealth - amount);
        bool died = !IsAlive;
        HealthChanged?.Invoke(m_currentHealth, m_runtimeMaxHealth);
        if (died) Died?.Invoke();
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
