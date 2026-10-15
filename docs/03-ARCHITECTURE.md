# Arsitektur implementasi

Baseline yang dipilih pada 15 Oktober 2026: C#/.NET 10 LTS untuk P1 dan fase berikutnya. Menurut kebijakan dukungan resmi Microsoft yang diperiksa saat P0, .NET 10 berada dalam active support sampai 14 November 2028, sedangkan .NET 8 berada dalam maintenance dan berakhir 10 November 2026. P1 menggunakan SDK 10.0.402 dan `global.json` dengan roll-forward patch saja.
Windows UI: WPF untuk settings, preview dan tray (tray adapter terisolasi). Core tidak bergantung WPF atau Discord.

Struktur yang dituju (boleh sederhanakan jumlah project sesuai fase):
- src/TouchRpc.Core: typed events, state reducer, roster, AFK, presence formatter.
- src/TouchRpc.Infrastructure: log reader, process detector, config, Discord adapter.
- src/TouchRpc.App: Windows settings/diagnostics/tray.
- tests/TouchRpc.Tests: synthetic fixtures, replay dan unit tests bermakna.
- docs: spesifikasi dan progres.

Struktur aktual P1 menyederhanakan target tersebut menjadi `TouchRpc.Core`, `TouchRpc.Replay`, dan `TouchRpc.Tests`. Infrastructure dan App belum dibuat karena live reader, Windows UI dan Discord berada pada fase berikutnya. Core tidak memiliki package runtime pihak ketiga. Test menggunakan MSTest 4.0.2 dari template SDK .NET 10.

Replay menerima path file, character role ID manual opsional, dan kalibrasi role opsional. Output dibatasi ke presence final, phase, role/evidence, roster completeness dan diagnostic code; raw line dan daftar ID tidak dicetak.

Pipeline: log tail → line assembler → parser → typed events → state reducer → presence model → preview/Discord.

State: clientSession, roomSession/internalRoomId, phase (unknown/lobby/waiting/loading/playing/results), localRole (unknown/joined/spectator), evidence/confidence, members HashSet<string>, rosterCompleteness, AFK clock. Simpan role ID sebagai string atau UInt64, jangan Int32.

## Reader
Read-only, FileShare.ReadWrite | FileShare.Delete sesuai platform. Initial replay membangun state tanpa mem-publish semua history ke Discord; publish hanya keadaan akhir yang konsisten. Tail berdasarkan byte offset dengan decoder incremental, buffer baris parsial, CRLF/UTF-8. Jangan baca ulang seluruh log tiap poll.
Tangani truncation, replacement walau file baru berukuran lebih besar, rename, file missing sementara, process exit/restart, partial writes, Unicode, file panjang dan cancel/dispose.
Pisahkan event time, read time, monotonic runtime. Jangan mengasumsikan korelasi waktu internal selalu sempurna.

## Roster
Snapshot entry mengganti daftar, bukan menggabungkan anggota room lama. Gunakan batas snapshot terverifikasi dari fixture; pending RoomEnter tidak dianggap sukses sampai balasan.
ID unik untuk upsert; AddPlayer rebuild tidak menambah duplikat. DelPlayer saja bukan bukti leave. RoomLeave_Re remote menghapus ID. Local leave menghapus seluruh room state. Game transitions tidak mengosongkan roster.
Jika snapshot/log tidak lengkap, flag unknown/partial; jangan mengklaim count pasti.

## Identitas lokal
Cari field local character yang eksplisit dan uji dengan fixture. `userid` launcher berbeda dari `roleid` karakter; jangan disamakan. Tidak boleh memilih AddPlayer pertama atau semua RoomNotify sebagai lokal.
Jika belum bisa otomatis: input Role ID manual dengan validasi, tersimpan lokal. Default kosong; contoh ID pengguna bukan default.

## Discord
Adapter interface agar Core dapat diuji tanpa Discord. Fase awal disabled bila ID belum tersedia, jangan menghalangi preview.
Satu Application ID proyek dapat dipakai distribusi umum; public ID bukan secret. Hanya basic local Rich Presence; tanpa user token, bot, OAuth login atau Client Secret.
Evaluasi library C# / binding SDK resmi berdasarkan dukungan, lisensi, maintenance dan kompatibilitas. Laporkan keputusan/versi sebelum mengunci dependency; jangan gunakan library deprecated tanpa alasan.
Set Details/State/assets; N sebagai teks, jangan pakai party capacity fiktif. Deduplicate payload, coalesce bursts, patuhi rate limits library/platform, retry bounded/backoff. Reconnect kirim latest state saja, bukan backlog.
Clear saat game exit atau stop. Pengguna harus membuka Discord desktop dan mengaktifkan activity sharing. Target visible update 2–5 detik adalah acceptance target, bukan garansi.

Referensi resmi untuk diperiksa Cline saat implementasi:
https://discord.com/developers/applications
https://docs.discord.com/developers/platform/rich-presence
https://github.com/discord/discord-api-docs/blob/main/developers/discord-social-sdk/development-guides/setting-rich-presence.mdx

Privacy: raw log tidak dicopy otomatis, tidak dikirim ke server/Discord. Diagnostic whitelist typed events; redact signature/token/password/commandline, tanpa chat dan nama anggota. Tidak menyimpan semua log pada setiap sesi.
