namespace MEC.AssetManagementUI.Services
{
    public interface IAuthService
    {
        // İleride burası veritabanına soracak
        bool ValidateLogin(string username, string password);
    }
}
