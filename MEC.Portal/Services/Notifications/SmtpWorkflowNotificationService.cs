using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using MEC.Application.Abstractions.Service.LoggingService;
using MEC.Application.Abstractions.Service.LoggingService.Model;
using MEC.Application.Abstractions.Service.NotificationService;
using MEC.Application.Abstractions.Service.NotificationService.Model;
using MEC.Portal.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MEC.Portal.Services.Notifications
{
    public class SmtpWorkflowNotificationService : IWorkflowNotificationService
    {
        private const string EmailTemplateFolderName = "EmailTemplates";

        private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

        private readonly SmtpOptions _smtpOptions;
        private readonly NotificationOptions _notificationOptions;
        private readonly PortalUrlOptions _portalUrlOptions;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IUserActionLogService _userActionLogService;

        public SmtpWorkflowNotificationService(
            IOptions<SmtpOptions> smtpOptions,
            IOptions<NotificationOptions> notificationOptions,
            IOptions<PortalUrlOptions> portalUrlOptions,
            IHostEnvironment hostEnvironment,
            IUserActionLogService userActionLogService)
        {
            _smtpOptions = smtpOptions.Value;
            _notificationOptions = notificationOptions.Value;
            _portalUrlOptions = portalUrlOptions.Value;
            _hostEnvironment = hostEnvironment;
            _userActionLogService = userActionLogService;
        }

        public async Task NotifyLeaveRequestCreatedAsync(LeaveRequestCreatedNotificationModel model)
        {
            var userTokens = CreateLeaveTokens(model, BuildAbsoluteUrl($"/Leave/History/{model.LeaveId}"));
            await SendTemplateEmailAsync(
                model.EmployeeEmail,
                "MEC Portal - İzin Talebiniz Oluşturuldu",
                "leave-request-created-user.html",
                userTokens,
                model,
                "SendLeaveNotification");

            var approverTokens = CreateLeaveTokens(model, BuildAbsoluteUrl($"/Admin/LeaveRequests/{model.LeaveId}"));
            await SendTemplateEmailsAsync(
                GetRecipientsOrFallback(model.Approvers),
                "MEC Portal - Yeni İzin Talebi",
                "leave-request-created-approver.html",
                approverTokens,
                model,
                "SendLeaveNotification");
        }

        public async Task NotifyLeaveRequestCancelledAsync(LeaveRequestCancelledNotificationModel model)
        {
            var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["EmployeeName"] = model.EmployeeName,
                ["StartDate"] = FormatDateTime(model.StartDate),
                ["EndDate"] = FormatDateTime(model.EndDate),
                ["DateRange"] = FormatDateRange(model.StartDate, model.EndDate),
                ["ReasonHtml"] = FormatMultilineHtml(model.Reason),
                ["CancelledBy"] = model.CancelledBy
            };

            await SendTemplateEmailsAsync(
                GetRecipientsOrFallback(model.Approvers),
                "MEC Portal - İzin Talebi İptal Edildi",
                "leave-request-cancelled-approver.html",
                tokens,
                model,
                "SendLeaveNotification");
        }

        public async Task NotifyLeaveRequestDecisionAsync(LeaveRequestDecisionNotificationModel model)
        {
            var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["EmployeeName"] = model.EmployeeName,
                ["StartDate"] = FormatDateTime(model.StartDate),
                ["EndDate"] = FormatDateTime(model.EndDate),
                ["DateRange"] = FormatDateRange(model.StartDate, model.EndDate),
                ["ReasonHtml"] = FormatMultilineHtml(model.Reason),
                ["DecisionBy"] = model.DecisionBy,
                ["DecisionLabel"] = model.DecisionLabel,
                ["DecisionAction"] = ToDecisionAction(model.DecisionLabel),
                ["DecisionAuthority"] = model.IsManagerDecision ? "Okul müdürü" : "Genel Müdürlük",
                ["LocationNames"] = model.LocationNames,
                ["RequestUrl"] = BuildAbsoluteUrl($"/Leave/History/{model.LeaveId}")
            };

            if (model.IsManagerDecision && string.Equals(model.DecisionLabel, "Onaylandı", StringComparison.Ordinal))
            {
                await SendTemplateEmailAsync(
                    model.EmployeeEmail,
                    "MEC Portal - İzin Talebiniz Okul Müdürü Tarafından Onaylandı",
                    "leave-request-manager-approved-user.html",
                    tokens,
                    model,
                    "SendLeaveNotification");

                var finalApproverTokens = new Dictionary<string, string>(tokens, StringComparer.Ordinal)
                {
                    ["RequestUrl"] = BuildAbsoluteUrl($"/Admin/LeaveRequests/{model.LeaveId}")
                };

                await SendTemplateEmailsAsync(
                    model.NextApprovers,
                    "MEC Portal - Nihai Onay Bekleyen İzin Talebi",
                    "leave-request-manager-approved-final-approver.html",
                    finalApproverTokens,
                    model,
                    "SendLeaveNotification");
                return;
            }

            await SendTemplateEmailAsync(
                model.EmployeeEmail,
                $"MEC Portal - İzin Talebiniz {model.DecisionLabel}",
                "leave-request-decision-user.html",
                tokens,
                model,
                "SendLeaveNotification");

            if (!model.IsManagerDecision)
            {
                var managerTokens = new Dictionary<string, string>(tokens, StringComparer.Ordinal)
                {
                    ["RequestUrl"] = BuildAbsoluteUrl($"/Admin/LeaveRequests/{model.LeaveId}")
                };

                await SendTemplateEmailsAsync(
                    model.RegionalManagers,
                    $"MEC Portal - İzin Talebi {model.DecisionLabel}",
                    "leave-request-final-decision-manager.html",
                    managerTokens,
                    model,
                    "SendLeaveNotification");
            }
        }

        public async Task NotifyOvertimeRequestCreatedAsync(OvertimeRequestCreatedNotificationModel model)
        {
            var userTokens = CreateOvertimeTokens(model, BuildAbsoluteUrl($"/Overtime/History/{model.OvertimeRequestId}"));
            await SendTemplateEmailAsync(
                model.EmployeeEmail,
                "MEC Portal - Mesai Talebiniz Oluşturuldu",
                "overtime-request-created-user.html",
                userTokens,
                model,
                "SendOvertimeNotification");

            var approverTokens = CreateOvertimeTokens(model, BuildAbsoluteUrl($"/Admin/OvertimeRequests/{model.OvertimeRequestId}"));
            await SendTemplateEmailsAsync(
                GetRecipientsOrFallback(model.Approvers),
                "MEC Portal - Yeni Mesai Talebi",
                "overtime-request-created-approver.html",
                approverTokens,
                model,
                "SendOvertimeNotification");
        }

        public async Task NotifyOvertimeRequestCancelledAsync(OvertimeRequestCancelledNotificationModel model)
        {
            var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["EmployeeName"] = model.EmployeeName,
                ["StartDate"] = FormatDateTime(model.StartDate),
                ["EndDate"] = FormatDateTime(model.EndDate),
                ["DateRange"] = FormatDateRange(model.StartDate, model.EndDate),
                ["RequestedHours"] = FormatHours(model.RequestedHours),
                ["ReasonHtml"] = FormatMultilineHtml(model.Reason),
                ["CancelledBy"] = model.CancelledBy
            };

            await SendTemplateEmailsAsync(
                GetRecipientsOrFallback(model.Approvers),
                "MEC Portal - Mesai Talebi İptal Edildi",
                "overtime-request-cancelled-approver.html",
                tokens,
                model,
                "SendOvertimeNotification");
        }

        public async Task NotifyOvertimeRequestDecisionAsync(OvertimeRequestDecisionNotificationModel model)
        {
            var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["EmployeeName"] = model.EmployeeName,
                ["StartDate"] = FormatDateTime(model.StartDate),
                ["EndDate"] = FormatDateTime(model.EndDate),
                ["DateRange"] = FormatDateRange(model.StartDate, model.EndDate),
                ["RequestedHours"] = FormatHours(model.RequestedHours),
                ["ReasonHtml"] = FormatMultilineHtml(model.Reason),
                ["DecisionBy"] = model.DecisionBy,
                ["DecisionLabel"] = model.DecisionLabel,
                ["DecisionAction"] = ToDecisionAction(model.DecisionLabel),
                ["DecisionAuthority"] = model.IsManagerDecision ? "Okul müdürü" : "Genel Müdürlük",
                ["LocationNames"] = model.LocationNames,
                ["RequestUrl"] = BuildAbsoluteUrl($"/Overtime/History/{model.OvertimeRequestId}")
            };

            if (model.IsManagerDecision && string.Equals(model.DecisionLabel, "Onaylandı", StringComparison.Ordinal))
            {
                await SendTemplateEmailAsync(
                    model.EmployeeEmail,
                    "MEC Portal - Mesai Talebiniz Okul Müdürü Tarafından Onaylandı",
                    "overtime-request-manager-approved-user.html",
                    tokens,
                    model,
                    "SendOvertimeNotification");

                var finalApproverTokens = new Dictionary<string, string>(tokens, StringComparer.Ordinal)
                {
                    ["RequestUrl"] = BuildAbsoluteUrl($"/Admin/OvertimeRequests/{model.OvertimeRequestId}")
                };

                await SendTemplateEmailsAsync(
                    model.NextApprovers,
                    "MEC Portal - Nihai Onay Bekleyen Mesai Talebi",
                    "overtime-request-manager-approved-final-approver.html",
                    finalApproverTokens,
                    model,
                    "SendOvertimeNotification");
                return;
            }

            await SendTemplateEmailAsync(
                model.EmployeeEmail,
                $"MEC Portal - Mesai Talebiniz {model.DecisionLabel}",
                "overtime-request-decision-user.html",
                tokens,
                model,
                "SendOvertimeNotification");

            if (!model.IsManagerDecision)
            {
                var managerTokens = new Dictionary<string, string>(tokens, StringComparer.Ordinal)
                {
                    ["RequestUrl"] = BuildAbsoluteUrl($"/Admin/OvertimeRequests/{model.OvertimeRequestId}")
                };

                await SendTemplateEmailsAsync(
                    model.RegionalManagers,
                    $"MEC Portal - Mesai Talebi {model.DecisionLabel}",
                    "overtime-request-final-decision-manager.html",
                    managerTokens,
                    model,
                    "SendOvertimeNotification");
            }
        }

        private async Task SendTemplateEmailsAsync(
            IEnumerable<WorkflowNotificationRecipientModel> recipients,
            string subject,
            string templateFileName,
            IReadOnlyDictionary<string, string> tokens,
            WorkflowNotificationContextModel context,
            string methodName)
        {
            foreach (var recipient in recipients
                .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                .GroupBy(x => x.Email.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First()))
            {
                var recipientTokens = new Dictionary<string, string>(tokens, StringComparer.Ordinal)
                {
                    ["RecipientName"] = string.IsNullOrWhiteSpace(recipient.DisplayName)
                        ? recipient.Email
                        : recipient.DisplayName
                };

                await SendTemplateEmailAsync(
                    recipient.Email,
                    subject,
                    templateFileName,
                    recipientTokens,
                    context,
                    methodName);
            }
        }

        private async Task SendTemplateEmailAsync(
            string? toEmail,
            string subject,
            string templateFileName,
            IReadOnlyDictionary<string, string> tokens,
            WorkflowNotificationContextModel context,
            string methodName)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                await LogNotificationWarningAsync(
                    context,
                    methodName,
                    $"Bildirim gönderilemedi. Alıcı e-posta boş. Şablon: {templateFileName}.");
                return;
            }

            if (!IsSmtpConfigured())
            {
                await LogNotificationWarningAsync(
                    context,
                    methodName,
                    $"Bildirim gönderilemedi. SMTP ayarları eksik. Şablon: {templateFileName}, Alıcı: {toEmail}.");
                return;
            }

            var template = await LoadTemplateAsync(templateFileName, context, methodName);
            if (template == null)
            {
                return;
            }

            var body = ReplaceTemplateTokens(template, tokens);

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_smtpOptions.FromAddress, _smtpOptions.DisplayName, Encoding.UTF8),
                    Subject = subject,
                    SubjectEncoding = Encoding.UTF8,
                    Body = body,
                    BodyEncoding = Encoding.UTF8,
                    IsBodyHtml = true
                };

                message.To.Add(new MailAddress(toEmail));

                using var smtpClient = CreateSmtpClient();
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                await LogNotificationErrorAsync(
                    context,
                    methodName,
                    $"Bildirim gönderilemedi. Şablon: {templateFileName}, Alıcı: {toEmail}, Hata: {ex.Message}");
            }
        }

        private async Task<string?> LoadTemplateAsync(string templateFileName, WorkflowNotificationContextModel context, string methodName)
        {
            try
            {
                var templatePath = Path.Combine(_hostEnvironment.ContentRootPath, EmailTemplateFolderName, templateFileName);
                if (!File.Exists(templatePath))
                {
                    await LogNotificationWarningAsync(
                        context,
                        methodName,
                        $"Bildirim şablonu bulunamadı. Dosya: {templateFileName}.");
                    return null;
                }

                return await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                await LogNotificationErrorAsync(
                    context,
                    methodName,
                    $"Bildirim şablonu okunamadı. Dosya: {templateFileName}, Hata: {ex.Message}");
                return null;
            }
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                EnableSsl = _smtpOptions.UseStartTls,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_smtpOptions.Username, _smtpOptions.Password)
            };
        }

        private bool IsSmtpConfigured()
        {
            return !string.IsNullOrWhiteSpace(_smtpOptions.Host)
                && _smtpOptions.Port > 0
                && !string.IsNullOrWhiteSpace(_smtpOptions.FromAddress)
                && !string.IsNullOrWhiteSpace(_smtpOptions.Username)
                && !string.IsNullOrWhiteSpace(_smtpOptions.Password);
        }

        private string BuildAbsoluteUrl(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            var trimmedBaseUrl = (_portalUrlOptions.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(trimmedBaseUrl))
            {
                return relativePath.StartsWith("/", StringComparison.Ordinal) ? relativePath : "/" + relativePath;
            }

            var normalizedRelativePath = relativePath.StartsWith("/", StringComparison.Ordinal)
                ? relativePath
                : "/" + relativePath;

            return trimmedBaseUrl + normalizedRelativePath;
        }

        private static string ReplaceTemplateTokens(string template, IReadOnlyDictionary<string, string> tokens)
        {
            var renderedTemplate = template;

            foreach (var token in tokens)
            {
                var replacement = token.Key.EndsWith("Html", StringComparison.Ordinal)
                    ? token.Value ?? string.Empty
                    : HtmlEncoder.Default.Encode(token.Value ?? string.Empty);

                renderedTemplate = renderedTemplate.Replace("{{" + token.Key + "}}", replacement, StringComparison.Ordinal);
            }

            return renderedTemplate;
        }

        private static Dictionary<string, string> CreateLeaveTokens(LeaveRequestCreatedNotificationModel model, string requestUrl)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["EmployeeName"] = model.EmployeeName,
                ["StartDate"] = FormatDateTime(model.StartDate),
                ["EndDate"] = FormatDateTime(model.EndDate),
                ["DateRange"] = FormatDateRange(model.StartDate, model.EndDate),
                ["ReasonHtml"] = FormatMultilineHtml(model.Reason),
                ["LocationNames"] = model.LocationNames,
                ["ApprovalTarget"] = model.ApprovalTarget,
                ["RequestUrl"] = requestUrl
            };
        }

        private static Dictionary<string, string> CreateOvertimeTokens(OvertimeRequestCreatedNotificationModel model, string requestUrl)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["EmployeeName"] = model.EmployeeName,
                ["StartDate"] = FormatDateTime(model.StartDate),
                ["EndDate"] = FormatDateTime(model.EndDate),
                ["DateRange"] = FormatDateRange(model.StartDate, model.EndDate),
                ["RequestedHours"] = FormatHours(model.RequestedHours),
                ["ReasonHtml"] = FormatMultilineHtml(model.Reason),
                ["LocationNames"] = model.LocationNames,
                ["ApprovalTarget"] = model.ApprovalTarget,
                ["RequestUrl"] = requestUrl
            };
        }

        private IEnumerable<WorkflowNotificationRecipientModel> GetRecipientsOrFallback(
            IReadOnlyCollection<WorkflowNotificationRecipientModel> recipients)
        {
            if (recipients.Count > 0)
            {
                return recipients;
            }

            return string.IsNullOrWhiteSpace(_notificationOptions.ApproverFallbackEmail)
                ? Array.Empty<WorkflowNotificationRecipientModel>()
                : new[]
                {
                    new WorkflowNotificationRecipientModel
                    {
                        Email = _notificationOptions.ApproverFallbackEmail,
                        DisplayName = "Onay Yetkilisi"
                    }
                };
        }

        private static string FormatDateTime(DateTime value)
        {
            return value.ToString("dd.MM.yyyy HH:mm", TrCulture);
        }

        private static string FormatDateRange(DateTime startDate, DateTime endDate)
        {
            return $"{FormatDateTime(startDate)} - {FormatDateTime(endDate)}";
        }

        private static string FormatHours(decimal value)
        {
            return value.ToString("0.##", TrCulture);
        }

        private static string FormatMultilineHtml(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            return HtmlEncoder.Default.Encode(value.Trim()).Replace(Environment.NewLine, "<br />", StringComparison.Ordinal).Replace("\n", "<br />", StringComparison.Ordinal);
        }

        private static string ToDecisionAction(string decisionLabel)
        {
            return decisionLabel switch
            {
                "Onaylandı" => "onayladı",
                "Reddedildi" => "reddetti",
                "İptal" => "iptal etti",
                _ => "güncelledi"
            };
        }

        private Task LogNotificationWarningAsync(WorkflowNotificationContextModel context, string methodName, string message)
        {
            return LogNotificationAsync(context, methodName, "Warning", message);
        }

        private Task LogNotificationErrorAsync(WorkflowNotificationContextModel context, string methodName, string message)
        {
            return LogNotificationAsync(context, methodName, "Error", message);
        }

        private async Task LogNotificationAsync(WorkflowNotificationContextModel context, string methodName, string level, string message)
        {
            try
            {
                await _userActionLogService.LogAsync(new UserActionLogEntryModel
                {
                    IpAddress = string.IsNullOrWhiteSpace(context.IpAddress) ? "unknown" : context.IpAddress,
                    MacAddress = null,
                    User = string.IsNullOrWhiteSpace(context.TriggeredByUser) ? "anonymous" : context.TriggeredByUser,
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    MethodName = methodName,
                    Message = message
                });
            }
            catch
            {
                // Notification logging must never break the underlying business workflow.
            }
        }
    }
}
