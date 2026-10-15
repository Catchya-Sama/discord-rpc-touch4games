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
| Masuk room tengah permainan | Spectator, jangan Smashing Key |
| Spec → results → WaitRoom | role tetap Spec, AFK clock tidak reset |
| Ready/CancelReady | tidak mengubah role |
| Host transfer/oper=1 tanpa SwitchRoleType | tidak toggle role |
| SwitchRoleType tanpa balasan | jangan langsung toggle; unknown/diagnostic |
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
