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
        Gameplay
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

    public MainStateType MainState => m_mainState;
    public MainStateType NextMainState => m_nextMainState;
    public MainStatusType MainStatus => m_mainStatus;
    public bool IsTransitioning => m_isTransitioning;
    public string LastStartError { get; private set; }

    private IEnumerator Start()
    {
        // Let scene-owned references initialize before showing either screen.
        yield return null;
        Time.timeScale = 1f;

        string startingScene = SceneManager.GetActiveScene().name;
        if (startingScene == SceneController.GameplaySceneName)
        {
            // Direct Editor play uses the same Home -> Start flow as a build.
            SetMainState(MainStateType.MainMenu);
        }
        else if (startingScene == SceneController.LoadingSceneName)
        {
            SetMainState(MainStateType.Loading);
        }
        else
        {
            yield return LoadHomeScene();
        }
    }

    private void Update()
    {
        if (m_isTransitioning)
            return;

        if (m_mainStatus == MainStatusType.Exit)
        {
            OnExitMainState();
            return;
        }

        switch (m_mainState)
        {
            case MainStateType.Loading:
                if (m_mainStatus != MainStatusType.Enter)
                    break;
                LoadingController loading = LoadingController.Instance;
                if (loading == null || !loading.IsInitialized)
                    break;
                SetStatus(MainStatusType.Update);
                loading.ShowLoadingScreen(CallbackLoadingScreen);
                break;

            case MainStateType.MainMenu:
                if (m_mainStatus != MainStatusType.Enter)
                    break;
                if (SceneManager.GetActiveScene().name != SceneController.GameplaySceneName)
                {
                    StartCoroutine(LoadHomeScene());
                    break;
                }
                if (UIManager.Instance == null)
                    break;
                GamePlayController.Instance?.OnFreeData();
                UIManager.Instance.HideAllUIPopup();
                UIManager.Instance.ShowScreen(ScreenType.Home);
                SetStatus(MainStatusType.Update);
                break;

            case MainStateType.Gameplay:
                GamePlayController gameController = GamePlayController.Instance;
                if (m_mainStatus == MainStatusType.Enter)
                {
                    if (gameController == null || !gameController.IsRunActive)
                    {
                        ReturnHome("Select a level and press Start to begin.");
                        break;
                    }
                    UIManager.Instance.HideAllUIPopup();
                    UIManager.Instance.ShowScreen(ScreenType.Gameplay);
                    SetStatus(MainStatusType.Update);
                }
                else if (m_mainStatus == MainStatusType.Update && gameController != null)
                {
                    // The session is ticked here only, once per frame.
                    gameController.OnUpdate();
                }
                break;
        }
    }

    public void CallbackLoadingScreen()
    {
        LastStartError = null;
        SetMainState(MainStateType.MainMenu);
    }

    public bool TryStartGameplay(Action<bool> completed = null)
    {
        if (m_mainState != MainStateType.MainMenu || m_mainStatus != MainStatusType.Update ||
            m_isTransitioning || SceneManager.GetActiveScene().name != SceneController.GameplaySceneName)
            return false;

        m_isTransitioning = true;
        bool succeeded = false;
        try
        {
            GamePlayController controller = GamePlayController.Instance;
            UIManager ui = UIManager.Instance;
            UIScreen home = ui != null ? ui.GetUIScreen(ScreenType.Home) : null;
            UIScreen hud = ui != null ? ui.GetUIScreen(ScreenType.Gameplay) : null;
            if (controller == null)
            {
                LastStartError = "Gameplay scene needs a GamePlayController.";
            }
            else if (home == null || home.Panel == null || hud == null || hud.Panel == null)
            {
                LastStartError = "Assign Home and Gameplay screens with their panels on UIManager.";
            }
            else
            {
                succeeded = controller.TryStartLevel(out string error);
                LastStartError = error;
            }

            if (succeeded)
                SetMainState(MainStateType.Gameplay);
        }
        finally
        {
            m_isTransitioning = false;
        }

        completed?.Invoke(succeeded);
        return true;
    }

    public void ReturnHome(string error = null)
    {
        GamePlayController.Instance?.OnFreeData();
        LastStartError = error;
        SetMainState(MainStateType.MainMenu);
    }

    private IEnumerator LoadHomeScene()
    {
        SceneController sceneController = SceneController.Instance;
        if (sceneController == null || sceneController.IsLoading)
            yield break;

        m_isTransitioning = true;
        try
        {
            yield return sceneController.LoadGamePlay();
            if (sceneController.LastLoadSucceeded)
                CallbackLoadingScreen();
            else
                SetStatus(MainStatusType.None);
        }
        finally
        {
            m_isTransitioning = false;
        }
    }

    private void OnExitMainState()
    {
        if (m_nextMainState != MainStateType.None)
            SetMainState(m_nextMainState);
        else
            SetStatus(MainStatusType.None);
    }

    public void SetStatus(MainStatusType status, MainStateType nextMainState = MainStateType.None)
    {
        m_mainStatus = status;
        m_nextMainState = nextMainState;
    }

    public void SetMainState(MainStateType mainState)
    {
        m_previousMainState = m_mainState;
        m_mainState = mainState;
        SetStatus(MainStatusType.Enter);
    }
}
