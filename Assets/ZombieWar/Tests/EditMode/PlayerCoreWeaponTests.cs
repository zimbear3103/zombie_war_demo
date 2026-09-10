using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PlayerCoreWeaponTests
{
    [TestCase(false, false, 92f)]
    [TestCase(true, false, 100f)]
    [TestCase(false, true, 100f)]
    public void BulletDamagesOnArrivalAndStopsAtCover(bool distantWall, bool muzzleWall, float expectedHealth)
    {
        var shooter = new GameObject("Shooter");
        var origin = new GameObject("Origin");
        var muzzle = new GameObject("Muzzle");
        var target = new GameObject("Target");
        var child = new GameObject("Target hitbox");
        var wall = new GameObject("Wall");
        WeaponScriptableObject data = null;
        try
        {
            var owner = shooter.AddComponent<PlayerStats>();
            owner.RestoreFullHealth();
            origin.transform.position = new Vector3(1000f, 1f, 1000f);
            muzzle.transform.position = origin.transform.position + Vector3.forward * 0.5f;
            target.transform.position = new Vector3(1000f, 1f, 1004f);
            var health = target.AddComponent<PlayerStats>();
            health.RestoreFullHealth();
            child.transform.SetParent(target.transform, false);
            child.AddComponent<BoxCollider>();
            if (distantWall || muzzleWall)
            {
                wall.transform.position = new Vector3(1000f, 1f, muzzleWall ? 1000.25f : 1002f);
                wall.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 0.1f);
            }
            var weapon = CreateWeapon(shooter, muzzle.transform, origin.transform, owner, out data);
            Physics.SyncTransforms();
            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f),
                "Firing must not apply hitscan damage before the bullet reaches the target.");
            var bullet = GetBullets(weapon).Single();
            bullet.Simulate(0.01f);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
            bullet.Simulate(0.25f);
            Assert.That(health.CurrentHealth, Is.EqualTo(expectedHealth));
            Assert.That(bullet.IsFlying, Is.False);
            bullet.Simulate(1f);
            Assert.That(health.CurrentHealth, Is.EqualTo(expectedHealth), "A bullet must only damage once.");
        }
        finally
        {
            DestroyShooter(shooter);
            Object.DestroyImmediate(origin);
            Object.DestroyImmediate(muzzle);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(wall);
            if (data != null) Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void CooldownSurvivesReleaseAndDeadOrDisabledOwnerCannotFire()
    {
        var owner = new GameObject("Shooter");
        var origin = new GameObject("Origin");
        var muzzle = new GameObject("Muzzle");
        WeaponScriptableObject data = null;
        try
        {
            var health = owner.AddComponent<PlayerStats>();
            health.RestoreFullHealth();
            origin.transform.position = new Vector3(1000f, 1f, 1000f);
            muzzle.transform.position = origin.transform.position + Vector3.forward * 0.5f;
            var weapon = CreateWeapon(owner, muzzle.transform, origin.transform, health, out data);
            int shots = 0;
            weapon.Fired += () => shots++;
            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            Assert.That(weapon.TryFire(Vector3.forward, 0.1f), Is.False);
            weapon.SetGameplayEnabled(false);
            Assert.That(weapon.TryFire(Vector3.forward, 0.2f), Is.False);
            weapon.SetGameplayEnabled(true);
            Assert.That(weapon.TryFire(Vector3.forward, 0.125f), Is.True);
            Assert.That(weapon.TryFire(Vector3.forward, 10f), Is.True);
            Assert.That(weapon.TryFire(Vector3.forward, 10f), Is.False);
            Assert.That(shots, Is.EqualTo(3));
            health.TakeDamage(new DamageInfo(100f, Vector3.zero, Vector3.forward, 0f, null));
            Assert.That(weapon.TryFire(Vector3.forward, 11f), Is.False);
        }
        finally
        {
            DestroyShooter(owner);
            Object.DestroyImmediate(origin);
            Object.DestroyImmediate(muzzle);
            if (data != null) Object.DestroyImmediate(data);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RangeOrLifetimeRecyclesBulletAndNextShotReusesIt(bool expireByLifetime)
    {
        var shooter = new GameObject("Shooter");
        WeaponScriptableObject data = null;
        try
        {
            shooter.transform.position = new Vector3(1000f, 10f, 1000f);
            var health = shooter.AddComponent<PlayerStats>();
            health.RestoreFullHealth();
            var weapon = CreateWeapon(shooter, shooter.transform, shooter.transform, health, out data);
            var settings = new SerializedObject(data);
            settings.FindProperty("m_range").floatValue = expireByLifetime ? 100f : 1f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var template = GetBulletTemplate(weapon);
            var flight = new SerializedObject(template);
            flight.FindProperty("m_lifetime").floatValue = expireByLifetime ? 0.05f : 5f;
            flight.ApplyModifiedPropertiesWithoutUndo();
            weapon.Initialize(data, shooter.transform, health, 1 << 0);

            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            var first = GetBullets(weapon).Single();
            Vector3 start = first.transform.position;
            first.Simulate(0f);
            Assert.That(first.transform.position, Is.EqualTo(start), "Zero gameplay time must freeze flight.");
            first.Simulate(1f);
            Assert.That(first.IsFlying, Is.False);
            Assert.That(Vector3.Distance(start, first.transform.position), Is.EqualTo(1f).Within(0.001f));

            Assert.That(weapon.TryFire(Vector3.right, 1f), Is.True);
            Assert.That(GetBullets(weapon).Single(), Is.SameAs(first));
            Assert.That(first.IsFlying, Is.True);
            Assert.That(first.transform.position, Is.EqualTo(start));
            first.Simulate(0.01f);
            Assert.That(first.transform.position.x, Is.GreaterThan(start.x));
            Assert.That(first.transform.position.z, Is.EqualTo(start.z));
            weapon.ResetWeapon();
            Assert.That(first.IsFlying, Is.False);
        }
        finally
        {
            DestroyShooter(shooter);
            if (data != null) Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void ShotgunSpawnsSeparateBulletsAndWeaponDisableDoesNotParentTheirFlight()
    {
        var shooter = new GameObject("Shooter");
        WeaponScriptableObject data = null;
        try
        {
            shooter.transform.position = new Vector3(1000f, 10f, 1000f);
            var health = shooter.AddComponent<PlayerStats>();
            health.RestoreFullHealth();
            var weapon = CreateWeapon(shooter, shooter.transform, shooter.transform, health, out data);
            var settings = new SerializedObject(data);
            settings.FindProperty("m_kind").enumValueIndex = (int)WeaponKind.Shotgun;
            settings.FindProperty("m_pelletCount").intValue = 3;
            settings.FindProperty("m_spreadAngle").floatValue = 20f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            weapon.Initialize(data, shooter.transform, health, 1 << 0);

            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            var bullets = GetBullets(weapon);
            Assert.That(bullets.Length, Is.EqualTo(3));
            Vector3 start = shooter.transform.position;
            shooter.transform.position += Vector3.right * 10f;
            weapon.enabled = false;
            foreach (var bullet in bullets)
            {
                Assert.That(bullet.transform.parent, Is.Null);
                Assert.That(bullet.transform.position, Is.EqualTo(start));
                bullet.Simulate(0.1f);
                Assert.That(bullet.transform.position.z, Is.GreaterThan(start.z));
            }
            Assert.That(bullets.Min(bullet => bullet.transform.position.x), Is.LessThan(start.x));
            Assert.That(bullets.Max(bullet => bullet.transform.position.x), Is.GreaterThan(start.x));
            health.TakeDamage(new DamageInfo(100f, start, Vector3.forward, 0f, null));
            foreach (var bullet in bullets)
            {
                bullet.Simulate(0.1f);
                Assert.That(bullet.IsFlying, Is.False);
            }
        }
        finally
        {
            DestroyShooter(shooter);
            if (data != null) Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void FastBulletIgnoresOwnerAndTriggersButStopsAtThinWall()
    {
        var shooter = new GameObject("Shooter");
        var muzzle = new GameObject("Muzzle");
        var wall = new GameObject("Wall");
        var target = new GameObject("Target");
        var trigger = new GameObject("Pickup trigger");
        WeaponScriptableObject data = null;
        try
        {
            shooter.transform.position = new Vector3(1000f, 1f, 1000f);
            var owner = shooter.AddComponent<PlayerStats>();
            owner.RestoreFullHealth();
            var ownerHitbox = new GameObject("Owner hitbox");
            ownerHitbox.transform.SetParent(shooter.transform, false);
            ownerHitbox.transform.localPosition = Vector3.forward;
            ownerHitbox.AddComponent<BoxCollider>();
            trigger.transform.position = shooter.transform.position + Vector3.forward * 2f;
            trigger.AddComponent<BoxCollider>().isTrigger = true;
            muzzle.transform.position = shooter.transform.position;
            wall.transform.position = shooter.transform.position + Vector3.forward * 3f;
            wall.AddComponent<BoxCollider>().size = new Vector3(2f, 2f, 0.01f);
            target.transform.position = shooter.transform.position + Vector3.forward * 4f;
            var targetHealth = target.AddComponent<PlayerStats>();
            targetHealth.RestoreFullHealth();
            target.AddComponent<BoxCollider>();
            var weapon = CreateWeapon(shooter, muzzle.transform, shooter.transform, owner, out data);
            Collider impacted = null;
            weapon.Hit += hit => impacted = hit.collider;
            Physics.SyncTransforms();

            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            var bullet = GetBullets(weapon).Single();
            bullet.Simulate(1f);
            Assert.That(impacted, Is.SameAs(wall.GetComponent<Collider>()));
            Assert.That(owner.CurrentHealth, Is.EqualTo(100f));
            Assert.That(targetHealth.CurrentHealth, Is.EqualTo(100f));
            Assert.That(bullet.IsFlying, Is.False);
        }
        finally
        {
            DestroyShooter(shooter);
            Object.DestroyImmediate(muzzle);
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(trigger);
            if (data != null) Object.DestroyImmediate(data);
        }
    }

    private static BulletBehaviour GetBulletTemplate(WeaponController weapon)
    {
        return (BulletBehaviour)new SerializedObject(weapon).FindProperty("m_bulletPrefab").objectReferenceValue;
    }

    [Test]
    public void PrewarmCreatesDistinctInactiveBulletsAndRepeatedBurstsReuseThem()
    {
        var shooter = new GameObject("Shooter");
        WeaponScriptableObject data = null;
        try
        {
            shooter.transform.position = new Vector3(1000f, 10f, 1000f);
            var owner = shooter.AddComponent<PlayerStats>();
            owner.RestoreFullHealth();
            var weapon = CreateWeapon(shooter, shooter.transform, shooter.transform, owner, out data, 8);
            var prewarmed = GetBullets(weapon);
            Assert.That(prewarmed.Length, Is.EqualTo(8), "Prewarm must create the requested instances before the first shot.");
            Assert.That(prewarmed.All(bullet => !bullet.gameObject.activeSelf), Is.True);

            for (int burst = 0; burst < 125; burst++)
            {
                for (int shot = 0; shot < 8; shot++)
                    Assert.That(weapon.TryFire(Vector3.forward, burst * 8f + shot), Is.True);
                Assert.That(GetBullets(weapon).Count(bullet => bullet.IsFlying), Is.EqualTo(8));
                weapon.ResetWeapon();
            }

            Assert.That(GetBullets(weapon), Is.EquivalentTo(prewarmed),
                "1000 shots within prewarmed capacity must not create additional bullets.");
            Assert.That(prewarmed.All(bullet => !bullet.IsFlying), Is.True);
        }
        finally
        {
            DestroyShooter(shooter);
            if (data != null) Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void RecycleTwiceOrDisableReturnsBulletOnlyOnce()
    {
        var shooter = new GameObject("Shooter");
        WeaponScriptableObject data = null;
        try
        {
            shooter.transform.position = new Vector3(1000f, 10f, 1000f);
            var owner = shooter.AddComponent<PlayerStats>();
            owner.RestoreFullHealth();
            var weapon = CreateWeapon(shooter, shooter.transform, shooter.transform, owner, out data, 1, 2);
            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            var first = GetBullets(weapon).Single();
            first.Recycle();
            first.Recycle();

            Assert.That(weapon.TryFire(Vector3.forward, 1f), Is.True);
            Assert.That(GetBullets(weapon).Single(), Is.SameAs(first));
            // Ordinary MonoBehaviour callbacks are not dispatched by EditMode tests.
            first.SendMessage("OnDisable");
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(weapon.TryFire(Vector3.forward, 2f), Is.True);
            Assert.That(GetBullets(weapon).Single(), Is.SameAs(first));
            Assert.That(weapon.TryFire(Vector3.forward, 3f), Is.True);
            var flying = GetBullets(weapon).Where(bullet => bullet.IsFlying).ToArray();
            Assert.That(flying.Length, Is.EqualTo(2));
            Assert.That(flying[0], Is.Not.SameAs(flying[1]));
        }
        finally
        {
            DestroyShooter(shooter);
            if (data != null) Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void ResetReturnsAllFlyingBulletsAndBoundsRetainedInstances()
    {
        var shooter = new GameObject("Shooter");
        WeaponScriptableObject data = null;
        try
        {
            shooter.transform.position = new Vector3(1000f, 10f, 1000f);
            var owner = shooter.AddComponent<PlayerStats>();
            owner.RestoreFullHealth();
            var weapon = CreateWeapon(shooter, shooter.transform, shooter.transform, owner, out data, 2, 2);
            for (int shot = 0; shot < 5; shot++)
                Assert.That(weapon.TryFire(Vector3.forward, shot), Is.True);
            Assert.That(GetBullets(weapon).Count(bullet => bullet.IsFlying), Is.EqualTo(5),
                "Retention capacity must not drop valid shots.");

            weapon.ResetWeapon();
            Assert.That(GetBullets(weapon).Length, Is.EqualTo(2));
            Assert.That(GetBullets(weapon).All(bullet => !bullet.gameObject.activeSelf), Is.True);
            Assert.That(weapon.TryFire(Vector3.forward, 0f), Is.True);
            Assert.That(GetBullets(weapon).Count(bullet => bullet.IsFlying), Is.EqualTo(1));
            var remaining = GetBullets(weapon);
            weapon.SendMessage("OnDestroy");
            Object.DestroyImmediate(weapon);
            Assert.That(remaining.All(bullet => bullet == null), Is.True,
                "Destroying a weapon must clean up both active and retained bullets.");
        }
        finally
        {
            DestroyShooter(shooter);
            if (data != null) Object.DestroyImmediate(data);
        }
    }

    private static BulletBehaviour[] GetBullets(WeaponController weapon)
    {
        string instanceName = GetBulletTemplate(weapon).name + "(Clone)";
        return Object.FindObjectsByType<BulletBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(bullet => bullet.name == instanceName).ToArray();
    }

    private static void DestroyShooter(GameObject shooter)
    {
        if (shooter == null) return;
        // Explicitly dispatch teardown in EditMode, including world-space pooled instances.
        foreach (var weapon in shooter.GetComponents<WeaponController>())
            weapon.SendMessage("OnDestroy");
        Object.DestroyImmediate(shooter);
    }

    private static WeaponController CreateWeapon(GameObject owner, Transform muzzle, Transform origin,
        PlayerStats stats, out WeaponScriptableObject data, int prewarmCount = 0, int maxRetained = 128)
    {
        data = ScriptableObject.CreateInstance<WeaponScriptableObject>();
        var configuration = new SerializedObject(data);
        configuration.FindProperty("m_damage").floatValue = 8f;
        configuration.FindProperty("m_fireInterval").floatValue = 0.125f;
        configuration.ApplyModifiedPropertiesWithoutUndo();

        var weapon = owner.AddComponent<WeaponController>();
        var template = new GameObject("Test bullet " + System.Guid.NewGuid());
        template.transform.SetParent(owner.transform, false);
        template.SetActive(false);
        var bullet = template.AddComponent<BulletBehaviour>();
        var wiring = new SerializedObject(weapon);
        wiring.FindProperty("m_muzzle").objectReferenceValue = muzzle;
        wiring.FindProperty("m_bulletPrefab").objectReferenceValue = bullet;
        wiring.FindProperty("m_prewarmBulletCount").intValue = prewarmCount;
        wiring.FindProperty("m_maxRetainedBullets").intValue = maxRetained;
        wiring.ApplyModifiedPropertiesWithoutUndo();
        weapon.Initialize(data, origin, stats, 1 << 0);
        weapon.SetGameplayEnabled(true);
        return weapon;
    }
}
