# Zombie War — Player Core Design

**Ngày:** 2026-09-08

**Trạng thái:** Luật điều khiển và cách tổ chức đã được duyệt trong hội thoại; bản spec này chờ review trước khi viết implementation plan.

**Phạm vi:** Player core chạy được trong một test arena.

**Baseline:** [Zombie War context](2026-09-07-zombie-war-context-design.md) và [MVP roadmap](../plans/2026-09-07-zombie-war-mvp.md).

## 1. Kết quả cần đạt

Player có thể chạy né, ngắm và bắn độc lập bằng hai joystick. Khi vừa chạy vừa bắn, hướng di chuyển không bị hướng ngắm kéo lệch. Movement có collision, đi được trên dốc cho phép, camera follow ổn định, animation và shot feedback phản ánh hành động thực tế.

Vòng kiểm chứng đầu tiên: vào arena → di chuyển → ngắm/bắn Rifle vào mục tiêu → nhận damage → chết hoặc pause → reset để test lại.

Đây là một phần triển khai của MVP, không phải thay thế roadmap cả game. Pistol, Shotgun, weapon switching, bom, zombie AI/spawner, hai level hoàn chỉnh, menu và timer 180 giây vẫn thuộc các phần tiếp theo của roadmap.

## 2. Những quyết định đã duyệt

| Điều khiển | Movement | Hướng nhìn | Bắn |
| --- | --- | --- | --- |
| Chỉ kéo stick trái | Theo stick trái | Theo hướng chạy | Không |
| Chỉ kéo stick phải vượt dead-zone | Đứng tại chỗ | Theo hướng aim | Liên tục theo fire rate |
| Kéo cả hai stick | Theo stick trái | Theo hướng aim | Liên tục theo fire rate |
| Thả stick phải, vẫn giữ stick trái | Tiếp tục chạy | Chuyển sang hướng chạy | Dừng |
| Thả cả hai | Dừng | Giữ hướng nhìn cuối | Dừng |

- Dùng `CharacterController` cho player thay vì movement bằng Transform kết hợp dynamic Rigidbody.
- Move speed ban đầu: `5` world units/giây. Aim dead-zone: `0.25`.
- Movement và aim là hai dữ liệu độc lập; không dùng enum `Idle / Moving / Shooting` để loại trừ nhau.
- Camera follow root của player. Model nằm dưới một visual root riêng và đảm nhiệm xoay hướng nhìn.
- Animation gồm Idle/Run ở base layer và shooting ở upper-body layer.
- Pause hoặc chết khóa các hành động của player; lỗi cấu hình cần hiện rõ trong Editor.

## 3. Bám project thực tế

Thông tin kiểm tra từ file và Unity MCP ngày 2026-09-08:

- `ProjectSettings/ProjectVersion.txt`: Unity `6000.6.0f1`.
- `Packages/manifest.json`: URP `17.6.0`, Input System `1.20.0`, Cinemachine `6.6.0`, uGUI `2.6.0`.
- Scene làm việc: `Assets/ZombieWar/Scenes/GameplayZombie.unity`.
- Code hiện ở `Assets/ZombieWar/Scripts/`, prefab player ở `Assets/ZombieWar/Prefabs/Ingame/Player.prefab`.
- `PlayerController` hiện chỉ đọc Move rồi gọi `transform.Translate` trong `Update`. Player có dynamic Rigidbody và CapsuleCollider; chưa có visual child hoặc Animator.
- Prefab dùng `Assets/ZombieWar/Inputs/InputSystem_Actions.inputactions`. Move đã được nối; Look/Attack chưa có callback. Scene có một OnScreenStick trái.
- `PlayerHealth`, `PlayerAnimationController`, `WeaponController`, `BombController` và các controller gameplay mới hiện là class rỗng.

Baseline riêng cho Zombie War và cấu hình thực tế được giữ nguyên. Dòng Unity `6.0.6f1` trong tài liệu cũ là cách ghi không khớp `ProjectVersion.txt`; không đổi Editor/package để làm khớp tài liệu. Không áp baseline Unity 2022/Built-in/nGUI trong hướng dẫn chung vào phần này.

Các đường dẫn mới trong spec này thay cho đường dẫn `Assets/Scripts/ZombieWar` và `Assets/Scenes/Gameplay.unity` của roadmap cũ đối với player core. Không tạo một cây code player thứ hai.

## 4. Phạm vi bản player core

### Có trong bản này

- Move/Aim độc lập qua keyboard, gamepad và hai on-screen stick.
- CharacterController, gravity, collision với tường và di chuyển trên dốc hợp lệ.
- Model Toon Soldier, Idle/Run, upper-body shooting, muzzle anchor và camera follow.
- Một khẩu Assault Rifle hitscan để kiểm chứng auto-fire, cooldown và damage.
- Shared damage contract, một mục tiêu test nhận damage, player HP và sự kiện chết.
- Cổng bật/tắt gameplay trên player; test pause/resume và reset trong arena mà không phụ thuộc menu/session hoàn chỉnh.
- Feedback nhìn thấy được cho phát bắn và hit; âm thanh nếu asset phù hợp đã có trong project.
- Kiểm chứng bằng test logic có ý nghĩa và test Play Mode trong arena.

### Để các phần sau của MVP xử lý

- Pistol, Shotgun, switching UI và cấu hình đầy đủ của ba loại súng.
- Ammo/reload, auto-target, aim assist, jump/dash, recoil ảnh hưởng aim.
- Bomb/pickup, zombie AI, spawn, ragdoll, shader dissolve và map hoàn chỉnh.
- Main Menu, scene/session flow, timer, win/result HUD và APK submission.
- Animation chạy ngang/lùi riêng, IK bàn tay và procedural upper-body aiming.

Không thêm dependency hoặc framework gameplay mới cho player core.

## 5. Tổ chức và quyền điều khiển

| Thành phần | Trách nhiệm | Không tự quyết định |
| --- | --- | --- |
| `ZombieWarInputRouter` | Đọc Move/Aim, đưa dữ liệu hiện tại cho player, xóa input cache khi ngừng điều khiển | HP, cooldown súng, animation |
| `PlayerController` | Điều phối một frame: đọc input, movement/gravity, facing, yêu cầu bắn khi được phép | Tính damage và thời điểm súng sẵn sàng |
| `WeaponController` | Kiểm tra cooldown, hitscan, áp damage, phát sự kiện `Fired` sau phát bắn thành công | Đọc joystick, di chuyển player |
| `PlayerAnimationController` | Đọc tốc độ thực tế và phản ứng với `Fired`; blend locomotion/shooting | Sinh damage hoặc quyết định được bắn |
| `PlayerHealth` | Giữ HP, nhận damage hợp lệ, phát `HealthChanged`/`Died`, reset HP | Đổi scene hoặc hiện result panel |

`PlayerController` là nơi duy nhất gọi movement của CharacterController và xoay visual root. Root motion tắt. Animator không ghi vị trí player. Weapon không xoay player hoặc lấy input riêng.

Điều kiện hành động: `gameplayEnabled && !playerHealth.IsDead`. Moving, aiming và shot feedback có thể cùng tồn tại bên trong điều kiện này; không cần thêm state-machine framework.

Một frame có thứ tự rõ ràng:

1. Lấy Move/Aim hiện tại từ cùng một nguồn input runtime.
2. Kiểm tra quyền hành động; khi không được phép thì bỏ input và ngừng phát sinh shot.
3. Tính và thực hiện movement cùng gravity; cập nhật tốc độ ngang thực tế.
4. Chọn hướng nhìn theo bảng điều khiển và xoay visual root.
5. Nếu đang aim hợp lệ, yêu cầu WeaponController bắn theo hướng aim.
6. Animation nhận tốc độ thực tế và các sự kiện shot đã thành công.

Gameplay flow sau này gọi `SetGameplayEnabled(bool)`; chết khóa hành động ngay cả khi flag gameplay vẫn đang bật. Arena dùng một harness nhỏ để gọi cùng cổng này khi pause/reset, không xây dựng lại `GameSessionManager` trong player core.

Harness là owner duy nhất của pause toàn arena: lưu time scale trước pause, đặt về `0` và khóa player; resume khôi phục time scale và mở lại cổng điều khiển. Harness cung cấp ba thao tác rõ ràng: Pause/Resume, Damage Player (10 HP), Reset Arena. Reset khôi phục cả player và target test. Scene `GameplayZombie` có thể chạy player trực tiếp; khi session flow được triển khai, session sẽ sở hữu cổng điều khiển này.

## 6. Input và facing

- Mở rộng asset `InputSystem_Actions.inputactions` đang được prefab sử dụng bằng một action `Aim` kiểu `Value/Vector2` cho player này. Không duy trì thêm một luồng runtime song song qua `InputManager.inputactions`.
- Move: WASD hoặc `<Gamepad>/leftStick`. Aim: arrow keys hoặc `<Gamepad>/rightStick`.
- Gỡ arrow keys khỏi Move của player để arrow keys chỉ điều khiển Aim. Action Look dùng mouse delta hiện tại không được coi là hướng aim của game top-down.
- Hai OnScreenStick ghi vào hai control gamepad khác nhau. Input router đọc action; không cộng thêm virtual vector trực tiếp vào cùng action lần thứ hai.
- Move ánh xạ `(x, y)` sang world `(x, 0, y)`, clamp magnitude tối đa `1` và giữ mức kéo analog dưới `1`. Camera giữ yaw cố định; không thêm camera-relative movement trong bản này.
- Aim đang hoạt động khi magnitude sau xử lý Input System lớn hơn `0.25`. Tại hoặc dưới ngưỡng này không bắn. Dead-zone gameplay chỉ kiểm tra ở một nơi; ghi rõ processor của binding khi tune để tránh áp cùng ngưỡng hai lần ngoài ý muốn.
- Hướng aim chuẩn hóa được dùng cho facing và hướng hitscan. Trong bản test đầu, visual căn hướng aim ngay trước shot để nòng súng không còn đang xoay về hướng cũ lúc ray đã bắn sang hướng mới. Khi chỉ move, visual quay mượt với tốc độ chỉnh được. Ray không lấy từ rotation bị animation làm lệch của muzzle.
- Khi ngừng aim, không bắn theo hướng aim được cache. Khi đứng yên không có input, giữ hướng nhìn cuối để tránh quay về hướng mặc định.
- Khi mất focus, tắt control hoặc disable player: xóa cache Move/Aim. Mất focus còn phải reset virtual stick để giá trị synthetic gamepad cũ không được đọc lại như một ngón tay đang giữ. Khi resume, chỉ dùng trạng thái control được đọc lại; release diễn ra trong lúc pause không được để lại input kẹt. Nếu control thực sự vẫn đang giữ khi resume, áp luật điều khiển bình thường.
- Hai ngón tay phải điều khiển đồng thời được. Chạm/thả stick này không đổi control scheme theo cách làm hủy stick còn lại.

## 7. Movement và prefab

Chọn CharacterController vì player cần phản hồi trực tiếp theo input và có sẵn xử lý trượt tường, bậc thấp, slope limit. Phương án dynamic Rigidbody phù hợp hơn cho player chịu lực vật lý; requirement knockback hiện tại chủ yếu dành cho zombie. Không tiếp tục dùng Transform để di chuyển dynamic Rigidbody.

Hierarchy mục tiêu:

```text
PlayerRoot — CharacterController + input/player/health/weapon scripts
└── VisualRoot — yaw theo facing
    └── ToonSoldier — Animator, root motion off
        └── RightHand/WeaponHolder
            └── Muzzle
```

- Khi chuyển prefab, thay dynamic Rigidbody và CapsuleCollider locomotion bằng CharacterController; giữ root để camera và reference scene tiếp tục trỏ đúng.
- Fit collider theo model thực tế, đặt chân model sát đáy capsule. Không áp thông số height/radius từ prefab capsule cũ khi chưa kiểm tra scale model.
- CharacterController xử lý horizontal collision; script tích phân vertical velocity và gravity. Đi theo XZ không có nghĩa là khóa tọa độ Y: Y cần thay đổi theo mặt đất/dốc.
- Khi grounded, chặn vận tốc rơi tích lũy và duy trì lực bám đất nhỏ. Có giới hạn vận tốc rơi. Collider, skin width, step offset và slope limit được tune bằng arena có tường, góc tường, dốc lên/xuống và bậc thấp.
- Không có jump, player knockback hoặc đẩy vật thể vật lý trong slice này.
- Tốc độ animation lấy từ quãng di chuyển ngang thực tế, không chỉ độ lớn input; giữ stick vào tường không được phát Run đầy tốc độ khi player đứng tại chỗ.
- Cinemachine tiếp tục follow root và giữ góc nhìn hiện có. Chỉ tune damping khi kiểm chứng movement mới cho thấy jitter hoặc độ trễ khó điều khiển.

## 8. Rifle, damage và health

- Reuse `WeaponController` đang rỗng. Giữ một cấu hình Rifle chỉnh được trong Inspector; chưa xây inventory/switching cho một súng test.
- Dùng giá trị Rifle trong roadmap: damage `8`, fire rate `8` phát/giây. Range ban đầu cho arena là `25` world units, là thông số tune chứ không phải giá trị cân bằng đã nghiệm thu.
- Shot đầu được phép ngay khi aim bắt đầu và cooldown đã sẵn sàng. Giữ aim gọi yêu cầu bắn mỗi frame; WeaponController quyết định có phát shot hay không.
- Cooldown dùng thời gian gameplay có pause. Re-aim không reset cooldown. Frame chậm không bắn bù nhiều phát thành một burst trong một frame.
- Hitscan xuất phát tại Muzzle, theo hướng aim world XZ. HitMask gồm vật cản và mục tiêu, loại player cùng weapon visual. Mục tiêu test dùng collider không-trigger; ray bỏ qua trigger tương tác. Ray dừng ở vật cản đầu tiên; không bỏ qua tường để tìm mục tiêu phía sau.
- Khi sát cover, kiểm tra đoạn từ gốc súng/vai đến Muzzle để nòng xuyên qua tường không cho phép bắn từ phía bên kia. Obstruction tính là shot bị chặn vào vật cản, vẫn tiêu thụ cooldown; không damage mục tiêu phía sau.
- Nhận collider hit ở child thì tìm một owner damage duy nhất ở parent. Một ray chỉ áp một lần damage cho owner đó.
- Dùng `DamageInfo` và `IDamageable` như contract đã có trong roadmap để zombie tích hợp sau này; PlayerHealth và target test cùng implement contract này, không triển khai zombie AI.
- `Fired` chỉ phát khi WeaponController thực sự chấp nhận shot; bắn trượt vẫn là shot hợp lệ. Muzzle feedback có cả khi trượt; hit feedback chỉ xuất hiện tại collision hợp lệ. Không để animation event tự sinh damage.
- Player HP ban đầu cho arena là `100`, chỉnh được. Damage âm, bằng không hoặc không hữu hạn không làm tăng HP hoặc phát sự kiện chết. HP clamp trong `[0, maxHealth]`; `Died` chỉ phát một lần mỗi lần sống.
- Khi chết, ngừng movement do input/aim/fire và bỏ input cache ngay; gravity tiếp tục cho đến khi player chạm đất nếu đang rơi. Pause đóng băng cả movement và gravity. Không yêu cầu death clip chưa có; test hiển thị trạng thái chết rõ ràng. Reset arena đặt lại vị trí, HP, movement/gravity, input, cooldown và shot feedback.

Hitscan XZ không tự ngắm lên/xuống giữa các cao độ. Test damage ban đầu dùng mục tiêu ở cao độ tương thích với muzzle; test dốc chứng minh movement. Bắn ở chênh lệch cao độ của Level 2 phải được kiểm chứng riêng trong phần level/combat, không được coi là đã đạt chỉ vì player đi được trên dốc.

## 9. Animation và feedback

Asset có sẵn:

- Prefab: `Assets/ToonSoldiers_WW2_demo/ToonSoldier_WW2_demo_Prefab.prefab`.
- Humanoid Avatar có mapping spine/arms/hands/legs, root motion hiện tắt, chưa gắn Animator Controller.
- Clip: `infantry_combat_idle` và `infantry_combat_run` loop; `infantry_combat_shoot` là clip một lần. Cả ba trong `Assets/ToonSoldiers_WW2_demo/animation/`.
- Bone `Bip001 R Hand` có thể gắn WeaponHolder. Vị trí cầm súng và muzzle cần xác minh trong Editor, không suy ra từ tên bone.

Thiết kế Animator:

- Base layer: Idle/Run blend bằng `MoveSpeed`, normalized theo tốc độ ngang thực tế.
- Upper-body override layer với AvatarMask: shoot trên torso/arms, giữ locomotion ở legs. Dùng tín hiệu `Fire` từ shot được chấp nhận; không dùng `IsAiming` như bằng chứng đã có shot.
- Shot feedback ngắn cần đọc được ở cadence `8` phát/giây. Khi test clip, chỉnh transition/độ dài phản ứng để shot mới không liên tục reset trước pose chính và làm nhân vật kẹt một pose.
- Khi ngừng aim, không thêm shot mới; một phản ứng shot đã phát được phép blend-out. Pause đóng băng feedback hiện tại; chết/reset xóa phản ứng cũ.
- Không cần directional footwork cho bản đã duyệt. Upper-body mask không tạo ra clip chạy ngang/lùi; đây là giới hạn visual đã biết của bộ clip hiện tại.

Model/weapon và effect phải được preview trước khi tích hợp. Một gun mesh proxy đơn giản và feedback Shuriken có thể dùng trong arena nếu project chưa có asset thích hợp; ghi rõ đó là proxy trong kết quả test, không tải/mua dependency để hoàn tất slice này.

## 10. File boundary dự kiến

| Hành động | File/nhóm file |
| --- | --- |
| Sửa class hiện có | `Assets/ZombieWar/Scripts/Player/PlayerController.cs`, `PlayerHealth.cs`, `PlayerAnimationController.cs` |
| Sửa class hiện có | `Assets/ZombieWar/Scripts/Combat/WeaponController.cs` |
| Thêm input adapter | `Assets/ZombieWar/Scripts/Input/ZombieWarInputRouter.cs` |
| Thêm shared damage contract | `Assets/ZombieWar/Scripts/Combat/DamageInfo.cs`, `IDamageable.cs` |
| Sửa input đang dùng | `Assets/ZombieWar/Inputs/InputSystem_Actions.inputactions` |
| Cập nhật player riêng của project | `Assets/ZombieWar/Prefabs/Ingame/Player.prefab` |
| Thêm Animator/AvatarMask | `Assets/ZombieWar/Animations/Player/` |
| Arena và fixture | `Assets/ZombieWar/Scenes/PlayerCoreTest.unity`, `Assets/ZombieWar/Tests/` |
| Tích hợp sau test arena | `Assets/ZombieWar/Scenes/GameplayZombie.unity` |

Tái sử dụng prefab/model/clip nguồn bằng reference hoặc nested prefab; không sửa source FBX/importer toàn bộ asset pack để phục vụ một player. Runtime test harness và target test nằm trong vùng test và không gắn vào scene production. Cấu trúc test assembly cụ thể được xác định khi viết plan để tránh di chuyển các script legacy chỉ nhằm làm test compile.

Không chỉnh `AGENTS.md`, `CLAUDE.md`, shader/volume, code rhythm cũ hoặc các file user đang sửa ngoài phạm vi này. Tạo `.meta` bằng Unity khi thêm asset; giữ GUID hiện có khi sửa.

## 11. Error handling và lifecycle

- Reference bắt buộc: CharacterController, input router, health, weapon, visual root. Thiếu reference thì log lỗi mô tả component/property và khóa hành động phụ thuộc; không spam exception mỗi frame.
- Thiếu muzzle hoặc damage mask hợp lệ: không phát shot giả, báo cấu hình. Thiếu audio/particle: gameplay damage vẫn chạy, nhưng checklist feedback chưa được đánh dấu đạt.
- Subscribe/unsubscribe sự kiện đối xứng khi enable/disable. Retry/reset không nhân đôi callback damage, shot hoặc death.
- UI pause tiếp tục nhận input khi gameplay bị khóa. Chỉ khóa player action map, không tắt cả EventSystem/UI map.
- Test harness có trách nhiệm khôi phục time scale khi thoát pause/arena. Disable player tự xóa trạng thái input, không phụ thuộc một callback release có thể không tới.

## 12. Acceptance và cách kiểm chứng

### Logic cần test tự động

- Move `(1,1)` không vượt tốc độ tối đa; analog dưới `1` vẫn giữ cường độ.
- Bảng luật facing hoạt động, bao gồm aim và move ngược hướng, thả aim khi vẫn move và idle giữ hướng cuối.
- Magnitude aim tại/dưới `0.25` không phát shot; trên ngưỡng thì được yêu cầu bắn.
- Cooldown chặn shot sớm, re-aim không bỏ qua cooldown và một frame không phát burst bù.
- HP clamp đúng, bỏ damage không hợp lệ, death chỉ một lần, reset cho phép một lần chết mới.
- Gameplay khóa hoặc player chết thì không thể tiếp tục movement do input/fire; gravity khi chết tuân theo mục 8. Chọn tests theo hành vi, không viết tests chỉ kiểm tra getter hoặc sao chép implementation.

### Play Mode và test bằng mắt

1. WASD và arrow keys điều khiển độc lập, gamepad và hai on-screen stick cùng tuân theo một luật.
2. Test hai ngón tay cùng lúc; thả từng stick, kéo qua ngưỡng aim, kéo ra ngoài vùng stick, pause khi đang giữ stick rồi thả trong pause.
3. Giữ tốc độ tối đa trên trục và đường chéo; kéo nhẹ stick cho tốc độ thấp hơn.
4. Va tường không xuyên, trượt theo tường; đi lên/xuống dốc hợp lệ, dốc quá giới hạn không leo được, qua bậc thấp không kẹt.
5. Camera không đổi hướng theo aim; quan sát follow khi chạy thẳng, quay đầu và đi dốc.
6. Đứng bắn, chạy bắn cùng hướng/ngược hướng, đổi aim nhanh; visual hội tụ về hướng aim và shot đi đúng hướng điều khiển.
7. Mục tiêu nhận damage đúng, child collider nhận qua cùng owner, tường chặn ray kể cả khi Muzzle nhô xuyên cover, player không tự bắn trúng mình.
8. Idle/Run chuyển đúng; giữ stick vào tường không chạy tại chỗ đầy tốc độ; shooting không dừng chân khi đang chạy.
9. Pause dừng player/shot/cooldown/animation, UI pause còn dùng được; resume không có input đã thả bị kẹt. Chết khóa hành động ngay, player đang rơi tiếp tục chạm đất; reset sạch và không nhân đôi sự kiện.
10. Test joystick ở `1920×1080`, `2400×1080`, `2048×1536`; kiểm tra vị trí, safe area và khả năng thao tác. APK/device verification cuối cùng vẫn theo roadmap.
11. Tích hợp prefab đã qua arena vào `GameplayZombie` và chạy lại movement/aim/fire; không còn lỗi lặp trong Console liên quan player core.

Không đánh dấu runtime pass chỉ bằng việc script compile hoặc prefab serialize thành công. Lưu kết quả thực tế và giới hạn asset/thiết bị đã test khi kết thúc implementation.

## 13. Thứ tự triển khai sau khi spec được duyệt

1. Input contract, CharacterController movement, facing và test arena.
2. Toon Soldier, camera follow và locomotion.
3. Rifle hitscan, damage target, shot feedback và upper-body animation.
4. Health, pause/death/reset và các lifecycle edge cases.
5. Touch/resolution verification, tích hợp lại scene làm việc và ghi kết quả test.

Mỗi bước để lại một phần chạy được và có điểm kiểm chứng. Implementation plan sẽ cụ thể hóa file edits, test assembly/fixtures và thao tác Unity theo thứ tự này; không chạy nguyên Task 1–7 của roadmap MVP trong player-core slice.

## 14. Căn cứ kỹ thuật và giới hạn

- [Unity Character Controller manual](https://docs.unity3d.com/6000.0/Documentation/Manual/class-CharacterController.html): collision, slope/step và việc không tự phản ứng với lực vật lý.
- [Unity CharacterController.Move API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html): movement delta bị collision giới hạn; gravity do code gọi xử lý.
- Tài liệu API tham chiếu thuộc Unity 6.0; project thực tế là `6000.6.0f1`. Khi implementation dùng API cụ thể, đối chiếu package/API có trong Editor hiện tại.
- Khả năng kết hợp Humanoid/AvatarMask được suy ra từ importer và prefab; chất lượng pose/blending chỉ được xác nhận sau Editor preview và Play test.
- Footwork ngang/lùi, bắn chênh cao độ và mesh súng hoàn thiện không được ghi nhận là đã giải quyết bởi spec này.

## 15. Spec self-review

- Luật input/facing khớp hai lần duyệt trong hội thoại; giữ speed `5`, aim dead-zone `0.25` và hướng CharacterController.
- File paths/version khớp project đã kiểm tra; không tạo hệ thống player song song theo đường dẫn cũ.
- Một thành phần sở hữu movement/facing; input, weapon, health và animation có trách nhiệm phân biệt.
- Scope có một vòng test hoàn chỉnh; không kéo menu/spawner/bom/ba súng vào bước player core.
- Pause/death, cooldown, hướng ray, slope/gravity, callback lifecycle và giới hạn animation có hành vi hoặc điều kiện kiểm chứng cụ thể.
- Đây là review thiết kế và tài liệu; chưa có code mới hoặc kết quả runtime cho player core mới.
