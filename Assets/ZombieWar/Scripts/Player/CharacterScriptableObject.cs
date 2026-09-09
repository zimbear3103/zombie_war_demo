using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Zombie War/Character")]
public class CharacterScriptableObject : ScriptableObject
{
    [SerializeField, Min(1f)] private float m_maxHealth = 100f;
    [SerializeField, Min(0f)] private float m_moveSpeed = 5f;

    public float MaxHealth => m_maxHealth;
    public float MoveSpeed => m_moveSpeed;
}
