# Progress

## Kondisi awal — 15 Oktober 2026
- Perencanaan dan eksperimen log selesai cukup untuk memulai prototipe.
- Belum ada kode aplikasi, build, automated tests, repo remote, atau integrasi Discord dalam paket ini.
- Mode serta kapasitas slot ditunda. Count mencakup spectator.
- Role inference masih bersyarat pada urutan log lengkap; unknown fallback wajib.
- Path detection dan role ID otomatis belum diuji untuk pengguna lain.

## Fase aktif berikutnya
Kandidat tray-only/installer sedang divalidasi. Discord presence telah dikonfirmasi pengguna, termasuk spectator saat gameplay. Install bersih, startup login, popup Enable/Disable, dan deteksi otomatis lintas perangkat tetap memerlukan acceptance pengguna.

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
- Live retest setelah build koreksi kemudian dikonfirmasi pengguna: Waiting/Spectating/Smashing Key, role dan roster/count berfungsi baik pada UI.
- Boundary AFK 30 menit tidak dijalankan secara live karena durasinya; pengguna menerima hasil automated clock tests sebagai dasar acceptance. Ini bukan klaim live pass untuk boundary tersebut.

Risiko atau unknown:
- Marker live gameplay/results tetap bergantung format log yang tidak merupakan API stabil.
- Start monitoring di tengah room sengaja mulai elapsed dari nol dan tidak mengklaim waktu sebelum monitoring.
- Role ID manual/kalibrasi masih dibutuhkan bila bukti role lokal tidak lengkap.
- Discord, tray dan packaging belum diimplementasikan.

Push status: dicatat pada laporan fase setelah commit/push.

Langkah berikutnya: review P3 diterima dengan pengecualian live boundary AFK yang terdokumentasi; P4 Discord dapat direncanakan sebagai fase terpisah.

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

Live acceptance pengguna:
- User-confirmed: seluruh alur live yang dicoba berfungsi baik, termasuk phase/status, role, roster/count dan nickname manual.
- Nickname dan character role ID pribadi hanya digunakan pada settings lokal; nilainya tidak dicatat atau di-hard-code di repository.
- Boundary AFK 30 menit tidak dijalankan live. Acceptance AFK didasarkan pada automated clock tests; status pengujian live boundary tetap `not run`, bukan `pass`.
- P3 diterima pengguna dengan keterbatasan tersebut. Deteksi nickname otomatis dan hover/pengiriman tooltip Discord tetap pending P4.

## 15 Oktober 2026 / P4 Discord adapter / main
Perubahan:
- Ditambahkan adapter Discord terisolasi di Infrastructure dengan package `DiscordRichPresence` 1.6.1.70 (MIT); Core tetap tidak bergantung Discord.
- Settings/UI menerima public Application ID dan optional large image asset key. Preview tetap berfungsi saat konfigurasi kosong atau Discord tidak tersedia.
- Payload hanya memuat Details, State, asset key dan nickname tooltip; tidak ada party capacity, role ID, daftar/nama anggota, raw log, token atau Client Secret.
- Service menyimpan latest payload, deduplicate nilai identik, menunggu Discord Ready, dan hanya mengirim latest state setelah reconnect.
- Stop monitoring, game exit dan app disposal membersihkan presence serta pending state.

Keputusan + alasan:
- Stable 1.6.1.70 dipilih daripada versi pre-release. Target net9.0 package kompatibel dengan net10.0 proyek dan tidak memerlukan native installer.
- Application ID tidak di-hard-code karena ID/aset final belum diberikan. Public ID disimpan di LocalAppData; secret/token tidak dibutuhkan.
- Live Discord tidak diklaim pass dari fake transport atau WPF smoke test.

Pemeriksaan dijalankan dan hasil:
- Restore package, formatter verification dan Release build lulus.
- Automated tests: 80 passed, 0 failed, 0 skipped.
- Tests baru mencakup disabled mode, mapping tooltip, deduplication, latest-only reconnect, clear, status/error dan recovery initial connection failure.
- WPF smoke launch tanpa Application ID lulus; Discord disabled tidak menghalangi startup aplikasi.

Live verification: pending — belum ada Application ID/asset key proyek untuk menguji Discord desktop, hover `assets.large_text`, dua urutan startup, restart Discord, clear nyata dan target visible update 2–5 detik.

Risiko atau unknown:
- Asset key harus cocok dengan asset pada Discord Application yang sama; tanpa asset, tooltip large image tidak dapat diamati.
- Visibilitas activity bergantung Discord desktop/login dan pengaturan activity sharing pengguna.
- Library adalah dependency komunitas MIT, bukan SDK resmi Discord; dibungkus interface agar dapat diganti tanpa mengubah Core.

Push status: dicatat pada laporan fase setelah commit/push.

Langkah berikutnya: pengguna menyediakan/memasukkan public Application ID dan asset key, lalu menjalankan protokol live P4. Jangan mulai P5 sebelum hasil review P4.

## 15 Oktober 2026 / P5 tray, packaging dan persiapan rilis / main
Perubahan:
- Seluruh input, binding identitas dan persistence nickname manual dihapus. Settings JSON lama tetap dapat dimuat; field nickname lama diabaikan dan hilang pada save berikutnya.
- Model nickname otomatis tetap tersedia untuk mapping terverifikasi di masa depan. Selama belum ada sumber log yang sah, Discord large-image tooltip memakai `TOUCH`.
- Ditambahkan notification-area tray dengan preferensi lokal: close menyembunyikan jendela, double-click/menu Open membukanya, dan menu Exit melakukan save/dispose serta clear Discord.
- Ditambahkan script publish portable self-contained single-file Windows x64, workflow CI Windows build/test/package, README setup/troubleshooting, dan third-party notices.
- Dokumentasi evidence dan acceptance diperbarui: `HostPlayer id` hanya kandidat identitas; nama room/`ElsePlayerName` tidak pernah dipakai sebagai nickname lokal.
- Nickname dan character role ID pribadi yang masih tertulis di dokumentasi lama dihapus. `ExeCode-Generation.py` milik pengguna tetap untracked dan tidak disentuh.

Keputusan + alasan:
- Pengguna memilih menghapus manual nickname sepenuhnya daripada merilis fallback yang memerlukan input. Automatic nickname menjadi penyempurnaan berikutnya dan tidak menghalangi rilis pertama.
- Paket dibuat portable self-contained agar pengguna target tidak perlu memasang .NET. Output build/ZIP tetap di `artifacts/`, diabaikan Git, dan tidak diterbitkan otomatis.
- Tray memakai `System.Windows.Forms.NotifyIcon` bawaan Windows sehingga tidak menambah dependency runtime NuGet.
- Lisensi project belum dipilih; tidak dibuat `LICENSE`. Notices mencatat direct package `DiscordRichPresence` 1.6.1.70 (MIT) dan MSTest 4.0.2 (MIT).

Pemeriksaan dijalankan dan hasil:
- Formatter verification dan Release build lulus.
- Automated tests: 79 passed, 0 failed, 0 skipped. Test manual-nickname diganti dengan regression automatic-only, fallback `TOUCH`, settings migration dan default tray.
- Replay fixture midgame spectator tetap menghasilkan `2 Players Joined` / `Spectating`.
- Publish `win-x64` self-contained single-file dan ZIP berhasil. ZIP berisi tepat executable, README dan third-party notices; tidak ada settings, raw log, JSON privat atau PDB.
- WPF publish smoke lulus: main window muncul dan close membuat proses tetap hidup di tray.
- NuGet vulnerability audit tidak menemukan package vulnerable dari source aktif.
- CI workflow ditambahkan tetapi run GitHub pertamanya baru dapat diamati setelah push.

Live verification:
- User-confirmed sebelum P5: lobby, room counts, waiting, spectator-in-room, playing activity dan jalur tooltip Discord pernah tampil.
- Pending: klik nyata tray Open/Exit, clean-machine extraction/run, restart Discord dan dua urutan startup yang dirinci, clear nyata, latency target 2–5 detik, serta boundary AFK live.
- Laporan spectator saat gameplay pernah menampilkan `Smashing Key` belum direproduksi; expected tetap `Spectating`, jadi kasus ini bukan pass.

Risiko atau unknown:
- Deteksi nickname otomatis belum tersedia karena log belum menunjukkan mapping nickname ↔ self character role ID yang defensible.
- Paket portable belum ditandatangani dan belum dipublikasikan sebagai GitHub Release/installer.
- Tidak ada klaim clean-machine compatibility sampai diuji pada mesin lain.
- Project belum memiliki lisensi source sampai pemilik memilihnya.

Push status: dicatat pada laporan fase setelah commit/push.

Langkah berikutnya: review paket portable dan jalankan checklist pengguna P5. Jangan otomatis memulai fitur pascarilis atau menerbitkan GitHub Release.

## 15 Oktober 2026 / P5 usability tray-only dan installer / main
Perubahan:
- Dashboard konfigurasi dihapus dari alur pengguna. Aplikasi startup langsung ke notification area; klik kiri menampilkan popup sakelar Enable/Disable dan klik kanan menyediakan Exit.
- Monitoring otomatis menunggu satu `TouchClient.exe`, memperoleh executable path melalui `MainModule` atau fallback Windows `QueryFullProcessImageName`, menemukan log, mengikuti lifecycle game, dan membersihkan presence saat Disable/game exit/Exit.
- Public Discord Application ID dan asset key proyek dibundle. Path log, Role ID, role calibration, nickname, Application ID, dan asset key tidak lagi diminta atau dipersist.
- Settings dimigrasikan menjadi preferensi Enabled/Disabled saja. Ditambahkan single-instance guard.
- Ditambahkan MSI per-user WiX: install ke LocalAppData, launch setelah install, serta startup saat login melalui HKCU Run. Ditambahkan script build installer dan artifact CI.

Keputusan + alasan:
- Pengguna meminta pengalaman install-and-use tanpa input manual. Konfigurasi Discord publik aman dibundle; secret/token tetap tidak digunakan.
- Beberapa client tidak dipilih arbitrer karena berisiko membaca log yang salah.
- MSI per-user dipilih agar tidak wajib admin dan startup dapat dibersihkan oleh uninstall.

Pemeriksaan dijalankan dan hasil:
- Release build lulus setelah penggantian UI/runtime.
- Automated tests: 79 passed, 0 failed, 0 skipped.
- Formatter verification, diff check, publish portable dan build MSI WiX berhasil.
- ZIP final berisi tepat executable, README, dan third-party notices. Scan source tidak menemukan identifier pribadi atau secret; vulnerability audit tidak menemukan package vulnerable.
- Portable smoke: satu proses, tidak ada dashboard/taskbar window, launch kedua tetap satu instance, dan settings instalasi bersih hanya menyimpan `Enabled`.
- MSI acceptance lokal: install dan uninstall exit 0; EXE terpasang per-user di LocalAppData; startup HKCU dibuat; aplikasi langsung berjalan tanpa dashboard; launch kedua tetap satu instance; uninstall saat aplikasi masih aktif menghentikan proses serta menghapus EXE dan startup registry.

Live verification:
- User-confirmed: seluruh display Discord yang dicoba berfungsi benar; spectator saat gameplay tampil `Spectating`, bukan `Smashing Key`.
- Pending: popup tray Enable/Disable baru, install bersih pada perangkat lain, startup login, auto-detection tanpa settings lama, dan uninstall nyata.

Risiko atau unknown:
- Fallback process path memperluas kompatibilitas tetapi belum membuktikan semua kebijakan keamanan/perangkat Windows.
- Installer belum ditandatangani dan project source belum memiliki lisensi pilihan pemilik.

Push status: dicatat pada laporan fase setelah commit/push.

Langkah berikutnya: pengguna menguji MSI pada perangkat berbeda. Jangan menerbitkan GitHub Release otomatis.

## 16 Oktober 2026 / P5.1 pemulihan tray dan metode instalasi / main
Perubahan:
- Menambahkan shortcut Start Menu per-user agar aplikasi dapat dijalankan kembali setelah pengguna memilih Exit tanpa restart Windows.
- Menserialkan initialize, Enable/Disable, dan Exit; switch dinonaktifkan selama transisi dan disinkronkan kembali bila operasi gagal.
- Worker monitoring sekarang memulihkan diri dari exception tak terduga, sementara batas event UI mencegah kegagalan toggle/cleanup menutup aplikasi.
- Menambahkan `Install-TouchRpc.ps1` untuk instalasi MSI lokal atau download HTTPS dengan verifikasi SHA-256. Build installer menghasilkan checksum dan CI mengunggah MSI, checksum, serta script bersama-sama.

Keputusan + alasan:
- Exit tetap berarti menutup aplikasi sepenuhnya. Jalur buka ulang disediakan melalui Start Menu, bukan mengubah Exit menjadi Hide.
- Metode PowerShell tetap memakai MSI yang sama agar startup, upgrade, Installed Apps, dan uninstall tidak bercabang.
- Auto-detection TOUCH tidak diubah karena pengguna membuktikan deteksi berhasil pada perangkat lain dan instalasi game di drive E:.

Pemeriksaan dijalankan dan hasil:
- Release build, formatter verification, dan 79/79 automated tests lulus setelah perubahan lifecycle.
- Syntax parser PowerShell lulus untuk script install dan build installer; HTTP ditolak oleh mode download sesuai kebijakan HTTPS-only.
- MSI WiX 1.0.1 dan checksum berhasil dibangun. Instalasi PowerShell lokal menampilkan hash, memasang EXE, membuat shortcut Start Menu dan startup HKCU, menjalankan tepat satu proses, serta mempertahankan settings hanya `Enabled`.
- Smoke relaunch: setelah proses dihentikan, shortcut Start Menu menjalankan kembali satu proses tanpa restart; peluncuran EXE kedua tetap ditolak oleh single-instance guard.
- Uninstall saat aplikasi berjalan exit 0, menghentikan proses, serta menghapus EXE, startup entry dan shortcut Start Menu.
- Vulnerability audit tidak menemukan package vulnerable. Final diff/privacy, commit dan push dicatat setelah selesai.

Live verification:
- User-confirmed pass pada dua perangkat: install, tray-only popup, auto-detection, Discord presence, Disable/Enable, startup login, uninstall, termasuk TOUCH di drive E:.
- User-confirmed fail pada build sebelumnya: aplikasi tidak memiliki jalur buka ulang setelah Exit; pada satu perangkat aplikasi juga menghilang setelah toggle kedua.
- Pending: build P5.1 diuji pengguna untuk toggle berulang dan Exit → buka ulang dari Start Menu.

Risiko atau unknown:
- Installer tetap unsigned. Script hash memverifikasi integritas artifact yang diketahui, tetapi bukan pengganti code signing.
- URL release stabil belum tersedia dan publikasi GitHub Release belum dilakukan.

Push status: dicatat pada laporan fase setelah commit/push.

Langkah berikutnya: pengguna menguji MSI P5.1 pada perangkat yang sebelumnya mengalami aplikasi hilang. Jangan menerbitkan GitHub Release otomatis.

## Format update fase
Tanggal / fase / branch:
Perubahan:
Keputusan + alasan:
Pemeriksaan dijalankan dan hasil:
Live verification (pass/pending/fail):
Risiko atau unknown:
Push status (hasil push dicatat di laporan setelah commit):
Langkah berikutnya:
