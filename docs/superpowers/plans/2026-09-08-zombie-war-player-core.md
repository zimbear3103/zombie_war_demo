# Zombie War Player Core Implementation Plan

> **Paused / scope superseded — 2026-09-08:** Chủ project hoãn prefab, visual/animation và mọi test/build; chuyển sang scripts-only Core/Combat/Enemies. Xem [scripts-only design](../specs/2026-09-08-zombie-war-scripts-core-design.md). Không áp tiếp các code blocks/API cũ lên scripts user vừa sửa.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tạo một player twin-stick có movement/collision, Rifle hitscan, animation, HP và pause/reset, kiểm chứng trong test arena rồi tích hợp vào GameplayZombie.

**Architecture:** Một runtime assembly nhỏ chứa các thư mục Player, Combat và Input bằng asmdef/asmref; không kéo code legacy vào assembly mới. PlayerController sở hữu movement/facing; input router chỉ đọc action, WeaponController sở hữu shot/cooldown/damage, PlayerHealth sở hữu HP và PlayerAnimationController phản ứng với tốc độ thực tế/shot event. Arena harness sở hữu pause/reset; production session flow được triển khai riêng theo roadmap MVP.

**Tech Stack:** Unity `6000.6.0f1`, URP `17.6.0`, Input System `1.20.0`, Cinemachine `6.6.0`, uGUI `2.6.0`, Unity Test Framework `1.8.0`, CharacterController, Humanoid Animator, Shuriken.

**Spec:** [2026-09-08-zombie-war-player-core-design.md](../specs/2026-09-08-zombie-war-player-core-design.md), đã được chủ project chốt ngày 2026-09-08.

## Global Constraints

- Giữ Unity `6000.6.0f1`, URP và uGUI thực tế; không đổi package hoặc render pipeline.
- Private fields dùng `m_camelCase`, Inspector fields dùng `[SerializeField] private`, code comments dùng English. Khi cung cấp source, cung cấp nguyên file.
- Move speed ban đầu `5` world units/giây; aim hoạt động khi processed magnitude **lớn hơn** `0.25`.
- World XZ controls: WASD/leftStick để Move, arrow keys/rightStick để Aim. Aim và movement độc lập; thả aim ngừng bắn ngay.
- Camera follow root, chỉ VisualRoot xoay. CharacterController là owner collision/movement; root motion tắt.
- Một Rifle trong slice này: damage `8`, fire rate `8`/giây, range test `25`. HP player test `100`.
- Không mở rộng sang switching, bom, zombie AI, session/menu/timer, IK hoặc directional footwork.
- Giữ file user đang sửa: `Inputs/InputManager.inputactions`, `Settings/DefaultVolumeProfile.asset`, các `.meta` đã bị user xóa; không stage các file này.
- InputManager vừa được user thêm Look/rightStick, nhưng prefab và scene vẫn trỏ InputSystem_Actions. Sửa đúng asset đang dùng, giữ nguyên file user đang sửa.
- Preserve root/GUID prefab hiện có; sửa asset Unity bằng Editor API/MCP có Undo và save đúng target. Không sửa FBX nguồn hoặc assets trong Library/PackageCache.
- Không kết luận runtime pass chỉ từ compile/serialize. Hai touch đồng thời và visual/feel cần test riêng.
- Gameplay UI reference `1920×1080`, Match `0.5`; smoke test thêm `2400×1080`, `2048×1536`.

## Evidence and execution setup

- Editor path đã tìm thấy: `C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe`.
- Unity MCP đang điều khiển project `F:/zombie_war_demo`; scene hiện tại `Assets/ZombieWar/Scenes/GameplayZombie.unity`.
- Ưu tiên Unity Test Runner trong Editor đang mở. Không mở thêm batchmode vào cùng project đang bị Editor giữ lock.
- Khi cần chạy command C# qua MCP: class phải là `internal class CommandScript : IRunCommand`; chỉ script setup task hiện tại, không broad cleanup.
- Runtime script edits dùng apply_patch. Asset setup dùng Unity Editor/MCP; trước khi đổi scene, kiểm tra dirty state và lưu scene chỉ khi thuộc thay đổi của task. Nếu user có unsaved scene edit, giữ nó bằng additive test scene hoặc báo cần giải quyết đúng scene đó.
- Subagents đã hỗ trợ đọc input/asset nhưng chạm giới hạn sử dụng. Nếu chưa hoạt động lại, dùng executing-plans trực tiếp; không chờ quota để xử lý phần có thể làm trong phiên này.

## File map and assembly layout

| File | Trách nhiệm |
| --- | --- |
| `Assets/ZombieWar/Scripts/Player/ZombieWar.PlayerCore.asmdef` | Runtime assembly, references Unity.InputSystem và UnityEngine.UI |
| `Assets/ZombieWar/Scripts/Combat/ZombieWar.PlayerCore.asmref` | Combat biên dịch cùng runtime assembly |
| `Assets/ZombieWar/Scripts/Input/ZombieWar.PlayerCore.asmref` | Input biên dịch cùng runtime assembly |
| `Assets/ZombieWar/Scripts/Player/PlayerControlRules.cs` | Move clamp, aim threshold và facing priority thuần logic |
| `Assets/ZombieWar/Scripts/Player/PlayerController.cs` | CharacterController, gravity, facing, gameplay gate |
| `Assets/ZombieWar/Scripts/Player/PlayerHealth.cs` | HP, damage, death một lần và reset |
| `Assets/ZombieWar/Scripts/Player/PlayerAnimationController.cs` | Idle/Run + Fire layer, event subscription |
| `Assets/ZombieWar/Scripts/Input/ZombieWarInputRouter.cs` | Một runtime clone của action asset, Move/Aim, focus/disable reset |
| `Assets/ZombieWar/Scripts/Combat/DamageInfo.cs`, `IDamageable.cs` | Shared damage contract |
| `Assets/ZombieWar/Scripts/Combat/WeaponController.cs` | Rifle cadence, cover check, hitscan và shot feedback |
| `Assets/ZombieWar/Tests/Runtime/PlayerCoreArenaHarness.cs` | Pause, Damage10, Reset và target HP display |
| `Assets/ZombieWar/Tests/EditMode/` | Pure rules, health và rifle tests |
| `Assets/ZombieWar/Tests/PlayMode/` | Input/physics/integration tests |
| `Assets/ZombieWar/Animations/Player/PlayerCore.controller`, `PlayerUpperBody.mask` | Project-owned animation setup |
| `Assets/ZombieWar/Scenes/PlayerCoreTest.unity` | Arena riêng, không thêm vào production build list |
| `Assets/ZombieWar/Prefabs/Ingame/Player.prefab` | Prefab hiện có được nâng cấp tại chỗ |
| `Assets/ZombieWar/Inputs/InputSystem_Actions.inputactions` | Move/Aim actions và keyboard/gamepad bindings |
| `Assets/ZombieWar/Scenes/GameplayZombie.unity` | Tích hợp prefab và hai joystick đã qua arena |
| `docs/testing/2026-09-08-player-core-verification.md` | Kết quả thực tế, ảnh/test counts, các giới hạn chưa kiểm chứng |

Runtime asmdef được auto-reference để code Assembly-CSharp còn lại dùng PlayerCore được. Runtime không tham chiếu Assembly-CSharp. Arena harness để ngoài test asmdef, trong Assembly-CSharp, nên có thể reuse `Crystal.SafeArea` lúc setup mà không dời utilities vào assembly mới.

Các code blocks dưới đây là full files để bắt đầu mỗi task. Inspector/prefab wiring và các kiểm chứng liệt kê trong cùng task là một phần bắt buộc của deliverable; code block không thay thế runtime verification.

---

### Task 1: Damage/health và control rules có test độc lập

**Files:** Create PlayerControlRules.cs, DamageInfo.cs, IDamageable.cs, runtime asmdef/asmrefs; modify PlayerHealth.cs; create Tests/EditMode/ZombieWar.PlayerCore.EditModeTests.asmdef và PlayerCoreRulesTests.cs. Xóa duy nhất unused `using Unity.VisualScripting;` khỏi PlayerController trước khi chuyển nó vào runtime assembly.

**Interfaces:** Produces `PlayerControlRules.Move(Vector2)`, `IsAiming(Vector2,float)`, `Facing(Vector2,Vector2,Vector3,float)`; `PlayerHealth : IDamageable` với `CurrentHealth`, `MaxHealth`, `IsAlive`, `IsDead`, `HealthChanged`, `Died`, `TakeDamage(DamageInfo)`, `RestoreFullHealth()`.

- [x] **Step 1: Add the runtime and EditMode assembly configuration.**

Full runtime asmdef:

```json
{
  "name": "ZombieWar.PlayerCore",
  "references": ["Unity.InputSystem", "UnityEngine.UI"],
  "autoReferenced": true
}
```

Full asmref in both Combat and Input folders:

```json
{
  "reference": "ZombieWar.PlayerCore"
}
```

Full EditMode asmdef:

```json
{
  "name": "ZombieWar.PlayerCore.EditModeTests",
  "references": ["ZombieWar.PlayerCore", "Unity.InputSystem", "Unity.InputSystem.TestFramework"],
  "includePlatforms": ["Editor"],
  "optionalUnityReferences": ["TestAssemblies"],
  "autoReferenced": false
}
```

Import with Unity so `.meta` files exist. If Editor stores asmref by GUID, resolve the generated runtime asmdef GUID and use Editor's assembly reference picker; never invent a GUID. Confirm existing scripts still compile before adding failing tests.

- [x] **Step 2: Add the full failing test file PlayerCoreRulesTests.cs.**

```csharp
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
```

- [x] **Step 3: Run EditMode filter PlayerCoreRulesTests and confirm the expected missing-type/member failures.** Save actual failure evidence; do not count unrelated project compile errors as the intended red test.

- [x] **Step 4: Add the complete contracts, rules and health implementation.**

Full DamageInfo.cs:

```csharp
using UnityEngine;

public readonly struct DamageInfo
{
    public readonly float amount;
    public readonly Vector3 hitPoint;
    public readonly Vector3 hitDirection;
    public readonly float knockbackForce;
    public readonly GameObject source;

    public DamageInfo(float amount, Vector3 hitPoint, Vector3 hitDirection, float knockbackForce, GameObject source)
    {
        this.amount = amount;
        this.hitPoint = hitPoint;
        this.hitDirection = hitDirection;
        this.knockbackForce = knockbackForce;
        this.source = source;
    }
}
```

Full IDamageable.cs:

```csharp
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(DamageInfo damageInfo);
}
```

Full PlayerControlRules.cs:

```csharp
using UnityEngine;

public static class PlayerControlRules
{
    public static Vector3 Move(Vector2 input)
    {
        input = Vector2.ClampMagnitude(input, 1f);
        return new Vector3(input.x, 0f, input.y);
    }

    public static bool IsAiming(Vector2 aim, float deadZone)
    {
        return aim.sqrMagnitude > deadZone * deadZone;
    }

    public static Vector3 Facing(Vector2 move, Vector2 aim, Vector3 previous, float deadZone)
    {
        if (IsAiming(aim, deadZone)) return Move(aim).normalized;
        Vector3 motion = Move(move);
        return motion.sqrMagnitude > 0.0001f ? motion.normalized : previous;
    }
}
```

Full PlayerHealth.cs:

```csharp
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1f)] private float m_maxHealth = 100f;
    private float m_currentHealth;

    public float CurrentHealth => m_currentHealth;
    public float MaxHealth => m_maxHealth;
    public bool IsAlive => m_currentHealth > 0f;
    public bool IsDead => !IsAlive;
    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake() => RestoreFullHealth();

    public void RestoreFullHealth()
    {
        if (float.IsNaN(m_maxHealth) || float.IsInfinity(m_maxHealth) || m_maxHealth <= 0f)
            m_maxHealth = 100f;
        m_currentHealth = m_maxHealth;
        HealthChanged?.Invoke(m_currentHealth, m_maxHealth);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        float amount = damageInfo.amount;
        if (!IsAlive || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        m_currentHealth = Mathf.Max(0f, m_currentHealth - amount);
        HealthChanged?.Invoke(m_currentHealth, m_maxHealth);
        if (!IsAlive) Died?.Invoke();
    }
}
```

- [x] **Step 5: Re-run PlayerCoreRulesTests, verify pass, then review public names against interfaces.** All tests destroy objects they create; no scene changes are required for this task.
- [x] **Step 6: Commit only the task's paths and generated metas.** Message: `feat: add player control rules and health contracts`. Keep user's InputManager diff unstaged.

**Acceptance:** Pure input/facing rules and real health component pass meaningful tests; legacy scripts still compile without an Assembly-CSharp dependency cycle.

### Task 2: Rifle cadence and damage through cover

**Files:** Modify Combat/WeaponController.cs; create Tests/EditMode/PlayerCoreWeaponTests.cs. Produces `Configure(Transform muzzle, Transform origin, PlayerHealth owner, LayerMask mask)`, `SetGameplayEnabled(bool)`, `ResetWeapon()`, `TryFire(Vector3 direction, float now)`, `Fired`, `Hit`.

Move the minimal damage/weapon foundations ahead of player wiring so PlayerController can compile against real behavior, not placeholder weapon methods. This does not require full weapon switching or presentation.

- [ ] **Step 1: Add full PlayerCoreWeaponTests.cs and run it red.**

```csharp
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
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(origin);
            Object.DestroyImmediate(muzzle);
        }
    }
}
```

- [ ] **Step 2: Replace WeaponController.cs with this full baseline.**

```csharp
using System;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform m_muzzle;
    [SerializeField] private Transform m_obstructionOrigin;
    [SerializeField] private PlayerHealth m_owner;
    [SerializeField] private LayerMask m_hitMask = 1;
    [SerializeField, Min(0.01f)] private float m_damage = 8f;
    [SerializeField, Min(0.01f)] private float m_shotsPerSecond = 8f;
    [SerializeField, Min(0.01f)] private float m_range = 25f;
    [SerializeField] private ParticleSystem m_muzzleEffect;
    [SerializeField] private ParticleSystem[] m_hitEffects;
    [SerializeField] private AudioSource m_audio;
    [SerializeField] private AudioClip m_fireClip;
    private bool m_gameplayEnabled = true;
    private bool m_reportedConfigurationError;
    private float m_nextShotTime = float.NegativeInfinity;
    private int m_effectIndex;

    public event Action Fired;
    public event Action<RaycastHit> Hit;

    public void Configure(Transform muzzle, Transform origin, PlayerHealth owner, LayerMask mask)
    {
        m_muzzle = muzzle;
        m_obstructionOrigin = origin;
        m_owner = owner;
        m_hitMask = mask;
        m_reportedConfigurationError = false;
    }

    public void SetGameplayEnabled(bool value) => m_gameplayEnabled = value;

    public void ResetWeapon()
    {
        m_nextShotTime = float.NegativeInfinity;
        if (m_muzzleEffect != null) m_muzzleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (m_hitEffects != null)
            foreach (var effect in m_hitEffects)
                if (effect != null) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public bool TryFire(Vector3 direction, float now)
    {
        if (!m_gameplayEnabled || !isActiveAndEnabled || m_owner == null || !m_owner.IsAlive) return false;
        if (float.IsNaN(now) || float.IsInfinity(now) || now < m_nextShotTime) return false;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;
        if (m_muzzle == null || m_obstructionOrigin == null || m_hitMask.value == 0 ||
            !PositiveFinite(m_damage) || !PositiveFinite(m_shotsPerSecond) || !PositiveFinite(m_range))
        {
            if (!m_reportedConfigurationError)
                Debug.LogError("WeaponController requires muzzle, obstruction origin, hit mask and positive finite stats.", this);
            m_reportedConfigurationError = true;
            return false;
        }

        direction.Normalize();
        m_nextShotTime = now + 1f / m_shotsPerSecond;
        Vector3 toMuzzle = m_muzzle.position - m_obstructionOrigin.position;
        RaycastHit coverHit = default;
        bool blocked = toMuzzle.sqrMagnitude > 0.0001f && Physics.Raycast(
            m_obstructionOrigin.position, toMuzzle.normalized, out coverHit,
            toMuzzle.magnitude + 0.02f, m_hitMask, QueryTriggerInteraction.Ignore);
        if (blocked)
        {
            ApplyHit(coverHit, direction);
        }
        else if (Physics.Raycast(m_muzzle.position, direction, out RaycastHit hit,
                     m_range, m_hitMask, QueryTriggerInteraction.Ignore))
        {
            ApplyHit(hit, direction);
        }

        if (m_muzzleEffect != null) m_muzzleEffect.Play(true);
        if (m_audio != null && m_fireClip != null) m_audio.PlayOneShot(m_fireClip);
        Fired?.Invoke();
        return true;
    }

    private void ApplyHit(RaycastHit hit, Vector3 direction)
    {
        var target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null && target.IsAlive && !hit.collider.transform.IsChildOf(m_owner.transform))
            target.TakeDamage(new DamageInfo(m_damage, hit.point, direction, 0f, m_owner.gameObject));
        if (m_hitEffects != null && m_hitEffects.Length > 0)
        {
            var effect = m_hitEffects[m_effectIndex++ % m_hitEffects.Length];
            if (effect != null)
            {
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
                effect.Play(true);
            }
        }
        Hit?.Invoke(hit);
    }

    private static bool PositiveFinite(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
```

- [ ] **Step 3: Run all four weapon test cases green and compile all runtime scripts.** Three parameterized geometry cases exercise child hitbox damage, distant cover and muzzle cover; the fourth proves cadence/death gates.
- [ ] **Step 4: Inspect the geometry fixture results and ensure every temporary object is destroyed even on failure.** Run the class twice to catch leaked colliders changing the second result.
- [ ] **Step 5: Review and commit task paths.** Message: `feat: add rifle hitscan and cover-safe damage`.

**Acceptance:** Actual raycasts hit the first collider, child hitboxes route damage once, cover blocks muzzle protrusion, and cooldown cannot be bypassed by re-aim or frame stalls.

### Task 3: Independent input and CharacterController movement in a test arena

**Files:** Create Input/ZombieWarInputRouter.cs, Tests/EditMode/PlayerCoreInputTests.cs, Tests/PlayMode/ZombieWar.PlayerCore.PlayModeTests.asmdef and PlayerCoreMovementTests.cs; replace Player/PlayerController.cs; modify InputSystem_Actions.inputactions; create PlayerCoreTest.unity. Existing Player.prefab is upgraded in Task 4 after the isolated arena proves movement.

**Interfaces:** Router produces `MoveInput`, `AimInput`, `Configure(InputActionAsset, OnScreenStick[])`, `SetGameplayEnabled(bool)`, `ResetVirtualControls()`. Player produces `AimDirection`, `IsAiming`, `NormalizedMoveSpeed`, `CanAct`, `Configure(ZombieWarInputRouter, CharacterController, PlayerHealth, WeaponController, Transform)`, `SetGameplayEnabled(bool)`, `Tick(float,float)`, `ResetAt(Vector3,Quaternion)`.

- [ ] **Step 1: Add the full PlayerCoreInputTests.cs and run it red.**

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCoreInputTests : InputTestFixture
{
    [Test]
    public void SticksRemainIndependentAndReleaseDuringPauseIsNotReplayed()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
        map.AddAction("Aim", InputActionType.Value, "<Gamepad>/rightStick");
        asset.AddActionMap(map);
        var root = new GameObject("Router test");
        root.SetActive(false);
        try
        {
            var router = root.AddComponent<ZombieWarInputRouter>();
            router.Configure(asset, null);
            root.SetActive(true);
            var gamepad = InputSystem.AddDevice<Gamepad>();
            Set(gamepad.leftStick, Vector2.up);
            Set(gamepad.rightStick, Vector2.left);
            Assert.That(router.MoveInput, Is.EqualTo(Vector2.up));
            Assert.That(router.AimInput, Is.EqualTo(Vector2.left));
            router.SetGameplayEnabled(false);
            Set(gamepad.rightStick, Vector2.zero);
            Assert.That(router.MoveInput, Is.EqualTo(Vector2.zero));
            router.SetGameplayEnabled(true);
            InputSystem.Update();
            Assert.That(router.MoveInput, Is.EqualTo(Vector2.up));
            Assert.That(router.AimInput, Is.EqualTo(Vector2.zero));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(asset);
        }
    }
}
```

- [ ] **Step 2: Add the full input router.** It uses a clone of the chosen action asset without PlayerInput device pairing; remove the old PlayerInput component when wiring the new test player. Keep OnScreenStick `useIsolatedInputActions=false` because there is no auto-switching PlayerInput and the public OnPointerUp reset API is valid in this mode. This choice follows the installed Input System source, not package-version assumptions.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;

public class ZombieWarInputRouter : MonoBehaviour
{
    [SerializeField] private InputActionAsset m_actions;
    [SerializeField] private OnScreenStick[] m_virtualSticks;
    private InputActionAsset m_runtimeActions;
    private InputActionMap m_playerMap;
    private InputAction m_move;
    private InputAction m_aim;
    private bool m_requestedEnabled = true;
    private bool m_hasFocus = true;

    public Vector2 MoveInput => CanRead ? m_move.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 AimInput => CanRead ? m_aim.ReadValue<Vector2>() : Vector2.zero;
    private bool CanRead => isActiveAndEnabled && m_hasFocus && m_requestedEnabled && m_playerMap != null && m_playerMap.enabled;

    public void Configure(InputActionAsset actions, OnScreenStick[] sticks)
    {
        ReleaseActions();
        m_actions = actions;
        m_virtualSticks = sticks;
        Initialize();
        ApplyEnabled();
    }

    private void OnEnable()
    {
        Initialize();
        ApplyEnabled();
    }

    private void Start()
    {
        if (m_playerMap == null)
            Debug.LogError("ZombieWarInputRouter requires Player/Move and Player/Aim Vector2 actions.", this);
    }

    private void Initialize()
    {
        if (m_runtimeActions != null || m_actions == null) return;
        m_runtimeActions = Instantiate(m_actions);
        m_playerMap = m_runtimeActions.FindActionMap("Player", false);
        m_move = m_playerMap?.FindAction("Move", false);
        m_aim = m_playerMap?.FindAction("Aim", false);
        if (m_move == null || m_aim == null || m_move.type != InputActionType.Value || m_aim.type != InputActionType.Value)
        {
            ReleaseActions();
            return;
        }
    }

    public void SetGameplayEnabled(bool value)
    {
        m_requestedEnabled = value;
        ApplyEnabled();
    }

    private void ApplyEnabled()
    {
        if (m_playerMap == null) return;
        if (isActiveAndEnabled && m_hasFocus && m_requestedEnabled) m_playerMap.Enable();
        else m_playerMap.Disable();
    }

    public void ResetVirtualControls()
    {
        if (m_virtualSticks == null) return;
        foreach (var stick in m_virtualSticks)
            if (stick != null && !stick.useIsolatedInputActions) stick.OnPointerUp(null);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        m_hasFocus = hasFocus;
        if (!hasFocus) ResetVirtualControls();
        ApplyEnabled();
    }

    private void OnDisable()
    {
        m_playerMap?.Disable();
        ResetVirtualControls();
    }

    private void OnDestroy() => ReleaseActions();

    private void ReleaseActions()
    {
        m_playerMap?.Disable();
        m_move = null;
        m_aim = null;
        m_playerMap = null;
        if (m_runtimeActions != null)
        {
            if (Application.isPlaying) Destroy(m_runtimeActions);
            else DestroyImmediate(m_runtimeActions);
        }
        m_runtimeActions = null;
    }
}
```

- [ ] **Step 3: Modify only the Player map of the actual input asset.** Add Aim Value/Vector2 with rightStick and a 2DVector arrow-key composite. Remove arrow-key paths only from Player/Move; preserve WASD, gamepad Move, unrelated action IDs and UI bindings. Do not reuse Look's mouse delta. Keep existing asset GUID and reference the actual imported asset on the new router. Rerun PlayerCoreInputTests green.

- [ ] **Step 4: Add the PlayMode asmdef and full movement test file; run the movement tests red before implementing the host.**

```json
{
  "name": "ZombieWar.PlayerCore.PlayModeTests",
  "references": ["ZombieWar.PlayerCore", "Unity.InputSystem", "Unity.InputSystem.TestFramework"],
  "optionalUnityReferences": ["TestAssemblies"],
  "autoReferenced": false
}
```

Add the full PlayerCoreMovementTests.cs below and run PlayMode filter PlayerCoreMovementTests. Expected red: the existing PlayerController lacks Configure/Tick and the lifecycle API. It uses temporary objects far from the live arena, restores Input System state through InputTestFixture, and never saves the scene. Add failing cases before corresponding fixes when the integrated behavior differs from the spec.

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCoreMovementTests : InputTestFixture
{
    private readonly List<GameObject> m_objects = new List<GameObject>();
    private InputActionAsset m_asset;

    public override void TearDown()
    {
        for (int i = m_objects.Count - 1; i >= 0; i--)
            if (m_objects[i] != null) Object.DestroyImmediate(m_objects[i]);
        m_objects.Clear();
        if (m_asset != null) Object.DestroyImmediate(m_asset);
        base.TearDown();
    }

    private GameObject CreateObject(string name)
    {
        var value = new GameObject(name);
        m_objects.Add(value);
        return value;
    }

    private PlayerController CreatePlayer(out PlayerHealth health, out Transform visual, out WeaponController weapon)
    {
        m_asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
        map.AddAction("Aim", InputActionType.Value, "<Gamepad>/rightStick");
        m_asset.AddActionMap(map);
        var floor = CreateObject("Test floor");
        floor.transform.position = new Vector3(1000f, -0.25f, 1000f);
        var floorCollider = floor.AddComponent<BoxCollider>();
        floorCollider.size = new Vector3(20f, 0.5f, 20f);
        var root = CreateObject("Test player");
        root.SetActive(false);
        root.layer = 2;
        root.transform.position = new Vector3(1000f, 0f, 1000f);
        var controller = root.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.35f;
        controller.center = Vector3.up;
        health = root.AddComponent<PlayerHealth>();
        weapon = root.AddComponent<WeaponController>();
        var input = root.AddComponent<ZombieWarInputRouter>();
        input.Configure(m_asset, null);
        visual = CreateObject("VisualRoot").transform;
        visual.SetParent(root.transform, false);
        var muzzle = CreateObject("Muzzle").transform;
        muzzle.SetParent(visual, false);
        muzzle.localPosition = new Vector3(0f, 1.4f, 0.6f);
        var origin = CreateObject("Origin").transform;
        origin.SetParent(root.transform, false);
        origin.localPosition = new Vector3(0f, 1.4f, 0f);
        weapon.Configure(muzzle, origin, health, 1 << 0);
        var player = root.AddComponent<PlayerController>();
        player.Configure(input, controller, health, weapon, visual);
        root.SetActive(true);
        health.RestoreFullHealth();
        Physics.SyncTransforms();
        return player;
    }

    [Test]
    public void MoveAndAimAreIndependentAndPauseDeathResetAreConsistent()
    {
        var player = CreatePlayer(out var health, out var visual, out var weapon);
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Set(gamepad.leftStick, Vector2.up);
        Set(gamepad.rightStick, Vector2.left);
        int shots = 0;
        weapon.Fired += () => shots++;
        Vector3 start = player.transform.position;
        for (int i = 0; i < 25; i++) player.Tick(0.02f, i * 0.02f);
        Assert.That(player.transform.position.z - start.z, Is.EqualTo(2.5f).Within(0.08f));
        Assert.That(Vector3.Dot(visual.forward, Vector3.left), Is.GreaterThan(0.999f));
        Assert.That(shots, Is.GreaterThan(1));
        player.SetGameplayEnabled(false);
        Set(gamepad.rightStick, Vector2.zero);
        Vector3 paused = player.transform.position;
        int pausedShots = shots;
        player.Tick(1f, 1f);
        Assert.That(player.transform.position, Is.EqualTo(paused));
        Assert.That(shots, Is.EqualTo(pausedShots));
        player.SetGameplayEnabled(true);
        InputSystem.Update();
        player.Tick(0.02f, 1.02f);
        Assert.That(player.IsAiming, Is.False);
        health.TakeDamage(new DamageInfo(100f, Vector3.zero, Vector3.forward, 0f, null));
        Vector3 deadPosition = player.transform.position;
        Set(gamepad.rightStick, Vector2.left);
        player.Tick(0.02f, 1.04f);
        Assert.That(player.transform.position.z, Is.EqualTo(deadPosition.z).Within(0.001f));
        Assert.That(shots, Is.EqualTo(pausedShots));
        player.ResetAt(start, Quaternion.identity);
        Assert.That(health.CurrentHealth, Is.EqualTo(100f));
        Assert.That(player.CanAct, Is.True);
    }

    [Test]
    public void WallStopsMovementAndActualSpeedFallsToZero()
    {
        var player = CreatePlayer(out _, out _, out _);
        var wall = CreateObject("Wall");
        wall.transform.position = new Vector3(1000f, 1f, 1002f);
        wall.AddComponent<BoxCollider>().size = new Vector3(5f, 2f, 0.5f);
        Physics.SyncTransforms();
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Set(gamepad.leftStick, Vector2.up);
        for (int i = 0; i < 60; i++) player.Tick(0.02f, i * 0.02f);
        Assert.That(player.transform.position.z, Is.LessThan(1001.6f));
        Assert.That(player.NormalizedMoveSpeed, Is.LessThan(0.05f));
    }
}
```

- [ ] **Step 5: Replace PlayerController.cs with the full movement host.**

```csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerHealth), typeof(WeaponController))]
[RequireComponent(typeof(ZombieWarInputRouter))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private ZombieWarInputRouter m_input;
    [SerializeField] private CharacterController m_controller;
    [SerializeField] private PlayerHealth m_health;
    [SerializeField] private WeaponController m_weapon;
    [SerializeField] private Transform m_visualRoot;
    [SerializeField, Min(0.01f)] private float m_moveSpeed = 5f;
    [SerializeField, Range(0f, 1f)] private float m_aimDeadZone = 0.25f;
    [SerializeField, Min(1f)] private float m_turnSpeed = 720f;
    [SerializeField] private float m_gravity = -25f;
    [SerializeField] private float m_terminalVelocity = -35f;
    private bool m_gameplayEnabled = true;
    private bool m_reportedConfigurationError;
    private float m_verticalVelocity;
    private Vector3 m_facing = Vector3.forward;

    public Vector3 AimDirection => m_facing;
    public bool IsAiming { get; private set; }
    public float NormalizedMoveSpeed { get; private set; }
    public bool CanAct => m_gameplayEnabled && m_health != null && m_health.IsAlive;

    private void Awake()
    {
        if (m_input == null) m_input = GetComponent<ZombieWarInputRouter>();
        if (m_controller == null) m_controller = GetComponent<CharacterController>();
        if (m_health == null) m_health = GetComponent<PlayerHealth>();
        if (m_weapon == null) m_weapon = GetComponent<WeaponController>();
        if (m_visualRoot == null) m_visualRoot = transform.Find("VisualRoot");
    }

    public void Configure(ZombieWarInputRouter input, CharacterController controller, PlayerHealth health, WeaponController weapon, Transform visual)
    {
        if (m_health != null) m_health.Died -= OnDied;
        m_input = input;
        m_controller = controller;
        m_health = health;
        m_weapon = weapon;
        m_visualRoot = visual;
        m_reportedConfigurationError = false;
        if (isActiveAndEnabled && m_health != null) m_health.Died += OnDied;
    }

    private void OnEnable()
    {
        if (m_health != null)
        {
            m_health.Died -= OnDied;
            m_health.Died += OnDied;
        }
    }

    private void Update() => Tick(Time.deltaTime, Time.time);

    public void Tick(float deltaTime, float now)
    {
        if (m_input == null || m_controller == null || m_health == null || m_weapon == null || m_visualRoot == null)
        {
            if (!m_reportedConfigurationError)
                Debug.LogError("PlayerController requires input, character controller, health, weapon and VisualRoot.", this);
            m_reportedConfigurationError = true;
            return;
        }
        m_input.SetGameplayEnabled(CanAct);
        m_weapon.SetGameplayEnabled(CanAct);
        if (!m_gameplayEnabled || deltaTime <= 0f || !m_controller.enabled)
        {
            IsAiming = false;
            NormalizedMoveSpeed = 0f;
            return;
        }

        Vector2 moveInput = CanAct ? m_input.MoveInput : Vector2.zero;
        Vector2 aimInput = CanAct ? m_input.AimInput : Vector2.zero;
        Vector3 horizontal = PlayerControlRules.Move(moveInput) * m_moveSpeed;
        if (m_controller.isGrounded && m_verticalVelocity < 0f) m_verticalVelocity = -2f;
        m_verticalVelocity = Mathf.Max(m_terminalVelocity, m_verticalVelocity + m_gravity * deltaTime);
        Vector3 before = transform.position;
        m_controller.Move((horizontal + Vector3.up * m_verticalVelocity) * deltaTime);
        Vector3 displacement = transform.position - before;
        displacement.y = 0f;
        NormalizedMoveSpeed = CanAct ? Mathf.Clamp01(displacement.magnitude / (Mathf.Max(0.01f, m_moveSpeed) * deltaTime)) : 0f;
        IsAiming = CanAct && PlayerControlRules.IsAiming(aimInput, m_aimDeadZone);
        if (!CanAct) return;

        m_facing = PlayerControlRules.Facing(moveInput, aimInput, m_facing, m_aimDeadZone);
        Quaternion rotation = Quaternion.LookRotation(m_facing, Vector3.up);
        m_visualRoot.rotation = IsAiming ? rotation : Quaternion.RotateTowards(m_visualRoot.rotation, rotation, m_turnSpeed * deltaTime);
        if (IsAiming) m_weapon.TryFire(m_facing, now);
    }

    public void SetGameplayEnabled(bool value)
    {
        m_gameplayEnabled = value;
        if (m_input != null) m_input.SetGameplayEnabled(CanAct);
        if (m_weapon != null) m_weapon.SetGameplayEnabled(CanAct);
        if (!value) { IsAiming = false; NormalizedMoveSpeed = 0f; }
    }

    private void OnDied()
    {
        IsAiming = false;
        NormalizedMoveSpeed = 0f;
        m_input?.SetGameplayEnabled(false);
        m_input?.ResetVirtualControls();
        m_weapon?.SetGameplayEnabled(false);
        m_weapon?.ResetWeapon();
    }

    public void ResetAt(Vector3 position, Quaternion rotation)
    {
        m_controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        m_controller.enabled = true;
        m_visualRoot.localRotation = Quaternion.identity;
        m_facing = m_visualRoot.forward;
        m_verticalVelocity = 0f;
        IsAiming = false;
        NormalizedMoveSpeed = 0f;
        m_health.RestoreFullHealth();
        m_weapon.ResetWeapon();
        m_input.ResetVirtualControls();
        SetGameplayEnabled(true);
    }

    private void OnDisable()
    {
        if (m_health != null) m_health.Died -= OnDied;
        m_input?.SetGameplayEnabled(false);
        m_input?.ResetVirtualControls();
        m_weapon?.SetGameplayEnabled(false);
    }
}
```

- [ ] **Step 6: Build a minimal additive PlayerCoreTest arena using Editor/MCP.** Before changing scene selection, inspect current dirty state. Arena geometry: floor cube center `(0,-0.25,0)` scale `(30,0.5,30)`; wall center `(0,1,6)` scale `(8,2,0.5)`; clear running area at `(0,0,0)`. Add a `30°` ramp and a `60°` ramp as separate collidable cubes, separated in X so tests cannot confuse them. Test a `0.2`-unit step. CharacterController initial tuning for a 2-unit player: height `2`, center `(0,1,0)`, radius `0.35`, stepOffset `0.3`, slopeLimit `45`, skinWidth `0.04`, minMoveDistance `0`. Do not reuse capsule mesh/collider scale blindly when adding the soldier next task.
- [ ] **Step 7: Wire a temporary player, camera and debug aim marker.** Root has the runtime components above and VisualRoot child. Muzzle at local `(0,1.4,0.6)` and obstruction origin `(0,1.4,0)` for the temporary visual. No dynamic Rigidbody or old PlayerInput. Player and all child colliders use a dedicated Player layer excluded by weapon hitMask. Use an available user layer slot, preserve all pre-existing names and resolve the actual layer index through LayerMask.NameToLayer; do not assume index 8 is free.
- [ ] **Step 8: Play and test the control table, wall/step/ramp movement, gravity and camera follow.** Confirm 5 units/second cardinal vs diagonal in clear terrain, slower analog, no shot below threshold, left/right vectors independent. Test death while above ground: no input movement/fire but gravity still settles; paused controller does not move at all.
- [ ] **Step 9: Re-run the full PlayerCoreMovementTests green.** The test source was added in Step 4 before implementing the host; review both input/aim/pause/death/reset and wall-contact results. Record actual output.
- [ ] **Step 10: Re-run the relevant EditMode and PlayMode tests and commit task files.** Message: `feat: add independent twin stick player movement`.

**Acceptance:** A real CharacterController player moves/aims independently in the test arena with correct collision, slope, gravity and gameplay gating; all referenced APIs have real implementations.

### Task 4: Soldier, camera and visible shot feedback

**Files:** Replace Player/PlayerAnimationController.cs; modify Player.prefab; create Animations/Player/PlayerCore.controller, PlayerUpperBody.mask; create owned effect objects/prefabs under Prefabs/Effects/PlayerCore; modify test arena. Consumes player speed/IsAiming/health and WeaponController.Fired. Produces `PlayerAnimationController.ResetPresentation()`.

- [ ] **Step 1: Preview the three exact imported animation clips in the Editor.** Use `infantry_combat_idle` (2 seconds, looping), `infantry_combat_run` (0.8333334 seconds, looping), `infantry_combat_shoot` (1 second, non-looping), not the `__preview__Take 001` clips also embedded in each FBX. Inspect weapon/grip pose and choose the intended shoot phase before assigning animator states.
- [ ] **Step 2: Upgrade the existing Player prefab via prefab contents editing.** Keep root identity/GUID. Remove old PlayerInput, dynamic Rigidbody, CapsuleCollider and capsule render mesh from this player only. Add CharacterController, router, health, weapon, player and animation components; wire all fields. Instantiate ToonSoldier as nested prefab under VisualRoot. Measure skinned bounds, scale the nested visual so standing height is approximately 2 units, then fit CC height/center/radius. Agent observed the raw soldier bounds near 2.60 units; that measurement must be rechecked after instantiate. Source soldier material is already URP/Lit; do not reconvert asset-pack materials.
- [ ] **Step 3: Author PlayerCore.controller and PlayerUpperBody.mask.** Parameters: Float MoveSpeed, Trigger Fire. Base layer has a 1D blend tree `Locomotion`, thresholds 0=idle and 1=run. UpperBody override layer weight 1 has Empty default state and Shoot. Mask includes body/head/arms/fingers, excludes root and both legs; Root motion remains off. AnyState→Shoot uses Fire, no exit time, fixed-duration blend 0.015 s, self-transition allowed. Shoot→Empty has exit time 1, duration 0.025 s. Start Shoot state speed at 10 to fit 1 s source into 0.1 s; preview at rifle 8 Hz and tune state speed/blends to make the recoil readable without holding the first pose. Only modify the project-owned controller/mask, not FBX import settings.
- [ ] **Step 4: Replace PlayerAnimationController.cs with this complete file.**

```csharp
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator m_animator;
    [SerializeField] private PlayerController m_player;
    [SerializeField] private PlayerHealth m_health;
    [SerializeField] private WeaponController m_weapon;
    private static readonly int m_moveSpeedId = Animator.StringToHash("MoveSpeed");
    private static readonly int m_fireId = Animator.StringToHash("Fire");
    private bool m_wasDead;

    private void Awake()
    {
        if (m_player == null) m_player = GetComponent<PlayerController>();
        if (m_health == null) m_health = GetComponent<PlayerHealth>();
        if (m_weapon == null) m_weapon = GetComponent<WeaponController>();
        if (m_animator == null) m_animator = GetComponentInChildren<Animator>();
        if (m_animator != null) m_animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        if (m_weapon != null)
        {
            m_weapon.Fired -= OnFired;
            m_weapon.Fired += OnFired;
        }
    }

    private void Start()
    {
        if (m_animator == null || m_animator.runtimeAnimatorController == null || m_player == null || m_health == null || m_weapon == null)
            Debug.LogError("PlayerAnimationController requires Animator/controller, player, health and weapon.", this);
    }

    private void LateUpdate()
    {
        if (m_animator == null || m_animator.runtimeAnimatorController == null || m_player == null || m_health == null) return;
        if (Time.timeScale <= 0f) return;
        if (m_health.IsDead)
        {
            if (!m_wasDead) ResetPresentation();
            m_wasDead = true;
            return;
        }
        m_wasDead = false;
        m_animator.SetFloat(m_moveSpeedId, m_player.NormalizedMoveSpeed, 0.08f, Time.deltaTime);
    }

    private void OnFired()
    {
        if (m_animator != null && m_animator.runtimeAnimatorController != null && m_player != null && m_player.CanAct)
            m_animator.SetTrigger(m_fireId);
    }

    public void ResetPresentation()
    {
        if (m_animator == null || m_animator.runtimeAnimatorController == null) return;
        m_animator.ResetTrigger(m_fireId);
        m_animator.SetFloat(m_moveSpeedId, 0f);
        m_animator.Play("Base Layer.Locomotion", 0, 0f);
        int upperBody = m_animator.GetLayerIndex("UpperBody");
        if (upperBody >= 0) m_animator.Play("UpperBody.Empty", upperBody, 0f);
        m_wasDead = false;
    }

    private void OnDisable()
    {
        if (m_weapon != null) m_weapon.Fired -= OnFired;
        ResetPresentation();
    }
}
```

- [ ] **Step 5: Add WeaponHolder and Muzzle using the actual right-hand transform.** Prefer Humanoid `Animator.GetBoneTransform(HumanBodyBones.RightHand)` after validating Animator/avatar; fallback exact prefab bone name is `Bip001 R Hand`. Verify hand pose in idle and shoot. Use existing suitable weapon mesh if present; otherwise author a dark low-poly proxy from cubes/cylinders with no colliders and mark it as a proxy in verification notes. Muzzle must be at barrel tip; obstruction origin stays inside player body's safe space at shoulder height, not another moving point already beyond cover.
- [ ] **Step 6: Wire visible muzzle/hit effects and optional audio.** Reuse suitable local effects after preview; otherwise create owned Shuriken systems: muzzle lifetime 0.05–0.08 s, non-looping burst, no collider; four pre-created impact systems with lifetime 0.15–0.25 s, world-space simulation, no automatic destruction. Assign ring to WeaponController; never instantiate effects per frame. Muzzle particle follows muzzle; impact systems are not parented to a moving hand. All effect renderers/weapon meshes use URP-compatible materials. If no matching local shot audio exists, record audio absent and keep the documented MVP audio task outstanding.
- [ ] **Step 7: Reuse current Cinemachine camera setup.** Follow upgraded Player root; preserve fixed camera yaw/downward angle. Validate visual framing after soldier scale; adjust damping only based on measured jitter/lag. A new test camera can reuse settings without changing the production camera until Task 6.
- [ ] **Step 8: Play idle, move, standing fire, move+fire, opposite-direction fire, release and rapid direction change.** Confirm shot effects correspond to accepted shots, misses still show muzzle, hit effects stay in world, legs continue locomotion during shooting, pause freezes animation and death/reset clears the upper-body reaction.
- [ ] **Step 9: Commit only owned player/animation/effect assets and script.** Message: `feat: add soldier animation and rifle feedback`.

**Acceptance:** Soldier and shots are visible and readable; root motion does not move the character; no source FBX or asset-pack importer change; missing directional footwork is documented, not claimed fixed.

### Task 5: Touch controls, pause/death/reset and test harness

**Files:** Create Tests/Runtime/PlayerCoreArenaHarness.cs; reuse the PlayMode test assembly/source from Task 3; modify PlayerCoreTest.unity. Harness remains in default Assembly-CSharp outside the test assemblies; it is never attached to GameplayZombie.

**Interfaces:** Consumes PlayerController.SetGameplayEnabled/ResetAt, PlayerHealth.TakeDamage/RestoreFullHealth, PlayerAnimationController.ResetPresentation. Produces UI handlers `TogglePause()`, `DamagePlayer()`, `ResetArena()`.

- [ ] **Step 1: Add the full arena harness.**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class PlayerCoreArenaHarness : MonoBehaviour
{
    [SerializeField] private PlayerController m_player;
    [SerializeField] private PlayerHealth m_health;
    [SerializeField] private PlayerHealth m_target;
    [SerializeField] private PlayerAnimationController m_animation;
    [SerializeField] private Text m_status;
    private Vector3 m_spawnPosition;
    private Quaternion m_spawnRotation;
    private bool m_paused;
    private float m_previousTimeScale = 1f;

    private void Start()
    {
        if (m_player == null || m_health == null || m_target == null)
        {
            Debug.LogError("PlayerCoreArenaHarness requires player, player health and target health.", this);
            enabled = false;
            return;
        }
        m_spawnPosition = m_player.transform.position;
        m_spawnRotation = m_player.transform.rotation;
    }

    public void TogglePause()
    {
        if (m_player == null) return;
        if (!m_paused)
        {
            m_previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            m_paused = true;
            m_player.SetGameplayEnabled(false);
        }
        else
        {
            RestoreTime();
            m_player.SetGameplayEnabled(true);
        }
    }

    public void DamagePlayer()
    {
        if (m_paused || m_health == null) return;
        m_health.TakeDamage(new DamageInfo(10f, m_player.transform.position, Vector3.zero, 0f, gameObject));
    }

    public void ResetArena()
    {
        if (m_player == null) return;
        RestoreTime();
        m_player.ResetAt(m_spawnPosition, m_spawnRotation);
        if (m_target != null) m_target.RestoreFullHealth();
        if (m_animation != null) m_animation.ResetPresentation();
    }

    private void Update()
    {
        if (m_status == null || m_health == null || m_target == null) return;
        string state = m_paused ? "PAUSED" : m_health.IsDead ? "DEAD - Reset to retry" : "PLAYING";
        m_status.text = $"{state}\nHP {m_health.CurrentHealth:0}/{m_health.MaxHealth:0} | Target {m_target.CurrentHealth:0}";
    }

    private void RestoreTime()
    {
        if (!m_paused) return;
        Time.timeScale = m_previousTimeScale;
        m_paused = false;
    }

    private void OnDisable() => RestoreTime();
}
```

- [ ] **Step 2: Build test UI under a safe-area root.** Canvas Overlay, scaler ScaleWithScreenSize/reference `(1920,1080)`/match `0.5`; reuse Crystal.SafeArea on full-stretch root. Existing EventSystem/InputSystemUIInputModule stays active while Player map is disabled. Put stick backgrounds at bottom-left/bottom-right, anchors `(0,0)` and `(1,0)`, offsets `(190,190)` and `(-190,190)`, size `240×240`; stick handles `110×110`, movementRange `85`, control paths `<Gamepad>/leftStick` and `<Gamepad>/rightStick`, RelativePositionWithStaticOrigin. Make non-interactive background images raycastTarget=false. Assign both OnScreenStick references to the player's router; use non-isolated mode as specified in Task 3. Add clearly labeled Pause/Resume, Damage10, Reset buttons above the stick zones and assign the harness handlers.
- [ ] **Step 3: Add the stationary target.** Use a clearly colored root at forward shooting distance with PlayerHealth (100 HP) and a child non-trigger BoxCollider on a weapon-hit layer. Its collider spans muzzle height. Assign target to harness. It stays present at 0 HP for reset; do not add zombie AI/dissolve.
- [ ] **Step 4: Re-run the existing PlayerCoreMovementTests against the integrated player.** Inspect actual results before the UI/touch lifecycle smoke tests below.

- [ ] **Step 5: Test UI pause/death/reset in Play Mode.** Pause while both sticks held, release Aim while paused, resume and verify no replayed fire. Damage10 ten times causes one death; held aim must not shoot. Reset while paused restores time scale, player position/HP, target HP and no duplicate Fired/Died callbacks. Exit Play Mode during pause and verify time scale restored. Test loss of focus while dragging each virtual stick.
- [ ] **Step 6: Verify simultaneous touch separately from desktop mouse.** Use connected touch device or Input System simulated Touchscreen events with two distinct touch IDs; moving/releasing one finger must not overwrite the other. If neither can be exercised in this environment, record this as unverified and never label mouse-only testing as two-touch pass.
- [ ] **Step 7: Run focused tests and commit task assets.** Message: `feat: add player core arena and lifecycle controls`.

**Acceptance:** Arena demonstrates the complete player loop with usable dual-stick UI, repeatable damage/death/reset and pause ownership; automated input/physics checks pass and touch coverage is reported accurately.

### Task 6: Integrate and record actual verification

**Files:** Modify GameplayZombie.unity; create docs/testing/2026-09-08-player-core-verification.md. All other files are changed only to fix a failure directly observed in the previous tasks.

- [ ] **Step 1: Update the existing scene instance from the proven Player prefab.** Preserve its root/camera reference. Add/configure right joystick using the arena setup, bind both sticks to router and verify no stale UnityEvent refers to removed OnMove. Keep harness/target test UI out of production scene. Do not add PlayerCoreTest to EditorBuildSettings.
- [ ] **Step 2: Run both PlayerCore test assemblies in the open Unity Test Runner.** Record counts, actual failures and execution mode. No second Editor/batchmode process against the same open project.
- [ ] **Step 3: Run manual smoke tests at all three required resolutions.** Check Canvas scale, safe area, stick hit zones, camera framing, soldier pose, muzzle/impact visibility and no missing scripts/materials. Capture Game view for evidence. A screenshot proves layout only, not touch/collision behavior.
- [ ] **Step 4: Run close-cover, uphill/downhill, pause during input, focus loss, death and reset checks.** Fix any failure at its owning component, add a regression when it represents logic rather than visual tuning, then rerun only the affected checks plus final scene smoke.
- [ ] **Step 5: Write verification evidence.** Record Unity version, tested scenes, test result counts, device/simulated touch coverage, the three resolutions, actual compile/console errors and any remaining limitations. Distinguish raw imported animation limits, proxy gun/audio, slope movement and cross-height shooting. Never pre-fill PASS before executing a check.
- [ ] **Step 6: Review diff and commit only player-core files.** Confirm user's InputManager and volume changes remain unstaged, generated metas are included and no Library/Temp/log files are tracked. Message: `feat: integrate verified player core into gameplay scene`. No remote push or merge is part of this plan.

**Acceptance:** GameplayZombie runs the approved controls with the real model, weapon and health components; evidence describes both verified behavior and remaining asset/device limits without claiming the full Zombie War MVP is finished.

## Execution checkpoints and review

- Task 1: types/tests compile, HP and facing rules are proven independently.
- Task 2: Rifle behavior is testable with actual collider geometry.
- Task 3: movement/aim works in isolation with the real controller.
- Task 4: character and shot feedback are visually readable.
- Task 5: full player loop including pause/death/reset and touch is exercised.
- Task 6: production scene integration and evidence are complete.

At each checkpoint review the scoped diff, API names and actual test output. The primary agent owns the plan's spec-coverage/type-consistency/placeholder self-review; subagents may inspect assets or implement independent approved tasks, not substitute for that self-review.

## Primary-agent self-review before execution

- [x] Map spec sections 2, 5–7 to Tasks 1 and 3; sections 8/11 to Tasks 1, 2 and 5; section 9 to Task 4; section 12 to Tasks 3, 5 and 6.
- [x] Verify exact API names in tests, full scripts, harness and component wiring match.
- [x] Read all code blocks for missing types/unassigned locals before extraction; runtime compilation is still required during execution. Explicitly initialized coverHit and aligned all private field names.
- [x] Code-bearing steps supply complete files; input/scene/Animator operations have explicit targets and values. The movement test is created before its implementation.
- [x] Task commits exclude existing user changes; paths follow Assets/ZombieWar and no dependency migration is required.

The approved spec remains the acceptance contract. Runtime findings may require a surgical implementation correction; record the finding and correction, preserve scope and re-run its proof.
