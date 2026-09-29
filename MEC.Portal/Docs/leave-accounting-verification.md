# İzin sistemi doğrulaması
Tarih: 29 Eylül 2026.

- Portal Release derlemesi başarılı.
- MEC.WorkflowSmokeTests: 20 mevcut kontrol geçti.
- MEC.Portal.SmokeTests: saat dilimi, EF uyumluluğu, rol yenileme ve personel formu kontrolleri geçti.
- MEC.LeaveAccountingTests: hesaplama kontrolleri ve izole MySQL 8.4.8 üzerinde kabul testleri geçti. Her çalıştırma rastgele bir test veritabanı oluşturup sonunda kaldırır; canlı bağlantı yapılandırması kullanılmaz.
- Eşzamanlı sekiz job tek yıllık hak verdi; beş Excel tekrarı, beş onay ve beş iptal kararı bakiyeyi yalnız bir kez değiştirdi.
- 14 → 28 → 42 birikim, 5 gün kullanım sonrası 37 gün; 1 Ocak'ta sıfırlanmama; manuel hareketin korunması; işe giriş düzeltmesiyle mükerrer hak oluşmaması doğrulandı.
- Negatif mutabakat, mutabakat günü dahil bölme, geç onay incelemesi, açılışa dahil günlerin iade kaydı, eski imzalı PDF sürümünün korunması doğrulandı.
- Profil güncellemesi bakiye hareketi ve job tarihini değiştirmedi. Eski önizleme reddedildi.
- Takvim/cumartesi değişimi gelecekteki izin ve bakiyeyi birlikte güncelledi; başlamış izinlerin hesabı değişmedi.
- Başlamış izin için iptal iadesi yapılmadı. Bildirim işlem sonrasına ertelendi; bildirim hatası tamamlanmış hesabı geri almadı.
- Çıkışta hata enjekte edildiğinde aktiflik ve bakiye birlikte geri alındı. Başarılı çıkışta çıkış sonrası hak geri alındı. Yeniden giriş yeni tarih ve mutabakat kullandı.
- Yetkisiz Excel servis çağrısı reddedildi; Excel yükleme rotasının Admin zorunluluğu kontrol edildi.
- Geçiş SQL'i iki kez çalıştırıldı, açılış kurulumu tekrarlandı; mükerrer kayıt oluşmadı.
- Personel hatası job'ın diğer personelleri işlemesini engellemedi.
- Tarayıcıda hesap, takvim, politika önizleme, job, iptal ve yükleme geçmişi görünümleri açıldı. Masaüstü ve 390 piksel görünümde sayfa taşması bulunmadı. Personel menüsünde yönetim görünmedi; Genel Müdürlük menüsünde onay işlemleri göründü.
- İptal talepleri için personel adı/e-posta, durum ve izin başlangıç tarihi filtreleri ile sayfa sınırlandırması izole MySQL üzerinde doğrulandı. Sonuçlanmış talepler bekleyen listesinden çıktı; personel rolünün listeye erişimi reddedildi. Ayrı karar ekranı ve satıra tıklayarak geçiş tarayıcıda kontrol edildi; liste ve detay 390 piksel görünümde sayfayı yatay taşırmadı.

## Kaynak ve yayın sınırı
Diyanet'in boş User-Agent başlığıyla gelen uygulama isteğine HTTP 403 döndürdüğü yeniden üretildi. İsteklere `MEC-Portal/1.0` kimliği eklendi; tırnaksız bağlantılar ve `dinigunler.php?yil=` sayfaları desteklendi. Uygulamanın gerçek HTTP istemcisiyle 2026 ve 2027 listeleri canlı kaynaktan alındı ve her biri 17 tatil kaydıyla doğrulandı. Bu okuma testleri veritabanına yazmaz; adminin kaydetme/onaylama işleminin yerine geçmez. Başarısız aktarım açık hata mesajı gösterir; takvim onaylanmaz. Yayın ortamında kaynak erişimi ayrıca kontrol edilmelidir.

Veritabanına dokunmadan kaynağı tekrar kontrol etmek için:
`dotnet run --project MEC.LeaveAccountingTests -c Release -- --official-calendar 2026`

Canlı veritabanına geçiş ve yayın yapılmadı. Yayın öncesinde `leave-accounting-deployment.md` adımları uygulanıp gerekli takvimler admin tarafından doğrulanmalıdır.
