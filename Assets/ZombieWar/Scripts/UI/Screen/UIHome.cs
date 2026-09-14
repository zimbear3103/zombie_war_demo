using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHome : UIScreen
{
    [Header("Top UI")]
    [SerializeField] private Button m_settingButton;
    [SerializeField] private Button m_playButton;
    [SerializeField] private TextMeshProUGUI m_statusText;

    [Header("Level Selection")]
    [SerializeField] private Button m_previousLevelButton;
    [SerializeField] private Button m_nextLevelButton;
    [SerializeField] private TextMeshProUGUI m_levelNameText;
    private bool m_isPreparing;
    private void OnEnable()
    {
        if (m_playButton != null)
            m_playButton.onClick.AddListener(OnStartGame);
        if (m_settingButton != null)
            m_settingButton.onClick.AddListener(OnSettingButtonPressed);
        if (m_previousLevelButton != null)
            m_previousLevelButton.onClick.AddListener(OnPreviousLevel);
        if (m_nextLevelButton != null)
            m_nextLevelButton.onClick.AddListener(OnNextLevel);
    }

    private void OnDisable()
    {
        if (m_playButton != null)
            m_playButton.onClick.RemoveListener(OnStartGame);
        if (m_settingButton != null)
            m_settingButton.onClick.RemoveListener(OnSettingButtonPressed);
        if (m_previousLevelButton != null)
            m_previousLevelButton.onClick.RemoveListener(OnPreviousLevel);
        if (m_nextLevelButton != null)
            m_nextLevelButton.onClick.RemoveListener(OnNextLevel);
    }

    private void Start()
    {
        SelectLevel(0);
    }
    public override void Show()
    {
        base.Show();
        SetLoading(false);
        MainStateManager stateManager = MainStateManager.Instance;
        if (stateManager != null && !string.IsNullOrEmpty(stateManager.LastStartError))
            ShowStatus(stateManager.LastStartError);
        if (SoundManager.Instance != null)
            SoundManager.Instance.OnPlayMusic(ESoundId.Bg_MainMenu, isLoop: true, 1f);
    }

    private void OnStartGame()
    {
        MainStateManager stateManager = MainStateManager.Instance;
        if (stateManager == null || stateManager.IsTransitioning || m_isPreparing)
            return;

        SetLoading(true);
        if (!stateManager.TryStartGameplay(OnGameplayPrepared))
        {
            SetLoading(false);
            return;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);
    }

    private void OnGameplayPrepared(bool succeeded)
    {
        if (this == null || succeeded)
            return;

        SetLoading(false);
        MainStateManager manager = MainStateManager.Instance;
        ShowStatus(manager != null ? manager.LastStartError : "Could not prepare the level. Please try again.");
    }

    private void SetLoading(bool isLoading)
    {
        m_isPreparing = isLoading;
        if (m_settingButton != null)
            m_settingButton.interactable = !isLoading;
        RefreshLevelSelection();
        if (isLoading)
            ShowStatus("Preparing level...");
    }

    private void OnPreviousLevel()
    {
        SelectLevel(-1);
    }

    private void OnNextLevel()
    {
        SelectLevel(1);
    }

    private void SelectLevel(int offset)
    {
        if (m_isPreparing || LevelManager.Instance == null || LevelManager.Instance.LevelCount == 0)
            return;

        int count = LevelManager.Instance.LevelCount;
        int index = (LevelManager.Instance.SelectedLevelIndex + offset + count) % count;
        if (LevelManager.Instance.TrySelectLevel(index))
            RefreshLevelSelection();
    }

    private void RefreshLevelSelection()
    {
        LevelScriptableObject level = LevelManager.Instance != null ? LevelManager.Instance.SelectedLevel : null;
        bool canSelect = !m_isPreparing && LevelManager.Instance != null &&
                         LevelManager.Instance.isActiveAndEnabled && LevelManager.Instance.CurrentMap == null;
        bool hasLevel = level != null;
        if (m_levelNameText != null)
            m_levelNameText.text = hasLevel
                ? $"{LevelManager.Instance.SelectedLevelIndex + 1}/{LevelManager.Instance.LevelCount} - {level.LevelName}"
                : "No level selected";
        if (m_playButton != null)
            m_playButton.interactable = canSelect && hasLevel;
        if (m_previousLevelButton != null)
            m_previousLevelButton.interactable = canSelect && LevelManager.Instance.LevelCount > 1;
        if (m_nextLevelButton != null)
            m_nextLevelButton.interactable = canSelect && LevelManager.Instance.LevelCount > 1;
        ShowStatus(hasLevel ? string.Empty : "Assign a LevelManager with level assets on UIHome.");
    }

    private void ShowStatus(string message)
    {
        if (m_statusText != null)
            m_statusText.text = message ?? string.Empty;
        else if (!string.IsNullOrEmpty(message) && message != "Preparing level...")
            Debug.LogWarning($"[UIHome] {message}", this);
    }

    private void OnSettingButtonPressed()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowPopup(PopupType.Setting);
        if (SoundManager.Instance != null)
            SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);
    }
}
