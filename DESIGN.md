# DESIGN: Standar Tampilan Trainer

Standar ini dipakai untuk semua trainer milik user (NSUNS Trainer dan seterusnya). Sumber kebenaran: `src\<Proyek>\Themes\AppStyles.xaml`. Salin file itu ke trainer baru, jangan menggambar ulang dari nol.

## 1. Prinsip

1. **Datar dan hening.** Tanpa kartu, tanpa bayangan, tanpa gradien, tanpa Mica. Isi berupa daftar baris yang dipisah garis tipis.
2. **Satu aksen.** Warna aksen hanya untuk status aktif (garis kiri baris, toggle, tombol utama). Bukan hiasan.
3. **Ringan.** Jendela kecil dan tetap, tanpa panel samping, tanpa pengaturan yang tidak perlu. Data game tertanam di kode.
4. **Jelas untuk orang awam.** Satu baris = satu fitur. Nama, status satu kalimat, toggle. Hotkey bisa diubah langsung di baris.
5. **Teks UI bahasa Inggris.**

## 2. Palet (token di AppStyles.xaml)

| Token | Warna | Dipakai untuk |
|---|---|---|
| `BgBrush` | `#141211` | Latar jendela (hitam hangat, bukan abu biru) |
| `StripBrush` | `#2A1D17` | Latar strip peringatan |
| `LineBrush` | `#2B2724` | Garis pemisah baris |
| `TextBrush` | `#EDE8E0` | Teks utama (putih tulang) |
| `MutedBrush` | `#8C857B` | Teks status dan keterangan |
| `AccentBrush` | `#E4572E` | Aksen hanko merah-jingga |
| `DotOnBrush` | `#6FCF97` | Titik "terhubung ke game" |

Aksen WPF-UI (toggle, tombol Primary) diseragamkan di konstruktor `MainWindow`:
`ApplicationAccentColorManager.Apply(Color.FromRgb(0xE4, 0x57, 0x2E), ApplicationTheme.Dark);`
Setiap trainer boleh punya satu aksen sendiri, tapi hanya satu.

## 3. Huruf

- Teks: Segoe UI Variable Text, cadangan Segoe UI.
- Angka dan hotkey: Cascadia Mono, cadangan Consolas (`MonoFont`).
- Ukuran: judul baris 14 SemiBold, status 11.5, label bagian 11 SemiBold huruf kapital, angka besar 24 mono.

## 4. Bentuk

- Sudut hampir siku (radius 4). Tidak ada kartu bulat.
- Baris: garis bawah 1 px (`LineBrush`), jarak atas bawah 12.
- Jarak tepi jendela 28 kiri kanan.

## 5. Jendela

- `ui:FluentWindow`, `ExtendsContentIntoTitleBar="True"`, `WindowBackdropType="None"`, `Background="{StaticResource BgBrush}"`.
- Ukuran awal 520 x 700, minimum 480 x 560.
- Tema `Dark` lewat `ui:ThemesDictionary`, lalu `ui:ControlsDictionary`, lalu `Themes/AppStyles.xaml` (urutan ini penting).
- Kontrol memakai WPF-UI (`ui:Button`, `ui:TextBox`, `ui:ToggleSwitch`). Kontrol WPF biasa tidak mengikuti tema gelap.

## 6. Susunan layar

1. Title bar.
2. Strip peringatan (hanya muncul saat perlu): pesan error dan peringatan versi game. Garis kiri 3 px aksen.
3. Bagian `VALUES`: baris nilai (judul, status, angka besar mono, kolom nilai baru, tombol Set, toggle Lock).
4. Bagian `PATCHES`: baris fitur (garis aktif, kotak hotkey, nama, status, toggle di kanan).
5. Baris status bawah: titik koneksi (abu = menunggu, hijau = terhubung) dan satu kalimat status.

## 7. Baris fitur

Kolom: garis aktif 3 px | kotak hotkey 64 px (mono, bisa diedit) | nama + status + info hotkey | toggle.

- Garis aktif transparan saat mati, `AccentBrush` saat nyala. Ditulis sebagai `Style` langsung pada `Border` di DataTemplate. Jangan lewat `Border.Resources` (tidak berlaku).
- Kotak hotkey: ketik `F6` atau `Ctrl+F6`, klik di luar supaya tersimpan. Pilihan disimpan di `%AppData%\<Nama Trainer>\hotkeys.txt`.
- Status satu baris, tidak mengulang kalimat yang sama di tiap baris.

## 8. Larangan

- Jangan memakai kartu bertumpuk, bayangan, gradien, Mica, atau ikon dekoratif.
- Jangan menambah warna aksen kedua.
- Jangan menampilkan pengaturan yang tidak dipakai (profil, kecepatan kunci, panel samping).
- Jangan menulis teks UI dalam bahasa selain Inggris.
- Jangan menghapus fitur atau alamat milik user demi merapikan tampilan.

## 9. Trainer baru

1. Salin `Themes\AppStyles.xaml`, `App.xaml`, dan pola `MainWindow.xaml` / `MainWindow.xaml.cs`.
2. Ganti hanya: judul, ukuran jendela bila perlu, warna aksen di `MainWindow.xaml.cs`.
3. Pertahankan token, jarak, dan huruf di atas.
4. Ikon mengikuti bagian 10.

## 10. Ikon

Ikon dan UI satu gaya. Hanya warna aksen yang berbeda per game.

- Satu bentuk geometris sederhana, datar, satu warna aksen di atas hitam hangat (`BgBrush`), di dalam kotak bulat.
- Acuan gaya: spiral kotak oranye di kotak bulat hitam (gambar dari user, disetujui). Gaya ini jadi standar semua trainer.
- Warna aksen ikon sama dengan aksen UI game itu (NSUNS Trainer: `#E4572E`).
- Tanpa teks, gradien, bayangan, atau detail kecil. Harus tetap terbaca di 16 px.
- Satu file `.ico` berisi ukuran 16, 32, 48, dan 256.
- Dipasang lewat `<ApplicationIcon>` di csproj.
