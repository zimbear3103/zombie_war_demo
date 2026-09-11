using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BombController : MonoBehaviour
{
    [Header("Throw")]
    [SerializeField, Min(0.1f)] private float m_throwRadius = 8f;
    [SerializeField, Min(0.1f)] private float m_flightDuration = 0.8f;
    [SerializeField, Min(0.1f)] private float m_arcHeight = 2.5f;
    [SerializeField, Min(0.01f)] private float m_collisionRadius = 0.12f;
    [SerializeField] private LayerMask m_collisionMask = Physics.DefaultRaycastLayers;

    [Header("Explosion")]
    [FormerlySerializedAs("explosionRadius")]
    [SerializeField, Min(0.1f)] private float m_explosionRadius = 5f;
    [FormerlySerializedAs("explosionForce")]
    [SerializeField, Min(0f)] private float m_explosionForce = 700f;
    [FormerlySerializedAs("explosionDelay")]
    [Tooltip("Fuse starts when thrown, including time spent in the air.")]
    [SerializeField, Min(0f)] private float m_explosionDelay = 3f;
    [FormerlySerializedAs("explosionEffectPrefab")]
    [SerializeField] private GameObject m_explosionEffectPrefab;

    private readonly RaycastHit[] m_hitBuffer = new RaycastHit[16];
    private PlayerController m_owner;
    private Vector3 m_startPosition;
    private Vector3 m_targetPosition;
    private float m_flightTime;
    private float m_fallSpeed;
    private float m_countdown;
    private bool m_isArmed;
    private bool m_isFlying;
    private bool m_hasLanded;
    private bool m_hasExploded;

    public bool Throw(PlayerController owner, Vector3 launchPosition, Vector3 direction)
    {
        if (m_isArmed || m_hasExploded || owner == null || !owner.CanAct ||
            !IsFinite(launchPosition) || !IsFinite(direction)) return false;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;
        ValidateSettings();

        m_owner = owner;
        m_startPosition = launchPosition;
        // Range is measured horizontally from the player at the instant of release.
        m_targetPosition = owner.transform.position + direction.normalized * m_throwRadius;
        Vector3 probeStart = m_targetPosition;
        probeStart.y = Mathf.Max(launchPosition.y, m_targetPosition.y) + m_arcHeight + 1f;
        RaycastHit[] groundHits = Physics.RaycastAll(probeStart, Vector3.down,
            probeStart.y - m_targetPosition.y + 20f, m_collisionMask, QueryTriggerInteraction.Ignore);
        if (SelectNearestHit(groundHits, groundHits.Length, true, out RaycastHit groundHit))
            m_targetPosition.y = groundHit.point.y;
        m_targetPosition.y += m_collisionRadius;

        transform.SetParent(null, true);
        transform.SetPositionAndRotation(m_startPosition, Quaternion.LookRotation(direction));
        // Scripted flight owns the position, even if a Rigidbody is added later.
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        m_flightTime = 0f;
        m_fallSpeed = 0f;
        m_countdown = m_explosionDelay;
        m_isFlying = true;
        m_hasLanded = false;
        m_isArmed = true;
        // A stored scene reference may include the pickup component itself.
        foreach (BombPickup pickup in GetComponentsInChildren<BombPickup>(true))
            pickup.enabled = false;
        m_owner.RunReset += CancelThrow;
        enabled = true;
        gameObject.SetActive(true);
        return true;
    }

    private void Update()
    {
        if (!m_isArmed || m_hasExploded) return;
        if (m_owner == null || !m_owner.isActiveAndEnabled || m_owner.Stats == null || !m_owner.Stats.IsAlive)
        {
            CancelThrow();
            return;
        }
        if (!m_owner.CanAct || Time.deltaTime <= 0f) return;

        float step = Mathf.Min(Time.deltaTime, m_countdown);
        if (m_isFlying)
        {
            m_flightTime += step;
            float t = Mathf.Clamp01(m_flightTime / m_flightDuration);
            Vector3 position = Vector3.Lerp(m_startPosition, m_targetPosition, t);
            position.y += 4f * m_arcHeight * t * (1f - t);
            MoveTo(position);
            if (t >= 1f) m_isFlying = false;
        }
        else if (!m_hasLanded)
        {
            // After hitting a wall, drop vertically instead of hanging in the air.
            m_fallSpeed += Mathf.Abs(Physics.gravity.y) * step;
            MoveTo(transform.position + Vector3.down * (m_fallSpeed * step));
        }

        m_countdown -= Time.deltaTime;
        if (m_countdown <= 0f) Explode();
    }

    private void MoveTo(Vector3 position)
    {
        Vector3 movement = position - transform.position;
        float distance = movement.magnitude;
        if (distance <= 0.00001f) return;

        Vector3 direction = movement / distance;
        int count = Physics.SphereCastNonAlloc(transform.position, m_collisionRadius, direction,
            m_hitBuffer, distance, m_collisionMask, QueryTriggerInteraction.Ignore);
        RaycastHit[] hits = m_hitBuffer;
        if (count == m_hitBuffer.Length)
        {
            hits = Physics.SphereCastAll(transform.position, m_collisionRadius, direction,
                distance, m_collisionMask, QueryTriggerInteraction.Ignore);
            count = hits.Length;
        }

        if (SelectNearestHit(hits, count, false, out RaycastHit hit))
        {
            transform.position += direction * Mathf.Max(0f, hit.distance - 0.005f);
            m_isFlying = false;
            m_hasLanded = hit.normal.y > 0.5f;
            return;
        }
        transform.position = position;
    }

    private bool SelectNearestHit(RaycastHit[] hits, int count, bool groundOnly, out RaycastHit nearest)
    {
        nearest = default;
        float distance = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) ||
                (m_owner != null && hit.collider.transform.IsChildOf(m_owner.transform))) continue;
            if (groundOnly && (hit.normal.y <= 0.5f ||
                hit.collider.GetComponentInParent<IDamageable>() != null)) continue;
            if (hit.distance >= distance) continue;
            nearest = hit;
            distance = hit.distance;
            found = true;
        }
        return found;
    }

    public void Explode()
    {
        if (m_hasExploded) return;
        m_hasExploded = true;
        m_isArmed = false;
        ReleaseOwner();

        if (m_explosionEffectPrefab != null)
            Instantiate(m_explosionEffectPrefab, transform.position, transform.rotation);
        KnockBack();
        Destroy(gameObject);
    }

    private void KnockBack()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_explosionRadius);
        var affectedBodies = new HashSet<Rigidbody>();
        foreach (Collider nearbyObject in colliders)
        {
            Rigidbody body = nearbyObject.attachedRigidbody;
            if (body != null && !body.isKinematic && affectedBodies.Add(body))
                body.AddExplosionForce(m_explosionForce, transform.position, m_explosionRadius);
        }
    }

    private void CancelThrow()
    {
        m_isArmed = false;
        ReleaseOwner();
        Destroy(gameObject);
    }

    private void ReleaseOwner()
    {
        if (m_owner != null) m_owner.RunReset -= CancelThrow;
        m_owner = null;
    }

    private void OnDisable()
    {
        m_isArmed = false;
        ReleaseOwner();
    }

    private void OnValidate() => ValidateSettings();

    private void ValidateSettings()
    {
        m_throwRadius = PositiveOrDefault(m_throwRadius, 8f);
        m_flightDuration = PositiveOrDefault(m_flightDuration, 0.8f);
        m_arcHeight = PositiveOrDefault(m_arcHeight, 2.5f);
        m_collisionRadius = PositiveOrDefault(m_collisionRadius, 0.12f);
        m_explosionRadius = PositiveOrDefault(m_explosionRadius, 5f);
        if (!IsFinite(m_explosionForce) || m_explosionForce < 0f) m_explosionForce = 700f;
        if (!IsFinite(m_explosionDelay) || m_explosionDelay < 0f) m_explosionDelay = 3f;
    }

    private static float PositiveOrDefault(float value, float fallback) =>
        IsFinite(value) && value > 0f ? value : fallback;
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
}
