# TOUCH RPC
Tanggal keputusan: 15 Oktober 2026. Repository ini berada pada fase P1:
Core parser dan console replay offline sudah tersedia untuk fixture sintetis.

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

## Build dan test P1
```powershell
dotnet restore TouchRpc.slnx
dotnet build TouchRpc.slnx -c Release --no-restore
dotnet test TouchRpc.slnx -c Release --no-build
```

Replay fixture sintetis:
```powershell
dotnet run --project src/TouchRpc.Replay -c Release -- `
  tests/TouchRpc.Tests/Fixtures/midgame-spectator.synthetic.log `
  --role-id 10000000001
```

Console hanya menampilkan state akhir dan diagnostik terpilih. Console tidak mencetak raw line, chat, nama anggota, atau daftar role ID. Parameter `--role joined|spectator` dapat dipakai untuk kalibrasi eksplisit saat bukti role dari log tidak lengkap.

P1 masih offline. Live log reader, path detection, UI, AFK, tray dan Discord belum dibuat.

Yayyy itu step awal nya gengggg. kalo bisa yaudah
