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
- Notification-area tray dan Discord Rich Presence siap pakai tanpa konfigurasi manual.

Mode permainan, musik, difficulty, nomor room, kapasitas slot, overlay, akun online, dan backend berada di luar scope.

## Instalasi

1. Jalankan `TouchRpc-Setup-win-x64.msi`.
2. Installer memasang aplikasi untuk pengguna Windows saat ini, menjalankannya di notification area, dan mengaktifkan startup saat login Windows.
3. Tidak ada path log, Application ID, asset key, Role ID, nickname, atau role calibration yang perlu diisi.
4. Jalankan TOUCH dan Discord desktop seperti biasa. TOUCH RPC otomatis menunggu `TouchClient.exe`, menemukan `TouchClient_Data\output_log.txt`, dan mengirim state terbaru.

Klik kiri ikon tray untuk membuka popup sakelar. **Enabled** berarti aplikasi otomatis menunggu dan memonitor TOUCH; **Disabled** menghentikan monitoring dan membersihkan Rich Presence. Klik kanan ikon lalu pilih **Exit** untuk menutup aplikasi sepenuhnya. Pilihan Enabled/Disabled diingat untuk startup berikutnya.

Installer bersifat per-user dan tidak memerlukan admin. Program dipasang di `%LocalAppData%\TOUCH RPC`; startup login menggunakan entri pengguna Windows saat ini. Settings hanya menyimpan preferensi Enabled/Disabled di `%LocalAppData%\TouchRpc\settings.json`. Raw log hanya dibaca dengan file sharing yang sesuai dan tidak disalin.

Paket portable tetap tersedia untuk diagnosis pengembang: ekstrak `TouchRpc-win-x64.zip` dan jalankan `TouchRpc.App.exe`. Portable tidak membuat startup login otomatis.

## Tooltip nickname

Rilis pertama tidak menyediakan input nickname manual. Tooltip large image menggunakan `TOUCH` sampai ditemukan sumber log yang memetakan nickname karakter secara eksplisit ke character role ID lokal. Aplikasi tidak mengambil `ElsePlayerName`, nama room, urutan roster, launcher userid, atau nickname yang di-hard-code.

## Discord

Public Application ID dan large image asset key proyek dibundle dalam aplikasi; pengguna tidak memasukkan Client Secret atau token. Adapter mengirim Details/State, large image, dan tooltip `TOUCH`; payload identik dideduplicate, latest state dipulihkan setelah reconnect, dan presence dibersihkan saat Disable, game exit yang terdeteksi, atau app Exit.

Discord desktop harus berjalan, pengguna harus login, dan activity sharing harus aktif. Asset key harus sama persis dengan Rich Presence asset pada Discord Application tersebut.

## Troubleshooting

- **Ikon tidak terlihat:** buka overflow notification area Windows dan pin TOUCH RPC bila diinginkan.
- **TOUCH tidak terdeteksi:** pastikan `TouchClient.exe` berjalan. Aplikasi memakai path executable proses dan tidak mengandalkan lokasi instalasi tetap.
- **Discord tidak menampilkan activity:** pastikan Discord desktop berjalan, pengguna login, activity sharing aktif, dan sakelar TOUCH RPC Enabled.
- **Lebih dari satu TOUCH client:** tutup instance tambahan; aplikasi tidak memilih log secara arbitrer.
- **Role/roster Unknown:** tunggu rangkaian room lengkap; aplikasi tidak mengarang count atau role.
- **Aplikasi tidak keluar saat popup ditutup:** popup memang hanya disembunyikan; klik kanan ikon tray lalu pilih **Exit**.

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

Membuat paket portable dan installer lokal:

```powershell
.\scripts\Publish-Release.ps1
.\scripts\Build-Installer.ps1
```

Output portable berada di `artifacts\TouchRpc-win-x64\` dan `artifacts\TouchRpc-win-x64.zip`. Installer berada di `artifacts\installer\TouchRpc-Setup-win-x64.msi`. Seluruh output diabaikan Git. GitHub Actions menjalankan build/test/publish/installer pada runner Windows dan mengunggah ZIP serta MSI sebagai workflow artifacts. Script tidak membuat atau menerbitkan GitHub Release.

## Status rilis dan lisensi

Source menyiapkan paket portable dan installer per-user. Publikasi GitHub Release tetap merupakan keputusan terpisah; installer belum ditandatangani dan clean-machine acceptance tetap memerlukan pengujian pengguna.

Pemilik belum memilih lisensi project, sehingga repository belum memiliki `LICENSE`. Dependency langsung dan lisensinya dicatat di `THIRD-PARTY-NOTICES.md`.