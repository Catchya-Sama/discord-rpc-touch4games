# Prompt awal untuk Cline — salin seluruh bagian di bawah

Kamu adalah implementer proyek TOUCH RPC Windows. ChatGPT bertindak sebagai arsitek dan saya sebagai pemilik produk/penguji. Paket dokumentasi di folder ini adalah sumber spesifikasi. Jangan implement semua fitur sekaligus.

Baca AGENTS.md, README.md dan seluruh docs/01 sampai 06 serta PROGRESS.md. Periksa working tree, instruksi repo, SDK .NET, remote Git dan identitas Git yang tersedia. Jelaskan pemahaman scope terbaru: status lobby/waiting/play/spec, jumlah SEMUA anggota room, AFK 30 menit tanpa bermain; mode, musik, nomor room dan kapasitas dikeluarkan.

Mulai di Plan Mode dengan SATU fase: P0 bootstrap repository dan dokumentasi. Sebutkan file yang akan diubah, acceptance, informasi yang belum tersedia, dan keputusan teknis. Setelah saya menerima rencana fase, implementasikan fase itu di Act Mode sampai pemeriksaan selesai. Jangan langsung melompat ke Discord atau membangun seluruh UI.

Saya mengotorisasi commit dan push source proyek per fase ke remote repository yang saya berikan/yang sudah terverifikasi. Periksa diff, hindari data sensitif, update PROGRESS dalam commit fase yang sama, lalu commit dan push. Jangan menebak repo dari URL profil, mengubah identitas Git global, melakukan force push, atau membuat commit palsu demi contribution graph. Bila remote/identity belum tersedia, selesaikan persiapan lokal yang bisa dikerjakan lalu minta nilai yang dibutuhkan. Jangan klaim push jika belum berhasil.

Raw output_log saya tidak boleh di-commit. Gunakan synthetic fixtures. Jangan hard-code role ID saya atau path D:/Touch untuk semua pengguna. Jangan menyamakan launcher userid dengan character roleid. Unknown state harus terlihat sebagai unknown/fallback, bukan data yang dikarang.

Setelah fase selesai, laporkan perubahan, tes, batasan, instruksi uji Windows bila diperlukan, branch, commit hash, dan hasil push. Berhenti untuk review hasil sebelum melanjutkan fase berikutnya.
