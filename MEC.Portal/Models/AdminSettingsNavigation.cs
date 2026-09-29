namespace MEC.Portal.Models;

public static class AdminSettingsNavigation
{
    public static IReadOnlyList<SettingsGroup> Groups { get; } =
    [
        new("Kullanıcılar ve okullar",
        [
            new("users", "Portal Kullanıcıları", "/Admin/PortalUsers", "Kullanıcı bilgileri, yetkiler ve aktiflik durumları."),
            new("schools", "Okullar", "/Admin/Schools", "Okullar ve okul müdürü atamaları.")
        ]),
        new("İzin ayarları",
        [
            new("calendar", "Tatil Takvimi", "/Admin/LeaveAccounts/Calendar", "Yıllık resmî tatiller ve takvim onayları."),
            new("policy", "Cumartesi Kuralı", "/Admin/LeaveAccounts/Policy", "Cumartesi günlerinin izin hesabına etkisi."),
            new("imports", "Excel Yükleme Geçmişi", "/Admin/LeaveAccounts/Imports", "Toplu yüklemelerin sonuçları ve hatalı satırlar."),
            new("jobs", "İzin Job Sonuçları", "/Admin/LeaveAccounts/Jobs", "Otomatik hak ediş işlemleri ve hata kayıtları.")
        ]),
        new("Portal içerikleri",
        [
            new("slider", "Slider", "/Admin/Slider", "Ana sayfada gösterilen görseller."),
            new("birthday", "Doğum Günü Popup", "/Admin/BirthdayPopup", "Doğum günü duyurusunda kullanılan görsel."),
            new("food", "Yemek Menüsü", "/Admin/FoodMenu", "Aylık yemek menüleri ve belgeleri."),
            new("surveys", "Anket Yönetimi", "/Admin/Surveys", "Anketler, sorular ve yanıtlar.")
        ])
    ];
}

public record SettingsGroup(string Title, IReadOnlyList<SettingsLink> Links);
public record SettingsLink(string Key, string Title, string Url, string Description);
