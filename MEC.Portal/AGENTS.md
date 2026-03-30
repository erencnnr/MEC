# MEC.Portal Agent Guide

## Amac
Bu dosya, `MEC.Portal` uzerinde birlikte calisirken ayni baglamdan ilerlemek icin hazirlanmis operasyon rehberidir. Portal su anda aktif gelistirme alanidir. `MEC.AssetManagementUI` park edilmis durumdadir; sadece paylasilan katmanlarda zorunlu bir degisiklik varsa dokunulmalidir.

## Kisa Baglam
- Cozum clean architecture niyetiyle katmanlara ayrilmis durumda, ancak mevcut kod tabaninda sinirlar tam oturmamis.
- `MEC.Portal` calisan portalidir.
- Ana ozellikler: giris, duyurular, profil, izin talebi, admin paneli.
- `MEC.WebAPI` su an portalin request akisinda aktif olarak kullanilmiyor. Portal dogrudan ortak katmanlara baglaniyor.
- Frontend kararlarinda once `FRONTEND.md` dosyasina bak.

## Portalin Bagli Oldugu Katmanlar
- `MEC.Portal`: MVC presentation katmani.
- `MEC.Application.Abstractions`: servis contract'larinin bir kismi burada.
- `MEC.Application`: bazi application servis implementasyonlari burada.
- `MEC.DAL`: EF Core `ApplicationDbContext`, generic repository ve portalin halen dogrudan kullandigi veri erisim bilesenleri burada.
- `MEC.Domain`: entity'ler ve temel ortak siniflar burada.

## Ozellik Haritasi
| Alan | Presentation | Application / Service | Data / Domain |
| --- | --- | --- | --- |
| Giris | `Controllers/AccountController.cs`, `Views/Account/Login.cshtml` | `ILoginService`, `LoginService` | `Employee`, LDAP ayarlari |
| Ana sayfa ve duyurular | `Controllers/HomeController.cs`, `Controllers/AnnouncementController.cs` | `IAnnouncementService`, `AnnouncementService` | `Announcement`, generic repository |
| Izin talebi | `Controllers/LeaveController.cs`, `Views/Leave/RequestLeave.cshtml` | `ILeaveService`, `LeaveService` | `Leave`, `Employee`, `EmployeePortal` |
| Profil | `Controllers/ProfileController.cs`, `Views/Profile/Index.cshtml` | `IEmployeePortalService`, `EmployeePortalService` | `EmployeePortal`, `Leave`, `Employee` |
| Admin | `Controllers/AdminController.cs`, `Views/Admin/*` | `ILeaveService`, `IAnnouncementService` | `Employee`, `EmployeePortal`, `Leave`, `Announcement` |

## Calisma Kurallari
1. Yeni portal isi gelistirirken once `MEC.Portal` icindeki controller/view akisina, ardindan bagli servisleri ve entity'leri bak.
2. Yeni is kurali ekleniyorsa tercihen controller icine degil application servisine alin.
3. Yeni gelistirmelerde `MEC.Portal` icine dogrudan `IGenericRepository<T>` enjekte etmek yerine bir use case servisi acmak tercih edilmeli.
4. Domain entity'lerini view'a dogrudan tasimak yerine yeni eklemelerde view model kullanimi tercih edilmeli.
5. `MEC.DAL` icinde application namespace'i ile duran siniflar var. Yeni eklemelerde bu karisikligi buyutme; once mevcut yerlesimi dogrula, sonra minimum surprizle ilerle.
6. Admin yetkisi gerektiren aksiyonlarda sadece menu gorunurlugune guvenme; action seviyesinde yetki kontrolu dusun.
7. Yeni konfigurasyon eklerken secret degerleri `appsettings.json` icine gomme. Ortam degiskeni, user-secrets veya guvenli konfigurasyon kaynagi kullan.
8. `AssetManagementUI` tarafini sadece paylasilan servis/entity etkileniyorsa guncelle. Portal odak kaybolmasin.

## Frontend Kurallari
1. Arka plan beyaz kullanilacak.
2. Birincil renk `#18285c`, ikincil renk siyah, ucuncul renk `#06c6f7`. Yeni frontend islerinde bu palette disina cikma.
3. Portalin gorsel dili mat, kurumsal ve sakin olmali; parlayan gradient, glow, cam efekti ve asiri kontrastli vurgu kullanma.
4. Emoji kullanma.
5. Dekoratif ikon kullanma; sadece gercek islev veya anlam katiyorsa kullan.
6. Baslik, kart metni ve butonlarda AI usulu gosterisli copy kullanma.
7. Dekoratif dash ayiraclari kullanma; gerekiyorsa virgul, slash veya dogrudan cumle yapisi tercih et.
8. Hover ve motion sade olmali; yukselen kart, parlayan buton ve dikkat dagitan animasyon istemiyoruz.

## Kritik Gercekler
- Portal su an `MEC.DAL` ve `MEC.Domain` referanslarini dogrudan aliyor.
- `Announcement` servis sozlesmesi ve implementasyonu fiziksel olarak beklenen katmanda degil; isim alani ile gercek proje konumu farkli.
- Izin akisinda durum kodlari fiilen `0 = Bekliyor`, `1 = Onaylandi`, `2 = Reddedildi` olarak kullaniliyor.
- Profil ekrani `EmployeePortal` tablosundan beslenirken izin akisi `Employee` tablosu uzerinden kullanici eslemesi yapiyor.

## Dokunmadan Once Kontrol Et
- Authentication degisikligi mi yapiliyor?
- Sadece admin'e acik bir ekran mi guncelleniyor?
- Ayni veri hem `Employee` hem `EmployeePortal` tarafinda mi tutuluyor?
- View icinde `Html.Raw(...)` ile gosterilen icerik etkileniyor mu?
- Degisiklik portali mi ilgilendiriyor, yoksa ortak katmanda baska projeleri de etkiliyor mu?

## Beklenen Dogrulama
- Derleme: `dotnet build MEC.Portal\MEC.Portal.csproj`
- Gerekirse tum cozum: `dotnet build MEC.sln`
- Manuel smoke check:
  - Login
  - Home / announcements list
  - Leave request submit
  - Profile summary
  - Admin leave approval flow

## Teslim Sekli
- Hangi dosyalarin degistigini kisa ozetle.
- Kullanici etkisini belirt.
- Bilinen risk veya takip edilmesi gereken teknik borc varsa acikca yaz.
