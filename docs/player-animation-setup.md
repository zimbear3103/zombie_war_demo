# Player animation: idle + combat_run và IK gun

Setup đã áp dụng: Animator chạy hai clip `idle` và `combat_run`; súng đặt cố định trước ngực, hai tay bám grip bằng Two Bone IK. Cả người và súng xoay theo aim; thả aim giữ hướng cuối. Movement vẫn dùng `Transform.Translate` trong world space.

## Animator và thông số chính

Controller: `Assets/ZombieWar/Animation/Character/CharacterAnimationController.controller`.
Chỉ một layer `Movement`, default state `Movement`, full path `Movement.Movement`. Motion là Blend Tree **1D**, parameter Float `Velocity` trong khoảng 0–1.

| Motion / field | Giá trị đã lưu |
|---|---|
| idle | Threshold 0; Speed 1; Mirror OFF; Cycle Offset 0 |
| combat_run | Threshold 1; Speed 1.6; Mirror OFF; Cycle Offset 0 |
| Speed Damp Time | 0.08 giây |
| Equip Blend Duration | 0.20 giây, SmoothStep |
| Left / Right Hand Weight | 1 / 1 |
| Apply Root Motion | OFF |
| Animator Update Mode | Normal |
| Animator Culling Mode | Always Animate |
| RigBuilder / HandsRig | Enabled; layer active; Rig weight 1 |

`Velocity` lấy từ `PlayerController.NormalizedMoveSpeed`: đứng yên = 0, chạy hết joystick = 1, giữ giá trị analog ở giữa. Aim không chặn locomotion. Khi gameplay bị khóa hoặc player chết, Velocity về 0.

Speed 1.6 được chọn cho tốc độ gameplay hiện tại **5 m/s**: lấy mẫu đoạn chân tiếp đất của clip cho vận tốc khoảng 3.08 m/s ở 1x, suy ra hệ số khoảng 1.63. Đây là hiệu chỉnh nhịp chạy tiến; không khắc phục bước chân ngang/lùi.

## Hierarchy và IK

Prefab: `Assets/ZombieWar/Prefabs/Ingame/Player.prefab`. Scene `GameplayZombie` đã đồng bộ các override target liên quan.

```text
Player [PlayerController, PlayerAnimationController, PlayerHandIKController]
  Model
    Survivalist (1) [Animator, RigBuilder]
      root/...                         skeleton
      WeaponSocket [RigTransform]      ngoài skeleton
        RightHandTarget
        LeftHandTarget
        WeaponInstance                 tạo khi equip
      HandsRig [Rig]
        RightHandIK [TwoBoneIKConstraint]
        LeftHandIK [TwoBoneIKConstraint]
        RightHandIK_hint
        LeftHandIK_hint
```

WeaponSocket là con **trực tiếp của object Animator**, không nằm dưới `hand_r`. Hai target là con trực tiếp của socket; hai hint nằm dưới HandsRig và ngoài skeleton.

| Transform | Local Position | Local Euler Angles |
|---|---|---|
| WeaponSocket | (0.14, 1.22, 0.45) | (0, 0, 0) |
| RightHandTarget | (0.06, -0.06, -0.20) | (293.74, 228.34, 28.06) |
| LeftHandTarget | (-0.12, -0.09, -0.19) | (39.62, 146.31, 347.62) |
| RightHandIK_hint | (0.42, 1.02, -0.12) | (0, 0, 0) |
| LeftHandIK_hint | (-0.38, 1.02, 0.06) | (0, 0, 0) |

Các transform trên dùng scale (1, 1, 1). Target Position Weight, Target Rotation Weight và Hint Weight đều 1. Hai Maintain Target Offset đều OFF. Constraint weight khởi đầu 0, script blend lên 1 khi có grip.

- RightHandIK: RightUpperArm → RightLowerArm → RightHand; target/hint cùng bên phải.
- LeftHandIK: LeftUpperArm → LeftLowerArm → LeftHand; target/hint cùng bên trái.
- PlayerHandIKController: references trỏ đúng PlayerController, Animator và hai constraint tương ứng.
- Reload gameplay vẫn giữ cả hai tay trên súng vì không có reload clip. Pause giữ tiến độ blend; death/disable thả IK, reset và re-enable nối lại đúng weapon.

## Grip súng và ngón tay

Cả ba data `Pistol.asset`, `Rifle.asset`, `Shotgun.asset` hiện cùng `Kind = Pistol` và cùng tham chiếu `Prefabs/Ingame/Pistol.prefab`. Các assignment này được giữ nguyên. Prefab súng dùng scale **0.2**; Hand Local Position/Euler Angles trong data đều (0, 0, 0).

| Marker trên Pistol.prefab | Local Position | Local Euler Angles |
|---|---|---|
| rightHandPos / Right Hand Grip | (0.30, -0.30, -1.00) | (293.74, 228.34, 28.06) |
| leftHandPos / Left Hand Grip | (-0.60, -0.45, -0.95) | (39.62, 146.31, 347.62) |

Grip chỉ định pose **bone cổ tay**, không phải tâm lòng bàn tay. Khi equip, script cache pose grip trong hệ tọa độ socket; target tồn tại xuyên suốt việc đổi súng. Sửa marker lúc Play cần equip lại hoặc disable/enable PlayerHandIKController. Súng một tay có thể để Left Hand Grip trống.

Hai clip dùng cùng các giá trị finger muscle để ngón tay không đổi pose khi blend idle/run. Các curve này trước đó đều bằng 0; các curve locomotion khác được giữ nguyên.

| Hand / finger | 1 / 2 / 3 Stretched |
|---|---|
| Right Middle, Ring, Little | -0.55 / -0.65 / -0.55 |
| Right Index | -0.10 / -0.25 / -0.15 |
| Left Index, Middle, Ring, Little | -0.35 / -0.45 / -0.35 |
| Hai Thumb | -0.15 / -0.10 / -0.05 |
| Mọi Spread | 0 |

## Đã kiểm tra

- Unity compile thành công. Play Mode: **115 kiểm tra pass, 0 fail** cho idle, analog movement, chạy tiến/ngang/lùi, aim các hướng và đường chéo, thả aim, reload, pause/resume, gameplay lock, death/reset và disable/re-enable IK.
- **12 kiểm tra bổ sung pass, 0 fail**: dùng data clone tạm với Kind và attachment offset khác nhau để kiểm tra đổi súng thật, đổi tiếp giữa blend và grip trái tùy chọn; không sửa data gốc.
- Sai lệch cổ tay–grip lớn nhất ở các mẫu integration là **0.000068 m**; sai lệch rotation đo được 0°. Số đo xác nhận bám grip, không thay thế đánh giá visual.
- Đã xem ảnh idle và chạy thực tế sau khi simulation tiến frame: hai IK weight 1; sai lệch cổ tay–target dưới 0.00004 m. Ảnh và kết quả chi tiết ở `Temp/PlayerAnimationValidation/` (`idle-final.png`, `run-final.png`, `run-side-final.png`, `runtime-results.txt`, `switch-results.txt`, `final-capture-results.txt`). Đây là thư mục tạm của lượt test, không phải test suite lưu trong Assets.
- Unity đã về Edit Mode, các gamepad/camera dùng test đã được dọn. Code diff check pass; YAML do Unity sinh có khoảng trắng cuối dòng, kiểm tra bỏ qua riêng loại khoảng trắng này thì pass.

## Giới hạn và side-effect

- Chỉ có clip chạy tiến: chạy ngang/lùi khi vẫn nhìn theo aim còn foot sliding và không có bước strafe/backpedal riêng.
- Không có visual clip shoot/reload/death. Gameplay bắn, đạn, reload và HP vẫn dùng logic hiện tại.
- Finger curl nằm trong hai clip nên cũng áp dụng khi unarmed; đây là pose ưu tiên cầm súng. Model súng khác cần căn lại grip và attachment offset.
- Always Animate tiếp tục evaluate Animator khi ngoài camera; setup này dành cho một player, không nên áp hàng loạt cho NPC.
- Do ba data hiện cùng Kind, gameplay hiện tại không tạo ba loại súng riêng khi nhặt chúng; lượt test đổi súng dùng bản sao tạm để kiểm tra đúng cơ chế IK.
- Unity tự ghi thêm editor curves và binding tables khi lưu clip nên diff `.anim` lớn; review đã đối chiếu toàn bộ curve locomotion với bản trước khi chỉnh.
