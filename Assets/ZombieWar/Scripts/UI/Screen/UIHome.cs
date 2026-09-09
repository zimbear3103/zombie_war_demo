using System.Collections.Generic;
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


    public override void Show()
    {
        base.Show();
        SoundManager.Instance.OnPlayMusic(ESoundId.Bg_MainMenu, isLoop: true, 1f);
    }

    private void OnStartGame()
    {
        UIManager.Instance.ShowScreen(ScreenType.Gameplay);
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);

        // add start game
    }

    private void OnSettingButtonPressed()
    {
        UIManager.Instance.ShowPopup(PopupType.Setting);
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);
    }
}
