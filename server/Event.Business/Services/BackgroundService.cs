using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Event.Contracts.IServices;

namespace Event.Business.Services
{
    public class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public BackgroundService(IServiceProvider serviceProvider, ILogger<BackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        #region Execute Background Daemon

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Expired Resources Release Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Triggering release of expired bookings and events...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

                        await bookingService.ReleaseExpiredEventBookingAsync();
                        await eventService.ReleaseExpiredEventCreationAsync();
                        await eventService.ReleaseCompletedEventsAsync();
                    }

                    _logger.LogInformation("Completed release of expired bookings and events. Sleeping for {Interval} minutes.", _checkInterval.TotalMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while running the expired resources release background job.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Expired Resources Release Background Service stopped.");
        }

        #endregion
    }
}
