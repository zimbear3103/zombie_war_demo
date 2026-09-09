# Zombie War — Scripts-only Core, Combat and Enemies

**Ngày:** 2026-09-08.

**Trạng thái:** Chủ project đã duyệt và yêu cầu bắt đầu sửa scripts trong hội thoại. Compatibility-only edits trong hai test cũ được cho phép; vẫn không thêm/chạy test hoặc build. Đây là design, không phải báo cáo runtime đã hoàn thành.

**Cập nhật 2026-09-09:** User xác nhận giữ player movement bằng `Transform.Translate` trong world space. Quyết định này thay thế CharacterController/gravity của player trong các plan trước; collision/gravity không được tự thêm vào slice này.

**Baseline code đã đọc:** commit `9992491` và các chỉnh sửa chưa commit của chủ project trong Player, Combat và Enemies.

## 1. Phạm vi và thứ tự ưu tiên

Tập trung scripts cho vòng gameplay: bắt đầu run → di chuyển/aim → dùng hoặc nhặt súng → zombie spawn/chase/attack → damage/death → thắng sau 180 giây hoặc thua khi hết HP → restart.

- Dùng cây code hiện có: `Assets/ZombieWar/Scripts/`.
- Giữ cấu hình project thực tế: Unity `6000.6.0f1`, URP và Input System đang cài; không đổi package.
- Ưu tiên sửa correctness và lifecycle trước, sau đó giảm công việc thừa trong các đường chạy thường xuyên. Không khẳng định cải thiện FPS/GC khi chưa đo.
- Giữ code user đã viết và các hướng thiết kế có ích; không reset worktree hoặc áp lại nguyên code blocks của plan cũ.
- Private fields: `m_camelCase`; Inspector fields: `[SerializeField] private`; code comments bằng English; KISS.

### Ngoài phạm vi lượt triển khai này

- Không tạo/sửa prefab, scene, model, material, visual, animation, audio hoặc hiệu ứng.
- Không chỉnh InputAction asset, UI layout, NavMesh bake hoặc Build Settings; cung cấp callback/API để chủ project tự nối trong Editor.
- Không viết/chạy test, tạo test arena, chạy Play Mode, build hoặc benchmark. Không dùng MCP để thay đổi Editor.
- Không tự thêm hệ thống ammo/reload, inventory UI, rarity, nâng cấp súng, generic stat framework hay ECS.
- Bomb và projectile visual vẫn ở roadmap MVP sau; không biến phần sửa weapon pickup thành triển khai tất cả feature combat còn lại.
- Giữ branch và các thay đổi Git của chủ project; không tự stage/commit/push/merge.

Spec này thay thế phần kiến trúc và trình tự thực thi đang xung đột trong [MVP plan](../plans/2026-09-07-zombie-war-mvp.md) và [player-core plan](../plans/2026-09-08-zombie-war-player-core.md). Các yêu cầu presentation/Editor trong tài liệu cũ được hoãn, không bị xóa khỏi mục tiêu sản phẩm cuối.

## 2. Quyền sở hữu gameplay

| Script | Trách nhiệm |
| --- | --- |
| `Core/GamePlayController.cs` | Owner duy nhất của trạng thái run, timer 180 giây, start/pause/resume/win/lose/restart; nối player, spawner và level hiện tại |
| `Player/PlayerController.cs` | Đọc Move/Aim, movement/facing, action gate, starting weapon tùy chọn và active weapon; chứa trực tiếp các control rules |
| `Player/PlayerStats.cs` | Stats runtime của một player, bao gồm health và trạng thái sống/chết; thực thi `IDamageable` |
| `Player/CharacterScriptableObject.cs` | Cấu hình base stats của player; kế thừa `ScriptableObject` |
| `Player/PlayerCollector.cs` | Nhận pickup và chuyển yêu cầu thu thập/trang bị; không tự sở hữu firing loop |
| `Combat/WeaponController.cs` và các weapon cụ thể | Cooldown, kiểm tra phát bắn, hitscan/damage; không tự bắn khi chưa có lệnh aim của player |
| `Combat/WeaponScriptableObject.cs` | Cấu hình weapon; kế thừa `ScriptableObject`, không giữ cooldown runtime |
| `Enemies/ZombieSpawner.cs` | Wave schedule, chọn group/điểm spawn, giới hạn số đang sống, reset và quản lý vòng đời các zombie đã spawn |
| `Enemies/ZombieController.cs` | Nhận target khi spawn, chase/attack và khóa hành động khi chết/run dừng |
| `Enemies/ZombieStats.cs` | Stats runtime từng zombie, health/damage/death và reset khi tái sử dụng; thực thi `IDamageable` |
| `Enemies/ZombieScriptableObject.cs` | Base config Normal/Giant; kế thừa `ScriptableObject`, không chứa HP runtime |

`ZombieWarGameController` hiện rỗng: không thêm logic mới vào đó. Hợp nhất vai trò vào `GamePlayController` hiện có, không tạo `GameplayController.cs` chỉ khác cách viết hoa trên Windows.

Không tạo thêm session owner trong `GameSessionManager` hoặc một state machine mới. `MainStateManager` vẫn điều phối main flow và gọi `GamePlayController.OnUpdate()`; không thêm một `Update()` thứ hai làm timer chạy hai lần. Giữ các entry point mà UI đang gọi trong quá trình chuyển sang Zombie War; không xóa API khiến UI ngoài phạm vi bị hỏng.

`GamePlayController` push trạng thái cho player/spawner và subscribe event chết của player. Player chỉ phát event/nhận lệnh qua API của chính nó, không gọi trực tiếp `GamePlayController.Instance`; cách này giữ đúng chiều dependency assembly hiện có.

## 3. Survival 180 giây và wave nối tiếp

### Quy tắc đã chốt

- Run thắng khi sống đủ **180 giây gameplay**; hết HP là thua.
- Zombie spawn liên tục theo các wave nối tiếp, không yêu cầu dọn sạch zombie mới được sang wave.
- Wave không quyết định thắng/thua; timer và player health quyết định kết quả run.
- Pause đóng băng timer, lịch spawn và gameplay actions. Resume không tạo burst bù các lần spawn/bắn trong lúc pause.
- Win/lose chỉ được chấp nhận một lần; sau terminal state, không spawn, bắn, attack hoặc chuyển kết quả lần nữa.

### Quy tắc thực thi được ghi rõ để review

- Giữ ý tưởng `Wave` chứa các `ZombieGroup` và quota mỗi group từ code của chủ project.
- Một wave kết thúc khi đã **spawn thành công đủ quota** của các group. Chọn wave kế tiếp ngay, phát spawn tiếp theo vẫn tuân thủ interval của wave mới; không có coroutine chờ thêm 3 giây giữa wave.
- Khi đã dùng hết danh sách mà run chưa hết 180 giây, lặp lại cấu hình wave cuối. Reset counter quota cho lượt lặp, không reset timer run hoặc số zombie đang sống.
- Mỗi nhịp spawn tạo tối đa một zombie từ group còn quota, đi theo thứ tự group đã cấu hình. Không vô tình tạo một zombie từ mọi group trong cùng nhịp.
- Mỗi wave cấu hình `spawnInterval` theo đơn vị giây giữa hai lần spawn. Không giữ hai nguồn cấu hình cạnh tranh là `Wave.spawnRate` và global interval.
- Tách config quota khỏi counter runtime. Tách `spawnedThisWave` khỏi `aliveCount`; số đã spawn không phải số còn sống.
- Có giới hạn zombie sống toàn run. Chạm giới hạn thì tạm ngưng spawn, không tăng counter hoặc xếp hàng để bù hàng loạt khi cap được giải phóng. Không xóa zombie wave trước khi chuyển wave.
- Chỉ tính quota/alive sau khi spawn và initialize thành công. Chết/despawn được ghi nhận đúng một lần; restart hủy đăng ký callback cũ và reset toàn bộ counter.
- Chọn từ `spawnPoints` được gán, tránh safety radius quanh player. Chỉ thử số điểm hữu hạn trong một nhịp; không có điểm hợp lệ thì thử lại ở nhịp sau, không quay vòng vô hạn hoặc tiêu quota.
- Danh sách wave/group rỗng, quota không dương, prefab thiếu hoặc interval không hợp lệ phải được phát hiện lúc initialize. Dừng scheduler và báo lỗi một lần; không crash hoặc spam Console từng frame.

Lặp wave cuối và một zombie mỗi nhịp là cách diễn giải cụ thể của yêu cầu spawn liên tục trong bản spec này; chúng chưa phải kết quả runtime đã kiểm chứng.

## 4. Player và stats

- Gộp `PlayerControlRules` vào các helper của `PlayerController`; không để hai bản logic song song.
- Giữ Move/Aim độc lập trên world XZ, speed ban đầu `5`, processed aim magnitude phải lớn hơn `0.25` để bắn.
- Aim có ưu tiên hướng nhìn; thả aim thì hướng nhìn theo movement; thả cả hai giữ hướng cuối. Clamp input để không tăng tốc chạy chéo, giữ cường độ analog.
- Player movement dùng `Transform.Translate(..., Space.World)` theo xác nhận mới nhất; không yêu cầu CharacterController và không tự thay component trên prefab/scene. Chưa có collision sweep, sliding hoặc gravity; việc gắn collider không tự làm Translate bị chặn bởi tường.
- Player không điều khiển movement/fire khi pause hoặc chết; death phải có đường gọi sang lose của `GamePlayController`.
- Giữ callback `OnMove` đang có; bổ sung callback aim và các method điều khiển cần thiết để user tự nối. Xóa input tồn dư khi disable, mất focus hoặc kết thúc run.
- `PlayerHealth` đổi thành `PlayerStats`; `ZombieHealth` đổi thành `ZombieStats`. Đây là các **component runtime**, không phải data asset.
- Config ScriptableObject chỉ cung cấp base values. Current health, attack cooldown, temporary state và event subscribers thuộc từng instance; không ghi chúng vào shared asset.
- Giữ `DamageInfo`, `IDamageable`, các guard damage không hợp lệ, health clamping và event nullable. Death phát một lần cho mỗi life; reset phục hồi đúng base stats của instance.

## 5. Starting weapon, pickup và combat

- Player có reference starting weapon tùy chọn trong Inspector; không hardcode Rifle. Để trống là trạng thái không có súng, không phải lỗi bắt buộc gán Rifle.
- Trạng thái không có súng vẫn cho phép movement/aim; yêu cầu fire bị bỏ qua an toàn.
- Weapon pickup là một nguồn nhận weapon khác; equip luôn đi qua một đường duy nhất trên player. Không có nhiều weapon cùng nhận lệnh fire.
- Giữ yêu cầu switch giữa những súng đã có của MVP: danh sách weapon đã nhận thuộc runtime player; pickup mới thêm weapon chưa sở hữu và trang bị nó. Pickup cùng loại không thêm slot trùng; không tự thêm ammo hoặc drop súng cũ.
- Cung cấp API pickup/equip/switch cho bước wiring sau; không tạo pickup prefab hay UI.
- Pickup đã thu thập subscribe `PlayerController.RunReset` để hiện lại khi reset loadout của run mới; không để restart làm mất nguồn súng khi starting weapon để trống. Hủy subscription khi pickup được reset hoặc destroy.
- Pistol/Rifle/Shotgun dùng chung hợp đồng fire. Giữ hitscan baseline trong plan hiện có; Shotgun tạo nhiều tia theo spread, không nhân ba hệ thống owner/cooldown.
- Player yêu cầu fire khi aim hợp lệ; weapon sở hữu cooldown và trả kết quả có bắn thật hay không. Bỏ autonomous firing loop không kiểm tra aim trong `WeaponController.Update()` hiện tại.
- Đơn vị cấu hình rõ ràng: `fireInterval` là giây giữa các phát để phù hợp giá trị `0.5` trong code user. Không đổi ngầm `0.5` thành shots/second; khi rename field phải giữ dữ liệu serialized cũ.
- Thả/kéo aim lại không reset cooldown; đổi súng không được dùng để bypass cooldown còn lại của súng đó. Reset cooldown là thao tác lifecycle rõ ràng lúc bắt đầu life/run, không phải mỗi frame.
- Raycast bỏ qua owner/trigger, dừng ở collider cản đầu tiên, tìm `IDamageable` ở parent của hitbox. Giữ kiểm tra từ origin đến muzzle để tránh nòng xuyên tường bắn trúng phía sau.
- Hit và shot có thể phát event cho presentation về sau, nhưng animation/effect không quyết định damage.
- Giữ hỗ trợ knockback script của baseline: `ZombieStats.Damaged` chuyển payload sang `ZombieController.ApplyKnockback`. `knockbackForce` là vận tốc đẩy ban đầu (world units/second), giảm dần bằng deceleration và di chuyển qua NavMeshAgent; các pellet refresh vận tốc, không cộng dồn cùng một phát. Không tạo Rigidbody, animation hoặc VFX.

## 6. Zombie, pooling và performance có giới hạn

- Spawner truyền target và cấu hình vào zombie khi spawn; không để mỗi zombie tự tìm tagged Player trong `Start()`.
- Giữ NavMeshAgent chase của baseline MVP để đi quanh chướng ngại vật. Không dùng `Translate(..., playerTransform)`; không triển khai pathfinder mới. NavMesh asset/bake do chủ project chuẩn bị sau.
- Khi ngoài tầm attack: chase. Trong tầm: attack theo cooldown nếu target còn sống và run đang Playing. Pause/death khóa cả di chuyển lẫn damage.
- Target null, target bị disable hoặc agent chưa đứng trên NavMesh phải được xử lý an toàn. Không gọi path API liên tục khi cấu hình chưa sẵn sàng.
- Normal/Giant dùng cùng behavior; khác base stats trong config, không copy toàn bộ controller.
- Reuse `ObjectPooling` hiện có, nhưng không thay `Instantiate` bằng `Spawn` một cách máy móc: pool hiện activate trước khi caller đặt pose/initialize.
- Đường spawn được dùng phải bảo đảm configure pose, target, stats/reset trước khi gameplay của instance bắt đầu. Cache component references trong `Awake`; không dựa vào `Start` chạy lại khi lấy từ pool.
- Spawner nhận death/despawn event để cập nhật alive count đúng một lần. Không quét tìm tất cả zombie trong `Update` để đếm số sống.
- Không coroutine chuyển wave mỗi frame, không log completion mỗi frame, không LINQ hoặc tìm object toàn scene trong hot path. Cache references và dùng counters/event thay vì polling toàn scene.
- Phần thay đổi pool chỉ giới hạn ở lifecycle cần cho gameplay này; giữ API mà các consumer khác đang dùng. Kiểm tra ranh giới assembly trước khi nối dependency.

## 7. Migration và giới hạn thay đổi

- Rename file/component stats cùng `.meta` để giữ GUID; cập nhật type references trong scripts thuộc luồng này. Kiểm tra nơi còn dùng tên cũ trước khi loại bỏ class cũ.
- Folding rules và hợp nhất controller chỉ loại bỏ phần thật sự đã được thay thế; không broad cleanup các Core/UI/Utilities khác.
- `MainStateManager`, `UIInGame`, `UISettingPopup`, `UILevelComplete` đang dùng API của `GamePlayController`. Giữ tương thích source trong phạm vi scripts; chưa thiết kế lại màn hình kết quả.
- `ZombieWar.PlayerCore.asmdef` chứa Player, Combat và Input qua asmref; Core/Enemies/Utilities hiện nằm ngoài assembly đó. Không tạo dependency ngược từ assembly này sang `Assembly-CSharp`; implementation plan phải chọn ranh giới source có thể biên dịch.
- Xóa dependency test vô tình có trong source runtime, ví dụ unused `using NUnit.Framework` của `ZombieSpawner`; việc này không yêu cầu viết hoặc chạy test.
- Tests cũ còn reference `PlayerHealth`, `PlayerControlRules` và weapon API cũ. User đã hoãn test: không tự viết/chạy hoặc xóa chúng. Nếu type rename làm chúng không còn compile được, phải nêu rõ điểm xung đột và xin phép một thay đổi tương thích tối thiểu trước khi thực hiện; không để lại lỗi rồi tuyên bố hoàn tất.
- Đổi `MonoBehaviour` thành `ScriptableObject` cần user tạo/gán data asset đúng kiểu trong Editor. Giữ `.meta` không tự chuyển component instance cũ thành data asset.

## 8. Handoff và tiêu chí kiểm tra trong phạm vi

Trước handoff scripts:

1. Review tĩnh call paths start/pause/resume/death/win/restart và nơi đăng ký/hủy event.
2. Search references tên cũ sau migration; kiểm tra filename/class, namespace/assembly và những public API hiện được gọi.
3. Kiểm tra không có hai timer hoặc hai firing loop, runtime counters không bị lưu vào config asset, spawn/death accounting không tăng/giảm hai lần.
4. Review diff chỉ trong scripts, metadata cần cho rename và tài liệu liên quan; giữ các thay đổi của user ngoài phạm vi.
5. Bàn giao full scripts qua file links và danh sách field/callback/component user cần gán sau trong Editor.

Không báo compile pass, test pass, runtime ổn định hoặc đã tối ưu performance từ static review. Những kiểm chứng đó được chủ project chủ động hoãn và phải được liệt kê rõ trong handoff.

## 9. Lý do chọn cách này

- **Tận dụng controller và flow hiện có:** giữ một session owner, ít thay wiring/API hơn việc tạo controller song song. Đổi lại phải bảo vệ các consumer UI cũ khi thay gameplay bên trong.
- **Stats runtime tách base config:** đúng yêu cầu đổi tên, hỗ trợ Normal/Giant mà không chia sẻ nhầm HP. Chưa cần generic stat/modifier framework.
- **Wave quota nối tiếp trong run có thời hạn:** tận dụng cấu trúc user đã viết, giữ survival liên tục và tách rõ quota khỏi alive cap. Không đổi game thành clear-wave để thắng.
- **Scripts trước, Editor sau:** phù hợp quyền chủ động của user; đổi lại chưa thể xác nhận collision, input wiring, NavMesh hoặc game feel trong lượt này.
