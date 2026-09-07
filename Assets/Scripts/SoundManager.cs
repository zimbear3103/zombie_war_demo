using System.Collections;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Sound
{
    [SerializeField] string m_name;
    [SerializeField] AudioClip m_clip;
    public string Name => m_name;
    public AudioClip Clip => m_clip;
}

public enum ESoundId
{
    Bg_Deafeat,
    Bg_MainGame,
    Bg_MainMenu,
    Bg_Victory,
    UI_Click_ButtonMain,
    UI_Click_ButtonNegative,
    UI_Click_Other,
    Ingame_Run,
}

public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio Source")]
    [SerializeField] AudioSource m_sfxSource;
    [SerializeField] AudioSource m_musicSource;
    [SerializeField] AudioSource m_voiceSource;

    [Header("Audio Clip")]
    [SerializeField] Sound[] m_sfxSounds;
    [SerializeField] Sound[] m_musicSounds;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => UserProfile.Instance.IsInitialized);

        SetMuteSFX(!UserProfile.Instance.OnSFX);
        SetMuteVoice(!UserProfile.Instance.OnSFX);
        SetMuteMusic(!UserProfile.Instance.OnMusic);
    }

    public void SetMuteSFX(bool isMute)
    {
        m_sfxSource.mute = isMute;
    }

    public void SetMuteMusic(bool isMute)
    {
        m_musicSource.mute = isMute;

        if (Conductor.Instance != null)
            Conductor.Instance.SetMuted(isMute);
    }
    public void SetMuteVoice(bool isMute)
    {
        m_voiceSource.mute = isMute;
    }

    #region SFX
    public Sound GetSoundSfxAudioClip(ESoundId soundId)
    {
        string name = soundId.ToString();
        return m_sfxSounds.FirstOrDefault(clip => clip.Name == name);
    }

    public void OnPlaySfxAudio(ESoundId soundId)
    {
        OnPlaySfxAudio(GetSoundSfxAudioClip(soundId));
    }

    public void OnPlaySfxAudio(Sound soundData)
    {
        OnPlaySfxAudio(soundData.Clip);
    }

    public void OnPlaySfxAudio(AudioClip soundData)
    {
        m_sfxSource.PlayOneShot(soundData);
    }
    #endregion //SFX

    #region Music
    public Sound GetSoundMusicAudioClip(ESoundId soundId)
    {
        string name = soundId.ToString();
        return m_musicSounds.FirstOrDefault(clip => clip.Name == name);
    }
    public void OnPlayMusic(Sound soundData, bool isLoop, float volume = 1.0f)
    {
        OnPlayMusic(soundData.Clip, isLoop, volume);
    }
    public void OnPlayMusic(ESoundId soundId, bool isLoop, float volume = 1.0f)
    {
        OnPlayMusic(GetSoundMusicAudioClip(soundId), isLoop, volume);
    }
    public void OnPlayMusic(AudioClip soundData, bool isLoop, float volume = 1.0f)
    {
        if (m_musicSource.isPlaying)
            m_musicSource.Stop();
        m_musicSource.volume = volume;
        m_musicSource.loop = isLoop;
        m_musicSource.clip = soundData;
        m_musicSource.Play();
    }

    // Stops whatever BGM is playing (menu music, previews). The gameplay song
    // runs on the Conductor's own source and is not affected.
    public void StopMusic()
    {
        m_musicSource.Stop();
        m_musicSource.clip = null;
    }
    #endregion //Music
}
