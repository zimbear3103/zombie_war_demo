using UnityEngine;

public class HitFlashEffect : MonoBehaviour
{
    private const float m_defaultDuration = 0.08f;

    [SerializeField] private Renderer m_targetRenderer;
    [SerializeField] private Material m_onDamageVFXMaterial;
    [SerializeField] private float m_onDamageVFXDuration = m_defaultDuration;

    private Material[] m_originalMaterials;
    private Material[] m_flashMaterials;
    private float m_remainingTime;
    private bool m_isInitialized;
    private bool m_isFlashing;

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (!m_isFlashing)
            return;

        m_remainingTime -= Time.deltaTime;
        if (m_remainingTime <= 0f)
            ResetEffect();
    }

    private void OnDisable()
    {
        ResetEffect();
    }

    public void PlayOnDamageVFX()
    {
        if (!isActiveAndEnabled || m_onDamageVFXMaterial == null || !Initialize())
            return;

        if (!m_isFlashing)
        {
            for (int i = 0; i < m_flashMaterials.Length; i++)
                m_flashMaterials[i] = m_onDamageVFXMaterial;

            m_targetRenderer.sharedMaterials = m_flashMaterials;
            m_isFlashing = true;
        }

        m_remainingTime = m_onDamageVFXDuration;
        if (m_remainingTime <= 0f || float.IsNaN(m_remainingTime) || float.IsInfinity(m_remainingTime))
            m_remainingTime = m_defaultDuration;
    }

    public void ResetEffect()
    {
        if (m_isFlashing && m_targetRenderer != null)
            m_targetRenderer.sharedMaterials = m_originalMaterials;

        m_isFlashing = false;
        m_remainingTime = 0f;
    }

    private bool Initialize()
    {
        if (m_isInitialized)
            return m_targetRenderer != null && m_originalMaterials.Length > 0;

        if (!(m_targetRenderer is SkinnedMeshRenderer) && !(m_targetRenderer is MeshRenderer))
        {
            m_targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (m_targetRenderer == null)
                m_targetRenderer = GetComponentInChildren<MeshRenderer>(true);
        }

        if (m_targetRenderer == null)
            return false;

        m_originalMaterials = m_targetRenderer.sharedMaterials;
        m_flashMaterials = new Material[m_originalMaterials.Length];
        m_isInitialized = true;
        return m_originalMaterials.Length > 0;
    }
}
