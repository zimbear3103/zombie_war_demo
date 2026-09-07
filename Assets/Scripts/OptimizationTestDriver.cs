using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptimizationTestDriver : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] m_effects;

    private int m_index;

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ParticleSystem ps = m_effects[m_index];
            ps.Clear(true);
            ps.Play(true);
            m_index = (m_index + 1) % m_effects.Length;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            foreach (ParticleSystem ps in m_effects)
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }
    }
}
