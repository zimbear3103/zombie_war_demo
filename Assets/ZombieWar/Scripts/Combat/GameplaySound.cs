using System;
using UnityEngine;

[Serializable]
public class GameplaySound
{
    [SerializeField] private AudioClip[] m_clips = Array.Empty<AudioClip>();
    [SerializeField, Range(0f, 1f)] private float m_volume = 0.8f;
    [Tooltip("Random pitch range. Use (1, 1) for reload clips authored to match Reload Time.")]
    [SerializeField] private Vector2 m_pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField, Min(0.01f)] private float m_minDistance = 5f;
    [SerializeField, Min(0.01f)] private float m_maxDistance = 40f;

    public float Volume => ClampFinite(m_volume, 0f, 1f, 0.8f);
    public float MinDistance => ClampFinite(m_minDistance, 0.01f, 10000f, 5f);
    public float MaxDistance => Mathf.Max(MinDistance + 0.01f,
        ClampFinite(m_maxDistance, 0.01f, 10000f, 40f));

    public AudioClip GetRandomClip()
    {
        if (m_clips == null || m_clips.Length == 0)
            return null;

        // Reservoir sampling ignores empty Inspector slots without allocating a filtered array.
        AudioClip selected = null;
        int count = 0;
        foreach (AudioClip clip in m_clips)
        {
            if (clip != null && UnityEngine.Random.Range(0, ++count) == 0)
                selected = clip;
        }
        return selected;
    }

    public float GetRandomPitch()
    {
        float first = ClampFinite(m_pitchRange.x, 0.1f, 3f, 1f);
        float second = ClampFinite(m_pitchRange.y, 0.1f, 3f, 1f);
        return UnityEngine.Random.Range(Mathf.Min(first, second), Mathf.Max(first, second));
    }

    private static float ClampFinite(float value, float minimum, float maximum, float fallback)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? fallback
            : Mathf.Clamp(value, minimum, maximum);
    }
}
