using UnityEngine;

public class TapEffect : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Lifetime before this root returns to the pool")]
    [SerializeField] private float m_duration = 0.35f;
    [SerializeField] private ParticleSystem m_tapParticle;

    private float m_timer = -1f;

    private void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        m_timer = 0f;

        if (m_tapParticle != null)
        {
            m_tapParticle.Clear(true);
            m_tapParticle.Play(true);
        }
    }

    private void Update()
    {
        if (m_timer < 0f)
            return;

        m_timer += Time.deltaTime;
        if (m_timer >= m_duration)
        {
            m_timer = -1f;
            gameObject.SetActive(false);
        }
    }
}
