# SPEC: NSUNS Trainer

> Dokumen serah-terima untuk sesi AI berikutnya. BACA SELURUHNYA sebelum menulis apa pun. Terakhir diperbarui: 3 Nov 2026.
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
14. **Jangan memakai Mode Nop** (NOP polos membuat musuh ikut kebal). Trainer ini hanya memakai Mode Tree.
15. **Mencari nilai atau alamat BARU** (fitur baru, game di-update) dilakukan user dengan **RAMREADER-nya** (`D:\Projects\Ramreader`: Cari Nilai, Jalur Stabil, Penulis Kode, Buat Patch). JANGAN menyuruh Cheat Engine dan jangan menebak alamat. NSUNS Trainer hanya membungkus hasil yang sudah ditemukan; alamat baru dimasukkan ke `GameData.cs` dari sesi Ramreader (`%AppData%\Ramreader\sessions\NSUNSC.json`). Jangan mengedit JSON itu lewat ConvertFrom/To-Json.

## 3. Lingkungan

Windows 11, VS Code, PowerShell 5.1, .NET SDK 10.0.x, folder `D:\Projects\NSUNS Trainer`. Paket: WPF-UI 4.3.0, CommunityToolkit.Mvvm 8.4.2, Iced 1.22.0. Target `net10.0-windows`. Menyambung ke proses game butuh Administrator.

## 4. Arsitektur dan kode (semua build lolos, dilaporkan user)

```
D:\Projects\NSUNS Trainer\
  NsunsTrainer.slnx   .gitignore   SPEC.md
  src\NsunsTrainer\    (namespace NsunsTrainer.*)
    Models\    ScanValueType, ModuleInfo, MemoryRegion, SignatureRef, CodePatch (+Hotkey), SavedAddress
    Native\    NativeMethods, ProcessMemory (baca), MemoryWriter (satu-satunya yang menulis ke game)
    Scanning\  AobPattern, AobScanner, SignatureResolver, ValueCodec
    Pointers\  PointerChain, PointerVerifier, AddressResolver (jalur berurutan, lalu signature)
    Cave\      CodeCave (BuildTreeDiag), SideTree, CodePatchLocator
    Data\      GameData (SEMUA data NSUNSC tertanam)
    Services\  HotkeyService, TrainerSession
    ViewModels\ FeatureItem (kartu nilai), PatchItem (patch Tree), MainViewModel
```

Cara kerja patch (jangan diubah tanpa alasan kuat): instruksi penulis (HP, chakra, gauge, Kawarimi) dipakai bersama tim dan musuh. Patch mengganti instruksi dengan `jmp` ke code cave. Cave membandingkan register basis objek penulis dengan hingga 12 slot (alamat karakter satu sisi pemain); sama = lewati penulisan, beda = jalankan instruksi asli. Sisi pemain ditentukan Mode Tree: tiap karakter punya simpul dengan tiga penunjuk (+0x00, +0x08, +0x10) yang saling tersambung timbal balik; awal simpul = nilai register basis dikurangi 0x28. `SideTree.Collect` menelusurinya dari objek pemain (alamat nilai pemain dikurangi displacement instruksi), thread latar memperbarui slot tiap 10 ms. Penanda 12 byte (Mode Team) gagal di mirror match, jangan dipakai.

Isi `GameData.cs`: proses `NSUNSC`; `ExpectedModuleSize` 166223872; kartu nilai: **Gold Ticket** (Int32) dan **Team Bar** (Float, AutoLock "100": dikunci otomatis di 100 sekali per proses game). Patch (semua Mode Tree, hotkey): **God Mode** F2 (+94BAE2 `F3 0F 11 41 10`), **Unlimited Chakra** F3 (+94BA22 `F3 0F 11 41 18`), **Team Tag** F4 (+94BB7A `F3 0F 11 40 2C`, summon), **Tag Switch** F7 (+94C3B6 `F3 0F 11 70 2C`), **Unlimited Kawarimi** F9 (+94C09E `F3 0F 11 51 20`). Kartu alamat "Kawarimi" di data = jalur KAWARIMI 2 dari sesi Ramreader (memang yang dipakai) dan hanya pemilik alamat untuk patch. Cooldown (+21BDC30) jalan buntu, tidak ikut. Lokasi kode dicari lewat signature di modul utama (sekali per proses). Nilai hasil Ramreader tervalidasi di NSUNSC; fitur lain selain yang tercantum di atas tidak ada.

## 5. Status dan langkah

Daftar langkah pribadi user (posisi disebut tiap pesan):

| Langkah | Isi | Status |
|---|---|---|
| R1-4a | Kerangka proyek, paket, folder dan file kosong | SUDAH |
| R1-4b | Models, Native, Scanning, Pointers | SUDAH (build lolos) |
| R1-4c | CodeCave, SideTree, CodePatchLocator, GameData | SUDAH (build lolos) |
| R1-4d | TrainerSession, HotkeyService, FeatureItem, PatchItem, MainViewModel | SUDAH (build lolos) |
| R1-4e | Tampilan: `Themes\AppStyles.xaml` (BELUM dibuat; JANGAN buat .xaml kosong, build gagal), edit `App.xaml` (hubungkan AppStyles, template default dotnet), `MainWindow.xaml` dan `MainWindow.xaml.cs` (buat `MainViewModel`, daftarkan `HotkeyService` di `OnSourceInitialized`, panggil `Shutdown()` di `OnClosed`, seperti Ramreader.Trainer), seragamkan pesan error lama berbahasa Indonesia ke Inggris (`Native\ProcessMemory.cs`, `Native\MemoryWriter.cs`, `Cave\CodePatchLocator.cs`, `Cave\CodeCave.cs`) | BELUM |
| R1-4f | Satu exe untuk dibagikan: `app.manifest` `requireAdministrator`, `AssemblyName` "NSUNS Trainer", publish single-file self-contained win-x64, ikon | BELUM |
| Ikon | Ikon Ramreader, Ramreader.Trainer, dan NSUNS Trainer (Naruto, beda lagi). Desain ANTI AI SLOP. Versi pertama (kurung siku + kotak/segitiga) DITOLAK user seluruhnya, jangan diulang atau diperbaiki; mulai dari konsep baru dan tanyakan acuan gaya (satu aplikasi yang user suka). Ikon belum terpasang di proyek mana pun | BELUM |

Acuan tampilan: `D:\Projects\Ramreader\src\Ramreader.Trainer\MainWindow.xaml` dan `Themes\AppStyles.xaml` (WPF-UI, Mica, dark, kartu `FeatureItem` dan `PatchItem` lewat DataTemplate). Minta file itu lewat perintah clipboard (aturan 8) sebelum menulis XAML. Kontrol WPF biasa tidak mengikuti tema gelap; pakai kontrol WPF-UI (`ui:Button`, `ui:TextBox`, `ui:ToggleSwitch`). Proyek WPF tidak memuat `System.IO` otomatis.

## 6. Yang BELUM terbukti (jangan dicatat terbukti sebelum user melapor)

NSUNS Trainer belum pernah dijalankan di game. Logika yang disalin dari Ramreader terbukti di sana, tetapi versi mandiri ini baru lolos build. Uji setelah R1-4e: tiap patch (tim kebal, musuh kena), Team Bar auto-lock, Gold Ticket, hotkey, di 3 kondisi (match sekarang, rematch, ganti karakter lalu match baru).

## 7. Penutup tiap pekerjaan

Akhiri dengan: **"MASIH ADA KERJA: <apa>"** atau **"SUDAH SELESAI"**. Selalu sebut langkah berikutnya.