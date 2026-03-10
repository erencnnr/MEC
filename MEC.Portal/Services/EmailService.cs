using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MEC.Portal.Services
{
    // Arayüz (Interface) tanımlaması
    public interface IEmailService
    {
        Task SendLeaveRequestEmailAsync(string userEmail, string startDate, string endDate, string reason);
    }

    // Servis Sınıfı
    public class EmailService : IEmailService
    {
        public async Task SendLeaveRequestEmailAsync(string userEmail, string startDate, string endDate, string reason)
        {
            // GÖNDERİCİ (Sistemin kendi mail adresi - Şimdilik test için Gmail/Outlook yazabilirsin)
            var fromAddress = new MailAddress("unalmehmetcetin@gmail.com", "MEC Portal");

            // ALICI (İzin talebinin kime gideceği - Örneğin bir yöneticinin maili)
            var toAddress = new MailAddress("themehmetoyunda@gmail.com", "Yönetici");

            // Gönderici mailin şifresi (Gmail kullanıyorsan "Uygulama Şifresi" alman gerekir)
            const string fromPassword = "mail_sifresi_buraya";

            const string subject = "MEC Portal - Yeni İzin Talebi Bildirimi";

            // Yöneticiye gidecek olan HTML formatındaki şık mail içeriği
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                    <h2 style='color: #0056b3;'>Yeni İzin Talebi Onay Bekliyor</h2>
                    <p>Sistem üzerinden yeni bir personel izin talebinde bulundu. Detaylar aşağıdadır:</p>
                    <ul>
                        <li><strong>Talep Eden:</strong> {userEmail}</li>
                        <li><strong>Başlangıç Tarihi:</strong> {startDate}</li>
                        <li><strong>Bitiş Tarihi:</strong> {endDate}</li>
                    </ul>
                    <p><strong>Açıklama / Neden:</strong></p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #0056b3;'>
                        {reason}
                    </div>
                    <br/>
                    <p><em>Lütfen portal üzerinden giriş yaparak talebi değerlendiriniz.</em></p>
                </div>";

            // SMTP Ayarları (Örnek olarak Gmail ayarları yazılmıştır, şirket mailin varsa değiştirebilirsin)
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true // Mailin HTML olarak yorumlanmasını sağlar
            })
            {
                // Maili asenkron olarak gönder
                await smtp.SendMailAsync(message);
            }
        }
    }
}