using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_playerController;
    [SerializeField] private Animator m_animator;

    [Header("Locomotion")]
    [FormerlySerializedAs("speedDampTime")]
    [SerializeField, Min(0f)] private float m_speedDampTime = 0.08f;
    [SerializeField] private string m_locomotionStateName = "Movement.Movement";

    private static readonly int m_velocityHash = Animator.StringToHash("Velocity");

    private PlayerStats m_stats;
    private int m_locomotionStateHash;
    private bool m_hasVelocityParameter;
    private bool m_hasLocomotionState;
    private bool m_resetPending;

    private void Awake()
    {
        if (m_playerController == null) m_playerController = GetComponentInParent<PlayerController>();
        if (m_animator == null && m_playerController != null)
            m_animator = m_playerController.GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        if (m_playerController == null || m_animator == null || m_animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Player animation requires a PlayerController and an Animator with a controller assigned.", this);
            enabled = false;
            return;
        }

        CacheAnimatorSetup();
        m_stats = m_playerController.Stats;
        m_playerController.RunReset += ResetForRun;
        if (m_stats != null) m_stats.Died += OnDied;
        ResetForRun();
    }

    private void OnDisable()
    {
        if (m_playerController != null) m_playerController.RunReset -= ResetForRun;
        if (m_stats != null) m_stats.Died -= OnDied;
        m_stats = null;
        ResetForRun();
    }

    private void Update()
    {
        if (m_animator == null || !m_animator.isActiveAndEnabled || Time.deltaTime <= 0f) return;

        if (m_resetPending)
        {
            if (m_hasLocomotionState) m_animator.Play(m_locomotionStateHash, 0, 0f);
            m_resetPending = false;
        }

        // Movement is already updated; the Animator and hand rig evaluate after this Update.
        bool canMove = m_playerController != null && m_playerController.CanAct;
        float speed = canMove ? m_playerController.NormalizedMoveSpeed : 0f;
        SetVelocity(speed, canMove ? m_speedDampTime : 0f);
    }

    private void OnDied()
    {
        SetVelocity(0f);
    }

    private void ResetForRun()
    {
        m_resetPending = true;
        SetVelocity(0f);
    }

    private void CacheAnimatorSetup()
    {
        m_hasVelocityParameter = false;
        foreach (AnimatorControllerParameter parameter in m_animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == m_velocityHash)
                m_hasVelocityParameter = true;
        }

        m_locomotionStateHash = Animator.StringToHash(m_locomotionStateName);
        m_hasLocomotionState = m_animator.HasState(0, m_locomotionStateHash);
        if (!m_hasVelocityParameter || !m_hasLocomotionState)
            Debug.LogWarning("Player Animator requires a Velocity float and the configured locomotion state with an idle/combat_run blend tree.", this);
    }

    private void SetVelocity(float value, float dampTime = 0f)
    {
        if (!m_hasVelocityParameter || m_animator == null || m_animator.runtimeAnimatorController == null) return;
        if (dampTime > 0f) m_animator.SetFloat(m_velocityHash, value, dampTime, Time.deltaTime);
        else m_animator.SetFloat(m_velocityHash, value);
    }
}
