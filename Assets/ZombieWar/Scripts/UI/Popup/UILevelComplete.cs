using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILevelComplete : UIPopup
{
    [SerializeField] private Button m_leftButton;
    [SerializeField] private Button m_rightButton;
    [SerializeField] private TextMeshProUGUI m_titleText;
    [SerializeField] private TextMeshProUGUI m_descriptionText;
    [SerializeField] private TextMeshProUGUI m_scoreText;
    [SerializeField] private TextMeshProUGUI m_scoreBestText;
    [SerializeField] private Image[] m_groupStar;
    [Tooltip("Tint for the stars not earned this run")]
    [SerializeField] private Color m_starOffColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private void OnEnable()
    {
        m_leftButton.onClick.AddListener(OnLeftButtonPressed);
        m_rightButton.onClick.AddListener(OnRightButtonPressed);
    }

    public void OnDisable()
    {
        m_leftButton.onClick.RemoveListener(OnLeftButtonPressed);
        m_rightButton.onClick.RemoveListener(OnRightButtonPressed);
    }

    public override void Show()
    {
        base.Show();

        GamePlayController.RunResult result = GamePlayController.Instance.LastRunResult;

        SoundManager.Instance.OnPlayMusic(
            result.isWin ? ESoundId.Bg_Victory : ESoundId.Bg_Deafeat, isLoop: false, volume: 1f);

        if (m_titleText != null)
            m_titleText.text = result.isWin ? "WIN" : "GAME OVER";

        if (m_descriptionText != null)
            m_descriptionText.text = result.isWin ? "Awesome run!" : "Better luck next time!";

        if (m_scoreText != null)
            m_scoreText.text = $"{result.score}";

        if (m_scoreBestText != null)
            m_scoreBestText.text = result.isNewBest ? $"NEW BEST: {result.bestScore}" : $"BEST: {result.bestScore}";

        for (int i = 0; i < m_groupStar.Length; i++)
        {
            if (m_groupStar[i] != null)
                m_groupStar[i].color = i < result.stars ? Color.yellow : m_starOffColor;
        }
    }

    private void OnLeftButtonPressed()
    {
        UserProfile.Instance.SaveGameData();
        GamePlayController.Instance.QuitLevel();
        Hide();
    }

    private void OnRightButtonPressed()
    {
        UserProfile.Instance.SaveGameData();
        GamePlayController.Instance.RestartLevel();
        Hide();
    }
}
