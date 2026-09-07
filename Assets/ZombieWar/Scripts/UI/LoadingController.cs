using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingController : Singleton<LoadingController>
{
    [SerializeField] Slider m_progressSlider;
    [SerializeField] GameObject m_loadingScreen;
    [SerializeField] GameObject m_splashScreen;

    private Action LoadingDoneCallBack;

    public bool IsInitialized { set; get; } = false;

    private void Start()
    {
        m_splashScreen.SetActive(false);
        m_loadingScreen.SetActive(true);
        IsInitialized = true;
    }

    public void ShowLoadingScreen(Action callback = null)
    {
        m_loadingScreen.SetActive(true);
        LoadingDoneCallBack = callback;
        StartCoroutine(OnWaitForLoading());
    }

    IEnumerator OnWaitForLoading()
    {
        yield return StartCoroutine(SceneController.Instance.LoadMainMenu((progress) =>
        {
            m_progressSlider.value = progress;
        }, () =>
        {
            m_progressSlider.value = 1;
            LoadingDoneCallBack?.Invoke();
        }));
    }
}
