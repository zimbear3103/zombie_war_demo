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
        if (other.CompareTag("Weapon") == true)
        {
            var pickup = other.GetComponent<WeaponPickup>();
            if (pickup != null) pickup.TryCollect(m_player);
        }

        if (other.CompareTag("Bomb") == true)
        {
            var pickup = other.GetComponent<BombPickup>();
            if (pickup != null) pickup.TryCollect(m_player);
        }
    }
}
