namespace MEC.Portal.Models
{
    public class AnnouncementFormViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Eyebrow { get; set; } = "Duyuru yönetimi";
        public string PageTitle { get; set; } = "Yeni Duyuru Ekle";
        public string Description { get; set; } = "Çalışan portalında yayınlanacak duyuruyu başlık, içerik ve yayın durumu ile birlikte hazırlayın.";
        public string TitleLabel { get; set; } = "Duyuru Başlığı";
        public string TitlePlaceholder { get; set; } = "Duyuru başlığını giriniz";
        public string ContentLabel { get; set; } = "Duyuru İçeriği";
        public string EditorPlaceholder { get; set; } = "Duyuru detaylarını yazın, link veya görsel ekleyin...";
        public string AttachmentLabel { get; set; } = "Duyuru Ekleri";
        public string AttachmentHelpText { get; set; } = "İsterseniz duyuruya belge, doküman veya indirilebilir dosya ekleyebilirsiniz.";
        public string GalleryLabel { get; set; } = "Duyuru Galerisi";
        public string GalleryHelpText { get; set; } = "Detay sayfasında galeri alanında gösterilecek fotoğrafları buradan ekleyebilirsiniz.";
        public string SaveButtonText { get; set; } = "Kaydet";
        public string CancelButtonText { get; set; } = "İptal";
        public string ControllerName { get; set; } = "Announcement";
        public string IndexAction { get; set; } = "Index";
        public string EditAction { get; set; } = "Edit";
        public string DeleteAttachmentAction { get; set; } = "DeleteAttachment";
        public string DeleteImageAction { get; set; } = "DeleteImage";
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
