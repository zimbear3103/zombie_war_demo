using System;
using System.Collections.Generic;
using UnityEngine;

public enum NoteType
{
    Tap = 0,
    Hold = 1
}

[Serializable]
public class NoteData
{
    public float time;
    public int lane;
    public NoteType type;
    public float holdDuration;
}

[Serializable]
public class BeatMap
{
    public string songName;
    public string songAuthor;
    public float bpm;
    public float offset;
    public List<NoteData> notes = new();
}

public class SongInfo
{
    public int index;
    public string songName;
    public string songAuthor;
    public Sprite previewSprite;
    public AudioClip clip;
}

public class BeatMapLoader : Singleton<BeatMapLoader>
{
    [Serializable]
    public class SongEntry
    {
        [Tooltip("Resources path of the AudioClip, e.g. music/demo_song")]
        public string clipPath;
        [Tooltip("Resources path of the beatmap TextAsset, without .json, e.g. music/on_and_on_beatmap")]
        public string beatmapPath;
        [Tooltip("Cover shown on the song bar in the main menu")]
        public Sprite previewSprite;
    }

    [Tooltip("Selectable songs — indices beyond the list wrap around")]
    [SerializeField] private SongEntry[] m_songs =
    {
        new SongEntry { clipPath = "music/demo_song", beatmapPath = "music/on_and_on_beatmap" }
    };

    public BeatMap CurrentMap { get; private set; }
    public AudioClip CurrentClip { get; private set; }

    public int SongCount => m_songs != null ? m_songs.Length : 0;

    // Menu display data for one entry. Parses the beatmap JSON for name/author —
    // only runs when the song list is built, never during gameplay.
    public SongInfo GetSongInfo(int index)
    {
        if (index < 0 || index >= SongCount)
            return null;

        SongEntry entry = m_songs[index];
        AudioClip clip = Resources.Load<AudioClip>(entry.clipPath);
        TextAsset json = Resources.Load<TextAsset>(entry.beatmapPath);
        if (clip == null || json == null)
        {
            GameLog.Log(LogType.Error, $"[BeatMapLoader] Song info {index}: missing '{entry.clipPath}' or '{entry.beatmapPath}'");
            return null;
        }

        BeatMap map = JsonUtility.FromJson<BeatMap>(json.text);
        return new SongInfo
        {
            index = index,
            songName = string.IsNullOrEmpty(map?.songName) ? entry.clipPath : map.songName,
            songAuthor = map != null ? map.songAuthor : "",
            previewSprite = entry.previewSprite,
            clip = clip,
        };
    }

    public bool LoadSong(int songIndex)
    {
        Clear();

        if (m_songs == null || m_songs.Length == 0)
        {
            GameLog.Log(LogType.Error, "[BeatMapLoader] No songs configured");
            return false;
        }

        SongEntry entry = m_songs[Mathf.Abs(songIndex) % m_songs.Length];

        AudioClip clip = Resources.Load<AudioClip>(entry.clipPath);
        TextAsset json = Resources.Load<TextAsset>(entry.beatmapPath);
        if (clip == null || json == null)
        {
            GameLog.Log(LogType.Error, $"[BeatMapLoader] Missing Resources asset: '{entry.clipPath}' or '{entry.beatmapPath}'");
            return false;
        }

        BeatMap map = JsonUtility.FromJson<BeatMap>(json.text);
        if (map == null || map.notes == null || map.notes.Count == 0)
        {
            GameLog.Log(LogType.Error, $"[BeatMapLoader] Beatmap '{entry.beatmapPath}' is empty or malformed");
            return false;
        }

        map.notes.Sort((a, b) => a.time.CompareTo(b.time));

        if (map.offset != 0f)
        {
            for (int i = 0; i < map.notes.Count; i++)
                map.notes[i].time += map.offset;
        }

        CurrentMap = map;
        CurrentClip = clip;
        GameLog.Log(LogType.Log, $"[BeatMapLoader] Loaded '{map.songName}': {map.notes.Count} notes, bpm {map.bpm}, offset {map.offset}s, clip {clip.length:F1}s");
        return true;
    }

    public void Clear()
    {
        CurrentMap = null;
        CurrentClip = null;
    }
}
