using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Event.Contracts.IServices;

namespace Event.Business.Services
{
    public class PayoutBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PayoutBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public PayoutBackgroundService(IServiceProvider serviceProvider, ILogger<PayoutBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        #region Execute Background Daemon

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Organizer Payout Release Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking for completed events with dismissed reports to release payouts...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                        await eventService.ProcessDismissedPayoutsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while running the organizer payout release background job.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Organizer Payout Release Background Service stopped.");
        }

        #endregion
    }
}
