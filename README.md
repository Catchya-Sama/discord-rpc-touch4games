# TOUCH RPC

Pendamping Windows untuk membaca `TouchClient_Data/output_log.txt` secara lokal dan menampilkan status game sebagai Discord Rich Presence. Aplikasi tidak mengubah file TOUCH, tidak mengunggah log, dan tidak memerlukan token Discord.

Project owner: repository maintainer.

## Scope rilis pertama

- Lobby: `In Lobby`.
- Room menunggu: jumlah semua anggota room, termasuk spectator, dengan state `Waiting`.
- Peserta bermain: `Smashing Key` hanya jika role lokal Joined terverifikasi.
- Spectator: `Spectating`, termasuk selama gameplay dan setelah hasil sampai role berubah secara sah.
- AFK: 30 menit berada di room tanpa mulai bermain sebagai peserta.
- Unknown fallback jika role atau roster belum dapat dipastikan; aplikasi tidak mengarang count atau state.
- Notification-area tray, preview lokal, dan Discord Rich Presence opsional.

Mode permainan, musik, difficulty, nomor room, kapasitas slot, overlay, akun online, dan backend berada di luar scope.

## Menjalankan paket portable

1. Ekstrak seluruh isi `TouchRpc-win-x64.zip` ke folder yang dapat ditulis pengguna.
2. Jalankan `TouchRpc.App.exe`. Paket bersifat self-contained dan tidak memerlukan instalasi .NET terpisah.
3. Klik **Detect** untuk mencari `TouchClient.exe`. Jika path proses tidak tersedia, klik **Browse** dan pilih `TouchClient_Data\output_log.txt`.
4. Character Role ID boleh dikosongkan. Jika diisi, gunakan character role ID berupa digit—bukan launcher userid.
5. Gunakan role calibration hanya bila bukti role dari log tidak lengkap.
6. Isi public Discord Application ID dan optional large image asset key bila ingin mengaktifkan Discord Rich Presence. Jangan masukkan Client Secret atau token.
7. Klik **Start monitoring**.

Menutup jendela akan menyembunyikan aplikasi ke notification area bila opsi **Minimize to notification area when closing** aktif. Double-click ikon tray atau pilih **Open TOUCH RPC** untuk membuka kembali. Pilih **Exit** agar monitoring berhenti dan Rich Presence dibersihkan.

Settings disimpan di `%LocalAppData%\TouchRpc\settings.json`, bukan di repository atau folder game. Raw log hanya dibaca dengan file sharing yang sesuai dan tidak disalin.

## Tooltip nickname

Rilis pertama tidak menyediakan input nickname manual. Tooltip large image menggunakan `TOUCH` sampai ditemukan sumber log yang memetakan nickname karakter secara eksplisit ke character role ID lokal. Aplikasi tidak mengambil `ElsePlayerName`, nama room, urutan roster, launcher userid, atau nickname yang di-hard-code.

## Discord

Tanpa Application ID, status Discord tetap `Disabled` dan preview berfungsi normal. Dengan konfigurasi valid, adapter mengirim Details/State, optional large image, dan tooltip `TOUCH`; payload identik dideduplicate, latest state dipulihkan setelah reconnect, dan presence dibersihkan saat Stop, game exit yang terdeteksi, atau app Exit.

Discord desktop harus berjalan, pengguna harus login, dan activity sharing harus aktif. Asset key harus sama persis dengan Rich Presence asset pada Discord Application tersebut.

## Troubleshooting

- **TouchClient tidak ditemukan:** gunakan Browse; akses path proses dapat ditolak Windows dan bukan berarti log rusak.
- **Log missing/unreadable:** pastikan memilih `output_log.txt`, game sedang berjalan, dan akun Windows memiliki izin baca.
- **Discord Disabled:** isi Application ID publik yang hanya berisi digit.
- **Discord tidak menampilkan activity:** buka Discord desktop, cek activity sharing, lalu restart monitoring.
- **Large image tidak muncul:** pastikan asset sudah tersedia pada Discord Application dan asset key cocok persis.
- **Role/roster Unknown:** jangan paksa asumsi; gunakan kalibrasi role hanya jika benar-benar diketahui dan tunggu rangkaian room lengkap.
- **Close tidak keluar:** aplikasi berada di tray; gunakan menu **Exit** pada ikon notification area.

Kasus spectator saat gameplay harus tetap `Spectating`. Jika terlihat `Smashing Key`, catat hanya Phase, Role, Roster, State, dan Diagnostic—jangan kirim raw log atau settings privat.

## Build dan test

Project menargetkan .NET 10 LTS dan dikunci ke SDK 10.0.402 melalui `global.json`.

```powershell
dotnet restore TouchRpc.slnx
dotnet format TouchRpc.slnx --verify-no-changes --no-restore
dotnet build TouchRpc.slnx -c Release --no-restore
dotnet test TouchRpc.slnx -c Release --no-build
```

Jika SDK user-local digunakan:

```powershell
$dotnet = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"
& $dotnet build TouchRpc.slnx -c Release
& $dotnet test TouchRpc.slnx -c Release --no-build
```

Replay fixture sintetis:

```powershell
dotnet run --project src/TouchRpc.Replay -c Release -- `
  tests/TouchRpc.Tests/Fixtures/midgame-spectator.synthetic.log `
  --role-id 10000000001
```

Membuat paket portable lokal:

```powershell
.\scripts\Publish-Release.ps1
```

Output berada di `artifacts\TouchRpc-win-x64\` dan `artifacts\TouchRpc-win-x64.zip`; keduanya diabaikan Git. GitHub Actions menjalankan build/test/publish yang sama pada runner Windows dan mengunggah ZIP sebagai workflow artifact. Script tidak membuat atau menerbitkan GitHub Release.

## Status rilis dan lisensi

Source P5 menyiapkan paket portable, tetapi clean-machine install dan seluruh matriks live Discord belum selesai. Publikasi GitHub Release/installer merupakan keputusan terpisah.

Pemilik belum memilih lisensi project, sehingga repository belum memiliki `LICENSE`. Dependency langsung dan lisensinya dicatat di `THIRD-PARTY-NOTICES.md`.