# MEC Agent Handoff Dokumani

Bu dokuman, yeni bir Codex/agent thread'inde MEC projesine hizli ve guvenli sekilde adapte olmak icin hazirlandi. Yeni thread baslarken bu dosyayi once okut; sonra gerekiyorsa `MEC.Portal/AGENTS.md`, `MEC.Portal/FRONTEND.md` ve mevcut kod uzerinden son dogrulamayi yap.

> Guncel baglam: 16.04.2026. Repo aktif gelistirme halinde ve worktree kirli olabilir. Kullanici degisikliklerini veya onceki agent degisikliklerini geri alma.

## 1. Cozumun Genel Rolu

MEC cozumunde birden fazla uygulama ve ortak katman var:

| Proje | Rol |
| --- | --- |
| `MEC.Portal` | Calisan portali. Duyurular, kutuphane, profil, izin islemleri ve yonetim paneli burada. |
| `MEC.WebAPI` | Dosya/image API'leri, LDAP sync endpointleri ve diger API yuzeyi. Portal, dosya islemlerinde buradaki endpointleri kullanir. |
| `MEC.AssetManagementUI` | Varlik/zimmet yonetimi uygulamasi. Portal disi ama ortak katmanlari kullaniyor. |
| `MEC.Application.Abstractions` | Servis sozlesmeleri ve application DTO/model katmani. |
| `MEC.Application` | Is kurallari ve orchestration servisleri. Controller'lardan tasinan logic burada toplanmali. |
| `MEC.DAL` | EF Core `ApplicationDbContext`, generic repository ve veri erisim katmani. |
| `MEC.Domain` | Entity'ler, enumlar ve ortak domain tipleri. |

Hedef mimari yonu:

`Presentation -> Application.Abstractions/Application -> DAL -> Domain`

Gercekte bazi eski kodlarda Presentation katmani halen Domain/DAL tiplerini gorebilir. Yeni gelistirmelerde bu bagimlilik buyutulmemeli; is kurali Application servisine tasinmali.

## 2. Calisma Mantigimiz

- Once repo gercegini oku, sonra karar ver. Tahminle dosya degistirme.
- Yeni ozellikte controller'lari ince tut. Routing, auth, `ModelState`, `TempData`, JSON/File response, upload/download gibi HTTP/MVC detaylari controller'da kalabilir.
- Filtreleme, sayfalama, Excel import/export, loglama, domain hesaplari ve metadata yonetimi Application servislerinde olmali.
- `IGenericRepository<T>` yeni controller'lara enjekte edilmemeli. Once Application service ac veya mevcut servisi genislet.
- ViewModel'ler Portal'da kalabilir; Application servisleri kendi DTO/result modelleriyle donmeli, controller map etmelidir.
- Kirli worktree varsay. Kullaniciya ait unrelated degisiklikleri geri alma, silme veya formatlama.
- Manuel editlerde `apply_patch` tercih edilir. Formatters sadece kullanici isterse veya kapsam netse calistirilir.
- Mutlaka build ile dogrula. Kilitli `bin` sorunu icin gecici `OutDir` kullan.

## 3. Kritik Domain Kararlari

### Portal kullanicisi kaynagi

- `MEC.Portal` tarafinda kullanici kaynagi `employee_portal` tablosudur.
- Portal izin akisi artik `employee` tablosuna bakmamali.
- `leaves.employee_id` kolonu isim olarak eski kalsa da Portal semantiginde `employee_portal.id` tutar.
- Eski kayitlarda `leaves.employee_id` `employee.id` tuttuysa, gecmis veriler icin DB migration/mapping gerekebilir. Kod artik yeni semantige gore calisir.

### Izin turleri

- `leaves.leave_type` string yaklasimindan `leave_type` tablosuna gecildi.
- `leaves.leave_type_id` FK olarak kullanilmali.
- Baslangic tipleri:
  - `ANNUAL` -> `Yillik Izin`
  - `EXCUSE` -> `Mazeret Izni`
  - `OTHER` -> `Diger`
  - `UNPAID` -> `Ucretsiz Izin`
  - `SICK` -> `Hastalik Izni`
  - `MATERNITY` -> `Dogum Izni`
  - `PATERNITY` -> `Babalik Izni`
- Yillik izin bakiyesi sadece `LeaveType.Code == ANNUAL` icin etkilenir.

### Izin status enumu

`LeaveStatus` integer olarak tutulur:

- `Pending = 0`
- `Approved = 1`
- `Rejected = 2`
- `Cancelled = 3`

Magic number kullanma; enum veya helper metodlarla calis.

### Izin tarih/saat hesabi

- `/Leave/RequestLeave` baslangic ve bitis tarihi ile saat alir.
- Saat secimi 09:00-18:00 araligindadir.
- Hafta sonlari izin gun hesabindan dusulur.
- Sonuc en yakin yarim gune yukari yuvarlanir.
- Bitiş saati varsayilan olarak `09:00` gelir.

## 4. Ana Ozellikler ve Akislar

### Authentication ve kullanici

- Login cookie/claim uretimi `MEC.Portal.Controllers.AccountController` tarafindadir.
- Aktif portal kullanicisi `IEmployeePortalService.GetActivePortalUserByEmailAsync` ile bulunur.
- Header'da `Hoş geldiniz, Ad Soyad` gostermek icin `employee_portal` bilgisi kullanilir.
- `employee` tablosu Portal icin kaynak olmamali; AssetManagementUI veya legacy kisimlar ayridir.

### Profil

- `ProfileController` `IEmployeePortalService` kullanir.
- Profil kartinda kullanici bilgileri, `Yillik Izin Hakki` ve `Kalan Izin` gosterilir.
- `Kullanilan Izin` alani kaldirildi.
- Bekleyen yillik izin ozeti, `employee_portal.id` ile ilgili leave kayitlarindan hesaplanir.

### Izin talebi ve gecmisi

- `LeaveController` HTTP/file concern'leri tasir; is kurali `ILeaveService`tedir.
- Ek dosya upload'u WebAPI Attachment API uzerinden yapilir.
- `/Leave/History` filtreleme, paging ve detay verisi `ILeaveService`ten gelir.
- `/Leave/History/{id}` kullanicinin kendi `employee_portal.id` kaydina ait izin detayini gosterir.
- Detay sayfasinda `Kalan Izin Hakki` kaldirildi.

### Admin izin yonetimi

- `Yonetim Paneli > Izinler > Izin Talepleri` route'u: `/Admin/LeaveRequests`
- Detay route'u: `/Admin/LeaveRequests/{id}`
- Rapor route'u: `/Admin/LeaveReport`
- Liste ve detayda onay/red butonlari vardir.
- Onay/red islemi `ILeaveService.UpdateLeaveStatusWithLogAsync` ile yapilir ve `log` tablosuna user action log yazar.
- Rapor filtreleri: calisan, izin turu, baslangic ve bitis tarihi.
- Excel export `ILeaveService.ExportAdminLeaveReportAsync` uzerinden uretilir.

### Toplu izin yukleme

- `/Admin/PortalUsers` sayfasinda `Toplu Izin Yukle` butonu vardir.
- Upload endpoint'i: `POST /Leave/BulkLeaveUpload`
- Sablon endpoint'i: `GET /Leave/BulkLeaveUpload/Template`
- Excel ilk satiri header kabul edilir.
- Kolon sirasi: `email`, `eklenecek izin gun sayisi`, `aciklama`.
- Dogru satirlar islenir, hatali satirlar tum islemi iptal etmez.
- Hata raporu `.xlsx` olarak memory cache'te token ile 15 dakika tutulur.
- Her basarili ekleme `log` tablosuna `method_name = BulkLeaveUpload` ile yazilir.

### Duyurular

- Public liste route'u: `/Announcements`
- Public detay route'u: `/Announcements/{id}`
- Admin liste/olusturma/duzenleme route'lari:
  - `/Admin/Announcements`
  - `/Admin/Announcements/Create`
  - `/Admin/Announcements/Edit/{id}`
- `/Admin/Announcements/{id}` artik kullanilmamali; detay public `/Announcements/{id}`.
- Duyurular kart gorunumundedir; public kartlarda aktif/pasif yazisi yoktur.
- Duyuru detayinda ekler ve galeri gosterilir.
- `Diger Duyurular`, `Tum Duyurular`, `Tum duyurulara don` metinleri Turkce karakterlerle duzeltildi.
- Related duyuru logo/gorsellerinde `object-fit: contain` kullanilir; logo kirpilmamali.

### Duyuru dosyalari ve galeri

- Fiziksel dosyalar `MEC.WebAPI` uzerinden sunucuda tutulur.
- Metadata Portal DB'de tutulur:
  - `announcement_attachment`
  - `announcement_image`
- Attachment scope: `announcements-attachments`
- Image scope: `announcements-gallery`
- Create/Edit ekranlari dosya ekleme ve silme destekler.
- Detail sayfasi DB metadata'sindan beslenir; eski regex fallback yaklasimi kullanilmamali.

### Kutuphane ve Icerik Yonetimi

- Public route: `/Library`
- Admin route: `/Admin/Content`
- Admin menusu: `Yonetim Paneli > Icerik Yonetimi`
- Public navbar item: `Kutuphane`, `Profilim`in solundadir.
- Entity'ler:
  - `LibraryFolder`
  - `LibraryDocument`
- Dosyalar WebAPI Attachment API ile `scope=library-documents` altinda tutulur.
- Metadata Portal DB'de tutulur.
- `/Library` explorer tarzidir:
  - solda klasor + dokuman tree
  - sagda secili klasorun direkt alt klasor/dokumanlari ikonlu grid
  - PDF browser'da acilir
- `/Admin/Content` klasor olusturma, yeniden adlandirma, silme, dokuman upload/silme/rename ve PDF goruntuleme destekler.

### Slider

- Admin route: `/Admin/Slider`
- Menu: `Yonetim Paneli > Ayarlar > Slider`
- Entity: `SliderImage`
- Fiziksel gorseller WebAPI Image API ile `scope=homepage-slider` altinda tutulur.
- Metadata Portal DB'de `slider_image` tablosundadir.
- Ana sayfada slider sadece kayit varsa render edilir.
- Siralama drag-drop ile `display_order` uzerinden tutulur.
- Onerilen gorsel olcusu: `1920x700` veya `1920x800`, WebP/JPG, onemli icerik merkezde.

### Portal kullanicilari

- Admin route: `/Admin/PortalUsers`
- Menu: `Yonetim Paneli > Ayarlar > Portal Kullanicilari`
- Kaynak tablo: `employee_portal`
- Aktif/pasif ayrimi `IsDeleted` uzerindendir.
- Liste paging: sayfa basina 10 kayit.
- Detay/edit sayfasi portal kullanicisini gunceller.

### LDAP

- `MEC.WebAPI.Controllers.LdapController`
- Mevcut endpoint:
  - `GET api/Ldap/SyncUsers`
  - `employee` tablosuna sync yapar; davranisi korunmali.
- Yeni endpoint:
  - `GET api/Ldap/SyncPortalUsers`
  - `employee_portal` tablosuna sync yapar.
- `SyncPortalUsersFromLdapAsync` yeni kullanicilarda:
  - `HireDate = new DateTime(1000, 1, 1)`
  - `BirthDate = new DateTime(1000, 1, 1)`
  - `LeaveDays = 0`
  - `IsDeleted = false`
- Mevcut portal kullanicisinda sadece kimlik bilgileri guncellenir; `LeaveDays`, `HireDate`, `BirthDate` korunur.

### Loglama

- Serilog host seviyesinde kuruldu.
- DB log tabloları:
  - `api_log`
  - `log`
- Ortak alanlar:
  - `id`, `ip_address`, `mac_address`, `user`, `timestamp`, `message`, `level`, `method_name`
- `api_log` ek alanlari:
  - `request_path`, `http_method`, `status_code`, `request_body`, `response_body`, `query_string`
- WebAPI icin altyapi hazir, aktif request logging bu turda acik degil.
- Portal user action loglari:
  - izin onay/red
  - toplu izin yukleme basarili satirlar

### AssetManagementUI

- Portal disi ama ayni solution icinde.
- WebAPI dosya API degisikliklerinden etkilendi.
- `AssetInfo` dosya akisi server-side MVC action + typed API client modeline tasindi.
- Sabit WebAPI IP kullanimi kaldirilmaya baslandi.
- Bu projede calisirken Portal odagini kaybetme; sadece ortak katman veya user istegi varsa dokun.

## 5. WebAPI Dosya API Modeli

WebAPI dosya endpointleri generic scope + entityId mantigina yaklastirildi.

### Attachment

- Leave ekleri, announcement ekleri ve library dokumanlari icin kullanilir.
- Ornek scope'lar:
  - `leave-attachments` veya mevcut leave helper akisi
  - `announcements-attachments`
  - `library-documents`

### Image

- Announcement gallery, homepage slider ve asset image akislarinda kullanilir.
- Ornek scope'lar:
  - `announcements-gallery`
  - `homepage-slider`
  - `assets`

Portal tarafinda controller dosya API client cagirabilir; fiziksel dosya API concern'i Application servislerine tasinmadi. Metadata CRUD ise Application servislerine alinmaya baslandi.

## 6. Application Servisleri

Onemli servisler:

- `ILeaveService` / `LeaveService`
  - izin talebi
  - izin gecmisi/detayi
  - admin izin listesi/detayi
  - rapor filtreleme/export
  - onay/red + log
  - bulk leave upload
- `IEmployeePortalService` / `EmployeePortalService`
  - profil
  - portal user liste/detay/update
  - aktif portal user lookup
- `IAnnouncementService` / `AnnouncementService`
  - admin/public duyuru data
  - ek/galeri metadata
- `ILibraryService` / `LibraryService`
  - public library tree/grid
  - admin content metadata CRUD
- `ISliderService` / `SliderService`
  - slider metadata, silme sonrasi order normalize, reorder
- `ILdapService` / `LdapService`
  - employee sync
  - employee_portal sync
- `IUserActionLogService` / `UserActionLogService`
  - `log` tablosuna kullanici aksiyon logu

Yeni is kurali eklerken once bu servislerden hangisinin genisletilecegini degerlendir.

## 7. Frontend Kararlari

Aktif tema `MEC.Portal/wwwroot/css/site.css` icinde.

Ana palet:

- Arka plan: `#ffffff`
- Birincil: `#18285c`
- Ikincil: `#000000`
- Ucuncul: `#06c6f7`

Kurallar:

- Sade, kurumsal, profesyonel tasarim.
- Parlak gradient/glow/cam efekti kullanma.
- Emoji kullanma.
- Dekoratif ikon kullanma; sadece islevsel ikonlar.
- Hover ve motion sade olmali.
- Mobil kirilimlarda kartlar tam genislikte akmali; yatay scroll veya dar kolon sikismasi kabul edilmez.
- Var olan design language'i bozma; yeni componentlerde mevcut class pattern'lerine bak.

## 8. DB ve Script Notlari

Kullanici DB scriptlerini genelde kendisi phpMyAdmin/MySQL uzerinde calistiriyor. Agent script paylasirken:

- `log` gibi reserved/olasi problemli tablo adlarini backtick ile yaz.
- Migration scriptlerini iki asamali ve kontrollu ver:
  - once create/add/map
  - sonra cleanup/drop
- FK ekleme oncesi orphan kontrol sorgusu ver.
- phpMyAdmin'de `SHOW CREATE TABLE` ciktilari okunamayabilir; information_schema sorgulari daha pratik olabilir.

Onemli script konumlari:

- `MEC.Portal/DatabaseScripts`
- Daha once leave decimal, leave type, log, announcement metadata, slider, library gibi scriptler verildi.

## 9. Bilinen Teknik Borclar ve Dikkat Noktalari

- Worktree'de birden fazla alanda yarim/aktif degisiklik olabilir; `git status --short` ile basla.
- Eski `MEC.Portal/AGENTS.md` ve `ARCHITECTURE.md` bazi noktalarda eski bilgi icerebilir. Bu handoff daha gunceldir; yine de kodu dogrula.
- `leaves.employee_id` kolon adi semantik olarak `employee_portal.id` tutuyor ama isim eski. Yeni agent bunu yanlislikla `employee.id` sanmamali.
- `Employee` tablosu Portal icin kullanilmamali; legacy/AssetManagementUI/LDAP eski endpoint tarafinda bulunabilir.
- `LoginService` ve auth akisinda eski/test davranislari olabilir; production guvenligi icin ozellikle kontrol edilmeli.
- `appsettings.json` icinde secret/connection string olabilir; yeni secret ekleme.
- `Html.Raw(Model.Content)` ile duyuru icerigi render ediliyor; icerik kaynagi guvenli degilse XSS riski vardir.
- `MEC.WebAPI` CORS policy ve eski NuGet uyumluluk warning'leri ayrica ele alinabilir.

## 10. Build ve Dogrulama Komutlari

Kilitli `bin` problemi yasamamak icin gecici `OutDir` kullan:

```powershell
dotnet build MEC.Portal\MEC.Portal.csproj -m:1 /p:UseSharedCompilation=false /p:OutDir=C:\Users\Eren\AppData\Local\Temp\mec-portal-build\ -v:minimal
```

```powershell
dotnet build MEC.WebAPI\MEC.WebAPI.csproj -m:1 /p:UseSharedCompilation=false /p:OutDir=C:\Users\Eren\AppData\Local\Temp\mec-webapi-build\ -v:minimal
```

```powershell
dotnet build MEC.AssetManagementUI\MEC.AssetManagementUI.csproj -m:1 /p:UseSharedCompilation=false /p:OutDir=C:\Users\Eren\AppData\Local\Temp\mec-asset-build\ -v:minimal
```

Tum solution gerekiyorsa:

```powershell
dotnet build MEC.sln -m:1 /p:UseSharedCompilation=false /p:OutDir=C:\Users\Eren\AppData\Local\Temp\mec-sln-build\ -v:minimal
```

Build warning'leri sifir degil. Basarili build icin oncelik `0 hata`.

## 11. Manuel Smoke Checklist

Portal icin:

- Login
- Navbar desktop/mobile
- `/Announcements`
- `/Announcements/{id}`
- `/Library`
- `/Profile`
- `/Leave/RequestLeave`
- `/Leave/History`
- `/Leave/History/{id}`
- `/Admin/LeaveRequests`
- `/Admin/LeaveRequests/{id}`
- `/Admin/LeaveReport`
- `/Admin/PortalUsers`
- `/Admin/Announcements`
- `/Admin/Announcements/Create`
- `/Admin/Announcements/Edit/{id}`
- `/Admin/Content`
- `/Admin/Slider`

WebAPI icin:

- `GET api/Ldap/SyncUsers`
- `GET api/Ldap/SyncPortalUsers`
- Attachment upload/list/file/delete
- Image upload/list/file/delete

## 12. Yeni Thread Icin Baslangic Rutini

1. Bu dosyayi oku.
2. `git status --short` calistir.
3. Istenen konuya gore ilgili controller, service interface, service implementation ve view'i oku.
4. Eski dokumanlarla celiski varsa kodu esas al; bu handoff'u ikinci kaynak kabul et.
5. Degisiklik yapmadan once scope'u netlestir.
6. Degisiklikten sonra ilgili proje build'ini gecici `OutDir` ile calistir.
7. Final cevapta ne degisti, nasil dogrulandi ve kalan risk var mi kisa yaz.

## 13. Kullanici Tercihleri

- Kullanici genelde dogrudan implementasyon ister; gereksiz soru sorma.
- Karar etkisi buyukse once secenekleri ve onerilen yolu net anlat.
- Turkish UI metinleri dogru Turkce karakterlerle yazilmali.
- Kullanici GitHub push isteyebilir; push istenmedikce commit/push yapma.
- Kullanici DB scriptlerini kendisi calistirmayi tercih ediyor; DB degisikligi gerekiyorsa scripti finalde acik ver.
- Frontend islerinde sade ama profesyonel tasarim bekleniyor; mevcut tema korunmali.

