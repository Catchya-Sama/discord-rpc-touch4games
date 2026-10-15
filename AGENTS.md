# Instruksi proyek untuk Cline

## Ruang lingkup
Ikuti spesifikasi fase ini. Jangan menambah mode, katalog musik, OCR, memory reading, injection, packet capture, overlay, akun online, atau backend tanpa permintaan baru.
Jangan mengubah file TOUCH; hanya baca log dengan file sharing yang sesuai.

## Siklus kerja
- Awali dengan membaca dokumentasi dan status working tree. Pertahankan perubahan pengguna.
- Rencanakan SATU fase dan jelaskan hasil reviewable serta pemeriksaannya sebelum implementasi.
- Setelah fase dipilih, implementasikan sampai pemeriksaan lokal yang tersedia selesai. Jangan minta izin untuk setiap edit rutin.
- Uji Windows/game jika memungkinkan. Jika memerlukan laptop pengguna, sediakan langkah tepat dan tunggu hasil untuk bagian yang bergantung pada itu. Jangan mengklaim live test selesai.
- Periksa diff, staged files, data sensitif, dan hasil tes sebelum commit.
- Update docs/PROGRESS.md dalam commit fase yang sama: perubahan, bukti tes, keterbatasan, keputusan, langkah berikutnya. Catat commit hash di laporan, bukan menuliskan hash commit itu sendiri ke file dalam commit yang sama.
- Commit dan push perubahan fase yang layak disimpan ke branch proyek setelah pemeriksaan. Jika live test tertunda, tandai WIP/unverified dengan jujur; jangan tandai fase selesai.
- Akhiri laporan dengan branch, hash, hasil push, hasil pemeriksaan dan apa yang perlu diuji pengguna. Jangan otomatis lanjut fase lain.

## Kejujuran data
Pisahkan observed, user-confirmed, inferred, unknown. Tidak ada data bukan berarti nol.
Jangan hard-code ID karakter Sultan untuk semua pengguna. Jangan menganggap READY sama dengan JOINED.
Jangan menghitung baris AddPlayer sebagai jumlah orang. Gunakan ID unik, daftar awal, masuk, keluar, dan batas sesi room.
Jangan reset AFK karena log ramai, pemain lain bermain, chat, pergantian role, atau spectating.

## Git dan keamanan
Tidak ada force push, reset --hard, amend commit yang sudah dipush, penghapusan branch remote, atau pembuatan history palsu.
Jangan ubah identitas Git global atau menebak email. Raw log, config lokal, path pribadi, token dan installer build tidak masuk commit.
Application ID Discord boleh publik; Client Secret dan user/bot token tidak dibutuhkan oleh fitur ini.
