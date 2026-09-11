using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAnimationTests
{
    private GameObject m_parent;
    private GameObject m_zombie;
    private Animator m_animator;

    [SetUp]
    public void SetUp()
    {
        m_parent = new GameObject("Zombie animation test");
        m_parent.SetActive(false);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ZombieWar/Prefabs/Ingame/NormalZombie.prefab");
        m_zombie = Object.Instantiate(prefab, m_parent.transform);
        m_zombie.GetComponent<NavMeshAgent>().enabled = false;
        m_animator = (Animator)new SerializedObject(m_zombie.GetComponent("ZombieController"))
            .FindProperty("m_animator").objectReferenceValue;
        m_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        m_parent.SetActive(true);
        Component controller = m_zombie.GetComponent("ZombieController");
        controller.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
        m_animator.Rebind();
        m_animator.Update(0f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(m_parent);
    }

    [TestCase(0f)]
    [TestCase(0.5f)]
    [TestCase(1f)]
    public void MovingFromIdleSelectsLocomotionWithoutWaitingForClip(float blend)
    {
        m_animator.SetBool("move", true);
        m_animator.SetFloat("locomotion", blend);
        AdvanceAnimation(0.35f);
        Assert.That(m_animator.GetCurrentAnimatorStateInfo(0).IsName("Moving"), Is.True);
    }

    [Test]
    public void StoppingReturnsToIdleWithoutPlayingAnotherLocomotionClip()
    {
        m_animator.Play("Moving", 0, 0f);
        m_animator.Update(0f);
        m_animator.SetBool("move", false);
        m_animator.SetFloat("locomotion", 0f);
        AdvanceAnimation(0.35f);
        Assert.That(m_animator.GetCurrentAnimatorStateInfo(0).IsName("idle"), Is.True);
    }

    [Test]
    public void AttackCanStartFromIdleAndReturnToIdleWhenStationary()
    {
        m_animator.SetTrigger("Attack");
        AdvanceAnimation(0.35f);
        Assert.That(m_animator.GetCurrentAnimatorStateInfo(0).IsName("attack"), Is.True);
        AdvanceAnimation(3f);
        Assert.That(m_animator.GetCurrentAnimatorStateInfo(0).IsName("idle"), Is.True);
    }

    [Test]
    public void LosingTargetClearsMovingAnimationOnNextUpdate()
    {
        m_animator.SetBool("move", true);
        m_animator.SetFloat("locomotion", 1f);
        m_animator.Play("Moving", 0, 0f);
        m_animator.Update(0f);
        Component controller = m_zombie.GetComponent("ZombieController");
        controller.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
        AdvanceAnimation(0.35f);
        Assert.That(m_animator.GetBool("move"), Is.False);
        Assert.That(m_animator.GetFloat("locomotion"), Is.EqualTo(0f));
        Assert.That(m_animator.GetCurrentAnimatorStateInfo(0).IsName("idle"), Is.True);
    }

    [Test]
    public void RequestedAttackFinishesWhenTargetBecomesUnavailable()
    {
        m_animator.SetTrigger("Attack");
        Component controller = m_zombie.GetComponent("ZombieController");
        controller.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
        AdvanceAnimation(0.35f);
        Assert.That(m_animator.GetCurrentAnimatorStateInfo(0).IsName("attack"), Is.True);
    }

    [Test]
    public void WalkingAnimationDoesNotMoveModelAwayFromAgentRoot()
    {
        Vector3 localPosition = m_animator.transform.localPosition;
        Quaternion localRotation = m_animator.transform.localRotation;
        m_animator.SetBool("move", true);
        m_animator.SetFloat("locomotion", 0f);
        m_animator.Play("Moving", 0, 0f);
        AdvanceAnimation(2f);
        Assert.That(Vector3.Distance(m_animator.transform.localPosition, localPosition), Is.LessThan(0.001f));
        Assert.That(Quaternion.Angle(m_animator.transform.localRotation, localRotation), Is.LessThan(0.01f));
    }

    private void AdvanceAnimation(float seconds)
    {
        const float step = 1f / 60f;
        for (int i = 0; i < Mathf.CeilToInt(seconds / step); i++)
            m_animator.Update(step);
    }
}
