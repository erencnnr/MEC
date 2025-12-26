using Microsoft.AspNetCore.Mvc;

namespace MEC.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttachmentController : ControllerBase
    {
        // Klasör yolunu sabit bir değişkene alıyoruz ki her yerde aynısı olsun.
        private readonly string _folderPath = @"C:\Attachments";

        [HttpPost("UploadAttachment")]
        public async Task<IActionResult> UploadAttachment(IFormFile file, [FromForm] int assetId)
        {
            // 1. Dosya kontrolü (PDF Zorunluluğunu KALDIRDIK)
            if (file == null || file.Length == 0)
                return BadRequest("Lütfen geçerli bir dosya seçiniz.");

            /* Eğer her dosyayı kabul etmek istiyorsan bu kısmı siliyoruz:
               if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
                   return BadRequest("Sadece PDF dosyaları yüklenebilir.");
            */

            try
            {
                // 2. Klasör kontrolü
                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }

                // 3. Dosya isimlendirme
                // Dosyanın orijinal uzantısını (.png, .docx, .pdf vs.) alıyoruz
                string extension = Path.GetExtension(file.FileName);

                // İsimlendirmeyi uzantıya göre dinamik yapıyoruz
                var fileName = $"File_{assetId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";

                var filePath = Path.Combine(_folderPath, fileName);

                // 4. Dosyayı kaydetme
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { message = "Dosya başarıyla yüklendi", fileName = fileName, path = filePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Dosya yüklenirken hata oluştu: {ex.Message}");
            }
        }

        [HttpGet("GetAttachment")]
        public IActionResult GetAttachment(string fileName)
        {
            // DÜZELTME BURADA:
            // Artık projenin klasörüne değil, yükleme yaptığımız C:\Attachments klasörüne bakıyoruz.
            string filePath = Path.Combine(_folderPath, fileName);

            // Dosya gerçekten var mı diye kontrol ediyoruz.
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Dosya şu konumda bulunamadı: {filePath}");
            }

            // Dosyayı byte dizisi olarak okuyoruz.
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Dosyayı geriye döndürüyoruz. 
            // Dosya isminden MIME type'ı (dosya türünü) otomatik bulmaya çalışabiliriz ama
            // şimdilik 'application/octet-stream' her türlü dosyayı indirir.
            return File(fileBytes, "application/octet-stream", fileName);
        }
    }
}