# Zombie War Scripts Core Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development with scoped source ownership and static review. User explicitly overrides test/build, Editor, Git mutation and prefab/presentation steps.

**Goal:** Implement scripts for twin-stick survival, optional/collected weapons and continuous waves over a 180-second run.

**Architecture:** Existing GamePlayController owns session state and pushes gates into PlayerCore and spawner. Stats are per-instance components backed by optional base config assets. Combat requests originate on the player; enemy spawn/lifecycle owns pooling and alive accounting.

**Tech Stack:** Existing Unity 6000.6.0f1, Input System, Transform.Translate player movement, NavMeshAgent enemies; no dependencies added. Translate supersedes the earlier CharacterController decision by explicit user confirmation on 2026-09-09.

**Spec:** ../specs/2026-09-08-zombie-war-scripts-core-design.md (approved to implement in latest conversation).

## Global Constraints

- Only scripts, script metadata and supporting implementation notes. No prefab/scene/InputAction/visual/animation edits, Editor MCP, tests/builds or Git mutations.
- The user approved compatibility-only changes in the two existing EditMode test source files; no new tests or test execution.
- Keep current dirty worktree and branch. Use apply_patch. Preserve existing script GUIDs on rename.
- Private fields m_camelCase; serialized private Inspector fields; English comments; no generic framework.
- 180 gameplay seconds; sequential quota waves without clear-wave waiting; last wave repeats; one spawn per interval with persistent alive cap.
- Speed 5 fallback; aim magnitude > 0.25; world-space Translate movement without built-in collision/gravity; three hitscan firearms, optional starting weapon and pickup/switch.
- Static review and reference searches only. Never claim compiler, runtime, test or performance proof.

## Shared source contracts

```csharp
// Player-owned runtime health contract, implemented in Task 1.
public float CurrentHealth { get; }
public float MaxHealth { get; }
public float MoveSpeed { get; }
public bool IsAlive { get; }
public bool IsDead { get; }
public event System.Action<float, float> HealthChanged;
public event System.Action Died;
public void RestoreFullHealth();
public void TakeDamage(DamageInfo damage);

// PlayerController contract used by session and pickup.
public PlayerStats Stats { get; }
public bool CanAct { get; }
public void SetGameplayEnabled(bool value);
public void ResetForRun(Vector3 position, Quaternion rotation);
public bool TryCollectWeapon(WeaponScriptableObject data);
public void SwitchWeapon();

// WeaponController contract used by player.
public WeaponScriptableObject Data { get; }
public bool Initialize(WeaponScriptableObject data, Transform origin, PlayerStats owner, int hitMask);
public void Configure(Transform muzzle, Transform origin, PlayerStats owner, int hitMask);
public bool TryFire(Vector3 direction, float now);
public void SetGameplayEnabled(bool value);
public void ResetWeapon();
public event System.Action Fired;
public event System.Action<UnityEngine.RaycastHit> Hit;

// Enemy subsystem contract used by session.
public bool ZombieSpawner.BeginRun(Transform target, PlayerStats targetStats);
public void ZombieSpawner.SetGameplayEnabled(bool value);
public void ZombieSpawner.EndRun();
public int ZombieSpawner.AliveCount { get; }
public event System.Action<ZombieController> ZombieSpawner.ZombieKilled;
```

The code above is a member-contract list, not an additional class/file to generate.

### Task 1: Player stats and controls (root)

**Files:** Player/PlayerHealth.cs -> PlayerStats.cs (+ same meta); CharacterScriptableObject.cs; PlayerController.cs; PlayerCollector.cs; fold/remove PlayerControlRules.cs/meta after migrating consumers.

- [x] Rename health component with GUID preservation; retain finite damage guards, null-safe events and once-per-life death. Optional CharacterScriptableObject supplies max health/move speed; local defaults keep old configuration usable.
- [x] Implement world-space Translate movement (user override 2026-09-09), move/aim callbacks, merged rules, clear inputs on gate/focus/disable, no state singleton reference from PlayerCore. Remove frame-by-frame debug logging and stale CharacterController checks; do not edit the user's prefab/input changes.
- [x] Implement one runtime weapon instance per WeaponKind, optional starting weapon, configured socket/origin and hitMask. Equip calls Initialize only once per owned instance, preserves cooldown on switch, resets loadout on new run.
- [x] Collector calls WeaponPickup.TryCollect with the owning player; no pickup on dead/paused player.
- [x] Static review references and source event lifecycle; update only two existing tests for API compatibility after user approval.

### Task 2: Combat scripts (combat implementer)

**Files owned:** Combat/WeaponScriptableObject.cs, WeaponController.cs, PistolController.cs, RifleController.cs, ShotgunController.cs; new WeaponPickup.cs and meta. Do not edit DamageInfo/IDamageable, player files, tests or docs.

- [x] Data derives ScriptableObject. WeaponKind enum Pistol/Rifle/Shotgun; WeaponPrefab, Kind, FireInterval, Damage, Range, PelletCount, SpreadAngle, KnockbackForce. Preserve m_fireRate value via FormerlySerializedAs on m_fireInterval; default .5 seconds, damage10, range100, pellets6, spread20 degrees. Clamp/validate finite configs once, not in every inner ray loop.
- [x] Weapon owns serialized muzzle; Initialize(data,origin,owner,mask) binds/configures it and validates once. Configure retained for existing API users; no autonomous Update firing. Missing muzzle/data/config logs once and refuses a shot.
- [x] TryFire(direction,now) gates enabled/active/alive, finite direction/time and cooldown; flatten XZ, fire immediately when ready, nextShot = now + interval, no catchup loop. Firing uses data; not spread in second argument. Release/switch cannot reset timer.
- [x] Hitscan excludes owner's colliders, ignores triggers, respects nearest solid cover including origin-to-muzzle obstruction and parent IDamageable; apply damage once per ray. Shotgun spreads data.PelletCount rays symmetrically across SpreadAngle, each with per-pellet damage and knockback. Pistol/Rifle one ray. Fire event once per shot.
- [x] Pickup holds WeaponScriptableObject, exposes bool TryCollect(PlayerController). Gate player.CanAct, call TryCollectWeapon(data), disable pickup only on successful collection; no renderer/visual work.
- [x] Preserve existing class/file/meta identities for gun subclasses. Static self-review; report APIs/files and unresolved concerns, no test/build/commit.

### Task 3: Enemy stats, chase, spawn and pool lifecycle (enemy implementer)

**Files owned:** Enemies/ZombieHealth.cs -> ZombieStats.cs/meta, ZombieScriptableObject.cs, ZombieController.cs, ZombieSpawner.cs; Utilities/ObjectPooling.cs (minimal lifecycle extension only). No Core/Combat/Player/test/docs edits.

- [x] Stats runtime component implements IDamageable with health100, move2, damage10, attackInterval1, attackRange1.5 fallback; config optional and read-only, null-safe death once/reset. SO exposes base values; no shared current HP.
- [x] NavMesh chase gets target and targetStats from spawn, cache components in Awake. Repath bounded interval .25 seconds, verify enabled/isOnNavMesh before agent APIs; attack only within range, target alive, gate open and cooldown ready. Cover blocks melee; horizontal direction, no transform.Translate relative to target. Agent stopped on pause/death; reset cooldown/path/state each spawn.
- [x] Add an inactive-acquire pool path, retaining public Spawn/Despawn consumers. Set pose/initialize zombie before activation; no prefab asset mutation. Cache/reset before OnEnable participates in gameplay.
- [x] Spawner uses shared contracts above. Wave list owns group quotas and interval; runtime counters separate. Validate once at BeginRun. One zombie per interval, group order, advance on successfully spawned quota, repeat last wave. No inter-wave coroutine and no per-frame completion log.
- [x] Alive cap spans waves (default40), successful spawn only increments; death/despawn once removes count and returns pool. Subscribe/unsubscribe correctly on EndRun/restart; no Instantiate in normal spawn loop except pool growth. Clear input target/state on release.
- [x] Authored points with safety radius5, bounded scan, NavMesh sample radius2 and check resulting point safety; failure retries next cadence without burning quota. Preserve field names with FormerlySerializedAs where renaming user's Inspector fields.
- [x] EndRun stops scheduling and safely despawns owned live zombies. SetGameplayEnabled freezes/resumes scheduler and active zombie gates without resetting wave counters.
- [x] Static self-review; source boundaries Core/Enemies/Utilities are Assembly-CSharp, can depend on PlayerCore. Remove runtime NUnit import. No tests/build/Editor/commits.

### Task 4: Session integration (core implementer)

**File owned:** Core/GamePlayController.cs only. Existing MainStateManager owns ticking via OnUpdate, so no second Update.

- [x] Preserve current public entry points and enums used by UI/MainStateManager; replace stale level setup behavior with actual player/spawner run initialization. Serialize PlayerController, ZombieSpawner, optional spawn transform; capture initial pose once if spawn omitted. Local scene references wired later by user.
- [x] Begin run resets health/input/loadout/spawner and 180-second remaining timer; start only if required references/configuration ready. GamePlayController pushes gates; never add reverse singleton calls from PlayerCore.
- [x] Subscribe player.Stats.Died and spawner.ZombieKilled exactly once; clean up OnDisable/OnDestroy calling base.OnDestroy. No repeated persistent subscription on restart. Record kills in RunResult.score to preserve existing result consumer shape; retain legacy fields/API without letting song data control gameplay.
- [x] Playing only decrements remaining time, alive at expiration wins; death triggers loss. Terminal result guarded once; close gates immediately before popup/delay. Pause/settings close all gates and freeze scaled time while remembering prior timeScale; resume restores it, no delay catchup. Restart/quit/disable also restore timeScale.
- [x] Preserve public IEnumerator PlayerLose(float waitTime), ForceWin/ForceLose, SetGameState, OnPause/OnResume, StartLevel/RestartLevel/QuitLevel, OnGetUIIngame and RunResult APIs. Cancel stale delayed callbacks at reset/quit. Null UI/MainStateManager should not cause NRE when script is used as local gameplay-only scene.
- [x] Static self-review callpaths against shared signatures and existing UI. No other files, tests/build, Editor or commits.

### Task 5: Integration review and handoff (root + independent reviewer)

- [x] Verify rename GUIDs, duplicate class names, old-type references, dependency direction, source contracts and event lifecycle by inspection/search.
- [x] Remove empty ZombieWarGameController placeholder only after checking source and serialized references; preserve referenced placeholder if Editor migration is needed and report it.
- [x] Compile/test/PlayMode/build are explicitly not run. Record source review limitations and exact Inspector callbacks/components/config fields needed for later user integration.
- [x] Independent scoped static review, address important findings, report changed full script links and remaining limitations; no commit.

## Execution ledger

- Preflight: Tasks1/2 share PlayerStats and Weapon interfaces listed above; Task1/4 share Stats/gate/reset; Tasks3/4 share BeginRun/gate/end/death events. Paths owned separately; no conflicting writers.
- Task1: source implemented; PlayerHealth/meta renamed to PlayerStats preserving GUID 3688f733403a4cfa9be92ba7ed758d22; rules merged, loadout/collector implemented. User chose Translate on 2026-09-09 after temporarily bypassing movement guards; Translate retained and session/death guards restored. Independent static review completed; no substantive findings remain after fixes.
- Task2: source implemented; nearest-hit non-alloc query with overflow fallback, point-blank obstruction, disabled-owner and missing-mask guards in place. Independent review found pickups stayed consumed after reset; PlayerController.RunReset now restores consumed pickups without a scene scan.
- Task3: stats/controller/spawner/pool implemented; per-wave interval is the single source, disabled/missing references end scheduling, successful NavMesh activation required before quota advances, and melee ignores owner/target colliders while cover blocks attacks.
- Task4: existing GamePlayController integrated with player/spawner, 180-second timer and pause/settings locks. CharacterController validation removed to match the user's Translate decision. Independent static review completed; transition races and subsystem-loss terminal handling corrected.
- Task5: old placeholder/rules source and meta removed after zero source/serialized consumers found; both old test sources migrated for compatibility, not executed. Scoped independent reviews completed; important lifecycle findings addressed. Source/reference/GUID and changed-line whitespace checks completed without compiler or runtime execution.
- Scope record: GameplayZombie.unity acquired a diff during work; no agent edits to this scene are authorized or performed, so preserve it untouched.
- Resume 2026-09-09: user also changed Player.prefab, InputManager.inputactions and added NormalZombie/NavMesh scene assets. Preserve these without editing or claiming agent authorship.
- User override: no TDD/test/build/Editor/Git writes. Compatibility-only edits in the two old test files approved explicitly.
- Parallel independent source tasks are used per active developer orchestration instructions; coordination remains in this current worktree to preserve user edits.

## Handoff: wiring do user thực hiện sau trong Editor

Không có thao tác Editor hoặc thay asset nào được thực hiện trong lượt viết scripts. Các mục dưới đây là yêu cầu wiring, không phải trạng thái scene đã xác nhận.

| Phần | Components / fields cần nối |
| --- | --- |
| Session | `GamePlayController`: `m_playerController`, `m_zombieSpawner`, tùy chọn `m_playerSpawnPoint`. Giữ `MainStateManager` gọi `OnUpdate()` một lần mỗi frame; không thêm timer driver thứ hai. Bắt đầu bằng `StartLevel()` qua flow hiện có. |
| Player | Root có `PlayerController` + `PlayerStats` enabled. `m_characterData` tùy chọn; mặc định health100, speed5. `m_facingRoot` tùy chọn, để trống quay root. Không cần CharacterController. |
| Input | Move/Aim là Vector2: nối started/performed/canceled vào `OnMove` / `OnAim`. Switch là Button press-only, nối `OnSwitchWeapon`. Callback dùng `InputAction.CallbackContext` (Invoke Unity Events), không phải Send Messages/InputValue. |
| Weapon | Trên player: `m_startingWeapon` được để trống, `m_weaponSocket`, `m_obstructionOrigin`, `m_hitMask` gồm hitbox zombie và solid cover. Weapon prefab root có `WeaponController` hoặc subclass enabled, gán `m_muzzle`; socket phải theo facing root. SO gán đúng `Kind`, `WeaponPrefab`, interval/damage/range; Shotgun dùng thêm pellets/spread. |
| Pickup | `WeaponPickup` + `m_weaponData`; player có `PlayerCollector` và owner. Author trigger/collider/Rigidbody phù hợp để Unity gửi OnTriggerEnter. Collection qua `TryCollect` sẽ ẩn pickup; `RunReset` hiện lại nó khi bắt đầu run mới. Không destroy pickup sau khi nhặt nếu muốn reset cùng scene. |
| Zombie | Prefab root có `ZombieController` + `ZombieStats` + `NavMeshAgent` enabled; solid hitbox để hitscan trúng. `m_zombieData` tùy chọn (Normal/Giant khác stats). Gán `m_coverMask` gồm cover thật, không để None; query bỏ qua collider của chính nó và player. |
| Spawn / pool | Có `ObjectPooling` active. Spawner gán `m_waves` / groups / quota / interval và `m_spawnPoints`; `m_spawnedZombieParent` nếu gán phải active. NavMesh đã bake với agent type của prefab và điểm spawn hợp lệ. Mặc định cap40, safety radius5, sample radius2, tối đa8 điểm được thử mỗi nhịp. |

### Luật runtime đã viết

- `GamePlayController` sở hữu 180 gameplay seconds và result duy nhất; chỉ Playing tick timer. Kills lưu vào `RunResult.score` để giữ UI API cũ; các tên `songName` / `SelectedSongIndex` chỉ còn là compatibility với consumer hiện tại.
- Wave tiến khi đã spawn đủ quota, không chờ diệt sạch; hết danh sách thì lặp wave cuối. Mỗi interval tối đa một spawn; pause hoặc chạm alive cap không tạo burst bù.
- Input bị khóa cho đến khi session khởi tạo thành công. Không bypass `CanAct` để debug movement trong scene chưa nối session; có thể gọi API gate chủ động từ driver riêng, nhưng chỉ dùng một session driver.
- Player dùng Translate theo lựa chọn mới nhất: clamp input chéo, world XZ, không có gravity/collision sweep. Movement không tự dừng trước tường chỉ vì gắn collider.
- Knockback zombie nhận từ damage payload: force0 tắt đẩy; force dương refresh vận tốc, deceleration mặc định20. Không chồng lực theo số pellet; pause đóng băng, pool reset xóa vận tốc.
- Mất player/spawner hoặc lỗi activate zombie giữa run đi vào lose/result để vẫn có đường restart/quit. Lỗi cấu hình ngay lúc bắt đầu được báo Console và không mở gameplay gates.

### Migration và giới hạn

- `PlayerHealth` → `PlayerStats`, `ZombieHealth` → `ZombieStats`: giữ GUID cũ. Speed player chuyển sang PlayerStats/data; giá trị Inspector cũ trên PlayerController không tự chuyển sang component khác.
- `WeaponScriptableObject` và `CharacterScriptableObject` là data assets thật. Component instance cũ không tự chuyển thành ScriptableObject asset; cần tạo/gán data bằng Create menu. `m_fireRate` giữ giá trị qua FormerlySerializedAs, đơn vị vẫn là giây giữa phát bắn.
- `PlayerControlRules` và placeholder rỗng `ZombieWarGameController` cùng meta đã được xóa sau khi kiểm tra không còn source/serialized consumer; logic tương ứng ở PlayerController / GamePlayController. Các file tracked đã xóa vẫn có thể phục hồi từ Git; không có commit trong lượt này.
- Chỉ hai test C# cũ được chỉnh compatibility; số Test/TestCase giữ nguyên (3 và 4). Không chạy chúng, không tạo test mới.
- Không có compiler, Play Mode, build, benchmark hoặc scene/prefab validation. Các phép kiểm tra source/reference/GUID/whitespace không chứng minh runtime, collision, NavMesh, input hoặc FPS/GC.
- Source giảm công việc thừa bằng cache components/stats, bounded repath và spawn scan, event-based alive count, raycast buffer và bỏ log mỗi frame. Weapon raycast có allocation fallback khi buffer16 đầy để giữ nearest-cover correctness; melee buffer đầy chặn attack an toàn. Cần đo trong Editor sau nếu muốn kết luận performance.
- Final static evidence (2026-09-09): old type references in C# = 0; removed rules/placeholder serialized references = 0; 54 public class declarations and 57 script GUIDs inspected with no duplicates; renamed stats GUIDs preserved; existing test attribute counts unchanged (3/4); git diff --check clean. No build/test/Editor validation claimed.
