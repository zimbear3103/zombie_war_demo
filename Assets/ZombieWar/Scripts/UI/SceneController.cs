using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class SceneController : PersistenceSingleton<SceneController>
{
    public const string LoadingSceneName = "LoadScene";
    public const string GameplaySceneName = "GameplayZombie";

    private static SceneController m_activeController;
    private AsyncOperation m_asyncOp;

    public AsyncOperation AsyncOp => m_asyncOp;
    public bool IsLoading => m_asyncOp != null;
    public bool LastLoadSucceeded { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        m_activeController = null;
    }

    private void Awake()
    {
        // SceneController owns the persistent System prefab, including its other managers.
        if (m_activeController != null && m_activeController != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        m_activeController = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (m_activeController == this)
            m_activeController = null;
    }

    public IEnumerator LoadMainMenu(Action<float> updateCallback = null, Action callback = null)
    {
        // Home is a screen inside GameplayZombie, not a separate scene.
        return LoadScene(GameplaySceneName, updateCallback, callback);
    }

    public IEnumerator LoadStartup(Action<float> updateCallback = null, Action callback = null)
    {
        return LoadScene(LoadingSceneName, updateCallback, callback);
    }

    public IEnumerator LoadGamePlay(Action<float> updateCallback = null, Action callback = null)
    {
        return LoadScene(GameplaySceneName, updateCallback, callback);
    }

    private IEnumerator LoadScene(string sceneName, Action<float> updateCallback, Action callback)
    {
        if (IsLoading)
            yield break;

        LastLoadSucceeded = false;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneController] Cannot load '{sceneName}'. Enable it in the build scene list.", this);
            yield break;
        }

        string loadingError = null;
        try
        {
            m_asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            loadingError = exception.Message;
        }
        if (m_asyncOp == null)
        {
            Debug.LogError($"[SceneController] Could not load '{sceneName}': {loadingError ?? "no loading operation was returned."}", this);
            yield break;
        }

        try
        {
            while (!m_asyncOp.isDone)
            {
                updateCallback?.Invoke(Mathf.Clamp01(m_asyncOp.progress / 0.9f));
                yield return null;
            }

            // Let destination-scene Start methods finish before entering its game state.
            yield return null;
            LastLoadSucceeded = SceneManager.GetActiveScene().name == sceneName;
            updateCallback?.Invoke(1f);
        }
        finally
        {
            m_asyncOp = null;
        }

        if (LastLoadSucceeded)
            callback?.Invoke();
    }
}
