using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;

namespace ConnectThruEventsBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/created-events")]
    public class CreatedEventController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CreatedEventController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/created-events/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateEvent([FromBody] CreatedEvent newEvent)
        {
            if (newEvent == null)
            {
                return BadRequest(new { message = "Invalid event data." });
            }

            try
            {
                //Get user info from token
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? 
                                User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found in database." });
                }

                // Assign user ID
                newEvent.UserId = user.UserId;

                // Dynamically calculate service charge percentage based on budget
                newEvent.SetServiceChargePercentage(); // Use the original SetServiceChargePercentage method

                // Calculate service charge and total price
                newEvent.CalculateServiceChargeAndTotalPrice();

                // Save the new event
                _context.CreatedEvents.Add(newEvent);
                await _context.SaveChangesAsync();

                // Return the created event with all its details including the calculated fields
                return CreatedAtAction("GetEvent", new { id = newEvent.Id }, newEvent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while creating the event: {ex.Message}" });
            }
        }

        // GET: api/created-events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            var createdEvent = await _context.CreatedEvents
                .Include(ce => ce.User)
                .Include(ce => ce.EventManager)
                .FirstOrDefaultAsync(ce => ce.Id == id);

            if (createdEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            return Ok(createdEvent);
        }

        // GET: api/created-events/dashboard
[HttpGet("dashboard")]
public async Task<IActionResult> GetDashboardData()
{
    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

    if (string.IsNullOrEmpty(userEmail))
    {
        return Unauthorized(new { message = "Invalid token. Please log in again." });
    }

    var eventManager = await _context.EventManagers
        .Include(em => em.User)
        .FirstOrDefaultAsync(em => em.User != null && em.User.Email == userEmail);

    if (eventManager == null || eventManager.User == null)
    {
        return NotFound(new { message = "Event Manager not found." });
    }

    var completedEventsCount = await _context.EventDetails
        .Where(e => e.EventManagerId == eventManager.EventManagerId && e.Status == "Completed")
        .CountAsync();

    //Get all pending created events assigned to this event manager
    var pendingEvents = await _context.CreatedEvents
        .Where(e => e.EventManagerId == eventManager.EventManagerId && e.Status == "Pending")
        .Select(e => new
        {
            e.Id,
            e.EventTitle,
            e.EventDate,
            e.Budget,
            e.ServiceCharge,
            e.TotalPrice,
            e.Status,
            e.CreatedAt
        })
        .ToListAsync();

    return Ok(new
    {
        eventManagerId = eventManager.EventManagerId,
        managerName = eventManager.User.FullName ?? "Unknown",
        email = eventManager.User.Email ?? "Unknown",
        organization = eventManager.Organization ?? "Not Provided",
        experienceYears = eventManager.ExperienceYears,
        certifications = eventManager.Certifications ?? "Not Provided",
        eventsCompleted = completedEventsCount,
        pricePerEvent = eventManager.PricePerEvent,

        // 🛎️ Return pending events instead of string notifications
        notifications = pendingEvents
    });
}


        // PUT: api/created-events/accept/{id}
        [HttpPut("accept/{id}")]
        public async Task<IActionResult> AcceptEvent(Guid id)
        {
            var createdEvent = await _context.CreatedEvents
                .Include(ce => ce.EventManager)
                .FirstOrDefaultAsync(ce => ce.Id == id);

            if (createdEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            // Check if the EventManager is assigned
            if (createdEvent.EventManager == null)
            {
                return BadRequest(new { message = "Event Manager is not assigned yet." });
            }

            // Change status to 'Accepted' and notify the user
            createdEvent.Status = "Accepted";
            createdEvent.UpdatedAt = DateTime.UtcNow;

            _context.Entry(createdEvent).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Here you can add a notification for the user about the acceptance
            // For simplicity, I'm just returning a success message.
            return Ok(new { message = "Event accepted by the Event Manager." });
        }

        // PUT: api/created-events/deny/{id}
        [HttpPut("deny/{id}")]
        public async Task<IActionResult> DenyEvent(Guid id)
        {
            var createdEvent = await _context.CreatedEvents
                .Include(ce => ce.EventManager)
                .FirstOrDefaultAsync(ce => ce.Id == id);

            if (createdEvent == null)
            {
                return NotFound(new { message = "Event not found." });
            }

            // Check if the EventManager is assigned
            if (createdEvent.EventManager == null)
            {
                return BadRequest(new { message = "Event Manager is not assigned yet." });
            }

            // Change status to 'Denied' and notify the user
            createdEvent.Status = "Denied";
            createdEvent.UpdatedAt = DateTime.UtcNow;

            _context.Entry(createdEvent).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Here you can add a notification for the user about the denial
            // For simplicity, I'm just returning a success message.
            return Ok(new { message = "Event denied by the Event Manager." });
        }
        
        // GET: api/created-events/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyCreatedEvents()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? 
                            User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { message = "User not authenticated." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found in database." });
            }

            // Fetch the events created by the user
            var createdEvents = await _context.CreatedEvents
                .Where(ce => ce.UserId == user.UserId)
                .ToListAsync();

            if (createdEvents == null || createdEvents.Count == 0)
            {
                return NotFound(new { message = "No created events found." });
            }

            return Ok(createdEvents);
        }

        // GET: api/created-events/accepted/{eventManagerId}
[HttpGet("accepted/{eventManagerId}")]
public async Task<IActionResult> GetAcceptedEventsByManager(Guid eventManagerId)
{
    // Check if the EventManager exists in the database
    var eventManager = await _context.EventManagers
        .FirstOrDefaultAsync(em => em.EventManagerId == eventManagerId);

    if (eventManager == null)
    {
        return NotFound(new { message = "Event Manager not found." });
    }

    // Get all accepted events assigned to this event manager
    var acceptedEvents = await _context.CreatedEvents
        .Where(e => e.EventManagerId == eventManagerId && e.Status == "Accepted")
        .Select(e => new
        {
            e.Id,
            e.EventTitle,
            e.EventDate,
            e.Budget,
            e.ServiceCharge,
            e.TotalPrice,
            e.Status,
            e.CreatedAt
        })
        .ToListAsync();

    if (acceptedEvents == null || acceptedEvents.Count == 0)
    {
        return NotFound(new { message = "No accepted events found for this event manager." });
    }

    return Ok(acceptedEvents);
}


    }
}
