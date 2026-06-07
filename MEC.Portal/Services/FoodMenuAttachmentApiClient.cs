using System.Net.Http.Headers;
using System.Text.Json;

namespace MEC.Portal.Services
{
    public interface IFoodMenuAttachmentApiClient
    {
        Task<FoodMenuAttachmentUploadResult> UploadAsync(int monthId, IFormFile file, CancellationToken cancellationToken = default);
        Task<FoodMenuAttachmentDeleteResult> DeleteAsync(int monthId, string fileName, CancellationToken cancellationToken = default);
        Task<FoodMenuAttachmentDownloadResult> DownloadAsync(int monthId, string fileName, CancellationToken cancellationToken = default);
        string GetFileUrl(int monthId, string fileName);
    }

    public class FoodMenuAttachmentApiClient : IFoodMenuAttachmentApiClient
    {
        private const string Scope = "food-menus";
        private readonly HttpClient _httpClient;

        public FoodMenuAttachmentApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<FoodMenuAttachmentUploadResult> UploadAsync(int monthId, IFormFile file, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return FoodMenuAttachmentUploadResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var formData = new MultipartFormDataContent();
            await using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

            formData.Add(fileContent, "file", file.FileName);
            formData.Add(new StringContent(Scope), "scope");
            formData.Add(new StringContent(monthId.ToString()), "entityId");

            using var response = await _httpClient.PostAsync("api/Attachment/upload", formData, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return FoodMenuAttachmentUploadResult.Fail(await ReadMessageAsync(response, cancellationToken));
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return FoodMenuAttachmentUploadResult.Fail("PDF yükleme yanıtı eksik döndü.");
            }

            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            return FoodMenuAttachmentUploadResult.Success(
                root.TryGetProperty("fileName", out var fileNameElement) ? fileNameElement.GetString() ?? string.Empty : string.Empty,
                root.TryGetProperty("relativePath", out var relativePathElement) ? relativePathElement.GetString() ?? string.Empty : string.Empty,
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        }

        public async Task<FoodMenuAttachmentDeleteResult> DeleteAsync(int monthId, string fileName, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return FoodMenuAttachmentDeleteResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            var url = $"api/Attachment/delete?scope={Scope}&entityId={monthId}&fileName={Uri.EscapeDataString(fileName)}";
            using var response = await _httpClient.DeleteAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return FoodMenuAttachmentDeleteResult.Success();
            }

            return FoodMenuAttachmentDeleteResult.Fail(await ReadMessageAsync(response, cancellationToken));
        }

        public async Task<FoodMenuAttachmentDownloadResult> DownloadAsync(int monthId, string fileName, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return FoodMenuAttachmentDownloadResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var response = await _httpClient.GetAsync(
                $"api/Attachment/file?scope={Scope}&entityId={monthId}&fileName={Uri.EscapeDataString(fileName)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return FoodMenuAttachmentDownloadResult.Fail(await ReadMessageAsync(response, cancellationToken));
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return FoodMenuAttachmentDownloadResult.Success(bytes, contentType);
        }

        public string GetFileUrl(int monthId, string fileName)
        {
            if (_httpClient.BaseAddress == null)
            {
                return "#";
            }

            return new Uri(_httpClient.BaseAddress, $"api/Attachment/file?scope={Scope}&entityId={monthId}&fileName={Uri.EscapeDataString(fileName)}").ToString();
        }

        private static async Task<string> ReadMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    using var document = JsonDocument.Parse(responseContent);
                    if (document.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        return messageElement.GetString() ?? "Yemek menüsü PDF işlemi başarısız.";
                    }
                }
                catch (JsonException)
                {
                    return responseContent;
                }
            }

            return "Yemek menüsü PDF işlemi başarısız.";
        }
    }

    public sealed record FoodMenuAttachmentUploadResult(bool IsSuccess, string Message, string FileName, string RelativePath, string ContentType)
    {
        public static FoodMenuAttachmentUploadResult Success(string fileName, string relativePath, string contentType)
        {
            return new FoodMenuAttachmentUploadResult(true, string.Empty, fileName, relativePath, contentType);
        }

        public static FoodMenuAttachmentUploadResult Fail(string message)
        {
            return new FoodMenuAttachmentUploadResult(false, message, string.Empty, string.Empty, string.Empty);
        }
    }

    public sealed record FoodMenuAttachmentDeleteResult(bool IsSuccess, string Message)
    {
        public static FoodMenuAttachmentDeleteResult Success()
        {
            return new FoodMenuAttachmentDeleteResult(true, string.Empty);
        }

        public static FoodMenuAttachmentDeleteResult Fail(string message)
        {
            return new FoodMenuAttachmentDeleteResult(false, message);
        }
    }

    public sealed record FoodMenuAttachmentDownloadResult(bool IsSuccess, string Message, byte[] Content, string ContentType)
    {
        public static FoodMenuAttachmentDownloadResult Success(byte[] content, string contentType)
        {
            return new FoodMenuAttachmentDownloadResult(true, string.Empty, content, contentType);
        }

        public static FoodMenuAttachmentDownloadResult Fail(string message)
        {
            return new FoodMenuAttachmentDownloadResult(false, message, Array.Empty<byte>(), string.Empty);
        }
    }
}
