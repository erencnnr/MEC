using System.Net.Http.Headers;
using System.Text.Json;

namespace MEC.Portal.Services
{
    public interface ILibraryAttachmentApiClient
    {
        Task<LibraryAttachmentUploadResult> UploadAsync(int documentId, IFormFile file, CancellationToken cancellationToken = default);
        Task<LibraryAttachmentDeleteResult> DeleteAsync(int documentId, string fileName, CancellationToken cancellationToken = default);
        string GetFileUrl(int documentId, string fileName);
        Task<LibraryAttachmentDownloadResult> DownloadAsync(int documentId, string fileName, CancellationToken cancellationToken = default);
    }

    public class LibraryAttachmentApiClient : ILibraryAttachmentApiClient
    {
        private const string Scope = "library-documents";
        private readonly HttpClient _httpClient;

        public LibraryAttachmentApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<LibraryAttachmentUploadResult> UploadAsync(int documentId, IFormFile file, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return LibraryAttachmentUploadResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var formData = new MultipartFormDataContent();
            await using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType);

            formData.Add(fileContent, "file", file.FileName);
            formData.Add(new StringContent(Scope), "scope");
            formData.Add(new StringContent(documentId.ToString()), "entityId");

            using var response = await _httpClient.PostAsync("api/Attachment/upload", formData, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(payload))
                {
                    using var document = JsonDocument.Parse(payload);
                    var root = document.RootElement;

                    return LibraryAttachmentUploadResult.Success(
                        root.TryGetProperty("fileName", out var fileNameElement) ? fileNameElement.GetString() ?? string.Empty : string.Empty,
                        root.TryGetProperty("relativePath", out var relativePathElement) ? relativePathElement.GetString() ?? string.Empty : string.Empty,
                        string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
                }

                return LibraryAttachmentUploadResult.Fail("Doküman yükleme yanıtı eksik döndü.");
            }

            return LibraryAttachmentUploadResult.Fail(await ReadMessageAsync(response, cancellationToken));
        }

        public async Task<LibraryAttachmentDeleteResult> DeleteAsync(int documentId, string fileName, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return LibraryAttachmentDeleteResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            var url = $"api/Attachment/delete?scope={Scope}&entityId={documentId}&fileName={Uri.EscapeDataString(fileName)}";
            using var response = await _httpClient.DeleteAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return LibraryAttachmentDeleteResult.Success();
            }

            return LibraryAttachmentDeleteResult.Fail(await ReadMessageAsync(response, cancellationToken));
        }

        public string GetFileUrl(int documentId, string fileName)
        {
            if (_httpClient.BaseAddress == null)
            {
                return "#";
            }

            return new Uri(_httpClient.BaseAddress, $"api/Attachment/file?scope={Scope}&entityId={documentId}&fileName={Uri.EscapeDataString(fileName)}").ToString();
        }

        public async Task<LibraryAttachmentDownloadResult> DownloadAsync(int documentId, string fileName, CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return LibraryAttachmentDownloadResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var response = await _httpClient.GetAsync(
                $"api/Attachment/file?scope={Scope}&entityId={documentId}&fileName={Uri.EscapeDataString(fileName)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return LibraryAttachmentDownloadResult.Fail(await ReadMessageAsync(response, cancellationToken));
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return LibraryAttachmentDownloadResult.Success(bytes, contentType);
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
                        return messageElement.GetString() ?? "Doküman işlemi başarısız.";
                    }
                }
                catch (JsonException)
                {
                    return responseContent;
                }
            }

            return "Doküman işlemi başarısız.";
        }
    }

    public sealed record LibraryAttachmentUploadResult(
        bool IsSuccess,
        string Message,
        string FileName,
        string RelativePath,
        string ContentType)
    {
        public static LibraryAttachmentUploadResult Success(string fileName, string relativePath, string contentType)
        {
            return new LibraryAttachmentUploadResult(true, string.Empty, fileName, relativePath, contentType);
        }

        public static LibraryAttachmentUploadResult Fail(string message)
        {
            return new LibraryAttachmentUploadResult(false, message, string.Empty, string.Empty, string.Empty);
        }
    }

    public sealed record LibraryAttachmentDeleteResult(bool IsSuccess, string Message)
    {
        public static LibraryAttachmentDeleteResult Success()
        {
            return new LibraryAttachmentDeleteResult(true, string.Empty);
        }

        public static LibraryAttachmentDeleteResult Fail(string message)
        {
            return new LibraryAttachmentDeleteResult(false, message);
        }
    }

    public sealed record LibraryAttachmentDownloadResult(bool IsSuccess, string Message, byte[] Content, string ContentType)
    {
        public static LibraryAttachmentDownloadResult Success(byte[] content, string contentType)
        {
            return new LibraryAttachmentDownloadResult(true, string.Empty, content, contentType);
        }

        public static LibraryAttachmentDownloadResult Fail(string message)
        {
            return new LibraryAttachmentDownloadResult(false, message, Array.Empty<byte>(), string.Empty);
        }
    }
}
