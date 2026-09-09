using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHome : UIScreen
{
    [Header("Top UI")]
    [SerializeField] private TextMeshProUGUI m_levelText;
    [SerializeField] private TextMeshProUGUI m_coinsText;
    [SerializeField] private TextMeshProUGUI m_starText;
    [SerializeField] private TextMeshProUGUI m_favoriteText;
    [SerializeField] private Button m_settingButton;
    [SerializeField] private Button m_playButton;
    [SerializeField] private TextMeshProUGUI m_statusText;

    private void OnEnable()
    {
        if (m_playButton != null)
            m_playButton.onClick.AddListener(OnStartGame);
        if (m_settingButton != null)
            m_settingButton.onClick.AddListener(OnSettingButtonPressed);
    }

    private void OnDisable()
    {
        if (m_playButton != null)
            m_playButton.onClick.RemoveListener(OnStartGame);
        if (m_settingButton != null)
            m_settingButton.onClick.RemoveListener(OnSettingButtonPressed);
    }


    public override void Show()
    {
        base.Show();
        SetLoading(false);
        if (SoundManager.Instance != null)
            SoundManager.Instance.OnPlayMusic(ESoundId.Bg_MainMenu, isLoop: true, 1f);
    }
    private void OnStartGame()
    {
        MainStateManager stateManager = MainStateManager.Instance;
        if (stateManager == null || stateManager.IsTransitioning)
            return;

        SetLoading(true);
        if (!stateManager.TryStartGameplay(OnGameplayLoaded))
        {
            SetLoading(false);
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);
    }

    private void OnGameplayLoaded(bool succeeded)
    {
        // A successful single-scene load destroys this UIHome instance.
        if (this == null || succeeded)
            return;

        SetLoading(false);
        if (m_statusText != null)
            m_statusText.text = "Could not load the game. Please try again.";
    }

    private void SetLoading(bool isLoading)
    {
        if (m_playButton != null)
            m_playButton.interactable = !isLoading;
        if (m_settingButton != null)
            m_settingButton.interactable = !isLoading;
        if (m_statusText != null)
            m_statusText.text = isLoading ? "Loading..." : string.Empty;
    }

    private void OnSettingButtonPressed()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPopup(PopupType.Setting);
        if (SoundManager.Instance != null)
            SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);
    }
}
