using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utility;

public class UIInGame : UIScreen
{
    [Header("Top UI")]
    [SerializeField] private Button m_settingButton;
    [SerializeField] private TextMeshProUGUI m_timerText;

    [FormerlySerializedAs("m_statsGunText")]
    [SerializeField] private TextMeshProUGUI m_gunText;
    [SerializeField] private TextMeshProUGUI m_statsText;
    [SerializeField] private TextMeshProUGUI m_playerHealthText;
    [Header("Gameplay Actions")]
    [SerializeField] private Button m_bombButton;
    [SerializeField] private Button m_swapButton;
    [SerializeField] private Button m_reloadButton;

    private WeaponController m_displayedWeapon;
    private WeaponKind? m_displayedWeaponKind;
    private int m_displayedAmmoInMagazine;
    private int m_displayedTotalAmmo;
    private int m_displayedBombCount;
    private int m_displayedRemainingSeconds;
    private float m_displayedHealth;

    private void OnEnable()
    {
        if (m_settingButton != null)
            m_settingButton.onClick.AddListener(OnSettingButtonPressed);
        if (m_bombButton != null)
            m_bombButton.onClick.AddListener(OnBombButtonPressed);
        if (m_swapButton != null)
            m_swapButton.onClick.AddListener(OnSwapButtonPressed);
        if(m_reloadButton != null)
            m_reloadButton.onClick.AddListener(OnReloadButtonPressed);

        RefreshHud(true);
    }

    public override void Show()
    {
        base.Show();
        RefreshHud(true);
    }

    private void LateUpdate()
    {
        RefreshHud();
    }

    private void OnDisable()
    {
        if (m_settingButton != null)
            m_settingButton.onClick.RemoveListener(OnSettingButtonPressed);
        if (m_bombButton != null)
            m_bombButton.onClick.RemoveListener(OnBombButtonPressed);
        if (m_swapButton != null)
            m_swapButton.onClick.RemoveListener(OnSwapButtonPressed);
        if (m_reloadButton != null)
            m_reloadButton.onClick.RemoveListener(OnReloadButtonPressed);
    }

    private void RefreshHud(bool force = false)
    {
        GamePlayController gameController = GamePlayController.Instance;
        PlayerController player = gameController != null ? gameController.Player : null;
        WeaponController weapon = player != null ? player.ActiveWeapon : null;
        WeaponScriptableObject weaponData = weapon != null ? weapon.Data : null;
        WeaponKind? weaponKind = weaponData != null ? weaponData.Kind : (WeaponKind?)null;
        int ammoInMagazine = weapon != null ? weapon.AmmoInMagazine : 0;
        int totalAmmo = weapon != null ? weapon.TotalAmmo : 0;
        int bombCount = player != null ? player.BombCount : 0;
        PlayerStats playerStats = player != null ? player.Stats : null;
        float health = playerStats != null ? playerStats.CurrentHealth : 0f;
        int remainingSeconds = GetTimerDisplaySeconds(gameController != null ? gameController.RemainingTime : 0f);

        if (force || remainingSeconds != m_displayedRemainingSeconds)
        {
            if (m_timerText != null)
                m_timerText.text = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
        }

        if (force || weapon != m_displayedWeapon || weaponKind != m_displayedWeaponKind)
        {
            if (m_gunText != null)
                m_gunText.text = weaponKind.HasValue ? weaponKind.Value.ToString() : "-";
        }

        if (force || ammoInMagazine != m_displayedAmmoInMagazine ||
            totalAmmo != m_displayedTotalAmmo || bombCount != m_displayedBombCount)
        {
            if (m_statsText != null)
                m_statsText.text = $"{ammoInMagazine}/{totalAmmo} Bomb: {bombCount}";
        }

        if (force || health != m_displayedHealth)
        {
            if (m_playerHealthText != null)
                m_playerHealthText.text = $"Health: {health}";
        }

        m_displayedWeapon = weapon;
        m_displayedWeaponKind = weaponKind;
        m_displayedAmmoInMagazine = ammoInMagazine;
        m_displayedTotalAmmo = totalAmmo;
        m_displayedBombCount = bombCount;
        m_displayedRemainingSeconds = remainingSeconds;
        m_displayedHealth = health;
    }

    private static int GetTimerDisplaySeconds(float remainingTime)
    {
        if (float.IsNaN(remainingTime) || float.IsInfinity(remainingTime) || remainingTime <= 0f)
            return 0;

        return remainingTime >= int.MaxValue ? int.MaxValue : Mathf.CeilToInt(remainingTime);
    }

    private void OnSettingButtonPressed()
    {
        GamePlayController.Instance.EnterSettingPopupStatus();
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_Other);
    }

    private void OnBombButtonPressed()
    {
        PlayerController player = GetActivePlayer();
        if (player != null) player.OnThrowBomb();
    }
    private void OnReloadButtonPressed()
    {
        PlayerController player = GetActivePlayer();
        if (player != null) player.onReloadGun();
    }
    private void OnSwapButtonPressed()
    {
        PlayerController player = GetActivePlayer();
        if (player != null) player.SwitchWeapon();
    }

    private PlayerController GetActivePlayer()
    {
        GamePlayController gameController = GamePlayController.Instance;
        return gameController != null && gameController.IsGamePlaying ? gameController.Player : null;
    }
}
