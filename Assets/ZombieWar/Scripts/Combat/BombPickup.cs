using UnityEngine;

public class BombPickup : MonoBehaviour
{
    [SerializeField] private BombController m_bombData;
    private PlayerController m_collectedBy;
    public bool TryCollect(PlayerController player)
    {
        if (!isActiveAndEnabled || m_collectedBy != null || player == null || !player.CanAct || m_bombData == null)
        {
            return false;
        }

        if (!player.TryCollectBomb(m_bombData))
        {
            return false;
        }

        m_collectedBy = player;
        m_collectedBy.RunReset += RestoreForRun;
        gameObject.SetActive(false);
        return true;
    }

    private void RestoreForRun()
    {
        DetachFromPlayer();
        gameObject.SetActive(true);
    }

    private void DetachFromPlayer()
    {
        if (m_collectedBy != null)
            m_collectedBy.RunReset -= RestoreForRun;
        m_collectedBy = null;
    }

    private void OnDestroy() => DetachFromPlayer();
}
