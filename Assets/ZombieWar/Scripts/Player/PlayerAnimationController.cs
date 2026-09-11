using UnityEngine;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_playerController;
    [SerializeField] private Animator m_animator;

    [Header("Hand IK")]
    [SerializeField] private TwoBoneIKConstraint m_rightTwoBoneIKConstraint;
    [SerializeField] private TwoBoneIKConstraint m_leftTwoBoneIKConstraint;

    [Header("Equip")]
    [Tooltip("Seconds to blend into a weapon's hand grips. Zero applies the pose immediately.")]
    [SerializeField, Min(0f)] private float m_equipBlendDuration = 0.2f;

    private Transform m_rightHandTarget;
    private Transform m_leftHandTarget;
    private Transform m_rightHandGrip;
    private Transform m_leftHandGrip;
    private WeaponController m_weapon;
    private PlayerStats m_stats;
    private HandPose m_rightStartPose;
    private HandPose m_leftStartPose;
    private float m_equipProgress;

    private struct HandPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public float weight;
    }

    private void Awake()
    {
        if (m_playerController == null) m_playerController = GetComponentInParent<PlayerController>();
        if (m_animator == null) m_animator = GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        if (m_playerController == null || m_animator == null)
        {
            Debug.LogError("PlayerAnimationController requires a PlayerController and the model's Animator.", this);
            enabled = false;
            return;
        }

        RigBuilder rigBuilder = m_animator.GetComponent<RigBuilder>();
        if (rigBuilder == null || !rigBuilder.enabled)
        {
            Debug.LogError("Equip IK requires an enabled RigBuilder on the same GameObject as the Animator.", this);
            enabled = false;
            return;
        }

        m_rightHandTarget = GetHandTarget(m_rightTwoBoneIKConstraint, rigBuilder);
        m_leftHandTarget = GetHandTarget(m_leftTwoBoneIKConstraint, rigBuilder);
        if (m_rightHandTarget == null && m_leftHandTarget == null)
        {
            Debug.LogError("Equip IK requires at least one configured hand constraint in an active Rig layer.", this);
            enabled = false;
            return;
        }

        SetHandWeights(0f);
        m_playerController.WeaponChanged += OnWeaponChanged;
        m_stats = m_playerController.Stats;
        if (m_stats != null) m_stats.Died += OnDied;
        OnWeaponChanged(m_playerController.ActiveWeapon);
    }

    private void OnDisable()
    {
        if (m_playerController != null) m_playerController.WeaponChanged -= OnWeaponChanged;
        if (m_stats != null) m_stats.Died -= OnDied;
        m_stats = null;
        m_weapon = null;
        m_rightHandGrip = null;
        m_leftHandGrip = null;
        SetHandWeights(0f);
    }

    private void Update()
    {
        if (m_animator == null || !m_animator.isActiveAndEnabled || Time.deltaTime <= 0f) return;

        if (m_stats != null && !m_stats.IsAlive)
        {
            SetHandWeights(0f);
            return;
        }

        m_equipProgress = m_equipBlendDuration > 0f
            ? Mathf.Clamp01(m_equipProgress + Time.deltaTime / m_equipBlendDuration) : 1f;
        float blend = Mathf.SmoothStep(0f, 1f, m_equipProgress);

        // Update runs after player facing and before the normal Animator/rig evaluation.
        UpdateHand(m_rightTwoBoneIKConstraint, m_rightHandTarget, m_rightHandGrip, m_rightStartPose, blend);
        UpdateHand(m_leftTwoBoneIKConstraint, m_leftHandTarget, m_leftHandGrip, m_leftStartPose, blend);
    }

    private void OnWeaponChanged(WeaponController weapon)
    {
        // Preserve the current targets and weights when switching during an unfinished blend.
        m_rightStartPose = CaptureHandPose(m_rightTwoBoneIKConstraint, m_rightHandTarget);
        m_leftStartPose = CaptureHandPose(m_leftTwoBoneIKConstraint, m_leftHandTarget);
        m_equipProgress = 0f;
        m_weapon = weapon;
        m_rightHandGrip = null;
        m_leftHandGrip = null;

        if (weapon == null) return;
        if (!weapon.transform.IsChildOf(m_animator.transform) || IsInArmHierarchy(weapon.transform))
        {
            Debug.LogWarning("Equip IK requires WeaponSocket under the Animator, outside the arm skeleton. Move the socket manually.", weapon);
            return;
        }

        m_rightHandGrip = GetWeaponGrip(weapon.RightHandGrip, weapon);
        m_leftHandGrip = GetWeaponGrip(weapon.LeftHandGrip, weapon);
        if (m_rightHandGrip == null && m_leftHandGrip == null)
            Debug.LogWarning("Weapon has no valid hand grips. Equip IK will release both hands.", weapon);
    }

    private HandPose CaptureHandPose(TwoBoneIKConstraint constraint, Transform target)
    {
        if (constraint == null || target == null) return default;

        float weight = constraint.weight;
        Transform source = weight > 0f ? target : constraint.data.tip;
        return new HandPose
        {
            position = m_animator.transform.InverseTransformPoint(source.position),
            rotation = Quaternion.Inverse(m_animator.transform.rotation) * source.rotation,
            weight = weight
        };
    }

    private void UpdateHand(TwoBoneIKConstraint constraint, Transform target, Transform grip, HandPose startPose, float blend)
    {
        if (constraint == null || target == null) return;

        bool hasGrip = m_weapon != null && m_weapon.isActiveAndEnabled && grip != null;
        Transform animationRoot = m_animator.transform;
        Vector3 position = startPose.position;
        Quaternion rotation = startPose.rotation;
        if (hasGrip)
        {
            position = Vector3.Lerp(position, animationRoot.InverseTransformPoint(grip.position), blend);
            rotation = Quaternion.Slerp(rotation, Quaternion.Inverse(animationRoot.rotation) * grip.rotation, blend);
        }

        // Stable targets stay bound to the rig; only their poses change for each weapon.
        target.SetPositionAndRotation(animationRoot.TransformPoint(position), animationRoot.rotation * rotation);
        constraint.weight = Mathf.Lerp(startPose.weight, hasGrip ? 1f : 0f, blend);
    }

    private Transform GetHandTarget(TwoBoneIKConstraint constraint, RigBuilder rigBuilder)
    {
        if (constraint == null) return null;
        Rig rig = constraint.GetComponentInParent<Rig>();
        bool hasActiveLayer = false;
        foreach (RigLayer layer in rigBuilder.layers)
        {
            if (layer.rig == rig && layer.active && rig != null) hasActiveLayer = true;
        }

        var data = constraint.data;
        if (!constraint.enabled || !constraint.IsValid() || !hasActiveLayer ||
            !constraint.transform.IsChildOf(m_animator.transform) ||
            !data.root.IsChildOf(m_animator.transform) ||
            !data.target.IsChildOf(m_animator.transform) || data.root.IsChildOf(data.target) ||
            IsInArmHierarchy(data.target) || (data.hint != null && IsInArmHierarchy(data.hint)))
        {
            constraint.weight = 0f;
            Debug.LogWarning("Hand IK requires a valid chain, an active Rig layer, and independent targets/hints outside the arm skeleton.", constraint);
            return null;
        }

        return data.target;
    }

    private bool IsInArmHierarchy(Transform target)
    {
        Transform rightRoot = m_rightTwoBoneIKConstraint != null ? m_rightTwoBoneIKConstraint.data.root : null;
        Transform leftRoot = m_leftTwoBoneIKConstraint != null ? m_leftTwoBoneIKConstraint.data.root : null;
        return (rightRoot != null && target.IsChildOf(rightRoot)) ||
               (leftRoot != null && target.IsChildOf(leftRoot));
    }

    private Transform GetWeaponGrip(Transform grip, WeaponController weapon)
    {
        if (grip == null || grip.IsChildOf(weapon.transform)) return grip;
        Debug.LogWarning("Hand grips must belong to their weapon instance.", weapon);
        return null;
    }

    private void SetHandWeights(float weight)
    {
        if (m_rightTwoBoneIKConstraint != null) m_rightTwoBoneIKConstraint.weight = weight;
        if (m_leftTwoBoneIKConstraint != null) m_leftTwoBoneIKConstraint.weight = weight;
    }

    private void OnDied()
    {
        m_weapon = null;
        m_rightHandGrip = null;
        m_leftHandGrip = null;
        SetHandWeights(0f);
    }
}
