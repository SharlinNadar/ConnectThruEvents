using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;
using System.Collections.Generic;

namespace ConnectThruEventsBackend.Services
{
    public class EventStatusUpdaterService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventStatusUpdaterService> _logger;

        public EventStatusUpdaterService(IServiceProvider serviceProvider, ILogger<EventStatusUpdaterService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logger.LogInformation("EventStatusUpdaterService initialized.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EventStatusUpdaterService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Running event status update...");
                await UpdateEventStatuses();

                _logger.LogInformation("Event status update completed. Waiting for the next cycle...");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }

            _logger.LogInformation("EventStatusUpdaterService stopped.");
        }

        public async Task UpdateEventStatuses()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    var currentDate = DateTime.UtcNow;
                    _logger.LogInformation("Current UTC date and time: {CurrentDate}", currentDate);

                    var pastEvents = await dbContext.EventDetails
                        .Where(e => e.Status.Trim().ToLower() == "upcoming" && e.Date < currentDate)
                        .ToListAsync();

                    if (pastEvents.Any())
                    {
                        foreach (var eventDetail in pastEvents)
                        {
                            eventDetail.Status = "Completed";
                            dbContext.Entry(eventDetail).State = EntityState.Modified;
                        }

                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Updated {Count} events to Completed.", pastEvents.Count);
                    }
                    else
                    {
                        _logger.LogInformation("No upcoming events found to update to Completed.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while updating event statuses.");
                }
            }
        }

        public List<EventDetail> GetEventsByStatus(string status)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    _logger.LogInformation("Fetching events with status: {Status}", status);
                    var events = dbContext.EventDetails
                        .AsNoTracking()
                        .Where(e => e.Status.Trim().ToLower() == status.Trim().ToLower())
                        .ToList();
                    _logger.LogInformation("Found {Count} events with status {Status}.", events.Count, status);
                    return events;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while fetching events by status.");
                    return new List<EventDetail>();
                }
            }
        }

        public List<EventDetail> GetEventsByStatusAndManager(string status, Guid managerId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    _logger.LogInformation("Fetching events with status: {Status} and manager ID: {ManagerId}", status, managerId);
                    var events = dbContext.EventDetails
                        .AsNoTracking()
                        .Where(e => e.Status.Trim().ToLower() == status.Trim().ToLower() && e.EventManagerId == managerId)
                        .ToList();
                    _logger.LogInformation("Found {Count} events with status {Status} for manager ID {ManagerId}.", events.Count, status, managerId);
                    return events;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while fetching events by status and manager ID.");
                    return new List<EventDetail>();
                }
            }
        }
    }
}
