using System.Collections;
using UnityEngine;

public class GamePlayController : Singleton<GamePlayController>
{
    public enum GameStateType
    {
        None = 0,
        Playing,
        SettingIngame,
        Pause,
        StartLevel,
        Win,
        Lose,
        QuitLevel
    }

    public enum ControlStatusType
    {
        None = 0,
        Enter,
        Update,
        Exit
    }

    [Header("Session References")]
    [SerializeField] private ZombieSpawner m_zombieSpawner;

    [Header("Game State")]
    [SerializeField, ReadOnly] private GameStateType m_gameState = GameStateType.None;
    [SerializeField, ReadOnly] private GameStateType m_nextGameState = GameStateType.None;
    [SerializeField, ReadOnly] private ControlStatusType m_controlStatus = ControlStatusType.None;

    private GameStateType m_pauseReturnState = GameStateType.None;
    private GameStateType m_settingReturnState = GameStateType.None;
    private PlayerStats m_subscribedPlayerStats;
    private ZombieSpawner m_subscribedSpawner;
    private Coroutine m_resultCoroutine;
    private string m_runName;
    private string m_runScoreKey;
    private float m_remainingTime;
    private float m_savedTimeScale = 1f;
    private int m_killCount;
    private int m_runVersion;
    private bool m_isPaused;
    private bool m_isRunActive;
    private bool m_isSettingOpen;
    private bool m_resultResolved;
    private bool m_resultShown;
    private bool m_timeScaleCaptured;
    private PlayerController m_playerController;
    private float m_playerDeadWaitingTime = 0.5f;

    public bool IsGamePlaying { set; get; }
    public bool SaveIsGamePlaying { set; get; }
    public bool AutoPlay { get; set; }
    public bool IsPaused => m_isPaused;
    public GameStateType GameState => m_gameState;
    public GameStateType NextGameState => m_nextGameState;
    public float RemainingTime => m_remainingTime;
    public int KillCount => m_killCount;
    public PlayerController Player => m_playerController;
    public bool IsRunActive => m_isRunActive;
    public string LastSetupError { get; private set; }

    public struct RunResult
    {
        public string songName;
        public int score;
        public int bestScore;
        public int stars;
        public bool isWin;
        public bool isNewBest;
    }

    public RunResult LastRunResult { get; private set; }

    public void SetRunResult(RunResult result)
    {
        LastRunResult = result;
    }

    private void Awake()
    {
        SetGameplayEnabled(false);
    }

    private void Start()
    {
        UserProfile userProfile = UserProfile.Instance;
        if (userProfile != null)
            userProfile.LoadGameData();
    }

    private void OnDisable()
    {
        OnFreeData();
    }

    protected override void OnDestroy()
    {
        OnFreeData();
        base.OnDestroy();
    }

    public bool TryStartLevel(out string error)
    {
        OnFreeData();
        SetGameState(GameStateType.None);
        SetGameControlStatus(ControlStatusType.None);
        LastSetupError = null;
        m_remainingTime = 0f;

        if (!HasRequiredReferences(out error))
            return FailSetup(error);

        if (!LevelManager.Instance.TryPrepareSelectedLevel(out error))
            return FailSetup(error);

        LevelScriptableObject level = LevelManager.Instance.CurrentLevel;
        LevelMap map = LevelManager.Instance.CurrentMap;
        Transform spawnPoint = map.PlayerSpawnPoint;
        m_playerController = LevelManager.Instance.CurrentPlayer;
        if (!m_playerController.ResetForRun(spawnPoint.position, spawnPoint.rotation))
        {
            error = "The player loadout could not be prepared. Check the starting weapon, socket and weapon prefab.";
            return FailSetup(error);
        }
        m_zombieSpawner.ConfigureLevel(level, map);

        if (!m_zombieSpawner.BeginRun(m_playerController.transform, m_playerController.Stats))
        {
            error = "Zombie spawning could not be prepared. Check the level waves, pool and NavMesh.";
            return FailSetup(error);
        }

        m_zombieSpawner.SetGameplayEnabled(false);
        SubscribeRuntimeEvents();
        m_remainingTime = level.SurvivalDuration;
        m_runName = level.LevelName;
        m_runScoreKey = $"level_{level.LevelId}";
        m_killCount = 0;
        m_resultResolved = false;
        m_resultShown = false;
        LastRunResult = default;

        m_isRunActive = true;
        // MainStateManager shows the HUD before the next gameplay tick opens input and spawning.
        SetGameState(GameStateType.Playing);
        error = null;
        return true;
    }

    private bool FailSetup(string error)
    {
        OnFreeData();
        LastSetupError = error;
        return false;
    }

    private bool HasRequiredReferences(out string error)
    {
        if (!isActiveAndEnabled)
        {
            error = "GamePlayController must be active and enabled.";
            return false;
        }

        if (LevelManager.Instance == null || !LevelManager.Instance.isActiveAndEnabled)
        {
            error = "The gameplay scene needs an active LevelManager with a Player Prefab.";
            return false;
        }

        if (m_zombieSpawner == null || !m_zombieSpawner.isActiveAndEnabled)
        {
            error = "Assign an active ZombieSpawner on GamePlayController.";
            return false;
        }

        error = null;
        return true;
    }

    private void SubscribeRuntimeEvents()
    {
        PlayerStats playerStats = m_playerController != null ? m_playerController.Stats : null;
        if (m_subscribedPlayerStats != playerStats)
        {
            if (m_subscribedPlayerStats != null)
                m_subscribedPlayerStats.Died -= OnPlayerDied;

            m_subscribedPlayerStats = playerStats;
            if (m_subscribedPlayerStats != null)
                m_subscribedPlayerStats.Died += OnPlayerDied;
        }

        if (m_subscribedSpawner != m_zombieSpawner)
        {
            if (m_subscribedSpawner != null)
                m_subscribedSpawner.ZombieKilled -= OnZombieKilled;

            m_subscribedSpawner = m_zombieSpawner;
            if (m_subscribedSpawner != null)
                m_subscribedSpawner.ZombieKilled += OnZombieKilled;
        }
    }

    private void UnsubscribeRuntimeEvents()
    {
        if (m_subscribedPlayerStats != null)
        {
            m_subscribedPlayerStats.Died -= OnPlayerDied;
            m_subscribedPlayerStats = null;
        }

        if (m_subscribedSpawner != null)
        {
            m_subscribedSpawner.ZombieKilled -= OnZombieKilled;
            m_subscribedSpawner = null;
        }
    }

    private void OnPlayerDied()
    {
        if (!m_isRunActive || m_resultResolved)
            return;

        m_resultCoroutine = StartCoroutine(PlayerLose(m_playerDeadWaitingTime));
    }

    private void OnZombieKilled(ZombieController zombie)
    {
        if (m_isRunActive && !m_resultResolved)
            m_killCount++;
    }

    private void OnExitInGameState()
    {
        MainStateManager mainStateManager = MainStateManager.Instance;
        if (mainStateManager != null &&
            mainStateManager.NextMainState != MainStateManager.MainStateType.None)
        {
            mainStateManager.SetStatus(
                MainStateManager.MainStatusType.Exit,
                mainStateManager.NextMainState);
            SetGameState(GameStateType.None);
            SetGameControlStatus(ControlStatusType.None);
            return;
        }

        if (NextGameState != GameStateType.None)
            SetGameState(NextGameState);
        else
            SetGameControlStatus(ControlStatusType.None);
    }

    public void OnUpdate()
    {
        switch (m_gameState)
        {
            case GameStateType.None:
                HandleNoneState();
                break;
            case GameStateType.Playing:
                HandlePlayingState();
                break;
            case GameStateType.StartLevel:
                HandleStartLevelState();
                break;
            case GameStateType.Lose:
                HandleResultState();
                break;
            case GameStateType.Win:
                HandleResultState();
                break;
            case GameStateType.Pause:
                HandlePauseState();
                break;
            case GameStateType.SettingIngame:
                HandleSettingState();
                break;
            case GameStateType.QuitLevel:
                HandleQuitLevelState();
                break;
        }
    }

    private void HandleNoneState()
    {
        if (m_controlStatus == ControlStatusType.Enter)
            SetGameControlStatus(ControlStatusType.Update);
        else if (m_controlStatus == ControlStatusType.Exit)
            OnExitInGameState();
    }

    private void HandlePlayingState()
    {
        switch (m_controlStatus)
        {
            case ControlStatusType.Enter:
                SetGameControlStatus(ControlStatusType.Update);
                SetGameplayEnabled(m_isRunActive && !m_resultResolved);
                break;
            case ControlStatusType.Update:
                TickPlaying();
                break;
            case ControlStatusType.Exit:
                OnExitInGameState();
                break;
        }
    }

    private void TickPlaying()
    {
        if (!m_isRunActive || m_resultResolved || !IsGamePlaying)
            return;

        if (m_playerController == null || !m_playerController.isActiveAndEnabled ||
            m_playerController.Stats == null || !m_playerController.Stats.isActiveAndEnabled ||
            m_zombieSpawner == null || !m_zombieSpawner.isActiveAndEnabled || !m_zombieSpawner.IsRunning ||
            LevelManager.Instance == null || !LevelManager.Instance.isActiveAndEnabled ||
            LevelManager.Instance.CurrentMap == null || !LevelManager.Instance.CurrentMap.isActiveAndEnabled)
        {
            Debug.LogError("[GamePlayController] Run stopped because the player, map or zombie scheduler is unavailable.", this);
            m_resultCoroutine = StartCoroutine(PlayerLose(0f));
            return;
        }

        m_remainingTime = Mathf.Max(0f, m_remainingTime - Time.deltaTime);
        if (m_remainingTime > 0f)
            return;

        if (m_playerController != null &&
            m_playerController.Stats != null &&
            m_playerController.Stats.IsAlive)
        {
            PlayerWin();
        }
        else
        {
            m_resultCoroutine = StartCoroutine(PlayerLose(0f));
        }
    }

    private void HandleStartLevelState()
    {
        if (m_controlStatus == ControlStatusType.Enter)
        {
            StartLevel();
        }
        else if (m_controlStatus == ControlStatusType.Exit)
        {
            OnExitInGameState();
        }
    }

    private void HandleResultState()
    {
        if (m_controlStatus == ControlStatusType.Enter)
        {
            SetGameControlStatus(ControlStatusType.Update);
            ShowResultPopup();
        }
        else if (m_controlStatus == ControlStatusType.Exit)
        {
            OnExitInGameState();
        }
    }

    private void ShowResultPopup()
    {
        if (m_resultShown)
            return;

        m_resultShown = true;
        UIManager uiManager = UIManager.Instance;
        if (uiManager != null)
            uiManager.ShowPopup(PopupType.LevelComplete);
    }

    private void HandlePauseState()
    {
        if (m_controlStatus == ControlStatusType.Enter)
            SetGameControlStatus(ControlStatusType.Update);
        else if (m_controlStatus == ControlStatusType.Exit)
            OnExitInGameState();
    }

    private void HandleSettingState()
    {
        if (m_controlStatus == ControlStatusType.Enter)
        {
            SetGameControlStatus(ControlStatusType.Update);
            UIManager uiManager = UIManager.Instance;
            if (uiManager != null)
                uiManager.ShowPopup(PopupType.Setting);
        }
        else if (m_controlStatus == ControlStatusType.Exit)
        {
            OnExitInGameState();
        }
    }

    private void HandleQuitLevelState()
    {
        if (m_controlStatus == ControlStatusType.Enter)
        {
            SetGameControlStatus(ControlStatusType.Update);
            OnFreeData();

            MainStateManager mainStateManager = MainStateManager.Instance;
            if (mainStateManager != null)
            {
                mainStateManager.SetStatus(
                    MainStateManager.MainStatusType.Exit,
                    MainStateManager.MainStateType.MainMenu);
            }
            else
            {
                SetGameState(GameStateType.None);
            }
        }
        else if (m_controlStatus == ControlStatusType.Exit)
        {
            OnExitInGameState();
        }
    }

    public void SetGameState(GameStateType inGameState)
    {
        m_gameState = inGameState;
        SetGameControlStatus(ControlStatusType.Enter);
    }

    private void SetGameControlStatus(
        ControlStatusType statusInGameState,
        GameStateType nextInGameState = GameStateType.None)
    {
        m_controlStatus = statusInGameState;
        m_nextGameState = nextInGameState;
    }

    public void QuitLevel()
    {
        OnFreeData();
        SetGameControlStatus(ControlStatusType.Exit, GameStateType.QuitLevel);
    }

    public IEnumerator PlayerLose(float waitTime)
    {
        int runVersion = m_runVersion;
        if (!ResolveRun(false))
            yield break;

        if (waitTime > 0f)
            yield return new WaitForSecondsRealtime(waitTime);

        if (runVersion != m_runVersion || !m_resultResolved || LastRunResult.isWin)
            yield break;

        m_resultCoroutine = null;
        SetGameControlStatus(ControlStatusType.Exit, GameStateType.Lose);
    }

    public void PlayerWin()
    {
        if (!ResolveRun(true))
            return;

        SetGameControlStatus(ControlStatusType.Exit, GameStateType.Win);
    }

    public void ForceWin()
    {
        if (IsGamePlaying)
            PlayerWin();
    }

    public void ForceLose()
    {
        if (IsGamePlaying)
            m_resultCoroutine = StartCoroutine(PlayerLose(m_playerDeadWaitingTime));
    }

    private bool ResolveRun(bool isWin)
    {
        if (!m_isRunActive || m_resultResolved)
            return false;

        m_resultResolved = true;
        m_isRunActive = false;
        SetGameplayEnabled(false);
        m_zombieSpawner?.EndRun();
        RestoreTimeScale();
        RecordRunResult(isWin);
        return true;
    }

    private void RecordRunResult(bool isWin)
    {
        int bestScore = m_killCount;
        bool isNewBest = false;
        UserProfile userProfile = UserProfile.Instance;

        if (userProfile != null)
        {
            int previousBest = userProfile.GetBestScore(m_runScoreKey);
            isNewBest = userProfile.SubmitSongScore(m_runScoreKey, m_killCount);
            bestScore = Mathf.Max(previousBest, m_killCount);
        }

        LastRunResult = new RunResult
        {
            songName = m_runName,
            score = m_killCount,
            bestScore = bestScore,
            stars = 0,
            isWin = isWin,
            isNewBest = isNewBest
        };
    }

    public void StartLevel()
    {
        if (TryStartLevel(out string error))
            return;

        Debug.LogError($"[GamePlayController] Cannot start: {error}", this);
        MainStateManager mainStateManager = MainStateManager.Instance;
        if (mainStateManager != null)
            mainStateManager.ReturnHome(error);
    }

    public void RestartLevel()
    {
        StartLevel();
    }

    public void EnterSettingPopupStatus()
    {
        if (!m_isRunActive || m_resultResolved || m_isSettingOpen || m_isPaused ||
            m_gameState != GameStateType.Playing || m_controlStatus != ControlStatusType.Update || !IsGamePlaying)
            return;

        SaveIsGamePlaying = IsGamePlaying;
        m_settingReturnState = m_gameState;
        m_isSettingOpen = true;
        SetGameplayEnabled(false);
        RefreshTimeScaleLock();
        SetGameControlStatus(ControlStatusType.Exit, GameStateType.SettingIngame);
    }

    public void LeaveSettingPopupStatus()
    {
        if (!m_isSettingOpen)
            return;

        GameStateType returnState = m_settingReturnState == GameStateType.None
            ? GameStateType.Playing
            : m_settingReturnState;

        m_isSettingOpen = false;
        m_settingReturnState = GameStateType.None;
        if (m_isPaused)
        {
            m_pauseReturnState = returnState;
            RefreshTimeScaleLock();
            return;
        }

        RefreshTimeScaleLock();
        if (!m_isRunActive || m_resultResolved)
            return;

        SetGameControlStatus(ControlStatusType.Exit, returnState);
    }

    public void OnPause()
    {
        if (m_isPaused || !m_isRunActive || m_resultResolved)
            return;

        if (!m_isSettingOpen && (m_gameState != GameStateType.Playing ||
            m_controlStatus != ControlStatusType.Update || !IsGamePlaying))
            return;

        if (!m_isSettingOpen)
            SaveIsGamePlaying = IsGamePlaying;

        m_isPaused = true;
        m_pauseReturnState = m_isSettingOpen
            ? GameStateType.SettingIngame
            : m_gameState;
        SetGameplayEnabled(false);
        RefreshTimeScaleLock();
        SetGameControlStatus(ControlStatusType.Exit, GameStateType.Pause);
    }

    public void OnResume()
    {
        if (!m_isPaused)
            return;

        GameStateType returnState = m_pauseReturnState == GameStateType.None
            ? GameStateType.Playing
            : m_pauseReturnState;

        m_isPaused = false;
        m_pauseReturnState = GameStateType.None;
        RefreshTimeScaleLock();
        if (!m_isRunActive || m_resultResolved)
            return;

        SetGameControlStatus(ControlStatusType.Exit, returnState);
    }

    private void RefreshTimeScaleLock()
    {
        if (m_isPaused || m_isSettingOpen)
        {
            if (!m_timeScaleCaptured)
            {
                m_savedTimeScale = Time.timeScale;
                m_timeScaleCaptured = true;
            }

            Time.timeScale = 0f;
            return;
        }

        RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!m_timeScaleCaptured)
            return;

        Time.timeScale = m_savedTimeScale;
        m_timeScaleCaptured = false;
    }

    private void SetGameplayEnabled(bool value)
    {
        IsGamePlaying = value;

        if (m_playerController != null)
            m_playerController.SetGameplayEnabled(value);

        if (m_zombieSpawner != null)
            m_zombieSpawner.SetGameplayEnabled(value);
    }

    private void CancelDelayedCallbacks()
    {
        m_runVersion++;

        if (m_resultCoroutine == null)
            return;

        StopCoroutine(m_resultCoroutine);
        m_resultCoroutine = null;
    }

    public void OnFreeData()
    {
        CancelDelayedCallbacks();
        SetGameplayEnabled(false);

        if (SoundManager.HasInstance)
            SoundManager.Instance.StopAllGameplaySounds();

        if (m_zombieSpawner != null)
        {
            m_zombieSpawner.EndRun();
            m_zombieSpawner.ClearLevelConfiguration();
        }

        UnsubscribeRuntimeEvents();
        if (m_playerController != null)
            m_playerController.EndRun();
        if (LevelManager.Instance != null)
            LevelManager.Instance.ClearLevel();
        m_playerController = null;
        RestoreTimeScale();
        m_isPaused = false;
        m_isRunActive = false;
        m_isSettingOpen = false;
        m_resultResolved = false;
        m_pauseReturnState = GameStateType.None;
        m_settingReturnState = GameStateType.None;
        SaveIsGamePlaying = false;
    }

    public UIInGame OnGetUIIngame()
    {
        UIManager uiManager = UIManager.Instance;
        return uiManager != null
            ? uiManager.GetUIScreen(ScreenType.Gameplay) as UIInGame
            : null;
    }
}
