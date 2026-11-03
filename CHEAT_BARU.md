# Cara Membuat Cheat Baru

Contoh: uang, tiket lain, atau nilai angka apa pun di game.

Aturan dasar: **alamat dicari dengan Ramreader** (`D:\Projects\Ramreader`), bukan Cheat Engine, dan tidak ditebak. NSUNS Trainer hanya membungkus hasil yang sudah ditemukan. Semua data ditanam di `src\NsunsTrainer\Data\GameData.cs`.

## Jenis cheat

| Jenis | Contoh | Yang dibutuhkan |
|---|---|---|
| Kartu nilai | Gold Ticket, uang, tiket lain | Satu alamat nilai (jalur stabil). Bisa dibaca, di-Set, dan di-Lock. Tanpa hotkey. |
| Baris kunci | Unlimited Team Bar | Alamat nilai + nilai kunci. Tampil di PATCHES dengan hotkey dan toggle. |
| Patch kode | God Mode, Unlimited Chakra | Instruksi penulis nilai (Penulis Kode di Ramreader). Memakai Mode Tree. |

Untuk uang atau tiket, pakai **kartu nilai**. Itu yang paling mudah.

## A. Kartu nilai (uang, tiket)

### 1. Cari alamat di Ramreader
1. Jalankan game dan Ramreader (sebagai Administrator), sambungkan ke proses `NSUNSC`.
2. Pakai **Cari Nilai** dengan angka yang sedang tampil di game (mis. jumlah uang). Ubah nilainya di game (beli atau pakai sesuatu), cari lagi sampai kandidat tinggal sedikit.
3. Catat tipe nilainya: Int32 (bilangan bulat) atau Float (desimal). Uang dan tiket biasanya Int32 (Gold Ticket Int32).
4. Buka **Jalur Stabil** supaya alamat tetap benar setelah game ditutup lalu dibuka lagi, lalu klik **Simpan Alamat** dengan nama jelas, mis. `Gold`.
5. Ramreader menyimpan hasilnya di `%AppData%\Ramreader\sessions\NSUNSC.json`.

Jangan mengedit file JSON itu lewat `ConvertFrom-Json` / `ConvertTo-Json`.

### 2. Masukkan ke GameData.cs
1. Buka `Data\GameData.cs`.
2. Salin entri **Gold Ticket** yang sudah ada (itu contoh yang terbukti jalan).
3. Ganti nama (label), tipe nilai, jalur (chains), dan signature dengan hasil dari sesi Ramreader tadi.
4. Tambahkan label baru ke daftar `FeatureLabels`. Tanpa ini alamatnya ada, tetapi kartunya tidak tampil.
5. Untuk alamat yang hanya jadi pemilik patch, jangan masukkan label ke `FeatureLabels`.

### 3. Aturan nilai
- `FeatureItem` menolak nilai di luar 0 sampai 1.000.000.000. Kalau butuh batas lain, ubah `MinValue` / `MaxValue` di `FeatureItem.cs`.
- Nilai yang dibaca di luar batas dianggap alamat salah, jadi trainer tidak menulis apa pun.

### 4. Hasil di trainer
Kartu baru muncul di bagian VALUES: nilai sekarang, kolom nilai baru, tombol Set, toggle Lock.

## B. Baris kunci (mis. "Unlimited X")

Sama seperti bagian A, lalu tambahkan label ke kamus `AutoLock` di `GameData.cs` dengan nilai kuncinya (mis. `"100"`). Entri dengan AutoLock tampil sebagai baris **"Unlimited <label>"** di PATCHES, toggle-nya mengunci nilai.

Catatan:
- Trainer tidak mengunci otomatis. Pengguna menyalakan sendiri lewat toggle atau hotkey.
- Hotkey bawaan semua baris kunci saat ini **F5**, ditulis tetap di `FeatureItem.cs`. Kalau ada dua baris kunci, keduanya F5 dan bentrok. Ubah salah satunya di kotak hotkey pada aplikasi.
- Hotkey yang diubah disimpan di `%AppData%\NSUNS Trainer\hotkeys.txt` berdasarkan judul baris.

## C. Patch kode (kebal, tanpa cooldown, dan sejenisnya)

Untuk fitur yang harus menghentikan game menulis nilai, dan hanya untuk sisi pemain.

1. Di Ramreader pakai **Penulis Kode** untuk menemukan instruksi yang menulis nilai itu, lalu **Buat Patch**.
2. Instruksi harus memenuhi syarat `CodeCave.TryAnalyze`:
   - minimal 5 byte,
   - satu instruksi utuh, bukan cabang atau call,
   - alamat memakai register basis 64-bit (mis. `[rcx+10]`),
   - tanpa register indeks, tanpa alamat relatif RIP.
3. Pakai **Mode Tree** saja. Jangan Mode Nop: musuh ikut kebal.
4. Isi data patch: label, pola byte (signature), offset instruksi, byte asli, dan hotkey bawaan. Tulis di entri alamat pemiliknya (daftar `CodePatches`). Salin dari patch yang sudah ada, mis. God Mode.
5. Pola byte harus cocok di **tepat satu** tempat di modul utama game. Kalau nol atau lebih dari satu, trainer menolak memakainya.

## D. Setelah mengubah kode

1. Tutup trainer. Kalau exe di folder `publish` masih berjalan, build ulang gagal "access denied".
2. Build dan tes:
   ```powershell
   dotnet build
   ```
3. Jalankan exe dari folder `publish` (butuh Administrator, `dotnet run` dari terminal biasa gagal karena manifest).
4. Tes dengan game berjalan: nilai terbaca, Set berhasil, Lock bertahan, hotkey bekerja. Untuk patch: tim kebal, musuh tidak.
5. Tes di tiga kondisi: match sekarang, rematch, ganti karakter lalu match baru.

## E. Rilis versi baru

1. Naikkan `<Version>` di `NsunsTrainer.csproj` (mis. 1.1.0).
2. Publish:
   ```powershell
   dotnet publish src\NsunsTrainer\NsunsTrainer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "D:\Projects\NSUNS Trainer\publish"
   ```
3. Commit, push, lalu buat release baru di GitHub dengan tag `v1.1.0` dan unggah exe.
4. Tambahkan fitur baru ke tabel Features di `README.md`.

## F. Kalau game di-update

1. Peringatan versi game muncul kalau ukuran exe game berbeda dari `ExpectedModuleSize`.
2. Cari ulang alamat dan pola di Ramreader, perbarui `GameData.cs`, lalu perbarui `ExpectedModuleSize`.
3. Fitur yang polanya tidak ketemu atau cocok di banyak tempat tidak jalan sampai diperbarui.

## Hal yang dilarang

- Menebak alamat atau memakai Cheat Engine.
- Menghapus alamat, patch, atau kartu yang sudah berfungsi.
- Memakai Mode Nop.
- Mengedit `NSUNSC.json` lewat ConvertFrom/To-Json.