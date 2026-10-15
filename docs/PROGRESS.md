# Progress

## Kondisi awal — 15 Oktober 2026
- Perencanaan dan eksperimen log selesai cukup untuk memulai prototipe.
- Belum ada kode aplikasi, build, automated tests, repo remote, atau integrasi Discord dalam paket ini.
- Mode serta kapasitas slot ditunda. Count mencakup spectator.
- Role inference masih bersyarat pada urutan log lengkap; unknown fallback wajib.
- Path detection dan role ID otomatis belum diuji untuk pengguna lain.

## Fase aktif berikutnya
P3 nickname preparation: status/roster/AFK dipertahankan, identitas nickname dan kandidat tooltip disiapkan untuk P4. Jangan mulai adapter Discord dalam fase ini.

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

## 15 Oktober 2026 / P2 / main
Perubahan:
- Ditambahkan Infrastructure untuk initial replay + incremental tail, source health, process detection, path validation, settings LocalAppData dan monitoring coordinator.
- Ditambahkan WPF preview dengan Detect, pemilihan proses eksplisit, Browse, Character Role ID manual, role calibration, Start/Stop, source health dan presence diagnostics.
- Monitoring process-bound membersihkan preview saat game tutup dan menunggu satu TouchClient baru untuk reconnect sebagai session baru.
- Reader menangani UTF-8/CRLF parsial, byte offset, truncate, replacement, missing/unreadable file dan cancellation tanpa menulis ke file TOUCH.
- Ditambahkan integration tests berbasis file sementara; tidak ada raw log pengguna yang masuk Git.

Keputusan + alasan:
- File identity Windows memakai handle metadata agar replacement dapat dikenali walau file baru lebih besar. Truncate juga memulai session baru.
- Reducer diganti pada session file baru supaya roster/role lama tidak terbawa. Initial history hanya menghasilkan snapshot akhir untuk preview.
- Jika executable path tidak dapat dibaca atau ada beberapa client, aplikasi tidak memilih secara diam-diam; fallback adalah pilihan proses atau Browse.
- UI memisahkan source health dari Discord status. Discord tetap disabled karena adapter baru masuk P4.
- Settings disimpan di LocalAppData, bukan repository/folder game. Path instalasi dan role ID tidak memiliki default pribadi.

Pemeriksaan dijalankan dan hasil:
- `dotnet format --verify-no-changes` lulus.
- Release build untuk Core, Infrastructure, Replay, WPF App dan Tests berhasil dengan warnings-as-errors.
- Automated tests: 35 passed, 0 failed, 0 skipped.
- Smoke launch `TouchRpc.App.exe`: proses tetap hidup selama 3 detik dan ditutup otomatis tanpa crash.
- TouchClient terdeteksi berjalan, tetapi executable path tidak tersedia pada sesi pemeriksaan; fallback Browse tetap diperlukan untuk live test ini.

Live verification: pending — lobby/room/play/spec/count dan instalasi alternatif belum dikonfirmasi pada UI oleh pengguna.

Risiko atau unknown:
- Marker snapshot dan variasi format live tetap heuristic berdasarkan bukti yang tersedia.
- Process path dapat ditolak OS; Browse merupakan fallback resmi, bukan error palsu.
- Monitoring manual tanpa process selection tidak dapat memastikan game closed dari process lifecycle dan bergantung pada source health file.
- AFK, Discord, tray dan packaging belum diimplementasikan.

Push status: dicatat pada laporan fase setelah commit/push.

Langkah berikutnya: pengguna menjalankan protokol live P2 dan mengirim diagnostics teredaksi. P3 AFK tidak dimulai sampai review P2.

## 15 Oktober 2026 / P3 / main
Perubahan:
- Ditambahkan monotonic clock injectable, default AFK 30 menit, timer origin/elapsed/tracking dan presence `AFK · Waiting`/`AFK · Spectating`.
- Joined gameplay atau PlayRoom menghentikan timer; interval baru dimulai saat kembali WaitRoom. Spectator tetap menghitung melalui gameplay/results/waiting.
- Ready, role switch, host/slot operation dan perubahan roster tidak mereset timer. Lobby, local leave dan session file baru membersihkan timer.
- Historical replay tidak merekonstruksi durasi tanpa bukti; timer dimulai dari monitoring dan diagnostic menyatakan history tidak tersedia.
- Preview WPF menampilkan elapsed, origin, threshold dan status AFK. Settings menyimpan threshold positif dengan default 30 menit.
- Reader diperkuat untuk mendeteksi rewrite in-place dengan ukuran sama agar state/AFK lama tidak terbawa.

Keputusan + alasan:
- Runtime memakai `Stopwatch` melalui interface Core agar perubahan jam sistem tidak memengaruhi AFK dan test tetap deterministik.
- Role unknown tidak pernah menghasilkan teks AFK. Setelah threshold, diagnostic `AfkRoleUnknown` menjelaskan bahwa keputusan tidak dapat dibuat.
- Threshold pendek hanya digunakan oleh automated tests; runtime dan persisted default tetap 30 menit.
- Initial log history tidak dianggap sebagai elapsed AFK karena timestamp monotonic sebelum aplikasi berjalan tidak tersedia.

Pemeriksaan dijalankan dan hasil:
- Formatter verification dan Release build dengan warnings-as-errors lulus.
- Automated tests: 50 passed, 0 failed, 0 skipped.
- Replay fixture menghasilkan presence lama yang sama dan menambahkan AFK origin/diagnostic teredaksi.
- Regression rewrite same-length sempat menemukan defect reader; prefix probe/last-write marker ditambahkan dan test kemudian lulus.

Live verification: pending — boundary 30 menit dan transisi Joined/Spectator belum dikonfirmasi pada TOUCH nyata.

Corrective review dari live test pengguna:
- User-confirmed: preview berpindah `In Room` → `In Lobby` → `In Session`, tetapi role tetap `Unknown`; tes manual boundary AFK 30 menit ditunda.
- Observed read-only pada log lokal: `_OnCmd_RoomEnter` dan `_OnCmd_GetGameInfoRe` tidak muncul, sementara room detail, WaitRoom/PlayRoom dan SwitchRoleType tersedia.
- Observed: satu baris world memuat `the_current_world` dan `the_last_world`; parser lama dapat salah membaca last world sebagai phase aktif.
- Koreksi: parse current world saja, room detail dapat membuka entry, direct entry ke PlayRoom menginfer Spectator, dan konfirmasi switch dapat mempelajari local role ID untuk session tanpa hard-code.
- Sanitized structural replay dari log aktual menghasilkan phase Waiting, role Joined inferred, roster complete dan state Waiting; raw log/ID tidak disalin ke repository.
- Corrective validation: formatter verification, Release build, 56/56 automated tests dan WPF smoke launch melalui host .NET user-local lulus.
- Live retest setelah build koreksi masih diperlukan untuk mengonfirmasi Waiting/Spectating/Smashing Key pada UI; boundary AFK 30 menit tetap ditunda dan tidak diklaim pass.

Risiko atau unknown:
- Marker live gameplay/results tetap bergantung format log yang tidak merupakan API stabil.
- Start monitoring di tengah room sengaja mulai elapsed dari nol dan tidak mengklaim waktu sebelum monitoring.
- Role ID manual/kalibrasi masih dibutuhkan bila bukti role lokal tidak lengkap.
- Discord, tray dan packaging belum diimplementasikan.

Push status: dicatat pada laporan fase setelah commit/push.

Langkah berikutnya: pengguna menjalankan protokol live P3. P4 Discord tidak dimulai sampai review P3.

## 15 Oktober 2026 / P3 nickname preparation / main
Perubahan:
- Ditambahkan model local identity/nickname, sumber nilai, diagnostic dan kandidat tooltip `LargeText` tanpa mengubah Details/State.
- Parser mengenali self character role ID hanya dari `isuserself=True`; `userid` launcher, command line dan `ElsePlayerName` tidak digunakan sebagai nickname lokal.
- Settings/UI menambahkan nickname manual. Manual yang belum terikat hanya berlaku pada session aktif; setelah self role ID terverifikasi, binding disimpan lokal.
- Otomatis terverifikasi mengalahkan manual; manual mismatch tidak dipakai; fallback umum adalah `TOUCH`.
- Unicode/kapitalisasi dipertahankan dan kandidat tooltip dipotong aman pada 128 text elements; nilai satu text element jatuh ke `TOUCH` agar memenuhi minimum P4.

Observed/user-confirmed/unknown:
- User-confirmed: pembacaan phase/role live setelah koreksi sebelumnya berhasil; boundary AFK manual tetap tidak diuji.
- Observed read-only: log memuat self `roleid` eksplisit melalui `isuserself=True` serta memuat launcher/login fields yang tidak boleh disimpan.
- Observed: `ElsePlayerName` tidak memiliki pemetaan roleid yang cukup dan dapat mewakili pemain lain.
- Unknown: sumber nickname otomatis terverifikasi belum ditemukan, sehingga fitur otomatis belum diklaim berhasil; manual fallback tersedia.
- Pending P4: pengiriman `assets.large_text` dan hover logo Discord belum diimplementasikan atau diuji.

Pemeriksaan:
- Formatter verification dan Release build lulus.
- Automated tests: 69 passed, 0 failed, 0 skipped.
- Replay fixture lama tetap menghasilkan `2 Players Joined` dan `Spectating`; perubahan nickname tidak mengubah payload status P1–P3.
- WPF smoke launch melalui host .NET user-local lulus dengan kolom nickname baru.
- Live nickname otomatis belum dapat diverifikasi karena log aktual tidak menyediakan pemetaan nickname↔self role ID yang cukup.

## Format update fase
Tanggal / fase / branch:
Perubahan:
Keputusan + alasan:
Pemeriksaan dijalankan dan hasil:
Live verification (pass/pending/fail):
Risiko atau unknown:
Push status (hasil push dicatat di laporan setelah commit):
Langkah berikutnya:
