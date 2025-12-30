using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles; // MIME türleri için (Gerekirse NuGet'ten ekleyin, yoksa manuel switch kullanın)
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MEC.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttachmentController : ControllerBase
    {
        // Ana kök klasör
        private readonly string _rootPath = @"C:\Attachments";

        // 1. DOSYA YÜKLEME (Update mantığı dahil: Varsa üzerine yazar)
        [HttpPost("UploadAttachment")]
        public async Task<IActionResult> UploadAttachment(IFormFile file, [FromForm] int assetId)
        {
            try
            {
                // Dosya Kontrolü
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "Lütfen geçerli bir dosya seçiniz." });

                if (assetId <= 0)
                    return BadRequest(new { success = false, message = "Geçersiz Asset ID." });

                // Klasör Yolu: C:\Attachments\{AssetId}
                string assetFolderPath = Path.Combine(_rootPath, assetId.ToString());

                // Klasör yoksa oluştur
                if (!Directory.Exists(assetFolderPath))
                {
                    Directory.CreateDirectory(assetFolderPath);
                }

                // Dosya Adı (Orijinal isim)
                string fileName = file.FileName;
                string filePath = Path.Combine(assetFolderPath, fileName);

                // Kaydetme (FileMode.Create: Dosya varsa üzerine yazar/günceller)
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new
                {
                    success = true,
                    message = "Dosya başarıyla yüklendi/güncellendi.",
                    fileName = fileName,
                    path = filePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Dosya yüklenirken hata oluştu: " + ex.Message });
            }
        }

        // 2. DOSYA LİSTELEME
        [HttpGet("GetAttachmentList")]
        public IActionResult GetAttachmentList(int assetId)
        {
            try
            {
                string assetFolderPath = Path.Combine(_rootPath, assetId.ToString());

                // Klasör yoksa boş liste dönelim (Hata değil, henüz dosya yok demek)
                if (!Directory.Exists(assetFolderPath))
                {
                    return Ok(new { success = true, files = new List<string>() });
                }

                // Klasördeki dosyaları al
                var filePaths = Directory.GetFiles(assetFolderPath);

                // Sadece dosya isimlerini listeye çevir
                var fileNames = filePaths.Select(Path.GetFileName).ToList();

                return Ok(new { success = true, files = fileNames });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Liste alınırken hata oluştu: " + ex.Message });
            }
        }

        // 3. DOSYA İNDİRME / GÖRÜNTÜLEME
        [HttpGet("GetAttachment")]
        public IActionResult GetAttachment(int assetId, string fileName)
        {
            try
            {
                // Güvenlik: Directory Traversal engelleme
                if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                    return BadRequest("Geçersiz dosya adı.");

                string filePath = Path.Combine(_rootPath, assetId.ToString(), fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "Dosya bulunamadı." });
                }

                // Dosyayı oku
                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                // MIME Türünü Belirle
                string contentType = GetContentType(fileName);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Dosya okunurken hata: " + ex.Message });
            }
        }

        // 4. DOSYA SİLME (Sadece Diskten)
        [HttpDelete("DeleteAttachment")]
        public IActionResult DeleteAttachment(int assetId, string fileName)
        {
            try
            {
                string filePath = Path.Combine(_rootPath, assetId.ToString(), fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return Ok(new { success = true, message = "Dosya diskten silindi." });
                }
                else
                {
                    return Ok(new { success = true, message = "Dosya zaten mevcut değil." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Disk hatası: " + ex.Message });
            }
        }

        // Yardımcı Metot: Dosya uzantısına göre Content-Type belirler
        private string GetContentType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();

            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream" // Bilinmeyen türler için genel binary
            };
        }
    }
}