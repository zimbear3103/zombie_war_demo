using System.Collections;
using UnityEngine;

public class MuzzleEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] m_muzzleParticleSystems;

    [SerializeField] private Light m_light;

    [SerializeField] private float time = 0.05f;

    private Coroutine m_activeRoutine;

    private void Start()
    {
        StopMuzzle();
    }

    public void PlayMuzzle()
    {
        // Bắn liên thanh: nếu phát trước chưa kịp auto-stop mà đã Fire tiếp,
        // hủy coroutine cũ trước khi start cái mới, tránh 2 coroutine tranh nhau tắt light
        if (m_activeRoutine != null)
            StopCoroutine(m_activeRoutine);

        if (m_muzzleParticleSystems != null)
        {
            foreach (var ps in m_muzzleParticleSystems)
                ps.Play();
        }

        if (m_light != null)
            m_light.enabled = true;

        m_activeRoutine = StartCoroutine(AutoStopAfterDelay());
    }

    public void StopMuzzle()
    {
        if (m_activeRoutine != null)
        {
            StopCoroutine(m_activeRoutine);
            m_activeRoutine = null;
        }

        HardStop();
    }

    private IEnumerator AutoStopAfterDelay()
    {
        yield return new WaitForSeconds(time);
        m_activeRoutine = null;
        HardStop();
    }

    private void HardStop()
    {
        if (m_muzzleParticleSystems != null)
        {
            foreach (var ps in m_muzzleParticleSystems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (m_light != null)
            m_light.enabled = false;
    }
}