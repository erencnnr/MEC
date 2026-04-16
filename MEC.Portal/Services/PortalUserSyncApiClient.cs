using System.Text.Json;

namespace MEC.Portal.Services
{
    public interface IPortalUserSyncApiClient
    {
        Task<PortalUserSyncResult> SyncPortalUsersAsync(CancellationToken cancellationToken = default);
    }

    public class PortalUserSyncApiClient : IPortalUserSyncApiClient
    {
        private readonly HttpClient _httpClient;

        public PortalUserSyncApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PortalUserSyncResult> SyncPortalUsersAsync(CancellationToken cancellationToken = default)
        {
            if (_httpClient.BaseAddress == null)
            {
                return PortalUserSyncResult.Fail("WebAPI adresi yapılandırılmamış.");
            }

            using var response = await _httpClient.GetAsync("api/Ldap/SyncPortalUsers", cancellationToken);
            var payload = await ReadPayloadAsync(response, cancellationToken);

            return response.IsSuccessStatusCode
                ? PortalUserSyncResult.Success(payload.ProcessedUsers, payload.Message)
                : PortalUserSyncResult.Fail(payload.Message);
        }

        private static async Task<PortalUserSyncPayload> ReadPayloadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return new PortalUserSyncPayload(
                    response.IsSuccessStatusCode
                        ? "Kullanıcılar başarıyla senkronize edildi."
                        : "Hata! Senkronizasyon başarısız.",
                    0);
            }

            try
            {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;
                var message = string.Empty;
                var processedUsers = 0;

                if (root.TryGetProperty("message", out var messageElement))
                {
                    message = messageElement.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("Message", out var pascalMessageElement))
                {
                    message = pascalMessageElement.GetString() ?? message;
                }

                if (root.TryGetProperty("error", out var errorElement))
                {
                    message = errorElement.GetString() ?? message;
                }

                if (root.TryGetProperty("Error", out var pascalErrorElement))
                {
                    message = pascalErrorElement.GetString() ?? message;
                }

                if (root.TryGetProperty("processedUsers", out var processedUsersElement) &&
                    processedUsersElement.TryGetInt32(out var camelProcessedUsers))
                {
                    processedUsers = camelProcessedUsers;
                }

                if (root.TryGetProperty("ProcessedUsers", out var pascalProcessedUsersElement) &&
                    pascalProcessedUsersElement.TryGetInt32(out var pascalProcessedUsers))
                {
                    processedUsers = pascalProcessedUsers;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    message = response.IsSuccessStatusCode
                        ? "Kullanıcılar başarıyla senkronize edildi."
                        : "Hata! Senkronizasyon başarısız.";
                }

                return new PortalUserSyncPayload(message, processedUsers);
            }
            catch (JsonException)
            {
                return new PortalUserSyncPayload(responseContent, 0);
            }
        }
    }

    public sealed record PortalUserSyncPayload(string Message, int ProcessedUsers);

    public sealed record PortalUserSyncResult(bool IsSuccess, string Message, int ProcessedUsers)
    {
        public static PortalUserSyncResult Success(int processedUsers, string message)
        {
            return new PortalUserSyncResult(true, message, processedUsers);
        }

        public static PortalUserSyncResult Fail(string message)
        {
            return new PortalUserSyncResult(false, message, 0);
        }
    }
}
