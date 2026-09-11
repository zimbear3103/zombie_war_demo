using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace TheVayuputra
{
    public class DissolveDemoController : MonoBehaviour
    {
        [Header("Material Names")]
        public List<string> materialNames = new List<string>();
        public DissolveController[] objects;
        public Text materialNameText;
        [Header("Material Presets")]
        public List<Material> materials = new List<Material>();
        public List<Color> materialColors = new List<Color>();
        [Header("Loop")]
        public float loopDelay = 2f;

        private int currentIndex = 0;
        private Coroutine loopRoutine;
        private bool isLoopDissolved;

        private void Awake()
        {
            if (objects == null)
                return;
            UpdateText();

        }

        private void Start()
        {
            StartLoop();
        }

        public void Dissolve()
        {
            StopLoop();

            if (objects == null)
                return;

            SetDissolveState(true);
        }
        public void Reverse()
        {
            StopLoop();

            if (objects == null)
                return;

            SetDissolveState(false);
        }

        public void StartLoop()
        {
            if (loopRoutine != null)
                return;

            loopRoutine = StartCoroutine(LoopDissolveRoutine());
        }

        public void ToggleLoop()
        {
            if (loopRoutine != null)
            {
                StopLoop();
                return;
            }

            StartLoop();
        }

        private void StopLoop()
        {
            if (loopRoutine == null)
                return;

            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        private IEnumerator LoopDissolveRoutine()
        {
            while (true)
            {
                isLoopDissolved = !isLoopDissolved;
                SetDissolveState(isLoopDissolved);
                yield return new WaitForSeconds(loopDelay);
            }
        }

        private void SetDissolveState(bool dissolve)
        {
            if (objects == null)
                return;

            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                    continue;

                if (dissolve)
                    obj.PlayDissolve();
                else
                    obj.ReverseDissolve();
            }
        }
        void ApplyMaterial()
        {
            if (materials.Count == 0 || objects == null)
                return;

            Material mat = materials[currentIndex];
            if (mat == null)
                return;

            for (int i = 0; i < objects.Length; i++)
            {
                DissolveController obj = objects[i];
                if (obj != null)
                {
                    obj.SetMaterial(mat);
                }
            }
            for (int i = 0; i < objects.Length; i++)
            {
                DissolveController obj = objects[i];
                if (obj != null && materialColors.Count > currentIndex)
                {
                    var main = obj.dissolveParticleSystem.main;
                    main.startColor = materialColors[currentIndex];

                    var reverseMain = obj.reverseDissolveParticleSystem.main;
                    reverseMain.startColor = materialColors[currentIndex];
                }
            }
            UpdateText();
            Debug.Log("Applied Material: " + mat.name);
        }

        // Next Button
        public void NextMaterial()
        {
            if (materials.Count == 0)
                return;

            currentIndex++;

            if (currentIndex >= materials.Count)
                currentIndex = 0;

            ApplyMaterial();

        }

        // Previous Button
        public void PreviousMaterial()
        {
            if (materials.Count == 0)
                return;

            currentIndex--;

            if (currentIndex < 0)
                currentIndex = materials.Count - 1;

            ApplyMaterial();
        }
        void UpdateText()
        {
            if (materialNameText == null)
                return;

            if (currentIndex < materialNames.Count && !string.IsNullOrEmpty(materialNames[currentIndex]))
            {
                materialNameText.text = materialNames[currentIndex];
                return;
            }

            if (currentIndex < materials.Count && materials[currentIndex] != null)
                materialNameText.text = materials[currentIndex].name;
        }
    }
}