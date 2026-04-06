namespace MEC.WebAPI.Models
{
    public class ImageResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string RelativePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }
}
