using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponScriptableObject m_weaponData;
    [SerializeField] private Transform m_muzzle;
    [Tooltip("Prefab with BulletBehaviour on its root. Flight speed and lifetime are configured on that prefab.")]
    [SerializeField] private BulletBehaviour m_bulletPrefab;
    [SerializeField] private MuzzleEffect m_muzzleFlash;

    [Header("Hand Grips")]
    [Tooltip("Child transform defining the right hand bone's position and rotation while holding this weapon.")]
    [SerializeField] private Transform m_rightHandGrip;
    [Tooltip("Optional child transform for the left hand. Leave empty for a one-handed pose.")]
    [SerializeField] private Transform m_leftHandGrip;

    [Header("Bullet Pool")]
    [Tooltip("Distinct bullets created when the weapon is initialized, before firing. Clamped to Max Retained Bullets.")]
    [SerializeField, Min(0)] private int m_prewarmBulletCount = 16;
    [Tooltip("Maximum inactive bullets retained for reuse. Concurrent shots may exceed this; excess bullets are destroyed on return.")]
    [SerializeField, Min(1)] private int m_maxRetainedBullets = 128;

    private Transform m_origin;
    private PlayerStats m_owner;
    private int m_hitMask;
    private float m_fireInterval;
    private int m_maxMagazine;
    private int m_numberMagazine;
    private int m_magazineCapacity;
    private int m_ammoInMagazine;
    private float m_reloadTime;
    private float m_damage;
    private float m_range;
    private int m_pelletCount;
    private float m_spreadAngle;
    private float m_knockbackForce;
    private float m_nextShotTime = float.NegativeInfinity;
    private bool m_gameplayEnabled = true;
    private bool m_isConfigured;
    private bool m_reportedInvalidConfiguration;
    private bool m_isReloading;
    private Coroutine m_reloadCoroutine;
    private readonly List<BulletBehaviour> m_bullets = new List<BulletBehaviour>();
    private Action<RaycastHit> m_bulletHitCallback;
    private Action<ProjectileBehaviour> m_releaseBulletCallback;
    private ObjectPool<BulletBehaviour> m_bulletPool;
    private BulletBehaviour m_pooledBulletPrefab;
    private Transform m_inactiveCreationRoot;

    public WeaponScriptableObject Data => m_weaponData;
    public int MaxMagazine => m_maxMagazine;
    public int MagazineCapacity => m_magazineCapacity;
    public int AmmoInMagazine => m_ammoInMagazine;
    public bool IsReloading => m_isReloading;
    public float ReloadDuration => m_reloadTime;
    public Transform RightHandGrip => m_rightHandGrip;
    public Transform LeftHandGrip => m_leftHandGrip;
    public Vector3 HandLocalPosition => m_weaponData != null ? m_weaponData.HandLocalPosition : Vector3.zero;
    public Vector3 HandLocalEulerAngles => m_weaponData != null ? m_weaponData.HandLocalEulerAngles : Vector3.zero;

    public event Action Fired;
    public event Action<RaycastHit> Hit;
    public event Action<bool> ReloadStateChanged;

    public bool Initialize(WeaponScriptableObject data, Transform origin, PlayerStats owner, int hitMask)
    {
        m_weaponData = data;
        Configure(m_muzzle, origin, owner, hitMask);
        return m_isConfigured;
    }

    public void Configure(Transform muzzle, Transform origin, PlayerStats owner, int hitMask)
    {
        CancelReload();
        m_muzzle = muzzle;
        m_origin = origin;
        m_owner = owner;
        m_hitMask = hitMask;
        m_bulletHitCallback = OnBulletHit;
        m_releaseBulletCallback = ReleaseBullet;
        m_isConfigured = ValidateAndCacheConfiguration();

        if (m_isConfigured)
        {
            EnsureBulletPool();
            m_ammoInMagazine = m_magazineCapacity;
            m_maxMagazine = m_weaponData.MagazineMaximum;
            m_numberMagazine = m_weaponData.MagazineMaximum;
            m_reportedInvalidConfiguration = false;
        }
    }

    public bool TryFire(Vector3 direction, float now)
    {
        if (!m_gameplayEnabled || !isActiveAndEnabled || m_isReloading)
        {
            return false;
        }

        if (!m_isConfigured || m_weaponData == null || m_muzzle == null || m_origin == null ||
            m_owner == null || m_bulletPrefab == null)
        {
            ReportInvalidConfiguration("Weapon cannot fire because its data, muzzle, origin, owner or bullet prefab is missing.");
            return false;
        }

        if (!m_owner.isActiveAndEnabled || !m_owner.IsAlive)
        {
            return false;
        }

        if (!IsFinite(now) || !IsFinite(direction.x) || !IsFinite(direction.y) || !IsFinite(direction.z))
        {
            return false;
        }
        
        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
        if (flatDirection.sqrMagnitude <= Mathf.Epsilon || now < m_nextShotTime)
        {
            return false;
        }

        flatDirection.Normalize();
        m_nextShotTime = now + m_fireInterval;
        if (m_ammoInMagazine > 0)
        {
            m_ammoInMagazine--;
        }
        else
        {
            Debug.Log($"{name} cannot fire because the magazine is empty.");
            TryReload();
            return false;
        }
        Debug.Log($"{name} firing at {flatDirection} with {m_ammoInMagazine}/{m_magazineCapacity} rounds remaining. {m_numberMagazine} megazine remaining");
        FireBullets(flatDirection);
        Fired?.Invoke();
        return true;
    }

    public bool TryReload()
    {
        if (!m_gameplayEnabled || !isActiveAndEnabled)
        {
            return false;
        }
        if (!m_isConfigured || m_weaponData == null || m_muzzle == null || m_origin == null ||
            m_owner == null || m_bulletPrefab == null)
        {
            ReportInvalidConfiguration("Weapon cannot reload because its data, muzzle, origin, owner or bullet prefab is missing.");
            return false;
        }
        if (!m_owner.isActiveAndEnabled || !m_owner.IsAlive)
        {
            return false;
        }
        if (m_ammoInMagazine >= m_magazineCapacity)
        {
            Debug.Log($"{name} cannot reload because the magazine is already full.");
            return false;
        }
        if (m_numberMagazine <= 0)
        {
            Debug.Log($"{name} cannot reload because there are no magazines remaining.");
            return false;
        }
        if (m_isReloading)
        {
            Debug.Log($"{name} cannot reload because it is already reloading.");
            return false;
        }
        
        Debug.Log($"{name} starting reload with {m_numberMagazine} magazines remaining.");
        m_reloadCoroutine = StartCoroutine(Reload());
        SetReloading(true);
        return true;
    }

    private IEnumerator Reload()
    {
        float remainingTime = m_reloadTime;
        while (true)
        {
            // Yield first so the coroutine handle exists before any reload callbacks run.
            yield return null;
            if (!isActiveAndEnabled || m_owner == null || !m_owner.isActiveAndEnabled || !m_owner.IsAlive)
            {
                m_reloadCoroutine = null;
                SetReloading(false);
                yield break;
            }

            // Gameplay gating and scaled time both hold progress during a pause.
            if (!m_gameplayEnabled || Time.deltaTime <= 0f) continue;
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f) break;
        }

        m_numberMagazine = Mathf.Max(0, m_numberMagazine - 1);
        m_ammoInMagazine = m_magazineCapacity;
        Debug.Log($"{name} reloading to {m_ammoInMagazine}/{m_magazineCapacity} rounds.");
        m_reloadCoroutine = null;
        SetReloading(false);
    }

    private void SetReloading(bool value)
    {
        if (m_isReloading == value) return;
        m_isReloading = value;
        ReloadStateChanged?.Invoke(value);
    }

    private void CancelReload()
    {
        if (m_reloadCoroutine != null)
        {
            StopCoroutine(m_reloadCoroutine);
            m_reloadCoroutine = null;
        }
        SetReloading(false);
    }

    private void OnDisable()
    {
        CancelReload();
    }

    public void SetGameplayEnabled(bool value)
    {
        m_gameplayEnabled = value;
    }

    public void ResetWeapon()
    {
        CancelReload();
        m_nextShotTime = float.NegativeInfinity;
        m_numberMagazine = m_maxMagazine;
        m_ammoInMagazine = m_magazineCapacity;
        RecycleBullets();
    }

    private void RecycleBullets()
    {
        // Returning overflow bullets may remove them from the tracked collection.
        for (int index = m_bullets.Count - 1; index >= 0; index--)
        {
            var bullet = m_bullets[index];
            if (bullet != null) bullet.Recycle();
        }
    }

    private bool ValidateAndCacheConfiguration()
    {
        if (m_weaponData == null || m_muzzle == null || m_origin == null || m_owner == null ||
            m_hitMask == 0 || m_bulletPrefab == null || !m_bulletPrefab.HasValidFlightSettings)
        {
            ReportInvalidConfiguration("Weapon requires data, muzzle, origin, owner, a non-zero hit mask and a bullet prefab with positive speed/lifetime.");
            return false;
        }

        float fireInterval = m_weaponData.FireInterval;
        int magazineCapacity = m_weaponData.MagazineCapacity;
        float reloadTime = m_weaponData.ReloadTime;
        float damage = m_weaponData.Damage;
        float range = m_weaponData.Range;
        int pelletCount = m_weaponData.PelletCount;
        float spreadAngle = m_weaponData.SpreadAngle;
        float knockbackForce = m_weaponData.KnockbackForce;

        if (!IsFinite(fireInterval) || fireInterval <= 0f ||
            magazineCapacity < 1 ||
            !IsFinite(reloadTime) || reloadTime < 0f ||
            !IsFinite(damage) || damage < 0f ||
            !IsFinite(range) || range <= 0f ||
            pelletCount < 1 || pelletCount > 32 ||
            !IsFinite(spreadAngle) || spreadAngle < 0f || spreadAngle > 360f ||
            !IsFinite(knockbackForce) || knockbackForce < 0f)
        {
            ReportInvalidConfiguration("Weapon data contains an invalid fire interval, magazine capacity, reload time, damage, range, pellet count, spread or knockback value.");
            return false;
        }

        m_fireInterval = fireInterval;
        m_magazineCapacity = magazineCapacity;
        m_reloadTime = reloadTime;
        m_damage = damage;
        m_range = range;
        m_pelletCount = pelletCount;
        m_spreadAngle = spreadAngle;
        m_knockbackForce = knockbackForce;
        return true;
    }

    private void FireBullets(Vector3 centerDirection)
    {
        int bulletCount = m_weaponData.Kind == WeaponKind.Shotgun ? m_pelletCount : 1;
        float spreadStep = bulletCount > 1 ? m_spreadAngle / (bulletCount - 1) : 0f;
        float firstAngle = bulletCount > 1 ? -m_spreadAngle * 0.5f : 0f;
        for (int index = 0; index < bulletCount; index++)
        {
            float angle = firstAngle + spreadStep * index;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * centerDirection;
            //Debug.Log($"{name} firing bullet {index + 1}/{bulletCount} at angle {angle} degrees, direction {direction}");
            var bullet = AcquireBullet();
            m_muzzleFlash.PlayMuzzle();
            bullet.transform.SetPositionAndRotation(m_muzzle.position, Quaternion.LookRotation(direction, Vector3.up));
            bullet.Launch(direction, m_origin.position, m_owner, m_hitMask, m_range,
                m_damage, m_knockbackForce, m_bulletHitCallback);
        }
    }

    private BulletBehaviour AcquireBullet()
    {
        var bullet = m_bulletPool.Get();
        bullet.SetPoolReleaseCallback(m_releaseBulletCallback);
        return bullet;
    }

    private void EnsureBulletPool()
    {
        if (m_bulletPool != null && m_pooledBulletPrefab == m_bulletPrefab) return;
        DisposeBulletPool();
        m_pooledBulletPrefab = m_bulletPrefab;

        var creationRoot = new GameObject(name + " Bullet Pool Creation");
        creationRoot.transform.SetParent(transform, false);
        creationRoot.SetActive(false);
        m_inactiveCreationRoot = creationRoot.transform;

        int maxRetained = Mathf.Max(1, m_maxRetainedBullets);
        int prewarmCount = Mathf.Clamp(m_prewarmBulletCount, 0, maxRetained);
        m_bullets.Capacity = Mathf.Max(m_bullets.Capacity, prewarmCount);
        m_bulletPool = new ObjectPool<BulletBehaviour>(CreateBullet, null, DeactivateBullet, DestroyBullet,
            collectionCheck: Application.isEditor, defaultCapacity: Mathf.Max(1, prewarmCount), maxSize: maxRetained);

        // Hold all instances before releasing; alternating Get/Release only prewarms one object.
        var prewarmed = new BulletBehaviour[prewarmCount];
        for (int index = 0; index < prewarmed.Length; index++) prewarmed[index] = m_bulletPool.Get();
        foreach (var bullet in prewarmed) m_bulletPool.Release(bullet);
    }

    private BulletBehaviour CreateBullet()
    {
        // Configure inactive instances before any OnEnable callbacks can run.
        var instance = Instantiate(m_pooledBulletPrefab, m_inactiveCreationRoot);
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(null, false);
        instance.CachePoolReferences();
        m_bullets.Add(instance);
        return instance;
    }

    private void ReleaseBullet(ProjectileBehaviour projectile)
    {
        m_bulletPool.Release((BulletBehaviour)projectile);
    }

    private static void DeactivateBullet(BulletBehaviour bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    private void DestroyBullet(BulletBehaviour bullet)
    {
        m_bullets.Remove(bullet);
        if (bullet == null) return;
        if (Application.isPlaying) Destroy(bullet.gameObject);
        else DestroyImmediate(bullet.gameObject);
    }

    private void OnBulletHit(RaycastHit hit)
    {
        Hit?.Invoke(hit);
    }

    protected virtual void OnDestroy()
    {
        DisposeBulletPool();
    }

    private void DisposeBulletPool()
    {
        m_nextShotTime = float.NegativeInfinity;
        RecycleBullets();
        m_bulletPool?.Dispose();
        m_bulletPool = null;
        m_pooledBulletPrefab = null;
        m_bullets.Clear();
        if (m_inactiveCreationRoot == null) return;
        if (Application.isPlaying) Destroy(m_inactiveCreationRoot.gameObject);
        else DestroyImmediate(m_inactiveCreationRoot.gameObject);
        m_inactiveCreationRoot = null;
    }

    private void ReportInvalidConfiguration(string message)
    {
        if (m_reportedInvalidConfiguration)
        {
            return;
        }

        m_reportedInvalidConfiguration = true;
        Debug.LogError(message, this);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
