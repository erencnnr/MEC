# İzin job'ı ve okul müdürü ayarlarının yayına alınması

Bu bilgisayara görev kurulmaz. Aşağıdaki adımlar portalın yayınlanacağı Windows sunucusunda uygulanır.

1. Veritabanını yedekleyin. `DatabaseScripts/mysql_school_managers_annual_job.sql` dosyasını bir kez çalıştırın. Önceki izin politikası şeması (özellikle `leave_agreement.balance_as_of_date`) mevcut olmalıdır. Eski uygulamayı durdurup SQL ve yeni yayını birlikte devreye alın.
2. `dotnet publish MEC.Portal/MEC.Portal.csproj -c Release -o <yayın-klasörü>` ile çıktıyı oluşturun. Yayın klasörünü sunucuya aktarın. `Scripts` klasörü de yayına dahildir.
3. Görev hesabına yayın klasörünü okuma, `logs/annual-leave` klasörünü yazma ve veritabanına erişim verin. Sunucu saat dilimi `Turkey Standard Time` olmalıdır. .NET 8 ASP.NET Core Runtime kurulu olmalıdır.
4. Görev işlemi IIS'in ortam değişkenlerini devralmaz. Üretim bağlantısını görev hesabının erişebildiği güvenli yapılandırmada veya `ConnectionStrings__ProdConnection` ortam değişkeninde sağlayın. Parolayı görev komutuna yazmayın. Runner, `ASPNETCORE_ENVIRONMENT` ve `AppSettings__Environment` değerlerini Production yapar.
5. Yönetici PowerShell oturumunda kurulumu çalıştırın (örnek yolu ve hesabı değiştirin):

```powershell
& 'D:\MEC\Portal\Scripts\Install-AnnualLeaveTask.ps1' -PublishPath 'D:\MEC\Portal' -TaskUser 'DOMAIN\mec-job'
```

Görev her gün 07.00'de, kullanıcı oturum açmamış olsa da çalışır. Kaçırılmış başlangıç ilk fırsatta çalışır; başarısız çalıştırma 10 dakika arayla en fazla üç kez yeniden denenir. Aynı görevin üst üste başlaması engellenir. PowerShell betikleri sunucunun yürütme politikasına uygun imzalanmış/izinli olmalıdır.

Kontrollü ilk çalıştırma ve durum kontrolü:

```powershell
Start-ScheduledTask -TaskName 'MEC-AnnualLeave-0700'
Get-ScheduledTaskInfo -TaskName 'MEC-AnnualLeave-0700'
```

`LastTaskResult = 0` ve `logs/annual-leave/job-YYYY-MM-DD.log` içindeki tamamlanma mesajını kontrol edin. Job hata alırsa sıfırdan farklı çıkış kodu döner. Görev kaydını tekrar kurmak mevcut aynı adlı görevi günceller.

## Hesaplama ve geçiş

- İşe giriş yıl dönümü esas alınır: 1–5 tamamlanmış yıl 14, 6–14 yıl 20, 15 ve üzeri 26 gün. İlk yıl tamamlanmadan hak ediş yoktur. Mevcut 18 yaş ve altı / 50 yaş ve üzeri için en az 20 gün kuralı korunur. 29 Şubat işe girişleri artık olmayan yıllarda 28 Şubat kabul edilir.
- Mutabakatı olanlarda bakiye mutabakat tarihi, sonrasındaki hak edişler ve onaylı yıllık izinlerden yeniden hesaplanır. Aynı hakkın tekrar eklenmesi önlenir.
- Mutabakatı olmayanlarda mevcut bakiye korunur ve job işlenen tarih kaydından sonraki yıl dönümlerini ekler. SQL geçişi başlangıcı çalıştırıldığı günün bir önceki günü olarak ayarlar. Geçmiş yılların hakları topluca tekrar yüklenmez; geçmiş bakiye düzeltmeleri mutabakat ekranından yapılır.
- İlerleyen günlerde görev çalışamazsa bir sonraki çalıştırma son başarılı tarihten itibaren kaçırılan yıl dönümlerini de kapsar. Güncelleme ve işlenen tarih aynı veritabanı transaction'ında kaydedilir; personel satırı kilitlenerek eşzamanlı job tekrarları korunur.
- Silinmiş personel işlenmez; işten çıkış tarihinden sonraki yıl dönümleri hak oluşturmaz. Yeni personelin işe giriş tarihini doğru girin. Geçmiş işe giriş tarihi/bakiye düzeltmelerini mevcut mutabakatla uyumlu yapın.
- Portalın eski 12 saatlik arka plan servisi kayıtlı değildir. Mutabakatlı personel için ekran/talep sırasındaki mevcut yeniden hesaplama devam eder; 07.00 job'ı portal kapalıyken de çalışır.
- Yalnızca yıllık izin bakiyeden düşer. Sıfır ve eksi bakiye ile talep açılabilir. Evlilik, ölüm, babalık gibi türlerin mevcut azami süreleri ve geçerli tarih kontrolleri korunur.

## Okullar ve yetkiler

`Yönetim Paneli > Ayarlar > Okullar` ekranında her okulun müdürünü seçin. Eski kayıtlarda yalnızca bir müdürü olan okullar SQL tarafından taşınır. Birden fazla müdürü olan veya müdürü olmayan okullar SQL sonucunda listelenir; bu okulları ekrandan tamamlayın. Kullanıcının çalıştığı lokasyonlar ile yönettiği okullar birbirinden bağımsızdır.

Admin rolü `employee_portal.IsAdmin` alanından okunur ve her istekte güncellenir; yetki verme/kaldırma için yeniden giriş gerekmez. Adminler izin/mesai akışının her iki aşamasında işlem yapabilir; iki aşama korunur. Portal girişi aktif `employee_portal` kaydı ve mevcut kimlik doğrulamasıyla yapılır; eski `employee.IsAdmin` koşuluna bağlı değildir.

## Doğrulama

```powershell
dotnet build MEC.sln -c Release
dotnet run --project MEC.WorkflowSmokeTests -c Release
dotnet run --project MEC.Portal.SmokeTests -c Release
```

Ekran örneklerini gerçek veritabanına bağlanmadan görüntülemek için son komuta `-- --serve-ui` ekleyin. `http://127.0.0.1:5187/preview/agreements`, `schools`, `request`, `user`, `balances`, `requests` ve `login` yollarında örnek kayıtlarla Razor görünümleri açılır. Test host'u veri kaydetmez. 320, 390, 768, 1024 ve 1440 piksel genişliklerinde bu yedi ekranın sayfa taşması kontrol edilmiştir.

IDE'de `Unknown column 'l.manager_employee_portal_id'` hatası, şema betiğinin henüz uygulanmadığını gösterir. Yapılandırma `Test` iken `MEC.Portal.SmokeTests --check-test-schema` ile iki alan kontrol edilebilir; `--apply-test-schema` aynı test veritabanına eksik geçişi uygular. Kısmen uygulanmış şemada otomatik devam etmez. Üretimde yukarıdaki SQL geçişi kullanılır.
