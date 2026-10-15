# Progress

## Kondisi awal — 15 Oktober 2026
- Perencanaan dan eksperimen log selesai cukup untuk memulai prototipe.
- Belum ada kode aplikasi, build, automated tests, repo remote, atau integrasi Discord dalam paket ini.
- Mode serta kapasitas slot ditunda. Count mencakup spectator.
- Role inference masih bersyarat pada urutan log lengkap; unknown fallback wajib.
- Path detection dan role ID otomatis belum diuji untuk pengguna lain.

## Fase aktif berikutnya
P1: Core parser dan offline replay sedang dilaksanakan setelah P0 diterima pemilik. Jangan mulai P2 sebelum hasil P1 direview.

## 15 Oktober 2026 / P0 / main
Perubahan:
- Repository Git lokal diinisialisasi pada branch `main` dan remote `origin` diarahkan ke repository proyek yang diberikan pemilik.
- README dirapikan untuk menjelaskan status awal, scope rilis, batas privasi dan input yang belum tersedia.
- Ignore rules diperluas untuk build/test/package outputs, raw logs, dumps, secrets dan konfigurasi lokal.
- Baseline P1+ dipilih sebagai .NET 10 LTS; belum ada kode aplikasi atau dependency.

Keputusan + alasan:
- .NET 10 LTS dipilih karena masih active support sampai 14 November 2028. .NET 8 yang tersedia lokal berakhir dukungan 10 November 2026 sehingga tidak tepat sebagai baseline proyek baru.
- Tidak membuat `global.json` atau solution pada P0 karena SDK .NET 10 belum terpasang dan coding fitur belum termasuk fase ini.
- Tidak menambahkan lisensi sebelum pemilik memilih jenis lisensi.

Pemeriksaan dijalankan dan hasil:
- Paket instruksi dan docs 01–06 dibaca lengkap; struktur dokumentasi tersedia.
- Git 2.45.0 dan SDK .NET 8.0.408 terdeteksi. Identitas commit tersedia dari config global dan tidak diubah.
- Remote proyek dapat diakses dan tidak memiliki branch/tag sebelum bootstrap.
- Pemeriksaan ignore, diff, staged files dan pola data sensitif dijalankan sebelum commit.

Live verification: pending — tidak berlaku untuk P0 dokumentasi; belum ada aplikasi atau integrasi game.

Risiko atau unknown:
- SDK .NET 10 belum terpasang pada mesin implementasi.
- Lisensi, Discord Application ID, branding/assets final dan deteksi otomatis character role ID belum tersedia.
- Izin push baru dapat dipastikan saat push commit P0 dilakukan.

Push status: dicatat pada laporan fase setelah commit/push; tidak menulis hash commit ke commit yang sama.

Langkah berikutnya: review hasil P0. Jangan mulai P1 sampai pemilik menerima fase ini.

## 15 Oktober 2026 / P1 / main
Perubahan:
- Perubahan pengguna yang sudah ada di remote di-fast-forward sebelum implementasi dan dipertahankan.
- SDK .NET 10.0.402 dipasang user-local; solution dikunci dengan `global.json`.
- Ditambahkan Core typed events, parser pola terdokumentasi, reducer state, roster berbasis ID unik, role inference terbatas, formatter presence dan unknown fallback.
- Ditambahkan console replay offline yang menerima path, role ID manual opsional dan kalibrasi role opsional tanpa mencetak raw log atau daftar ID.
- Ditambahkan fixture sintetis dan automated tests untuk alur acceptance P1.

Keputusan + alasan:
- Core tidak memakai dependency runtime pihak ketiga agar state logic tetap terisolasi. MSTest 4.0.2 digunakan dari template SDK resmi.
- Snapshot replay dianggap selesai pada world room yang dikenal hanya sebagai inferensi prototipe. Tanpa rangkaian entry lengkap, roster tetap partial dan count disembunyikan.
- Character role ID lokal tidak dideteksi dari launcher userid atau anggota pertama; P1 menerima input manual opsional dan default tetap kosong.
- AFK, live reader, WPF, tray dan Discord tidak dimasukkan karena berada di fase berikutnya.

Pemeriksaan dijalankan dan hasil:
- Restore dan Release build berhasil tanpa warning/error dengan warnings-as-errors.
- Automated tests: 22 passed, 0 failed, 0 skipped.
- Console replay dijalankan terhadap fixture sintetis; output hanya berisi state akhir dan diagnostik terpilih.
- Ignore rules memastikan hanya `*.synthetic.log` di folder fixture test yang dapat dilacak; raw log lain tetap diabaikan.

Live verification: pending — P1 hanya diverifikasi lewat fixture sintetis dan belum membaca log TOUCH secara live.

Risiko atau unknown:
- Marker akhir snapshot resmi belum terverifikasi; heuristic P1 tidak boleh dianggap universal.
- Format log bukan API stabil dan live log dapat memiliki variasi yang belum tercakup parser.
- Role ID manual masih diperlukan untuk membedakan local leave dan konfirmasi role switch secara aman.

Push status: dicatat pada laporan fase setelah commit/push.

Langkah berikutnya: review P1. Jika diterima, rencanakan P2 live reader, path detection dan preview Windows sebagai fase terpisah.

## Format update fase
Tanggal / fase / branch:
Perubahan:
Keputusan + alasan:
Pemeriksaan dijalankan dan hasil:
Live verification (pass/pending/fail):
Risiko atau unknown:
Push status (hasil push dicatat di laporan setelah commit):
Langkah berikutnya:
