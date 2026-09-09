using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class ZombieSpawner : MonoBehaviour
{
    [Serializable]
    public class Wave
    {
        [FormerlySerializedAs("waveName")]
        [SerializeField] private string m_waveName = "Wave";
        [FormerlySerializedAs("zombieGroups")]
        [SerializeField] private List<ZombieGroup> m_zombieGroups = new();
        [FormerlySerializedAs("spawnRate")]
        [SerializeField, Min(0.05f)] private float m_spawnInterval = 2f;

        public string WaveName => m_waveName;
        public List<ZombieGroup> ZombieGroups => m_zombieGroups;
        public float SpawnInterval => m_spawnInterval;
    }

    [Serializable]
    public class ZombieGroup
    {
        [FormerlySerializedAs("zombiePrefabs")]
        [SerializeField] private GameObject m_zombiePrefab;
        [FormerlySerializedAs("zombieName")]
        [SerializeField] private string m_zombieName = "Zombie";
        [FormerlySerializedAs("zombieCount")]
        [SerializeField, Min(1)] private int m_zombieCount = 1;

        public GameObject ZombiePrefab => m_zombiePrefab;
        public string ZombieName => m_zombieName;
        public int ZombieCount => m_zombieCount;
    }

    [FormerlySerializedAs("waves")]
    [SerializeField] private List<Wave> m_waves = new();
    [SerializeField, Min(1)] private int m_aliveCap = 40;

    [Header("Spawn Area Settings")]
    [FormerlySerializedAs("spawnPoints")]
    [SerializeField] private List<Transform> m_spawnPoints = new();
    [SerializeField] private Transform m_spawnedZombieParent;
    [SerializeField, Min(0f)] private float m_playerSafetyRadius = 5f;
    [SerializeField, Min(0.1f)] private float m_navMeshSampleRadius = 2f;
    [SerializeField, Min(1)] private int m_spawnPointScanLimit = 8;
    [SerializeField] private int m_navMeshAreaMask = NavMesh.AllAreas;

    private readonly List<ZombieController> m_liveZombies = new();
    private ObjectPooling m_pool;
    private Transform m_target;
    private PlayerStats m_targetStats;
    private int m_currentWaveIndex;
    private int m_currentGroupIndex;
    private int m_spawnedInCurrentGroup;
    private float m_spawnTimer;
    private bool m_isRunning;
    private bool m_gameplayEnabled;

    public int AliveCount => m_liveZombies.Count;
    public bool IsRunning => m_isRunning;
    public event Action<ZombieController> ZombieKilled;

    private void Update()
    {
        if (!m_isRunning)
            return;

        if (m_target == null
            || !m_target.gameObject.activeInHierarchy
            || m_targetStats == null
            || !m_targetStats.isActiveAndEnabled
            || m_pool == null
            || !m_pool.isActiveAndEnabled)
        {
            EndRun();
            return;
        }

        if (!m_gameplayEnabled)
            return;

        m_spawnTimer += Time.deltaTime;
        if (m_spawnTimer < GetCurrentSpawnInterval())
            return;

        m_spawnTimer = 0f;
        if (AliveCount >= GetAliveCap())
            return;
      
        TrySpawnNextZombie();
    }

    private void OnDisable()
    {
        EndRun();
    }

    public bool BeginRun(Transform target, PlayerStats targetStats)
    {
        EndRun();

        if (!isActiveAndEnabled)
        {
            Debug.LogError("ZombieSpawner cannot begin while its component or GameObject is disabled.", this);
            return false;
        }

        m_pool = ObjectPooling.Instance;
        if (!ValidateConfiguration(target, targetStats, out string validationError))
        {
            Debug.LogError($"ZombieSpawner cannot begin: {validationError}", this);
            m_pool = null;
            return false;
        }

        m_target = target;
        m_targetStats = targetStats;
        m_currentWaveIndex = 0;
        m_currentGroupIndex = 0;
        m_spawnedInCurrentGroup = 0;
        m_spawnTimer = 0f;
        m_isRunning = true;
        m_gameplayEnabled = true;
        return true;
    }

    public void SetGameplayEnabled(bool value)
    {
        m_gameplayEnabled = value && m_isRunning;

        for (int i = m_liveZombies.Count - 1; i >= 0; i--)
        {
            ZombieController zombie = m_liveZombies[i];
            if (zombie == null)
            {
                m_liveZombies.RemoveAt(i);
                continue;
            }

            zombie.SetGameplayEnabled(m_gameplayEnabled);
        }
    }

    public void EndRun()
    {
        m_isRunning = false;
        m_gameplayEnabled = false;

        for (int i = m_liveZombies.Count - 1; i >= 0; i--)
            ReleaseZombie(m_liveZombies[i], false, false);

        m_liveZombies.Clear();
        m_target = null;
        m_targetStats = null;
        m_currentWaveIndex = 0;
        m_currentGroupIndex = 0;
        m_spawnedInCurrentGroup = 0;
        m_spawnTimer = 0f;
        m_pool = null;
    }

    private bool ValidateConfiguration(Transform target, PlayerStats targetStats, out string error)
    {
        if (target == null
            || !target.gameObject.activeInHierarchy
            || targetStats == null
            || !targetStats.isActiveAndEnabled)
        {
            error = "an active target and enabled target stats are required.";
            return false;
        }

        if (m_pool == null || !m_pool.isActiveAndEnabled)
        {
            error = "an active ObjectPooling component is required.";
            return false;
        }

        if (m_spawnPoints == null || !HasAuthoredSpawnPoint())
        {
            error = "at least one authored spawn point is required.";
            return false;
        }

        if (m_waves == null || m_waves.Count == 0)
        {
            error = "at least one wave is required.";
            return false;
        }

        for (int waveIndex = 0; waveIndex < m_waves.Count; waveIndex++)
        {
            Wave wave = m_waves[waveIndex];
            if (wave == null || wave.ZombieGroups == null || wave.ZombieGroups.Count == 0)
            {
                error = $"wave {waveIndex} has no zombie groups.";
                return false;
            }

            if (!IsFinite(wave.SpawnInterval) || wave.SpawnInterval <= 0f)
            {
                error = $"wave {waveIndex} has an invalid spawn interval; use a finite value above zero.";
                return false;
            }

            for (int groupIndex = 0; groupIndex < wave.ZombieGroups.Count; groupIndex++)
            {
                ZombieGroup group = wave.ZombieGroups[groupIndex];
                if (group == null || group.ZombiePrefab == null || group.ZombieCount <= 0)
                {
                    error = $"wave {waveIndex}, group {groupIndex} has invalid prefab or quota.";
                    return false;
                }

                ZombieController controller = group.ZombiePrefab.GetComponent<ZombieController>();
                ZombieStats stats = group.ZombiePrefab.GetComponent<ZombieStats>();
                NavMeshAgent agent = group.ZombiePrefab.GetComponent<NavMeshAgent>();

                if (controller == null || !controller.enabled)
                {
                    error = $"{group.ZombiePrefab.name} needs an enabled ZombieController on its root.";
                    return false;
                }

                if (stats == null || !stats.enabled)
                {
                    error = $"{group.ZombiePrefab.name} needs enabled ZombieStats on its root.";
                    return false;
                }

                if (agent == null || !agent.enabled)
                {
                    error = $"{group.ZombiePrefab.name} needs an enabled NavMeshAgent on its root.";
                    return false;
                }
            }
        }

        if (m_spawnedZombieParent != null && !m_spawnedZombieParent.gameObject.activeInHierarchy)
        {
            error = "the assigned spawned-zombie parent must be active.";
            return false;
        }

        error = null;
        return true;
    }

    private bool HasAuthoredSpawnPoint()
    {
        for (int i = 0; i < m_spawnPoints.Count; i++)
        {
            if (m_spawnPoints[i] != null)
                return true;
        }

        return false;
    }

    private void TrySpawnNextZombie()
    {
        Wave wave = m_waves[m_currentWaveIndex];
        ZombieGroup group = wave.ZombieGroups[m_currentGroupIndex];

        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
            return;
        Debug.Log($"Spawning {group.ZombiePrefab.name} at {spawnPosition} for wave {m_currentWaveIndex}, group {m_currentGroupIndex}.", this);
        Transform parent = m_spawnedZombieParent != null ? m_spawnedZombieParent : transform;
        GameObject instance = m_pool.AcquireInactive(group.ZombiePrefab, parent);
        if (instance == null)
        {
            AbortRun("the object pool failed to acquire a zombie instance.");
            return;
        }

        instance.transform.SetPositionAndRotation(spawnPosition, GetSpawnRotation(spawnPosition));
        ZombieController zombie = instance.GetComponent<ZombieController>();
        if (zombie == null || !zombie.PrepareForSpawn(m_target, m_targetStats, m_gameplayEnabled))
        {
            m_pool.Despawn(instance);
            AbortRun($"{group.ZombiePrefab.name} failed runtime zombie initialization.");
            return;
        }

        zombie.Died += HandleZombieDied;
        zombie.Released += HandleZombieReleased;
        m_liveZombies.Add(zombie);
        instance.SetActive(true);
        if (!zombie.IsSpawnReady)
        {
            ReleaseZombie(zombie, false, false);
            AbortRun($"{group.ZombiePrefab.name} could not activate on its NavMesh. Check its agent type and spawn surface.");
            return;
        }
        AdvanceSpawnQuota(group);
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        int pointCount = m_spawnPoints.Count;
        int attempts = Mathf.Min(Mathf.Max(1, m_spawnPointScanLimit), pointCount);
        int startIndex = UnityEngine.Random.Range(0, pointCount);
        float safetyRadius = Mathf.Max(0f, m_playerSafetyRadius);
        float safetyRadiusSquared = safetyRadius * safetyRadius;
        float sampleRadius = GetPositiveValue(m_navMeshSampleRadius, 2f);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Transform spawnPoint = m_spawnPoints[(startIndex + attempt) % pointCount];
            if (spawnPoint == null || IsInsideSafetyRadius(spawnPoint.position, safetyRadiusSquared))
                continue;

            if (!NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit hit, sampleRadius, m_navMeshAreaMask))
                continue;

            if (IsInsideSafetyRadius(hit.position, safetyRadiusSquared))
                continue;

            position = hit.position;
            return true;
        }

        position = default;
        return false;
    }

    private bool IsInsideSafetyRadius(Vector3 position, float safetyRadiusSquared)
    {
        Vector3 difference = position - m_target.position;
        difference.y = 0f;
        return difference.sqrMagnitude < safetyRadiusSquared;
    }

    private Quaternion GetSpawnRotation(Vector3 spawnPosition)
    {
        Vector3 direction = m_target.position - spawnPosition;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
    }

    private void AdvanceSpawnQuota(ZombieGroup group)
    {
        m_spawnedInCurrentGroup++;
        if (m_spawnedInCurrentGroup < group.ZombieCount)
            return;

        m_spawnedInCurrentGroup = 0;
        m_currentGroupIndex++;

        if (m_currentGroupIndex < m_waves[m_currentWaveIndex].ZombieGroups.Count)
            return;

        m_currentGroupIndex = 0;
        if (m_currentWaveIndex < m_waves.Count - 1)
            m_currentWaveIndex++;
    }

    private float GetCurrentSpawnInterval()
    {
        return m_waves[m_currentWaveIndex].SpawnInterval;
    }

    private int GetAliveCap()
    {
        return m_aliveCap > 0 ? m_aliveCap : 40;
    }

    private void HandleZombieDied(ZombieController zombie)
    {
        ReleaseZombie(zombie, true, false);
    }

    private void HandleZombieReleased(ZombieController zombie)
    {
        ReleaseZombie(zombie, false, true);
    }

    private void ReleaseZombie(ZombieController zombie, bool killed, bool alreadyInactive)
    {
        if (zombie == null)
        {
            m_liveZombies.Remove(zombie);
            return;
        }

        if (!m_liveZombies.Remove(zombie))
            return;

        zombie.Died -= HandleZombieDied;
        zombie.Released -= HandleZombieReleased;

        if (killed)
            ZombieKilled?.Invoke(zombie);

        zombie.PrepareForPool();
        if (alreadyInactive && !zombie.gameObject.activeSelf)
            return;

        if (m_pool != null)
            m_pool.Despawn(zombie.gameObject);
        else
            zombie.gameObject.SetActive(false);
    }

    private void AbortRun(string reason)
    {
        Debug.LogError($"ZombieSpawner stopped: {reason}", this);
        EndRun();
    }

    private static float GetPositiveValue(float value, float fallback)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f
            ? value
            : fallback;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
