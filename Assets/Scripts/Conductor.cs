using UnityEngine;

public class Conductor : Singleton<Conductor>
{
    [Tooltip("Dedicated source for the gameplay song — separate from SoundManager's BGM source")]
    [SerializeField] private AudioSource m_musicSource;
    [Tooltip("Lead-in before the song starts, in seconds. Keep it >= the tile travel time so the first notes have room to fall in")]
    [SerializeField] private float m_startDelay = 2f;
    [Tooltip("Calibration offset in seconds added to SongPosition. Raise it if taps feel late (mobile audio latency)")]
    [SerializeField] private float m_inputOffset = 0f;

    private double m_dspSongStart;
    private float m_pausedPosition;
    private bool m_isPaused;
    private bool m_hasSong;

    public bool HasSong => m_hasSong;
    public bool IsPaused => m_isPaused;
    public float ClipLength => m_hasSong && m_musicSource.clip != null ? m_musicSource.clip.length : 0f;
    public bool IsSongFinished => m_hasSong && !m_isPaused && SongPosition >= ClipLength;

    public float SongPosition
    {
        get
        {
            if (!m_hasSong)
                return 0f;
            if (m_isPaused)
                return m_pausedPosition;
            return (float)(AudioSettings.dspTime - m_dspSongStart) + m_inputOffset;
        }
    }

    private void Awake()
    {
        if (m_musicSource == null)
            m_musicSource = GetComponent<AudioSource>();
    }

    public void PlaySong(AudioClip clip)
    {
        if (clip == null)
        {
            GameLog.Log(LogType.Error, "[Conductor] PlaySong called with a null clip");
            return;
        }

        m_musicSource.Stop();
        m_musicSource.clip = clip;
        m_musicSource.loop = false;

        m_dspSongStart = AudioSettings.dspTime + m_startDelay;
        m_musicSource.PlayScheduled(m_dspSongStart);

        m_isPaused = false;
        m_hasSong = true;
    }

    public void PauseSong()
    {
        if (!m_hasSong || m_isPaused)
            return;

        m_pausedPosition = SongPosition;
        m_isPaused = true;
        m_musicSource.Pause();
    }

    public void ResumeSong()
    {
        if (!m_hasSong || !m_isPaused)
            return;

        float rawPosition = m_pausedPosition - m_inputOffset;
        m_dspSongStart = AudioSettings.dspTime - rawPosition;
        m_isPaused = false;

        if (rawPosition < 0f)
        {
            m_musicSource.Stop();
            m_musicSource.PlayScheduled(m_dspSongStart);
        }
        else
        {
            m_musicSource.UnPause();
        }
    }

    public void StopSong()
    {
        m_hasSong = false;
        m_isPaused = false;
        m_musicSource.Stop();
        m_musicSource.clip = null;
    }

    // Mirrors the Music toggle in settings (SoundManager forwards it here)
    public void SetMuted(bool isMute)
    {
        m_musicSource.mute = isMute;
    }
}
