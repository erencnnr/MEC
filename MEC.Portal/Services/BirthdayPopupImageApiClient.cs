using System.Net.Http.Headers;
using System.Text.Json;

namespace MEC.Portal.Services
{
    public interface IBirthdayPopupImageApiClient
    {
        Task<BirthdayPopupImageUploadResult> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
        Task<BirthdayPopupImageDeleteResult> DeleteAsync(string fileName, CancellationToken cancellationToken = default);
        string GetFileUrl(string fileName);
    }

    public class BirthdayPopupImageApiClient : IBirthdayPopupImageApiClient
    {
        private const string Scope = "birthday-popup";
        private const int EntityId = 1;
        private readonly HttpClient _httpClient;

        public BirthdayPopupImageApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<BirthdayPopupImageUploadResult> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return BirthdayPopupImageUploadResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var formData = new MultipartFormDataContent();
            await using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

            formData.Add(fileContent, "file", file.FileName);
            formData.Add(new StringContent(Scope), "scope");
            formData.Add(new StringContent(EntityId.ToString()), "entityId");

            using var response = await _httpClient.PostAsync("api/Image/upload", formData, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(payload))
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

                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        return BirthdayPopupImageUploadResult.Success(fileName, relativePath, contentType);
                    }
                }

                return BirthdayPopupImageUploadResult.Fail("Doğum günü popup görseli yükleme yanıtı eksik döndü.");
            }

            var message = await ReadMessageAsync(response, cancellationToken);
            return BirthdayPopupImageUploadResult.Fail(message);
        }

        public async Task<BirthdayPopupImageDeleteResult> DeleteAsync(string fileName, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return BirthdayPopupImageDeleteResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            var url = $"api/Image/delete?scope={Scope}&entityId={EntityId}&fileName={Uri.EscapeDataString(fileName)}";
            using var response = await _httpClient.DeleteAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return BirthdayPopupImageDeleteResult.Success();
            }

            var message = await ReadMessageAsync(response, cancellationToken);
            return BirthdayPopupImageDeleteResult.Fail(message);
        }

        public string GetFileUrl(string fileName)
        {
            if (_httpClient.BaseAddress == null)
            {
                return "#";
            }

            return new Uri(_httpClient.BaseAddress, $"api/Image/file?scope={Scope}&entityId={EntityId}&fileName={Uri.EscapeDataString(fileName)}").ToString();
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
                        return messageElement.GetString() ?? "Doğum günü popup görseli işlemi başarısız.";
                    }
                }
                catch (JsonException)
                {
                    return responseContent;
                }
            }

            return "Doğum günü popup görseli işlemi başarısız.";
        }
    }

    public sealed record BirthdayPopupImageUploadResult(
        bool IsSuccess,
        string Message,
        string FileName,
        string RelativePath,
        string ContentType)
    {
        public static BirthdayPopupImageUploadResult Success(string fileName, string relativePath, string contentType)
        {
            return new BirthdayPopupImageUploadResult(true, string.Empty, fileName, relativePath, contentType);
        }

        public static BirthdayPopupImageUploadResult Fail(string message)
        {
            return new BirthdayPopupImageUploadResult(false, message, string.Empty, string.Empty, string.Empty);
        }
    }

    public sealed record BirthdayPopupImageDeleteResult(bool IsSuccess, string Message)
    {
        public static BirthdayPopupImageDeleteResult Success()
        {
            return new BirthdayPopupImageDeleteResult(true, string.Empty);
        }

        public static BirthdayPopupImageDeleteResult Fail(string message)
        {
            return new BirthdayPopupImageDeleteResult(false, message);
        }
    }
}
