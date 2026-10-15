# TOUCH RPC
Tanggal keputusan: 15 Oktober 2026. Repository ini masih berada pada fase P0
(bootstrap repository dan dokumentasi); belum ada aplikasi yang dapat dijalankan.

Project Owner: Putty Adelya Sasta.
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
Target implementation P1 dan seterusnya adalah .NET 10 LTS. Mesin bootstrap saat ini hanya memiliki SDK 8.0.408, sehingga SDK .NET 10 dipasang sebelum project aplikasi dibuat. P0 sendiri hanya mengubah dokumentasi dan tidak membutuhkan build.

Yayyy itu step awal nya gengggg. kalo bisa yaudah
