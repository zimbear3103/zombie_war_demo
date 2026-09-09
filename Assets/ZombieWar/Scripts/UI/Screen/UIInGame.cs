using UnityEngine;
using UnityEngine.UI;
using Utility;

public class UIInGame : UIScreen
{
    [Header("Top UI")]
    [SerializeField] private Button m_settingButton;

    [Header("Song Progress")]
    [Tooltip("Display-only slider showing how far the song has played")]
    [SerializeField] private Slider m_progressSlider;
    [Tooltip("Star images along the bar. Each lights up when the slider passes its position")]
    [SerializeField] private Image[] m_stars;
    [SerializeField] private Image m_crown;

    [Header("Progress Style")]
    [SerializeField] private Color m_dimColor = new Color(0.5f, 0.5f, 0.55f, 0.9f);
    [SerializeField] private Color m_litColor = new Color(1f, 0.85f, 0.2f);
    [Tooltip("Scale a star pops to when it lights up, settling back to 1")]
    [SerializeField] private float m_popScale = 1.4f;
    [SerializeField] private float m_popTime = 0.25f;

    [SerializeField] private Toggle m_autoPlayToggle;

    private float[] m_starThresholds;
    private bool[] m_starLit;
    private bool m_crownLit;

    private GamePlayController.GameStateType m_lastGameState = GamePlayController.GameStateType.None;

    private void Awake()
    {
        if (m_progressSlider != null)
        {
            m_progressSlider.interactable = false;
            m_progressSlider.minValue = 0f;
            m_progressSlider.maxValue = 1f;
        }
    }

    private void Start()
    {
    }
    private void OnEnable()
    {
        if (m_settingButton != null)
            m_settingButton.onClick.AddListener(OnSettingButtonPressed);
    }

    private void OnDisable()
    {
        if (m_settingButton != null)
            m_settingButton.onClick.RemoveListener(OnSettingButtonPressed);
    }

    private void OnSettingButtonPressed()
    {
        GamePlayController.Instance.EnterSettingPopupStatus();
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_Other);
    }
}
