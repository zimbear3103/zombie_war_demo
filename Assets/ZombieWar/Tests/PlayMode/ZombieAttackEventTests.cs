#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public class ZombieAttackEventTests
{
    private const BindingFlags m_instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject m_parent;
    private GameObject m_zombie;
    private Component m_controller;
    private Component m_zombieStats;
    private PlayerStats m_player;
    private Animator m_animator;
    private NavMeshData m_navMeshData;
    private NavMeshDataInstance m_navMeshInstance;
    private int m_attackCount;
    private float m_startingHealth;
    private float m_damage;

    [SetUp]
    public void SetUp()
    {
        m_attackCount = 0;
        m_parent = new GameObject("Zombie attack event test") { hideFlags = HideFlags.DontSave };
        m_parent.SetActive(false);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ZombieWar/Prefabs/Ingame/NormalZombie.prefab");
        Assert.That(prefab, Is.Not.Null);
        m_zombie = Object.Instantiate(prefab, m_parent.transform);
        m_controller = m_zombie.GetComponent("ZombieController");
        m_zombieStats = m_zombie.GetComponent("ZombieStats");
        m_animator = (Animator)new SerializedObject(m_controller)
            .FindProperty("m_animator").objectReferenceValue;
        Assert.That(m_animator, Is.SameAs(m_zombie.GetComponent<Animator>()),
            "The controller and animation event must use the enabled Animator on the prefab root.");
        Assert.That(m_animator.enabled, Is.True);
        m_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Count the audio hook without starting scene-owned audio voices.
        Behaviour audio = m_zombie.GetComponent("ZombieAudioController") as Behaviour;
        if (audio != null) audio.enabled = false;

        NavMeshAgent agent = m_zombie.GetComponent<NavMeshAgent>();
        Vector3 origin = new Vector3(10000f, 0f, 10000f);
        var sources = new List<NavMeshBuildSource>
        {
            new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(origin + Vector3.down * 0.1f, Quaternion.identity, Vector3.one),
                size = new Vector3(20f, 0.2f, 20f),
                area = 0
            }
        };
        m_navMeshData = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(agent.agentTypeID),
            sources, new Bounds(origin, new Vector3(20f, 4f, 20f)), Vector3.zero, Quaternion.identity);
        Assert.That(m_navMeshData, Is.Not.Null);
        m_navMeshInstance = NavMesh.AddNavMeshData(m_navMeshData);
        Assert.That(NavMesh.SamplePosition(origin, out NavMeshHit hit, 2f, NavMesh.AllAreas), Is.True);
        m_zombie.transform.position = hit.position;

        var playerObject = new GameObject("Melee target");
        playerObject.transform.SetParent(m_parent.transform, false);
        playerObject.transform.position = hit.position + Vector3.forward;
        m_player = playerObject.AddComponent<PlayerStats>();
        FieldInfo feedback = typeof(PlayerStats).GetField("m_hitFlashEffects", m_instanceFlags);
        feedback.SetValue(m_player, Array.CreateInstance(feedback.FieldType.GetElementType(), 0));
        m_parent.SetActive(true);
        Assert.That(agent.Warp(hit.position), Is.True);
        Assert.That(agent.isOnNavMesh, Is.True);
        PrepareForSpawn();
        EventInfo attack = m_controller.GetType().GetEvent("AttackPerformed");
        attack.AddEventHandler(m_controller, Delegate.CreateDelegate(attack.EventHandlerType, this,
            GetType().GetMethod(nameof(CountAttack), m_instanceFlags)));
        m_startingHealth = m_player.CurrentHealth;
        m_damage = (float)m_zombieStats.GetType().GetProperty("Damage").GetValue(m_zombieStats);
        Assert.That(m_damage, Is.GreaterThan(0f).And.LessThan(m_startingHealth));
        Physics.SyncTransforms();
    }

    [TearDown]
    public void TearDown()
    {
        if (m_parent != null) Object.DestroyImmediate(m_parent);
        if (m_navMeshInstance.valid) m_navMeshInstance.Remove();
        if (m_navMeshData != null) Object.DestroyImmediate(m_navMeshData);
    }

    [Test]
    public void AnimationEventWithoutRequestedAttackDoesNotDamage()
    {
        Invoke(m_controller, "OnAttackHit");
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth));
        Assert.That(m_attackCount, Is.Zero);
    }

    [Test]
    public void RealAttackDamagesAtAuthoredAnimationEventOnlyOnce()
    {
        StartAttack();
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth));
        AdvanceAnimation(0.9f);
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth),
            "Starting the swing must not damage before its authored hit event.");
        AdvanceAnimation(0.8f);
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth - m_damage),
            "Advancing the real prefab Animator across frame 40 must deliver the hit event.");
        Invoke(m_controller, "OnAttackHit");
        Invoke(m_controller, "OnAttackHit");
        AdvanceAnimation(2f);
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth - m_damage));
        Assert.That(m_attackCount, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RangeOrCoverAtHitTimeConsumesSwingWithoutDamage(bool addCover)
    {
        StartAttack();
        AdvanceAnimation(0.4f);
        Vector3 targetPosition = m_player.transform.position;
        GameObject cover = null;
        if (addCover)
        {
            cover = new GameObject("Cover appearing during windup");
            cover.transform.SetParent(m_parent.transform, false);
            cover.transform.position = (m_zombie.transform.position + targetPosition) * 0.5f + Vector3.up * 0.5f;
            cover.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 0.1f);
        }
        else
        {
            m_player.transform.position += Vector3.forward * 5f;
        }
        Physics.SyncTransforms();
        AdvanceAnimation(1.3f);
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth));

        if (cover != null) Object.DestroyImmediate(cover);
        m_player.transform.position = targetPosition;
        Physics.SyncTransforms();
        Invoke(m_controller, "OnAttackHit");
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth),
            "A missed swing must not leave a hit armed for a later duplicate event.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DisablingGameplayCancelsPendingHitEvenIfResumedBeforeEvent(bool resumeBeforeHit)
    {
        StartAttack();
        AdvanceAnimation(0.4f);
        Invoke(m_controller, "SetGameplayEnabled", false);
        if (resumeBeforeHit) Invoke(m_controller, "SetGameplayEnabled", true);
        AdvanceAnimation(1.3f);
        Invoke(m_controller, "OnAttackHit");
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth));
    }

    [Test]
    public void DeathBeforeHitPreventsQueuedDamage()
    {
        StartAttack();
        AdvanceAnimation(0.4f);
        Invoke(m_zombieStats, "TakeDamage", new DamageInfo(float.MaxValue,
            m_zombie.transform.position, Vector3.forward, 0f, m_player.gameObject));
        Assert.That((bool)m_zombieStats.GetType().GetProperty("IsAlive").GetValue(m_zombieStats), Is.False);
        Invoke(m_controller, "OnAttackHit");
        AdvanceAnimation(1.3f);
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth));
    }

    [Test]
    public void PoolReuseClearsPreviousHitAndAllowsANewSwing()
    {
        StartAttack();
        AdvanceAnimation(0.4f);
        Invoke(m_controller, "PrepareForPool");
        Invoke(m_controller, "OnAttackHit");
        PrepareForSpawn();
        Invoke(m_controller, "OnAttackHit");
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth));
        StartAttack();
        AdvanceAnimation(1.7f);
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth - m_damage));
        Assert.That(m_attackCount, Is.EqualTo(2));
    }

    [Test]
    public void ExpiredCooldownDoesNotRepeatAttackAudioDuringCurrentSwing()
    {
        StartAttack();
        AdvanceAnimation(0.4f);
        Assert.That(m_animator.GetCurrentAnimatorStateInfo(0).IsName("attack"), Is.True);
        ExpireCooldownAndUpdateMovement();
        AdvanceAnimation(1.3f);
        ExpireCooldownAndUpdateMovement();
        Assert.That(m_attackCount, Is.EqualTo(1),
            "AttackPerformed drives SFX and must represent a newly requested swing.");
        Assert.That(m_player.CurrentHealth, Is.EqualTo(m_startingHealth - m_damage));
        AdvanceAnimation(2f);
        ExpireCooldownAndUpdateMovement();
        Assert.That(m_attackCount, Is.EqualTo(2), "The next swing can start once the animation has finished.");
    }

    private void PrepareForSpawn()
    {
        Assert.That((bool)Invoke(m_controller, "PrepareForSpawn", m_player.transform, m_player, true), Is.True);
        Assert.That((bool)m_controller.GetType().GetProperty("IsSpawnReady").GetValue(m_controller), Is.True);
    }

    private void StartAttack()
    {
        int previousCount = m_attackCount;
        Invoke(m_controller, "UpdateMovement");
        Assert.That(m_attackCount, Is.EqualTo(previousCount + 1), "The real melee decision must request a swing.");
    }

    private void ExpireCooldownAndUpdateMovement()
    {
        // Manual Animator.Update calls do not advance the controller's cooldown.
        m_controller.GetType().GetField("m_attackCooldownRemaining", m_instanceFlags).SetValue(m_controller, 0f);
        Invoke(m_controller, "UpdateMovement");
    }

    private void CountAttack(object zombie)
    {
        m_attackCount++;
    }

    private void AdvanceAnimation(float seconds)
    {
        const float step = 1f / 60f;
        for (int i = 0; i < Mathf.CeilToInt(seconds / step); i++)
            m_animator.Update(step);
    }

    private static object Invoke(Component component, string method, params object[] arguments)
    {
        return component.GetType().GetMethod(method, m_instanceFlags).Invoke(component, arguments);
    }
}
#endif
