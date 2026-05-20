namespace MEC.Portal.Options
{
    public class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseStartTls { get; set; } = true;
        public string DisplayName { get; set; } = "Portal";
        public string FromAddress { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class NotificationOptions
    {
        public string ApproverFallbackEmail { get; set; } = string.Empty;
    }

    public class PortalUrlOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
    }
}
