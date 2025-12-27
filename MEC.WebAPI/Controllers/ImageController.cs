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

                string path = @"C:\Images";
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                // 2. İsimlendirme Formatı: DosyaAdı_AssetId.Uzantı
                // Örn: manzara.jpg ve AssetId:5  =>  manzara_5.jpg

                string originalName = Path.GetFileNameWithoutExtension(request.File.FileName);
                string extension = Path.GetExtension(request.File.FileName);

                // İstenilen format:
                string newFileName = $"{originalName}_{request.AssetId}{extension}";

                // 3. Kaydetme
                string filePath = Path.Combine(path, newFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(fileStream);
                }

                // 4. Cevap Dönme
                return Ok(new ImageResponseModel
                {
                    Success = true,
                    Message = "Dosya başarıyla yüklendi.",
                    FileName = newFileName // Yeni ismi dönüyoruz
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }
}
