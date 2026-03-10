using MEC.Domain.Common;
using System;

namespace MEC.Domain.Entity.School // Klasör yoluna göre namespace'i ayarlayabilirsin
{
    public class Announcement : BaseEntity
    {
        public string Title { get; set; } // Duyuru Başlığı
        public string Content { get; set; } // Duyuru İçeriği
        public bool IsActive { get; set; } = true; // Duyuru ana sayfada görünsün mü?

        // BaseEntity'de yoksa diye ekliyorum, eğer BaseEntity'de CreatedDate varsa buraya yazmana gerek yok.
        // public DateTime CreatedDate { get; set; } = DateTime.Now; 
    }
}