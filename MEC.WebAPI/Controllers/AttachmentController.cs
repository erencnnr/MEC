using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace MEC.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttachmentController : ControllerBase
    {
        private const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".txt",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx"
        };

        private readonly string _rootPath;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

        public AttachmentController(IConfiguration configuration)
        {
            _rootPath = configuration["AttachmentSettings:RootPath"] ?? @"C:\Attachments";
        }

        // Legacy Asset endpoint - geriye uyumluluk korunur.
        [HttpPost("UploadAttachment")]
        public Task<IActionResult> UploadAttachment(IFormFile file, [FromForm] int assetId)
        {
            return UploadInternalAsync(file, "assets", assetId);
        }

        [HttpPost("upload")]
        public Task<IActionResult> Upload(IFormFile file, [FromForm] string scope, [FromForm] int entityId)
        {
            return UploadInternalAsync(file, scope, entityId);
        }

        [HttpGet("GetAttachmentList")]
        public IActionResult GetAttachmentList(int assetId)
        {
            return GetAttachmentListInternal("assets", assetId);
        }

        [HttpGet("list")]
        public IActionResult List(string scope, int entityId)
        {
            return GetAttachmentListInternal(scope, entityId);
        }

        [HttpGet("GetAttachment")]
        public IActionResult GetAttachment(int assetId, string fileName)
        {
            return GetAttachmentInternal("assets", assetId, fileName);
        }

        [HttpGet("file")]
        public IActionResult Download(string scope, int entityId, string fileName)
        {
            return GetAttachmentInternal(scope, entityId, fileName);
        }

        [HttpDelete("DeleteAttachment")]
        public IActionResult DeleteAttachment(int assetId, string fileName)
        {
            return DeleteAttachmentInternal("assets", assetId, fileName);
        }

        [HttpDelete("delete")]
        public IActionResult Delete(string scope, int entityId, string fileName)
        {
            return DeleteAttachmentInternal(scope, entityId, fileName);
        }

        private async Task<IActionResult> UploadInternalAsync(IFormFile file, string scope, int entityId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Lütfen geçerli bir dosya seçiniz." });
                }

                if (entityId <= 0)
                {
                    return BadRequest(new { success = false, message = "Geçersiz kayıt numarası." });
                }

                if (!TryNormalizeScope(scope, out var normalizedScope))
                {
                    return BadRequest(new { success = false, message = "Geçersiz klasör türü." });
                }

                if (file.Length > DefaultMaxFileSizeBytes)
                {
                    return BadRequest(new { success = false, message = "Dosya boyutu 10 MB sınırını aşıyor." });
                }

                var sanitizedFileName = SanitizeFileName(file.FileName);
                if (string.IsNullOrWhiteSpace(sanitizedFileName))
                {
                    return BadRequest(new { success = false, message = "Geçersiz dosya adı." });
                }

                var extension = Path.GetExtension(sanitizedFileName);
                if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Bu dosya türüne izin verilmiyor." });
                }

                var entityFolderPath = GetEntityFolderPath(normalizedScope, entityId);
                Directory.CreateDirectory(entityFolderPath);

                var filePath = Path.Combine(entityFolderPath, sanitizedFileName);

                await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await file.CopyToAsync(stream);

                return Ok(new
                {
                    success = true,
                    message = "Dosya başarıyla yüklendi.",
                    fileName = sanitizedFileName,
                    scope = normalizedScope,
                    entityId,
                    relativePath = Path.Combine(normalizedScope, entityId.ToString(), sanitizedFileName).Replace("\\", "/"),
                    contentType = GetContentType(filePath)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Dosya yüklenirken hata oluştu: " + ex.Message });
            }
        }

        private IActionResult GetAttachmentListInternal(string scope, int entityId)
        {
            try
            {
                if (entityId <= 0)
                {
                    return BadRequest(new { success = false, message = "Geçersiz kayıt numarası." });
                }

                if (!TryNormalizeScope(scope, out var normalizedScope))
                {
                    return BadRequest(new { success = false, message = "Geçersiz klasör türü." });
                }

                var entityFolderPath = GetEntityFolderPath(normalizedScope, entityId);
                if (!Directory.Exists(entityFolderPath))
                {
                    return Ok(new { success = true, files = new List<string>() });
                }

                var fileNames = Directory.GetFiles(entityFolderPath)
                    .Select(Path.GetFileName)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                return Ok(new { success = true, files = fileNames });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Liste alınırken hata oluştu: " + ex.Message });
            }
        }

        private IActionResult GetAttachmentInternal(string scope, int entityId, string fileName)
        {
            try
            {
                if (entityId <= 0)
                {
                    return BadRequest(new { success = false, message = "Geçersiz kayıt numarası." });
                }

                if (!TryNormalizeScope(scope, out var normalizedScope))
                {
                    return BadRequest(new { success = false, message = "Geçersiz klasör türü." });
                }

                var sanitizedFileName = SanitizeFileName(fileName);
                if (!string.Equals(fileName, sanitizedFileName, StringComparison.Ordinal))
                {
                    return BadRequest(new { success = false, message = "Geçersiz dosya adı." });
                }

                var filePath = Path.Combine(GetEntityFolderPath(normalizedScope, entityId), sanitizedFileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "Dosya bulunamadı." });
                }

                var contentType = GetContentType(filePath);
                return PhysicalFile(filePath, contentType, sanitizedFileName, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Dosya okunurken hata oluştu: " + ex.Message });
            }
        }

        private IActionResult DeleteAttachmentInternal(string scope, int entityId, string fileName)
        {
            try
            {
                if (entityId <= 0)
                {
                    return BadRequest(new { success = false, message = "Geçersiz kayıt numarası." });
                }

                if (!TryNormalizeScope(scope, out var normalizedScope))
                {
                    return BadRequest(new { success = false, message = "Geçersiz klasör türü." });
                }

                var sanitizedFileName = SanitizeFileName(fileName);
                if (!string.Equals(fileName, sanitizedFileName, StringComparison.Ordinal))
                {
                    return BadRequest(new { success = false, message = "Geçersiz dosya adı." });
                }

                var filePath = Path.Combine(GetEntityFolderPath(normalizedScope, entityId), sanitizedFileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok(new { success = true, message = "Dosya zaten mevcut değil." });
                }

                System.IO.File.Delete(filePath);
                return Ok(new { success = true, message = "Dosya silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Dosya silinirken hata oluştu: " + ex.Message });
            }
        }

        private string GetEntityFolderPath(string scope, int entityId)
        {
            return Path.Combine(_rootPath, scope, entityId.ToString());
        }

        private bool TryNormalizeScope(string scope, out string normalizedScope)
        {
            normalizedScope = (scope ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedScope))
            {
                return false;
            }

            if (normalizedScope.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-' && ch != '_'))
            {
                return false;
            }

            return true;
        }

        private static string SanitizeFileName(string fileName)
        {
            var sanitizedFileName = Path.GetFileName(fileName ?? string.Empty).Trim();
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitizedFileName = sanitizedFileName.Replace(invalidChar, '_');
            }

            return sanitizedFileName;
        }

        private string GetContentType(string filePath)
        {
            if (_contentTypeProvider.TryGetContentType(filePath, out var contentType))
            {
                return contentType;
            }

            return "application/octet-stream";
        }
    }
}
