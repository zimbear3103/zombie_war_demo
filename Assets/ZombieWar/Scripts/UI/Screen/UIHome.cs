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


    public override void Show()
    {
        base.Show();
        OnSetCoins(UserProfile.Instance.Coin);
        OnSetStars(UserProfile.Instance.TotalStars);
        SoundManager.Instance.OnPlayMusic(ESoundId.Bg_MainMenu, isLoop: true, 1f);
    }


    public void OnSetCoins(int coins)
    {
        m_coinsText.text = coins.ToString();
    }

    private void OnSetStars(int totalStars)
    {
        if (m_starText != null)
            m_starText.text = totalStars.ToString();
    }

    private void OnSettingButtonPressed()
    {
        UIManager.Instance.ShowPopup(PopupType.Setting);
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);
    }
}
