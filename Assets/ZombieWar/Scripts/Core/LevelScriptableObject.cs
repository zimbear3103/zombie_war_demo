using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "LevelData", menuName = "Level/Level")]
public class LevelScriptableObject : ScriptableObject
{
    [SerializeField] private string m_levelId = "level_01";
    [SerializeField] private string m_levelName = "Level 1";
    [Tooltip("Assign LevelMap from the root of a map prefab. Spawn points and baked NavMesh belong to this prefab.")]
    [SerializeField] private LevelMap m_mapPrefab;
    [Tooltip("Survive this many gameplay seconds to win. Pausing does not advance the timer.")]
    [SerializeField, Min(0.01f)] private float m_survivalDuration = 180f;

    [Header("Zombie Waves")]
    [Tooltip("Waves run in order. The final wave repeats until the survival timer finishes.")]
    [SerializeField] private List<ZombieSpawner.Wave> m_waves = new();
    [SerializeField, Min(1)] private int m_aliveCap = 40;
    [Tooltip("Minimum horizontal distance in metres between the player and a zombie spawn.")]
    [SerializeField, Min(0f)] private float m_playerSafetyRadius = 5f;

    public string LevelId => m_levelId;
    public string LevelName => m_levelName;
    public LevelMap MapPrefab => m_mapPrefab;
    public float SurvivalDuration => m_survivalDuration;
    public IReadOnlyList<ZombieSpawner.Wave> Waves => m_waves;
    public int AliveCap => m_aliveCap;
    public float PlayerSafetyRadius => m_playerSafetyRadius;

    public bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(m_levelId) || string.IsNullOrWhiteSpace(m_levelName))
        {
            error = "Level ID and Level Name are required.";
            return false;
        }

        if (m_mapPrefab == null || m_mapPrefab.transform.parent != null)
        {
            error = "Assign a LevelMap component on the root of a map prefab.";
            return false;
        }

        if (!m_mapPrefab.TryValidate(out error))
            return false;

        if (!IsFinite(m_survivalDuration) || m_survivalDuration <= 0f)
        {
            error = "Survival Duration must be a finite number above zero.";
            return false;
        }

        if (m_aliveCap < 1 || !IsFinite(m_playerSafetyRadius) || m_playerSafetyRadius < 0f)
        {
            error = "Alive Cap must be at least 1 and Player Safety Radius must be a finite non-negative number.";
            return false;
        }

        if (m_waves == null || m_waves.Count == 0)
        {
            error = "At least one zombie wave is required.";
            return false;
        }

        for (int waveIndex = 0; waveIndex < m_waves.Count; waveIndex++)
        {
            ZombieSpawner.Wave wave = m_waves[waveIndex];
            if (wave == null || !IsFinite(wave.SpawnInterval) || wave.SpawnInterval <= 0f)
            {
                error = $"Wave {waveIndex + 1} needs a finite Spawn Interval above zero.";
                return false;
            }

            if (wave.ZombieGroups == null || wave.ZombieGroups.Count == 0)
            {
                error = $"Wave {waveIndex + 1} needs at least one zombie group.";
                return false;
            }

            for (int groupIndex = 0; groupIndex < wave.ZombieGroups.Count; groupIndex++)
            {
                ZombieSpawner.ZombieGroup group = wave.ZombieGroups[groupIndex];
                if (group == null || group.ZombiePrefab == null || group.ZombieCount < 1)
                {
                    error = $"Wave {waveIndex + 1}, group {groupIndex + 1} needs a zombie prefab and a count of at least 1.";
                    return false;
                }

                GameObject prefab = group.ZombiePrefab;
                ZombieController controller = prefab.GetComponent<ZombieController>();
                ZombieStats stats = prefab.GetComponent<ZombieStats>();
                NavMeshAgent agent = prefab.GetComponent<NavMeshAgent>();
                if (controller == null || !controller.enabled || stats == null || !stats.enabled ||
                    agent == null || !agent.enabled)
                {
                    error = $"{prefab.name} needs enabled ZombieController, ZombieStats and NavMeshAgent components on its root.";
                    return false;
                }
            }
        }

        error = null;
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
