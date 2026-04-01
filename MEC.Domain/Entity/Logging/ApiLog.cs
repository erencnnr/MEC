using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEC.Domain.Entity.Logging
{
    [Table("api_log")]
    public class ApiLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("ip_address")]
        [MaxLength(64)]
        public string IpAddress { get; set; } = string.Empty;

        [Column("mac_address")]
        [MaxLength(64)]
        public string? MacAddress { get; set; }

        [Required]
        [Column("user")]
        [MaxLength(256)]
        public string User { get; set; } = string.Empty;

        [Required]
        [Column("timestamp", TypeName = "datetime(6)")]
        public DateTime Timestamp { get; set; }

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("level")]
        [MaxLength(32)]
        public string Level { get; set; } = string.Empty;

        [Required]
        [Column("method_name")]
        [MaxLength(128)]
        public string MethodName { get; set; } = string.Empty;

        [Column("request_path")]
        [MaxLength(512)]
        public string? RequestPath { get; set; }

        [Column("http_method")]
        [MaxLength(16)]
        public string? HttpMethod { get; set; }

        [Column("status_code")]
        public int? StatusCode { get; set; }

        [Column("request_body")]
        public string? RequestBody { get; set; }

        [Column("response_body")]
        public string? ResponseBody { get; set; }

        [Column("query_string")]
        public string? QueryString { get; set; }
    }
}
