# Spesifikasi rilis fase pertama

## Keluaran
| Keadaan | Details | State |
|---|---|---|
| Lobby | In Lobby | kosong |
| Room, Joined, belum bermain | N Players Joined | Waiting |
| Peserta sedang bermain | N Players Joined | Smashing Key |
| Spectator di room/menonton | N Players Joined | Spectating |
| Joined AFK | N Players Joined | AFK · Waiting |
| Spectator AFK | N Players Joined | AFK · Spectating |

N mencakup pengguna sendiri dan SEMUA anggota room, termasuk spectator. N=1: `1 Player Joined`. Jangan tampilkan denominator, kapasitas, nomor room, nama anggota, role ID, atau host. Nama aplikasi sementara TOUCH RPC; belum keputusan branding final.
Keadaan internal loading dan results diperlukan, walau bukan fitur utama. Gunakan `Loading`/`Viewing Results` sementara bila diketahui; spectator tetap Spectating. Setelah WaitRoom, kembali Waiting/Spectating sesuai role.
Jika role belum diketahui: `In Room` atau `In Session`; jangan menebak Smashing Key. Jika roster belum lengkap, hilangkan jumlah, jangan 0.

## AFK
Definisi produk: tidak mulai bermain sebagai peserta selama 30 menit di room. Bukan deteksi input Windows.
- Masuk room: mulai timer non-playing.
- Mulai gameplay dengan role Joined terverifikasi: suspend/batalkan timer AFK.
- Akhir gameplay peserta: catat waktu selesai. Mulai interval baru saat kembali WaitRoom. Jika keluar di tengah, tangani transisi secara eksplisit.
- Spectating: waktu tetap berjalan, termasuk saat hasil dan kembali room; tidak reset.
- Switch Join/Spec, Ready, host transfer, slot changes, chat, anggota lain: tidak reset.
- Keluar lobby: hapus AFK room. Masuk room baru: interval baru.
- AFK hilang saat mulai gameplay peserta atau keluar room; menekan Join saja tidak menghilangkannya.
- Runtime gunakan monotonic clock. Replay gunakan clock yang bisa diinjeksi. Unknown role jangan diperlakukan sebagai peserta; tampilkan diagnostik jika AFK tidak dapat ditentukan.

## Pengaturan dan UX
Deteksi otomatis dari proses TouchClient.exe → folder executable → TouchClient_Data/output_log.txt. Path D:/Touch hanya contoh, bukan konstanta. Gunakan fallback Windows limited process query bila `MainModule` ditolak.
Tidak ada input path, Application ID, asset key, Role ID, nickname, atau role calibration pada rilis pengguna. Public Discord project configuration dibundle; secret/token tidak digunakan.
Aplikasi startup tanpa dashboard dan berada di notification area. Klik kiri membuka popup sakelar Enable/Disable; klik kanan menyediakan Exit. Disabled menghentikan monitoring dan membersihkan presence.
Installer per-user tidak wajib admin, menjalankan aplikasi setelah install, dan mendaftarkan startup saat login Windows. Config pengguna di LocalAppData hanya menyimpan preferensi Enabled/Disabled.
Jika beberapa client ditemukan, jangan mencampur atau memilih log secara arbitrer. Status ringkas tersedia melalui tooltip ikon tray.

## Identitas nickname dan tooltip aset
- `LocalNickname` adalah nickname karakter pemilik client, bukan nama anggota room pertama, `ElsePlayerName` terakhir, launcher userid, atau nilai hard-coded.
- Deteksi otomatis hanya sah jika nickname dapat dipetakan eksplisit ke `LocalRoleId` yang terverifikasi. Kedekatan baris log bukan bukti.
- Urutan nilai: otomatis terverifikasi → tooltip umum `TOUCH`.
- Preview menampilkan nickname, sumber, kandidat tooltip dan diagnostic. Nilai tidak mengubah Details, State, roster atau AFK.
- Nickname otomatis dibersihkan pada session client baru atau pergantian identitas. Keluar room tidak menghapus identitas karakter.
- Unicode dan kapitalisasi dipertahankan. Kandidat `assets.large_text` P4 disiapkan dalam batas 2–128 text elements; nilai terlalu pendek memakai `TOUCH`, nilai panjang dipotong aman.
- P3 hanya menyiapkan data/preview. Hover logo Discord dan pengiriman `LargeText` baru diuji pada P4.

## Discord Rich Presence
- Public Application ID dan large image asset key proyek dibundle untuk distribusi siap pakai. Client Secret, user token, bot token dan OAuth login tidak digunakan.
- Payload hanya mengirim Details, State, large image asset key dan kandidat nickname tooltip sebagai `assets.large_text`. Sampai sumber nickname otomatis terverifikasi tersedia, tooltip adalah `TOUCH`.
- Jangan kirim role ID, launcher userid, daftar/nama anggota, raw log, chat, signature/login field, party capacity fiktif atau join/spectate secret.
- Payload identik tidak dikirim ulang. Ketika Discord reconnect, kirim latest state saja dan jangan replay backlog.
- Stop monitoring, game exit dan app exit membersihkan Rich Presence.

## Non-goals rilis ini
Mode (ditunda pascarilis), judul musik/artis, difficulty, durasi/countdown, nomor room, kapasitas/lock slots, tombol join/spectate Discord, overlay, distribusi log, telemetry cloud.
