using System;
using System.Collections;
using TheVayuputra;
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
    [Tooltip("X: Moving blend (0-1). Y: measured world speed at playback multiplier 1. Recalibrate after changing model scale or clips. Keep speeds positive and increasing.")]
    [SerializeField] private AnimationCurve m_movementSpeedByBlend = new AnimationCurve(
        new Keyframe(0f, 0.07218f, 0.1138f, 0.1138f),
        new Keyframe(0.25f, 0.10063f, 0.1138f, 0.21368f),
        new Keyframe(0.5f, 0.15405f, 0.21368f, 0.46516f),
        new Keyframe(0.75f, 0.27034f, 0.46516f, 1.44808f),
        new Keyframe(1f, 0.63236f, 1.44808f, 1.44808f));
    [Tooltip("Animator world scale used when measuring the movement speed curve.")]
    [SerializeField, Min(0.0001f)] private float m_animationReferenceScale = 0.1f;
    [SerializeField, Min(0f)] private float m_animationDampTime = 0.1f;

    [Header("Death and Hit Effects")]
    [SerializeField] private HitFlashEffect m_hitFlashEffect;
    [SerializeField] private DissolveEffect m_dissolveEffect;
    [Tooltip("Maximum scaled seconds to wait for the dying state before continuing to dissolve.")]
    [SerializeField, Min(0.1f)] private float m_deathAnimationTimeout = 5f;

    private static readonly int m_moveHash = Animator.StringToHash("move");
    private static readonly int m_locomotionHash = Animator.StringToHash("locomotion");
    private static readonly int m_playbackSpeedHash = Animator.StringToHash("movementPlaybackSpeed");
    private static readonly int m_attackHash = Animator.StringToHash("Attack");
    private static readonly int m_attackStateHash = Animator.StringToHash("attack");
    private static readonly int m_dyingStateHash = Animator.StringToHash("Base Layer.dying");
    private static readonly int m_deathHash = Animator.StringToHash("death");

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
    private bool m_isDying;
    private bool m_collidersDisabledForDeath;
    private bool m_agentDisabledForDeath;
    private bool m_deathAnimatorOverridesApplied;
    private int m_spawnVersion;
    private Coroutine m_deathRoutine;
    private Collider[] m_colliders;
    private bool[] m_colliderEnabledStates;
    private float m_savedAnimatorSpeed;
    private AnimatorCullingMode m_savedAnimatorCullingMode;
    private float m_animationScaleMultiplier = 1f;
    private readonly RaycastHit[] m_coverHits = new RaycastHit[16];
    public ZombieStats Stats => m_stats;
    public bool IsSpawnReady => m_isSpawned && isActiveAndEnabled && m_stats != null &&
                                m_stats.isActiveAndEnabled && CanUseAgent();

    // Died records the kill; DeathCompleted allows the spawner to return the corpse to its pool.
    public event Action<ZombieController> Died;
    public event Action<ZombieController> DeathCompleted;
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
        ResetAnimation();
        m_stats.SetDamageEnabled(m_gameplayEnabled);
        SetAgentStopped(!m_gameplayEnabled);
    }

    private void Update()
    {
        if (m_isDying)
            return;

        UpdateMovement();
        SynchronizeAnimation();
    }

    private void UpdateMovement()
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
        if (CanUseAnimator())
            m_animator.ResetTrigger(m_attackHash);

        if (m_repathTimer <= 0f)
        {
            TrySetDestination(m_target.position);
            m_repathTimer = GetPositiveValue(m_repathInterval, 0.25f);
        }
    }

    private void OnDisable()
    {
        bool wasSpawned = m_isSpawned;
        m_isSpawned = false;
        ClearRuntimeState();
        if (wasSpawned)
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
        ResetDeathSequence();

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
        m_spawnVersion++;
        m_isSpawned = true;
        m_gameplayEnabled = gameplayEnabled;

        m_stats.RestoreFullHealth();
        m_stats.SetDamageEnabled(gameplayEnabled);
        ConfigureAgent();
        ResetAgentPath();
        SetAgentStopped(!gameplayEnabled);
        ResetAnimation();
        return true;
    }

    private void SynchronizeAnimation()
    {
        if (m_isDying || !CanUseAnimator())
            return;

        // Pool parents can change the world scale without changing the prefab.
        m_animationScaleMultiplier = GetPositiveValue(Mathf.Abs(m_animator.transform.lossyScale.z), 0.1f)
            / GetPositiveValue(m_animationReferenceScale, 0.1f);
        bool canPursue = CanPursueTarget();
        Vector3 velocity = canPursue && !m_agent.isStopped ? m_agent.velocity : Vector3.zero;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        bool moving = speed > 0.05f;
        m_animator.SetBool(m_moveHash, moving);

        if (!moving)
        {
            m_animator.SetFloat(m_locomotionHash, 0f);
            m_animator.SetFloat(m_playbackSpeedHash, 1f);
            return;
        }

        float blend = GetLocomotionBlend(speed);
        m_animator.SetFloat(m_locomotionHash, blend, m_animationDampTime, Time.deltaTime);

        // Match stride cadence to the speed of the currently blended clips.
        float blendedSpeed = GetAnimationSpeed(m_animator.GetFloat(m_locomotionHash));
        m_animator.SetFloat(m_playbackSpeedHash, speed / blendedSpeed);
    }

    private float GetLocomotionBlend(float speed)
    {
        if (speed <= GetAnimationSpeed(0f)) return 0f;
        if (speed >= GetAnimationSpeed(1f)) return 1f;

        // Blend Trees synchronize clip cycles, so their speed is not a linear blend.
        float low = 0f;
        float high = 1f;
        for (int i = 0; i < 8; i++)
        {
            float middle = (low + high) * 0.5f;
            if (GetAnimationSpeed(middle) < speed)
                low = middle;
            else
                high = middle;
        }
        return (low + high) * 0.5f;
    }

    private float GetAnimationSpeed(float blend)
    {
        float speed = m_movementSpeedByBlend != null && m_movementSpeedByBlend.length > 0
            ? m_movementSpeedByBlend.Evaluate(blend)
            : 0.63236f;
        return GetPositiveValue(speed, 0.63236f) * m_animationScaleMultiplier;
    }

    private bool CanUseAnimator()
    {
        return m_animator != null && m_animator.isActiveAndEnabled &&
               m_animator.runtimeAnimatorController != null;
    }

    private void ResetAnimation()
    {
        if (!CanUseAnimator())
            return;

        m_animator.Rebind();
        m_animator.SetBool(m_moveHash, false);
        m_animator.SetFloat(m_locomotionHash, 0f);
        m_animator.SetFloat(m_playbackSpeedHash, 1f);
        m_animator.ResetTrigger(m_attackHash);
        if (m_animator.HasState(0, m_dyingStateHash))
            m_animator.ResetTrigger(m_deathHash);
        m_animator.Update(0f);
    }

    public void SetGameplayEnabled(bool value)
    {
        m_gameplayEnabled = value && m_isSpawned && m_stats != null && m_stats.IsAlive;

        if (m_stats != null)
            m_stats.SetDamageEnabled(m_gameplayEnabled);

        SetAgentStopped(!m_gameplayEnabled);
        SynchronizeAnimation();
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
        if (m_animator == null)
            m_animator = GetComponentInChildren<Animator>(true);
        if (m_hitFlashEffect == null)
            m_hitFlashEffect = GetComponentInChildren<HitFlashEffect>(true);
        if (m_dissolveEffect == null)
            m_dissolveEffect = GetComponentInChildren<DissolveEffect>(true);

        m_colliders = GetComponentsInChildren<Collider>(true);
        m_colliderEnabledStates = new bool[m_colliders.Length];

        if (m_animator != null)
            m_animator.applyRootMotion = false;

        if (m_agent != null)
        {
            m_agent.updatePosition = true;
            m_agent.updateRotation = true;
        }
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

        if (CanUseAnimator() && m_animator.GetCurrentAnimatorStateInfo(0).shortNameHash != m_attackStateHash &&
            (!m_animator.IsInTransition(0) || m_animator.GetNextAnimatorStateInfo(0).shortNameHash != m_attackStateHash))
            m_animator.SetTrigger(m_attackHash);
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
        if (!m_isSpawned || m_isDying)
            return;

        m_isDying = true;
        m_gameplayEnabled = false;
        m_knockbackVelocity = Vector3.zero;
        m_stats.SetDamageEnabled(false);
        SetAgentStopped(true);
        ResetAgentPath();
        DisableDeathColliders();

        // A corpse must not keep participating in crowd avoidance.
        if (m_agent != null && m_agent.enabled)
        {
            m_agentDisabledForDeath = true;
            m_agent.enabled = false;
        }

        int spawnVersion = m_spawnVersion;
        Died?.Invoke(this);

        // A kill listener can end/restart the run and even reuse this instance immediately.
        if (IsCurrentDeath(spawnVersion))
            m_deathRoutine = StartCoroutine(PlayDeathSequence(spawnVersion));
    }

    private IEnumerator PlayDeathSequence(int spawnVersion)
    {
        bool animationStarted = BeginDeathAnimation();

        // Ensure the coroutine handle is assigned even when animation/effects are missing.
        yield return null;

        if (animationStarted)
            yield return WaitForDeathAnimation(spawnVersion);

        if (!IsCurrentDeath(spawnVersion))
            yield break;

        if (m_hitFlashEffect != null)
            m_hitFlashEffect.ResetEffect();

        if (m_dissolveEffect != null && m_dissolveEffect.TryPlayDissolve())
        {
            while (IsCurrentDeath(spawnVersion) && m_dissolveEffect != null && m_dissolveEffect.IsPlaying)
                yield return null;
        }

        if (!IsCurrentDeath(spawnVersion))
            yield break;

        m_deathRoutine = null;
        DeathCompleted?.Invoke(this);
    }

    private bool BeginDeathAnimation()
    {
        if (!CanUseAnimator() || !m_animator.HasState(0, m_dyingStateHash))
            return false;

        m_savedAnimatorSpeed = m_animator.speed;
        m_savedAnimatorCullingMode = m_animator.cullingMode;
        m_deathAnimatorOverridesApplied = true;
        m_animator.speed = GetPositiveValue(m_savedAnimatorSpeed, 1f);
        m_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        m_animator.ResetTrigger(m_attackHash);
        m_animator.ResetTrigger(m_deathHash);
        m_animator.SetBool(m_moveHash, false);
        m_animator.SetFloat(m_locomotionHash, 0f);
        m_animator.SetFloat(m_playbackSpeedHash, 1f);

        // Enter dying from any state without depending on authored trigger transitions.
        m_animator.Play(m_dyingStateHash, 0, 0f);
        m_animator.Update(0f);
        return true;
    }

    private IEnumerator WaitForDeathAnimation(int spawnVersion)
    {
        float remainingTime = GetPositiveValue(m_deathAnimationTimeout, 5f);
        bool enteredDying = false;

        while (IsCurrentDeath(spawnVersion) && CanUseAnimator() && remainingTime > 0f)
        {
            AnimatorStateInfo state = m_animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == m_dyingStateHash)
            {
                enteredDying = true;

                // The current controller exits dying before normalizedTime reaches one.
                if (state.normalizedTime >= 1f || m_animator.IsInTransition(0))
                    break;
            }
            else if (enteredDying)
            {
                break;
            }

            remainingTime -= Time.deltaTime;
            yield return null;
        }

        // Hold the observed death pose during dissolve; reset restores the original speed.
        if (IsCurrentDeath(spawnVersion) && CanUseAnimator())
            m_animator.speed = 0f;
    }

    private bool IsCurrentDeath(int spawnVersion)
    {
        return m_isSpawned && m_isDying && isActiveAndEnabled && m_spawnVersion == spawnVersion;
    }

    private void DisableDeathColliders()
    {
        for (int i = 0; i < m_colliders.Length; i++)
        {
            Collider hitCollider = m_colliders[i];
            if (hitCollider == null)
                continue;

            m_colliderEnabledStates[i] = hitCollider.enabled;
            hitCollider.enabled = false;
        }

        m_collidersDisabledForDeath = true;
    }

    private void ResetDeathSequence()
    {
        if (m_deathRoutine != null)
        {
            StopCoroutine(m_deathRoutine);
            m_deathRoutine = null;
        }

        m_isDying = false;

        // Restore a pending flash before restoring the dissolve material.
        if (m_hitFlashEffect != null)
            m_hitFlashEffect.ResetEffect();
        if (m_dissolveEffect != null)
            m_dissolveEffect.ResetEffect();

        if (m_collidersDisabledForDeath)
        {
            for (int i = 0; i < m_colliders.Length; i++)
            {
                if (m_colliders[i] != null)
                    m_colliders[i].enabled = m_colliderEnabledStates[i];
            }

            m_collidersDisabledForDeath = false;
        }

        if (m_agentDisabledForDeath)
        {
            if (m_agent != null)
                m_agent.enabled = true;
            m_agentDisabledForDeath = false;
        }

        if (m_deathAnimatorOverridesApplied)
        {
            if (m_animator != null)
            {
                m_animator.speed = m_savedAnimatorSpeed;
                m_animator.cullingMode = m_savedAnimatorCullingMode;
            }

            m_deathAnimatorOverridesApplied = false;
        }
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        if (!m_isSpawned || m_isDying)
            return;

        if (m_hitFlashEffect != null)
            m_hitFlashEffect.PlayOnDamageVFX();
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
        ResetDeathSequence();
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
        ResetAnimation();
    }

    private static float GetPositiveValue(float value, float fallback)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f
            ? value
            : fallback;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

}
