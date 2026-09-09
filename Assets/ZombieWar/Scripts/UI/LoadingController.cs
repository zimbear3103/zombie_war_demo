using System;
using UnityEngine;
using UnityEngine.UI;

public class LoadingController : Singleton<LoadingController>
{
    [SerializeField] private Slider m_progressSlider;
    [SerializeField] private GameObject m_loadingScreen;
    [SerializeField] private GameObject m_splashScreen;

    public bool IsInitialized { set; get; } = false;

    private void Start()
    {
        if (m_splashScreen != null)
            m_splashScreen.SetActive(false);
        if (m_loadingScreen != null)
            m_loadingScreen.SetActive(true);
        if (m_progressSlider != null)
        {
            m_progressSlider.minValue = 0f;
            m_progressSlider.maxValue = 1f;
            m_progressSlider.interactable = false;
            m_progressSlider.value = 0f;
        }
        IsInitialized = true;
    }

    public void ShowLoadingScreen(Action callback = null)
    {
        SceneController sceneController = SceneController.Instance;
        if (sceneController == null || sceneController.IsLoading)
            return;

        if (m_loadingScreen != null)
            m_loadingScreen.SetActive(true);

        // Run on System so unloading this scene cannot cancel the completion callback.
        sceneController.StartCoroutine(sceneController.LoadMainMenu(progress =>
        {
            if (this != null && m_progressSlider != null)
                m_progressSlider.value = progress;
        }, callback));
    }
}
