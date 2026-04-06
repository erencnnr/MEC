using System.Net.Http.Headers;
using System.Text.Json;

namespace MEC.Portal.Services
{
    public interface IAnnouncementAttachmentApiClient
    {
        Task<AnnouncementAssetUploadResult> UploadAsync(int announcementId, IFormFile file, CancellationToken cancellationToken = default);
        Task<AnnouncementAssetDeleteResult> DeleteAsync(int announcementId, string fileName, CancellationToken cancellationToken = default);
        string GetFileUrl(int announcementId, string fileName);
    }

    public interface IAnnouncementImageApiClient
    {
        Task<AnnouncementAssetUploadResult> UploadAsync(int announcementId, IFormFile file, CancellationToken cancellationToken = default);
        Task<AnnouncementAssetDeleteResult> DeleteAsync(int announcementId, string fileName, CancellationToken cancellationToken = default);
        string GetFileUrl(int announcementId, string fileName);
    }

    public class AnnouncementAttachmentApiClient : IAnnouncementAttachmentApiClient
    {
        private const string Scope = "announcements-attachments";
        private readonly HttpClient _httpClient;

        public AnnouncementAttachmentApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<AnnouncementAssetUploadResult> UploadAsync(int announcementId, IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadInternalAsync("api/Attachment/upload", Scope, announcementId, file, cancellationToken);
        }

        public Task<AnnouncementAssetDeleteResult> DeleteAsync(int announcementId, string fileName, CancellationToken cancellationToken = default)
        {
            return DeleteInternalAsync($"api/Attachment/delete?scope={Scope}&entityId={announcementId}&fileName={Uri.EscapeDataString(fileName)}", cancellationToken);
        }

        public string GetFileUrl(int announcementId, string fileName)
        {
            return BuildFileUrl($"api/Attachment/file?scope={Scope}&entityId={announcementId}&fileName={Uri.EscapeDataString(fileName)}");
        }

        private async Task<AnnouncementAssetUploadResult> UploadInternalAsync(string endpoint, string scope, int announcementId, IFormFile file, CancellationToken cancellationToken)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AnnouncementAssetUploadResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var formData = new MultipartFormDataContent();
            await using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

            formData.Add(fileContent, "file", file.FileName);
            formData.Add(new StringContent(scope), "scope");
            formData.Add(new StringContent(announcementId.ToString()), "entityId");

            using var response = await _httpClient.PostAsync(endpoint, formData, cancellationToken);
            return await AnnouncementAssetApiResponseReader.ReadUploadResponseAsync(response, cancellationToken);
        }

        private async Task<AnnouncementAssetDeleteResult> DeleteInternalAsync(string endpoint, CancellationToken cancellationToken)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AnnouncementAssetDeleteResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return AnnouncementAssetDeleteResult.Success();
            }

            var message = await AnnouncementAssetApiResponseReader.ReadMessageAsync(response, cancellationToken);
            return AnnouncementAssetDeleteResult.Fail(message);
        }

        private string BuildFileUrl(string relativeUrl)
        {
            if (_httpClient.BaseAddress == null)
            {
                return "#";
            }

            return new Uri(_httpClient.BaseAddress, relativeUrl).ToString();
        }
    }

    public class AnnouncementImageApiClient : IAnnouncementImageApiClient
    {
        private const string Scope = "announcements-gallery";
        private readonly HttpClient _httpClient;

        public AnnouncementImageApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<AnnouncementAssetUploadResult> UploadAsync(int announcementId, IFormFile file, CancellationToken cancellationToken = default)
        {
            return UploadInternalAsync("api/Image/upload", Scope, announcementId, file, cancellationToken);
        }

        public Task<AnnouncementAssetDeleteResult> DeleteAsync(int announcementId, string fileName, CancellationToken cancellationToken = default)
        {
            return DeleteInternalAsync($"api/Image/delete?scope={Scope}&entityId={announcementId}&fileName={Uri.EscapeDataString(fileName)}", cancellationToken);
        }

        public string GetFileUrl(int announcementId, string fileName)
        {
            return BuildFileUrl($"api/Image/file?scope={Scope}&entityId={announcementId}&fileName={Uri.EscapeDataString(fileName)}");
        }

        private async Task<AnnouncementAssetUploadResult> UploadInternalAsync(string endpoint, string scope, int announcementId, IFormFile file, CancellationToken cancellationToken)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AnnouncementAssetUploadResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var formData = new MultipartFormDataContent();
            await using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

            formData.Add(fileContent, "file", file.FileName);
            formData.Add(new StringContent(scope), "scope");
            formData.Add(new StringContent(announcementId.ToString()), "entityId");

            using var response = await _httpClient.PostAsync(endpoint, formData, cancellationToken);
            return await AnnouncementAssetApiResponseReader.ReadUploadResponseAsync(response, cancellationToken);
        }

        private async Task<AnnouncementAssetDeleteResult> DeleteInternalAsync(string endpoint, CancellationToken cancellationToken)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AnnouncementAssetDeleteResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return AnnouncementAssetDeleteResult.Success();
            }

            var message = await AnnouncementAssetApiResponseReader.ReadMessageAsync(response, cancellationToken);
            return AnnouncementAssetDeleteResult.Fail(message);
        }

        private string BuildFileUrl(string relativeUrl)
        {
            if (_httpClient.BaseAddress == null)
            {
                return "#";
            }

            return new Uri(_httpClient.BaseAddress, relativeUrl).ToString();
        }
    }

    public sealed record AnnouncementAssetUploadResult(
        bool IsSuccess,
        string Message,
        string FileName,
        string RelativePath,
        string ContentType)
    {
        public static AnnouncementAssetUploadResult Success(string fileName, string relativePath, string contentType)
        {
            return new AnnouncementAssetUploadResult(true, string.Empty, fileName, relativePath, contentType);
        }

        public static AnnouncementAssetUploadResult Fail(string message)
        {
            return new AnnouncementAssetUploadResult(false, message, string.Empty, string.Empty, string.Empty);
        }
    }

    public sealed record AnnouncementAssetDeleteResult(bool IsSuccess, string Message)
    {
        public static AnnouncementAssetDeleteResult Success()
        {
            return new AnnouncementAssetDeleteResult(true, string.Empty);
        }

        public static AnnouncementAssetDeleteResult Fail(string message)
        {
            return new AnnouncementAssetDeleteResult(false, message);
        }
    }

    internal static class AnnouncementAssetApiResponseReader
    {
        public static async Task<AnnouncementAssetUploadResult> ReadUploadResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    try
                    {
                        using var document = JsonDocument.Parse(payload);
                        var root = document.RootElement;
                        var fileName = root.TryGetProperty("fileName", out var fileNameElement)
                            ? fileNameElement.GetString() ?? string.Empty
                            : string.Empty;
                        var relativePath = root.TryGetProperty("relativePath", out var relativePathElement)
                            ? relativePathElement.GetString() ?? string.Empty
                            : string.Empty;
                        var contentType = root.TryGetProperty("contentType", out var contentTypeElement)
                            ? contentTypeElement.GetString() ?? string.Empty
                            : string.Empty;

                        if (string.IsNullOrWhiteSpace(fileName))
                        {
                            return AnnouncementAssetUploadResult.Fail("Dosya yükleme yanıtı eksik döndü.");
                        }

                        return AnnouncementAssetUploadResult.Success(fileName, relativePath, contentType);
                    }
                    catch (JsonException)
                    {
                        // response body'si parse edilemezse success kabul edip minimal bilgi döneriz.
                    }
                }

                return AnnouncementAssetUploadResult.Fail("Dosya yükleme yanıtı eksik döndü.");
            }

            var message = await ReadMessageAsync(response, cancellationToken);
            return AnnouncementAssetUploadResult.Fail(message);
        }

        public static async Task<string> ReadMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    using var document = JsonDocument.Parse(responseContent);
                    if (document.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        return messageElement.GetString() ?? "Dosya işlemi başarısız.";
                    }
                }
                catch (JsonException)
                {
                    return responseContent;
                }
            }

            return "Dosya işlemi başarısız.";
        }
    }
}
