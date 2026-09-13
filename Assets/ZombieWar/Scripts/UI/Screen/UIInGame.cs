using UnityEngine;
using UnityEngine.UI;
using Utility;

public class UIInGame : UIScreen
{
    [Header("Top UI")]
    [SerializeField] private Button m_settingButton;

    [Header("Gameplay Actions")]
    [SerializeField] private Button m_bombButton;
    [SerializeField] private Button m_swapButton;
    [SerializeField] private Button m_reloadButton;
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
