using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(ZombieStats))]
public class ZombieController : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float m_repathInterval = 0.25f;
    [SerializeField, Min(0f)] private float m_coverCheckHeight = 0.5f;
    [SerializeField] private LayerMask m_coverMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0.01f)] private float m_knockbackDeceleration = 20f;

    [SerializeField] private Animator m_animator;
    [SerializeField]
    private Transform m_target;
    private PlayerStats m_targetStats;
    private NavMeshAgent m_agent;
    private ZombieStats m_stats;
    private float m_repathTimer;
    private float m_attackCooldownRemaining;
    private Vector3 m_knockbackVelocity;
    private bool m_gameplayEnabled;
    private bool m_isSpawned;
    private bool m_componentsCached;
    private readonly RaycastHit[] m_coverHits = new RaycastHit[16];
    private Vector2 m_velocity;
    private Vector2 m_smoothDeltaPosition;
    public ZombieStats Stats => m_stats;
    public bool IsSpawnReady => m_isSpawned && isActiveAndEnabled && m_stats != null &&
                                m_stats.isActiveAndEnabled && CanUseAgent();
    public event Action<ZombieController> Died;
    public event Action<ZombieController> Released;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        if (!m_isSpawned)
            return;

        ConfigureAgent();
        m_stats.SetDamageEnabled(m_gameplayEnabled);
        SetAgentStopped(!m_gameplayEnabled);
    }

    private void Update()
    {
        if (!CanPursueTarget())
        {
            SetAgentStopped(true);
            return;
        }

        float deltaTime = Time.deltaTime;
        m_repathTimer -= deltaTime;
        m_attackCooldownRemaining = Mathf.Max(0f, m_attackCooldownRemaining - deltaTime);

        if (m_knockbackVelocity.sqrMagnitude > 0.0001f)
        {
            SetAgentStopped(true);
            m_agent.Move(m_knockbackVelocity * deltaTime);
            m_knockbackVelocity = Vector3.MoveTowards(m_knockbackVelocity, Vector3.zero,
                GetPositiveValue(m_knockbackDeceleration, 20f) * deltaTime);
            return;
        }

        Vector3 direction = m_target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= m_stats.AttackRange * m_stats.AttackRange && !IsMeleeBlocked())
        {
            SetAgentStopped(true);
            TryAttack(direction);
            return;
        }

        SetAgentStopped(false);
        if (m_repathTimer <= 0f)
        {
            //SynchronizeAnimatorOrAgent();
            TrySetDestination(m_target.position);
            m_repathTimer = GetPositiveValue(m_repathInterval, 0.25f);
        }
    }

    private void OnDisable()
    {
        if (!m_isSpawned)
            return;

        m_isSpawned = false;
        ClearRuntimeState();
        Released?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (m_stats != null)
        {
            m_stats.Died -= HandleStatsDied;
            m_stats.Damaged -= HandleDamaged;
        }
    }

    public bool PrepareForSpawn(Transform target, PlayerStats targetStats, bool gameplayEnabled)
    {
        CacheComponents();

        if (!gameObject.activeSelf && !enabled) enabled = true;
        if (!enabled || m_agent == null || !m_agent.enabled || m_stats == null || !m_stats.enabled ||
            target == null || !target.gameObject.activeInHierarchy || targetStats == null ||
            !targetStats.isActiveAndEnabled || m_coverMask.value == 0)
            return false;

        m_target = target;
        m_targetStats = targetStats;
        m_repathTimer = 0f;
        m_attackCooldownRemaining = 0f;
        m_knockbackVelocity = Vector3.zero;
        m_isSpawned = true;
        m_gameplayEnabled = gameplayEnabled;

        m_stats.RestoreFullHealth();
        m_stats.SetDamageEnabled(gameplayEnabled);
        ConfigureAgent();
        ResetAgentPath();
        SetAgentStopped(!gameplayEnabled);
        return true;
    }

    private void SynchronizeAnimatorOrAgent()
    {
        Vector3  worldDeltaPosition = m_agent.nextPosition - transform.position;
        worldDeltaPosition.y = 0f;

        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);

        Vector2 deltaPosition = new Vector2(dx, dy);

        float smooth = Mathf.Min(1, Time.deltaTime / 0.1f);
        m_smoothDeltaPosition = Vector2.Lerp(m_smoothDeltaPosition, deltaPosition, smooth);

        m_velocity = m_smoothDeltaPosition * Time.deltaTime;

        if (m_agent.remainingDistance <= m_agent.stoppingDistance)
        {
            m_velocity = Vector2.Lerp(
                Vector2.zero, 
                m_velocity, 
                m_agent.remainingDistance/m_agent.stoppingDistance);
        }

        bool shouldMode = m_velocity.magnitude > 0.5f && m_agent.remainingDistance > m_agent.stoppingDistance;

        m_animator.SetBool("move", m_agent.velocity.magnitude > 0.5f);
        m_animator.SetFloat("locomotion", m_agent.velocity.magnitude);

        float deltaMagnitude = worldDeltaPosition.magnitude;
        if (deltaMagnitude > m_agent.radius/2f) {
            transform.position = Vector3.Lerp(
               m_animator.rootPosition,
               m_agent.nextPosition,
               smooth);
        }

    }
    public void SetGameplayEnabled(bool value)
    {
        m_gameplayEnabled = value && m_isSpawned && m_stats != null && m_stats.IsAlive;

        if (m_stats != null)
            m_stats.SetDamageEnabled(m_gameplayEnabled);

        SetAgentStopped(!m_gameplayEnabled);
    }

    public void PrepareForPool()
    {
        m_isSpawned = false;
        ClearRuntimeState();
    }

    private void CacheComponents()
    {
        if (m_componentsCached)
            return;

        m_agent = GetComponent<NavMeshAgent>();
        m_stats = GetComponent<ZombieStats>();
        m_animator = GetComponentInChildren<Animator>();

        //m_animator.applyRootMotion = true;
        //m_agent.updatePosition = false;
        //m_agent.updateRotation = true;
        if (m_stats != null)
        {
            m_stats.Died += HandleStatsDied;
            m_stats.Damaged += HandleDamaged;
        }

        m_componentsCached = true;
    }

    private void ConfigureAgent()
    {
        if (m_agent == null || m_stats == null)
            return;

        m_agent.speed = m_stats.MoveSpeed;
        // Stop explicitly only when in melee range with a clear path to the target.
        m_agent.stoppingDistance = 0f;
    }

    private bool CanPursueTarget()
    {
        return IsSpawnReady
            && m_gameplayEnabled
            && m_stats != null
            && m_stats.IsAlive
            && m_target != null
            && m_target.gameObject.activeInHierarchy
            && m_targetStats != null
            && m_targetStats.isActiveAndEnabled
            && m_targetStats.IsAlive;
    }

    private void TryAttack(Vector3 direction)
    {
        if (m_attackCooldownRemaining > 0f)
            return;

        Vector3 hitDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : transform.forward;

        m_animator.SetTrigger("Attack");
        m_targetStats.TakeDamage(new DamageInfo(
            m_stats.Damage,
            m_target.position,
            hitDirection,
            0f,
            gameObject));
        m_attackCooldownRemaining = m_stats.AttackInterval;

    }

    private bool IsMeleeBlocked()
    {
        Vector3 offset = Vector3.up * m_coverCheckHeight;
        Vector3 start = transform.position + offset;
        Vector3 direction = m_target.position + offset - start;
        float distance = direction.magnitude;
        if (distance <= 0.0001f) return false;

        int count = Physics.RaycastNonAlloc(start, direction / distance, m_coverHits, distance,
            m_coverMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = m_coverHits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform) ||
                hitCollider.transform.IsChildOf(m_targetStats.transform))
                continue;
            return true;
        }

        // A full buffer may omit cover. Fail closed without allocating in the melee loop.
        return count == m_coverHits.Length;
    }

    private void TrySetDestination(Vector3 destination)
    {
        if (CanUseAgent())
            m_agent.SetDestination(destination);
    }

    private void SetAgentStopped(bool value)
    {
        if (CanUseAgent())
            m_agent.isStopped = value;
    }

    private void ResetAgentPath()
    {
        if (CanUseAgent())
            m_agent.ResetPath();
    }

    private bool CanUseAgent()
    {
        return m_agent != null && m_agent.enabled && m_agent.isOnNavMesh;
    }

    private void HandleStatsDied()
    {
        if (!m_isSpawned)
            return;

        m_gameplayEnabled = false;
        m_stats.SetDamageEnabled(false);
        SetAgentStopped(true);
        Died?.Invoke(this);
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        ApplyKnockback(damageInfo.hitDirection, damageInfo.knockbackForce);
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (!CanPursueTarget() || !IsFinite(force) || force <= 0f ||
            !IsFinite(direction.x) || !IsFinite(direction.z))
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;
        // A hit refreshes velocity; pellets from one shot do not multiply the impulse.
        m_knockbackVelocity = direction.normalized * force;
    }

    private void ClearRuntimeState()
    {
        m_gameplayEnabled = false;
        m_target = null;
        m_targetStats = null;
        m_repathTimer = 0f;
        m_attackCooldownRemaining = 0f;
        m_knockbackVelocity = Vector3.zero;

        if (m_stats != null)
            m_stats.SetDamageEnabled(false);

        SetAgentStopped(true);
        ResetAgentPath();
    }

    private static float GetPositiveValue(float value, float fallback)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f
            ? value
            : fallback;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void OnAnimatorMove()
    {
        Vector3 rootPosition = m_animator.rootPosition;
        rootPosition.y = m_agent.nextPosition.y;
        transform.position = rootPosition;
        transform.rotation = m_animator.rootRotation;
        m_agent.nextPosition = rootPosition;
    }
}
