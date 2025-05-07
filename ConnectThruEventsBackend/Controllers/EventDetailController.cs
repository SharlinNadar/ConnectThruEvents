using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System;

namespace ConnectThruEventsBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventDetailController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EventDetailController> _logger;

        public EventDetailController(AppDbContext context, ILogger<EventDetailController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private Guid GetEventManagerIdFromJwt()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                _logger.LogWarning("User identity is null.");
                return Guid.Empty;
            }

            var eventManagerIdClaim = identity.FindFirst("EventManagerId");
            if (eventManagerIdClaim == null)
            {
                _logger.LogWarning("Event Manager ID not found in JWT token.");
                return Guid.Empty;
            }

            if (!Guid.TryParse(eventManagerIdClaim.Value, out var eventManagerId))
            {
                _logger.LogWarning($"Invalid Event Manager ID format in JWT token: {eventManagerIdClaim.Value}");
                return Guid.Empty;
            }

            _logger.LogInformation($"Successfully retrieved Event Manager ID from JWT token: {eventManagerId}");
            return eventManagerId;
        }

        // Get all event details for the event manager
        [HttpGet("event-manager/event-details")]
        [Authorize]
        public async Task<ActionResult<object>> GetEventDetailsByEventManager()
        {
            try
            {
                var eventManagerId = GetEventManagerIdFromJwt();
                if (eventManagerId == Guid.Empty)
                {
                    return Unauthorized(new { status = "error", message = "Event Manager not found for the logged-in user" });
                }

                var eventDetails = await _context.EventDetails
                    .Where(e => e.EventManagerId == eventManagerId)
                    .ToListAsync();

                var totalCount = eventDetails.Count;
                var upcomingCount = eventDetails.Count(e => e.Status == "Upcoming");
                var completedCount = eventDetails.Count(e => e.Status == "Completed");

                return Ok(new { status = "success", message = "Event details retrieved successfully", eventDetails, totalCount, upcomingCount, completedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving event details for Event Manager.");
                return StatusCode(500, new { status = "error", message = "Failed to retrieve event details", error = ex.Message });
            }
        }

        // Mark an event as completed
        [HttpPut("event-manager/event-details/{id}/complete")]
        [Authorize]
        public async Task<ActionResult<object>> MarkEventAsCompleted(int id)
        {
            _logger.LogInformation($"Attempting to mark event with ID {id} as completed.");
            try
            {
                var eventDetail = await _context.EventDetails.FindAsync(id);
                if (eventDetail == null)
                {
                    _logger.LogWarning($"Event with ID {id} not found.");
                    return NotFound(new { status = "error", message = "Event detail not found" });
                }

                eventDetail.Status = "Completed";
                _context.EventDetails.Update(eventDetail);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully marked event with ID {id} as completed.");
                return Ok(new { status = "success", message = "Event marked as completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while marking event with ID {id} as completed.");
                return StatusCode(500, new { status = "error", message = "Failed to update event status", error = ex.Message });
            }
        }

        // Add a new event detail
        [HttpPost("event-manager/event-details")]
        [Authorize]
        public async Task<ActionResult<object>> AddEventDetail([FromBody] EventDetail newEventDetail)
        {
            if (newEventDetail == null || string.IsNullOrWhiteSpace(newEventDetail.Title))
            {
                return BadRequest(new { status = "error", message = "Invalid event detail data. Event title is required." });
            }

            try
            {
                var eventManagerId = GetEventManagerIdFromJwt();
                if (eventManagerId == Guid.Empty)
                {
                    return Unauthorized(new { status = "error", message = "Event Manager not found for the logged-in user" });
                }

                newEventDetail.EventManagerId = eventManagerId;
                newEventDetail.Status = "Upcoming";

                _context.EventDetails.Add(newEventDetail);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEventDetailsByEventManager), new { id = newEventDetail.EventDetailId }, new { status = "success", message = "Event detail added successfully", eventDetail = newEventDetail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding the event detail.");
                return StatusCode(500, new { status = "error", message = "An error occurred while adding the event detail", error = ex.Message });
            }
        }

        // Delete an event detail
        [HttpDelete("event-manager/event-details/{id}")]
[Authorize]
public async Task<ActionResult<object>> DeleteEventDetail(int id)
{
    try
    {
        var eventDetail = await _context.EventDetails
            .Include(e => e.EmployeeAssignments)
                .ThenInclude(emp => emp.TaskAssignments)
            .FirstOrDefaultAsync(e => e.EventDetailId == id);

        if (eventDetail == null)
        {
            return NotFound(new { status = "error", message = "Event detail not found" });
        }

        // Step 1: Delete TaskAssignments first
        foreach (var employeeAssignment in eventDetail.EmployeeAssignments)
        {
            _context.TaskAssignments.RemoveRange(employeeAssignment.TaskAssignments);
        }

        // Step 2: Delete EmployeeAssignments
        _context.EmployeeAssignments.RemoveRange(eventDetail.EmployeeAssignments);

        // Step 3: Delete EventDetail
        _context.EventDetails.Remove(eventDetail);

        await _context.SaveChangesAsync();

        return Ok(new { status = "success", message = "Event detail deleted successfully" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while deleting the event detail.");
        return StatusCode(500, new
        {
            status = "error",
            message = "An error occurred while deleting the event detail",
            error = ex.InnerException?.Message ?? ex.Message
        });
    }
}

        // Get completed events for the event manager
        [HttpGet("event-manager/completed-events")]
        [Authorize]
        public async Task<ActionResult<object>> GetCompletedEventsByEventManager()
        {
            try
            {
                var eventManagerId = GetEventManagerIdFromJwt();
                if (eventManagerId == Guid.Empty)
                {
                    return Unauthorized(new { status = "error", message = "Event Manager not found for the logged-in user" });
                }

                var completedEvents = await _context.EventDetails
                    .Where(e => e.EventManagerId == eventManagerId && e.Status == "Completed")
                    .ToListAsync();

                if (completedEvents == null || !completedEvents.Any())
                {
                    return NotFound(new { status = "error", message = "No completed events found" });
                }

                return Ok(new { status = "success", message = "Completed events retrieved successfully", completedEvents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving completed events for Event Manager.");
                return StatusCode(500, new { status = "error", message = "Failed to retrieve completed events", error = ex.Message });
            }
        }

        // Get count of upcoming events for the event manager
        [HttpGet("event-manager/upcoming-events-count")]
        [Authorize]
        public async Task<ActionResult<object>> GetUpcomingEventsCount()
        {
            try
            {
                var eventManagerId = GetEventManagerIdFromJwt();
                if (eventManagerId == Guid.Empty)
                {
                    return Unauthorized(new { status = "error", message = "Event Manager not found for the logged-in user" });
                }

                var upcomingEventsCount = await _context.EventDetails
                    .Where(e => e.EventManagerId == eventManagerId && e.Status == "Upcoming")
                    .CountAsync();

                return Ok(new { status = "success", message = "Upcoming events count retrieved successfully", count = upcomingEventsCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving upcoming events count.");
                return StatusCode(500, new { status = "error", message = "Failed to retrieve upcoming events count", error = ex.Message });
            }
        }

        // Get event details by ID, including Notes
        [HttpGet("event-manager/event-details/{id}")]
        [Authorize]
        public async Task<ActionResult<object>> GetEventDetailById(int id)
        {
            try
            {
                var eventManagerId = GetEventManagerIdFromJwt();
                if (eventManagerId == Guid.Empty)
                {
                    return Unauthorized(new { status = "error", message = "Event Manager not found for the logged-in user" });
                }

                // Fetch the event detail by ID and ensure the event belongs to the logged-in event manager
                var eventDetail = await _context.EventDetails
                    .FirstOrDefaultAsync(e => e.EventDetailId == id && e.EventManagerId == eventManagerId);

                if (eventDetail == null)
                {
                    return NotFound(new { status = "error", message = "Event detail not found for the given ID" });
                }

                return Ok(new { status = "success", message = "Event detail retrieved successfully", eventDetail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving the event detail.");
                return StatusCode(500, new { status = "error", message = "Failed to retrieve event detail", error = ex.Message });
            }
        }

        // Update notes for event details
        [HttpPut("event-manager/event-details/{id}/notes")]
[Authorize]
public async Task<ActionResult<object>> UpdateEventNotes(int id, [FromBody] UpdateNotesDto request)
{
    try
    {
        var eventDetail = await _context.EventDetails.FindAsync(id);
        if (eventDetail == null)
        {
            return NotFound(new { status = "error", message = "Event detail not found" });
        }

        eventDetail.Notes = request.Notes;
        _context.EventDetails.Update(eventDetail);
        await _context.SaveChangesAsync();

        return Ok(new { status = "success", message = "Notes updated successfully" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error occurred while updating notes for event detail with ID {id}.");
        return StatusCode(500, new { status = "error", message = "Failed to update event notes", error = ex.Message });
    }
}


    }
}
