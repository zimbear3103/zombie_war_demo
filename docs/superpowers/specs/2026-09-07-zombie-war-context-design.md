# Zombie War Project Context and Design

**Document type:** Project context file and approved design baseline  
**Date:** 2026-09-07  
**Source brief:** `C:\Users\kiend\Downloads\IndieZ_TEST_1_Game_Developer_(Unity.pdf`

## 1. Goal

Deliver a polished, playable 3D top-down mobile Zombie War prototype in six working days, with the mandatory gameplay, physics, animation, shader, visual and UI requirements visible in the final APK and video submission.

The working budget is 10-12 hours per day. The target is a complete playable build by the end of Day 5 and a dedicated Day 6 for polish, QA, APK validation, GitHub cleanup and video recording.

## 2. Fixed technical baseline

- Unity `6.0.6f1`.
- Universal Render Pipeline `17.6`.
- Input System package `1.20.0`.
- Cinemachine package `6.6.0`.
- AI Navigation package `2.0.14`.
- UI uses Unity UGUI and TextMesh Pro.
- Target platform is Android in landscape orientation.
- The existing project remains the source project; do not downgrade Unity or convert away from URP.
- Build setup is known by the developer and is not a planning risk. Only final APK smoke testing is required.
- Private fields follow the project convention `m_camelCase`; public fields use `camelCase`; Inspector fields use `[SerializeField] private`.
- Code comments are written in English.

## 3. Current project state

The current playable baseline is:

`SampleScene -> Play -> virtual joystick/keyboard -> player moves`

Already working:

- Camera follows the player.
- Player input works with keyboard and virtual joystick.
- `Assets/Scripts/PlayerController.cs` currently translates the player from a single Move vector.

The current project also contains an older rhythm-game flow. The following files are reusable only where they do not force rhythm-game behavior into Zombie War:

- `Assets/Scripts/GamePlayController.cs` contains a state machine, but its level setup still references songs, beat maps and conductor playback.
- `Assets/Scripts/MainStateManager.cs` routes the current loading, menu and gameplay states.
- `Assets/Scripts/UI/Screen/UIHome.cs` builds a song list and must be replaced or isolated for level selection.
- `Assets/Scripts/UI/Screen/UIInGame.cs` displays song progress and stars and must be replaced or isolated for Zombie War HUD.
- `Assets/Scripts/Utilities/ObjectPooling.cs` is reusable for zombies, projectiles and effects after its prefab lifecycle is verified.
- `Assets/Scripts/Utilities/SafeArea.cs`, `UIScaler.cs` and `ResponsiveAspectRatio.cs` are reusable starting points for mobile UI, but their final behavior must be validated in landscape.

Current build settings include only `Assets/Scenes/SampleScene.unity`. The final build settings must include the new menu and gameplay scenes.

## 4. Approved gameplay loop

1. Main Menu opens.
2. Player selects Level 1 or Level 2 directly.
3. The selected level loads into `Gameplay.unity`.
4. The player moves with the left virtual joystick.
5. The player aims with the right virtual joystick.
6. When the right joystick is held beyond the dead-zone, the active gun fires continuously in the aim direction.
7. The player can switch between Pistol, Assault Rifle and Shotgun.
8. The player can press a separate Bomb button if a bomb is available.
9. Zombies spawn continuously from four directions with increasing intensity.
10. The player survives for 180 seconds or loses when HP reaches zero.
11. Level 1 uses flat terrain and obstacles.
12. Level 2 uses slopes and adds Giant zombies.
13. A result screen exposes retry and return-to-menu actions.

## 5. Combat specification

### Player and aim

- Movement is on the XZ plane.
- Camera is a top-down Cinemachine follow camera with a fixed downward angle.
- The player visual rotates toward the current aim vector on the XZ plane.
- No directional foot animation is required.
- Use a simple Idle/Run locomotion blend driven by movement magnitude.
- Use an upper-body shooting layer while the player is moving.
- Aim input has a configurable dead-zone. The default planning value is `0.25`.

### Weapons

The mandatory weapons are:

- Pistol: low-to-medium damage, medium fire rate, one projectile per shot.
- Assault Rifle: lower per-shot damage, high fire rate, one projectile per shot.
- Shotgun: multiple pellets per shot, short range, high burst damage and light knockback.

The weapon system must expose a common fire interface so weapon switching does not change player input code. Rocket Gun is a stretch goal only and must not delay the three mandatory weapons.

### Bombs

- A separate Bomb button triggers the bomb.
- Bomb inventory is finite.
- Maximum pickup count on the level is three.
- Bomb pickups are placed along the playable route.
- Explosion applies radial damage with distance falloff and physical knockback to zombies.
- No cooldown is required for the finite-inventory MVP; input must reject accidental duplicate activation in the same frame.

### Zombies

- Normal zombie: melee chase, lower HP, normal speed and contact damage.
- Giant zombie: slower chase, higher HP and damage, used in Level 2.
- Both use `NavMeshAgent` and target the player.
- Zombie hit feedback uses a short URP Shader Graph hit flash.
- Zombie death uses a URP Shader Graph dissolve effect before the object is despawned or disabled.

## 6. Spawn and level specification

- Use four authored spawn points around the arena.
- A spawn director continuously schedules enemies and increases pressure over time.
- Do not spawn inside a safety radius around the player.
- Level duration is exactly 180 seconds.
- Level 1 initially spawns Normal zombies only.
- Level 2 introduces Giant zombies during the middle and late phase.
- Difficulty tuning is data-driven enough to adjust spawn interval, maximum alive count, enemy speed and giant timing from the Inspector without code changes.
- Level 1 map: flat ground, cover/obstacles and four-direction access.
- Level 2 map: slopes, obstacles and a route containing up to three bomb pickups.

## 7. Visual and audio specification

Use the already selected asset stack:

- Soldier: `Assets/ToonSoldiers_WW2_demo`.
- Zombie: `Assets/NewPunch/ShirtlessZombieFree`, using the URP prefab.
- Environment: `Assets/ithappy/Military_Free`.
- Guns and weapon audio: `Assets/PostApocalypseGunsDemo`.
- Mobile muzzle flash, bullet impact, explosion and smoke: `Assets/JMO Assets/WarFX Mobile` and its mobile effects folders.
- Existing sound resources may be reused when they fit the new gameplay.

Required feedback:

- Muzzle flash and shot audio for each weapon.
- Recoil or short firing pose feedback.
- Bullet impact feedback.
- Zombie hit flash.
- Zombie dissolve death.
- Bomb explosion particle, audio, damage and knockback.
- Ambient/background audio and basic UI click feedback.

## 8. UI and resolution baseline

- Landscape orientation.
- Canvas reference resolution: `1920 x 1080`.
- Canvas Scaler: `Scale With Screen Size`, Match approximately `0.5`.
- Apply Safe Area to the interactive HUD layer.
- Main Menu contains direct Level 1 and Level 2 buttons.
- Gameplay HUD contains HP, remaining time, active weapon, weapon switch control, Bomb count, Bomb button and pause.
- Result UI contains Win/Lose state, Retry and Main Menu.
- Test profiles: `1920 x 1080` 16:9, `2400 x 1080` 20:9, and `2048 x 1536` 4:3 smoke test.

## 9. Mandatory acceptance criteria

The submission is acceptable only when all of the following work in one clean playthrough:

- Main Menu launches and both levels can be selected directly.
- Gameplay scene loads the selected map and starts a 180-second run.
- Left joystick moves the player; right joystick aims and automatically fires beyond the dead-zone.
- Player rotation follows aim direction while moving.
- Pistol, Assault Rifle and Shotgun can be switched and have visibly different firing behavior.
- Normal zombies path toward the player from four directions.
- Giant zombies appear in Level 2.
- Damage, player HP, zombie death and game over work.
- Bomb pickups increase inventory up to three and Bomb button triggers radial physics damage and knockback.
- Level 1 and Level 2 are visually distinguishable and navigable.
- Hit flash and dissolve effects are visible.
- Timer reaches zero and produces a win result.
- HP reaches zero and produces a lose result.
- HUD remains usable in all three test profiles.
- APK, GitHub source and recorded video are ready for submission.

## 10. Scope protection

If the schedule slips, cut work in this order:

1. Rocket Gun.
2. Extra weapon reload/ammo systems.
3. Advanced bomb presentation beyond damage, knockback, particle and audio.
4. Decorative UI animation.
5. Additional zombie variants.

Never cut the twin-stick control, the three mandatory weapons, two level selections, NavMesh chase, 180-second win/lose flow, bomb interaction, hit/dissolve shader or final submission checks.

## 11. Submission package

- GitHub link with source code and project assets required to open the project.
- Android APK.
- Short gameplay video showing Main Menu, Level 1, Level 2, weapon switching, twin-stick firing, bomb explosion, Giant zombie, hit/dissolve effect and win/lose flow.

