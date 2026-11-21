# WindowsClockWidget – Linux (Avalonia)

Windows WPF saat widget'inin Avalonia ile Linux'a portlanmış, temiz kaynak kopyasıdır.
`bin/` ve `obj/` klasörleri dahil değildir; ilk derlemede otomatik oluşturulur.

## Gereksinimler

- .NET 8 SDK (`dotnet --version` → `8.x`)
- X11 veya Wayland masaüstü (Avalonia ikisini de destekler)
- Hava durumu için internet bağlantısı (API anahtarı isteğe bağlı)

Örnek kurulum:

```bash
# Arch / Manjaro
sudo pacman -S dotnet-sdk-8.0

# Debian / Ubuntu
sudo apt install dotnet-sdk-8.0
```

## Hızlı başlatma (kaynaktan)

```bash
cd ~/Masaüstü/WindowsClockWidget-Linux
dotnet restore
dotnet run -c Release
```

Uygulama tek örnek olarak çalışır; zaten açıksa ikinci başlatma sessizce kapanır.

## Dağıtım için klasöre yayınlama

.NET 8 runtime kurulu sistemler için (küçük çıktı):

```bash
cd ~/Masaüstü/WindowsClockWidget-Linux
dotnet publish -c Release -r linux-x64 --self-contained false -o ../WindowsClockWidget-publish
cd ../WindowsClockWidget-publish
./WindowsClockWidget
```

Runtime gerektirmeyen, tek başına çalışan sürüm için:

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -o ../WindowsClockWidget-publish
```

Minimal sistemlerde eksik olabilen paylaşımlı kütüphaneler: `libx11`, `fontconfig`.

## Kullanım

- Saate sağ tıkla: **Ayarlar**, **Hava Durumunu Güncelle**, **Takvim**, **Kapat**.
- Saati sürükleyerek taşı. Ayarlardaki **Konumu sabitle** açıksa sürükleme kapanır ve konum hatırlanır.
- Ayarlar penceresindeki **Sistem açılınca otomatik başlat** seçeneği Linux'ta XDG autostart dosyası oluşturur.
- Renk seçimi Avalonia `ColorView` bileşeniyle yapılır (spektrum + RGB + hex).

## Dosya konumları

| İçerik | Yol |
| --- | --- |
| Ayarlar | `~/.config/WindowsClockWidget/settings.json` |
| Hata günlüğü | `~/.config/WindowsClockWidget/error.log` |
| Otomatik başlatma | `~/.config/autostart/windowsclockwidget.desktop` |

Ayarları sıfırlamak için uygulama kapalıyken `~/.config/WindowsClockWidget/settings.json` dosyasını sil; varsayılanlar yeniden oluşturulur.

## Notlar

- Saat/dakika üst hizalaması gerçek glyph ink ölçümüyle yapılır; font veya boyut değişince kendini günceller.
- Takvim ve hatırlatmalar yerel olarak çalışır; hava durumu çevrimdışıyken `--°` olarak kalabilir.
- Hata ayıklama için `dotnet run` çıktısına ek olarak yukarıdaki `error.log` dosyasına bak.

## Contributors
\n- xpow3r-34

