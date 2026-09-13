using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ZombieController))]
public class ZombieAudioController : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private GameplaySound m_growlSound = new GameplaySound();
    [SerializeField] private GameplaySound m_attackSound = new GameplaySound();
    [SerializeField] private GameplaySound m_hurtSound = new GameplaySound();
    [SerializeField] private GameplaySound m_deathSound = new GameplaySound();

    [Header("Timing")]
    [SerializeField] private Vector2 m_growlInterval = new Vector2(3f, 6f);
    [SerializeField, Min(0f)] private float m_hurtCooldown = 0.25f;

    private ZombieController m_zombie;
    private ZombieStats m_stats;
    private SoundManager m_voiceManager;
    private int m_voiceHandle;
    private float m_growlTimeRemaining;
    private float m_hurtTimeRemaining;
    private bool m_deathPlayed;

    private void Awake()
    {
        m_zombie = GetComponent<ZombieController>();
        m_stats = GetComponent<ZombieStats>();
    }

    private void OnEnable()
    {
        m_zombie.AttackPerformed += HandleAttack;
        m_zombie.DeathStarted += HandleDeath;
        m_zombie.RuntimeStateReset += HandleRuntimeStateReset;
        if (m_stats != null)
            m_stats.Damaged += HandleDamaged;

        // PrepareForSpawn can run on an inactive instance before this component's Awake.
        ResetRuntimeState();
    }

    private void OnDisable()
    {
        m_zombie.AttackPerformed -= HandleAttack;
        m_zombie.DeathStarted -= HandleDeath;
        m_zombie.RuntimeStateReset -= HandleRuntimeStateReset;
        if (m_stats != null)
            m_stats.Damaged -= HandleDamaged;

        ResetRuntimeState();
    }

    private void Update()
    {
        if (m_deathPlayed)
            return;

        if (!m_zombie.CanPlayLivingAudio)
        {
            StopLivingVoice();
            return;
        }

        m_hurtTimeRemaining = Mathf.Max(0f, m_hurtTimeRemaining - Time.deltaTime);
        m_growlTimeRemaining -= Time.deltaTime;
        if (m_growlTimeRemaining > 0f)
            return;

        // Ambient voices never interrupt an attack or hurt clip.
        if (m_voiceManager != null && m_voiceManager.IsGameplaySoundActive(m_voiceHandle))
            return;

        PlayLivingVoice(m_growlSound, 192);
        ScheduleGrowl();
    }

    private void HandleAttack(ZombieController zombie)
    {
        if (m_deathPlayed || !m_zombie.CanPlayLivingAudio)
            return;

        PlayLivingVoice(m_attackSound, 128);
        ScheduleGrowl();
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        // ZombieStats raises Damaged before Died, including on a lethal hit.
        if (m_deathPlayed || m_stats == null || !m_stats.IsAlive ||
            !m_zombie.CanPlayLivingAudio || m_hurtTimeRemaining > 0f)
            return;

        m_hurtTimeRemaining = GetNonNegativeValue(m_hurtCooldown, 0.25f);
        PlayLivingVoice(m_hurtSound, 112);
        ScheduleGrowl();
    }

    private void HandleDeath(ZombieController zombie)
    {
        if (m_deathPlayed)
            return;

        m_deathPlayed = true;
        StopLivingVoice();
        SoundManager soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            // The manager owns this tail; respawning or disabling the corpse must not stop it.
            soundManager.PlayGameplaySound(m_deathSound, transform.position, 32);
        }
    }

    private void HandleRuntimeStateReset(ZombieController zombie)
    {
        ResetRuntimeState();
    }

    private void ResetRuntimeState()
    {
        StopLivingVoice();
        m_deathPlayed = false;
        m_hurtTimeRemaining = 0f;
        ScheduleGrowl();
    }

    private void PlayLivingVoice(GameplaySound sound, int priority)
    {
        StopLivingVoice();
        m_voiceManager = SoundManager.Instance;
        if (m_voiceManager != null)
            m_voiceHandle = m_voiceManager.PlayGameplaySound(sound, transform.position, priority, transform);
    }

    private void StopLivingVoice()
    {
        if (m_voiceManager != null && m_voiceHandle != 0)
            m_voiceManager.StopGameplaySound(m_voiceHandle);

        m_voiceHandle = 0;
        m_voiceManager = null;
    }

    private void ScheduleGrowl()
    {
        float minimum = Mathf.Max(0.1f, GetNonNegativeValue(m_growlInterval.x, 3f));
        float maximum = Mathf.Max(minimum, GetNonNegativeValue(m_growlInterval.y, 6f));
        m_growlTimeRemaining = Random.Range(minimum, maximum);
    }

    private static float GetNonNegativeValue(float value, float fallback)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f ? value : fallback;
    }
}
