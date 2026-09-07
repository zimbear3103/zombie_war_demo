# Zombie War MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a polished, playable 3D top-down Zombie War Android prototype in six working days, with the mandatory controls, weapons, zombie AI, physics bomb, two levels, shader effects, UI and submission package.

**Architecture:** Keep the existing rhythm-game scripts isolated and build the new game under `Assets/Scripts/ZombieWar`. A small session/state layer passes the selected `LevelManager` from the menu to one reusable `Gameplay.unity` scene. Combat uses shared damage contracts, ScriptableObject weapon/level data, hitscan firearms, pooled effects and NavMeshAgent-driven enemies.

**Tech Stack:** Unity `6.0.6f1`, URP `17.6`, Input System `1.20.0`, Cinemachine `6.6.0`, AI Navigation `2.0.14`, UGUI, TextMesh Pro, URP Shader Graph, Unity Test Framework.

**Spec:** `docs/superpowers/specs/2026-09-07-zombie-war-context-design.md`

## Global Constraints

- Use the existing Unity 6 project and URP pipeline; do not downgrade to Unity 2022 or convert to Built-in RP.
- Target Android landscape; reference UI resolution is `1920 x 1080` with Canvas Scaler `Scale With Screen Size` and Match approximately `0.5`.
- Preserve `SampleScene` as the current prototype until `Gameplay.unity` is proven playable.
- Do not put new Zombie War code inside rhythm-specific song, beat-map or conductor behavior.
- Use the selected asset stack: Toon Soldier, Shirtless Zombie URP, Military environment, Post Apocalypse guns and WarFX Mobile.
- Private fields use `m_camelCase`; public fields use `camelCase`; Inspector fields use `[SerializeField] private`.
- Comments are in English.
- The three mandatory weapons are Pistol, Assault Rifle and Shotgun. Rocket Gun is a Day 6 stretch goal only.
- A level lasts 180 seconds. Reaching zero seconds wins; player HP reaching zero loses.
- Only Normal and Giant zombies are required.
- Bomb inventory is finite, with up to three pickups authored per level; no bomb cooldown is required.
- The right aim joystick beyond a dead-zone automatically fires continuously in its current direction.
- Verification must include Unity Editor keyboard/gamepad fallback, both on-screen sticks, and 16:9, 20:9 and 4:3 UI smoke tests.

## File Map

The implementation uses focused files with one responsibility:

- `Assets/Scripts/ZombieWar/Core/`: session state, level data, bootstrap and timer.
- `Assets/Scripts/ZombieWar/Input/`: keyboard/gamepad fallback and on-screen stick routing.
- `Assets/Scripts/ZombieWar/Player/`: movement, aim rotation, health and animation parameters.
- `Assets/Scripts/ZombieWar/Combat/`: damage contracts, weapons, projectiles/effects and bombs.
- `Assets/Scripts/ZombieWar/Enemies/`: NavMesh zombie behavior, health, spawn director and dissolve effect.
- `Assets/Scripts/ZombieWar/UI/`: main menu, HUD and result screens.
- `Assets/Prefabs/ZombieWar/`: player, enemies, pickups, effects and map prefabs.
- `Assets/Scenes/MainMenu.unity` and `Assets/Scenes/Gameplay.unity`: final scene flow.
- `Assets/Tests/EditMode/ZombieWar/`: pure combat and wave-rule tests.

The old `Assets/Scripts/GamePlayController.cs`, `GameplayController.cs`, `UIHome.cs` and `UIInGame.cs` remain untouched until the new flow is independently playable. They may be removed from the final scene references after the new flow is verified.

---

### Task 1: Create the Zombie War scene shell, level data and input contract

**Day:** 1, first 2-3 hours  
**Files:**
- Create: `Assets/Scripts/ZombieWar/Core/GameplayController.cs`
- Create: `Assets/Scripts/ZombieWar/Core/LevelManager.cs`
- Create: `Assets/Scripts/ZombieWar/Core/GameSessionManager.cs`
- Create: `Assets/Scripts/ZombieWar/Core/GameplayBootstrap.cs`
- Modify: `Assets/Inputs/InputManager.inputactions`
- Create: `Assets/Scenes/MainMenu.unity`
- Create: `Assets/Scenes/Gameplay.unity`
- Modify: `ProjectSettings/EditorBuildSettings.asset`
- Test: `Assets/Tests/EditMode/ZombieWar/ZombieWarSessionTests.cs`

**Interfaces:**

- `GameplayController` exposes `Booting`, `Menu`, `Loading`, `Playing`, `Paused`, `Won` and `Lost`.
- `LevelManager` is a `ScriptableObject` with `int levelId`, `string displayName`, `GameObject mapPrefab`, `float durationSeconds`, `bool allowGiantZombies`, `float giantStartTime`, `int maxBombPickups`, `int baseMaxAlive` and `AnimationCurve spawnIntervalOverNormalizedTime`.
- `GameSessionManager` exposes `int SelectedLevelId`, `LevelManager ActiveLevel`, `GameplayController State`, `event Action<GameplayController> StateChanged`, `void SelectLevel(LevelManager level)`, `void StartSelectedLevel()`, `void SetWin()`, `void SetLose()`, `void RestartLevel()` and `void ReturnToMenu()`.
- `GameplayBootstrap` exposes `void LoadLevel(LevelManager level)` and raises `event Action LevelReady` after the selected map, player and spawn director references are initialized.
- `InputManager.inputactions` must contain `Move` and `Aim` `Value/Vector2` actions plus `Bomb` and `SwitchWeapon` `Button` actions. Keyboard fallback uses WASD for Move, arrow keys for Aim, Space for Bomb and Q for SwitchWeapon. The on-screen sticks will write to the same runtime input router in Task 2.

- [ ] **Step 1: Add the failing session state tests.**

  Create tests for `SelectLevel`, `StartSelectedLevel`, `SetWin` and `SetLose`. Assert that selecting Level 2 stores its id, starting without a selected level does not enter `Playing`, and both terminal states stop accepting a second terminal transition.

- [ ] **Step 2: Run the focused EditMode tests and verify they fail.**

  Run Unity Test Runner in EditMode for `ZombieWarSessionTests`. Expected result: the test assembly cannot find the new session types or the state transitions are not implemented.

- [ ] **Step 3: Implement the minimal state and data types.**

  Add the enum, ScriptableObject and session methods with explicit state transitions. Keep the session independent of songs, beat maps and `Conductor`. Use `Action` events only for UI and scene flow notifications.

- [ ] **Step 4: Re-run the focused tests.**

  Run `ZombieWarSessionTests` again. Expected result: all state tests pass.

- [ ] **Step 5: Create the two scenes and scene list entries.**

  Create `MainMenu.unity` with an EventSystem and an empty session/scene-flow root. Create `Gameplay.unity` with an empty `GameplayBootstrap`, a directional light, a URP-compatible camera and a Cinemachine camera object. Add both scenes to Build Settings in the order `MainMenu`, `Gameplay`; keep `SampleScene` available below them for prototype fallback.

- [ ] **Step 6: Extend the input asset and verify bindings.**

  Add the four named actions and keyboard bindings to `InputManager.inputactions`, regenerate the input wrapper if the project uses one, and verify in the Input Debugger that Move and Aim produce independent Vector2 values.

- [ ] **Step 7: Commit the shell.**

  Commit the scene shell, input asset and passing tests with `feat: add zombie war gameplay shell`.

**Acceptance:** The project opens `MainMenu.unity`, both scenes are in Build Settings, the new session can select a level and the focused EditMode tests pass without referencing the legacy rhythm flow.

### Task 2: Implement the 3D player, twin-stick input and Cinemachine follow

**Day:** 1, remaining 7-9 hours  
**Files:**
- Create: `Assets/Scripts/ZombieWar/Input/ZombieWarInputRouter.cs`
- Create: `Assets/Scripts/ZombieWar/Input/VirtualJoystickInput.cs`
- Create: `Assets/Scripts/ZombieWar/Player/ZombieWarPlayerController.cs`
- Create: `Assets/Scripts/ZombieWar/Player/PlayerHealth.cs`
- Create: `Assets/Scripts/ZombieWar/Player/PlayerAnimatorDriver.cs`
- Create: `Assets/Prefabs/ZombieWar/ZombieWarPlayer.prefab`
- Modify: `Assets/Scenes/Gameplay.unity`
- Modify: `Assets/Prefabs/Ingame/Player.prefab` only if its model or camera references are reused

**Interfaces:**

- `ZombieWarInputRouter` exposes `Vector2 MoveInput`, `Vector2 AimInput`, `bool BombPressedThisFrame`, `bool SwitchWeaponPressedThisFrame`, `void SetVirtualMove(Vector2 value)`, `void SetVirtualAim(Vector2 value)`, `void PressBomb()`, `void PressSwitchWeapon()` and `void ClearOneShotInputs()`.
- `VirtualJoystickInput` exposes `void OnStickValueChanged(Vector2 value)` and `void OnStickReleased()` and forwards values to a configured `ZombieWarInputRouter`.
- `ZombieWarPlayerController` exposes `Vector3 AimDirection`, `bool IsAiming`, `void SetMoveInput(Vector2 value)`, `void SetAimInput(Vector2 value)` and `event Action<Vector3> AimDirectionChanged`.
- `PlayerHealth` exposes `float CurrentHealth`, `float MaxHealth`, `bool IsDead`, `void TakeDamage(DamageInfo damageInfo)`, `void RestoreFullHealth()` and `event Action<float> HealthChanged`.
- `PlayerAnimatorDriver` consumes movement magnitude and `IsAiming`, then sets Animator parameters `MoveSpeed`, `AimX`, `AimY` and `IsFiring`.

- [ ] **Step 1: Add a failing movement/aim acceptance test.**

  Add a PlayMode test or a manual test checklist that instantiates `ZombieWarPlayerController`, sends `SetMoveInput(new Vector2(0, 1))`, verifies movement occurs on positive world Z, sends `SetAimInput(new Vector2(1, 0))`, and verifies the visual rotates toward positive world X while movement continues.

- [ ] **Step 2: Implement the input router without UI dependencies.**

  Read the keyboard/gamepad actions from `InputManager.inputactions` in `Update`, preserve the latest virtual stick values, and expose one-frame Bomb and SwitchWeapon flags. Do not let the UI own weapon or gameplay state.

- [ ] **Step 3: Add the virtual joystick adapter.**

  Reuse Unity Input System `OnScreenStick` for the left stick with control path `<Gamepad>/leftStick`, duplicate it for the right stick with `<Gamepad>/rightStick`, and connect both to the router. Confirm that the two controls do not write to the same action.

- [ ] **Step 4: Implement movement and aim rotation.**

  Move on XZ using `CharacterController` or a kinematic Rigidbody, clamp input magnitude, use a configurable move speed, calculate aim direction from the right stick only when its magnitude is at least `0.25`, and rotate the visual root with `Quaternion.LookRotation` around Y.

- [ ] **Step 5: Add the player prefab and camera follow.**

  Use the selected Toon Soldier URP-compatible model, add the controller, health, animator driver, input router reference and a muzzle anchor. Set the Cinemachine camera Follow target to the player root, use a fixed top-down downward angle and tune damping so the camera does not visibly jitter during movement.

- [ ] **Step 6: Add simple animation parameters.**

  Create an Animator Controller with a 1D Idle/Run blend based on `MoveSpeed`, an upper-body shooting layer driven by `IsFiring`, and no directional foot animation. Set the soldier visual to face the aim direction independently of the locomotion blend.

- [ ] **Step 7: Verify in Editor and on-screen controls.**

  Play `Gameplay.unity`; verify WASD moves, arrow keys aim, the left on-screen stick moves, the right on-screen stick rotates the player and stops aiming below the dead-zone. Confirm the camera follows through the full Level 1 map bounds.

- [ ] **Step 8: Commit the player slice.**

  Commit with `feat: add zombie war twin stick player`.

**Acceptance:** The player moves and aims independently in 3D, the camera follows, the two virtual sticks are independent, and Idle/Run plus upper-body animation respond to the correct parameters.

### Task 3: Implement damage contracts, three guns and finite bombs

**Day:** 2, 10-12 hours  
**Files:**
- Create: `Assets/Scripts/ZombieWar/Combat/DamageInfo.cs`
- Create: `Assets/Scripts/ZombieWar/Combat/IDamageable.cs`
- Create: `Assets/Scripts/ZombieWar/Combat/WeaponId.cs`
- Create: `Assets/Scripts/ZombieWar/Combat/WeaponDefinition.cs`
- Create: `Assets/Scripts/ZombieWar/Combat/WeaponController.cs`
- Create: `Assets/Scripts/ZombieWar/Combat/CombatEffectSpawner.cs`
- Create: `Assets/Scripts/ZombieWar/Combat/BombController.cs`
- Create: `Assets/Scripts/ZombieWar/Combat/BombPickup.cs`
- Create: `Assets/Scripts/ZombieWar/Combat/ExplosionDamage.cs`
- Create: `Assets/Tests/EditMode/ZombieWar/CombatRulesTests.cs`
- Create: `Assets/ScriptableObjects/ZombieWar/Weapons/Pistol.asset`
- Create: `Assets/ScriptableObjects/ZombieWar/Weapons/AssaultRifle.asset`
- Create: `Assets/ScriptableObjects/ZombieWar/Weapons/Shotgun.asset`
- Modify: `Assets/Prefabs/ZombieWar/ZombieWarPlayer.prefab`

**Interfaces:**

- `DamageInfo` is a readonly data struct containing `float amount`, `Vector3 hitPoint`, `Vector3 hitDirection`, `float knockbackForce` and `GameObject source`.
- `IDamageable` exposes `bool IsAlive { get; }` and `void TakeDamage(DamageInfo damageInfo)`.
- `WeaponId` contains `Pistol`, `AssaultRifle` and `Shotgun`.
- `WeaponDefinition` contains `WeaponId weaponId`, `float damage`, `float shotsPerSecond`, `float range`, `int pellets`, `float spreadAngle`, `float knockbackForce`, `LayerMask hitMask`, `GameObject muzzleEffectPrefab`, `GameObject impactEffectPrefab` and `AudioClip fireClip`.
- `WeaponController` exposes `WeaponId ActiveWeapon`, `void SetWeapon(WeaponId weaponId)`, `void CycleWeapon()`, `bool TryFire(Vector3 aimDirection, float now)`, `event Action<WeaponId> WeaponChanged` and `event Action Fired`.
- `BombController` exposes `int BombCount`, `int MaxBombCount`, `void AddBomb(int amount)`, `bool TryUseBomb(Vector3 worldPosition)` and `event Action<int> BombCountChanged`.
- `BombPickup` calls `BombController.AddBomb(1)` once and disables itself after collection.
- `ExplosionDamage` exposes `void Apply(Vector3 center, float radius, float damage, float force, LayerMask targetMask)` and uses `Physics.OverlapSphere` plus `Rigidbody.AddExplosionForce`.

- [ ] **Step 1: Add failing combat rule tests.**

  Test that a Pistol has one pellet, a Shotgun has more than one pellet, `WeaponController` rejects firing before the fire interval, BombCount cannot exceed three and `TryUseBomb` fails when BombCount is zero.

- [ ] **Step 2: Run the focused tests and verify they fail.**

  Run `CombatRulesTests` in EditMode. Expected result: the combat contracts and weapon data are not implemented yet.

- [ ] **Step 3: Implement the shared damage types and weapon data.**

  Create the data-only types and three ScriptableObject assets. Use intentionally visible prototype values: Pistol damage `20`, Assault Rifle damage `8` at `8` shots per second, Shotgun damage `12` per pellet with `6` pellets at `1` shot per second. Tune later only if the 180-second loop is unbalanced.

- [ ] **Step 4: Implement hitscan firing.**

  Fire from the muzzle anchor along the aim vector. Use one ray for Pistol/Rifle and multiple deterministic spread rays for Shotgun. Call `IDamageable.TakeDamage` on the first valid hit, spawn a mobile muzzle/impact effect through `CombatEffectSpawner`, play the weapon clip and raise `Fired` for animation.

- [ ] **Step 5: Connect right-stick auto-fire and weapon switching.**

  In the player combat host, call `TryFire` every frame while `IsAiming` is true. Consume `SwitchWeaponPressedThisFrame` to cycle Pistol -> Rifle -> Shotgun -> Pistol. Do not require a separate Fire button.

- [ ] **Step 6: Implement bombs and pickups.**

  Start each level with zero bombs. Place three pickup references in each map. `TryUseBomb` decrements the inventory once, spawns a mobile explosion effect and calls `ExplosionDamage.Apply`. Use a short same-frame input lock so one press cannot consume two bombs.

- [ ] **Step 7: Re-run combat tests.**

  Run `CombatRulesTests`; expected result is PASS. Then play in a temporary test arena and verify distinct Pistol/Rifle/Shotgun cadence, Shotgun spread, weapon switching, bomb count updates and pickup consumption.

- [ ] **Step 8: Commit the combat slice.**

  Commit with `feat: add zombie war weapons and bombs`.

**Acceptance:** The player fires continuously only while the right stick is held, all three mandatory guns visibly behave differently, switch input works, bombs are finite, and explosions can later apply both damage and knockback through the shared contract.

### Task 4: Implement Normal/Giant zombies, NavMesh chase, waves and dissolve shader

**Day:** 3, 10-12 hours  
**Files:**
- Create: `Assets/Scripts/ZombieWar/Enemies/ZombieType.cs`
- Create: `Assets/Scripts/ZombieWar/Enemies/ZombieController.cs`
- Create: `Assets/Scripts/ZombieWar/Enemies/ZombieHealth.cs`
- Create: `Assets/Scripts/ZombieWar/Enemies/ZombieAnimatorDriver.cs`
- Create: `Assets/Scripts/ZombieWar/Enemies/ZombieSpawnDirector.cs`
- Create: `Assets/Scripts/ZombieWar/Enemies/ZombieSpawnPoint.cs`
- Create: `Assets/Scripts/ZombieWar/Enemies/ZombieDissolveController.cs`
- Create: `Assets/Tests/EditMode/ZombieWar/SpawnDirectorRulesTests.cs`
- Create: `Assets/Prefabs/ZombieWar/NormalZombie.prefab`
- Create: `Assets/Prefabs/ZombieWar/GiantZombie.prefab`
- Create: `Assets/Shaders/ZombieDissolve.shadergraph`
- Create: `Assets/Shaders/ZombieDissolve.mat`
- Modify: `Assets/Scripts/ZombieWar/Combat/DamageInfo.cs` only if enemy-specific damage data is required

**Interfaces:**

- `ZombieType` contains `Normal` and `Giant`.
- `ZombieController` exposes `ZombieType Type`, `void Initialize(Transform target, ZombieDefinition definition)`, `void StopChasing()` and `void ApplyKnockback(Vector3 direction, float force)`.
- `ZombieHealth` implements `IDamageable`, exposes `float CurrentHealth`, `float MaxHealth`, `bool IsAlive`, `void TakeDamage(DamageInfo damageInfo)` and `event Action<ZombieHealth> Died`.
- `ZombieSpawnDirector` exposes `int AliveCount`, `void Initialize(LevelManager level, Transform target, IReadOnlyList<ZombieSpawnPoint> spawnPoints)`, `void BeginSpawning()`, `void StopSpawning()` and `event Action<ZombieController> ZombieSpawned`.
- `ZombieSpawnPoint` exposes `bool IsSafeFrom(Transform player, float safetyRadius)` and `Vector3 GetSpawnPosition()`.
- `ZombieDissolveController` exposes `void PlayHitFlash()` and `IEnumerator PlayDeathDissolve()`.

- [ ] **Step 1: Add failing spawn-rule tests.**

  Test that the spawn interval decreases as normalized level time increases, that Level 1 never requests a Giant zombie, that Level 2 can request a Giant after `giantStartTime`, and that a spawn point inside the safety radius is rejected.

- [ ] **Step 2: Run the focused tests and verify they fail.**

  Run `SpawnDirectorRulesTests` in EditMode. Expected result: the spawn data and director rules are not implemented yet.

- [ ] **Step 3: Implement zombie health and damage.**

  Add Normal and Giant stats. Start with Normal HP `100`, speed `2.6`, contact damage `10`; Giant HP `500`, speed `1.4`, contact damage `25`. On hit, call `PlayHitFlash`; on death, disable the NavMeshAgent, stop damage, play dissolve and return the object to `ObjectPooling` after the dissolve finishes.

- [ ] **Step 4: Implement NavMesh chase and contact damage.**

  Add `NavMeshAgent` to both prefabs, set the target from the director, update the destination only while the player is alive, and use a short per-target contact damage interval so an overlap does not drain HP every frame.

- [ ] **Step 5: Create the URP Shader Graph.**

  Create an opaque-compatible Shader Graph with `_DissolveAmount`, `_HitFlashAmount` and `_EdgeColor` properties. Use a noise texture or simple procedural noise for the dissolve threshold, add an emission edge, and expose the properties on `ZombieDissolve.mat`. Verify the graph works on the URP zombie prefab before wiring animation code.

- [ ] **Step 6: Implement the four-direction spawn director.**

  Author four spawn points around each map, reject positions within the player safety radius, enforce `baseMaxAlive`, ramp interval through `spawnIntervalOverNormalizedTime`, and request Giant only for Level 2 after its configured start time. Reuse `ObjectPooling` for zombie instances.

- [ ] **Step 7: Re-run tests and perform the arena test.**

  Run `SpawnDirectorRulesTests`; expected result is PASS. In a temporary flat arena, confirm four-direction spawning, NavMesh obstacle avoidance, normal/giant speed difference, hit flash, dissolve and knockback.

- [ ] **Step 8: Commit the enemy slice.**

  Commit with `feat: add zombie ai waves and dissolve effects`.

**Acceptance:** Zombies navigate around obstacles, spawn from all four directions with increasing pressure, Giant zombies are limited to Level 2, damage/death works, and the required Shader Graph effects are visible.

### Task 5: Build Level 1, Level 2, timer, win/lose flow and direct level selection

**Day:** 4, 10-12 hours  
**Files:**
- Create: `Assets/Scripts/ZombieWar/Core/LevelTimer.cs`
- Create: `Assets/Scripts/ZombieWar/Core/LevelMapLoader.cs`
- Create: `Assets/Scripts/ZombieWar/Core/WinLoseController.cs`
- Create: `Assets/Scripts/ZombieWar/UI/ZombieWarMainMenu.cs`
- Create: `Assets/Scripts/ZombieWar/UI/ZombieWarResultPanel.cs`
- Create: `Assets/ScriptableObjects/ZombieWar/Levels/Level1.asset`
- Create: `Assets/ScriptableObjects/ZombieWar/Levels/Level2.asset`
- Create: `Assets/Prefabs/ZombieWar/Maps/Level1_Map.prefab`
- Create: `Assets/Prefabs/ZombieWar/Maps/Level2_Map.prefab`
- Modify: `Assets/Scenes/MainMenu.unity`
- Modify: `Assets/Scenes/Gameplay.unity`
- Modify: `ProjectSettings/EditorBuildSettings.asset`

**Interfaces:**

- `LevelTimer` exposes `float RemainingSeconds`, `bool IsComplete`, `void StartTimer(float durationSeconds)`, `void PauseTimer()`, `void ResumeTimer()` and `event Action<float> RemainingTimeChanged`.
- `LevelMapLoader` exposes `GameObject ActiveMap`, `void Load(LevelManager level)` and `void UnloadCurrentMap()`.
- `WinLoseController` exposes `void OnPlayerDied()`, `void OnTimerCompleted()`, `bool IsResultShown` and `event Action<bool> ResultShown` where `true` means win.
- `ZombieWarMainMenu` exposes `void OnLevel1Pressed()` and `void OnLevel2Pressed()`; both call `GameSessionManager.SelectLevel` and `StartSelectedLevel`.
- `ZombieWarResultPanel` exposes `void ShowWin()`, `void ShowLose()`, `void OnRetryPressed()` and `void OnMainMenuPressed()`.

- [ ] **Step 1: Create map prefabs from the selected environment.**

  Build Level 1 with flat ground, visible cover and four NavMesh spawn points. Build Level 2 with at least one readable slope, obstacles, four spawn points and three bomb pickup locations along the route. Keep all collision geometry simple enough for reliable NavMesh baking.

- [ ] **Step 2: Create the two level assets.**

  Assign map prefabs, 180-second duration, Level 1 `allowGiantZombies = false`, Level 2 `allowGiantZombies = true`, Level 2 giant start at the middle phase, maximum bomb pickups `3`, base alive count and spawn interval curves.

- [ ] **Step 3: Add timer and terminal state tests.**

  Add PlayMode or EditMode tests for timer start at `180`, pause preserving time, completion at zero and Win/Lose being emitted only once. Run them before implementing the timer and verify the expected failures.

- [ ] **Step 4: Implement timer, map loader and result transitions.**

  Start the timer only after `GameplayBootstrap.LevelReady`, stop the spawn director on either terminal result, disable player combat, show the result panel and preserve the selected level for Retry.

- [ ] **Step 5: Implement direct level selection.**

  Wire Main Menu Level 1 and Level 2 buttons to the two ScriptableObjects. The selected asset is passed through `GameSessionManager`; `Gameplay.unity` loads only that asset and does not duplicate level-specific code.

- [ ] **Step 6: Bake and validate NavMesh per map.**

  Bake Level 1 and Level 2 separately, verify slope walkability and obstacle paths, then play each level for at least 60 seconds with both zombie types and bomb pickups.

- [ ] **Step 7: Re-run timer/result tests and verify full flow.**

  Confirm Main Menu -> Level 1, Main Menu -> Level 2, Retry and Main Menu return. Verify timer win at zero and lose on player HP zero without duplicate panels or zombie spawning after the result.

- [ ] **Step 8: Commit the level flow.**

  Commit with `feat: add zombie war levels and win lose flow`.

**Acceptance:** Both levels can be selected directly, each map is navigable, both terminal conditions work at 180 seconds/zero HP, and the result flow can retry or return to the menu.

### Task 6: Finish HUD, responsive UI, audio, effects and mobile feel

**Day:** 5, 10-12 hours  
**Files:**
- Create: `Assets/Scripts/ZombieWar/UI/ZombieWarHud.cs`
- Create: `Assets/Scripts/ZombieWar/Audio/ZombieWarAudio.cs`
- Create: `Assets/Prefabs/ZombieWar/UI/ZombieWarHud.prefab`
- Create: `Assets/Prefabs/ZombieWar/UI/ZombieWarMainMenu.prefab`
- Create: `Assets/Prefabs/ZombieWar/UI/ZombieWarResultPanel.prefab`
- Modify: `Assets/Scripts/Utilities/UIScaler.cs` only if the landscape profiles expose a real scaling defect
- Modify: `Assets/Scripts/Utilities/SafeArea.cs` only if the HUD needs a targeted landscape fix
- Modify: `Assets/Scenes/MainMenu.unity`
- Modify: `Assets/Scenes/Gameplay.unity`
- Modify: `ProjectSettings/ProjectSettings.asset`

**Interfaces:**

- `ZombieWarHud` exposes `void SetHealth(float current, float max)`, `void SetRemainingTime(float seconds)`, `void SetActiveWeapon(WeaponId weaponId)`, `void SetBombCount(int count)`, `void SetBombButtonInteractable(bool value)` and `void SetPauseVisible(bool value)`.
- `ZombieWarAudio` exposes `void PlayWeaponFire(WeaponId weaponId)`, `void PlayZombieHit()`, `void PlayZombieDeath()`, `void PlayBombExplosion()`, `void PlayUiClick()` and `void SetGameplayMusic(bool enabled)`.

- [ ] **Step 1: Add the HUD update tests or inspector checklist.**

  Define the HUD acceptance cases: HP updates after damage, timer displays `03:00` at start and `00:00` at completion, active weapon label/icon changes, bomb count never exceeds three, and the Bomb button is disabled at zero.

- [ ] **Step 2: Implement the HUD.**

  Place the left and right `OnScreenStick` controls in the lower corners, weapon switch control near the right stick, Bomb button above it, HP and timer at the top, and a pause button in a safe-area region. Use the existing `SafeArea` component on the interactive HUD root and avoid putting gameplay buttons behind notches or system bars.

- [ ] **Step 3: Implement menu and result visuals.**

  Use the selected font and a clear two-button Level 1/Level 2 layout. Result panel must show Win/Lose, Retry and Main Menu. Keep all text and button states readable at `1920 x 1080` and `2400 x 1080`.

- [ ] **Step 4: Wire audio and feedback.**

  Connect the available gun, zombie, explosion and UI clips. Add mobile WarFX muzzle, impact and explosion prefabs to the effect spawner. Confirm effects are disabled or returned to pooling after use.

- [ ] **Step 5: Set landscape and resolution behavior.**

  Set the application orientation to landscape, use the `1920 x 1080` Canvas reference, and test the UI in the three profiles from the context file. Adjust anchors and Safe Area rather than adding per-device pixel offsets.

- [ ] **Step 6: Tune feel and performance.**

  Tune aim dead-zone, player speed, fire rates, enemy pressure, camera damping, muzzle duration, hit flash duration and dissolve duration. Prewarm normal zombies, giant zombies and common effects with `ObjectPooling`; remove obvious per-frame allocations from spawn and damage loops.

- [ ] **Step 7: Verify the complete playable loop.**

  Record a local checklist run for both levels: menu selection, movement, twin-stick firing, all three guns, bomb pickup/use, normal/giant zombies, hit/dissolve, timer win, HP lose, retry and return to menu.

- [ ] **Step 8: Commit the presentation slice.**

  Commit with `feat: finish zombie war hud audio and mobile polish`.

**Acceptance:** The game reads clearly on landscape phone layouts, all required controls are reachable, feedback is audible/visible, and both levels are presentable without the old rhythm UI appearing.

### Task 7: Day 6 QA, stretch goal decision and submission package

**Day:** 6, 10-12 hours  
**Files:**
- Create: `docs/ZOMBIE_WAR_SUBMISSION_CHECKLIST.md`
- Modify: `Assets/Scenes/MainMenu.unity` only for verified presentation fixes
- Modify: `Assets/Scenes/Gameplay.unity` only for verified gameplay fixes
- Modify: `ProjectSettings/EditorBuildSettings.asset` only for verified build-order fixes
- Optional create: Rocket Gun files only if all mandatory acceptance items pass

- [ ] **Step 1: Run the mandatory regression checklist.**

  Test both levels from a fresh launch. Confirm no scene depends on `SampleScene`, no song/beat-map UI appears, no zombie can spawn inside the safety radius, no player can fire without aim input, and no terminal state continues spawning or damaging.

- [ ] **Step 2: Run the resolution checklist.**

  Test `1920 x 1080`, `2400 x 1080` and `2048 x 1536` in the Editor or Android Emulator. Check joystick positions, Bomb button, weapon control, HP, timer, pause and result buttons for clipping and overlap.

- [ ] **Step 3: Run the performance smoke test.**

  Play Level 2 during the highest spawn intensity. Verify the game remains responsive, pooled enemies/effects recycle, there are no repeated console exceptions and the camera remains stable. If performance is poor, reduce alive cap and effect lifetime before reducing required visuals.

- [ ] **Step 4: Decide the Rocket Gun stretch goal.**

  Add Rocket Gun only if the mandatory regression checklist is already clean. It must reuse `WeaponController`, have one rocket prefab, radial damage and an impact effect; it must not introduce a new input path or delay APK validation.

- [ ] **Step 5: Build and smoke-test the APK.**

  Build the Android APK using the existing known workflow. Install it on the available emulator or a borrowed Android device if available. Test menu launch, touch sticks, shooting, bomb, level transition and result screens once on device.

- [ ] **Step 6: Prepare the gameplay video.**

  Record a short landscape video showing: Main Menu level selection, Level 1 movement and firing, weapon switching, bomb pickup/use, Level 2 slope, Giant zombie, hit flash, dissolve and either Win or Lose result.

- [ ] **Step 7: Clean and verify GitHub source.**

  Confirm the repository includes `Assets`, `Packages`, `ProjectSettings`, the context file, the submission checklist and no machine-specific absolute asset dependencies. Open the project from the committed source before pushing.

- [ ] **Step 8: Complete the checklist and final commit.**

  Write the actual APK filename, Unity version, controls, known limitations and video link into `docs/ZOMBIE_WAR_SUBMISSION_CHECKLIST.md`. Commit with `chore: prepare zombie war submission`.

**Acceptance:** A fresh evaluator can launch the APK, select either level, understand the controls, see the required systems and watch a clean video without encountering a blocking error.

## Six-Day Schedule

| Day | Deliverable | Stop condition |
| --- | --- | --- |
| 1 | New scenes, input contract, twin-stick player, camera and animation base | Player can move/aim in `Gameplay.unity` with both sticks |
| 2 | Pistol, Rifle, Shotgun, damage and finite bombs | Combat is playable in a temporary arena |
| 3 | Normal/Giant AI, NavMesh, four-direction waves, hit/dissolve | Zombies chase, take damage and die reliably |
| 4 | Two maps, level assets, timer, win/lose, direct level selection | Both levels can be completed or lost from a fresh menu launch |
| 5 | HUD, Safe Area, audio, particles, tuning and pooling | Mandatory acceptance checklist passes in Editor |
| 6 | QA, performance, APK smoke test, video, GitHub and optional Rocket Gun | Submission package is ready and reproducible |

## Self-review

- Spec coverage: player/camera/input, three guns, bombs/physics, two zombie types, NavMesh, four-direction waves, two levels, timer/win/lose, Shader Graph, animation layer, audio/particles, UI/multi-resolution and submission are all assigned to tasks.
- Open-ended marker check: no task depends on an unnamed future system; stretch work is explicitly limited to Rocket Gun after mandatory acceptance.
- Type consistency: `DamageInfo`, `IDamageable`, `WeaponId`, `WeaponDefinition`, `WeaponController`, `BombController`, `LevelManager`, `GameSessionManager`, `LevelTimer` and `WinLoseController` are defined before later tasks consume them.
- Scope check: the plan is one cohesive vertical-slice project; the old rhythm flow is isolated rather than refactored broadly.
