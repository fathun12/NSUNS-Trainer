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
Buka `src\NsunsTrainer\Data\GameData.cs`. Ada dua tempat yang diubah.

**Tempat 1: tambah entri alamat** di daftar `Addresses`. Letakkan setelah entri terakhir (beri koma setelah kurung `}` entri sebelumnya). Bentuknya sama dengan Gold Ticket:

```csharp
new SavedAddress
{
    Label = "NAMA_KARTU",
    ValueType = ScanValueType.Int32,
    MainModuleSize = ExpectedModuleSize,
    Chains =
    {
        "NSUNSC.exe|OFFSET_BASE|OFFSET1,OFFSET2"
    }
},
```

Penjelasan (semua tulisan huruf kapital di atas adalah tempat pengganti, bukan alamat asli):
- `Label`: nama yang tampil di kartu.
- `ValueType`: `Int32` untuk bilangan bulat (Gold Ticket), `Float` untuk desimal (HP, Team Bar).
- `Chains`: satu baris per jalur stabil dari Ramreader. Formatnya `NSUNSC.exe|alamat-statis-hex|offset-hex,offset-hex`. Kalau alamatnya statis tanpa offset, bagian offset dikosongkan, seperti `"NSUNSC.exe|21DA00C|"` di Gold Ticket.
- Boleh memasukkan banyak jalur. Trainer mencoba satu per satu sampai ada yang cocok. Gold Ticket punya 17 jalur.
- `Signatures` tidak wajib untuk kartu nilai. Gold Ticket dan Team Bar tidak memakainya.

**Tempat 2: daftarkan kartunya** di `FeatureLabels` (dekat bagian atas file):

```csharp
public static readonly string[] FeatureLabels = { "Gold Ticket", "Team Bar", "NAMA_KARTU" };
```

Tanpa langkah ini alamatnya ada tetapi kartunya tidak tampil di trainer.
### 3. Aturan nilai
- `FeatureItem` menolak nilai di luar 0 sampai 1.000.000.000. Kalau butuh batas lain, ubah `MinValue` / `MaxValue` di `FeatureItem.cs`.
- Nilai yang dibaca di luar batas dianggap alamat salah, jadi trainer tidak menulis apa pun.

### 4. Hasil di trainer
Kartu baru muncul di bagian VALUES: nilai sekarang, kolom nilai baru, tombol Set, toggle Lock.

## B. Baris kunci (mis. "Unlimited X")

Ini yang dipakai Unlimited Team Bar. Alamatnya dibuat seperti bagian A (entri `SavedAddress` dan `FeatureLabels`), lalu tambahkan satu hal lagi: nilai kuncinya di kamus `AutoLock`:

```csharp
public static readonly Dictionary<string, string> AutoLock = new() { ["Team Bar"] = "100", ["NAMA_KARTU"] = "NILAI_KUNCI" };
```

Efeknya:
- Kartu itu pindah dari bagian VALUES ke PATCHES sebagai baris **"Unlimited NAMA_KARTU"** dengan kotak hotkey dan toggle.
- Toggle menyala = nilai dikunci di `NILAI_KUNCI`.
- Trainer **tidak** mengunci otomatis. Pengguna menyalakan sendiri lewat toggle atau hotkey.
- Hotkey bawaan semua baris kunci sekarang **F5**, ditulis tetap di `FeatureItem.cs`. Kalau ada dua baris kunci, keduanya F5 dan bentrok. Ubah salah satunya di kotak hotkey aplikasi.
- Hotkey yang diubah tersimpan di `%AppData%\NSUNS Trainer\hotkeys.txt` berdasarkan judul baris.
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