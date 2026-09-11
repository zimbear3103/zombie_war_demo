using System;
using UnityEngine;

[DisallowMultipleComponent]
public class BulletBehaviour : ProjectileBehaviour
{
    [SerializeField] private GameObject m_bullletTrailEffect;

    private float m_damage;
    private float m_knockbackForce;

    public void Launch(Vector3 direction, Vector3 obstructionOrigin, PlayerStats owner, int hitMask,
        float range, float damage, float knockbackForce, Action<RaycastHit> hitCallback = null)
    {
        m_damage = damage;
        m_knockbackForce = knockbackForce;
        Initialize(direction, obstructionOrigin, owner, hitMask, range, hitCallback);
    }

    protected override void OnHit(RaycastHit hit)
    {
        if (hit.collider == null) return;
        var damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable == null || !damageable.IsAlive) return;

        damageable.TakeDamage(new DamageInfo(m_damage, hit.point, m_direction,
            m_knockbackForce, m_owner.gameObject));
    }

    private void OnSetupBulletTrail()
    {
    }
}
