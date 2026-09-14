# Zombie War

Demo game sinh tồn góc nhìn từ trên xuống được phát triển bằng Unity. Người chơi vừa di chuyển vừa ngắm bắn, đổi vũ khí, ném lựu đạn và sống sót trước các đợt zombie liên tục trong một màn chơi giới hạn thời gian.

Project tập trung vào cảm giác điều khiển twin-stick, gameplay loop rõ ràng và phần trình bày gồm animation, hand IK, VFX, UI responsive cùng positional audio.

## Gameplay

- Chọn level tại màn hình Home và bắt đầu một lượt chơi.
- Di chuyển và ngắm bắn độc lập; vũ khí tự khai hỏa khi hướng ngắm vượt qua dead zone.
- Thu thập Pistol, Rifle, Shotgun, đạn và lựu đạn trực tiếp trên bản đồ.
- Quản lý magazine, reserve ammo, reload, đổi vũ khí và số lựu đạn còn lại.
- Đối đầu các wave zombie chạy tuần tự; wave cuối lặp lại đến khi hết thời gian.
- Thắng khi nhân vật còn sống sau 180 giây. Thua khi nhân vật hết máu hoặc một subsystem gameplay bắt buộc bị mất trong lúc chạy.
- Điểm của lượt chơi là tổng số zombie tiêu diệt; best score được lưu local.

## Điểm nổi bật

### Player và combat

- Twin-stick control cho phép movement và aim hoạt động đồng thời.
- Hướng nhìn giữ nguyên theo lần aim gần nhất khi người chơi thả stick.
- Ba loại vũ khí có fire rate, magazine, reload time, damage, range, spread và pellet count riêng bằng `ScriptableObject`.
- Projectile di chuyển theo từng bước raycast, gây damage tại thời điểm impact và được tái sử dụng bằng `UnityEngine.Pool.ObjectPool`.
- Shotgun bắn nhiều pellet nhưng chỉ tiêu thụ một viên đạn cho mỗi lần khai hỏa.
- Lựu đạn có quỹ đạo ném, fuse, phạm vi nổ, damage diện rộng và explosion force.

### Zombie và wave spawning

- Zombie truy đuổi người chơi bằng `NavMeshAgent`, có melee cooldown và kiểm tra vật cản trước khi gây damage.
- Spawner chạy quota theo từng group và từng wave, giới hạn số zombie sống đồng thời, tránh spawn quá gần người chơi và kiểm tra vị trí hợp lệ trên NavMesh.
- Zombie được pooling và reset đầy đủ giữa các lần spawn hoặc restart level.
- Lifecycle chết tách biệt giữa ghi nhận kill, animation, dissolve effect và trả object về pool.

### Animation, VFX và audio

- Locomotion được điều khiển theo tốc độ movement thực tế của player.
- Animation Rigging dùng Two Bone IK để hai tay bám theo grip của từng vũ khí, có blend khi equip hoặc đổi súng.
- Muzzle flash, bullet trail, hit flash, explosion và dissolve được tích hợp vào combat feedback.
- Gameplay SFX hỗ trợ nhiều clip ngẫu nhiên, pitch variation và âm thanh 3D theo khoảng cách.
- Voice của weapon, zombie và bomb được quản lý để không cắt mất audio tail khi object gameplay bị recycle.

### UI và game flow

- Flow hoàn chỉnh từ Loading → Home → Gameplay → Win/Lose.
- Home screen hỗ trợ chọn level và hiển thị lỗi setup thay vì bắt đầu một run không hợp lệ.
- HUD cập nhật timer, loại súng, magazine/reserve ammo, số lựu đạn và máu người chơi.
- Có popup Settings, pause/resume, restart, về Home và result screen.
- UI hỗ trợ safe area, nhiều tỉ lệ màn hình và virtual controls cho mobile.

## Điều khiển

| Thiết bị | Di chuyển | Ngắm và bắn | Hành động khác |
| --- | --- | --- | --- |
| Keyboard | `W`, `A`, `S`, `D` | Các phím mũi tên | Reload, đổi súng và ném lựu đạn qua HUD |
| Gamepad | Left Stick | Right Stick | Các nút HUD hiện tại |
| Mobile | Virtual Stick bên trái | Virtual Stick bên phải | Button Reload, Swap và Bomb |

Vũ khí tự bắn khi người chơi giữ hướng aim hợp lệ. Movement không thay đổi hướng ngắm và không khóa thao tác bắn.

## Gameplay flow

```text
LoadScene
   ↓
Loading
   ↓
GameplayZombie / Home
   ↓ Chọn level và Start
Khởi tạo Map + Player + ZombieSpawner
   ↓
Playing ── Settings / Pause ── Resume
   ↓
Win hoặc Lose
   ├── Restart
   └── Home
```

`MainStateManager` là owner của state cấp ứng dụng và tick gameplay một lần mỗi frame. `GamePlayController` quản lý lifecycle của một run, timer, pause/settings lock, win/lose và kết quả. Cách tách này tránh tạo nhiều update loop cùng điều khiển session.

## Kiến trúc chính

| Thành phần | Trách nhiệm |
| --- | --- |
| `MainStateManager` | Điều phối Loading, Main Menu và Gameplay |
| `GamePlayController` | Quản lý state của run, timer, kết quả và pause/settings |
| `LevelManager` | Chọn level, tạo map/player và giải phóng dữ liệu của run |
| `LevelScriptableObject` | Lưu map, survival duration, wave, alive cap và safety radius |
| `LevelMap` | Cung cấp player spawn, zombie spawn points, pickup root và NavMesh |
| `PlayerController` | Movement, aim, facing, loadout, weapon switching và bomb inventory |
| `WeaponController` | Fire/reload, magazine, projectile pool và weapon lifecycle |
| `ZombieSpawner` | Wave scheduling, spawn validation, alive cap và zombie pool |
| `ZombieController` | NavMesh chase, attack, knockback, animation và death lifecycle |
| `UIManager` | Quản lý screen, popup và HUD |
| `SoundManager` | Music, UI SFX và pooled positional gameplay audio |

Các thông số gameplay chính được tách thành `ScriptableObject`, giúp cân bằng level, player, zombie và từng loại vũ khí mà không sửa code.

## Công nghệ

- Unity `6000.6.0f1`
- Universal Render Pipeline `17.6.0`
- Input System `1.20.0`
- AI Navigation `2.0.14`
- Animation Rigging `6.6.0`
- Cinemachine `6.6.0`
- UGUI và TextMesh Pro
- C# / Unity Physics / NavMesh

## Cấu trúc project

```text
Assets/ZombieWar/
├── Animations/          Animator Controller và animation clips
├── Inputs/              Input Actions cho Move và Aim
├── Materials/           Material cho environment, character và effects
├── Particles/           Dissolve particles
├── Prefabs/
│   ├── Effect/          Muzzle flash, trail và explosion
│   ├── Ingame/          Player, zombie, weapon, pickup, bullet và grenade
│   ├── Map/             Map gameplay và environment props
│   ├── UI/              Home, HUD và popup
│   └── System.prefab    Các manager dùng xuyên scene
├── Scenes/
│   ├── LoadScene.unity
│   └── GameplayZombie.unity
├── ScriptableObjects/   Level, character, zombie và weapon data
├── Scripts/
│   ├── Audio/
│   ├── Combat/
│   ├── Core/
│   ├── Effects/
│   ├── Enemies/
│   ├── Player/
│   ├── UI/
│   └── Utilities/
├── Settings/            URP renderer và render pipeline assets
├── Shaders/             Effect shaders
└── Sound/               Weapon, zombie và explosion audio
```

## Chạy project

1. Clone repository.
2. Mở project bằng Unity Hub với Unity `6000.6.0f1`.
3. Chờ Unity import assets và resolve packages.
4. Mở `Assets/ZombieWar/Scenes/LoadScene.unity`.
5. Nhấn Play; flow mặc định sẽ tải `GameplayZombie` và mở Home screen.

Hai scene `LoadScene` và `GameplayZombie` đã được bật trong Build Settings. Khi test nhanh trong Editor, có thể mở trực tiếp `GameplayZombie`; project vẫn đưa người chơi về Home trước khi bắt đầu run.

## Phạm vi hiện tại

- Một level chơi được: `Level 1`.
- Một zombie archetype: `NormalZombie`.
- Ba loại vũ khí: Pistol, Rifle và Shotgun.
- Một survival run dài 180 giây, gồm hai wave cấu hình bằng data.
- Có local best score; hệ thống star hiện chưa tham gia vào luật chấm điểm.
- Project là gameplay demo, chưa tích hợp backend, cloud save hoặc multiplayer.

## Kỹ năng thể hiện qua project

- Tổ chức game flow bằng state rõ ràng và kiểm soát lifecycle giữa scene, menu và gameplay.
- Xây dựng data-driven level/weapon configuration bằng `ScriptableObject`.
- Kết hợp Input System, NavMesh, Animation Rigging, Cinemachine, UI và URP trong một gameplay loop hoàn chỉnh.
- Tối ưu object lifecycle bằng pooling cho projectile, zombie và positional audio.
- Tập trung vào game feel thông qua animation blending, hand IK, hit feedback, dissolve, particle effects và sound variation.
- Validation các dependency quan trọng trước khi bắt đầu run, giảm lỗi null reference và trạng thái gameplay dở dang.

## Ghi chú

Repository hiện chưa có gameplay screenshot hoặc video preview. Việc bổ sung một GIF ngắn thể hiện twin-stick combat, zombie wave và dissolve effect sẽ giúp recruiter đánh giá phần visual/game feel nhanh hơn.
