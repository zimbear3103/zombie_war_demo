using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponScriptableObject m_weaponData;

    private float m_cooldownFireTime = 0f;

    private void Start()
    {
        m_cooldownFireTime = m_weaponData.FireRate;
    }

    private void Update()
    {
        m_cooldownFireTime -= Time.deltaTime;
        if (m_cooldownFireTime <= 0f)
        {
            TryFire(transform.forward, 0f);
        }
    }

    virtual public void Configure(Transform muzzle, Transform origin, PlayerHealth owner, int layerMask)
    {
        // Configure the weapon with the given parameters
    }

    virtual public bool TryFire(Vector3 direction, float spread)
    {
        // Implement firing logic here
        m_cooldownFireTime = m_weaponData.FireRate;
        return false;
    }

    public float GetCooldownFireTime()
    {
        return m_cooldownFireTime;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Handle collision with enemy
        }
    }
}
