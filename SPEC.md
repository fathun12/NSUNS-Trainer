# SPEC: NSUNS Trainer

> Dokumen serah-terima untuk sesi AI berikutnya. BACA SELURUHNYA sebelum menulis apa pun. Terakhir diperbarui: 4 Nov 2026 (Unlimited Awakening dan Instant Awakening, urutan daftar baru, rilis v1.1.1; Damage Multiplier dan One Hit Kill, rilis v1.2.0; efek suara, rilis v1.2.1; baca bagian 9, 10 dan 11).
> Proyek ini MANDIRI. Proyek lain milik user (Ramreader, `D:\Projects\Ramreader`) punya SPEC.md sendiri. JANGAN mengedit SPEC Ramreader kecuali user menyuruh.

## 0. Prompt pembuka untuk sesi baru

"Ini spec proyek NSUNS Trainer. Baca semua, terutama bagian 2 (aturan kerja) dan 5 (status). Lanjutkan dari langkah pertama yang belum selesai. Ulangi dulu aturan kerjamu supaya aku tahu kamu paham. Jangan membangun apa pun sebelum aku minta."

## 1. Tujuan

Satu file `.exe` khusus game NSUNSC (Naruto x Boruto: Ultimate Ninja Storm Connections, proses `NSUNSC`) yang bisa user BAGIKAN. Tidak membaca JSON, tidak butuh Ramreader: semua alamat, signature, patch, dan hotkey sudah TERTANAM di `Data\GameData.cs`. Ini tujuan user sejak awal membuat Ramreader.

Perilaku wajib (sudah terbukti di Ramreader, jangan diubah): patch berlaku untuk SELURUH sisi pemain (tim, termasuk sub-karakter, termasuk mirror match) dan musuh TIDAK kebal.

## 2. Aturan kerja dengan user (WAJIB)

User berbahasa Indonesia, awam, mudah frustrasi. HURUF KAPITAL SEMUA = marah karena AI tidak membaca, melompat, atau berinisiatif sendiri. Token user terbatas.

1. **Bahasa:** balas dalam bahasa Indonesia. Kode, komentar, nama commit, dan teks UI dalam bahasa Inggris.
2. **Singkat.** Jawaban dulu (sudah/belum), detail seperlunya. Tidak ada pembuktian pengetahuan yang tidak diminta.
3. **Sebut posisi di daftar langkah di SETIAP pesan**, mis. "Ramreader R1, langkah R1-4e dari 6". Katakan jelas "sudah" bila langkah selesai, "belum" bila belum, beserta langkah berikutnya.
4. **Akhiri SETIAP kerja dengan pernyataan eksplisit: "MASIH ADA KERJA: ..." atau "SUDAH SELESAI".** Jangan menggantung.
5. **Jangan berinisiatif.** Jangan membangun, menghapus, menyentuh SPEC, atau menyuruh hal yang tidak diminta. Desain dulu, bangun kalau diminta. Kalau ragu apakah sudah disuruh, tanya (maksimal SATU pertanyaan per pesan, dalam teks biasa dengan rekomendasi, bukan tombol).
6. **Kode ditulis langsung di chat** sebagai blok kode dengan path lengkap di atasnya. Folder dan file KOSONG dibuat lewat SATU perintah PowerShell 5.1 (New-Item, hindari `&&`). Jangan membuat file atau kartu unduhan kecuali diminta.
7. **Ubah file yang sudah ada dengan format Sebelum/Sesudah.** Blok "Sebelum" harus PERSIS ada di file. Sebelum memberi edit untuk file yang pernah dikirim, MINTA isi terbarunya dulu (file di komputer user sudah berubah; jangan berasumsi dari versi lama).
8. **Meminta file:** jelaskan alasannya, lalu beri SATU perintah PowerShell yang menyalin ke clipboard: `Get-Content -LiteralPath ... -Raw -Encoding utf8`, header `=== path ===`, lalu `Set-Clipboard`. Kumpulkan semua yang dibutuhkan dalam satu pesan. Jangan minta CODE_SNAPSHOT.md.
9. **Jangan pernah menyuruh `git push`.** Jangan menulis atau mengubah file .md kecuali diminta.
10. **Build/tes:** beri perintah `dotnet build` (terminal VS Code di `D:\Projects\NSUNS Trainer`). Minta user melaporkan hanya "Build succeeded" atau error persis. Tes di game: trainer harus dijalankan sebagai Administrator.
11. **Jujur.** Pisahkan jelas yang terbukti dari layar user dan yang tebakan (beri label). Jangan mencatat "terbukti" sebelum user melapor. Laporan user dari pengalamannya sendiri adalah fakta; jangan dibantah.
12. **Baca pesan sampai habis dan jangan mengubah arti kalimat user.** Salah baca = frustrasi.
13. **Jangan pernah menghapus atau menganggap tak berfungsi apa pun hasil kerja user** (alamat, patch, kartu). Team Bar dan Unlimited Team Bar berfungsi. AI lama pernah salah menganggap Team Bar tidak jalan dan hendak menghapusnya; user marah besar. Hapus hanya jika user sendiri yang menyuruh.
14. **Jangan memakai Mode Nop** (NOP polos membuat musuh ikut kebal). Trainer ini hanya memakai Mode Tree dan Mode TreeOwner (hook pada call, khusus Unlimited Awakening).
15. **Mencari nilai atau alamat BARU** (fitur baru, game di-update) dilakukan user dengan **RAMREADER-nya** (`D:\Projects\Ramreader`: Cari Nilai, Jalur Stabil, Penulis Kode, Buat Patch). JANGAN menyuruh Cheat Engine dan jangan menebak alamat. NSUNS Trainer hanya membungkus hasil yang sudah ditemukan; alamat baru dimasukkan ke `GameData.cs` dari sesi Ramreader (`%AppData%\Ramreader\sessions\NSUNSC.json`). Jangan mengedit JSON itu lewat ConvertFrom/To-Json.

## 3. Lingkungan

Windows 11, VS Code, PowerShell 5.1, .NET SDK 10.0.x, folder `D:\Projects\NSUNS Trainer`. Paket: WPF-UI 4.3.0, CommunityToolkit.Mvvm 8.4.2, Iced 1.22.0. Target `net10.0-windows`. Menyambung ke proses game butuh Administrator.

## 4. Arsitektur dan kode (semua build lolos, dilaporkan user)

```
D:\Projects\NSUNS Trainer\
  NsunsTrainer.slnx   .gitignore   SPEC.md
  src\NsunsTrainer\    (namespace NsunsTrainer.*)
    Models\    ScanValueType, ModuleInfo, MemoryRegion, SignatureRef, CodePatch (+Hotkey, Mode, BaseDisp, OwnerOffset), SavedAddress
    Native\    NativeMethods, ProcessMemory (baca), MemoryWriter (satu-satunya yang menulis ke game)
    Scanning\  AobPattern, AobScanner, SignatureResolver, ValueCodec
    Pointers\  PointerChain, PointerVerifier, AddressResolver (jalur berurutan, lalu signature)
    Cave\      CodeCave (BuildTreeDiag, BuildCallOwner, BuildTreeDamage), SideTree, CodePatchLocator
    Data\      GameData (SEMUA data NSUNSC tertanam)
    Services\  HotkeyService, TrainerSession, Sfx
    ViewMod    ViewModels\ FeatureItem (kartu nilai), PatchItem (patch Tree dan TreeOwner), InstantAwakeningItem, IToggleRow (antarmuka baris switch), MainViewModelels\ FeatureItem (kartu nilai), PatchItem (patch Tree), MainViewModel
```

Cara kerja patch (jangan diubah tanpa alasan kuat): instruksi penulis (HP, chakra, gauge, Kawarimi) dipakai bersama tim dan musuh. Patch mengganti instruksi dengan `jmp` ke code cave. Cave membandingkan register basis objek penulis dengan hingga 12 slot (alamat karakter satu sisi pemain); sama = lewati penulisan, beda = jalankan instruksi asli. Sisi pemain ditentukan Mode Tree: tiap karakter punya simpul dengan tiga penunjuk (+0x00, +0x08, +0x10) yang saling tersambung timbal balik; awal simpul = nilai register basis dikurangi 0x28. `SideTree.Collect` menelusurinya dari objek pemain (alamat nilai pemain dikurangi displacement instruksi), thread latar memperbarui slot tiap 10 ms. Penanda 12 byte (Mode Team) gagal di mirror match, jangan dipakai.

Isi `GameData.cs`: proses `NSUNSC`; `ExpectedModuleSize` 166223872; kartu nilai: **Gold Ticket** (Int32) dan **Team Bar** (Float, tampil sebagai baris patch "Unlimited Team Bar", hotkey default F5, toggle = kunci di 100; TIDAK lagi nyala otomatis, user tidak mau). Patch (semua Mode Tree, hotkey): **God Mode** F2 (+94BAE2 `F3 0F 11 41 10`), **Unlimited Chakra** F3 (+94BA22 `F3 0F 11 41 18`), **Team Tag** F4 (+94BB7A `F3 0F 11 40 2C`, summon), **Tag Switch** F7 (+94C3B6 `F3 0F 11 70 2C`), **Unlimited Kawarimi** F9 (+94C09E `F3 0F 11 51 20`). Kartu alamat "Kawarimi" di data = jalur KAWARIMI 2 dari sesi Ramreader (memang yang dipakai) dan hanya pemilik alamat untuk patch. Cooldown (+21BDC30) jalan buntu, tidak ikut. Lokasi kode dicari lewat signature di modul utama (sekali per proses). Nilai hasil Ramreader tervalidasi di NSUNSC; fitur lain selain yang tercantum di atas dan di bagian 9 tidak ada. Sejak 4 Nov 2026 ada kartu nilai HP (tersembunyi, tidak tampil di VALUES; dipakai Instant Awakening untuk Set dan Lock) dan patch **Unlimited Awakening** (Mode TreeOwner) di dalam kartu HP. Jadi "semua patch Mode Tree" di atas berlaku untuk semua kecuali Unlimited Awakening (TreeOwner) dan Damage Multiplier serta One Hit Kill (TreeDamage). Detail di bagian 9 dan 10.

## 5. Status dan langkah

Daftar langkah pribadi user (posisi disebut tiap pesan):

| Langkah | Isi | Status |
|---|---|---|
| R1-4a | Kerangka proyek, paket, folder dan file kosong | SUDAH |
| R1-4b | Models, Native, Scanning, Pointers | SUDAH (build lolos) |
| R1-4c | CodeCave, SideTree, CodePatchLocator, GameData | SUDAH (build lolos) |
| R1-4d | TrainerSession, HotkeyService, FeatureItem, PatchItem, MainViewModel | SUDAH (build lolos) |
| R1-4e | Tampilan datar (AppStyles.xaml, App.xaml, MainWindow), hotkey bisa diubah di baris dan tersimpan di %AppData%\NSUNS Trainer\hotkeys.txt, pesan error diseragamkan ke Inggris | SUDAH (build lolos, tampilan dilihat user) |
| R1-4f | app.manifest requireAdministrator, AssemblyName "NSUNS Trainer", publish single-file self-contained win-x64 (folder publish, tidak masuk git) | SUDAH (exe tes: minta Administrator, tampilan sama, tersambung ke NSUNSC, laporan user) |
| Ikon | Spiral kotak #E4572E di atas #141211, Assets\NsunsTrainer.ico, dipasang lewat ApplicationIcon (gaya acuan: gambar user, disetujui, jadi standar semua trainer, lihat DESIGN.md bagian 10) | SUDAH dipasang di exe (tampilan ikon belum dilaporkan user) |
| W1-W6 | Unlimited Awakening (TreeOwner) dan Instant Awakening, daftar switch satu urutan (IToggleRow, ToggleOrder), kartu HP tersembunyi | SUDAH (build lolos; semua fungsi trainer berjalan, tanpa error dan tanpa nilai tidak sesuai, dilaporkan user 4 Nov 2026; tes tiga kondisi tidak dirinci). Lihat bagian 9 |
| Rilis | v1.0.0 (exe 64,7 MB) dan v1.1.1 (exe 61,1 MB, commit 874de3c) terbit di GitHub (screenshot user) | SUDAH. Lihat bagian 9 |
| D1-D3 | Damage Multiplier dan One Hit Kill (Mode TreeDamage, hook +94BADB) | SUDAH (build lolos; 20+ match, berfungsi tanpa konflik, dilaporkan user 4 Nov 2026; tiga kondisi tidak dirinci). Lihat bagian 10 |
| Rilis | v1.2.0 terbit di GitHub (commit 131d849, exe 61,1 MB, Latest, screenshot user) | SUDAH. Lihat bagian 10 |
| S1-S3 | Efek suara saat switch dinyalakan atau dimatikan (Services\Sfx.cs, WAV tertanam di exe) | SUDAH (build lolos; user melapor "udah menyala!" pada 5 Nov 2026; rincian tesnya tidak dilaporkan). Lihat bagian 11 |
| Rilis | v1.2.1: perintah publish, deskripsi, dan tag disiapkan | Terbitnya di GitHub belum dilaporkan user. Lihat bagian 11 |

Acuan tampilan: `D:\Projects\Ramreader\src\Ramreader.Trainer\MainWindow.xaml` dan `Themes\AppStyles.xaml` (WPF-UI, Mica, dark, kartu `FeatureItem` dan `PatchItem` lewat DataTemplate). Minta file itu lewat perintah clipboard (aturan 8) sebelum menulis XAML. Kontrol WPF biasa tidak mengikuti tema gelap; pakai kontrol WPF-UI (`ui:Button`, `ui:TextBox`, `ui:ToggleSwitch`). Proyek WPF tidak memuat `System.IO` otomatis.

## 6. Yang BELUM terbukti (jangan dicatat terbukti sebelum user melapor)

Untuk Damage Multiplier dan One Hit Kill, yang belum terbukti ada di bagian 10; untuk efek suara di bagian 11.

NSUNS Trainer sudah dijalankan: tersambung ke NSUNSC dan tampil benar (laporan user, 3 Nov 2026). Pada 4 Nov 2026 user melaporkan semua fungsi trainer berjalan (setelah penambahan Awakening), tanpa error dan tanpa nilai tidak sesuai; rincian per patch dan per kondisi tetap tidak diberikan, jadi tiga kondisi di bawah masih dianggap belum terbukti rinci. Logika yang disalin dari Ramreader terbukti di sana, tetapi versi mandiri ini baru lolos build. Uji setelah R1-4e: tiap patch (tim kebal, musuh kena), Team Bar auto-lock, Gold Ticket, hotkey, di 3 kondisi (match sekarang, rematch, ganti karakter lalu match baru).

## 7. Penutup tiap pekerjaan

Akhiri dengan: **"MASIH ADA KERJA: <apa>"** atau **"SUDAH SELESAI"**. Selalu sebut langkah berikutnya.

## 8. Catatan sesi 3 Nov 2026

- Standar tampilan dan ikon semua trainer user ada di `DESIGN.md` (sumber: `Themes\AppStyles.xaml`). Aksen warna mengikuti game, gaya UI dan ikon sama.
- Deskripsi: Team Tag = memanggil teman (RB/LB atau Q/U) tanpa cooldown. Tag Switch = ganti karakter di mode tim tanpa cooldown.
- Hotkey default: God Mode F2, Unlimited Chakra F3, Team Tag F4, Unlimited Team Bar F5, Tag Switch F7, Unlimited Kawarimi F9. Semua bisa diubah di baris.
- Ada file `README.md`, `DESIGN.md`, `LICENSE` (gratis, tidak boleh dijual, berlaku untuk exe dan ikon, hak cipta MFNiam / fathun12).
- Tes lewat exe di folder publish (butuh Administrator). `dotnet run` dari terminal biasa gagal karena manifest.
- Build ulang gagal "access denied" kalau exe di publish masih berjalan: tutup dulu.
- Berikutnya: release GitHub v1.0.0 (user membuatnya sendiri di web), lalu ikon di title bar jendela bila user minta. (v1.0.0 sudah terbit; lihat bagian 9.)

## 9. Catatan sesi 4 Nov 2026 (Awakening dan rilis v1.1.1)

Label: FAKTA = laporan atau screenshot user; DUGAAN = belum terbukti; TIDAK TAHU = jangan dianggap benar.

**Yang ditambahkan (build lolos, dilaporkan user).**
- **Unlimited Awakening** (hotkey bawaan kosong): patch di kartu HP, Mode "TreeOwner", BaseDisp 16, OwnerOffset 48, hook pada `call` di +97B965 (`E8 16 45 05 00`, InstructionOffset 4, pola AOB dari JSON Ramreader). Cave `CodeCave.BuildCallOwner` memanggil target asli; bila hasilnya 0 dan `[rdi+18h]` sama dengan salah satu slot (penunjuk pemilik efek tiap karakter sisi pemain, `[simpul+0x30]`), hasil dipaksa 1 sehingga game melewati pengurangan timer. Timer efek sisi pemain tidak habis, efek musuh tetap habis. Asal logika dan bukti di SPEC Ramreader bagian 2o dan 2q.
- **Instant Awakening** (hotkey bawaan kosong): satu switch, bukan patch game. Dinyalakan: God Mode dimatikan bila menyala; HP diset 40 lalu dikunci; Unlimited Awakening dinyalakan. Dimatikan: kunci HP dilepas dan Unlimited Awakening mati; God Mode TIDAK dinyalakan lagi. Tiap detik, bila switch menyala tetapi kunci HP atau patch mati atau gagal, semuanya dilepas otomatis. Pemicu Awakening di game tetap ditekan user sendiri. Konstanta HP = `GameData.AwakeningHp` ("40").
- **Daftar switch satu urutan** (`GameData.ToggleOrder`): God Mode, Unlimited Chakra, Instant Awakening, Unlimited Awakening, Team Tag, Tag Switch, Unlimited Kawarimi, Unlimited Team Bar. Judul yang tidak terdaftar dibaris terakhir. `MainViewModel.Toggles` (IToggleRow) menggantikan daftar Switch dan Patches lama di MainWindow.xaml; hotkey lama tetap dimuat dari hotkeys.txt per judul.
- **Kartu HP tersembunyi**: `GameData.HiddenLabels = {"HP"}`, `FeatureItem.IsHidden`, tidak tampil di VALUES tetapi tetap di-update dan bisa dikunci.
- Berkas baru: `ViewModels\IToggleRow.cs`, `ViewModels\InstantAwakeningItem.cs`. `CodePatch` punya field Mode (bawaan "Tree"), BaseDisp, OwnerOffset.

**FAKTA (laporan user, 4 Nov 2026).** Semua fungsi trainer berjalan, tidak ada error, tidak ada nilai tidak sesuai. Screenshot Trainer: Unlimited Awakening aktif dengan diagnostik "forced 12800x, left alone 900x", Instant Awakening aktif ("HP: Value locked at 40").

**Belum terbukti.** Tiga kondisi (match sekarang, rematch, ganti karakter lalu match baru) untuk Awakening tidak dirinci user. Tes exe berdiri sendiri belum dilaporkan (lihat rilis di bawah). TIDAK TAHU: apakah syarat Awakening persis HP 40 atau di bawah 40 (konstanta bisa diubah); apakah sub-karakter punya penunjuk pemilik di +0x30 (user melaporkan tim ikut tanpa batas, belum dirinci); arti kalimat user "darah diset tapi tetap god mode" (tidak dimasukkan ke deskripsi rilis).

**Rilis.** Folder rilis adalah `D:\Projects\NSUNS Trainer\publish` (di dalam proyek, tidak masuk git). JANGAN membuat folder rilis baru di luar proyek (AI sesi ini membuat `NSUNS Trainer-dist` dan `public`, user marah; keduanya dihapus). Perintah publish yang menghasilkan exe 61,1 MB: `dotnet publish src\NsunsTrainer -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:Version=$ver -o publish`. Tanpa `EnableCompressionInSingleFile` exe 134 MB. Opsi publish asli v1.0.0 (64,7 MB) tidak diketahui. `NsunsTrainer.csproj` masih `<Version>1.0.0</Version>`; versi rilis datang dari `-p:Version`. Folder publish juga berisi beberapa .dll dan .pdb di samping exe; hanya `NSUNS Trainer.exe` yang diunggah ke GitHub. DUGAAN: opsi kompresi membuat library bawaan WPF tidak tertanam; BELUM dibuktikan bahwa exe berjalan sendirian tanpa .dll itu (tes: salin exe ke folder kosong, jalankan sebagai Administrator). Release v1.1.0 sempat berisi exe 134 MB lalu diganti 61,1 MB; v1.1.1 terbit sebagai Latest (screenshot user). Commit "chore: revert gitignore" sempat memasukkan `.gitignore.bak`; user menghapus berkasnya dan commit penghapusannya belum dilaporkan.

**Pelajaran.** Jangan membuat folder atau berkas baru di luar struktur proyek yang sudah ada tanpa bertanya; baca dulu SPEC dan struktur folder (aturan 5).

## 10. Catatan sesi 4 Nov 2026 (Damage Multiplier, One Hit Kill, rilis v1.2.0)

Label: FAKTA = laporan atau screenshot user; DUGAAN = belum terbukti; TIDAK TAHU = jangan dianggap benar.

**Yang ditambahkan (build lolos, dilaporkan user).**
- **Hook baru di +94BADB, 7 byte** (`F3 0F 5C C2 0F 2F C8` = `subss xmm0,xmm2` + `comiss xmm1,xmm0`) di dalam fungsi pengurang HP (+94BA90). xmm2 = besar damage yang masuk, rcx = basis node (sudah +28h), xmm1 = 0. Bongkaran dari Lihat Kode Ramreader (user). Penulis HP +94BAE2 (God Mode) ada tepat sesudahnya, jadi kedua patch tidak tumpang tindih.
- **Mode "TreeDamage"** (CodePatch.Mode, bersama Tree dan TreeOwner). `CodeCave.BuildTreeDamage`: tiap slot sisi pemain dibandingkan dengan rcx; sama = jalankan kode asli (kamu kena damage normal); beda (musuh) dan damage > 0 = `mulss xmm2,[k]` sebelum kode asli. Tanpa pushfq (comiss asli mengisi ulang flag). Blok data 16 byte: +0 scaled, +4 left alone, +8 float k. Field baru `CodePatch.Multiplier` dan `Adjustable`, konstanta `CodeCave.DamageSiteBytes`. `PatchItem`: pemeriksa instruksi dilewati untuk mode ini (CaveInfo(1, BaseDisp, 7), BaseDisp 16), k ditulis ke cave oleh thread latar saat angka berubah (jadi bisa diubah selagi menyala), pesan khusus bila patch damage lain sedang aktif.
- **Dua patch di kartu HP** (GameData): "Damage Multiplier" (bawaan x2, rentang 1 sampai 10, desimal boleh dengan titik atau koma, kotak "Damage x" di baris; tersimpan di `%AppData%\NSUNS Trainer\multiplier.txt`, bentuk `Judul=Nilai`) dan "One Hit Kill" (k = 1.000.000.000, tanpa kotak). Pola: 12 `??` lalu `76 ?? C7 41 10 00 00 00`, InstructionOffset 0. Lokasi kode sama, jadi hanya satu yang bisa menyala sekaligus. Hotkey bawaan kosong. `ToggleOrder` baru: God Mode, Unlimited Chakra, Damage Multiplier, One Hit Kill, Instant Awakening, Unlimited Awakening, Team Tag, Tag Switch, Unlimited Kawarimi, Unlimited Team Bar. File yang berubah: CodePatch.cs, CodeCave.cs, PatchItem.cs, GameData.cs, MainViewModel.cs (LoadMultipliers/SaveMultipliers), MainWindow.xaml.

**FAKTA (laporan user, 4 Nov 2026).** Lebih dari 20 match: One Hit Kill dan Damage Multiplier berfungsi, tidak ada konflik. Semua cheat dinyalakan bersamaan (pasangan Damage Multiplier / One Hit Kill dan God Mode / Instant Awakening digilir karena tidak bisa menyala bersamaan): aman. Tidak ada musuh yang terkena efek trainer (sisi pemain tidak salah alamat), dan tim tetap menerima efek. Rilis v1.2.0 terbit (screenshot: commit 131d849, exe 61,1 MB, Latest). Perintah publish sama seperti v1.1.1, dengan `-p:Version=1.2.0`.

**Belum terbukti.** Tiga kondisi (match sekarang, rematch, ganti karakter lalu match baru) tidak dirinci user, hanya "20+ match". DUGAAN: xmm2 tidak dipakai lagi sesudah fungsi selesai (aturan register volatile Windows x64; tidak ada gejala dilaporkan). TIDAK TAHU: apakah semua jenis serangan (Ougi, cutscene, Awakening) lewat +94BA90. Fungsi kembar +94BB00 (`rax`, `xmm6`, `[rax+10h]`) dan +94BA40 (varian chakra) tidak di-hook; kalau ada serangan yang tidak terkali atau tidak langsung KO, kemungkinan lewat sana. Apakah KO lewat One Hit Kill memicu hasil dan skor normal: tidak dirinci.

## 11. Catatan sesi 5 Nov 2026 (efek suara, rilis v1.2.1)

Label: FAKTA = laporan atau hasil perintah user; DUGAAN = belum terbukti; TIDAK TAHU = jangan dianggap benar.

**Yang ditambahkan (build lolos, dilaporkan user).**
- **Efek suara** seperti WeMod: satu bunyi saat switch dinyalakan, bunyi lain saat dimatikan, untuk semua switch di daftar (termasuk Instant Awakening), lewat klik maupun hotkey. Pelepasan otomatis (game ditutup, Instant Awakening lepas sendiri) tidak berbunyi.
- `Services\Sfx.cs` (berkas baru): dua `SoundPlayer` statis dari resource tertanam `Sounds.enable.wav` dan `Sounds.disable.wav`. `Play(bool)` memutar; `PlayWhenSettled(IToggleRow, bool wanted)` menunda lewat `Dispatcher.BeginInvoke` (prioritas Background) dan memutar hanya bila `row.IsOn == wanted`, jadi switch yang ditolak dan kembali ke posisi semula tidak berbunyi (mis. Damage Multiplier saat One Hit Kill menyala).
- `NsunsTrainer.csproj`: dua `EmbeddedResource` dari `Assets\Sounds\enable.wav` dan `Assets\Sounds\disable.wav`, dengan `LogicalName` tetap. Exe tetap satu berkas, tanpa berkas lepas.
- `MainWindow.xaml`: `ToggleSwitch` di daftar punya `Click="OnToggleClick"`. `MainWindow.xaml.cs`: `OnToggleClick` memanggil `Sfx.PlayWhenSettled(row, toggle.IsChecked == true)`. `MainViewModel.AttachHotkeys`: aksi hotkey dibungkus (`wanted = !row.IsOn`, `HotkeyToggle()`, `PlayWhenSettled`). `HotkeyService` menjalankan aksi di `WndProc` (thread UI).
- **Berkas suara:** dari Mixkit (halaman unduhan user: https://mixkit.co/free-sound-effects/ding/), disalin dari Unduhan (`enable_sxf.wav`, `disable_sfx.wav`) ke `Assets\Sounds\enable.wav` dan `disable.wav`. Format hasil perintah user: PCM, 16-bit, stereo, 44,1 kHz. `enable.wav` 769788 byte (sekitar 4,4 detik, hitungan AI), `disable.wav` 246744 byte (sekitar 1,4 detik). `SoundPlayer` hanya bisa memutar WAV PCM.
- **Lisensi (ringkasan sumber sekunder, bukan teks resmi; AI tidak berhasil membuka halaman lisensi Mixkit):** Mixkit Sound Effects Free License memperbolehkan pemakaian pribadi dan komersial, atribusi dihargai tetapi tidak wajib, dan berkas mentah tidak boleh dibagikan berdiri sendiri. User memasang kredit satu baris di catatan rilis: "Sound effects via Mixkit" dengan link halaman unduhan. AI bukan penasihat hukum.

**FAKTA (laporan user, 5 Nov 2026).** Setelah build dan tes, user melapor "udah menyala!". Rincian tes (klik, hotkey, switch ditolak, pelepasan otomatis) tidak dilaporkan satu per satu.

**Rilis v1.2.1.** Disiapkan: perintah publish sama seperti sebelumnya dengan `$ver = "1.2.1"`, commit "feat: on/off sound effects for switches (v1.2.1)", tag `v1.2.1`, kredit Mixkit di bagian Notes. `NsunsTrainer.csproj` tetap `<Version>1.0.0</Version>`, versi datang dari `-p:Version`. Ukuran exe sekitar 62 MB hanya perkiraan AI (naik sekitar 1 MB karena WAV); ukuran aktual tidak dilaporkan. Apakah rilis sudah terbit di GitHub: tidak dilaporkan.

**Belum terbukti.** DUGAAN: `ToggleSwitch` WPF-UI memicu `Click` hanya saat diklik user, bukan saat nilainya berubah lewat binding (tidak dilaporkan terpisah). TIDAK TAHU: apakah switch yang ditolak benar-benar diam; apakah bunyi nyala (sekitar 4,4 detik) terlalu panjang; apakah WAV mentah yang ikut di repo dan "Source code zip" sesuai lisensi Mixkit. Volume tidak bisa diatur dari trainer (`SoundPlayer`); tidak ada pengaturan senyap.
