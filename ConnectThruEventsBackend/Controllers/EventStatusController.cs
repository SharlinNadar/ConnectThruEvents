using Microsoft.AspNetCore.Mvc;
using ConnectThruEventsBackend.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectThruEventsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventStatusController : ControllerBase
    {
        private readonly EventStatusUpdaterService _eventStatusUpdaterService;
        private readonly ILogger<EventStatusController> _logger;

        public EventStatusController(EventStatusUpdaterService eventStatusUpdaterService, ILogger<EventStatusController> logger)
        {
            _eventStatusUpdaterService = eventStatusUpdaterService ?? throw new ArgumentNullException(nameof(eventStatusUpdaterService));
            _logger = logger;
            _logger.LogInformation("EventStatusController initialized.");
        }

        // ✅ Endpoint to Update All Event Statuses
        [HttpPost("update-statuses")]
        public async Task<IActionResult> UpdateEventStatuses()
        {
            try
            {
                await _eventStatusUpdaterService.UpdateEventStatuses();
                _logger.LogInformation("Event statuses updated successfully.");
                return Ok(new { message = "Event statuses updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating event statuses.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ✅ Endpoint to Get Events by Status
        [HttpGet("by-status/{status}")]
        public IActionResult GetEventsByStatus(string status)
        {
            try
            {
                var events = _eventStatusUpdaterService.GetEventsByStatus(status);
                if (events == null || !events.Any())
                {
                    _logger.LogWarning("No events found with status: {Status}.", status);
                    return NotFound(new { message = $"No events found with status: {status}." });
                }
                _logger.LogInformation("Retrieved {Count} events with status {Status}.", events.Count, status);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting events by status.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ✅ Endpoint to Get Events by Status and Event Manager ID
        [HttpGet("by-status/{status}/manager/{managerId:guid}")]
        public IActionResult GetEventsByStatusAndManager(string status, Guid managerId)
        {
            try
            {
                var events = _eventStatusUpdaterService.GetEventsByStatusAndManager(status, managerId);
                if (events == null || !events.Any())
                {
                    _logger.LogWarning("No events found with status: {Status} for manager: {ManagerId}.", status, managerId);
                    return NotFound(new { message = $"No events found with status: {status} for manager: {managerId}." });
                }
                _logger.LogInformation("Retrieved {Count} events with status {Status} for manager {ManagerId}.", events.Count, status, managerId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting events by status and manager ID.");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
