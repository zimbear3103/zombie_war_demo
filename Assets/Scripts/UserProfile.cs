using UnityEngine;
using System;

public class UserProfile : PersistenceSingleton<UserProfile>
{
    public Action<int> OnLevelChanged;
    public Action<int> OnScoreChanged;
    public Action<int> OnStarsChanged;
    public Action OnFavoriteChanged;

    [Header("Demo Data")]
    [Tooltip("First run only: seed the profile with fake coins/stars/favorite so the Home screen doesn't look empty when presenting")]
    [SerializeField] private bool m_seedFakeData = true;

    private const string k_demoSongName = "Cartoon, Daniel Levi & Jeja - On & On (NCS Release)";

    private int m_level;
    private int m_coin;
    private int m_totalStars;

    public int Coin
    {
        get => m_coin;
        set
        {
            if (m_coin != value)
            {
                m_coin = value;
                OnScoreChanged?.Invoke(m_coin);
            }
        }
    }

    public int Level
    {
        get => m_level;
        set
        {
            if (m_level != value)
            {
                m_level = value;
                OnLevelChanged?.Invoke(m_level);
            }
        }
    }

    public int TotalStars
    {
        get => m_totalStars;
        private set
        {
            if (m_totalStars != value)
            {
                m_totalStars = value;
                OnStarsChanged?.Invoke(m_totalStars);
            }
        }
    }

    public bool OnMusic { set; get; } = true;
    public bool OnSFX { set; get; } = true;
    public bool OnVibration { set; get; } = true;

    public bool IsInitialized { set; get; } = false;

    private void Start()
    {
        Debug.Log("[UserProfile] called Start");
        LoadGameData();
        IsInitialized = true;
    }

    public void SaveGameData()
    {
        PlayerPrefs.SetInt("UserCoin", Coin);
        PlayerPrefs.SetInt("UserLevel", Level);
        PlayerPrefs.SetInt("UserStars", TotalStars);

        PlayerPrefs.SetInt("UserOnMusic", OnMusic ? 1 : 0);
        PlayerPrefs.SetInt("UserOnSFX", OnSFX ? 1 : 0);
    }

    public void LoadGameData()
    {
        Debug.Log("[UserProfile] called LoadGameData");

        if (m_seedFakeData && PlayerPrefs.GetInt("UserDataSeeded", 0) == 0)
            SeedFakeData();

        Coin = PlayerPrefs.GetInt("UserCoin", 0);
        Level = PlayerPrefs.GetInt("UserLevel", 0);
        TotalStars = PlayerPrefs.GetInt("UserStars", 0);

        OnMusic = PlayerPrefs.GetInt("UserOnMusic", 1) == 1;
        OnSFX = PlayerPrefs.GetInt("UserOnSFX", 1) == 1;
        OnVibration = PlayerPrefs.GetInt("UserOnVibration", 1) == 1;
    }

    #region Stars
    public int GetSongStars(string songName)
    {
        return PlayerPrefs.GetInt($"stars_{songName}", 0);
    }

    // Keeps the best record per song. Returns true when the record improved.
    public bool SubmitSongStars(string songName, int stars)
    {
        int best = GetSongStars(songName);
        if (stars <= best)
            return false;

        PlayerPrefs.SetInt($"stars_{songName}", stars);
        TotalStars += stars - best;
        PlayerPrefs.SetInt("UserStars", TotalStars);
        PlayerPrefs.Save();
        return true;
    }
    #endregion //Stars

    #region Score
    public int GetBestScore(string songName)
    {
        return PlayerPrefs.GetInt($"score_{songName}", 0);
    }

    // Keeps the best score per song. Returns true when the record improved.
    public bool SubmitSongScore(string songName, int score)
    {
        if (score <= GetBestScore(songName))
            return false;

        PlayerPrefs.SetInt($"score_{songName}", score);
        PlayerPrefs.Save();
        return true;
    }
    #endregion //Score

    #region Favorite
    public bool IsFavorite(string songName)
    {
        return PlayerPrefs.GetInt($"fav_{songName}", 0) == 1;
    }

    public void SetFavorite(string songName, bool isFavorite)
    {
        PlayerPrefs.SetInt($"fav_{songName}", isFavorite ? 1 : 0);
        PlayerPrefs.Save();
        OnFavoriteChanged?.Invoke();
    }
    #endregion //Favorite

    private void SeedFakeData()
    {
        PlayerPrefs.SetInt("UserDataSeeded", 1);
        PlayerPrefs.SetInt("UserCoin", 750);
        PlayerPrefs.SetInt("UserStars", 2);
        PlayerPrefs.SetInt($"stars_{k_demoSongName}", 2);
        PlayerPrefs.SetInt($"fav_{k_demoSongName}", 1);
        PlayerPrefs.Save();
        Debug.Log("[UserProfile] Seeded fake demo data");
    }

    public void SaveOnMusicGame(bool isOn)
    {
        OnMusic = isOn;
        PlayerPrefs.SetInt("UserOnMusic", OnMusic ? 1 : 0);
    }

    public void SaveOnSFXGame(bool isOn)
    {
        OnSFX = isOn;
        PlayerPrefs.SetInt("UserOnSFX", OnSFX ? 1 : 0);
    }

    public void SaveOnVibrationGame(bool isOn)
    {
        OnVibration = isOn;
        PlayerPrefs.SetInt("UserOnVibration", OnVibration ? 1 : 0);
    }
}
