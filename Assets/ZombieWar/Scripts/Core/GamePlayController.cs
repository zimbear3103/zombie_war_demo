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

    private const float m_gameplayDuration = 180f;
    private const string m_runName = "Zombie War";

    [Header("Session References")]
    [SerializeField] private PlayerController m_playerController;
    [SerializeField] private ZombieSpawner m_zombieSpawner;
    [Tooltip("Optional. When omitted, the player's first observed pose is reused for every run.")]
    [SerializeField] private Transform m_playerSpawnPoint;

    [Header("Game State")]
    [SerializeField, ReadOnly] private GameStateType m_gameState = GameStateType.None;
    [SerializeField, ReadOnly] private GameStateType m_nextGameState = GameStateType.None;
    [SerializeField, ReadOnly] private ControlStatusType m_controlStatus = ControlStatusType.None;

    private GameStateType m_pauseReturnState = GameStateType.None;
    private GameStateType m_settingReturnState = GameStateType.None;
    private PlayerStats m_subscribedPlayerStats;
    private ZombieSpawner m_subscribedSpawner;
    private Coroutine m_resultCoroutine;
    private Vector3 m_initialPlayerPosition;
    private Quaternion m_initialPlayerRotation;
    private float m_remainingTime;
    private float m_savedTimeScale = 1f;
    private int m_killCount;
    private int m_runVersion;
    private bool m_hasInitialPlayerPose;
    private bool m_isPaused;
    private bool m_isRunActive;
    private bool m_isSettingOpen;
    private bool m_resultResolved;
    private bool m_resultShown;
    private bool m_timeScaleCaptured;
    private bool m_setupSucceeded;

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

    public int SelectedSongIndex { get; set; }

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
        CaptureInitialPlayerPose();
    }

    private void Start()
    {
        UserProfile userProfile = UserProfile.Instance;
        if (userProfile != null)
            userProfile.LoadGameData();

        CaptureInitialPlayerPose();
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

    public void OnSetupGameLevel(int songIndex)
    {
        SelectedSongIndex = songIndex;
        m_setupSucceeded = TryBeginRun();
    }

    private bool TryBeginRun()
    {
        CancelDelayedCallbacks();
        SetGameplayEnabled(false);

        if (SoundManager.HasInstance)
            SoundManager.Instance.StopAllGameplaySounds();

        if (m_zombieSpawner != null)
            m_zombieSpawner.EndRun();

        UnsubscribeRuntimeEvents();
        RestoreTimeScale();
        m_isRunActive = false;

        if (!HasRequiredReferences())
            return false;

        CaptureInitialPlayerPose();
        if (!m_hasInitialPlayerPose && m_playerSpawnPoint == null)
        {
            Debug.LogError("[GamePlayController] Cannot start: player spawn pose is unavailable.", this);
            return false;
        }

        Vector3 spawnPosition = m_playerSpawnPoint != null
            ? m_playerSpawnPoint.position
            : m_initialPlayerPosition;
        Quaternion spawnRotation = m_playerSpawnPoint != null
            ? m_playerSpawnPoint.rotation
            : m_initialPlayerRotation;

        m_playerController.ResetForRun(spawnPosition, spawnRotation);
        m_playerController.SetGameplayEnabled(false);
        SubscribeRuntimeEvents();

        m_remainingTime = m_gameplayDuration;
        m_killCount = 0;
        m_resultResolved = false;
        m_resultShown = false;
        LastRunResult = default;

        if (!m_zombieSpawner.BeginRun(m_playerController.transform, m_playerController.Stats))
        {
            Debug.LogError("[GamePlayController] Cannot start: ZombieSpawner configuration is not ready.", this);
            m_zombieSpawner.EndRun();
            UnsubscribeRuntimeEvents();
            return false;
        }

        m_zombieSpawner.SetGameplayEnabled(false);
        m_isRunActive = true;
        return true;
    }

    private bool HasRequiredReferences()
    {
        if (m_playerController == null)
        {
            Debug.LogError("[GamePlayController] Cannot start: PlayerController reference is missing.", this);
            return false;
        }

        if (!m_playerController.isActiveAndEnabled)
        {
            Debug.LogError("[GamePlayController] Cannot start: PlayerController must be active and enabled.", this);
            return false;
        }

        PlayerStats playerStats = m_playerController.Stats;
        if (playerStats == null)
        {
            Debug.LogError("[GamePlayController] Cannot start: PlayerStats is missing from PlayerController.", this);
            return false;
        }

        if (!playerStats.isActiveAndEnabled)
        {
            Debug.LogError("[GamePlayController] Cannot start: PlayerStats must be active and enabled.", this);
            return false;
        }

        if (m_zombieSpawner == null)
        {
            Debug.LogError("[GamePlayController] Cannot start: ZombieSpawner reference is missing.", this);
            return false;
        }

        return true;
    }

    private void CaptureInitialPlayerPose()
    {
        if (m_hasInitialPlayerPose || m_playerSpawnPoint != null || m_playerController == null)
            return;

        m_initialPlayerPosition = m_playerController.transform.position;
        m_initialPlayerRotation = m_playerController.transform.rotation;
        m_hasInitialPlayerPose = true;
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
            m_zombieSpawner == null || !m_zombieSpawner.isActiveAndEnabled || !m_zombieSpawner.IsRunning)
        {
            Debug.LogError("[GamePlayController] Run stopped because the player or zombie scheduler is unavailable.", this);
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
            SetGameControlStatus(ControlStatusType.Update);
            OnSetupGameLevel(SelectedSongIndex);

            if (m_setupSucceeded)
                SetGameControlStatus(ControlStatusType.Exit, GameStateType.Playing);
            else
                SetGameState(GameStateType.None);
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
            int previousBest = userProfile.GetBestScore(m_runName);
            isNewBest = userProfile.SubmitSongScore(m_runName, m_killCount);
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
        OnFreeData();

        UserProfile userProfile = UserProfile.Instance;
        if (userProfile != null)
            userProfile.SaveGameData();

        SetGameControlStatus(ControlStatusType.Exit, GameStateType.StartLevel);
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
            m_zombieSpawner.EndRun();

        UnsubscribeRuntimeEvents();
        RestoreTimeScale();
        m_isPaused = false;
        m_isRunActive = false;
        m_isSettingOpen = false;
        m_resultResolved = false;
        m_setupSucceeded = false;
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
