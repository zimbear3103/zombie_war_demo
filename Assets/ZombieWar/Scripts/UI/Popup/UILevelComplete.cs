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
