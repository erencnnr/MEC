namespace MEC.Portal.Models
{
    public class AnnouncementFormViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<IFormFile> AttachmentFiles { get; set; } = new();
        public List<IFormFile> GalleryFiles { get; set; } = new();
        public List<AnnouncementAssetViewModel> ExistingAttachments { get; set; } = new();
        public List<AnnouncementAssetViewModel> ExistingGalleryImages { get; set; } = new();
    }

    public class AnnouncementAssetViewModel
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
