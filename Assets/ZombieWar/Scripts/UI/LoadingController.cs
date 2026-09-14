using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingController : Singleton<LoadingController>
{
    [SerializeField] private Slider m_progressSlider;
    [SerializeField] private GameObject m_loadingScreen;
    [SerializeField] private GameObject m_splashScreen;
    [SerializeField] private Button m_retryButton;
    [SerializeField] private TextMeshProUGUI m_statusText;
    private bool m_isLoading;

    public bool IsInitialized { set; get; } = false;

    private void OnEnable()
    {
        if (m_retryButton != null)
            m_retryButton.onClick.AddListener(RetryLoading);
    }

    private void OnDisable()
    {
        if (m_retryButton != null)
            m_retryButton.onClick.RemoveListener(RetryLoading);
    }

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
        if (m_retryButton != null)
            m_retryButton.gameObject.SetActive(false);
    }

    public void ShowLoadingScreen(Action callback = null)
    {
        SceneController sceneController = SceneController.Instance;
        if (m_isLoading || sceneController == null || sceneController.IsLoading)
            return;

        if (m_loadingScreen != null)
            m_loadingScreen.SetActive(true);
        if (m_retryButton != null)
            m_retryButton.gameObject.SetActive(false);
        if (m_statusText != null)
            m_statusText.text = string.Empty;
        m_isLoading = true;

        // Run on System so unloading this scene cannot cancel the completion callback.
        sceneController.StartCoroutine(LoadGameplayScene(sceneController, callback));
    }

    private IEnumerator LoadGameplayScene(SceneController sceneController, Action callback)
    {
        yield return sceneController.LoadGamePlay(progress =>
        {
            if (this != null && m_progressSlider != null)
                m_progressSlider.value = progress;
        });

        if (sceneController.LastLoadSucceeded)
        {
            callback?.Invoke();
            yield break;
        }

        if (this == null)
            yield break;
        m_isLoading = false;
        if (m_statusText != null)
            m_statusText.text = "Could not load the game. Please try again.";
        if (m_retryButton != null)
            m_retryButton.gameObject.SetActive(true);
    }

    public void RetryLoading()
    {
        MainStateManager manager = MainStateManager.Instance;
        if (manager != null)
            ShowLoadingScreen(manager.CallbackLoadingScreen);
    }
}
