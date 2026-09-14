using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelMap : MonoBehaviour
{
    [Tooltip("Active transform inside this map. Its position and rotation are applied to the player at Start and Restart.")]
    [SerializeField] private Transform m_playerSpawnPoint;
    [Tooltip("Active transforms inside this map. Place them on the baked NavMesh, outside the player safety radius.")]
    [SerializeField] private List<Transform> m_zombieSpawnPoints = new();
    [Tooltip("Enabled surface inside this map with saved, baked NavMesh Data. Runtime baking is not performed.")]
    [SerializeField] private NavMeshSurface m_navMeshSurface;

    public Transform PlayerSpawnPoint => m_playerSpawnPoint;
    public IReadOnlyList<Transform> ZombieSpawnPoints => m_zombieSpawnPoints;
    public NavMeshSurface NavMeshSurface => m_navMeshSurface;

    public bool TryValidate(out string error)
    {
        if (!enabled || !gameObject.activeSelf)
        {
            error = "The map root and its LevelMap component must be enabled.";
            return false;
        }

        if (!HasUnitScale(transform.lossyScale))
        {
            error = "The map root must have world scale (1, 1, 1); baked NavMesh data does not scale with the map.";
            return false;
        }

        if (!IsActiveInsideMap(m_playerSpawnPoint))
        {
            error = "Player Spawn Point must be an active transform inside the map prefab.";
            return false;
        }

        if (m_zombieSpawnPoints == null || m_zombieSpawnPoints.Count == 0)
        {
            error = "The map needs at least one Zombie Spawn Point.";
            return false;
        }

        for (int i = 0; i < m_zombieSpawnPoints.Count; i++)
        {
            if (!IsActiveInsideMap(m_zombieSpawnPoints[i]))
            {
                error = $"Zombie Spawn Point {i + 1} must be an active transform inside the map prefab.";
                return false;
            }
        }

        if (m_navMeshSurface == null || !m_navMeshSurface.enabled ||
            !IsActiveInsideMap(m_navMeshSurface.transform) || m_navMeshSurface.navMeshData == null)
        {
            error = "Assign an enabled NavMeshSurface inside the map with saved, baked NavMesh Data.";
            return false;
        }

        if (!HasUnitScale(m_navMeshSurface.transform.lossyScale))
        {
            error = "The NavMeshSurface must have world scale (1, 1, 1); baked NavMesh data does not support scaling.";
            return false;
        }

        error = null;
        return true;
    }

    private bool IsActiveInsideMap(Transform target)
    {
        // Prefab assets need authored activeSelf checks instead of activeInHierarchy.
        for (Transform current = target; current != null; current = current.parent)
        {
            if (!current.gameObject.activeSelf)
                return false;

            if (current == transform)
                return true;
        }

        return false;
    }

    private static bool HasUnitScale(Vector3 scale)
    {
        return Mathf.Approximately(scale.x, 1f) &&
               Mathf.Approximately(scale.y, 1f) &&
               Mathf.Approximately(scale.z, 1f);
    }
}
