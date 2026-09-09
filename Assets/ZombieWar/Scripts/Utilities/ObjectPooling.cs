using System.Collections.Generic;
using UnityEngine;

public class ObjectPooling : Singleton<ObjectPooling>
{
    [System.Serializable]
    public class PrewarmEntry
    {
        public GameObject prefab;
        public int count = 10;
        public Transform parent;
    }

    [Tooltip("Pools filled at Start so the first spawns cause no Instantiate spike during gameplay")]
    [SerializeField] private PrewarmEntry[] m_prewarmEntries;

    private readonly Dictionary<GameObject, List<GameObject>> m_pools = new();
    private Transform m_inactiveCreationRoot;

    private void Start()
    {
        if (m_prewarmEntries == null)
            return;

        foreach (var entry in m_prewarmEntries)
        {
            if (entry.prefab != null)
                Prewarm(entry.prefab, entry.count, entry.parent);
        }
    }

    public void Prewarm(GameObject prefab, int count, Transform parent = null)
    {
        if (prefab == null || count <= 0)
            return;

        var pool = GetPool(prefab);
        while (pool.Count < count)
        {
            CreateInstance(prefab, parent, pool);
        }
    }

    public GameObject Spawn(GameObject prefab, Transform parent = null)
    {
        var instance = AcquireInactive(prefab, parent);
        if (instance != null)
            instance.SetActive(true);

        return instance;
    }

    public GameObject AcquireInactive(GameObject prefab, Transform parent = null)
    {
        if (prefab == null)
            return null;

        var pool = GetPool(prefab);
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (pool[i] == null)
            {
                pool.RemoveAt(i);
                continue;
            }

            if (!pool[i].activeSelf)
            {
                var instance = pool[i];
                if (parent != null && instance.transform.parent != parent)
                    instance.transform.SetParent(parent, false);

                return instance;
            }
        }

        return CreateInstance(prefab, parent, pool);
    }

    // Releases an instance back to its pool.
    public void Despawn(GameObject instance)
    {
        if (instance != null)
            instance.SetActive(false);
    }

    private List<GameObject> GetPool(GameObject prefab)
    {
        if (!m_pools.TryGetValue(prefab, out var pool))
        {
            pool = new List<GameObject>();
            m_pools[prefab] = pool;
        }
        return pool;
    }

    private GameObject CreateInstance(GameObject prefab, Transform parent, List<GameObject> pool)
    {
        Transform targetParent = parent != null ? parent : transform;
        var instance = Instantiate(prefab, GetInactiveCreationRoot());
        instance.SetActive(false);
        instance.transform.SetParent(targetParent, false);

        pool.Add(instance);
        return instance;
    }

    private Transform GetInactiveCreationRoot()
    {
        if (m_inactiveCreationRoot != null)
            return m_inactiveCreationRoot;

        var root = new GameObject("Pool Inactive Creation Root");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        m_inactiveCreationRoot = root.transform;
        return m_inactiveCreationRoot;
    }
}
