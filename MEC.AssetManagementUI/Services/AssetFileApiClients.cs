using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MEC.AssetManagementUI.Services
{
    public interface IAssetImageApiClient
    {
        Task<AssetFileUploadResult> UploadAsync(int assetId, IFormFile file, CancellationToken cancellationToken = default);
        Task<AssetFileDeleteResult> DeleteAsync(int assetId, string fileName, CancellationToken cancellationToken = default);
        Task<AssetFileDownloadResult> DownloadAsync(int assetId, string fileName, CancellationToken cancellationToken = default);
        Task<AssetFileListResult> ListAsync(int assetId, CancellationToken cancellationToken = default);
    }

    public interface IAssetAttachmentApiClient
    {
        Task<AssetFileUploadResult> UploadAsync(int assetId, IFormFile file, CancellationToken cancellationToken = default);
        Task<AssetFileDeleteResult> DeleteAsync(int assetId, string fileName, CancellationToken cancellationToken = default);
        Task<AssetFileDownloadResult> DownloadAsync(int assetId, string fileName, CancellationToken cancellationToken = default);
        Task<AssetFileListResult> ListAsync(int assetId, CancellationToken cancellationToken = default);
    }

    public sealed class AssetImageApiClient : AssetFileApiClientBase, IAssetImageApiClient
    {
        public AssetImageApiClient(HttpClient httpClient)
            : base(httpClient, "api/Image", "assets")
        {
        }
    }

    public sealed class AssetAttachmentApiClient : AssetFileApiClientBase, IAssetAttachmentApiClient
    {
        public AssetAttachmentApiClient(HttpClient httpClient)
            : base(httpClient, "api/Attachment", "assets")
        {
        }
    }

    public abstract class AssetFileApiClientBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _controllerPath;
        private readonly string _scope;

        protected AssetFileApiClientBase(HttpClient httpClient, string controllerPath, string scope)
        {
            _httpClient = httpClient;
            _controllerPath = controllerPath;
            _scope = scope;
        }

        public async Task<AssetFileUploadResult> UploadAsync(int assetId, IFormFile file, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AssetFileUploadResult.Fail("WebAPI adresi yapilandirilmamis.");
            }

            if (file == null || file.Length == 0)
            {
                return AssetFileUploadResult.Fail("Yuklenecek dosya secilmedi.");
            }

            using var formData = new MultipartFormDataContent();
            await using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

            formData.Add(fileContent, "file", file.FileName);
            formData.Add(new StringContent(_scope), "scope");
            formData.Add(new StringContent(assetId.ToString()), "entityId");

            using var response = await _httpClient.PostAsync($"{_controllerPath}/upload", formData, cancellationToken);
            return await AssetFileApiResponseReader.ReadUploadResponseAsync(response, cancellationToken);
        }

        public async Task<AssetFileDeleteResult> DeleteAsync(int assetId, string fileName, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AssetFileDeleteResult.Fail("WebAPI adresi yapilandirilmamis.");
            }

            var endpoint = $"{_controllerPath}/delete?scope={_scope}&entityId={assetId}&fileName={Uri.EscapeDataString(fileName)}";
            using var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return AssetFileDeleteResult.Success();
            }

            return AssetFileDeleteResult.Fail(await AssetFileApiResponseReader.ReadMessageAsync(response, cancellationToken));
        }

        public async Task<AssetFileDownloadResult> DownloadAsync(int assetId, string fileName, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AssetFileDownloadResult.Fail("WebAPI adresi yapilandirilmamis.");
            }

            var endpoint = $"{_controllerPath}/file?scope={_scope}&entityId={assetId}&fileName={Uri.EscapeDataString(fileName)}";
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AssetFileDownloadResult.Fail(await AssetFileApiResponseReader.ReadMessageAsync(response, cancellationToken));
            }

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return AssetFileDownloadResult.Success(content, contentType, fileName);
        }

        public async Task<AssetFileListResult> ListAsync(int assetId, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return AssetFileListResult.Fail("WebAPI adresi yapilandirilmamis.");
            }

            var endpoint = $"{_controllerPath}/list?scope={_scope}&entityId={assetId}";
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AssetFileListResult.Fail(await AssetFileApiResponseReader.ReadMessageAsync(response, cancellationToken));
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return AssetFileListResult.Success(Array.Empty<string>());
            }

            try
            {
                using var document = JsonDocument.Parse(payload);
                if (document.RootElement.TryGetProperty("files", out var filesElement) &&
                    filesElement.ValueKind == JsonValueKind.Array)
                {
                    var files = filesElement
                        .EnumerateArray()
                        .Select(x => x.GetString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x!)
                        .ToArray();

                    return AssetFileListResult.Success(files);
                }
            }
            catch (JsonException)
            {
                return AssetFileListResult.Fail("Dosya listesi yaniti okunamadi.");
            }

            return AssetFileListResult.Success(Array.Empty<string>());
        }
    }

    public sealed record AssetFileUploadResult(
        bool IsSuccess,
        string Message,
        string FileName,
        string RelativePath,
        string ContentType)
    {
        public static AssetFileUploadResult Success(string fileName, string relativePath, string contentType)
        {
            return new AssetFileUploadResult(true, string.Empty, fileName, relativePath, contentType);
        }

        public static AssetFileUploadResult Fail(string message)
        {
            return new AssetFileUploadResult(false, message, string.Empty, string.Empty, string.Empty);
        }
    }

    public sealed record AssetFileDeleteResult(bool IsSuccess, string Message)
    {
        public static AssetFileDeleteResult Success()
        {
            return new AssetFileDeleteResult(true, string.Empty);
        }

        public static AssetFileDeleteResult Fail(string message)
        {
            return new AssetFileDeleteResult(false, message);
        }
    }

    public sealed record AssetFileDownloadResult(
        bool IsSuccess,
        string Message,
        byte[] Content,
        string ContentType,
        string FileName)
    {
        public static AssetFileDownloadResult Success(byte[] content, string contentType, string fileName)
        {
            return new AssetFileDownloadResult(true, string.Empty, content, contentType, fileName);
        }

        public static AssetFileDownloadResult Fail(string message)
        {
            return new AssetFileDownloadResult(false, message, Array.Empty<byte>(), string.Empty, string.Empty);
        }
    }

    public sealed record AssetFileListResult(bool IsSuccess, string Message, IReadOnlyCollection<string> Files)
    {
        public static AssetFileListResult Success(IReadOnlyCollection<string> files)
        {
            return new AssetFileListResult(true, string.Empty, files);
        }

        public static AssetFileListResult Fail(string message)
        {
            return new AssetFileListResult(false, message, Array.Empty<string>());
        }
    }

    internal static class AssetFileApiResponseReader
    {
        public static async Task<AssetFileUploadResult> ReadUploadResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                return AssetFileUploadResult.Fail(await ReadMessageAsync(response, cancellationToken));
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return AssetFileUploadResult.Fail("Dosya yukleme yaniti eksik dondu.");
            }

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

                return string.IsNullOrWhiteSpace(fileName)
                    ? AssetFileUploadResult.Fail("Dosya yukleme yaniti eksik dondu.")
                    : AssetFileUploadResult.Success(fileName, relativePath, contentType);
            }
            catch (JsonException)
            {
                return AssetFileUploadResult.Fail("Dosya yukleme yaniti okunamadi.");
            }
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
                        return messageElement.GetString() ?? "Dosya islemi basarisiz.";
                    }
                }
                catch (JsonException)
                {
                    return responseContent;
                }
            }

            return "Dosya islemi basarisiz.";
        }
    }
}
