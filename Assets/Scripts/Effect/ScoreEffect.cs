using Utility;
using TMPro;
using UnityEngine;

public class ScoreEffect : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_scoreText;

    [Header("Pop Animation")]
    [Tooltip("Small scale pop on each score change; keep subtle (1.1 - 1.2)")]
    [SerializeField] private float m_popScale = 1.15f;
    [SerializeField] private float m_popTime = 0.15f;

    private Coroutine m_popRoutine;

    private void Awake()
    {
        if (m_scoreText == null)
            m_scoreText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (TileSpawner.Instance != null)
        {
            TileSpawner.Instance.OnScoreChanged += SetScore;
            SetScore(TileSpawner.Instance.Score);
        }
    }

    private void OnDisable()
    {
        if (TileSpawner.Instance != null)
            TileSpawner.Instance.OnScoreChanged -= SetScore;
    }

    public void SetScore(int score)
    {
        m_scoreText.text = score.ToString();

        if (score <= 0)
        {
            ResetPop();
            return;
        }

        if (m_popRoutine != null)
            StopCoroutine(m_popRoutine);

        m_popRoutine = StartCoroutine(Tweener.IE_LocalScale(
            m_scoreText.transform, Vector3.one * m_popScale, Vector3.one, m_popTime, Tweener.Ease.OutQuad));
    }

    private void ResetPop()
    {
        if (m_popRoutine != null)
        {
            StopCoroutine(m_popRoutine);
            m_popRoutine = null;
        }
        m_scoreText.transform.localScale = Vector3.one;
    }
}
