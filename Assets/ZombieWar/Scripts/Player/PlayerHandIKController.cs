using UnityEngine;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
[DefaultExecutionOrder(110)]
public class PlayerHandIKController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController m_playerController;
    [SerializeField] private Animator m_animator;
    [SerializeField] private TwoBoneIKConstraint m_leftTwoBoneIKConstraint;
    [SerializeField] private TwoBoneIKConstraint m_rightTwoBoneIKConstraint;

    [Header("Hand Blending")]
    [Tooltip("Seconds to blend the grip pose and full IK weight. Zero applies changes immediately.")]
    [SerializeField, Min(0f)] private float m_equipBlendDuration = 0.2f;
    [SerializeField, Range(0f, 1f)] private float m_leftHandWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float m_rightHandWeight = 1f;

    private struct HandBlend
    {
        public Transform target;
        public Transform grip;
        public Vector3 startPosition;
        public Quaternion startRotation;
        public Vector3 gripPosition;
        public Quaternion gripRotation;
        public float poseProgress;
        public float weightProgress;
        public float startWeight;
        public float targetWeight;
    }

    private PlayerStats m_stats;
    private WeaponController m_weapon;
    private Transform m_socket;
    private HandBlend m_leftHand;
    private HandBlend m_rightHand;

    private void Awake()
    {
        if (m_playerController == null) m_playerController = GetComponentInParent<PlayerController>();
        if (m_animator == null && m_playerController != null)
            m_animator = m_playerController.GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        ReleaseImmediately();
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        m_stats = m_playerController.Stats;
        m_playerController.WeaponChanged += OnWeaponChanged;
        m_playerController.RunReset += ReleaseImmediately;
        if (m_stats != null) m_stats.Died += ReleaseImmediately;
        OnWeaponChanged(m_playerController.ActiveWeapon);
    }

    private void OnDisable()
    {
        if (m_playerController != null)
        {
            m_playerController.WeaponChanged -= OnWeaponChanged;
            m_playerController.RunReset -= ReleaseImmediately;
        }
        if (m_stats != null) m_stats.Died -= ReleaseImmediately;
        m_stats = null;
        ReleaseImmediately();
    }

    private void Update()
    {
        if (m_stats == null || !m_stats.IsAlive || m_playerController == null || m_socket == null ||
            m_animator == null || m_leftTwoBoneIKConstraint == null || m_rightTwoBoneIKConstraint == null ||
            m_leftHand.target == null || m_rightHand.target == null)
        {
            ReleaseImmediately();
            return;
        }
        if (!m_playerController.isActiveAndEnabled || !m_animator.isActiveAndEnabled)
        {
            ResetWeights();
            return;
        }
        if (!m_playerController.CanAct || Time.deltaTime <= 0f) return;

        bool hasWeapon = m_weapon != null && m_weapon.isActiveAndEnabled;
        UpdateHand(ref m_rightHand, m_rightTwoBoneIKConstraint, hasWeapon, m_rightHandWeight);
        UpdateHand(ref m_leftHand, m_leftTwoBoneIKConstraint, hasWeapon, m_leftHandWeight);
    }

    private void UpdateHand(ref HandBlend hand, TwoBoneIKConstraint constraint, bool hasWeapon, float handWeight)
    {
        bool hasGrip = hasWeapon && hand.grip != null;
        if (hasGrip)
        {
            hand.poseProgress = AdvanceBlend(hand.poseProgress);
            float blend = Mathf.SmoothStep(0f, 1f, hand.poseProgress);
            // The socket stays independent of the arm bones. Targets remain bound across weapon switches.
            hand.target.SetLocalPositionAndRotation(Vector3.Lerp(hand.startPosition, hand.gripPosition, blend),
                Quaternion.Slerp(hand.startRotation, hand.gripRotation, blend));
        }

        // This two-clip setup keeps the holding pose during gameplay reloads.
        float desiredWeight = hasGrip ? handWeight : 0f;
        if (!Mathf.Approximately(desiredWeight, hand.targetWeight))
        {
            hand.startWeight = constraint.weight;
            hand.targetWeight = desiredWeight;
            hand.weightProgress = 0f;
        }
        hand.weightProgress = AdvanceBlend(hand.weightProgress);
        constraint.weight = Mathf.Lerp(hand.startWeight, hand.targetWeight,
            Mathf.SmoothStep(0f, 1f, hand.weightProgress));
    }

    private void OnWeaponChanged(WeaponController weapon)
    {
        m_weapon = weapon;
        m_leftHand.grip = null;
        m_rightHand.grip = null;
        if (weapon == null || m_socket == null || m_leftHand.target == null || m_rightHand.target == null) return;
        if (weapon.transform.parent != m_socket || weapon.RightHandGrip == null ||
            !weapon.RightHandGrip.IsChildOf(weapon.transform))
        {
            Debug.LogWarning("Hand IK requires the weapon directly under WeaponSocket and RightHandGrip inside that weapon. Both hands will release.", weapon);
            return;
        }

        CacheGrip(ref m_rightHand, m_rightTwoBoneIKConstraint, weapon.RightHandGrip);
        if (weapon.LeftHandGrip == null) return;
        if (!weapon.LeftHandGrip.IsChildOf(weapon.transform))
        {
            Debug.LogWarning("LeftHandGrip must be inside the equipped weapon. Left-hand IK will release.", weapon);
            return;
        }
        CacheGrip(ref m_leftHand, m_leftTwoBoneIKConstraint, weapon.LeftHandGrip);
    }

    private void CacheGrip(ref HandBlend hand, TwoBoneIKConstraint constraint, Transform grip)
    {
        hand.grip = grip;
        hand.poseProgress = 0f;
        hand.startPosition = hand.target.localPosition;
        hand.startRotation = hand.target.localRotation;
        // Grip markers are fixed weapon-local poses, sampled only when equipping.
        hand.gripPosition = m_socket.InverseTransformPoint(grip.position);
        hand.gripRotation = Quaternion.Inverse(m_socket.rotation) * grip.rotation;
        if (constraint.weight <= 0f)
        {
            Transform tip = constraint.data.tip;
            hand.startPosition = m_socket.InverseTransformPoint(tip.position);
            hand.startRotation = Quaternion.Inverse(m_socket.rotation) * tip.rotation;
        }
    }

    private float AdvanceBlend(float progress)
    {
        return m_equipBlendDuration > 0f
            ? Mathf.Clamp01(progress + Time.deltaTime / m_equipBlendDuration) : 1f;
    }

    private void ReleaseImmediately()
    {
        m_weapon = null;
        m_leftHand.grip = null;
        m_rightHand.grip = null;
        ResetWeights();
    }

    private void ResetWeights()
    {
        ResetHandWeight(ref m_leftHand, m_leftTwoBoneIKConstraint);
        ResetHandWeight(ref m_rightHand, m_rightTwoBoneIKConstraint);
    }

    private static void ResetHandWeight(ref HandBlend hand, TwoBoneIKConstraint constraint)
    {
        hand.startWeight = 0f;
        hand.targetWeight = 0f;
        hand.weightProgress = 0f;
        if (constraint != null) constraint.weight = 0f;
    }

    private bool ValidateSetup()
    {
        if (m_playerController == null || m_animator == null || !m_animator.isHuman ||
            m_animator.avatar == null || !m_animator.avatar.isValid ||
            m_animator.GetComponentInParent<PlayerController>() != m_playerController ||
            m_leftTwoBoneIKConstraint == null || m_rightTwoBoneIKConstraint == null ||
            m_leftTwoBoneIKConstraint == m_rightTwoBoneIKConstraint)
        {
            Debug.LogWarning("PlayerHandIKController requires this player's valid Humanoid Animator and separate left/right Two Bone IK constraints. Hand IK has been disabled.", this);
            return false;
        }

        m_socket = m_playerController.WeaponSocket;
        if (m_socket == null || m_socket.parent != m_animator.transform || !IsOutsideSkeleton(m_socket))
        {
            Debug.LogWarning("Place WeaponSocket directly beneath the Animator object, outside the skeleton, so neither arm moves the weapon. Hand IK has been disabled.", this);
            return false;
        }

        RigBuilder builder = m_animator.GetComponent<RigBuilder>();
        Rig rig = m_leftTwoBoneIKConstraint.GetComponentInParent<Rig>();
        bool hasActiveLayer = false;
        if (builder != null && builder.isActiveAndEnabled && rig != null && rig.isActiveAndEnabled && rig.weight > 0f)
        {
            foreach (RigLayer layer in builder.layers)
            {
                if (layer.rig == rig && layer.active) hasActiveLayer = true;
            }
        }
        if (!hasActiveLayer || m_rightTwoBoneIKConstraint.GetComponentInParent<Rig>() != rig ||
            !rig.transform.IsChildOf(m_animator.transform))
        {
            Debug.LogWarning("Both hand constraints must belong to the same enabled Rig beneath the Animator, with positive weight and an active RigBuilder layer. Hand IK has been disabled.", this);
            return false;
        }

        if (!ValidateHand(m_leftTwoBoneIKConstraint, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand) ||
            !ValidateHand(m_rightTwoBoneIKConstraint, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand)) return false;
        if (m_leftTwoBoneIKConstraint.data.target == m_rightTwoBoneIKConstraint.data.target)
        {
            Debug.LogWarning("Left and right hand constraints require separate persistent targets. Hand IK has been disabled.", this);
            return false;
        }

        m_leftHand.target = m_leftTwoBoneIKConstraint.data.target;
        m_rightHand.target = m_rightTwoBoneIKConstraint.data.target;
        return true;
    }

    private bool ValidateHand(TwoBoneIKConstraint constraint, HumanBodyBones root, HumanBodyBones mid, HumanBodyBones tip)
    {
        var data = constraint.data;
        if (!constraint.isActiveAndEnabled || !constraint.IsValid() ||
            data.root != m_animator.GetBoneTransform(root) || data.mid != m_animator.GetBoneTransform(mid) ||
            data.tip != m_animator.GetBoneTransform(tip))
        {
            Debug.LogWarning("Each hand IK constraint must use its own Left or Right UpperArm/LowerArm/Hand chain from this Animator. Hand IK has been disabled.", constraint);
            return false;
        }
        if (data.target == null || data.target.parent != m_socket ||
            (data.hint != null && (!data.hint.IsChildOf(m_animator.transform) || !IsOutsideSkeleton(data.hint))))
        {
            Debug.LogWarning("Place each persistent hand target directly under WeaponSocket and its elbow hint beneath the Animator outside the skeleton. Hand IK has been disabled.", constraint);
            return false;
        }
        if (data.maintainTargetPositionOffset || data.maintainTargetRotationOffset ||
            !Mathf.Approximately(data.targetPositionWeight, 1f) || !Mathf.Approximately(data.targetRotationWeight, 1f))
        {
            Debug.LogWarning("Set hand IK Target Position/Rotation Weight to 1 and disable both Maintain Target Offset options so weapon grips define exact wrist poses. Hand IK has been disabled.", constraint);
            return false;
        }
        return true;
    }

    private bool IsOutsideSkeleton(Transform target)
    {
        for (int index = 0; index < (int)HumanBodyBones.LastBone; index++)
        {
            Transform bone = m_animator.GetBoneTransform((HumanBodyBones)index);
            if (bone != null && (target == bone || target.IsChildOf(bone) || bone.IsChildOf(target))) return false;
        }
        return true;
    }
}
