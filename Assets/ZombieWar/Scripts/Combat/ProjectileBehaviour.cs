using UnityEngine;

public class ProjectileBehaviour : MonoBehaviour
{
    protected Vector3 m_direction;
    [SerializeField] private float m_speed = 20f;
    [SerializeField] private float m_lifetime = 5f;
    [SerializeField] private float m_damage = 10f;

    virtual public void Initialize(Vector3 direction)
    {
        m_direction = direction.normalized;
        Destroy(gameObject, m_lifetime);
    }

    protected void DirectHit(Vector3 dir)
    {
        m_direction = dir;
    }
}
