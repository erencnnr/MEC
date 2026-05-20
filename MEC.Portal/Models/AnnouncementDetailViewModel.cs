using System;
using System.Collections.Generic;

namespace MEC.Portal.Models
{
    public class AnnouncementDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsAdminView { get; set; }
        public string ContentBodyTitle { get; set; } = "Duyuru Metni";
        public string AttachmentSectionTitle { get; set; } = "Duyuru Ekleri";
        public string GallerySectionTitle { get; set; } = "Duyuru Galerisi";
        public string RelatedSectionTitle { get; set; } = "Diğer Duyurular";
        public string BackButtonText { get; set; } = "Tüm duyurulara dön";
        public string SideActionText { get; set; } = "Tüm Duyurular";
        public string BackController { get; set; } = "Home";
        public string BackAction { get; set; } = "Announcements";
        public string DetailController { get; set; } = "Announcement";
        public List<AnnouncementDetailLinkViewModel> Attachments { get; set; } = new();
        public List<string> GalleryImages { get; set; } = new();
        public List<AnnouncementDetailRelatedItemViewModel> RelatedAnnouncements { get; set; } = new();
    }

    public class AnnouncementDetailLinkViewModel
    {
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class AnnouncementDetailRelatedItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
