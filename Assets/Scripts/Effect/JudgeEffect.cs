using Utility;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public enum JudgeLevel
{
    Perfect,
    Great,
    Miss
}

public enum JudgeParticle
{
    Transition,
    Perfect1,
    Perfect2,
    Perfect3
}

public class JudgeEffect : MonoBehaviour
{
    [System.Serializable]
    public class LevelStyle
    {
        public JudgeLevel level;
        public string text = "PERFECT";
        public Color topColor = Color.white;
        public Color bottomColor = Color.white;
    }

    [SerializeField] private TextMeshProUGUI m_levelText;
    [SerializeField] private LevelStyle[] m_styles =
    {
        new LevelStyle { level = JudgeLevel.Perfect, text = "PERFECT", topColor = new Color(1f, 0.45f, 0.75f), bottomColor = new Color(1f, 0.8f, 0.25f) },
        new LevelStyle { level = JudgeLevel.Great,   text = "GREAT",   topColor = new Color(0.65f, 1f, 0.5f),  bottomColor = new Color(0.1f, 0.8f, 0.3f) },
        new LevelStyle { level = JudgeLevel.Miss,    text = "MISS",    topColor = new Color(0.9f, 0.9f, 0.9f), bottomColor = new Color(0.55f, 0.55f, 0.6f) },
    };

    [Tooltip("Order must match the JudgeParticle enum: [0] Transition, [1] Perfect1, [2] Perfect2, [3] Perfect3")]
    [FormerlySerializedAs("m_transitionParticle")]
    [SerializeField] private ParticleSystem[] m_judgeParticles;
    [Header("Animation")]
    [Tooltip("Scale the text starts at before popping in to 1")]
    [SerializeField] private float m_popInScale = 0.5f;
    [SerializeField] private float m_popInTime = 0.12f;
    [Tooltip("Scale the text grows to while fading out")]
    [SerializeField] private float m_growScale = 1.35f;
    [SerializeField] private float m_fadeTime = 0.55f;

    private Coroutine m_scaleRoutine;
    private Coroutine m_fadeRoutine;

    private int m_hitStreak;

    private void Awake()
    {
        if (m_levelText == null)
            m_levelText = GetComponentInChildren<TextMeshProUGUI>();

        m_levelText.color = Color.white;
        m_levelText.enableVertexGradient = true;
        m_levelText.gameObject.SetActive(false);
    }

    public void Show(JudgeLevel level)
    {
        LevelStyle style = GetStyle(level);
        if (style == null)
        {
            GameLog.Log(LogType.Warning, $"[JudgeLevelEffect] No style configured for {level}");
            return;
        }

        PlayJudgeParticles(level);

        StopRoutines();

        m_levelText.text = style.text;
        m_levelText.colorGradient = new VertexGradient(style.topColor, style.topColor, style.bottomColor, style.bottomColor);
        m_levelText.alpha = 1f;
        m_levelText.transform.localScale = Vector3.one * m_popInScale;
        m_levelText.gameObject.SetActive(true);

        m_scaleRoutine = StartCoroutine(Tweener.IE_LocalScale(
            m_levelText.transform, Vector3.one * m_popInScale, Vector3.one, m_popInTime, Tweener.Ease.OutBack, () =>
            {
                m_scaleRoutine = StartCoroutine(Tweener.IE_LocalScale(
                    m_levelText.transform, Vector3.one, Vector3.one * m_growScale, m_fadeTime, Tweener.Ease.OutQuad));

                m_fadeRoutine = StartCoroutine(Tweener.IE_TransparencyText(
                    m_levelText, 1f, 0f, m_fadeTime, HideImmediate));
            }));
    }

    private void PlayJudgeParticles(JudgeLevel level)
    {
        if (level != JudgeLevel.Perfect)
        {
            m_hitStreak = 0;
            if (level == JudgeLevel.Great)
                PlayParticle(JudgeParticle.Transition);
            return;
        }

        m_hitStreak++;

        int tier = Mathf.Min(m_hitStreak, 3);
        PlayParticle(JudgeParticle.Perfect1 + (tier - 1));
    }

    private void PlayParticle(JudgeParticle particle)
    {
        int index = (int)particle;
        if (m_judgeParticles == null || index >= m_judgeParticles.Length)
            return;

        ParticleSystem ps = m_judgeParticles[index];
        if (ps == null)
            return;

        if (ps.isPlaying)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        ps.Play(true);
    }

    public void ResetStreak()
    {
        m_hitStreak = 0;
    }

    public void HideImmediate()
    {
        StopRoutines();
        m_levelText.transform.localScale = Vector3.one;
        m_levelText.gameObject.SetActive(false);
    }

    private LevelStyle GetStyle(JudgeLevel level)
    {
        for (int i = 0; i < m_styles.Length; i++)
        {
            if (m_styles[i].level == level)
                return m_styles[i];
        }
        return null;
    }

    private void StopRoutines()
    {
        if (m_scaleRoutine != null) StopCoroutine(m_scaleRoutine);
        if (m_fadeRoutine != null) StopCoroutine(m_fadeRoutine);
        m_scaleRoutine = null;
        m_fadeRoutine = null;
    }
}
