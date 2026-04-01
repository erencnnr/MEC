using System.Net.Http.Headers;
using System.Text.Json;

namespace MEC.Portal.Services
{
    public interface IAttachmentApiClient
    {
        Task<AttachmentUploadResult> UploadLeaveAttachmentAsync(int leaveId, IFormFile file, CancellationToken cancellationToken = default);
    }

    public class AttachmentApiClient : IAttachmentApiClient
    {
        private readonly HttpClient _httpClient;

        public AttachmentApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AttachmentUploadResult> UploadLeaveAttachmentAsync(int leaveId, IFormFile file, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AttachmentUploadResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var formData = new MultipartFormDataContent();
            await using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

            formData.Add(fileContent, "file", file.FileName);
            formData.Add(new StringContent("leave-requests"), "scope");
            formData.Add(new StringContent(leaveId.ToString()), "entityId");

            using var response = await _httpClient.PostAsync("api/Attachment/upload", formData, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return AttachmentUploadResult.Success();
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    using var document = JsonDocument.Parse(responseContent);
                    if (document.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        return AttachmentUploadResult.Fail(messageElement.GetString() ?? "Ek dosya yüklenemedi.");
                    }
                }
                catch (JsonException)
                {
                    // Mesaj düz metinse alttaki fallback çalışacak.
                }
            }

            return AttachmentUploadResult.Fail(string.IsNullOrWhiteSpace(responseContent)
                ? "Ek dosya yüklenemedi."
                : responseContent);
        }
    }

    public sealed record AttachmentUploadResult(bool IsSuccess, string Message)
    {
        public static AttachmentUploadResult Success()
        {
            return new AttachmentUploadResult(true, string.Empty);
        }

        public static AttachmentUploadResult Fail(string message)
        {
            return new AttachmentUploadResult(false, message);
        }
    }
}
