using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelManager : Singleton<LevelManager>
{
    [Tooltip("Levels offered by UIHome, in selection order. Each level needs a unique Level ID.")]
    [SerializeField] private List<LevelScriptableObject> m_levels = new();
    [SerializeField, Min(0)] private int m_selectedLevelIndex;
    [Tooltip("Optional parent for the instantiated map. Uses this transform when empty. Keep its world scale at (1, 1, 1).")]
    [SerializeField] private Transform m_mapRoot;

    [FormerlySerializedAs("m_player")]
    [Tooltip("Player prefab with PlayerController and PlayerStats on its root. A fresh player is created for each run.")]
    [SerializeField] private GameObject m_playerPrefab;
    private LevelScriptableObject m_currentLevel;
    private LevelMap m_currentMap;
    private PlayerController m_currentPlayer;
    private CameraManager m_cameraManager;

    public int LevelCount => m_levels != null ? m_levels.Count : 0;
    public int SelectedLevelIndex => m_selectedLevelIndex;
    public LevelScriptableObject SelectedLevel => m_selectedLevelIndex >= 0 && m_selectedLevelIndex < LevelCount
        ? m_levels[m_selectedLevelIndex]
        : null;
    public LevelScriptableObject CurrentLevel => m_currentLevel;
    public LevelMap CurrentMap => m_currentMap;
    public PlayerController CurrentPlayer => m_currentPlayer;

    public bool TrySelectLevel(int index)
    {
        if (m_currentMap != null || index < 0 || index >= LevelCount || m_levels[index] == null)
            return false;

        m_selectedLevelIndex = index;
        return true;
    }

    public bool TryPrepareSelectedLevel(out string error)
    {
        if (!isActiveAndEnabled)
        {
            error = "An active LevelManager is required.";
            return false;
        }

        if (m_currentMap != null)
        {
            error = "End the current run and clear its map before preparing another level.";
            return false;
        }

        LevelScriptableObject selectedLevel = SelectedLevel;
        if (selectedLevel == null)
        {
            error = "Select a valid level from the LevelManager level list.";
            return false;
        }

        if (!TryValidateLevelIds(out error) || !selectedLevel.TryValidate(out error))
            return false;

        if (!TryValidatePlayerPrefab(out error))
            return false;
        m_cameraManager = CameraManager.Instance;
        if (m_cameraManager == null || !m_cameraManager.isActiveAndEnabled)
        {
            error = "The gameplay scene needs an active CameraManager with a Cinemachine camera.";
            return false;
        }

        Transform mapRoot = m_mapRoot != null ? m_mapRoot : transform;
        Vector3 rootScale = mapRoot.lossyScale;
        if (!mapRoot.gameObject.activeInHierarchy || !Mathf.Approximately(rootScale.x, 1f) ||
            !Mathf.Approximately(rootScale.y, 1f) || !Mathf.Approximately(rootScale.z, 1f))
        {
            error = "Map Root must be active with world scale (1, 1, 1), so the map and baked NavMesh align.";
            return false;
        }

        // Final pose is supplied before OnEnable registers the map's baked NavMesh.
        LevelMap map = Instantiate(selectedLevel.MapPrefab, mapRoot.position, mapRoot.rotation, mapRoot);
        if (!map.TryValidate(out error))
        {
            DestroyMap(map);
            return false;
        }

        m_currentLevel = selectedLevel;
        m_currentMap = map;
        Transform spawn = map.PlayerSpawnPoint;
        GameObject playerObject = Instantiate(m_playerPrefab, spawn.position, spawn.rotation);
        m_currentPlayer = playerObject.GetComponent<PlayerController>();
        m_currentPlayer.SetGameplayEnabled(false);
        if (!m_currentPlayer.isActiveAndEnabled || !m_currentPlayer.Stats.isActiveAndEnabled)
        {
            error = "The spawned PlayerController and PlayerStats must remain active and enabled.";
            ClearLevel();
            return false;
        }
        if (!m_cameraManager.TrySetPlayerTarget(m_currentPlayer.transform, out error))
        {
            ClearLevel();
            return false;
        }
        error = null;
        return true;
    }

    public void ClearLevel()
    {
        PlayerController player = m_currentPlayer;
        LevelMap map = m_currentMap;
        m_currentPlayer = null;
        m_currentMap = null;
        m_currentLevel = null;
        if (player != null)
        {
            if (m_cameraManager != null)
                m_cameraManager.ClearPlayerTarget(player.transform);
            player.EndRun();
            player.gameObject.SetActive(false);
            Destroy(player.gameObject);
        }
        DestroyMap(map);
    }

    protected override void OnDestroy()
    {
        ClearLevel();
        base.OnDestroy();
    }

    private bool TryValidateLevelIds(out string error)
    {
        HashSet<string> levelIds = new(StringComparer.Ordinal);
        for (int i = 0; i < LevelCount; i++)
        {
            LevelScriptableObject level = m_levels[i];
            if (level == null || string.IsNullOrWhiteSpace(level.LevelId))
            {
                error = $"Level list entry {i + 1} needs a level asset with a non-empty Level ID.";
                return false;
            }

            if (!levelIds.Add(level.LevelId))
            {
                error = $"Level ID '{level.LevelId}' is duplicated in the LevelManager list. Assign a unique ID to every level.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private bool TryValidatePlayerPrefab(out string error)
    {
        if (m_playerPrefab == null || !m_playerPrefab.activeSelf || m_playerPrefab.transform.parent != null)
        {
            error = "Assign an active Player Prefab root on LevelManager.";
            return false;
        }

        PlayerController controller = m_playerPrefab.GetComponent<PlayerController>();
        PlayerStats stats = m_playerPrefab.GetComponent<PlayerStats>();
        if (controller == null || !controller.enabled || stats == null || !stats.enabled)
        {
            error = "Player Prefab needs enabled PlayerController and PlayerStats components on its root.";
            return false;
        }

        error = null;
        return true;
    }
    private static void DestroyMap(LevelMap map)
    {
        if (map == null)
            return;

        // Remove colliders and NavMesh immediately, even when Start/Restart share a frame.
        map.gameObject.SetActive(false);
        Destroy(map.gameObject);
    }
}
