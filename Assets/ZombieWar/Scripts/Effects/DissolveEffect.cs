using UnityEngine;
using UnityEngine.Serialization;


public class DissolveEffect : MonoBehaviour
{
    private static readonly int m_cutoffId = Shader.PropertyToID("_Cutoff");
    private static readonly int m_mainTextureId = Shader.PropertyToID("_MainTexture");
    private static readonly int m_noiseTextureId = Shader.PropertyToID("_NoiseTexture");
    private static readonly int m_baseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int m_mainTexId = Shader.PropertyToID("_MainTex");
    private const float m_visibleValue = 1f;
    private const float m_dissolvedValue = 0f;
    private const float m_defaultDuration = 2f;

    [FormerlySerializedAs("targetRenderer")]
    [SerializeField] private Renderer m_targetRenderer;
    [SerializeField] private Material m_dissolveMaterial;
    [FormerlySerializedAs("duration")]
    [SerializeField] private float m_duration = m_defaultDuration;
    [FormerlySerializedAs("dissolveParticleSystem")]
    [SerializeField] private ParticleSystem m_dissolveParticleSystem;
    [FormerlySerializedAs("reverseDissolveParticleSystem")]
    [SerializeField] private ParticleSystem m_reverseDissolveParticleSystem;

    private Material[] m_originalMaterials;
    private Material[] m_dissolveMaterials;
    private Material m_cachedTemplate;
    private float m_currentValue = m_visibleValue;
    private float m_startValue;
    private float m_targetValue;
    private float m_elapsedTime;
    private float m_animationDuration;
    private bool m_isInitialized;
    private bool m_materialsApplied;
    private bool m_isPlaying;

    public bool IsPlaying => m_isPlaying;

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (!m_isPlaying)
            return;

        if (m_targetRenderer == null)
        {
            ResetEffect();
            return;
        }

        m_elapsedTime += Time.deltaTime;
        SetCutoff(Mathf.Lerp(m_startValue, m_targetValue, m_elapsedTime / m_animationDuration));

        if (m_elapsedTime < m_animationDuration)
            return;

        SetCutoff(m_targetValue);
        m_isPlaying = false;
        if (m_targetValue == m_visibleValue)
            RestoreOriginalMaterials();
    }

    private void OnDisable()
    {
        ResetEffect();
    }

    private void OnDestroy()
    {
        ResetEffect();
        DestroyOwnedMaterials();
    }

    public bool TryPlayDissolve()
    {
        if (!StartDissolve(m_dissolvedValue))
            return false;

        StopParticles(m_reverseDissolveParticleSystem);
        PlayParticles(m_dissolveParticleSystem);
        return true;
    }

    public void PlayDissolve()
    {
        TryPlayDissolve();
    }

    public void ReverseDissolve()
    {
        if (!StartDissolve(m_visibleValue))
            return;

        StopParticles(m_dissolveParticleSystem);
        PlayParticles(m_reverseDissolveParticleSystem);
    }

    public void SetMaterial(Material newMaterial)
    {
        if (newMaterial == null || newMaterial == m_dissolveMaterial)
            return;

        ResetEffect();
        DestroyOwnedMaterials();
        m_dissolveMaterial = newMaterial;
    }

    public void ResetEffect()
    {
        m_isPlaying = false;
        m_elapsedTime = 0f;
        RestoreOriginalMaterials();
        SetCutoff(m_visibleValue);
        StopParticles(m_dissolveParticleSystem);
        StopParticles(m_reverseDissolveParticleSystem);
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
        m_isInitialized = true;
        return m_originalMaterials.Length > 0;
    }

    private bool StartDissolve(float targetValue)
    {
        if (!isActiveAndEnabled || !Initialize() || !PrepareMaterials())
        {
            ResetEffect();
            return false;
        }

        if (!m_materialsApplied)
        {
            SetCutoff(m_visibleValue);
            m_targetRenderer.sharedMaterials = m_dissolveMaterials;
            m_materialsApplied = true;
        }

        m_animationDuration = m_duration;
        if (m_animationDuration <= 0f || float.IsNaN(m_animationDuration) || float.IsInfinity(m_animationDuration))
            m_animationDuration = m_defaultDuration;

        m_startValue = m_currentValue;
        m_targetValue = targetValue;
        m_elapsedTime = 0f;
        m_isPlaying = true;
        return true;
    }

    private bool PrepareMaterials()
    {
        if (m_dissolveMaterials != null && m_cachedTemplate != m_dissolveMaterial)
        {
            ResetEffect();
            DestroyOwnedMaterials();
        }

        if (m_dissolveMaterials != null)
            return true;

        for (int i = 0; i < m_originalMaterials.Length; i++)
        {
            Material source = m_dissolveMaterial != null ? m_dissolveMaterial : m_originalMaterials[i];
            if (!SupportsDissolve(source))
                return false;
        }

        m_dissolveMaterials = new Material[m_originalMaterials.Length];
        for (int i = 0; i < m_dissolveMaterials.Length; i++)
        {
            Material source = m_dissolveMaterial != null ? m_dissolveMaterial : m_originalMaterials[i];
            Material instance = new Material(source);
            CopyBaseTexture(m_originalMaterials[i], instance);
            instance.SetFloat(m_cutoffId, m_visibleValue);
            m_dissolveMaterials[i] = instance;
        }

        m_cachedTemplate = m_dissolveMaterial;
        return true;
    }

    private static bool SupportsDissolve(Material material)
    {
        return material != null
            && material.HasProperty(m_cutoffId)
            && material.HasProperty(m_mainTextureId)
            && material.HasProperty(m_noiseTextureId);
    }

    private static void CopyBaseTexture(Material source, Material destination)
    {
        if (source == null)
            return;

        int sourceProperty;
        if (source.HasProperty(m_baseMapId))
            sourceProperty = m_baseMapId;
        else if (source.HasProperty(m_mainTexId))
            sourceProperty = m_mainTexId;
        else if (source.HasProperty(m_mainTextureId))
            sourceProperty = m_mainTextureId;
        else
            return;

        destination.SetTexture(m_mainTextureId, source.GetTexture(sourceProperty));
        destination.SetTextureScale(m_mainTextureId, source.GetTextureScale(sourceProperty));
        destination.SetTextureOffset(m_mainTextureId, source.GetTextureOffset(sourceProperty));
    }

    private void SetCutoff(float value)
    {
        m_currentValue = value;
        if (m_dissolveMaterials == null)
            return;

        for (int i = 0; i < m_dissolveMaterials.Length; i++)
        {
            if (m_dissolveMaterials[i] != null)
                m_dissolveMaterials[i].SetFloat(m_cutoffId, value);
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (m_materialsApplied && m_targetRenderer != null)
            m_targetRenderer.sharedMaterials = m_originalMaterials;

        m_materialsApplied = false;
    }

    private void DestroyOwnedMaterials()
    {
        if (m_dissolveMaterials == null)
            return;

        for (int i = 0; i < m_dissolveMaterials.Length; i++)
        {
            if (m_dissolveMaterials[i] != null)
                Destroy(m_dissolveMaterials[i]);
        }

        m_dissolveMaterials = null;
        m_cachedTemplate = null;
    }

    private static void PlayParticles(ParticleSystem particles)
    {
        if (particles == null)
            return;

        StopParticles(particles);
        particles.Play(true);
    }

    private static void StopParticles(ParticleSystem particles)
    {
        if (particles != null)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}