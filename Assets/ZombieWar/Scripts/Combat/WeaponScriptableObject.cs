using UnityEngine;
using UnityEngine.Serialization;

public enum WeaponKind
{
    Pistol,
    Rifle,
    Shotgun
}

[CreateAssetMenu(fileName = "WeaponScriptableObject", menuName = "ScriptableObjects/Weapon")]
public class WeaponScriptableObject : ScriptableObject
{
    [SerializeField] private GameObject m_weaponPrefab;
    [SerializeField] private WeaponKind m_kind;
    [FormerlySerializedAs("m_fireRate")]
    [SerializeField] private float m_fireInterval = 0.5f;
    [Tooltip("Rounds per magazine. A shotgun consumes one round per shot, regardless of pellet count.")]
    [SerializeField, Min(1)] private int m_magazineMaximum = 3;
    [SerializeField, Min(1)] private int m_magazineCapacity = 12;
    [SerializeField] private float m_reloadTime = 2f;
    [SerializeField] private float m_damage = 10f;
    [SerializeField] private float m_range = 100f;
    [SerializeField] private int m_pelletCount = 6;
    [SerializeField] private float m_spreadAngle = 20f;
    [Tooltip("Initial zombie knockback speed in world units per second. Hits refresh velocity; shotgun pellets do not stack it.")]
    [SerializeField] private float m_knockbackForce;

    public GameObject WeaponPrefab => m_weaponPrefab;
    public WeaponKind Kind => m_kind;
    public float FireInterval => m_fireInterval;
    public int MagazineMaximum => m_magazineMaximum;
    public int MagazineCapacity => m_magazineCapacity;
    public float Damage => m_damage;
    public float Range => m_range;
    public int PelletCount => m_pelletCount;
    public float SpreadAngle => m_spreadAngle;
    public float KnockbackForce => m_knockbackForce;

    private void OnValidate()
    {
        m_fireInterval = ClampFinite(m_fireInterval, 0.01f, float.MaxValue, 0.5f);
        m_magazineCapacity = Mathf.Max(1, m_magazineCapacity);
        m_damage = ClampFinite(m_damage, 0f, float.MaxValue, 10f);
        m_range = ClampFinite(m_range, 0.01f, float.MaxValue, 100f);
        m_pelletCount = Mathf.Clamp(m_pelletCount, 1, 32);
        m_spreadAngle = ClampFinite(m_spreadAngle, 0f, 360f, 20f);
        m_knockbackForce = ClampFinite(m_knockbackForce, 0f, float.MaxValue, 0f);
    }

    private static float ClampFinite(float value, float minimum, float maximum, float fallback)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? fallback
            : Mathf.Clamp(value, minimum, maximum);
    }
}
