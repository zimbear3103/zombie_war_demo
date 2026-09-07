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
        if (m_autoPlayToggle != null)
        {
            m_autoPlayToggle.isOn = GamePlayController.Instance.AutoPlay;
            m_autoPlayToggle.onValueChanged.AddListener((isOn) =>
            {
                GamePlayController.Instance.AutoPlay = isOn;
            });
        }
    }
    private void OnEnable()
    {
        m_settingButton.onClick.AddListener(OnSettingButtonPressed);
    }

    private void OnDisable()
    {
        m_settingButton.onClick.RemoveListener(OnSettingButtonPressed);
    }

    private void Update()
    {
        var controller = GamePlayController.Instance;
        if (controller == null || m_progressSlider == null)
            return;

        if (controller.GameState != m_lastGameState)
        {
            m_lastGameState = controller.GameState;

            if (m_lastGameState == GamePlayController.GameStateType.StartLevel)
                ResetProgress();
            else if (m_lastGameState == GamePlayController.GameStateType.Win)
                CompleteProgress();
        }

        var conductor = Conductor.Instance;
        if (conductor == null || !conductor.HasSong || conductor.ClipLength <= 0f)
            return;

        float progress = Mathf.Clamp01(conductor.SongPosition / conductor.ClipLength);
        m_progressSlider.value = progress;

        for (int i = 0; i < m_stars.Length; i++)
        {
            if (!m_starLit[i] && progress >= m_starThresholds[i])
            {
                m_starLit[i] = true;
                LightUp(m_stars[i]);
            }
        }
    }

    private void ResetProgress()
    {
        m_progressSlider.value = 0f;

        if (m_starThresholds == null || m_starThresholds.Length != m_stars.Length)
        {
            m_starThresholds = new float[m_stars.Length];
            m_starLit = new bool[m_stars.Length];
        }

        var sliderRect = (RectTransform)m_progressSlider.transform;
        for (int i = 0; i < m_stars.Length; i++)
        {
            float x = sliderRect.InverseTransformPoint(m_stars[i].rectTransform.position).x;
            m_starThresholds[i] = Mathf.InverseLerp(sliderRect.rect.xMin, sliderRect.rect.xMax, x);

            m_starLit[i] = false;
            m_stars[i].color = m_dimColor;
            m_stars[i].rectTransform.localScale = Vector3.one;
        }

        m_crownLit = false;
        if (m_crown != null)
        {
            m_crown.color = Color.white;
            m_crown.rectTransform.localScale = Vector3.one;
        }
    }

    private void CompleteProgress()
    {
        m_progressSlider.value = 1f;

        if (m_starLit != null)
        {
            for (int i = 0; i < m_stars.Length; i++)
            {
                if (!m_starLit[i])
                {
                    m_starLit[i] = true;
                    LightUp(m_stars[i]);
                }
            }
        }

        if (!m_crownLit && m_crown != null)
        {
            m_crownLit = true;
            LightUp(m_crown);
        }
    }

    private void LightUp(Image icon)
    {
        icon.color = m_litColor;
        StartCoroutine(Tweener.IE_LocalScale(
            icon.rectTransform, Vector3.one * m_popScale, Vector3.one, m_popTime, Tweener.Ease.OutBack));
    }


    private void OnSettingButtonPressed()
    {
        GamePlayController.Instance.EnterSettingPopupStatus();
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_Other);
    }
}
