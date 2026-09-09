using UnityEngine;

public class PlayerCollector : MonoBehaviour
{
    [SerializeField] private PlayerController m_player;

    private void Awake()
    {
        if (m_player == null) m_player = GetComponentInParent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_player == null || !m_player.CanAct) return;
        var pickup = other.GetComponentInParent<WeaponPickup>();
        if (pickup != null) pickup.TryCollect(m_player);
    }
}
