using NUnit.Framework;
using UnityEngine;

public class PlayerCoreWeaponTests
{
    [TestCase(false, false, 92f)]
    [TestCase(true, false, 100f)]
    [TestCase(false, true, 100f)]
    public void NearestColliderAndMuzzleCoverControlDamage(bool distantWall, bool muzzleWall, float expectedHealth)
    {
        var shooter = new GameObject("Shooter");
        var origin = new GameObject("Origin");
        var muzzle = new GameObject("Muzzle");
        var target = new GameObject("Target");
        var child = new GameObject("Target hitbox");
        var wall = new GameObject("Wall");
        try
        {
            var owner = shooter.AddComponent<PlayerHealth>();
            owner.RestoreFullHealth();
            origin.transform.position = new Vector3(1000f, 1f, 1000f);
            muzzle.transform.position = origin.transform.position + Vector3.forward * 0.5f;
            target.transform.position = new Vector3(1000f, 1f, 1004f);
            var health = target.AddComponent<PlayerHealth>();
            health.RestoreFullHealth();
            child.transform.SetParent(target.transform, false);
            child.AddComponent<BoxCollider>();
            if (distantWall || muzzleWall)
            {
                wall.transform.position = new Vector3(1000f, 1f, muzzleWall ? 1000.25f : 1002f);
                wall.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 0.1f);
            }
            var weapon = shooter.AddComponent<WeaponController>();
            weapon.Configure(muzzle.transform, origin.transform, owner, 1 << 0);
            Physics.SyncTransforms();
            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(expectedHealth));
        }
        finally
        {
            Object.DestroyImmediate(shooter);
            Object.DestroyImmediate(origin);
            Object.DestroyImmediate(muzzle);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(wall);
        }
    }

    [Test]
    public void CooldownSurvivesReleaseAndDeadOrDisabledOwnerCannotFire()
    {
        var owner = new GameObject("Shooter");
        var origin = new GameObject("Origin");
        var muzzle = new GameObject("Muzzle");
        try
        {
            var health = owner.AddComponent<PlayerHealth>();
            health.RestoreFullHealth();
            origin.transform.position = new Vector3(1000f, 1f, 1000f);
            muzzle.transform.position = origin.transform.position + Vector3.forward * 0.5f;
            var weapon = owner.AddComponent<WeaponController>();
            weapon.Configure(muzzle.transform, origin.transform, health, 1 << 0);
            int shots = 0;
            //weapon.Fired += () => shots++;
            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            Assert.That(weapon.TryFire(Vector3.forward, 0.1f), Is.False);
            //weapon.SetGameplayEnabled(false);
            Assert.That(weapon.TryFire(Vector3.forward, 0.2f), Is.False);
            //weapon.SetGameplayEnabled(true);
            Assert.That(weapon.TryFire(Vector3.forward, 0.125f), Is.True);
            Assert.That(weapon.TryFire(Vector3.forward, 10f), Is.True);
            Assert.That(weapon.TryFire(Vector3.forward, 10f), Is.False);
            Assert.That(shots, Is.EqualTo(3));
            health.TakeDamage(new DamageInfo(100f, Vector3.zero, Vector3.forward, 0f, null));
            Assert.That(weapon.TryFire(Vector3.forward, 11f), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(origin);
            Object.DestroyImmediate(muzzle);
        }
    }
}
