using System;
using System.Collections.Generic;

namespace MEC.Portal.Models
{
    public class AnnouncementListViewModel
    {
        public List<AnnouncementCardViewModel> Items { get; set; } = new();
        public string Status { get; set; } = "active";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 6;
        public string Eyebrow { get; set; } = "Duyuru yönetimi";
        public string Title { get; set; } = "Duyurular";
        public string Description { get; set; } = "Aktif ve pasif duyuruları tarih sırasıyla görüntüleyin, kartlardan seçerek güncelleme ekranına geçin.";
        public string CreateButtonText { get; set; } = "Yeni Duyuru";
        public string EmptyTitle { get; set; } = "Listelenecek duyuru bulunmuyor.";
        public string EmptyDescription { get; set; } = "Seçili filtre için henüz bir duyuru kaydı yok. Yeni bir duyuru oluşturabilirsiniz.";
        public string IndexAction { get; set; } = "Index";
        public string CreateAction { get; set; } = "Create";
        public string EditAction { get; set; } = "Edit";
    }

    public class AnnouncementCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
