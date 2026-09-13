using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public class WeaponAudioController : MonoBehaviour
{
    [SerializeField] private PlayerController m_player;

    private WeaponController m_weapon;
    private SoundManager m_reloadSoundManager;
    private int m_reloadSoundHandle;

    private void Awake()
    {
        if (m_player == null) m_player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (m_player == null) m_player = GetComponent<PlayerController>();
        if (m_player == null) return;
        m_player.WeaponChanged += OnWeaponChanged;
        // Attaching to an existing loadout is not a gameplay equip action.
        BindWeapon(m_player.ActiveWeapon);
    }

    private void OnDisable()
    {
        if (m_player != null) m_player.WeaponChanged -= OnWeaponChanged;
        BindWeapon(null);
    }

    private void LateUpdate()
    {
        if (m_reloadSoundHandle == 0) return;
        if (m_player == null || m_weapon == null || !m_weapon.isActiveAndEnabled ||
            !m_weapon.IsReloading || m_player.Stats == null || !m_player.Stats.IsAlive)
        {
            StopReloadSound();
            return;
        }

        if (m_reloadSoundManager != null)
            m_reloadSoundManager.SetGameplaySoundPaused(m_reloadSoundHandle, !m_player.CanAct);
    }

    private void OnWeaponChanged(WeaponController weapon)
    {
        if (m_weapon == weapon) return;
        BindWeapon(weapon);
        if (m_weapon == null || m_weapon.Data == null || m_player == null || !m_player.CanAct) return;

        SoundManager manager = SoundManager.Instance;
        if (manager != null)
            manager.PlayGameplaySound(m_weapon.Data.EquipSound, m_weapon.transform.position, 64,
                m_weapon.transform);
    }

    private void BindWeapon(WeaponController weapon)
    {
        StopReloadSound();
        if (m_weapon != null)
        {
            m_weapon.Fired -= OnFired;
            m_weapon.ReloadStateChanged -= OnReloadStateChanged;
        }

        m_weapon = weapon;
        if (m_weapon == null) return;
        m_weapon.Fired += OnFired;
        m_weapon.ReloadStateChanged += OnReloadStateChanged;
    }

    private void OnFired()
    {
        if (m_weapon == null || m_weapon.Data == null) return;
        SoundManager manager = SoundManager.Instance;
        if (manager != null)
        {
            // Fired is raised once per shot, including shotgun shots with multiple pellets.
            manager.PlayGameplaySound(m_weapon.Data.FireSound, m_weapon.transform.position, 24);
        }
    }

    private void OnReloadStateChanged(bool isReloading)
    {
        StopReloadSound();
        if (!isReloading || m_weapon == null || m_weapon.Data == null) return;

        m_reloadSoundManager = SoundManager.Instance;
        if (m_reloadSoundManager == null) return;
        m_reloadSoundHandle = m_reloadSoundManager.PlayGameplaySound(m_weapon.Data.ReloadSound,
            m_weapon.transform.position, 48, m_weapon.transform);
        m_reloadSoundManager.SetGameplaySoundPaused(m_reloadSoundHandle, m_player == null || !m_player.CanAct);
    }

    private void StopReloadSound()
    {
        if (m_reloadSoundHandle != 0 && m_reloadSoundManager != null)
            m_reloadSoundManager.StopGameplaySound(m_reloadSoundHandle);
        m_reloadSoundHandle = 0;
        m_reloadSoundManager = null;
    }
}
