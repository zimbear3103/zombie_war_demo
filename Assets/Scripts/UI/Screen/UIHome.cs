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
    [SerializeField] private Transform m_songBarHolder;
    [SerializeField] private GameObject m_songBarPrefab;

    private readonly List<SongBar> m_songBars = new();
    private readonly List<string> m_songNames = new();

    public override void Show()
    {
        base.Show();
        OnSetCoins(UserProfile.Instance.Coin);
        OnSetStars(UserProfile.Instance.TotalStars);
        SoundManager.Instance.OnPlayMusic(ESoundId.Bg_MainMenu, isLoop: true, 1f);
        BuildSongList();
        RefreshFavoriteCount();
    }
    private void BuildSongList()
    {
        var loader = BeatMapLoader.Instance;
        if (loader == null || m_songBarPrefab == null || m_songBarHolder == null)
        {
            GameLog.Log(LogType.Error, "[UIHome] Song list refs missing — assign song bar prefab + holder, and put BeatMapLoader in the System prefab");
            return;
        }

        m_songNames.Clear();
        for (int i = 0; i < loader.SongCount; i++)
        {
            SongInfo info = loader.GetSongInfo(i);
            if (info == null)
                continue;

            m_songNames.Add(info.songName);

            SongBar bar;
            if (i < m_songBars.Count)
            {
                bar = m_songBars[i];
            }
            else
            {
                GameObject barObj = Instantiate(m_songBarPrefab, m_songBarHolder);
                bar = barObj.GetComponent<SongBar>();
                if (bar == null)
                {
                    GameLog.Log(LogType.Error, "[UIHome] Song bar prefab has no SongBar component");
                    Destroy(barObj);
                    return;
                }
                m_songBars.Add(bar);
            }

            bar.gameObject.SetActive(true);
            bar.Init(info, OnSongPlayPressed);
        }
    }

    // Stores the picked song — OnSetupGameLevel feeds it into
    // BeatMapLoader.LoadSong when the Gameplay state starts.
    private void OnSongPlayPressed(int songIndex)
    {
        GamePlayController.Instance.SelectedSongIndex = songIndex;
        MainStateManager.Instance.SetStatus(MainStateManager.MainStatusType.Exit, MainStateManager.MainStateType.Gameplay);
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

    // Counts favorites over the songs actually shown in the list — no separate
    // saved counter that could drift from the per-song flags
    private void RefreshFavoriteCount()
    {
        if (m_favoriteText == null || UserProfile.Instance == null)
            return;

        int count = 0;
        for (int i = 0; i < m_songNames.Count; i++)
        {
            if (UserProfile.Instance.IsFavorite(m_songNames[i]))
                count++;
        }
        m_favoriteText.text = count.ToString();
    }

    private void OnSettingButtonPressed()
    {
        UIManager.Instance.ShowPopup(PopupType.Setting);
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);
    }
}
