using System;
using System.Collections.Generic;
using UnityEngine;

public class TileSpawner : Singleton<TileSpawner>
{
    [SerializeField] private GameObject m_tilePrefab;
    [SerializeField] private RectTransform m_tileContainer;
    [SerializeField] private RectTransform[] m_lanes;
    [Tooltip("Zone tiles must be tapped in — its bottom edge is the miss boundary")]
    [SerializeField] private TapZone m_tapZone;

    [Header("Fall Config")]
    [Tooltip("Fall speed in canvas units per second")]
    [SerializeField] private float m_fallSpeed = 900f;

    [Header("Effects (drag & drop, optional)")]
    [Tooltip("Particle prefab played at the tile position on a hit. Set Stop Action = Disable on the ParticleSystem so it returns to the pool by itself.")]
    [SerializeField] private GameObject m_tapEffectPrefab;
    [Tooltip("Parent for spawned effects — place it above the tiles in the hierarchy so effects draw on top")]
    [SerializeField] private Transform m_effectContainer;
    [SerializeField] private ComboEffect m_comboEffect;
    [SerializeField] private JudgeEffect m_JudgeLevelEffect;
    [Tooltip("Confetti burst prefab, pooled like the other effects. Set Stop Action = Disable on its ParticleSystem so it returns to the pool by itself.")]
    [SerializeField] private GameObject m_confettiEffect;
    [Tooltip("Parent for spawned confetti — its position also pins the burst height, only X follows the lane")]
    [SerializeField] private Transform m_confettiContainer;
    [Tooltip("Judges each tap (Perfect/Great/Miss grade) by distance to the line and pulses on hit")]
    [SerializeField] private HitLine m_hitLine;

    [Header("Scoring")]
    [Tooltip("Score multiplier cap for consecutive Perfect hits: 2 in a row = x2, 3+ in a row = x3")]
    [SerializeField] private int m_maxScoreMultiplier = 3;

    private readonly List<Tile> m_activeTiles = new();
    private int m_nextNoteIndex;
    private int m_hitCount;
    private int m_perfectStreak;
    private bool m_autoPlayUsed;

    private float[] m_laneSpawnY;
    private float[] m_laneHitY;
    private float[] m_laneMissY;
    private float m_travelTime;
    private Camera m_canvasCamera;

    private GamePlayController.GameStateType m_lastGameState = GamePlayController.GameStateType.None;

    public int Score { get; private set; }
    public Action<int> OnScoreChanged;

    private void Update()
    {
        var controller = GamePlayController.Instance;
        if (controller == null)
            return;

        if (controller.GameState != m_lastGameState)
        {
            m_lastGameState = controller.GameState;
            if (m_lastGameState == GamePlayController.GameStateType.StartLevel)
                ResetRun();
        }

        if (!controller.IsGamePlaying)
            return;

        SpawnLoop();
        if (controller.AutoPlay)
            AutoPlayLoop();
        CheckWin(controller);
    }

    private void SpawnLoop()
    {
        var loader = BeatMapLoader.Instance;
        var conductor = Conductor.Instance;
        if (loader == null || conductor == null || loader.CurrentMap == null || !conductor.HasSong)
            return;

        var notes = loader.CurrentMap.notes;
        float songPosition = conductor.SongPosition;

        while (m_nextNoteIndex < notes.Count && songPosition >= notes[m_nextNoteIndex].time - m_travelTime)
        {
            SpawnTile(notes[m_nextNoteIndex]);
            m_nextNoteIndex++;
        }
    }

    // Taps each tile the moment its note time crosses the hit line (always Perfect).
    // Goes through the normal tap path so judging, effects and score still fire
    private void AutoPlayLoop()
    {
        var conductor = Conductor.Instance;
        if (conductor == null)
            return;

        m_autoPlayUsed = true;
        float songPosition = conductor.SongPosition;

        for (int i = m_activeTiles.Count - 1; i >= 0; i--)
        {
            Tile tile = m_activeTiles[i];
            if (tile != null && tile.State == TileState.Falling && songPosition >= tile.NoteTime)
                tile.TapFromInput();
        }
    }

    // Every note spawns as a tap tile — hold notes in the beatmap are played as taps
    private void SpawnTile(NoteData note)
    {
        if (m_tilePrefab == null || m_tileContainer == null || m_lanes == null || m_lanes.Length == 0)
        {
            GameLog.Log(LogType.Error, "[TileSpawner] Missing references — assign tile prefab, container and lanes in the Inspector");
            enabled = false;
            return;
        }

        int lane = Mathf.Abs(note.lane) % m_lanes.Length;

        GameObject tileObj = ObjectPooling.Instance != null
            ? ObjectPooling.Instance.Spawn(m_tilePrefab, m_lanes[lane])
            : Instantiate(m_tilePrefab, m_lanes[lane]);

        var tile = tileObj.GetComponent<Tile>();
        if (tile == null)
        {
            GameLog.Log(LogType.Error, "[TileSpawner] Tile prefab has no Tile component");
            enabled = false;
            return;
        }

        tile.OnHit = HandleTileHit;
        tile.OnMiss = HandleTileMiss;
        tile.Init(lane, note.time, m_fallSpeed, m_laneHitY[lane], m_laneMissY[lane], m_hitLine);

        m_activeTiles.Add(tile);
    }

    private void PrepareGeometry()
    {
        if (m_tileContainer == null || m_tapZone == null || m_hitLine == null)
        {
            GameLog.Log(LogType.Error, "[TileSpawner] Missing references — assign container, tap zone and hit line in the Inspector");
            enabled = false;
            return;
        }

        int laneCount = m_lanes.Length;
        if (m_laneSpawnY == null || m_laneSpawnY.Length != laneCount)
        {
            m_laneSpawnY = new float[laneCount];
            m_laneHitY = new float[laneCount];
            m_laneMissY = new float[laneCount];
        }

        for (int i = 0; i < laneCount; i++)
        {
            RectTransform lane = m_lanes[i];
            m_laneSpawnY[i] = lane.rect.yMax;
            m_laneHitY[i] = lane.InverseTransformPoint(m_hitLine.transform.position).y;
            m_laneMissY[i] = lane.InverseTransformPoint(m_tapZone.GetBottomEdgeWorldPosition()).y;
        }

        m_travelTime = (m_laneSpawnY[0] - m_laneHitY[0]) / m_fallSpeed;

        Canvas canvas = m_tileContainer.GetComponentInParent<Canvas>();
        m_canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        GameLog.Log(LogType.Log, $"[TileSpawner] Geometry: spawnY {m_laneSpawnY[0]:F0}, hitY {m_laneHitY[0]:F0}, missY {m_laneMissY[0]:F0}, travelTime {m_travelTime:F2}s");
    }

    public void TapAtScreenPosition(Vector2 screenPos)
    {
        Tile target = null;
        for (int i = 0; i < m_activeTiles.Count; i++)
        {
            Tile tile = m_activeTiles[i];
            if (tile == null || tile.State != TileState.Falling)
                continue;

            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    (RectTransform)tile.transform, screenPos, m_canvasCamera))
                continue;

            if (target == null || tile.NoteTime < target.NoteTime)
                target = tile;
        }

        if (target != null)
            target.TapFromInput();
    }

    // Consecutive Perfects multiply the base score (2 in a row = x2, 3+ = x3);
    // any non-Perfect hit drops the multiplier back to x1
    private void HandleTileHit(Tile tile, int scorePerHit)
    {
        m_activeTiles.Remove(tile);

        JudgeLevel level = tile.HitJudge;
        m_perfectStreak = level == JudgeLevel.Perfect ? m_perfectStreak + 1 : 0;
        int multiplier = Mathf.Clamp(m_perfectStreak, 1, m_maxScoreMultiplier);
        Score += scorePerHit * multiplier;
        m_hitCount++;
        OnScoreChanged.Invoke(Score);

        m_comboEffect.Increment();
        m_JudgeLevelEffect.Show(level);

        Vector3 effectPos = m_hitLine != null ? m_hitLine.GetHitPosition(tile.transform) : tile.transform.position;
        PlayEffect(m_tapEffectPrefab, effectPos, m_effectContainer);

        if (m_confettiContainer != null)
        {
            Vector3 confettiPos = m_confettiContainer.position;
            confettiPos.x = effectPos.x;
            PlayEffect(m_confettiEffect, confettiPos, m_confettiContainer);
        }
    }

    private void HandleTileMiss(Tile tile)
    {
        m_activeTiles.Remove(tile);
        GameLog.Log(LogType.Log, "[TileSpawner] Tile missed");

        m_perfectStreak = 0;
        m_comboEffect.ResetCombo();
        m_JudgeLevelEffect.Show(JudgeLevel.Miss);

        if (tile.DroppedToBottom && GamePlayController.Instance != null)
        {
            int stars = CalculateStars();
            if (!m_autoPlayUsed && stars > 0 && UserProfile.Instance != null && BeatMapLoader.Instance != null && BeatMapLoader.Instance.CurrentMap != null)
            {
                UserProfile.Instance.SubmitSongStars(BeatMapLoader.Instance.CurrentMap.songName, stars);
                UserProfile.Instance.SaveGameData();
            }

            ReportRunResult(isWin: false, stars: stars);
            GamePlayController.Instance.ForceLose();
        }
    }

    private void PlayEffect(GameObject effectPrefab, Vector3 worldPos, Transform parent)
    {
        if (effectPrefab == null)
            return;

        GameObject effect = ObjectPooling.Instance != null
            ? ObjectPooling.Instance.Spawn(effectPrefab, parent)
            : Instantiate(effectPrefab, parent);

        effect.transform.position = worldPos;

        var particle = effect.GetComponent<ParticleSystem>();
        if (particle != null)
        {
            particle.Clear(true);
            particle.Play(true);
        }
    }

    // All beatmap notes spawned and none left on screen -> level cleared
    private void CheckWin(GamePlayController controller)
    {
        var loader = BeatMapLoader.Instance;
        if (loader == null || loader.CurrentMap == null)
            return;

        if (m_nextNoteIndex >= loader.CurrentMap.notes.Count && m_activeTiles.Count == 0)
        {
            AwardRunRewards(loader.CurrentMap);
            controller.PlayerWin();
        }
    }

    // Stars by accuracy (hits / total notes), coins scale with the stars.
    // UserProfile keeps only the best star record per song.
    private void AwardRunRewards(BeatMap map)
    {
        if (UserProfile.Instance == null || map.notes.Count == 0)
            return;

        int stars = CalculateStars();

        if (!m_autoPlayUsed)
        {
            UserProfile.Instance.SubmitSongStars(map.songName, stars);
            UserProfile.Instance.Coin += stars * 50;
            UserProfile.Instance.SaveGameData();
        }
        ReportRunResult(isWin: true, stars: stars);

        GameLog.Log(LogType.Log, $"[TileSpawner] Run done: {m_hitCount}/{map.notes.Count} hits -> {stars} stars");
    }

    private int CalculateStars()
    {
        var loader = BeatMapLoader.Instance;
        if (loader == null || loader.CurrentMap == null || loader.CurrentMap.notes.Count == 0)
            return 0;

        float accuracy = (float)m_hitCount / loader.CurrentMap.notes.Count;
        return accuracy >= 0.9f ? 3 : accuracy >= 0.6f ? 2 : accuracy >= 0.3f ? 1 : 0;
    }

    // Fills the result UILevelComplete reads. The best score record persists in
    // UserProfile for every run — a failed run can still set a new best.
    private void ReportRunResult(bool isWin, int stars)
    {
        var loader = BeatMapLoader.Instance;
        var controller = GamePlayController.Instance;
        if (loader == null || loader.CurrentMap == null || controller == null || UserProfile.Instance == null)
            return;

        string songName = loader.CurrentMap.songName;
        bool isNewBest = !m_autoPlayUsed && UserProfile.Instance.SubmitSongScore(songName, Score);

        controller.SetRunResult(new GamePlayController.RunResult
        {
            songName = songName,
            score = Score,
            bestScore = UserProfile.Instance.GetBestScore(songName),
            stars = stars,
            isWin = isWin,
            isNewBest = isNewBest,
        });
    }

    private void ResetRun()
    {
        for (int i = 0; i < m_activeTiles.Count; i++)
        {
            if (m_activeTiles[i] == null)
                continue;

            if (ObjectPooling.Instance != null)
                ObjectPooling.Instance.Despawn(m_activeTiles[i].gameObject);
            else
                m_activeTiles[i].gameObject.SetActive(false);
        }
        m_activeTiles.Clear();

        m_nextNoteIndex = 0;
        m_hitCount = 0;
        m_perfectStreak = 0;
        m_autoPlayUsed = false;
        Score = 0;
        OnScoreChanged.Invoke(Score);

        PrepareGeometry();

        m_comboEffect.ResetCombo();
        m_JudgeLevelEffect.HideImmediate();
        m_JudgeLevelEffect.ResetStreak();
    }
}
