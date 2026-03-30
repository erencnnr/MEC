# MEC.Portal Architecture Notes

## 1. Projenin Rolu
`MEC.Portal`, calisanlarin gunluk operasyonlarini kullandigi MVC tabanli portal uygulamasi. Mevcut durumda duyurular, profil gorunumu, izin talebi ve admin yonetim akislari calisiliyor.

## 2. Katmanlarin Bugunku Rolu
| Proje | Rol | Portal ile iliskisi |
| --- | --- | --- |
| `MEC.Portal` | Presentation | Controller, view, view model, local UI service |
| `MEC.Application.Abstractions` | Contract katmani | Login, leave, employee portal gibi servis arayuzleri |
| `MEC.Application` | Is kurali / orchestration | Login, leave ve employee portal servislerinin bir kismi |
| `MEC.DAL` | Veri erisim / EF Core | `ApplicationDbContext`, generic repository, ayrica bazi portal servis sozlesmeleri ve implementasyonlari fiilen burada |
| `MEC.Domain` | Entity cekirdegi | `Employee`, `EmployeePortal`, `Leave`, `Announcement` ve diger ortak entity'ler |
| `MEC.WebAPI` | Ayri sunum yuzeyi | Portal su an bu katman uzerinden calismiyor |

## 3. Gercek Bagimlilik Akisi
Hedeflenen yon muhtemelen `Presentation -> Application -> Infrastructure -> Domain`.

Bugunku pratik akis ise su sekilde:

`MEC.Portal -> MEC.Application.Abstractions`

`MEC.Portal -> MEC.Application`

`MEC.Portal -> MEC.DAL`

`MEC.Portal -> MEC.Domain`

Bu yuzden portal controller'lari bazi senaryolarda application servislerini kullaniyor, bazi senaryolarda ise repository ve entity'lere dogrudan iniyor.

## 4. Ozellik Bazli Akislar

### Authentication
- Giris ekrani `AccountController` uzerinden calisiyor.
- Contract olarak `ILoginService` var.
- `LoginService` LDAP ve `Employee` tablosunu birlikte kullanmak uzere tasarlanmis.
- Ancak mevcut controller akisinda hard-coded kullanici kontrolu aktif, servis cagrisi yorum satirinda.

### Duyurular
- Kullanici ana sayfasi ve tum duyurular listesi `HomeController` uzerinden geliyor.
- Yonetim tarafinda `AnnouncementController` duyuru olusturma, listeleme, silme islerini yapiyor.
- `IAnnouncementService` ve `AnnouncementService` isim olarak application katmanini temsil ediyor, fakat fiziksel konumlari `MEC.DAL` altinda.

### Izin Sureci
- Personel izin talebi `LeaveController` uzerinden olusturuluyor.
- Bu controller dogrudan `IGenericRepository<Leave>` ve `IGenericRepository<Employee>` kullaniyor.
- Admin onay/red sureci `AdminController` uzerinden `ILeaveService` ile ilerliyor.
- `LeaveService`, onay durumuna gore `EmployeePortal.LeaveDays` alanini guncelliyor.

### Profil
- `ProfileController`, profil ozetini `IEmployeePortalService` ile aliyor.
- Ayni ekranda izin gecmisi icin dogrudan `Leave` ve `Employee` repository'leri kullaniliyor.
- Boylece ekran tek bir servis yerine hibrit bir akisla besleniyor.

## 5. Clean Architecture Acisindan Durum
| Baslik | Mevcut durum | Etki |
| --- | --- | --- |
| Presentation bagimliliklari | Portal dogrudan DAL ve Domain referansi aliyor | UI katmani veri erisim detaylarini biliyor |
| Abstraction bagimliliklari | `MEC.Application.Abstractions` projesi `MEC.DAL` referansi aliyor | Katman yonu tersine donuyor |
| Service yerlesimi | `IAnnouncementService` ve `AnnouncementService` fiziksel olarak DAL altinda | Kod ararken surpriz yaratiyor |
| Controller kalinligi | `LeaveController` ve `ProfileController` veri erisim ve is kurali iceriyor | Test ve refactor maliyeti artiyor |
| Entity kullanimi | Domain entity'leri view tarafina sik tasiniyor | UI ile domain siki baglaniyor |

## 6. Onemli Bulgular
1. Giris akisi su an gecici/hard-coded durumda. Mevcut haliyle gercek kullanici akisina temsil etmiyor.
2. Admin ekranlari gorunurlukte role bagli olsa da action seviyesinde acik yetkilendirme belirgin degil.
3. `AdminController` namespace'i `MEC.Portal` yerine `MEC.AssetManagementUI.Controllers` olarak kalmis.
4. `appsettings.json` icinde connection string ve LDAP secret'lari duz metin tutuluyor.
5. Duyuru icerikleri view icinde `Html.Raw(...)` ile gosteriliyor; icerik kaynagi guvenilmiyorsa XSS riski var.
6. Cozumde test projesi gorunmuyor; mevcut gelistirme tamamen manuel dogrulamaya dayaniyor.

## 7. Refactor Icin Onerilen Sira
1. Authentication ve authorization akisini netlestir.
2. Portal use case'lerini servis bazinda toparla; controller'lardan repository erisimini azalt.
3. `Announcement` sozlesmesi ve implementasyonunu dogru projelere tasi.
4. `Employee` ve `EmployeePortal` ayrimini yazili hale getir; hangi ekran hangi tabloyu neden kullaniyor netlestir.
5. Ortam konfigurasyonunu secret-safe hale getir.
6. En azindan `LeaveService` ve login akisi icin test altyapisi kur.

## 8. Yeni Gelistirme Icin Karar Notu
Bu repo tamamen bozuk degil; cekirdek akislar okunabilir durumda. Ancak yeni gelistirmeleri mevcut karisik sinirlara ekleyerek buyutmek teknik borcu hizlandirir. En dogru yaklasim, portali gelistirirken her yeni iste kucuk kucuk sinir duzeltmeleri yapmak:

- controller incelt
- servis contract'ini netlestir
- data access'i application arkasina al
- entity yerine view model/DTO ile don

Bu stratejiyle hem urun gelistirmesi durmaz hem de mimari yavas yavas temizlenir.
