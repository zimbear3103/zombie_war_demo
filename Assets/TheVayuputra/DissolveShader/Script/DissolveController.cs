using System.Collections;
using UnityEngine;
namespace TheVayuputra
{
    public class DissolveController : MonoBehaviour
    {
        private static readonly int DissolveAmountId = Shader.PropertyToID("_Cutoff");
        private static readonly int EdgeColorId = Shader.PropertyToID("_Edge_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private const float VisibleValue = 1f;
        private const float DissolvedValue = 0f;

        public Renderer targetRenderer;

        [SerializeField] private float duration = 1f;
        public ParticleSystem dissolveParticleSystem, reverseDissolveParticleSystem;

        private Material material;
        private Coroutine currentRoutine;

        void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            if (targetRenderer == null)
                return;

            material = targetRenderer.material;
            if (material.HasProperty(DissolveAmountId))
                material.SetFloat(DissolveAmountId, VisibleValue);
        }


        public void PlayDissolve()
        {
            PlayDissolveParticles();
            StartDissolve(DissolvedValue);
        }

        public void ReverseDissolve()
        {
            PlayReverseDissolveParticles();
            StartDissolve(VisibleValue);
        }

        public void SetMaterial(Material newMaterial)
        {
            if (newMaterial == null)
                return;

            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            if (targetRenderer == null)
                return;

            targetRenderer.material = newMaterial;
            material = targetRenderer.material;

            if (material.HasProperty(DissolveAmountId))
                material.SetFloat(DissolveAmountId, VisibleValue);
        }
        private void StartDissolve(float targetValue)
        {
            if (material == null || !material.HasProperty(DissolveAmountId))
                return;

            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine = StartCoroutine(DissolveRoutine(targetValue));
        }

        private void PlayDissolveParticles()
        {
            if (dissolveParticleSystem == null)
                return;

            dissolveParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dissolveParticleSystem.Play(true);
        }
        private void PlayReverseDissolveParticles()
        {
            if (reverseDissolveParticleSystem == null)
                return;

            reverseDissolveParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            reverseDissolveParticleSystem.Play(true);
        }


        private IEnumerator DissolveRoutine(float target)
        {
            float start = material.GetFloat(DissolveAmountId);
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float value = Mathf.Lerp(start, target, time / Mathf.Max(duration, 0.0001f));

                material.SetFloat(DissolveAmountId, value);
                yield return null;
            }

            material.SetFloat(DissolveAmountId, target);
        }
    }
}
