using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainStateManager : PersistenceSingleton<MainStateManager>
{
    public enum MainStateType
    {
        None = 0,
        Loading,
        MainMenu,
        Gameplay,
    }

    public enum MainStatusType
    {
        None = 0,
        Enter,
        Update,
        Exit
    }

    [Header("State Properties")]
    [SerializeField, ReadOnly] private MainStateType m_previousMainState;
    [SerializeField, ReadOnly] private MainStateType m_mainState;
    [SerializeField, ReadOnly] private MainStateType m_nextMainState;
    [SerializeField, ReadOnly] private MainStatusType m_mainStatus;
    private bool m_isTransitioning;

#if UNITY_EDITOR
    [Header("Editor Quick Start")]
    [Tooltip("Play directly in GameplayZombie. Disable to follow LoadScene -> MainMenu instead.")]
    [SerializeField] private bool m_startGameplayDirectlyInEditor = true;
#endif

    public MainStateType MainState => m_mainState;
    public MainStateType NextMainState => m_nextMainState;
    public MainStatusType MainStatus => m_mainStatus;
    public bool IsTransitioning => m_isTransitioning;

    private IEnumerator Start()
    {
        yield return null;

        Time.timeScale = 1f;
        string startingScene = SceneManager.GetActiveScene().name;

#if UNITY_EDITOR
        // Only the initial Editor scene can bypass boot. Scene transitions keep the normal session flow.
        if (m_startGameplayDirectlyInEditor && startingScene == SceneController.GameplaySceneName)
        {
            SetMainState(MainStateType.Gameplay);
            yield break;
        }
#endif

        switch (startingScene)
        {
            case SceneController.LoadingSceneName:
                SetMainState(MainStateType.Loading);
                break;
            case SceneController.MainMenuSceneName:
                SetMainState(MainStateType.MainMenu);
                break;
            case SceneController.GameplaySceneName:
                yield return TransitionTo(MainStateType.Loading);
                break;
        }
    }

    private void Update()
    {
        if (m_isTransitioning)
            return;

        switch (m_mainState)
        {
            case MainStateType.None:
                break;
            case MainStateType.Loading:
                switch (m_mainStatus)
                {
                    case MainStatusType.Enter:
                        LoadingController loadingController = LoadingController.Instance;
                        if (loadingController == null || !loadingController.IsInitialized)
                            break;
                        SetStatus(MainStatusType.Update);
                        loadingController.ShowLoadingScreen(CallbackLoadingScreen);
                        break;
                    case MainStatusType.Update:
                        break;
                    case MainStatusType.Exit:
                        OnExitMainState();
                        break;
                }
                break;

            case MainStateType.MainMenu:
                switch (m_mainStatus)
                {
                    case MainStatusType.Enter:
                        if (SceneManager.GetActiveScene().name != SceneController.MainMenuSceneName)
                        {
                            StartCoroutine(TransitionTo(MainStateType.MainMenu));
                            break;
                        }
                        if (UIManager.Instance == null)
                            break;
                        UIManager.Instance.HideAllUIPopup();
                        UIManager.Instance.ShowScreen(ScreenType.Home);
                        SetStatus(MainStatusType.Update);
                        break;
                    case MainStatusType.Update:
                        break;
                    case MainStatusType.Exit:
                        OnExitMainState();
                        break;
                }
                break;
            case MainStateType.Gameplay:
                switch (m_mainStatus)
                {
                    case MainStatusType.Enter:
                        if (SceneManager.GetActiveScene().name != SceneController.GameplaySceneName)
                        {
                            StartCoroutine(TransitionTo(MainStateType.Gameplay));
                            break;
                        }

                        GamePlayController gameController = GamePlayController.Instance;
                        if (gameController == null)
                        {
                            Debug.LogError("[MainStateManager] Gameplay scene needs a GamePlayController.", this);
                            SetStatus(MainStatusType.None);
                            break;
                        }

                        SetStatus(MainStatusType.Update);
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.HideAllUIPopup();
                            UIManager.Instance.ShowScreen(ScreenType.Gameplay);
                        }
                        gameController.StartLevel();
                        break;
                    case MainStatusType.Update:
                        // The session is ticked here only, once per frame.
                        if (GamePlayController.Instance != null)
                            GamePlayController.Instance.OnUpdate();
                        break;
                    case MainStatusType.Exit:
                        OnExitMainState();
                        break;
                }
                break;
        }

    }

    public void CallbackLoadingScreen()
    {
        SetMainState(MainStateType.MainMenu);
    }

    public bool TryStartGameplay(Action<bool> completed = null)
    {
        if (m_mainState != MainStateType.MainMenu || m_mainStatus != MainStatusType.Update ||
            m_isTransitioning || SceneController.Instance == null || SceneController.Instance.IsLoading)
            return false;

        StartCoroutine(TransitionTo(MainStateType.Gameplay, completed));
        return true;
    }

    private IEnumerator TransitionTo(MainStateType destination, Action<bool> completed = null)
    {
        SceneController sceneController = SceneController.Instance;
        if (sceneController == null || sceneController.IsLoading)
        {
            completed?.Invoke(false);
            yield break;
        }

        m_isTransitioning = true;
        Time.timeScale = 1f;
        SetStatus(MainStatusType.Update);

        if (UIManager.Instance != null)
            UIManager.Instance.HideAllUIPopup();

        try
        {
            // This manager lives on System and survives both source and destination scenes.
            switch (destination)
            {
                case MainStateType.Loading:
                    yield return sceneController.LoadStartup();
                    break;
                case MainStateType.MainMenu:
                    yield return sceneController.LoadMainMenu();
                    break;
                case MainStateType.Gameplay:
                    yield return sceneController.LoadGamePlay();
                    break;
            }
        }
        finally
        {
            m_isTransitioning = false;
        }

        bool succeeded = sceneController.LastLoadSucceeded;
        if (succeeded)
            SetMainState(destination);

        completed?.Invoke(succeeded);
    }

    #region State Management
    private void OnExitMainState()
    {
        if (NextMainState != MainStateType.None)
        {
            SetMainState(NextMainState);
        }
        else
        {
            SetStatus(MainStatusType.None);
        }
    }

    public void SetStatus(MainStatusType status, MainStateType nextMainState = MainStateType.None)
    {
        GameLog.Log(LogType.Log, $"[MainStateManager] SetStatus: current status {m_mainStatus} ==> new {status}");
        GameLog.Log(LogType.Log, $"[MainStateManager] SetStatus: current state {m_mainState} ==> new {nextMainState}");

        m_mainStatus = status;
        m_nextMainState = nextMainState;
    }

    public void SetMainState(MainStateType mainState)
    {
        GameLog.Log(LogType.Log, $"[MainStateManager] SetMainState: current state ==> new {mainState}");
        m_previousMainState = m_mainState;
        m_mainState = mainState;
        SetStatus(MainStatusType.Enter);
    }
    #endregion
}
