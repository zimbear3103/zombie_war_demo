using System;
using UnityEngine;

public abstract class ProjectileBehaviour : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField, Min(0.01f)] private float m_speed = 20f;
    [SerializeField, Min(0.01f)] private float m_lifetime = 5f;

    protected Vector3 m_direction;
    protected PlayerStats m_owner;

    private readonly RaycastHit[] m_hitBuffer = new RaycastHit[16];
    private PlayerController m_playerController;
    private TrailRenderer[] m_trails;
    private Action<RaycastHit> m_hitCallback;
    private Action<ProjectileBehaviour> m_releaseCallback;
    private Vector3 m_obstructionOrigin;
    private float m_remainingDistance;
    private float m_remainingLifetime;
    private int m_hitMask;
    private bool m_checkMuzzleObstruction;
    private bool m_isFlying;
    public bool IsFlying => m_isFlying;
    public bool HasValidFlightSettings => IsPositiveFinite(m_speed) && IsPositiveFinite(m_lifetime);

    protected void Initialize(Vector3 direction, Vector3 obstructionOrigin, PlayerStats owner,
        int hitMask, float range, Action<RaycastHit> hitCallback)
    {
        m_direction = direction.normalized;
        m_owner = owner;
        m_playerController = owner != null ? owner.GetComponent<PlayerController>() : null;
        m_hitMask = hitMask;
        m_remainingDistance = range;
        m_remainingLifetime = m_lifetime;
        m_obstructionOrigin = obstructionOrigin;
        m_checkMuzzleObstruction = true;
        m_hitCallback = hitCallback;
        m_isFlying = true;

        CachePoolReferences();
        enabled = true;
        gameObject.SetActive(true);
        foreach (var trail in m_trails)
        {
            if (trail != null) trail.Clear();
        }
    }

    protected virtual void Update()
    {
        Simulate(Time.deltaTime);
    }

    public void Simulate(float deltaTime)
    {
        if (!m_isFlying || !isActiveAndEnabled || !IsPositiveFinite(deltaTime)) return;
        if (m_owner == null || !m_owner.isActiveAndEnabled || !m_owner.IsAlive)
        {
            Recycle();
            return;
        }
        if (m_playerController != null && !m_playerController.CanAct) return;

        if (m_checkMuzzleObstruction)
        {
            m_checkMuzzleObstruction = false;
            Vector3 toMuzzle = transform.position - m_obstructionOrigin;
            float distanceToMuzzle = toMuzzle.magnitude;
            if (distanceToMuzzle > Mathf.Epsilon &&
                TryGetNearestHit(m_obstructionOrigin, toMuzzle / distanceToMuzzle, distanceToMuzzle, out var coverHit))
            {
                Impact(coverHit);
                return;
            }
        }

        float stepTime = Mathf.Min(deltaTime, m_remainingLifetime);
        float stepDistance = Mathf.Min(m_speed * stepTime, m_remainingDistance);
        if (TryGetNearestHit(transform.position, m_direction, stepDistance, out var hit))
        {
            Impact(hit);
            return;
        }

        transform.position += m_direction * stepDistance;
        m_remainingDistance -= stepDistance;
        m_remainingLifetime -= stepTime;
        if (m_remainingDistance <= 0f || m_remainingLifetime <= 0f) Recycle();
    }

    public void Recycle()
    {
        m_isFlying = false;
        m_hitCallback = null;
        m_owner = null;
        m_playerController = null;
        var release = m_releaseCallback;
        m_releaseCallback = null;
        // Clear ownership before OnDisable can re-enter this method.
        if (gameObject.activeSelf) gameObject.SetActive(false);
        release?.Invoke(this);
    }

    protected virtual void OnDisable()
    {
        Recycle();
    }

    internal void CachePoolReferences()
    {
        if (m_trails == null) m_trails = GetComponentsInChildren<TrailRenderer>(true);
    }

    internal void SetPoolReleaseCallback(Action<ProjectileBehaviour> releaseCallback)
    {
        m_releaseCallback = releaseCallback;
    }

    private void Impact(RaycastHit hit)
    {
        m_isFlying = false;
        transform.position = hit.point;
        try
        {
            OnHit(hit);
            m_hitCallback?.Invoke(hit);
        }
        finally
        {
            Recycle();
        }
    }

    protected abstract void OnHit(RaycastHit hit);

    private bool TryGetNearestHit(Vector3 start, Vector3 direction, float distance, out RaycastHit nearestHit)
    {
        int count = Physics.RaycastNonAlloc(start, direction, m_hitBuffer, distance,
            m_hitMask, QueryTriggerInteraction.Ignore);
        if (count < m_hitBuffer.Length) return SelectNearestHit(m_hitBuffer, count, out nearestHit);

        // A full buffer is not guaranteed to contain the nearest collider.
        var overflowHits = Physics.RaycastAll(start, direction, distance, m_hitMask, QueryTriggerInteraction.Ignore);
        return SelectNearestHit(overflowHits, overflowHits.Length, out nearestHit);
    }

    private bool SelectNearestHit(RaycastHit[] hits, int count, out RaycastHit nearestHit)
    {
        nearestHit = default;
        float nearestDistance = float.PositiveInfinity;
        bool found = false;
        for (int index = 0; index < count; index++)
        {
            var candidate = hits[index];
            if (candidate.collider == null || candidate.collider.transform.IsChildOf(m_owner.transform) ||
                candidate.collider.GetComponentInParent<ProjectileBehaviour>() != null) continue;
            if (candidate.distance >= nearestDistance) continue;
            nearestHit = candidate;
            nearestDistance = candidate.distance;
            found = true;
        }
        return found;
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    protected virtual void OnValidate()
    {
        if (!IsPositiveFinite(m_speed)) m_speed = 20f;
        if (!IsPositiveFinite(m_lifetime)) m_lifetime = 5f;
    }
}
