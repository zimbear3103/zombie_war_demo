using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponScriptableObject m_weaponData;
    private PlayerController m_collectedBy;

    public bool TryCollect(PlayerController player)
    {
        if (!isActiveAndEnabled || m_collectedBy != null || player == null || !player.CanAct || m_weaponData == null)
        {
            return false;
        }

        if (!player.TryCollectWeapon(m_weaponData))
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
