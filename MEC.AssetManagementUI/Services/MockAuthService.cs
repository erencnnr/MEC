

namespace MEC.AssetManagementUI.Services
{
    public class MockAuthService : IAuthService
    {
        public bool ValidateLogin(string username, string password)
        {
            // ŞİMDİLİK: Elle kontrol ediyoruz.
            // İLERİDE: Buraya Entity Framework / SQL kodları gelecek.
            if (username == "admin" && password == "12345")
            {
                return true;
            }

            return false;
        }
    }
}