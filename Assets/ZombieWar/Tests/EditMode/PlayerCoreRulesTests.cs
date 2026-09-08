using NUnit.Framework;
using UnityEngine;

public class PlayerCoreRulesTests
{
    [Test]
    public void DiagonalMoveIsClampedButAnalogIsPreserved()
    {
        Assert.That(PlayerControlRules.Move(Vector2.one).magnitude, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(PlayerControlRules.Move(new Vector2(0.2f, 0f)).magnitude, Is.EqualTo(0.2f).Within(0.0001f));
    }

    [Test]
    public void AimWinsOverMoveAndReleaseFallsBackToMoveOrLastFacing()
    {
        Assert.That(PlayerControlRules.Facing(Vector2.up, Vector2.left, Vector3.forward, 0.25f), Is.EqualTo(Vector3.left));
        Assert.That(PlayerControlRules.Facing(Vector2.up, Vector2.zero, Vector3.left, 0.25f), Is.EqualTo(Vector3.forward));
        Assert.That(PlayerControlRules.Facing(Vector2.zero, Vector2.zero, Vector3.left, 0.25f), Is.EqualTo(Vector3.left));
        Assert.That(PlayerControlRules.IsAiming(new Vector2(0.25f, 0f), 0.25f), Is.False);
        Assert.That(PlayerControlRules.IsAiming(new Vector2(0.251f, 0f), 0.25f), Is.True);
    }

    [Test]
    public void InvalidDamageIsIgnoredAndDeathFiresOncePerLife()
    {
        var target = new GameObject("Health test");
        try
        {
            var health = target.AddComponent<PlayerHealth>();
            health.RestoreFullHealth();
            int deaths = 0;
            health.Died += () => deaths++;
            foreach (float amount in new[] { -1f, 0f, float.NaN, float.PositiveInfinity })
                health.TakeDamage(new DamageInfo(amount, Vector3.zero, Vector3.forward, 0f, null));
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
            health.TakeDamage(new DamageInfo(120f, Vector3.zero, Vector3.forward, 0f, null));
            health.TakeDamage(new DamageInfo(10f, Vector3.zero, Vector3.forward, 0f, null));
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
            health.RestoreFullHealth();
            health.TakeDamage(new DamageInfo(100f, Vector3.zero, Vector3.forward, 0f, null));
            Assert.That(deaths, Is.EqualTo(2));
        }
        finally { Object.DestroyImmediate(target); }
    }
}
