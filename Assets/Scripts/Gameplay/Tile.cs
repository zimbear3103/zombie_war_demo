using Utility;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum TileState
{
    None = 0,
    Falling,
    Hit,
    Missed
}

public class Tile : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Image m_tileImage;
    [Header("Scoring")]
    [SerializeField] private int m_scorePerHit = 100;

    [Header("Timing")]
    [SerializeField] private float flashHold = 0.05f;
    [SerializeField] private float fadeDuration = 0.28f;

    [Header("Colors")]
    [SerializeField] private Color flashColor = Color.white;
    [Tooltip("Tint shown when the tile falls past the bottom and ends the run")]
    [SerializeField] private Color m_missColor = new Color(1f, 0.25f, 0.25f);
    private RectTransform m_rect;
    private TileState m_state = TileState.None;
    private Vector3 m_baseScale;
    private Color m_baseColor;
    private Coroutine m_running;

    private float m_noteTime;
    private float m_hitY;
    private float m_missY;
    private float m_fallSpeed;
    private HitLine m_hitLine;
    private JudgeLevel m_hitJudge = JudgeLevel.Great;

    public TileState State => m_state;
    public int Lane { get; private set; }
    public float NoteTime => m_noteTime;
    public JudgeLevel HitJudge => m_hitJudge;

    public bool DroppedToBottom { get; private set; }

    public Action<Tile, int> OnHit;
    public Action<Tile> OnMiss;

    private void Awake()
    {
        m_rect = (RectTransform)transform;
        m_baseScale = transform.localScale;

        if (m_tileImage == null)
            m_tileImage = GetComponent<Image>();

        m_baseColor = m_tileImage.color;
    }

    public void Init(int lane, float noteTime, float fallSpeed, float hitY, float missY, HitLine hitLine)
    {
        Lane = lane;
        m_noteTime = noteTime;
        m_fallSpeed = fallSpeed;
        m_hitY = hitY;
        m_missY = missY;
        m_hitLine = hitLine;
        m_hitJudge = JudgeLevel.Great;
        DroppedToBottom = false;

        var laneRect = m_rect.parent as RectTransform;
        if (laneRect != null)
            m_rect.sizeDelta = new Vector2(laneRect.rect.width, m_rect.sizeDelta.y);

        ResetEffect();
        transform.localScale = m_baseScale;

        UpdatePosition();
        m_state = TileState.Falling;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (m_state != TileState.Falling)
            return;

        if (GamePlayController.Instance != null && !GamePlayController.Instance.IsGamePlaying)
            return;

        UpdatePosition();

        if (m_rect.anchoredPosition.y <= m_missY)
        {
            FailTile(droppedToBottom: true);
        }
    }

    private void UpdatePosition()
    {
        var conductor = Conductor.Instance;
        if (conductor == null)
            return;

        float y = m_hitY + (m_noteTime - conductor.SongPosition) * m_fallSpeed;
        m_rect.anchoredPosition = new Vector2(0f, y);
    }

    public void TapFromInput()
    {
        if (m_state != TileState.Falling)
            return;

        if (GamePlayController.Instance != null && !GamePlayController.Instance.IsGamePlaying)
            return;

        m_hitJudge = m_hitLine != null ? m_hitLine.Judge(transform.position) : JudgeLevel.Great;
        if (m_hitJudge == JudgeLevel.Miss)
        {
            FailTile(droppedToBottom: false);
            return;
        }

        CompleteTap();
    }

    private void CompleteTap()
    {
        m_state = TileState.Hit;
        OnHit?.Invoke(this, m_scorePerHit);

        Play(Deactivate);
    }

    private void FailTile(bool droppedToBottom)
    {
        DroppedToBottom = droppedToBottom;
        m_state = TileState.Missed;
        OnMiss?.Invoke(this);

        if (droppedToBottom)
        {
            if (m_running != null) StopCoroutine(m_running);
            m_running = StartCoroutine(FlashAndFade(m_missColor, Deactivate));
        }
        else
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        m_state = TileState.None;
        gameObject.SetActive(false);
    }

    public void Play(Action onComplete = null)
    {
        if (m_running != null) StopCoroutine(m_running);
        m_running = StartCoroutine(FlashAndFade(flashColor, onComplete));
    }

    // Restore visuals before reusing a pooled tile (the hit flash fades alpha to 0)
    public void ResetEffect()
    {
        if (m_running != null) StopCoroutine(m_running);
        m_running = null;
        m_tileImage.color = m_baseColor;
    }

    private IEnumerator FlashAndFade(Color from, Action onComplete)
    {
        m_tileImage.color = from;
        yield return new WaitForSeconds(flashHold);

        float t = 0f;
        Color c = from;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = 1f - Tweener.Evaluate(Tweener.Ease.OutQuad, Mathf.Clamp01(t / fadeDuration));
            m_tileImage.color = c;
            yield return null;
        }

        m_running = null;
        onComplete.Invoke();
    }
}
