# TOUCH RPC
Tanggal keputusan: 15 Oktober 2026. Implementasi lokal P2 tersedia:
Core parser, live log reader, path detection/fallback Browse, settings lokal, dan preview Windows. Verifikasi live dengan game masih pending.

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
- Discord Application ID, nama software final, image dan asset key.
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

Console replay P1 tetap tersedia untuk diagnosis offline. P2 menambahkan monitoring live dan preview Windows; AFK, tray dan Discord belum dibuat.

## Menjalankan preview Windows P2
```powershell
& "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" run --project src/TouchRpc.App -c Release
```

Alur penggunaan:
1. Klik **Detect**. Jika path proses dapat dibaca, aplikasi membentuk path `TouchClient_Data/output_log.txt` dari folder executable.
2. Jika process path tidak tersedia atau lokasi berbeda, klik **Browse** dan pilih `output_log.txt` secara eksplisit.
3. Isi Character Role ID hanya bila diperlukan; launcher userid bukan character roleid. Biarkan kosong jika belum diketahui.
4. Gunakan role calibration hanya jika bukti role dari log tidak lengkap.
5. Klik **Start monitoring** dan periksa Source Health serta Presence Preview.

Settings disimpan di `%LocalAppData%\TouchRpc\settings.json`, bukan di repository atau folder game. Raw log hanya dibaca dan tidak disalin.

P2 belum mencakup AFK, tray atau Discord. Status Discord di preview sengaja tetap `Disabled (P4)`.

Yayyy itu step awal nya gengggg. kalo bisa yaudah
