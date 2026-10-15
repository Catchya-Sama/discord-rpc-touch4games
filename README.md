# TOUCH RPC
Tanggal keputusan: 15 Oktober 2026. Implementasi lokal P4 tersedia:
Core parser, live log reader, path detection/fallback Browse, lifecycle AFK monotonic, settings lokal, preview Windows dan Discord Rich Presence adapter. Verifikasi live Discord masih pending Application ID/aset proyek.

Project Owner: Bongbong (xooty).
Target: Software pendamping Windows, C# .NET, sumber data log local file TOUCH, keluaran Discord Rich Presence.

## Scope rilis pertama
- Lobby: `In Lobby`.
- Room menunggu: jumlah semua anggota room, termasuk spectator, dengan state `Waiting`.
- Peserta bermain: state `Smashing Key` hanya jika role lokal Joined terverifikasi.
- Spectator: state `Spectating`, termasuk selama gameplay dan setelah hasil sampai role berubah secara sah.
- AFK: 30 menit berada di room tanpa mulai bermain sebagai peserta; aktivitas lain bukan reset.
- Jika role atau roster belum dapat dipastikan, tampilkan unknown/fallback dan jangan mengarang count atau state.

Mode permainan, musik, nomor room dan kapasitas slot berada di luar scope rilis ini.


## Step Progress nya
AGENTS.md → docs/01-SPEC.md → docs/02-LOG-EVIDENCE.md → docs/03-ARCHITECTURE.md → docs/04-PHASES.md → docs/05-GITHUB.md → docs/06-TESTING.md → docs/PROGRESS.md.

## Input yang belum tersedia
- Pilihan license project ; ga boleh menambahkan license tanpa keputusan owner.
- Discord Application ID, nama software final, image dan asset key untuk live verification. Nilai tidak di-hard-code; masukkan melalui settings lokal.
- Deteksi role ID local untuk pengguna belum ada .


## Toolchain
Project menargetkan .NET 10 LTS dan dikunci ke SDK 10.0.402 melalui `global.json`.

## Build dan test
```powershell
dotnet restore TouchRpc.slnx
dotnet build TouchRpc.slnx -c Release --no-restore
dotnet test TouchRpc.slnx -c Release --no-build
```

Pada mesin bootstrap ini SDK 10.0.402 dipasang user-local tanpa mengubah PATH. Jika `dotnet --version` tidak menemukan SDK dari `global.json`, gunakan:
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

Console hanya menampilkan state akhir dan diagnostik terpilih. Console tidak mencetak raw line, chat, nama anggota, atau daftar role ID. Parameter `--role joined|spectator` dapat dipakai untuk kalibrasi eksplisit saat bukti role dari log tidak lengkap.

Console replay tetap tersedia untuk diagnosis offline. History log tidak dipakai untuk mengarang durasi AFK; timer replay dimulai dari monitoring dengan diagnostic eksplisit.

## Menjalankan preview Windows P4
```powershell
& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" run --project src/TouchRpc.App -c Release
```

Alur penggunaan:
1. Klik **Detect**. Jika path proses dapat dibaca, aplikasi membentuk path `TouchClient_Data/output_log.txt` dari folder executable.
2. Jika process path tidak tersedia atau lokasi berbeda, klik **Browse** dan pilih `output_log.txt` secara eksplisit.
3. Isi Character Role ID hanya bila diperlukan; launcher userid bukan character roleid. Biarkan kosong jika belum diketahui.
4. Gunakan role calibration hanya jika bukti role dari log tidak lengkap.
5. Klik **Start monitoring** dan periksa Source Health serta Presence Preview.
6. Untuk Discord, isi public Application ID dan optional large image asset key. Jangan masukkan Client Secret atau token.

AFK berarti 30 menit berada di room tanpa mulai bermain sebagai peserta Joined. Ini bukan deteksi input Windows. Spectator tetap menghitung selama gameplay/results; ready, chat, aktivitas anggota lain, host transfer, slot changes, dan role switch tidak mereset timer. Joined yang mulai bermain membatalkan AFK dan interval baru dimulai saat kembali ke WaitRoom.

Preview menampilkan elapsed AFK, origin timer, dan diagnostic. Jika monitoring dimulai dari log yang sudah berisi history room, timer dimulai saat monitoring karena durasi monotonic sebelumnya tidak tersedia. Threshold pendek hanya digunakan di automated tests dan bukan default runtime.

Settings disimpan di `%LocalAppData%\TouchRpc\settings.json`, bukan di repository atau folder game. Raw log hanya dibaca dan tidak disalin.

Tanpa Application ID, status Discord tetap `Disabled` dan preview berfungsi normal. Dengan konfigurasi valid, adapter mengirim Details/State serta large-image tooltip nickname, melakukan deduplication, memulihkan latest state setelah reconnect, dan clear saat Stop/game exit. Tray dan packaging tetap belum termasuk P4.

Yayyy itu step awal nya gengggg. kalo bisa yaudah
