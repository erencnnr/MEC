using Microsoft.AspNetCore.Http;

namespace MEC.WebAPI.Models
{
    public class ImageRequestModel
    {
        public IFormFile File { get; set; } = default!;
        public int AssetId { get; set; }
        public string? Scope { get; set; }
        public int? EntityId { get; set; }
        public string? FileName { get; set; }
    }
}
