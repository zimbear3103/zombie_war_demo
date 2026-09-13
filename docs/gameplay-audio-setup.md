# Gameplay SFX: manual setup

Đã thêm code cho weapon fire/reload/equip, zombie growl/attack/hurt/death và bomb explosion. Chưa sửa prefab, scene, ScriptableObject asset hoặc audio import settings; chưa compile, chạy test hay Play Mode. Làm wiring bên dưới rồi nghe thử để đánh giá volume và timing.

## 1. SoundManager và AudioListener

- Scene đang chơi cần một `SoundManager` active. Dùng instance hiện có của project; scene test chạy trực tiếp cũng cần instance này.
- `Max Gameplay Voices` mặc định **16**, chỉnh trước khi vào Play Mode. Manager tự tạo/reuse các AudioSource con lúc runtime, không cần tạo prefab audio.
- Các trường `Sfx Source`, `Music Source`, `Voice Source` hiện có vẫn phục vụ UI/music/voice cũ. Gameplay dùng source riêng và cùng setting SFX. Nếu `Sfx Source` có Output Audio Mixer Group, gameplay kế thừa group đó.
- Giữ **một AudioListener active** trong scene, thường trên camera. Sound 3D tính khoảng cách từ listener, gồm cả độ cao camera.
- `AudioManager` rỗng trong thư mục Audio không tham gia flow này.

Mỗi nhóm sound có các field chung:

| Field | Default trong code | Cách chỉnh |
|---|---|---|
| Clips | Rỗng | Kéo một hoặc nhiều AudioClip vào; chọn ngẫu nhiên mỗi lần phát, bỏ qua slot trống |
| Volume | 0.8 | Mức riêng của nhóm sound, từ 0 đến 1 |
| Pitch Range | 0.95–1.05 | Biến thiên nhẹ; reload dùng 1–1 để giữ timing |
| Min Distance | 5 | Bán kính nghe ở volume đầy đủ |
| Max Distance | 40 | Khoảng cách hết tiếng theo linear rolloff |

Gameplay source dùng Spatial Blend = 1, Doppler Level = 0 và không loop. Clip growl/chase dạng loop dài cần được thay bằng clip ngắn cho flow one-shot này. Min/Max Distance là khoảng cách world tới AudioListener; nếu camera cao làm tiếng quá nhỏ, tăng Min Distance dựa trên khoảng cách listener tới player.

## 2. Weapon

1. Thêm `WeaponAudioController` vào **root Player**, cùng object với `PlayerController`.
2. Field `Player` tự tìm component cùng object; có thể gán trực tiếp cho rõ Inspector.
3. Mở từng `WeaponScriptableObject` mà loadout/pickup đang dùng, gán `Audio > Fire Sound`, `Reload Sound`, `Equip Sound`.
4. Không cần AudioSource hoặc WeaponAudioController trên từng gun prefab.

| Sound | Volume khởi điểm để nghe thử | Pitch Range | Trigger |
|---|---|---|---|
| Fire | 0.8 | 0.97–1.03 | Sau shot thành công; shotgun một tiếng cho cả volley |
| Reload | 0.55 | 1–1 | Bắt đầu reload; dừng khi hoàn tất/cancel/đổi súng/player chết |
| Equip | 0.45 | 1–1 | Pickup hoặc đổi active weapon khi player được phép hành động |

Starting weapon khi setup/reset run không phát equip. Reload sound đi theo súng, giữ playback khi pause rồi resume; không tự lặp lại hoặc kéo dài clip. Chọn reload clip có thời lượng khớp `Reload Time`: clip dài hơn bị dừng khi gameplay reload hoàn tất, clip ngắn hơn sẽ hết tiếng sớm. Bật riêng component audio giữa lúc đang reload không phát bù từ đầu.

Các clip ứng viên đã có trong project, cần nghe và chọn theo feel của gun:

- Pistol fire: `Assets/PostApocalypseGunsDemo/Pistols/Zapper_3p_01.wav` và các variant `_02`, `_03`.
- Rifle fire: `Assets/PostApocalypseGunsDemo/AssaultRifles/AutoGun_3p_01.wav`, `_02`.
- Shotgun fire: `Assets/PostApocalypseGunsDemo/Shotguns/JackHammer_3p_01.wav`, `_02`, `_03`.
- Reload/equip: xem `Pistols/Pistol_ClipIn_05.wav` và `Shotguns/JackHammer_Reload.wav` trong cùng pack; một tiếng clip-in chỉ phù hợp cho thao tác ngắn, không đại diện đủ toàn bộ chuỗi reload.

Đây là gợi ý theo tên file, chưa xác nhận âm sắc/thời lượng bằng việc nghe clip.

## 3. Zombie

1. Thêm `ZombieAudioController` vào **root zombie**, cùng object với `ZombieController` và `ZombieStats`.
2. Gán bốn nhóm `Growl Sound`, `Attack Sound`, `Hurt Sound`, `Death Sound`.
3. Giữ `Growl Interval = (3, 6)`, `Hurt Cooldown = 0.25` lúc bắt đầu nghe thử.

| Nhóm | Volume khởi điểm để nghe thử | Clip ứng viên trong `Assets/Tybug Studios/Zombie Voice Pack - Free/` |
|---|---|---|
| Growl | 0.25 | `Zombie Growl/zombie_growl_010.wav`, `zombie_growl_023.wav` |
| Attack | 0.5 | `Zombie Aggressive/zombie_agressive_039.wav`, `zombie_agressive_044.wav` |
| Hurt | 0.45 | `Zombie Grunt/zombie_grunt_006.wav` |
| Death | 0.65 | `Zombie Death/zombie_death_004.wav`, `zombie_death_010.wav` |

- Growl chỉ chạy khi zombie đang có target hợp lệ và gameplay cho phép truy đuổi; lần đầu chờ ngẫu nhiên 3–6 giây.
- Mỗi zombie có tối đa một tiếng sống tại một thời điểm. Attack/hurt thay tiếng đang phát; growl chờ tiếng đó kết thúc.
- Hurt chỉ phát khi nhận damage còn sống, giới hạn mỗi 0.25 giây. Hit chí mạng phát death, không phát thêm hurt.
- Death phát một lần tại vị trí chết; manager giữ source độc lập nên corpse về pool hoặc respawn không cắt/di chuyển tiếng chết cũ.
- Khi gameplay bị khóa/pause, tiếng sống đang phát dừng; timer growl/hurt giữ lại. Tiếng death đang phát được manager pause/resume theo `Time.timeScale`.
- Attack hiện bám lần gây damage trong `TryAttack`, không phụ thuộc Animation Event. Frame tiếng và động tác cần nghe/nhìn cùng nhau khi polish animation sau này.

## 4. Bomb

1. Mở prefab bomb thực tế được `PlayerController` ném ra.
2. Thêm `BombAudioController` vào root có `BombController`.
3. `Bomb` tự tìm cùng object. Trên `BombController`, gán `Audio > Explosion Sound > Clips`.
4. Bắt đầu với Volume **0.9**, Pitch Range **0.95–1.05**, Min Distance **8**, Max Distance **55** rồi nghe thử so với tiếng súng.

Explosion phát đúng một lần khi `Explode()` chạy, trước FX/knockback/Destroy. Source giữ tại vị trí nổ và không làm child của bomb. Bomb bị cancel do restart/player chết không phát explosion.

Chưa gán sẵn clip explosion; cần chọn/import clip nổ bạn muốn dùng. Thay đổi này chỉ thêm audio, không thay hành vi knockback/damage của bomb.

## 5. Mute, pause và giới hạn số tiếng

- Settings SFX điều khiển cả weapon/zombie/bomb qua `SoundManager.SetMuteSFX`. Mute giữ tiến độ clip đang phát; sound mới trong lúc mute bị bỏ qua, không phát bù khi bật lại.
- Manager pause gameplay sources khi `Time.timeScale <= 0`, resume khi lớn hơn 0. Reload còn theo `PlayerController.CanAct`, nên khóa gameplay/focus cũng giữ tiếng reload.
- UI/music vẫn dùng sources riêng; pause gameplay không tự pause chúng.
- Bắt đầu run mới hoặc `GamePlayController.OnFreeData()` sẽ dừng toàn bộ gameplay sounds để không mang tiếng cũ sang run/scene tiếp theo.
- Pool ưu tiên theo thứ tự: **bomb > fire > death > reload > equip > hurt > attack > growl**. Khi đầy, sound mới thay tiếng cũ có ưu tiên thấp hơn hoặc bằng; nếu không có slot phù hợp thì bỏ sound mới. Các voice đang pause giữ slot.
- Giới hạn 16 áp dụng cho source do pool quản lý; cài đặt real/virtual voices của Unity vẫn ảnh hưởng khả năng nghe thực tế.

Manager theo dõi pause riêng vì Unity báo `AudioSource.isPlaying = false` khi source bị Pause; source đó không được coi là slot rảnh. Tham khảo [Unity AudioSource.isPlaying](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource-isPlaying.html). Thứ tự priority của AudioSource theo [Unity AudioSource.priority](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource-priority.html).

## 6. Checklist chạy thủ công sau khi wiring

Các bước sau **chưa được chạy**:

- [ ] Đợi Unity compile và kiểm tra Console trước khi Play.
- [ ] Pistol/rifle bắn từng shot và liên tục; shotgun không nhân tiếng theo pellet.
- [ ] Reload bằng UI và khi hết đạn: một tiếng bắt đầu, pause/resume giữ vị trí, đổi súng hoặc player chết dừng tiếng.
- [ ] Pickup/switch có equip; setup starting weapon và restart không có equip thừa.
- [ ] Zombie growl lệch nhịp nhau, hurt có cooldown; hit chí mạng chỉ có death.
- [ ] Zombie về pool rồi respawn: death cũ không chạy theo zombie mới, hurt/growl timer reset.
- [ ] Bomb nổ đúng một lần và tiếng kết thúc tự nhiên sau khi bomb bị destroy; bomb cancel không phát tiếng nổ.
- [ ] Pause lúc đang nghe gun tail/death/explosion rồi resume; menu click vẫn nghe được.
- [ ] Tắt/bật SFX trong Settings; restart và chuyển scene không để tiếng gameplay cũ tồn tại.
- [ ] Spawn đông zombie để nghe mức chồng tiếng và ưu tiên gun/bomb; runtime không tạo quá Max Gameplay Voices nguồn gameplay.
- [ ] Bỏ trống một nhóm clip hoặc một slot: nhóm thiếu clip im lặng, gameplay tiếp tục.

## 7. Lưu ý khi polish

- Chưa wiring component/clip thì code chưa phát tiếng. Các giá trị volume trên là điểm bắt đầu, chưa cân bằng theo độ lớn thực tế của asset.
- Pool có chủ ý cắt/bỏ tiếng khi quá tải; death/explosion được bảo vệ khỏi lifetime của object, nhưng vẫn chịu giới hạn pool và cleanup khi kết thúc scene/run.
- Clip reload lệch thời lượng dễ tạo cảm giác thao tác xong sớm hoặc bị cắt. Chỉnh clip/Reload Time cùng nhau; giữ pitch 1–1 cho reload.
- Mọi sound đều 3D; listener quá cao/xa hoặc mixer bị mute có thể khiến clip nghe nhỏ dù Volume cao.
- Chưa có kết quả compile/runtime/performance; checklist phía trên là bước xác nhận sau khi bạn setup prefab.
