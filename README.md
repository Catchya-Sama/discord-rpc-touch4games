# TOUCH RPC
Tanggal keputusan: 15 Oktober 2026. Repository ini masih berada pada fase P0
(bootstrap repository dan dokumentasi); belum ada aplikasi yang dapat dijalankan.

Pemilik produk: Sultan Akbar. Arsitek: ChatGPT. Implementasi: Cline.
Target: aplikasi pendamping Windows, C# .NET, sumber data log lokal TOUCH, keluaran Discord Rich Presence.

## Scope rilis pertama
- Lobby: `In Lobby`.
- Room menunggu: jumlah semua anggota room, termasuk spectator, dengan state `Waiting`.
- Peserta bermain: state `Smashing Key` hanya jika role lokal Joined terverifikasi.
- Spectator: state `Spectating`, termasuk selama gameplay dan setelah hasil sampai role berubah secara sah.
- AFK: 30 menit berada di room tanpa mulai bermain sebagai peserta; aktivitas lain bukan reset.
- Jika role atau roster belum dapat dipastikan, tampilkan unknown/fallback dan jangan mengarang count atau state.

Mode permainan, musik, nomor room dan kapasitas slot berada di luar scope rilis ini.

## Mulai
1. Clone repository dan buka foldernya di VS Code pada Windows.
2. Baca `AGENTS.md` dan docs sesuai urutan di bawah.
3. Gunakan `CLINE-START-HERE.md` untuk memulai sesi implementasi baru bila diperlukan.
4. Kerjakan satu fase, uji, review diff, commit, push, lalu catat progres. Jangan langsung membangun semua fase.

## Urutan baca
AGENTS.md → docs/01-SPEC.md → docs/02-LOG-EVIDENCE.md → docs/03-ARCHITECTURE.md → docs/04-PHASES.md → docs/05-GITHUB.md → docs/06-TESTING.md → docs/PROGRESS.md.

## Input yang belum tersedia
- Pilihan lisensi proyek; jangan menambahkan lisensi tanpa keputusan pemilik.
- Discord Application ID, nama aplikasi final, gambar dan asset key.
- Deteksi role ID lokal untuk pengguna umum belum tervalidasi.

Remote proyek terverifikasi sebagai `https://github.com/Catchya-Sama/discord-rpc-touch4games.git`.
Discord ID bukan prasyarat P0–P3 dan tidak boleh dikarang.
Raw log tidak disertakan: berpotensi berisi signature, password room, identitas dan chat. Pengguna menyediakan log lokal untuk pengujian; gunakan fixture sintetis di Git.

## Toolchain
Target implementasi P1 dan seterusnya adalah .NET 10 LTS. Mesin bootstrap saat ini hanya memiliki SDK 8.0.408, sehingga SDK .NET 10 harus dipasang sebelum project aplikasi dibuat. P0 sendiri hanya mengubah dokumentasi dan tidak membutuhkan build.
