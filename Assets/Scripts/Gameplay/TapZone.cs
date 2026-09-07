using UnityEngine;

public class TapZone : MonoBehaviour
{
    private RectTransform m_rect;
    private readonly Vector3[] m_corners = new Vector3[4];

    private void Awake()
    {
        m_rect = (RectTransform)transform;
    }

    public Vector3 GetBottomEdgeWorldPosition()
    {
        m_rect.GetWorldCorners(m_corners);
        return m_corners[0];
    }
}
