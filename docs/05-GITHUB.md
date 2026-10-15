# GitHub dan bukti pengerjaan
Pengguna meminta commit DAN push agar pengerjaan proyek terlihat di GitHub. Tidak perlu meminta izin ulang untuk push rutin source proyek ke remote terverifikasi dalam cakupan ini.

## Bootstrap
- Periksa git status, remote, branch dan identitas lokal; jangan menimpa repo pengguna.
- Jika repo baru, init dengan main. Jika repository existing, hormati branch policy; gunakan branch feature bila main protected.
- URL profil https://github.com/Catchya-Sama bukan remote repository. Jangan mengarang nama repo atau membuat repo publik tanpa visibility yang dipilih pengguna.
- Jika remote belum ada, selesaikan pekerjaan lokal yang bisa dilakukan dan minta URL repo. Jangan mengklaim push sukses.
- Verifikasi user.name/user.email sudah sesuai. Jika kosong, minta identitas; jangan menebak. Email commit perlu terhubung dengan akun GitHub (atau noreply yang benar) untuk atribusi.

## Setiap fase
1. Jalankan pemeriksaan sesuai perubahan, baca diff dan status.
2. Update progress dan README bila perlu.
3. Stage path yang relevan secara eksplisit, bukan git add . secara membabi buta.
4. Commit bermakna, contoh `docs: define TOUCH RPC phase-one scope` atau `feat: parse room membership events`.
5. Push ke branch verified (set upstream saat pertama). Jika rejected, fetch dan periksa penyebab; jangan force push atau overwrite.
6. Laporkan hash dan branch serta push success/failure. Jika auth dibutuhkan, gunakan flow GitHub resmi/credential manager, jangan minta token ditempel ke chat/repo.

Commit tidak harus banyak: jangan empty commits, backdate, fake activity, atau memecah perubahan tanpa alasan hanya untuk grafik.
Commit di branch feature tidak selalu langsung masuk contribution graph; umumnya perlu memenuhi syarat GitHub seperti email terasosiasi dan commit di default branch/gh-pages atau akhirnya masuk default branch. Push tetap terlihat di history branch. Verifikasi dokumentasi GitHub resmi bila menjelaskan aturan graph terkini. Tidak menjanjikan kotak hijau setiap push.

Jangan auto-merge ke main sekadar mengejar graph. PR draft boleh dibuat jika diminta atau workflow repo mengharuskan, tanpa mengarang review orang lain.
Exclude raw logs, fixtures asli, bin/obj, IDE state, user settings, secrets, dumps dan packages. Synthetic fixtures tidak menggunakan ID/nama/path/login asli.
