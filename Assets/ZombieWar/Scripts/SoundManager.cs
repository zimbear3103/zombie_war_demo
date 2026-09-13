using System.Collections;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Sound
{
    [SerializeField] private string m_name;
    [SerializeField] private AudioClip m_clip;
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
    [SerializeField] private AudioSource m_sfxSource;
    [SerializeField] private AudioSource m_musicSource;
    [SerializeField] private AudioSource m_voiceSource;

    [Header("Audio Clip")]
    [SerializeField] private Sound[] m_sfxSounds;
    [SerializeField] private Sound[] m_musicSounds;

    [Header("Gameplay SFX")]
    [Tooltip("Maximum simultaneous gameplay sounds. Lower-priority voices are replaced when full. Set before entering Play Mode.")]
    [SerializeField, Range(1, 64)] private int m_maxGameplayVoices = 16;

    private sealed class GameplayVoice
    {
        public AudioSource source;
        public Transform followTarget;
        public int handle;
        public bool manuallyPaused;
        public bool paused;
        public double startedAt;
    }

    private GameplayVoice[] m_gameplayVoices;
    private int m_nextGameplayHandle;
    private bool m_gameplayMuted;

    private void Awake()
    {
        EnsureGameplayVoices();
        SetMuteSFX(PlayerPrefs.GetInt("UserOnSFX", 1) == 0);
    }

    private IEnumerator Start()
    {
        UserProfile profile = UserProfile.Instance;
        if (profile == null)
            yield break;

        yield return new WaitUntil(() => profile == null || profile.IsInitialized);
        if (profile == null)
            yield break;

        SetMuteSFX(!profile.OnSFX);
        SetMuteVoice(!profile.OnSFX);
        SetMuteMusic(!profile.OnMusic);
    }

    private void LateUpdate()
    {
        RefreshGameplayVoices();
    }

    private void OnDisable()
    {
        StopAllGameplaySounds();
    }

    public void SetMuteSFX(bool isMute)
    {
        m_gameplayMuted = isMute;
        if (m_sfxSource != null)
            m_sfxSource.mute = isMute;
        if (m_gameplayVoices == null)
            return;
        foreach (GameplayVoice voice in m_gameplayVoices)
        {
            if (voice.source != null)
                voice.source.mute = isMute;
        }
    }

    public void SetMuteMusic(bool isMute)
    {
        if (m_musicSource != null)
            m_musicSource.mute = isMute;
    }
    public void SetMuteVoice(bool isMute)
    {
        if (m_voiceSource != null)
            m_voiceSource.mute = isMute;
    }

    #region SFX
    public Sound GetSoundSfxAudioClip(ESoundId soundId)
    {
        string name = soundId.ToString();
        return m_sfxSounds?.FirstOrDefault(clip => clip != null && clip.Name == name);
    }

    public void OnPlaySfxAudio(ESoundId soundId)
    {
        OnPlaySfxAudio(GetSoundSfxAudioClip(soundId));
    }

    public void OnPlaySfxAudio(Sound soundData)
    {
        if (soundData != null)
            OnPlaySfxAudio(soundData.Clip);
    }

    public void OnPlaySfxAudio(AudioClip soundData)
    {
        if (soundData != null && m_sfxSource != null && m_sfxSource.isActiveAndEnabled)
            m_sfxSource.PlayOneShot(soundData);
    }
    #endregion //SFX

    #region Gameplay SFX
    // Handles identify a playback, not a source slot, so stale owners cannot stop a reused voice.
    public int PlayGameplaySound(GameplaySound sound, Vector3 position, int priority = 128,
        Transform followTarget = null)
    {
        if (!isActiveAndEnabled || m_gameplayMuted || sound == null || sound.Volume <= 0f ||
            !IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            return 0;

        AudioClip clip = sound.GetRandomClip();
        if (clip == null)
            return 0;

        EnsureGameplayVoices();
        RefreshGameplayVoices();
        priority = Mathf.Clamp(priority, 0, 256);
        GameplayVoice voice = AcquireGameplayVoice(priority);
        if (voice == null)
            return 0;

        ReleaseGameplayVoice(voice);
        m_nextGameplayHandle = m_nextGameplayHandle == int.MaxValue ? 1 : m_nextGameplayHandle + 1;
        voice.handle = m_nextGameplayHandle;
        voice.followTarget = followTarget;
        voice.startedAt = Time.unscaledTimeAsDouble;

        AudioSource source = voice.source;
        source.transform.position = position;
        source.clip = clip;
        source.volume = sound.Volume;
        source.pitch = sound.GetRandomPitch();
        source.priority = priority;
        source.minDistance = sound.MinDistance;
        source.maxDistance = sound.MaxDistance;
        source.mute = m_gameplayMuted;
        source.outputAudioMixerGroup = m_sfxSource != null ? m_sfxSource.outputAudioMixerGroup : null;
        source.Play();
        UpdateGameplayPause(voice);
        return voice.handle;
    }

    public bool IsGameplaySoundActive(int handle)
    {
        GameplayVoice voice = FindGameplayVoice(handle);
        return voice != null && voice.source != null &&
               (voice.paused || voice.source.isPlaying);
    }

    public void StopGameplaySound(int handle)
    {
        GameplayVoice voice = FindGameplayVoice(handle);
        if (voice != null)
            ReleaseGameplayVoice(voice);
    }

    public void SetGameplaySoundPaused(int handle, bool paused)
    {
        GameplayVoice voice = FindGameplayVoice(handle);
        if (voice == null)
            return;

        voice.manuallyPaused = paused;
        UpdateGameplayPause(voice);
    }

    public void StopAllGameplaySounds()
    {
        if (m_gameplayVoices == null)
            return;
        foreach (GameplayVoice voice in m_gameplayVoices)
            ReleaseGameplayVoice(voice);
    }

    private void EnsureGameplayVoices()
    {
        if (m_gameplayVoices != null)
            return;

        m_gameplayVoices = new GameplayVoice[Mathf.Clamp(m_maxGameplayVoices, 1, 64)];
        for (int index = 0; index < m_gameplayVoices.Length; index++)
        {
            var sourceObject = new GameObject($"Gameplay SFX {index + 1}");
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            m_gameplayVoices[index] = new GameplayVoice { source = source };
        }
    }

    private GameplayVoice AcquireGameplayVoice(int priority)
    {
        GameplayVoice candidate = null;
        foreach (GameplayVoice voice in m_gameplayVoices)
        {
            if (voice.source == null)
                continue;
            if (voice.handle == 0)
                return voice;
            if (voice.source.priority < priority || voice.paused)
                continue;
            if (candidate == null || voice.source.priority > candidate.source.priority ||
                (voice.source.priority == candidate.source.priority && voice.startedAt < candidate.startedAt))
                candidate = voice;
        }
        return candidate;
    }

    private GameplayVoice FindGameplayVoice(int handle)
    {
        if (handle == 0 || m_gameplayVoices == null)
            return null;
        foreach (GameplayVoice voice in m_gameplayVoices)
        {
            if (voice.handle == handle)
                return voice;
        }
        return null;
    }

    private void RefreshGameplayVoices()
    {
        if (m_gameplayVoices == null)
            return;
        foreach (GameplayVoice voice in m_gameplayVoices)
        {
            if (voice.handle == 0)
                continue;
            // isPlaying is false while paused; paused voices must retain their slot.
            if (voice.source == null || (!voice.paused && !voice.source.isPlaying))
            {
                ReleaseGameplayVoice(voice);
                continue;
            }

            if (voice.followTarget != null)
            {
                if (voice.followTarget.gameObject.activeInHierarchy)
                    voice.source.transform.position = voice.followTarget.position;
                else
                    voice.followTarget = null;
            }
            UpdateGameplayPause(voice);
        }
    }

    private static void UpdateGameplayPause(GameplayVoice voice)
    {
        if (voice.source == null)
            return;
        bool paused = voice.manuallyPaused || Time.timeScale <= 0f;
        if (paused == voice.paused)
            return;
        if (paused)
            voice.source.Pause();
        else
            voice.source.UnPause();
        voice.paused = paused;
    }

    private static void ReleaseGameplayVoice(GameplayVoice voice)
    {
        if (voice.source != null)
        {
            voice.source.Stop();
            voice.source.clip = null;
        }
        voice.handle = 0;
        voice.followTarget = null;
        voice.manuallyPaused = false;
        voice.paused = false;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    #endregion //Gameplay SFX

    #region Music
    public Sound GetSoundMusicAudioClip(ESoundId soundId)
    {
        string name = soundId.ToString();
        return m_musicSounds?.FirstOrDefault(clip => clip != null && clip.Name == name);
    }
    public void OnPlayMusic(Sound soundData, bool isLoop, float volume = 1.0f)
    {
        if (soundData != null)
            OnPlayMusic(soundData.Clip, isLoop, volume);
    }
    public void OnPlayMusic(ESoundId soundId, bool isLoop, float volume = 1.0f)
    {
        OnPlayMusic(GetSoundMusicAudioClip(soundId), isLoop, volume);
    }
    public void OnPlayMusic(AudioClip soundData, bool isLoop, float volume = 1.0f)
    {
        if (soundData == null || m_musicSource == null || !m_musicSource.isActiveAndEnabled)
            return;
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
        if (m_musicSource == null)
            return;
        m_musicSource.Stop();
        m_musicSource.clip = null;
    }
    #endregion //Music
}
