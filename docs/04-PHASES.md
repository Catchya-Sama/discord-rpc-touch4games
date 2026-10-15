# Roadmap — satu fase per siklus Cline

## P0 — bootstrap dokumentasi dan repository
Periksa folder/AGENTS/SDK/git. Rapikan repo, .gitignore, README dan progress. Belum coding fitur. Jangan tentukan lisensi legal tanpa pilihan pemilik: open-source intent ada tetapi jenis lisensi belum dipilih. Repo remote hanya dari URL pengguna atau remote existing yang diverifikasi.
Exit: struktur docs terbaca, repo/branch/identitas jelas, commit bootstrap dan push jika remote tersedia.

## P1 — Core parser dan offline replay
Buat Core + console diagnostic/replay sederhana, synthetic fixtures, local role config manual bila perlu. Implement state lokasi, roster unik, role inference terbatas dan unknown fallback. Belum Discord/WPF/tray/AFK.
Exit: replay alur room pending + midgame spectator + role switch + duplicate AddPlayer + keluar memberi hasil yang benar. Output tidak membocorkan raw log.

## P2 — live log, deteksi path, preview Windows
Reader tail, detect executable path, Browse, validasi, persistence, local role config, preview state + source health. Rebuild state dari log yang lengkap. Uji restart/truncate/replace/partial lines. Game closed clear.
Exit: pengguna memeriksa live lobby/room/play/spec/count dan lokasi instalasi alternatif. Catat yang inferred vs confirmed.

## P3 — AFK dan lifecycle
Clock injectable, 30 menit default; threshold pendek hanya untuk pengujian dan diberi label. Spectating tetap menghitung; bermain peserta membatalkan; hasil/WaitRoom mengikuti SPEC. Rekonstruksi timer dari history hanya jika cukup bukti, selain itu mulai dari monitoring dengan label diagnostik.
Exit: boundary 29:59/30:00, semua reset/non-reset, restart dan role unknown diuji.

## P4 — Discord adapter
Pilih dependency, masukkan Application ID pengguna, aset, disconnect/reconnect/latest state/clear, preview sesuai Discord. Settings menjelaskan prerequisites tanpa token.
Exit: uji dua urutan startup, restart Discord, play/spec/AFK/count, stop TOUCH; ukur target 2–5 detik dan laporkan hasil nyata.

## P5 — tray, packaging dan rilis
Sempurnakan tray, settings, release build, clean machine install bila tersedia, docs setup/troubleshooting. Tambah CI build/test yang sesuai runner Windows sebelum release. Lisensi diputuskan pemilik, third-party notices diperiksa.
Exit: acceptance matrix lengkap, batas inferensi disebutkan, repo bebas log/secrets, release dibuat hanya jika pengguna memang meminta publikasi release. Push source per fase sudah termasuk otorisasi pengguna; menerbitkan installer/release atau merge PR tetap keputusan terpisah.

Backlog setelah rilis: mode, katalog lagu/difficulty/durasi, nomor room, kapasitas slot. Jangan implement selama fase ini.
