using UnityEngine;

public class PlayerCollector : MonoBehaviour
{
    [SerializeField] private PlayerController m_player;

    private void Awake()
    {
        if (m_player == null) m_player = GetComponent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_player == null || !m_player.CanAct) return;
        if (other.CompareTag("WeaponPickup") == true)
        {
            var pickup = other.GetComponentInParent<WeaponPickup>();
            if (pickup != null) pickup.TryCollect(m_player);
        }

        if (other.CompareTag("BombPickup") == true)
        {
            var pickup = other.GetComponentInParent<BombPickup>();
            if (pickup != null) pickup.TryCollect(m_player);
        }
    }
}
