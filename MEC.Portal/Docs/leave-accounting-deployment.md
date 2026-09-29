# İzin hesabı geçişi ve yayın

## Hazırlık
1. Veritabanı ve `wwwroot/uploads/leave-agreements` klasörünü birlikte yedekleyin. Eski PDF dosyalarını silmeyin.
2. Eski portalı ve `MEC-AnnualLeave-0700` zamanlanmış görevini durdurun/devre dışı bırakın. Eski ve yeni sürüm aynı veritabanına birlikte yazmamalıdır.
3. Önceki izin politikası ve okul müdürü/job şemaları mevcut olmalıdır. Bu geçiş mevcut bakiyeleri değiştirmez. Yeni veritabanlarında önce temel uygulama şeması gerekir.
4. `DatabaseScripts/mysql_leave_accounting.sql` dosyasını hedef MySQL veritabanında çalıştırın. Tablo ve indeks kurulumları tekrar çalıştırılabilir; kısmen kurulmuş tabloların kolonlarını ayrıca doğrulayın. Uygulamayı çalıştırmadan şema adımını tamamlayın.
5. Yeni yayını oluşturun: `dotnet publish MEC.Portal/MEC.Portal.csproj -c Release -o <yayın-klasörü>`.
6. Yeni yayın klasöründe, görevle aynı güvenli bağlantı yapılandırmasıyla `dotnet MEC.Portal.dll --initialize-leave-accounts` çalıştırın. Bu komut bakiyeyi tek açılış hareketine taşır; mevcut onaylı izinleri yeniden düşmez. Tekrar çalıştırılması yeni açılış üretmez. Bütün personellerde tamamlandığını doğrulayın.
7. Eski job tarihinin bulunmadığı ve geçmiş yılları belirsiz olan personel `NeedsReview` olarak ayrılır. Admin personel detayındaki **İzin hesabı** ekranından tarihli mutabakat yapmalıdır. Eski yıl hakları tahmin edilmez.
8. **Ayarlar > Tatil Takvimi** üzerinden mevcut taleplerin kapsadığı bütün yılları hazırlayın. Resmî aktarım taslaktır; bayram/arife ve sabit tatilleri doğrulayıp önizlemeden kullanıma açın. Kaynağa erişilemiyorsa elle giriş yapın. Onaysız yıl talep ve nihai onayı engeller.
9. **İzin hesabı** ve **İptal Talepleri** ekranlarını Admin; iptal kararını Genel Müdürlük rolüyle kontrol edin. Personelde Excel bağlantıları ve admin ekranları açılmamalıdır.
10. Yeni portalı açın, yalnız yeni yayını işaret eden mevcut job görevini etkinleştirin. `--annual-leave-job` bir kez çalıştırılarak **Ayarlar > İzin job sonuçları** sayfasında hatalar kontrol edilir. Başarısız personel işlenmiş sayılmaz; sonraki çalıştırmada yeniden denenir.

## İşleyiş
- Bakiye 1 Ocak'ta sıfırlanmaz. Her tamamlanan çalışma yılı hakkı bir kez eklenir; kıdem ve yaş kuralları korunur.
- `LeaveDays` ekranların kullandığı toplamdır; kalıcı kaynak hareketlerdir. Doğrudan SQL ile bu alanı değiştirmeyin. Gerekçeli artı/eksi düzeltmeyi **İzin hesabı** üzerinden yapın.
- Telefon/adres güncellemesi hak ediş veya bakiye yazmaz. Tarih düzeltmesi eski/yeni tarih ve fark önizlemesi gerektirir. Önizlemeden sonra kayıt değişirse işlem reddedilir.
- Mutabakat günü dahil önceki günler açılışın içindedir. Sınırı aşan eski iznin hesaplama kuralları bulunamazsa admin izin detayında gün dağılımını doğrular.
- Değişen mutabakat hemen hesaplamaya girer; eski imza ve PDF önceki sürümde kalır. Yeni sürüm için PDF yüklenip imza durumu ayrıca doğrulanır.
- İşten çıkış geleceğe planlanamaz. Yeniden işe giriş yeni tarih ve mutabakatla yapılır.
- Excel içerik kimliği dosya adına ve satır sırasına bağlı değildir. Aynı e-postanın birden fazla satırı reddedilir. Yarım yüklemede başarılı satırlar tekrar uygulanmaz; yükleme geçmişi ve satır kayıtları korunur.
- Başlamamış onaylı izin için personel iptal ister. Admin/Genel Müdürlük kabulünde gerçek düşüm bir kez iade edilir. Başlamış taleplerde manuel düzeltme gerekir.
- İşlemler önce ortak takvim kilidini, ardından personel kilidini alır. Bu ilk sürüm güvenli tutarlılık için bakiye yazmalarını sıraya koyar; yüksek hacimde kilit bekleme sürelerini izleyin.

## Doğrulama
```powershell
dotnet build MEC.Portal/MEC.Portal.csproj -c Release
dotnet run --project MEC.WorkflowSmokeTests -c Release
dotnet run --project MEC.Portal.SmokeTests -c Release
dotnet run --project MEC.LeaveAccountingTests -c Release
# Yalnız izole MySQL test sunucusunun bağlantısını ortam değişkeninde sağlayın:
dotnet run --project MEC.LeaveAccountingTests -c Release -- --mysql
```
Son komut `MEC_LEAVE_TEST_MYSQL` gerektirir. Üretim yapılandırmasını okumaz; rastgele `mec_leave_test_<guid>` veritabanı oluşturup sonunda yalnız onu kaldırır. Test hesabında test veritabanı oluşturma/silme izni gerekir.

Yeni şemayı EF modelinden yeniden üretmek için:
`dotnet run --project MEC.LeaveAccountingTests -c Release -- --generate-schema MEC.Portal/DatabaseScripts/mysql_leave_accounting.sql`

## Geri dönüş
Yeni sürüm hiç yazmadıysa eski yayın ve görev yedekten döndürülebilir. Yeni hareketler oluştuktan sonra eski uygulamayı aynı canlı veritabanına açmayın: yeni kayıtlar korunarak ayrı veritabanında geri dönüş mutabakatı yapılmalı veya veritabanı ve PDF yedeği birlikte geri yüklenmelidir.
