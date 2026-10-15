# Acceptance dan pemeriksaan
Gunakan fixture sintetis; dokumentasi log adalah hasil riset, bukan oracle universal.

| Kasus | Hasil |
|---|---|
| Snapshot tiga role ID unik | 3 Players Joined |
| AddPlayer ID sama berulang | count tetap |
| Del/Add ID sama saat role switch | count tetap |
| Add/Del tanpa ID untuk slot | count tetap |
| RoomLeave_Re remote | hapus anggota tepat satu kali |
| RoomLeave_Re local, lalu room baru | roster dan role lama hilang |
| Masuk room pending | infer Joined hanya setelah entry lengkap |
| RoomDetail tanpa RoomEnter marker | tetap membuka entry dan infer role saat world dikenal |
| Masuk room tengah permainan | Spectator, jangan Smashing Key |
| current world + last world satu baris | hanya current world menentukan phase |
| Spec → results → WaitRoom | role tetap Spec, AFK clock tidak reset |
| Ready/CancelReady | tidak mengubah role |
| Host transfer/oper=1 tanpa SwitchRoleType | tidak toggle role |
| SwitchRoleType tanpa balasan | jangan langsung toggle; unknown/diagnostic |
| SwitchRoleType tanpa configured role ID | pelajari ID dari konfirmasi pertama, lalu cocokkan switch berikutnya |
| Local gameplay Joined | Smashing Key, AFK suspend |
| Spectator menerima StartPlayedTime | tetap Spec dan timer berjalan |
| Count tak diketahui | tampilkan In Room, bukan nol |
| 1 anggota | 1 Player Joined |
| AFK 29:59/30:00 | berubah tepat batas |
| Chat/remote activity/slot changes | tidak reset AFK |
| Tail baris terpotong/Unicode | tidak rusak, tidak proses dua kali |
| File truncate/replaced/restart client | sesi baru, tidak status lama |
| Discord reconnect | publish state terbaru saja |
| Client exit/stop monitoring | clear RPC |
| isuserself=True/False | hanya True menetapkan character role ID lokal |
| ElsePlayerName/pemain lain | tidak pernah mengganti nickname lokal |
| Nickname otomatis/manual/fallback | verified auto → manual cocok ID → TOUCH |
| Account/session berubah | otomatis lama dibersihkan; manual mismatch tidak dipakai |
| Unicode/panjang tooltip | kapitalisasi tetap, grapheme aman, kandidat 2–128 |
| Perubahan nickname | count, state dan anchor AFK tidak berubah |

Offline tests: Core reducer/parser/clock dan reader integration dengan file sementara. Windows integration untuk process detection/UI/named-pipe/SDK. Live tests membutuhkan pengguna; tidak boleh ditandai pass dari fixture saja.
Catat waktu event diketahui + waktu preview + waktu terlihat Discord untuk beberapa transisi; jangan menganggap pecahan milidetik read minus timestamp client integer sebagai latency presisi.

## Protokol uji pengguna
Cline beri daftar satu fase, langkah, expected result, dan cara mengirim hasil diagnostik teredaksi. Pengguna dapat menyediakan log asli secara privat untuk investigasi, jangan commit.

## Pemeriksaan P1
Jalankan dari root repository dengan SDK yang dipilih `global.json`:

```powershell
dotnet restore TouchRpc.slnx
dotnet build TouchRpc.slnx -c Release --no-restore
dotnet test TouchRpc.slnx -c Release --no-build
```

Fixture yang boleh masuk Git harus berada di `tests/TouchRpc.Tests/Fixtures` dan berakhiran `.synthetic.log`. Semua ID dan urutan di fixture tersebut sintetis. Aturan umum `*.log` tetap mengabaikan log lain.

P1 menguji parser pola terdokumentasi, pending entry, midgame spectator, role switch yang dikonfirmasi, duplikasi roster, remote/local leave, fallback unknown, singular/plural dan replay deterministik. Hasil fixture tidak dianggap live verification terhadap game.

## Pemeriksaan P2
Automated integration tests memakai file sementara dan menguji:
- initial replay lalu append dibaca exactly once;
- partial CRLF dan UTF-8 multibyte tidak diproses sebelum lengkap;
- truncate memulai session baru dan membuang partial/state lama;
- replacement dideteksi walaupun file baru lebih besar;
- missing file menjadi source health, bukan exception keluar;
- cancellation token menghentikan read secara kooperatif;
- file biner ditolak, sedangkan text log kosong valid sebagai waiting;
- settings LocalAppData round-trip dan role ID invalid dibuang;
- path log diturunkan dari executable directory tanpa konstanta instalasi;
- game closed membersihkan monitoring snapshot.

Smoke test lokal: build Release, jalankan `TouchRpc.App.exe`, pastikan proses tetap hidup setidaknya beberapa detik, lalu tutup. Ini hanya membuktikan startup WPF, bukan live game acceptance.

### Protokol live P2 pengguna
1. Jalankan `dotnet run --project src/TouchRpc.App -c Release` (atau executable SDK user-local seperti di README).
2. Dengan TOUCH di lobby, pilih/deteksi log dan Start; expected `In Lobby` dengan State kosong.
3. Masuk room normal; expected count semua anggota termasuk spectator dan State `Waiting` setelah entry lengkap.
4. Mulai bermain sebagai Joined; expected `Smashing Key`.
5. Masuk atau switch Spectator; expected `Spectating`, termasuk saat gameplay/results/kembali waiting.
6. Keluar dari TOUCH; bila proses dipilih, expected source `GameClosed` dan preview state dibersihkan.
7. Buka kembali TOUCH; jika hanya satu client, expected monitoring reconnect sebagai session baru. Jika beberapa client, pilih eksplisit.
8. Ulangi dengan instalasi non-default atau Browse. Jangan kirim raw log; kirim teks health/phase/role evidence/roster/diagnostic saja.

Live hasil di atas tetap pending sampai dikonfirmasi pengguna pada game sebenarnya.

## Pemeriksaan P3
Clock test deterministik menguji:
- perubahan tepat pada 29:59/30:00;
- `AFK · Waiting` dan `AFK · Spectating` sesuai role;
- Joined gameplay/PlayRoom membatalkan AFK dan WaitRoom memulai interval baru;
- spectator tetap menghitung melalui gameplay, results dan kembali waiting;
- role switch, ready, host/slot operation dan perubahan roster tidak mereset anchor;
- local leave dan room baru membuat interval baru;
- pending room tidak mengarang timer;
- room terkonfirmasi dengan role unknown menghitung waktu tetapi tidak mengklaim AFK;
- historical replay dimulai dari monitoring dengan diagnostic eksplisit;
- truncate/rewrite same-length membuat session dan timer baru;
- threshold non-positive ditolak di Core atau dinormalisasi settings ke 30 menit.

### Protokol live P3 pengguna
1. Jalankan preview dan pilih log seperti P2. Pastikan baris AFK menunjukkan `Counting`, origin, elapsed, dan threshold `30m` setelah room terkonfirmasi.
2. Sebagai Joined, mulai gameplay. Expected: AFK menjadi `Not tracking` dan state `Smashing Key`.
3. Kembali ke WaitRoom. Expected: timer dimulai lagi dari sekitar `00:00`.
4. Sebagai Spectator, biarkan transisi gameplay → results → WaitRoom. Expected: elapsed tidak kembali ke nol.
5. Lakukan ready/cancel, switch Join/Spec, perubahan slot atau aktivitas room. Expected: elapsed tidak reset.
6. Untuk boundary produk 30 menit, expected state berubah tepat menjadi `AFK · Waiting` atau `AFK · Spectating`. Jangan mengubah clock sistem; runtime monotonic tidak bergantung jam dinding.
7. Tutup/restart TOUCH atau ganti file session. Expected: timer lama hilang dan session baru dimulai tanpa membawa AFK lama.
8. Kirim hanya Source Health, Phase, Role, Roster, AFK dan Diagnostic; jangan kirim raw log.

Hasil acceptance 15 Oktober 2026: pengguna mengonfirmasi phase/status, role, roster/count dan nickname manual pada alur live yang dicoba berfungsi baik. Boundary AFK 30 menit tidak dijalankan live karena durasinya; acceptance AFK memakai automated clock tests, sementara status boundary live tetap `not run`, bukan `pass`. Automated tests memakai threshold pendek hanya untuk mempercepat test dan selalu diberi label test.

## Pemeriksaan nickname P3
Gunakan fixture sintetis; raw log dan nickname pengguna tidak masuk repository. Test mencakup self identity eksplisit, penolakan `ElsePlayerName`, precedence otomatis/manual/fallback, binding manual, pergantian account/session, keluar room, Unicode, batas tooltip dan regresi status/roster/AFK. Log live saat ini hanya membuktikan self role ID, belum nickname otomatis. Pada P4, verifikasi `assets.large_text` dengan hover logo Discord; jangan tandai pass dari preview P3.

## Pemeriksaan P4
Automated tests memakai fake transport dan menguji:
- Application ID kosong tidak membuat transport dan tidak menghalangi preview;
- payload identik dikirim satu kali;
- perubahan cepat sebelum connect hanya menerbitkan latest state;
- disconnect/reconnect menerbitkan latest state tanpa backlog;
- clear menghapus pending/latest state dan mencegah republish setelah reconnect;
- Details, State, asset key dan kandidat nickname tooltip dipetakan tanpa role ID/roster list/raw log;
- status Connected/Disconnected/Error terpisah dari source health;
- initial connection failure tetap dapat pulih pada reconnect;
- settings Discord dinormalisasi dan settings lama tetap dapat dimuat.

### Prasyarat live P4
1. Buat/pilih Discord Application di Developer Portal.
2. Salin public Application ID ke UI. Jangan masukkan Client Secret atau token.
3. Optional untuk pengujian tooltip: upload satu Rich Presence art asset dan masukkan asset key persis ke kolom Large image asset key.
4. Gunakan Discord desktop, login, dan pastikan activity status/sharing tidak dinonaktifkan.

### Protokol live P4 pengguna
1. Urutan startup A: buka Discord desktop terlebih dahulu, lalu preview TOUCH RPC. Isi Application ID/asset key, pilih log dan Start monitoring. Expected: status `Connected`, activity muncul, Details/State sama dengan preview.
2. Hover large image. Expected: tooltip memakai nickname manual/verified; fallback adalah `TOUCH`. Jika asset key kosong atau belum tersedia di application, large image/hover memang tidak tampil.
3. Uji Lobby → Waiting → Smashing Key → Spectating/AFK yang tersedia. Catat waktu preview berubah dan waktu Discord terlihat berubah; target observasi 2–5 detik.
4. Tutup Discord saat monitoring. Expected: status menjadi Disconnected. Ubah state game bila memungkinkan, buka Discord kembali, dan pastikan hanya state terbaru yang muncul.
5. Urutan startup B: jalankan preview dan Start monitoring ketika Discord tertutup, lalu buka Discord. Expected: reconnect dan latest state saja yang muncul.
6. Klik Stop. Expected: Rich Presence dibersihkan. Start kembali harus menerbitkan state terbaru, bukan state sebelum Stop.
7. Bila monitoring mengikuti proses, tutup TOUCH. Expected: source `GameClosed` dan Rich Presence dibersihkan. Buka TOUCH kembali; expected session baru dan latest presence setelah log tersedia.
8. Kirim hasil status/Details/State/tooltip dan waktu observasi saja. Jangan kirim settings.json, raw log, Application Client Secret atau token.

Live Discord IPC, hover asset, restart Discord dan target 2–5 detik tetap pending sampai diuji dengan Application ID/aset proyek yang valid.
