# Bukti dan batas interpretasi log
Sumber: pengujian manual pengguna 13–15 Oktober 2026, micro client Windows, Unity 3.5.7f6 menurut header log. Format bukan API resmi/stabil.

| Role | Bukti / interpretation | Line |
|---|---|---|
| `the_current_world Type=WaitRoom` | Room menunggu, observed | Tidak menetapkan role |
| `Type=RoleCreateRoom` setelah keluar | Lobby pada alur diuji | Jangan samakan semua scene lain dengan lobby |
| `_OnCmd_RoomDetailInfo_Re` lalu `RoomMemer: ID` | Snapshot anggota awal | Cari ejaan typo RoomMemer persis; verifikasi akhir snapshot |
| `RoomMember roleid = ID`, `AddPlayer roleid = ID` | Pemuatan anggota | AddPlayer juga terjadi saat rebuild role/scene |
| `_OnCmd_RoomLeave_Re id: ID` | Keluar anggota | Bedakan local vs remote |
| `SwitchRoleType` lalu `oper = 1 roleid = LOCAL`, Del/Add ID sama | Pergantian role lokal pada pengujian | Tidak mencetak role tujuan; oper=1 juga untuk event lain |
| `RoomReady`, `oper=2` | Ready | BUKAN bukti role universal |
| `RoomCancelReady`, `oper=3` | Cancel Ready | Bukan Spectator |
| `StartPlayedTime` / `Dancing` | Gameplay/session aktif | Muncul juga saat spectating |
| `_OnCmd_MusicEnd`, Statistics | Sesi berakhir/hasil | Pertahankan role |
| `PlayerType: a` | Berkorelasi spectator dalam sampel | Tidak ada ID lokal eksplisit; bukan enum universal terverifikasi |
| `PlayerManager.Instance.Count` | Count internal sesi | Mencakup spectator dalam pengujian; bukan satu-satunya sumber roster |
| `AppointMaster`, `oper=21 roleid=ID` | Kandidat transfer host | Tidak diperlukan untuk tampilan fase ini |
| oper=12 / oper=13 | Unlock / lock satu slot, user-confirmed | ABAIKAN untuk hitungan orang |
| AddPlayer / DelPlayer tanpa roleid | Muncul pada slot changes | Bukan orang |
| oper=17 / oper=18 | Enter/leave mall pada log lain | Bukan room join/leave |

## Role inference untuk prototipe
User-confirmed: masuk room belum bermain → Joined; masuk room sudah bermain → Spectator; spectator bertahan setelah match selesai sampai menekan Join.
Observed masuk tengah game: RoomEnter → RoomDetailInfo → `_OnCmd_GetGameInfoRe`, `stage=2, stage=c` → langsung RoleCreateRoom ke PlayRoom. Jangan mengartikan huruf stage sebagai mode tanpa bukti.
Role disimpan terpisah dari phase. Inference menunggu rangkaian entry selesai agar WaitRoom sementara tidak menyebabkan salah Joined.
SwitchRoleType = request; toggle hanya setelah balasan lokal yang cocok dalam konteks request, sekali. Jangan toggle setiap oper=1. Jika timeout/ambigu, tandai unknown, jangan tebak.
Perubahan anggota remote oper=1 tidak otomatis berarti toggle role remote; fase ini menghitung semua anggota sehingga tidak perlu role remote.
Role awal tidak dapat dipastikan bila log mulai di tengah sesi/tidak lengkap. Tampilkan unknown atau minta kalibrasi role eksplisit melalui UI; jangan terus menampilkan status palsu.

## Contoh rangkaian terverifikasi
2026-10-15 13:25:31 masuk langsung PlayRoom sebagai spectator (konfirmasi pengguna).
13:29:01 MusicEnd → 13:29:21 WaitRoom (tetap spectator) → 13:29:58 SwitchRoleType + balasan lokal (Join, konfirmasi pengguna).
2026-10-13: role lokal yang diuji 7793881091; JANGAN jadikan default universal.

## Respons log
Tes PowerShell: SwitchRoleType timestamp 14:34:53 terbaca 14:34:53.488; berikutnya 14:35:00 terbaca 14:35:00.767. Ini menunjukkan respons cepat pada sampel, bukan ukuran latency presisi atau SLA. Timestamp wall client hanya detik; `time=` merupakan waktu internal client, bukan Unix timestamp.
