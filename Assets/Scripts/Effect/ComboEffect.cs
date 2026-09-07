using TMPro;
using UnityEngine;
using Utility;

public class ComboEffect : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform m_rect;
    [SerializeField] private TextMeshProUGUI m_comboText;

    [Header("Display")]
    [Tooltip("label only appears from this combo up")]
    [SerializeField] private int m_showFrom = 2;

    [Header("Bounce + Rotation")]
    [Tooltip("Scale the label punches in from, settling to 1")]
    [SerializeField] private float m_punchScale = 1.4f;
    [Tooltip("Random tilt on each hit, swinging back upright during the bounce")]
    [SerializeField] private float m_tiltDegrees = 12f;
    [Tooltip("Bounce-in duration — same as JudgeEffect's pop-in so both labels move together")]
    [SerializeField] private float m_popInTime = 0.12f;

    [Header("Fade Out")]
    [Tooltip("Scale the label grows to while fading out")]
    [SerializeField] private float m_growScale = 1.35f;
    [SerializeField] private float m_fadeTime = 0.55f;

    [Header("Reset")]
    [Tooltip("Quick fade when a miss breaks the combo while the label is still visible")]
    [SerializeField] private float m_fadeOutDuration = 0.15f;

    public int Current { get; private set; }

    private Coroutine m_scaleRoutine;
    private Coroutine m_tiltRoutine;
    private Coroutine m_fadeRoutine;

    private void Awake()
    {
        if (m_rect == null)
            m_rect = (RectTransform)transform;

        if (m_comboText == null)
            m_comboText = GetComponentInChildren<TextMeshProUGUI>(true);

        m_comboText.gameObject.SetActive(false);
    }

    public void Increment()
    {
        Current++;
        if (Current < m_showFrom) return;

        StopRoutines();

        m_comboText.text = $"x{Current}";
        m_comboText.alpha = 1f;
        m_comboText.gameObject.SetActive(true);

        float tilt = Random.value < 0.5f ? -m_tiltDegrees : m_tiltDegrees;
        m_rect.localScale = Vector3.one * m_punchScale;
        m_rect.localRotation = Quaternion.Euler(0f, 0f, tilt);

        m_tiltRoutine = StartCoroutine(Tweener.IE_LocalRotate(
            m_rect, Quaternion.Euler(0f, 0f, tilt), Quaternion.identity, m_popInTime, Tweener.Ease.OutBack));

        m_scaleRoutine = StartCoroutine(Tweener.IE_LocalScale(
            m_rect, Vector3.one * m_punchScale, Vector3.one, m_popInTime, Tweener.Ease.OutBack, () =>
            {
                m_scaleRoutine = StartCoroutine(Tweener.IE_LocalScale(
                    m_rect, Vector3.one, Vector3.one * m_growScale, m_fadeTime, Tweener.Ease.OutQuad));

                m_fadeRoutine = StartCoroutine(Tweener.IE_TransparencyText(
                    m_comboText, 1f, 0f, m_fadeTime, HideImmediate));
            }));
    }

    public void ResetCombo()
    {
        Current = 0;

        if (!m_comboText.gameObject.activeSelf)
            return;

        StopRoutines();
        m_fadeRoutine = StartCoroutine(Tweener.IE_TransparencyText(
            m_comboText, m_comboText.alpha, 0f, m_fadeOutDuration, HideImmediate));
    }

    public void HideImmediate()
    {
        StopRoutines();
        m_rect.localScale = Vector3.one;
        m_rect.localRotation = Quaternion.identity;
        m_comboText.gameObject.SetActive(false);
    }

    private void StopRoutines()
    {
        if (m_scaleRoutine != null) StopCoroutine(m_scaleRoutine);
        if (m_tiltRoutine != null) StopCoroutine(m_tiltRoutine);
        if (m_fadeRoutine != null) StopCoroutine(m_fadeRoutine);
        m_scaleRoutine = null;
        m_tiltRoutine = null;
        m_fadeRoutine = null;
    }
}
