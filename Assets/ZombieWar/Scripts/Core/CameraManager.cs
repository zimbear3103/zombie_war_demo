using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : Singleton<CameraManager>
{
    [Tooltip("Active Cinemachine camera that follows the player spawned for the current level.")]
    [SerializeField] private CinemachineCamera m_camera;

    public CinemachineCamera Camera => m_camera;

    private void Awake()
    {
        if (m_camera == null)
        {
            m_camera = GetComponentInChildren<CinemachineCamera>(true);
        }
    }

    public bool TrySetPlayerTarget(Transform player, out string error)
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            error = "An active spawned player is required for the camera target.";
            return false;
        }

        if (m_camera == null || !m_camera.isActiveAndEnabled)
        {
            error = "Assign an active CinemachineCamera on CameraManager.";
            return false;
        }

        var target = m_camera.Target;
        target.TrackingTarget = player;
        target.LookAtTarget = player;
        target.CustomLookAtTarget = true;
        m_camera.Target = target;
        m_camera.UpdateTargetCache();
        // A new run starts at its spawn instead of damping from the previous player's pose.
        m_camera.PreviousStateIsValid = false;
        error = null;
        return true;
    }

    public void ClearPlayerTarget(Transform player)
    {
        if (m_camera == null || ReferenceEquals(player, null))
            return;

        var target = m_camera.Target;
        bool trackingMatches = target.TrackingTarget == player;
        bool lookAtMatches = target.LookAtTarget == player;
        if (!trackingMatches && !lookAtMatches)
            return;

        if (trackingMatches)
            target.TrackingTarget = null;
        if (lookAtMatches)
            target.LookAtTarget = null;
        m_camera.Target = target;
        m_camera.UpdateTargetCache();
        m_camera.PreviousStateIsValid = false;
    }

    public void onSetupCameraForPlayer(GameObject player)
    {
        if (!TrySetPlayerTarget(player != null ? player.transform : null, out string error))
            Debug.LogError($"[CameraManager] {error}", this);
    }
}
