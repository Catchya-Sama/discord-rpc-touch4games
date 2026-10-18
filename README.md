make sure to update to the latest version due to changes to the rpc api on discord's side

masih proses, gw juga bingung ini apaan

jadi ginii.... untuk hasil nya kek giniii



![image](https://cdn.discordapp.com/attachments/886863611916582923/1560328509358866452/7F051796-73B4-46ED-B5E4-32BFA923210D.png?ex=6ad6452a&is=6ad4f3aa&hm=2ee0c0fda8455a4eb85b52cdb615a898dbac0511e82de268f1921da72b110d6f&)
![image](https://media.discordapp.net/attachments/886863611916582923/1560329737170198568/8E5B8518-46BA-42AE-8F41-B08A542077B2.png?ex=6ad6464e&is=6ad4f4ce&hm=9e201ac9b47a8edbf5265d75c15453075816afa6846b74dafc496c539daa96ad&=&format=webp&quality=lossless)
![image](https://media.discordapp.net/attachments/886863611916582923/1560327715557744690/0083B5CE-0A03-4F27-AD72-A38531870879.png?ex=6ad6446c&is=6ad4f2ec&hm=f65bf650acc46352a19aacf876fd8639a594f1cad816368c187dab543b2b0922&=&format=webp&quality=lossless)
![image](https://media.discordapp.net/attachments/886863611916582923/1560328643215884430/EC6FAE79-00A1-4FE8-A9ED-0786982B1931.png?ex=6ad6454a&is=6ad4f3ca&hm=fb0f30526366b311834856dfda29db3656650593a17cb44705050b9e9a499acc&=&format=webp&quality=lossless)
![image](https://media.discordapp.net/attachments/886863611916582923/1560328869553119312/image.png?ex=6ad64580&is=6ad4f400&hm=584918714ab93c9cc91ce3c92e95a7efaa3cbabf16d85046e6fd3c077f64e956&=&format=webp&quality=lossless)
![image](https://media.discordapp.net/attachments/886863611916582923/1560329168896659467/DD94A1F6-9CEE-4823-8A79-5BC60767A1B6.png?ex=6ad645c7&is=6ad4f447&hm=7924b0cdd8f706661ca7e09991fdb6728904ebcb7fb46009161a60e65fd4fede&=&format=webp&quality=lossless)



# TOUCH RPC

Pendamping Windows untuk membaca `TouchClient_Data/output_log.txt` secara lokal dan menampilkan status game sebagai Discord Rich Presence. Aplikasi tidak mengubah file TOUCH, tidak mengunggah log, dan tidak memerlukan token Discord.

Project owner: bongbong (xooty).

## Scope first release

- Lobby: `In Lobby`.
- Room menunggu: jumlah semua anggota room, termasuk spectator, dengan state `Waiting`.
- Peserta bermain: `Smashing Key` hanya jika role lokal Joined terverifikasi.
- Spectator: `Spectating`, termasuk selama gameplay dan setelah hasil sampai role berubah secara sah.
- AFK: 30 menit berada di room tanpa mulai bermain sebagai peserta.
- Unknown fallback jika role atau roster belum dapat dipastikan; aplikasi tidak mengarang count atau state.
- Notification-area tray dan Discord Rich Presence siap pakai tanpa konfigurasi manual.

Mode permainan, song, difficulty, nomor room, jumlah slot, overlay, akun online, dan backend berada di luar scope.

## install lewat file

1. Running `TouchRpc-Setup-win-x64.msi`.
2. Installer memasang aplikasi untuk pengguna Windows saat ini, menjalankannya di notification area, dan mengaktifkan startup saat login Windows.
3. Tidak ada path log, Application ID, asset key, Role ID, nickname, atau role calibration yang perlu diisi.
4. Jalankan TOUCH dan Discord desktop seperti biasa. TOUCH RPC otomatis menunggu `TouchClient.exe`, menemukan `TouchClient_Data\output_log.txt`, dan mengirim state terbaru.

## Instalasi melalui PowerShell

Script `scripts\Install-TouchRpc.ps1` memasang MSI yang sama, sehingga lokasi instalasi, startup login, Start Menu, upgrade, dan uninstall tetap identik. Script tidak mengubah execution policy.

Dari clone/repository atau paket yang sudah berisi MSI:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Install-TouchRpc.ps1 `
  -InstallerPath .\artifacts\installer\TouchRpc-Setup-win-x64.msi
```

Dari URL HTTPS setelah artifact diterbitkan, sertakan SHA-256 resmi:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install-TouchRpc.ps1 `
  -InstallerUri "https://example.invalid/TouchRpc-Setup-win-x64.msi" `
  -Sha256 "64_HEX_CHARACTER_SHA256"
```

### Uninstall melalui PowerShell

Uninstall tidak memerlukan MSI asli atau download ulang:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Uninstall-TouchRpc.ps1
```

Script menemukan registrasi MSI TOUCH RPC yang cocok, menjalankan uninstall resmi Windows Installer, dan membersihkan aplikasi per-user, startup login, serta shortcut Start Menu. Jika TOUCH RPC sudah tidak terpasang, script selesai tanpa error. Settings `%LocalAppData%\TouchRpc\settings.json` dipertahankan agar pilihan Enabled/Disabled dapat digunakan kembali bila aplikasi dipasang lagi.

## Penggunaan tray

Klik kiri ikon tray untuk membuka popup sakelar. **Enabled** berarti aplikasi otomatis menunggu dan memonitor TOUCH; **Disabled** menghentikan monitoring dan membersihkan Rich Presence, tetapi aplikasi tetap berjalan di tray. Klik kanan ikon lalu pilih **Exit** untuk menutup aplikasi sepenuhnya. Setelah Exit, buka **Start → TOUCH RPC → TOUCH RPC** untuk menjalankannya kembali tanpa restart Windows. Pilihan Enabled/Disabled diingat untuk startup berikutnya.

Installer bersifat per-user dan tidak memerlukan admin. Program dipasang di `%LocalAppData%\TOUCH RPC`; startup login menggunakan entri pengguna Windows saat ini. Settings hanya menyimpan preferensi Enabled/Disabled di `%LocalAppData%\TouchRpc\settings.json`. Raw log hanya dibaca dengan file sharing yang sesuai dan tidak disalin.
Pemilik belum memilih lisensi project, sehingga repository belum memiliki `LICENSE`. Dependency langsung dan lisensinya dicatat di `THIRD-PARTY-NOTICES.md`.
