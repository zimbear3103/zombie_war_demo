using Utility;
using UnityEngine;
using UnityEngine.UI;

public class HitLine : MonoBehaviour
{
    [Header("Judgment ranger")]
    [Tooltip("Tap within this distance of the line = PERFECT")]
    [SerializeField] private float m_perfectRange = 80f;
    [Tooltip("Tap within this distance = GREAT; further out = Miss grade")]
    [SerializeField] private float m_greatRange = 220f;

    [Header("Pulse on hit")]
    [SerializeField] private float m_pulseScale = 1.35f;
    [SerializeField] private float m_pulseTime = 0.15f;

    public float GetOffset(Transform target)
    {
        return GetOffset(target.position);
    }

    public float GetOffset(Vector3 worldPosition)
    {
        return Mathf.Abs(transform.InverseTransformPoint(worldPosition).y);
    }

    public Vector3 GetHitPosition(Transform target)
    {
        Vector3 local = transform.InverseTransformPoint(target.position);
        local.y = 0f;
        local.z = 0f;
        return transform.TransformPoint(local);
    }

    public JudgeLevel Judge(Transform target)
    {
        return Judge(target.position);
    }

    public JudgeLevel Judge(Vector3 worldPosition)
    {
        float offset = GetOffset(worldPosition);

        if (offset <= m_perfectRange)
            return JudgeLevel.Perfect;
        if (offset <= m_greatRange)
            return JudgeLevel.Great;

        return JudgeLevel.Miss;
    }
}
