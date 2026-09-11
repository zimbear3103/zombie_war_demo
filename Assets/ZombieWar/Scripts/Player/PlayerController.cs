using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform m_facingRoot;
    [SerializeField, Range(0f, 1f)] private float m_aimDeadZone = 0.25f;

    [Header("Loadout")]
    [SerializeField] private WeaponScriptableObject m_startingWeapon;
    [SerializeField] private Transform m_weaponSocket;
    [SerializeField] private Transform m_obstructionOrigin;
    [SerializeField] private LayerMask m_hitMask = Physics.DefaultRaycastLayers;

    [Header("Bomb")]
    [Tooltip("Optional hand socket. Falls back to one world unit above the player.")]
    [SerializeField] private Transform m_bombThrowOrigin;
    [SerializeField, Min(1)] private int m_maxBombCount = 3;
        
    private readonly List<WeaponController> m_ownedWeapons = new List<WeaponController>(3);
    private readonly List<BombController> m_bombs = new List<BombController>(3);
    private InputAction m_bombAction;
    private int m_lastBombThrowFrame = -1;
    private PlayerStats m_stats;
    private WeaponController m_activeWeapon;
    private Vector2 m_moveInput;
    private Vector2 m_aimInput;
    private Vector3 m_lastFacing = Vector3.forward;
    private bool m_gameplayEnabled;
    private bool m_hasFocus = true;
    private bool m_applicationPaused;
    private bool m_reportedLoadoutError;

    public PlayerStats Stats => m_stats != null ? m_stats : (m_stats = GetComponent<PlayerStats>());
    public WeaponController ActiveWeapon => m_activeWeapon;
    public int BombCount => m_bombs.Count;
    public bool CanAct => m_gameplayEnabled && m_hasFocus && !m_applicationPaused && isActiveAndEnabled &&
                          Stats != null && Stats.isActiveAndEnabled && Stats.IsAlive;
    public float NormalizedMoveSpeed { get; private set; }
    public event Action<WeaponController> WeaponChanged;
    public event Action RunReset;

    private void Awake()
    {
        m_stats = GetComponent<PlayerStats>();
        if (m_stats == null)
        {
            Debug.LogError("PlayerController requires PlayerStats on the same GameObject.", this);
            enabled = false;
            return;
        }
        m_stats.SetDamageEnabled(false);
        m_lastFacing = Facing(Vector2.zero, Vector2.zero, transform.forward, m_aimDeadZone);
    }

    private void OnEnable()
    {
        if (Stats != null) Stats.Died += OnDied;
        PlayerInput playerInput = GetComponent<PlayerInput>();
        m_bombAction = playerInput != null && playerInput.actions != null
            ? playerInput.actions.FindAction("Player/Bomb", false) : null;
        if (m_bombAction != null) m_bombAction.performed += OnBombPerformed;
        ClearInput();
    }

    private void OnDisable()
    {
        if (m_stats != null) m_stats.Died -= OnDied;
        if (m_bombAction != null) m_bombAction.performed -= OnBombPerformed;
        m_bombAction = null;
        SetGameplayEnabled(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        m_hasFocus = hasFocus;
        ClearInput();
        if (m_activeWeapon != null) m_activeWeapon.SetGameplayEnabled(CanAct);
    }

    private void OnApplicationPause(bool paused)
    {
        m_applicationPaused = paused;
        ClearInput();
        if (m_activeWeapon != null) m_activeWeapon.SetGameplayEnabled(CanAct);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        m_moveInput = CanAct ? SanitizeInput(context.ReadValue<Vector2>()) : Vector2.zero;
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        m_aimInput = CanAct ? SanitizeInput(context.ReadValue<Vector2>()) : Vector2.zero;
    }

    public void OnSwitchWeapon(InputAction.CallbackContext context)
    {
        if (context.performed) SwitchWeapon();
    }

    private void Update()
    {
        if (!CanAct || Time.deltaTime <= 0f)
        {
            NormalizedMoveSpeed = 0f;
            return;
        }

        // Movement and shooting run independently during the same frame.
        onPlayerMove();
        UpdateFacing();
        UpdateShooting();
    }

    public void onPlayerMove()
    {
        if (!CanAct || Time.deltaTime <= 0f)
        {
            NormalizedMoveSpeed = 0f;
            return;
        }

        Vector3 movement = Move(m_moveInput);
        transform.Translate(movement * m_stats.MoveSpeed * Time.deltaTime, Space.World);
        NormalizedMoveSpeed = m_stats.MoveSpeed > 0f ? movement.magnitude : 0f;
    }

    private void UpdateFacing()
    {
        m_lastFacing = Facing(Vector2.zero, m_aimInput, m_lastFacing, m_aimDeadZone);
        Transform facingRoot = m_facingRoot != null ? m_facingRoot : transform;
        facingRoot.rotation = Quaternion.LookRotation(m_lastFacing, Vector3.up);
    }

    private void UpdateShooting()
    {
        if (m_activeWeapon == null) return;
        if (!IsAiming(m_aimInput, m_aimDeadZone)) return;

        m_activeWeapon.TryFire(m_lastFacing, Time.time);
    }

    public void SetGameplayEnabled(bool value)
    {
        m_gameplayEnabled = value;
        ClearInput();
        if (Stats != null) Stats.SetDamageEnabled(value);
        if (m_activeWeapon != null) m_activeWeapon.SetGameplayEnabled(CanAct);
    }

    public void ResetForRun(Vector3 position, Quaternion rotation)
    {
        SetGameplayEnabled(false);
        if (Stats == null) return;
        transform.SetPositionAndRotation(position, rotation);
        m_lastFacing = Facing(Vector2.zero, Vector2.zero, rotation * Vector3.forward, m_aimDeadZone);
        if (m_facingRoot != null) m_facingRoot.rotation = Quaternion.LookRotation(m_lastFacing, Vector3.up);
        Stats.RestoreFullHealth();
        ClearLoadout();
        m_bombs.Clear();
        m_lastBombThrowFrame = -1;
        RunReset?.Invoke();
        m_reportedLoadoutError = false;
        if (m_startingWeapon != null) AddOrEquipWeapon(m_startingWeapon);
        // The session opens gameplay only after both player and spawner are ready.
    }

    public bool TryCollectWeapon(WeaponScriptableObject data)
    {
        return CanAct && data != null && AddOrEquipWeapon(data);
    }

    public bool TryCollectBomb(BombController data)
    {
        if (!CanAct || data == null || m_bombs.Count >= Mathf.Max(1, m_maxBombCount)) return false;
        m_bombs.Add(data);
        return true;
    }

    private bool AddOrEquipWeapon(WeaponScriptableObject data)
    {
        foreach (var owned in m_ownedWeapons)
        {
            if (owned != null && owned.Data != null && owned.Data.Kind == data.Kind)
            {
                Equip(owned);
                return true;
            }
        }
        if (m_weaponSocket == null || m_obstructionOrigin == null || data.WeaponPrefab == null ||
            data.WeaponPrefab.GetComponent<WeaponController>() == null)
        {
            if (!m_reportedLoadoutError)
                Debug.LogError("Equipping a weapon requires a socket, obstruction origin and a prefab with WeaponController on its root.", this);
            m_reportedLoadoutError = true;
            return false;
        }

        var instance = Instantiate(data.WeaponPrefab, m_weaponSocket);
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        var weapon = instance.GetComponent<WeaponController>();
        if (!weapon.Initialize(data, m_obstructionOrigin, Stats, m_hitMask.value))
        {
            instance.SetActive(false);
            Destroy(instance);
            return false;
        }
        m_ownedWeapons.Add(weapon);
        Equip(weapon);
        return true;
    }

    private void Equip(WeaponController weapon)
    {
        if (m_activeWeapon == weapon) return;
        if (m_activeWeapon != null)
        {
            m_activeWeapon.SetGameplayEnabled(false);
            m_activeWeapon.gameObject.SetActive(false);
        }
        m_activeWeapon = weapon;
        m_activeWeapon.gameObject.SetActive(true);
        m_activeWeapon.SetGameplayEnabled(CanAct);
        WeaponChanged?.Invoke(m_activeWeapon);
    }

    public void SwitchWeapon()
    {
        if (!CanAct || m_ownedWeapons.Count < 2) return;
        int index = m_ownedWeapons.IndexOf(m_activeWeapon);
        for (int offset = 1; offset <= m_ownedWeapons.Count; offset++)
        {
            var next = m_ownedWeapons[(index + offset) % m_ownedWeapons.Count];
            if (next == null) continue;
            Equip(next);
            return;
        }
    }

    private void ClearLoadout()
    {
        foreach (var weapon in m_ownedWeapons)
        {
            if (weapon == null) continue;
            weapon.SetGameplayEnabled(false);
            weapon.gameObject.SetActive(false);
            Destroy(weapon.gameObject);
        }
        m_ownedWeapons.Clear();
        m_activeWeapon = null;
        WeaponChanged?.Invoke(null);
    }

    private void OnDestroy() => ClearLoadout();

    private void OnDied()
    {
        ClearInput();
        if (m_activeWeapon != null) m_activeWeapon.SetGameplayEnabled(false);
    }

    private void ClearInput()
    {
        m_moveInput = Vector2.zero;
        m_aimInput = Vector2.zero;
        NormalizedMoveSpeed = 0f;
    }

    // These rules also define the public movement/aim conversion contract.
    public static Vector3 Move(Vector2 input)
    {
        input = SanitizeInput(input);
        return new Vector3(input.x, 0f, input.y);
    }

    public static bool IsAiming(Vector2 aim, float deadZone)
    {
        return SanitizeInput(aim).sqrMagnitude > deadZone * deadZone;
    }

    public static Vector3 Facing(Vector2 move, Vector2 aim, Vector3 previous, float deadZone)
    {
        // Keep the existing signature; movement no longer controls facing.
        if (IsAiming(aim, deadZone)) return Move(aim).normalized;
        previous.y = 0f;
        return previous.sqrMagnitude > 0.0001f ? previous.normalized : Vector3.forward;
    }

    private static Vector2 SanitizeInput(Vector2 input)
    {
        if (float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsInfinity(input.x) || float.IsInfinity(input.y))
            return Vector2.zero;
        return Vector2.ClampMagnitude(input, 1f);
    }

    public void onReloadGun()
    {
        if (!CanAct) return;

        if (m_activeWeapon != null)
        {
            m_activeWeapon.TryReload();
        }
    }

    public void OnThrowBomb()
    {
        if (!CanAct || Time.timeScale <= 0f || m_lastBombThrowFrame == Time.frameCount) return;
        StartThrowBomb();
    }

    private void StartThrowBomb()
    {
        while (m_bombs.Count > 0 && m_bombs[0] == null) m_bombs.RemoveAt(0);
        if (m_bombs.Count == 0) return;

        Vector3 direction = Facing(Vector2.zero, m_aimInput, m_lastFacing, m_aimDeadZone);
        Vector3 origin = m_bombThrowOrigin != null
            ? m_bombThrowOrigin.position : transform.position + Vector3.up;
        BombController bomb = Instantiate(m_bombs[0], origin, Quaternion.LookRotation(direction));
        if (!bomb.Throw(this, origin, direction))
        {
            Destroy(bomb.gameObject);
            return;
        }

        m_bombs.RemoveAt(0);
        m_lastBombThrowFrame = Time.frameCount;
    }

    private void OnBombPerformed(InputAction.CallbackContext context) => OnThrowBomb();
}
