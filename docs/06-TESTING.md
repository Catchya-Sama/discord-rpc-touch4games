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

Live P3 tetap pending sampai urutan di atas dikonfirmasi pada TOUCH sebenarnya. Automated tests memakai threshold pendek hanya untuk mempercepat test dan selalu diberi label test.
