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

    [Header("Game State")]
    [SerializeField, ReadOnly] GameStateType m_gameState = GameStateType.None;
    [SerializeField, ReadOnly] GameStateType m_nextGameState = GameStateType.None;
    [SerializeField, ReadOnly] ControlStatusType m_controlStatus = ControlStatusType.None;

    private GameStateType m_saveGameState = GameStateType.None;

    private bool m_isPaused;
    private bool m_resultShown;

    private float m_playerDeadWaitingTime = 0.5f;

    public bool IsGamePlaying { set; get; } = false;
    public bool SaveIsGamePlaying { set; get; } = false;
    public bool AutoPlay { get; set; }
    public bool IsPaused => m_isPaused;
    public GameStateType GameState => m_gameState;
    public GameStateType NextGameState => m_nextGameState;

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

    private void Start()
    {
        UserProfile.Instance.LoadGameData();
    }

    public void OnSetupGameLevel(int songIndex)
    {
        GameLog.Log(LogType.Log, $"[GamePlayController] Setup song {songIndex}");

        if (SoundManager.Instance != null)
            SoundManager.Instance.StopMusic();
    }

    private void OnExitInGameState()
    {
        GameLog.Log(LogType.Log, $"-----Game controller------Exit state-----------:{GameState}  => next menu state: {NextGameState}  => next Main State: {MainStateManager.Instance.NextMainState}");

        if (MainStateManager.Instance.NextMainState != MainStateManager.MainStateType.None)
        {
            MainStateManager.Instance.SetStatus(MainStateManager.MainStatusType.Exit, MainStateManager.Instance.NextMainState);
            SetGameState(GameStateType.None);
            SetGameControlStatus(ControlStatusType.None);
        }
        else
        {
            if (NextGameState != GameStateType.None)
            {
                SetGameState(NextGameState);
            }
            else
            {
                SetGameControlStatus(ControlStatusType.None);
            }
        }
    }

    public void OnUpdate()
    {
        switch (m_gameState)
        {
            case GameStateType.None:
                {
                    switch (m_controlStatus)
                    {
                        case ControlStatusType.Enter:
                            {
                                GameLog.Log(LogType.Log, $"-----Ingame------Enter state-----------:{m_gameState}");
                                SetGameControlStatus(ControlStatusType.Update);
                            }
                            break;
                        case ControlStatusType.Exit:
                            {
                                OnExitInGameState();
                            }
                            break;
                    }
                }
                break;
            case GameStateType.Playing:
                {
                    switch (m_controlStatus)
                    {
                        case ControlStatusType.Enter:
                            {
                                GameLog.Log(LogType.Log, $"-----Ingame------Enter state-----------:{m_gameState}");
                                SetGameControlStatus(ControlStatusType.Update);
                                IsGamePlaying = true;
                            }
                            break;
                        case ControlStatusType.Update:
                            break;
                        case ControlStatusType.Exit:
                            {
                                OnExitInGameState();
                            }
                            break;
                    }
                }
                break;
            case GameStateType.StartLevel:
                {
                    switch (m_controlStatus)
                    {
                        case ControlStatusType.Enter:
                            {
                                GameLog.Log(LogType.Log, $"-----Ingame------Enter state-----------:{m_gameState}");
                                SetGameControlStatus(ControlStatusType.Exit, GameStateType.Playing);
                                OnFreeData();
                                m_resultShown = false;
                                OnSetupGameLevel(SelectedSongIndex);
                            }
                            break;
                        case ControlStatusType.Exit:
                            {
                                OnExitInGameState();
                            }
                            break;
                    }
                }
                break;

            case GameStateType.Lose:
                {
                    switch (m_controlStatus)
                    {
                        case ControlStatusType.Enter:
                            {
                                GameLog.Log(LogType.Log, $"-----Ingame------Enter state-----------:{m_gameState}");
                                SetGameControlStatus(ControlStatusType.Update);
                                if (!m_resultShown)
                                {
                                    m_resultShown = true;
                                    UIManager.Instance.ShowPopup(PopupType.LevelComplete);
                                }
                            }
                            break;
                        case ControlStatusType.Exit:
                            {
                                OnExitInGameState();
                            }
                            break;
                    }
                }
                break;

            case GameStateType.Win:
                {
                    switch (m_controlStatus)
                    {
                        case ControlStatusType.Enter:
                            {
                                GameLog.Log(LogType.Log, $"-----Ingame------Enter state-----------:{m_gameState}");
                                SetGameControlStatus(ControlStatusType.Update);
                                if (!m_resultShown)
                                {
                                    m_resultShown = true;
                                    UIManager.Instance.ShowPopup(PopupType.LevelComplete);
                                }
                            }
                            break;
                        case ControlStatusType.Exit:
                            {
                                OnExitInGameState();
                            }
                            break;
                    }
                }
                break;

            case GameStateType.Pause:
                {
                    switch (m_controlStatus)
                    {
                        case ControlStatusType.Enter:
                            {
                                GameLog.Log(LogType.Log, $"-----Ingame------Enter state-----------:{m_gameState}");
                                SetGameControlStatus(ControlStatusType.Update);
                            }
                            break;
                        case ControlStatusType.Exit:
                            {
                                OnExitInGameState();
                            }
                            break;
                    }
                }
                break;
            case GameStateType.SettingIngame:
                {
                    switch (m_controlStatus)
                    {
                        case ControlStatusType.Enter:
                            {
                                GameLog.Log(LogType.Log, $"-----Ingame------Enter state-----------:{m_gameState}");
                                SetGameControlStatus(ControlStatusType.Update);
                                UIManager.Instance.ShowPopup(PopupType.Setting);
                            }
                            break;
                        case ControlStatusType.Exit:
                            {
                                OnExitInGameState();
                            }
                            break;
                    }
                }
                break;
            case GameStateType.QuitLevel:
                {
                    switch (m_controlStatus)
                    {
                        case ControlStatusType.Enter:
                            {
                                GameLog.Log(LogType.Log, $"-----Ingame------Enter state-----------:{m_gameState}");
                                SetGameControlStatus(ControlStatusType.Update);
                                OnFreeData();
                                MainStateManager.Instance.SetStatus(MainStateManager.MainStatusType.Exit, MainStateManager.MainStateType.MainMenu);
                            }
                            break;
                        case ControlStatusType.Exit:
                            {
                                OnExitInGameState();
                            }
                            break;
                    }
                }
                break;
        }
    }

    public void SetGameState(GameStateType inGameState)
    {
        GameLog.Log(LogType.Log, $"[GameController]  OnSetInGameState InGameState = {GameState} New InGameState = {inGameState} ");
        m_gameState = inGameState;
        SetGameControlStatus(ControlStatusType.Enter);
    }

    private void SetGameControlStatus(ControlStatusType statusInGameState, GameStateType nextInGameState = GameStateType.None)
    {
        GameLog.Log(LogType.Log, $"[GameController] OnSetInGameStatus SetStatus: previous {m_controlStatus} ==> {statusInGameState}, next state: {nextInGameState}");
        m_controlStatus = statusInGameState;
        m_nextGameState = nextInGameState;
    }

    public void QuitLevel()
    {
        IsGamePlaying = false;
        SetGameControlStatus(ControlStatusType.Exit, GameStateType.QuitLevel);
    }

    public IEnumerator PlayerLose(float waitTime)
    {
        IsGamePlaying = false;
        yield return new WaitForSeconds(waitTime);
        SetGameControlStatus(ControlStatusType.Exit, GameStateType.Lose);
    }

    public void PlayerWin()
    {
        IsGamePlaying = false;
        SetGameControlStatus(ControlStatusType.Exit, GameStateType.Win);
    }


    public void ForceWin() { if (IsGamePlaying) PlayerWin(); }
    public void ForceLose() { if (IsGamePlaying) StartCoroutine(PlayerLose(m_playerDeadWaitingTime)); }

    public void StartLevel()
    {
        UserProfile.Instance.SaveGameData();
        SetGameControlStatus(ControlStatusType.Exit, GameStateType.StartLevel);
    }

    public void RestartLevel()
    {
        IsGamePlaying = false;
        StartLevel();
    }

    public void EnterSettingPopupStatus()
    {
        SaveIsGamePlaying = IsGamePlaying;
        m_saveGameState = m_gameState;
        IsGamePlaying = false;

        SetGameControlStatus(ControlStatusType.Exit, GameStateType.SettingIngame);
    }

    public void LeaveSettingPopupStatus()
    {
        SetGameControlStatus(ControlStatusType.Exit, m_saveGameState);
        IsGamePlaying = SaveIsGamePlaying;

    }

    public void OnPause()
    {
        if (!m_isPaused)
        {
            SaveIsGamePlaying = IsGamePlaying;
            m_isPaused = true;
            IsGamePlaying = false;
            m_saveGameState = m_gameState;

            SetGameControlStatus(ControlStatusType.Exit, GameStateType.Pause);
        }
    }

    public void OnResume()
    {
        if (m_isPaused)
        {
            m_isPaused = false;
            IsGamePlaying = SaveIsGamePlaying;
            m_saveGameState = GameStateType.None;

            SetGameControlStatus(ControlStatusType.Exit, GameStateType.Playing);
        }
    }

    public void OnFreeData()
    {
        Time.timeScale = 1f;
        m_isPaused = false;
    }

    public UIInGame OnGetUIIngame()
    {
        var uiIngame = (UIInGame)UIManager.Instance.GetUIScreen(ScreenType.Gameplay);
        return uiIngame;
    }
}
