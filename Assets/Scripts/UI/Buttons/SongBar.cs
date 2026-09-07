using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongBar : MonoBehaviour
{
    [SerializeField] private Image m_leftImg;
    [SerializeField] private TextMeshProUGUI m_songNameTxt;
    [SerializeField] private TextMeshProUGUI m_songAuthorTxt;
    [SerializeField] private Button m_playBtn;
    [SerializeField] private Button m_favoriteBtn;
    [SerializeField] private Image m_favoriteImg;
    [Tooltip("[0] = not favorite, [1] = favorite")]
    [SerializeField] private Sprite[] m_favoriteSprite;
    [SerializeField] private Image m_previewImg;
    [Tooltip("[0] = not preview, [1] = preview")]
    [SerializeField] private Sprite[] m_previewSprite;
    [SerializeField] private Button m_previewBtn;
    [SerializeField] private Image[] m_stars;
    [Tooltip("Tint for star slots not yet earned on this song")]
    [SerializeField] private Color m_starOffColor = new Color(0.35f, 0.35f, 0.35f);

    private SongInfo m_info;
    private Action<int> m_onPlay;
    private bool m_isFavorite;

    private static SongBar s_previewingBar;

    private void OnEnable()
    {
        m_playBtn.onClick.AddListener(OnPlayBtnClicked);
        m_favoriteBtn.onClick.AddListener(OnFavoriteBtnClicked);
        m_previewBtn.onClick.AddListener(OnPreviewBtnClicked);
    }

    private void OnDisable()
    {
        m_playBtn.onClick.RemoveListener(OnPlayBtnClicked);
        m_favoriteBtn.onClick.RemoveListener(OnFavoriteBtnClicked);
        m_previewBtn.onClick.RemoveListener(OnPreviewBtnClicked);

        if (s_previewingBar == this)
            s_previewingBar = null;
    }

    public void Init(SongInfo info, Action<int> onPlay)
    {
        m_info = info;
        m_onPlay = onPlay;

        m_songNameTxt.text = info.songName;
        m_songAuthorTxt.text = info.songAuthor;
        if (info.previewSprite != null)
            m_leftImg.sprite = info.previewSprite;

        m_isFavorite = UserProfile.Instance.IsFavorite(info.songName);
        UpdateFavoriteIcon();
        UpdatePreviewIcon();
        UpdateStars();
    }

    // Lights up the best star record earned on this song (re-Init on every
    // Home visit keeps it fresh after a run)
    private void UpdateStars()
    {
        if (m_stars == null || UserProfile.Instance == null)
            return;

        int earned = UserProfile.Instance.GetSongStars(m_info.songName);
        for (int i = 0; i < m_stars.Length; i++)
        {
            if (m_stars[i] != null)
                m_stars[i].color = i < earned ? Color.yellow : m_starOffColor;
        }
    }

    private void OnPlayBtnClicked()
    {
        if (m_info == null)
            return;

        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonMain);
        StopPreview();  
        m_onPlay.Invoke(m_info.index);
    }

    private void OnFavoriteBtnClicked()
    {
        if (m_info == null)
            return;

        m_isFavorite = !m_isFavorite;
        UserProfile.Instance.SetFavorite(m_info.songName, m_isFavorite);
        UpdateFavoriteIcon();
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_Other);
    }

    private void UpdateFavoriteIcon()
    {
        if (m_favoriteImg != null && m_favoriteSprite != null && m_favoriteSprite.Length >= 2)
            m_favoriteImg.sprite = m_favoriteSprite[m_isFavorite ? 1 : 0];
    }

    // Toggle: press to preview this song, press again to return to the menu BGM
    private void OnPreviewBtnClicked()
    {
        if (m_info == null)
            return;

        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_Other);

        if (s_previewingBar == this)
        {
            StopPreview();
            return;
        }

        SongBar previous = s_previewingBar;
        s_previewingBar = this;
        if (previous != null)
            previous.UpdatePreviewIcon();
        UpdatePreviewIcon();

        SoundManager.Instance.OnPlayMusic(m_info.clip, isLoop: true, 1f);
    }

    private void StopPreview()
    {
        if (s_previewingBar != this)
            return;

        s_previewingBar = null;
        UpdatePreviewIcon();
        SoundManager.Instance.OnPlayMusic(ESoundId.Bg_MainMenu, isLoop: true, 1f);
    }

    private void UpdatePreviewIcon()
    {
        if (m_previewImg != null && m_previewSprite != null && m_previewSprite.Length >= 2)
            m_previewImg.sprite = m_previewSprite[s_previewingBar == this ? 1 : 0];
    }
}
