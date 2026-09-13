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
    [Tooltip("Starting magazine equivalents, including the loaded magazine. Total starting ammo is this value multiplied by Magazine Capacity.")]
    [SerializeField, Min(1)] private int m_magazineMaximum = 3;
    [Tooltip("Rounds per magazine. A shotgun consumes one round per shot, regardless of pellet count.")]
    [SerializeField, Min(1)] private int m_magazineCapacity = 12;
    [SerializeField, Min(0f)] private float m_reloadTime = 2f;
    [SerializeField] private float m_damage = 10f;
    [SerializeField] private float m_range = 100f;
    [SerializeField] private int m_pelletCount = 6;
    [Tooltip("Total horizontal spread in degrees. Rifle randomizes each shot within half this angle on either side; shotgun distributes pellets across it. Pistol ignores it. Zero fires straight.")]
    [SerializeField] private float m_spreadAngle = 20f;
    [Tooltip("Initial zombie knockback speed in world units per second. Hits refresh velocity; shotgun pellets do not stack it.")]
    [SerializeField] private float m_knockbackForce;

    [Header("Hand Attachment")]
    [Tooltip("Weapon position relative to the fixed front WeaponSocket beneath the Animator.")]
    [SerializeField] private Vector3 m_handLocalPosition;
    [Tooltip("Weapon rotation relative to the fixed front WeaponSocket, in degrees.")]
    [SerializeField] private Vector3 m_handLocalEulerAngles;

    [Header("Audio")]
    [SerializeField] private GameplaySound m_fireSound = new GameplaySound();
    [Tooltip("Use a clip matching Reload Time. Playback stops when reload completes or is cancelled.")]
    [SerializeField] private GameplaySound m_reloadSound = new GameplaySound();
    [SerializeField] private GameplaySound m_equipSound = new GameplaySound();

    public GameObject WeaponPrefab => m_weaponPrefab;
    public WeaponKind Kind => m_kind;
    public float FireInterval => m_fireInterval;
    public int MagazineMaximum => m_magazineMaximum;
    public int MagazineCapacity => m_magazineCapacity;
    public float ReloadTime => m_reloadTime;
    public float Damage => m_damage;
    public float Range => m_range;
    public int PelletCount => m_pelletCount;
    public float SpreadAngle => m_spreadAngle;
    public float KnockbackForce => m_knockbackForce;
    public Vector3 HandLocalPosition => m_handLocalPosition;
    public Vector3 HandLocalEulerAngles => m_handLocalEulerAngles;
    public GameplaySound FireSound => m_fireSound;
    public GameplaySound ReloadSound => m_reloadSound;
    public GameplaySound EquipSound => m_equipSound;

    private void OnValidate()
    {
        m_fireInterval = ClampFinite(m_fireInterval, 0.01f, float.MaxValue, 0.5f);
        m_magazineMaximum = Mathf.Max(1, m_magazineMaximum);
        m_magazineCapacity = Mathf.Max(1, m_magazineCapacity);
        m_reloadTime = ClampFinite(m_reloadTime, 0f, float.MaxValue, 2f);
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
