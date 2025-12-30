using Microsoft.AspNetCore.Mvc;
using MEC.WebAPI.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MEC.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        [HttpPost("Upload")]
        public async Task<IActionResult> UploadImage([FromForm] ImageRequestModel request)
        {
            try
            {
                // 1. Dosya Kontrolü
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Dosya seçilmedi." });
                }

                if (request.AssetId <= 0)
                {
                    return BadRequest(new { success = false, message = "Geçersiz Asset ID." });
                }

                // 2. Klasör Yolu Oluşturma: C:\Images\{AssetId}
                // Not: Web projesi ise wwwroot/uploads/... kullanmanız daha sağlıklı olabilir.
                string rootPath = @"C:\Images";
                string assetFolderPath = Path.Combine(rootPath, request.AssetId.ToString());

                // Klasör yoksa oluştur
                if (!Directory.Exists(assetFolderPath))
                {
                    Directory.CreateDirectory(assetFolderPath);
                }

                // 3. Dosya Adı ve Yolu
                // Orijinal dosya adını kullanıyoruz (örn: manzara.jpg)
                string fileName = request.File.FileName;

                // Tam dosya yolu: C:\Images\5\manzara.jpg
                string filePath = Path.Combine(assetFolderPath, fileName);

                // 4. Kaydetme veya Üzerine Yazma
                // FileMode.Create: Dosya yoksa oluşturur, varsa içeriğini silip üzerine yazar (Güncelleme mantığı).
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(fileStream);
                }

                // 5. Cevap Dönme
                // DB'ye sadece dosya adını kaydedecekseniz fileName, 
                // klasör yapısıyla kaydedecekseniz relative path dönebilirsiniz.
                // Şimdilik sadece dosya adını dönüyoruz, DB kaydını yapan metod AssetId'yi zaten biliyor.
                return Ok(new ImageResponseModel
                {
                    Success = true,
                    Message = "Dosya başarıyla yüklendi/güncellendi.",
                    FileName = fileName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Hata: " + ex.Message });
            }
        }
        [HttpGet("GetImageList")]
        public IActionResult GetImageList(int assetId)
        {
            try
            {
                string rootPath = @"C:\Images";
                string assetFolderPath = Path.Combine(rootPath, assetId.ToString());

                // Klasör kontrolü
                if (!Directory.Exists(assetFolderPath))
                {
                    // Klasör yoksa boş liste dönebiliriz veya 404 verebiliriz.
                    // Mantıken hiç resim yüklenmediyse klasör yoktur, bu bir hata değil durumdur.
                    return NotFound(new { success = false, message = "Bu demirbaş için resim bulunamadı." });
                }

                // Klasördeki dosyaları al
                var filePaths = Directory.GetFiles(assetFolderPath);

                if (filePaths.Length == 0)
                {
                    return NotFound(new { success = false, message = "Klasör boş." });
                }

                // Sadece dosya isimlerini seçip listeye çeviriyoruz
                var fileNames = filePaths.Select(Path.GetFileName).ToList();

                return Ok(new { success = true, files = fileNames });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Hata: " + ex.Message });
            }
        }

        // 3. TEK BİR RESMİ GETİRME (Ekranda göstermek için)
        [HttpGet("GetImage/{assetId}/{fileName}")]
        public IActionResult GetImage(int assetId, string fileName)
        {
            try
            {
                // Güvenlik: Directory Traversal saldırılarını önlemek için basit bir kontrol
                if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                {
                    return BadRequest();
                }

                string path = Path.Combine(@"C:\Images", assetId.ToString(), fileName);

                if (!System.IO.File.Exists(path))
                    return NotFound();

                // Dosyayı okuyup stream olarak dönüyoruz
                var imageFileStream = System.IO.File.OpenRead(path);

                // MIME türünü belirleme (Basit yöntem)
                string contentType = "image/jpeg"; // Varsayılan
                string ext = Path.GetExtension(fileName).ToLower();
                if (ext == ".png") contentType = "image/png";
                else if (ext == ".gif") contentType = "image/gif";
                else if (ext == ".bmp") contentType = "image/bmp";
                else if (ext == ".webp") contentType = "image/webp";

                return File(imageFileStream, contentType);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpDelete("DeleteImage")]
        public IActionResult DeleteImage([FromQuery] int assetId, [FromQuery] string fileName)
        {
            try
            {
                // SADECE DİSK İŞLEMİ
                string path = Path.Combine(@"C:\Images", assetId.ToString(), fileName);

                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    return Ok(new { success = true, message = "Dosya diskten silindi." });
                }
                else
                {
                    // Dosya zaten yoksa da başarılı dönelim ki süreç bozulmasın
                    return Ok(new { success = true, message = "Dosya zaten mevcut değil." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Disk hatası: " + ex.Message });
            }
        }
    }
}
