namespace MEC.Application.Abstractions.Service.LoggingService.Model
{
    public class ApiLogEntryModel
    {
        public string IpAddress { get; set; } = string.Empty;
        public string? MacAddress { get; set; }
        public string User { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public string? RequestPath { get; set; }
        public string? HttpMethod { get; set; }
        public int? StatusCode { get; set; }
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
        public string? QueryString { get; set; }
    }
}
