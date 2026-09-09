using System;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    private const int HitBufferSize = 16;

    [Header("Weapon Settings")]
    [SerializeField] private WeaponScriptableObject m_weaponData;
    [SerializeField] private Transform m_muzzle;

    private Transform m_origin;
    private PlayerStats m_owner;
    private int m_hitMask;
    private float m_fireInterval;
    private float m_damage;
    private float m_range;
    private int m_pelletCount;
    private float m_spreadAngle;
    private float m_knockbackForce;
    private float m_nextShotTime = float.NegativeInfinity;
    private bool m_gameplayEnabled = true;
    private bool m_isConfigured;
    private bool m_reportedInvalidConfiguration;
    private readonly RaycastHit[] m_hitBuffer = new RaycastHit[HitBufferSize];

    public WeaponScriptableObject Data => m_weaponData;

    public event Action Fired;
    public event Action<RaycastHit> Hit;

    public bool Initialize(WeaponScriptableObject data, Transform origin, PlayerStats owner, int hitMask)
    {
        m_weaponData = data;
        Configure(m_muzzle, origin, owner, hitMask);
        return m_isConfigured;
    }

    public void Configure(Transform muzzle, Transform origin, PlayerStats owner, int hitMask)
    {
        m_muzzle = muzzle;
        m_origin = origin;
        m_owner = owner;
        m_hitMask = hitMask;
        m_isConfigured = ValidateAndCacheConfiguration();

        if (m_isConfigured)
        {
            m_reportedInvalidConfiguration = false;
        }
    }

    public bool TryFire(Vector3 direction, float now)
    {
        if (!m_gameplayEnabled || !isActiveAndEnabled)
        {
            return false;
        }

        if (!m_isConfigured || m_weaponData == null || m_muzzle == null || m_origin == null || m_owner == null)
        {
            ReportInvalidConfiguration("Weapon cannot fire because its data, muzzle, origin or owner is missing.");
            return false;
        }

        if (!m_owner.isActiveAndEnabled || !m_owner.IsAlive)
        {
            return false;
        }

        if (!IsFinite(now) || !IsFinite(direction.x) || !IsFinite(direction.y) || !IsFinite(direction.z))
        {
            return false;
        }

        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
        if (flatDirection.sqrMagnitude <= Mathf.Epsilon || now < m_nextShotTime)
        {
            return false;
        }

        flatDirection.Normalize();
        m_nextShotTime = now + m_fireInterval;
        FireHitscan(flatDirection);
        Fired?.Invoke();
        return true;
    }

    public void SetGameplayEnabled(bool value)
    {
        m_gameplayEnabled = value;
    }

    public void ResetWeapon()
    {
        m_nextShotTime = float.NegativeInfinity;
    }

    private bool ValidateAndCacheConfiguration()
    {
        if (m_weaponData == null || m_muzzle == null || m_origin == null || m_owner == null || m_hitMask == 0)
        {
            ReportInvalidConfiguration("Weapon requires data, muzzle, origin, owner and a non-zero hit mask before it can fire.");
            return false;
        }

        float fireInterval = m_weaponData.FireInterval;
        float damage = m_weaponData.Damage;
        float range = m_weaponData.Range;
        int pelletCount = m_weaponData.PelletCount;
        float spreadAngle = m_weaponData.SpreadAngle;
        float knockbackForce = m_weaponData.KnockbackForce;

        if (!IsFinite(fireInterval) || fireInterval <= 0f ||
            !IsFinite(damage) || damage < 0f ||
            !IsFinite(range) || range <= 0f ||
            pelletCount < 1 || pelletCount > 32 ||
            !IsFinite(spreadAngle) || spreadAngle < 0f || spreadAngle > 360f ||
            !IsFinite(knockbackForce) || knockbackForce < 0f)
        {
            ReportInvalidConfiguration("Weapon data contains an invalid fire interval, damage, range, pellet count, spread or knockback value.");
            return false;
        }

        m_fireInterval = fireInterval;
        m_damage = damage;
        m_range = range;
        m_pelletCount = pelletCount;
        m_spreadAngle = spreadAngle;
        m_knockbackForce = knockbackForce;
        return true;
    }

    private void FireHitscan(Vector3 centerDirection)
    {
        int rayCount = m_weaponData.Kind == WeaponKind.Shotgun ? m_pelletCount : 1;
        Vector3 originToMuzzle = m_muzzle.position - m_origin.position;
        float originToMuzzleDistance = originToMuzzle.magnitude;
        if (originToMuzzleDistance > Mathf.Epsilon &&
            TryGetNearestNonOwnerHit(m_origin.position, originToMuzzle / originToMuzzleDistance, originToMuzzleDistance, out RaycastHit obstruction))
        {
            for (int index = 0; index < rayCount; index++)
            {
                ResolveHit(obstruction, centerDirection);
            }

            return;
        }

        float spreadStep = rayCount > 1 ? m_spreadAngle / (rayCount - 1) : 0f;
        float firstAngle = rayCount > 1 ? -m_spreadAngle * 0.5f : 0f;

        for (int index = 0; index < rayCount; index++)
        {
            float angle = firstAngle + spreadStep * index;
            Vector3 rayDirection = Quaternion.AngleAxis(angle, Vector3.up) * centerDirection;
            FireRay(rayDirection);
        }
    }

    private void FireRay(Vector3 direction)
    {
        if (!TryGetNearestNonOwnerHit(m_muzzle.position, direction, m_range, out RaycastHit hit))
        {
            return;
        }

        ResolveHit(hit, direction);
    }

    private void ResolveHit(RaycastHit hit, Vector3 direction)
    {
        if (hit.collider == null)
        {
            return;
        }

        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null && damageable.IsAlive)
        {
            var damageInfo = new DamageInfo(
                m_damage,
                hit.point,
                direction,
                m_knockbackForce,
                m_owner.gameObject);
            damageable.TakeDamage(damageInfo);
        }

        Hit?.Invoke(hit);
    }

    private bool TryGetNearestNonOwnerHit(
        Vector3 start,
        Vector3 direction,
        float distance,
        out RaycastHit nearestHit)
    {
        int hitCount = Physics.RaycastNonAlloc(
            start,
            direction,
            m_hitBuffer,
            distance,
            m_hitMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount < m_hitBuffer.Length)
        {
            return TrySelectNearestNonOwnerHit(m_hitBuffer, hitCount, out nearestHit);
        }

        RaycastHit[] overflowHits = Physics.RaycastAll(
            start,
            direction,
            distance,
            m_hitMask,
            QueryTriggerInteraction.Ignore);
        return TrySelectNearestNonOwnerHit(overflowHits, overflowHits.Length, out nearestHit);
    }

    private bool TrySelectNearestNonOwnerHit(RaycastHit[] hits, int hitCount, out RaycastHit nearestHit)
    {
        nearestHit = default;
        float nearestDistance = float.PositiveInfinity;
        bool foundHit = false;

        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit candidate = hits[index];
            if (candidate.collider == null || IsOwnerCollider(candidate.collider))
            {
                continue;
            }

            if (candidate.distance < nearestDistance)
            {
                nearestHit = candidate;
                nearestDistance = candidate.distance;
                foundHit = true;
            }
        }

        return foundHit;
    }

    private bool IsOwnerCollider(Collider collider)
    {
        return m_owner != null && collider.transform.IsChildOf(m_owner.transform);
    }

    private void ReportInvalidConfiguration(string message)
    {
        if (m_reportedInvalidConfiguration)
        {
            return;
        }

        m_reportedInvalidConfiguration = true;
        Debug.LogError(message, this);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
