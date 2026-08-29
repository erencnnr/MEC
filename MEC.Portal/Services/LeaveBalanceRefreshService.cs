using MEC.Application.Abstractions.Service.LeaveService;

namespace MEC.Portal.Services
{
    public sealed class LeaveBalanceRefreshService : BackgroundService
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(12);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LeaveBalanceRefreshService> _logger;

        public LeaveBalanceRefreshService(
            IServiceScopeFactory scopeFactory,
            ILogger<LeaveBalanceRefreshService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RefreshAsync(stoppingToken);

            using var timer = new PeriodicTimer(RefreshInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RefreshAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal application shutdown.
            }
        }

        private async Task RefreshAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var leaveService = scope.ServiceProvider.GetRequiredService<ILeaveService>();
                await leaveService.RecalculateAllAnnualLeaveBalancesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yıllık izin hak ediş ve bakiye yenilemesi tamamlanamadı.");
            }
        }
    }
}
