using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BombController))]
public class BombAudioController : MonoBehaviour
{
    [SerializeField] private BombController m_bomb;

    private void Awake()
    {
        if (m_bomb == null) m_bomb = GetComponent<BombController>();
    }

    private void OnEnable()
    {
        if (m_bomb == null) m_bomb = GetComponent<BombController>();
        if (m_bomb != null) m_bomb.Exploded += OnExploded;
    }

    private void OnDisable()
    {
        if (m_bomb != null) m_bomb.Exploded -= OnExploded;
    }

    private void OnExploded(Vector3 position)
    {
        if (m_bomb == null) return;
        SoundManager manager = SoundManager.Instance;
        if (manager != null)
        {
            // A detached source can finish playing after the bomb is destroyed.
            manager.PlayGameplaySound(m_bomb.ExplosionSound, position, 16);
        }
    }
}
