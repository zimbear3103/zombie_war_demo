using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category("WeaponAmmo"), Timeout(3000)]
public class WeaponAmmoTests
{
    private GameObject m_owner;
    private WeaponScriptableObject m_data;
    private WeaponController m_weapon;
    private string m_bulletName;
    private float m_shotTime;
    private float m_originalTimeScale;

    [SetUp]
    public void SetUp()
    {
        m_originalTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        m_shotTime = 0f;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = m_originalTimeScale;
        if (m_owner != null) Object.Destroy(m_owner);
        if (m_data != null) Object.Destroy(m_data);
        yield return null;

        // Clean up only this fixture's world-space bullets if a failed test interrupted teardown.
        foreach (var bullet in FindFixtureBullets()) Object.Destroy(bullet.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RiflePartialReloadPreservesAllSeventySixRounds()
    {
        CreateWeapon(30, 3);
        AssertAmmo(30, 60, 90);
        Assert.That(m_weapon.TryReload(), Is.False);

        Fire(14);
        AssertAmmo(16, 60, 76);
        Assert.That(m_weapon.TryReload(), Is.True);
        AssertAmmo(16, 60, 76);
        Assert.That(m_weapon.TryReload(), Is.False);
        Assert.That(m_weapon.TryFire(Vector3.forward, m_shotTime), Is.False);
        yield return WaitForReload();

        AssertAmmo(30, 46, 76);
        Assert.That(m_weapon.TryReload(), Is.False);
        AssertAmmo(30, 46, 76);
    }

    [UnityTest]
    public IEnumerator RepeatedPartialReloadsUseLastReserveRoundsWithoutCreatingAmmo()
    {
        CreateWeapon(6, 2);
        Fire(3);
        Assert.That(m_weapon.TryReload(), Is.True);
        yield return WaitForReload();
        AssertAmmo(6, 3, 9);

        Fire(4);
        AssertAmmo(2, 3, 5);
        Assert.That(m_weapon.TryReload(), Is.True);
        yield return WaitForReload();
        AssertAmmo(5, 0, 5);
        Assert.That(m_weapon.TryReload(), Is.False);

        Fire(5);
        AssertAmmo(0, 0, 0);
        Assert.That(m_weapon.TryReload(), Is.False);
        Assert.That(m_weapon.TryFire(Vector3.forward, m_shotTime), Is.False);
        Assert.That(m_weapon.IsReloading, Is.False);
        AssertAmmo(0, 0, 0);
    }

    [UnityTest]
    public IEnumerator OneMagazineIncludesLoadedRoundsAndGrantsNoExtraReload()
    {
        CreateWeapon(6, 1);
        AssertAmmo(6, 0, 6);
        Fire(1);
        Assert.That(m_weapon.TryReload(), Is.False);
        AssertAmmo(5, 0, 5);
        Fire(5);
        Assert.That(m_weapon.TryFire(Vector3.forward, m_shotTime), Is.False);
        yield return null;
        Assert.That(m_weapon.IsReloading, Is.False);
        AssertAmmo(0, 0, 0);
    }

    [UnityTest]
    public IEnumerator DisablingWeaponCancelsReloadWithoutSpendingReserve()
    {
        CreateWeapon(6, 2, 0.05f);
        Fire(3);
        Assert.That(m_weapon.TryReload(), Is.True);
        m_weapon.gameObject.SetActive(false);
        Assert.That(m_weapon.IsReloading, Is.False);
        yield return new WaitForSecondsRealtime(0.1f);
        AssertAmmo(3, 6, 9);

        m_weapon.gameObject.SetActive(true);
        Assert.That(m_weapon.TryReload(), Is.True);
        yield return WaitForReload();
        AssertAmmo(6, 3, 9);
    }

    [UnityTest]
    public IEnumerator GameplayAndTimeScalePauseHoldReloadUntilGameplayResumes()
    {
        CreateWeapon(6, 2, 0.05f);
        Fire(3);
        Assert.That(m_weapon.TryReload(), Is.True);
        m_weapon.SetGameplayEnabled(false);
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(m_weapon.IsReloading, Is.True);
        AssertAmmo(3, 6, 9);

        m_weapon.SetGameplayEnabled(true);
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(m_weapon.IsReloading, Is.True);
        AssertAmmo(3, 6, 9);

        Time.timeScale = 1f;
        yield return WaitForReload();
        AssertAmmo(6, 3, 9);
    }

    [UnityTest]
    public IEnumerator ResetDuringReloadRestoresStartingAmmoWithoutDelayedTransfer()
    {
        CreateWeapon(6, 2, 0.05f);
        Fire(3);
        Assert.That(m_weapon.TryReload(), Is.True);
        m_weapon.ResetWeapon();
        Assert.That(m_weapon.IsReloading, Is.False);
        AssertAmmo(6, 6, 12);
        yield return new WaitForSecondsRealtime(0.1f);
        AssertAmmo(6, 6, 12);
        Fire(1);
        AssertAmmo(5, 6, 11);
    }

    [UnityTest]
    public IEnumerator ShotgunConsumesOneRoundAndRaisesOneShotEventPerVolley()
    {
        CreateWeapon(6, 3, kind: WeaponKind.Shotgun);
        int shots = 0;
        m_weapon.Fired += () => shots++;
        Fire(1);
        AssertAmmo(5, 12, 17);
        Assert.That(shots, Is.EqualTo(1));
        Assert.That(FindFixtureBullets().Count(bullet => bullet.IsFlying), Is.EqualTo(3));

        Assert.That(m_weapon.TryReload(), Is.True);
        yield return WaitForReload();
        AssertAmmo(6, 11, 17);
        Assert.That(shots, Is.EqualTo(1));
    }

    private void CreateWeapon(int capacity, int magazines, float reloadTime = 0f,
        WeaponKind kind = WeaponKind.Rifle)
    {
        m_data = ScriptableObject.CreateInstance<WeaponScriptableObject>();
        SetField(m_data, "m_magazineCapacity", capacity);
        SetField(m_data, "m_magazineMaximum", magazines);
        SetField(m_data, "m_reloadTime", reloadTime);
        SetField(m_data, "m_fireInterval", 0.01f);
        SetField(m_data, "m_kind", kind);
        SetField(m_data, "m_pelletCount", 3);

        m_owner = new GameObject("Weapon ammo test");
        m_owner.transform.position = new Vector3(1000f, 1000f, 1000f);
        var stats = m_owner.AddComponent<PlayerStats>();
        m_weapon = m_owner.AddComponent<WeaponController>();
        m_bulletName = "Ammo test bullet " + System.Guid.NewGuid();
        var template = new GameObject(m_bulletName);
        template.transform.SetParent(m_owner.transform, false);
        template.SetActive(false);
        SetField(m_weapon, "m_muzzle", m_owner.transform);
        SetField(m_weapon, "m_bulletPrefab", template.AddComponent<BulletBehaviour>());
        SetField(m_weapon, "m_muzzleFlash", m_owner.AddComponent<MuzzleEffect>());
        SetField(m_weapon, "m_prewarmBulletCount", 0);
        Assert.That(m_weapon.Initialize(m_data, m_owner.transform, stats, 1 << 0), Is.True);
        m_weapon.SetGameplayEnabled(true);
    }

    private void Fire(int rounds)
    {
        for (int index = 0; index < rounds; index++)
        {
            Assert.That(m_weapon.TryFire(Vector3.forward, m_shotTime), Is.True);
            m_shotTime += 1f;
        }
    }

    private IEnumerator WaitForReload()
    {
        float deadline = Time.realtimeSinceStartup + 3f;
        while (m_weapon.IsReloading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(m_weapon.IsReloading, Is.False, "Reload did not finish within three seconds.");
    }

    private void AssertAmmo(int loaded, int reserve, int total)
    {
        Assert.That(m_weapon.AmmoInMagazine, Is.EqualTo(loaded), "Loaded rounds");
        Assert.That(m_weapon.ReserveAmmo, Is.EqualTo(reserve), "Reserve rounds");
        Assert.That(m_weapon.TotalAmmo, Is.EqualTo(total), "Total rounds, including the loaded magazine");
    }

    private BulletBehaviour[] FindFixtureBullets()
    {
        return Object.FindObjectsByType<BulletBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(bullet => bullet.name == m_bulletName + "(Clone)").ToArray();
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing fixture configuration field: " + fieldName);
        field.SetValue(target, value);
    }
}
