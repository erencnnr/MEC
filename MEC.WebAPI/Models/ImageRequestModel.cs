using Microsoft.AspNetCore.Http;

namespace MEC.WebAPI.Models
{
    public class ImageRequestModel
    {
        // Dosyanın kendisi
        public IFormFile File { get; set; }

        // Dosyanın ait olduğu demirbaş ID'si
        public int AssetId { get; set; }

        // (Opsiyonel) Eğer manuel isim göndermek isterseniz
        public string? FileName { get; set; }
    }
}
