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
    [Route("api/event-manager")]
    public class EventManagerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventManagerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/event-manager/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardData()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? 
                            User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? 
                           User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { message = "Invalid token. Please log in again." });
            }

            var eventManager = await _context.EventManagers
                .Include(em => em.User)
                .Include(em => em.Events)
                .FirstOrDefaultAsync(em => em.User != null && em.User.Email == userEmail);

            if (eventManager == null || eventManager.User == null)
            {
                return NotFound(new { message = "Event Manager not found." });
            }

            var events = eventManager.Events ?? new List<Event>();

            var completedEventsCount = await _context.EventDetails
                .Where(e => e.EventManagerId == eventManager.EventManagerId && e.Status == "Completed")
                .CountAsync();

            return Ok(new
            {
                id = eventManager.EventManagerId,
                managerName = eventManager.User.FullName ?? "Unknown",
                email = eventManager.User.Email ?? "Unknown",
                organization = eventManager.Organization ?? "Not Provided",
                experienceYears = eventManager.ExperienceYears,
                certifications = eventManager.Certifications ?? "Not Provided",
                eventsCompleted = completedEventsCount,
                pricePerEvent = eventManager.PricePerEvent,
                notifications = new List<string>
                {
                    "New event assigned!",
                    "Budget report needs review.",
                    "Upcoming event in 3 days!"
                }
            });
        }

        // PUT: api/event-manager/update/{id}
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateEventManager(Guid id, [FromForm] EventManager updatedManager)
        {
            var eventManager = await _context.EventManagers.FindAsync(id);

            if (eventManager == null)
            {
                return NotFound(new { message = "Event Manager not found." });
            }

            // Update fields if they are valid (not default or zeroed out unintentionally)
            if (!string.IsNullOrWhiteSpace(updatedManager.Organization))
                eventManager.Organization = updatedManager.Organization;

            if (updatedManager.ExperienceYears > 0)
                eventManager.ExperienceYears = updatedManager.ExperienceYears;

            if (!string.IsNullOrWhiteSpace(updatedManager.Certifications))
                eventManager.Certifications = updatedManager.Certifications;

            // Calculate completed events using database query
            var completedEventsCount = await _context.EventDetails
                .Where(e => e.EventManagerId == id && e.Status == "Completed")
                .CountAsync();
            eventManager.EventsCompleted = completedEventsCount;

            if (updatedManager.PricePerEvent > 0)
                eventManager.PricePerEvent = updatedManager.PricePerEvent;

            try
            {
                _context.Entry(eventManager).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Event Manager updated successfully.",
                    eventManager = new
                    {
                        id = eventManager.EventManagerId,
                        organization = eventManager.Organization,
                        experienceYears = eventManager.ExperienceYears,
                        certifications = eventManager.Certifications,
                        eventsCompleted = eventManager.EventsCompleted,
                        pricePerEvent = eventManager.PricePerEvent
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while updating the profile: {ex.Message}" });
            }
        }

        // GET: api/event-manager/details/{id}
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetEventManagerDetails(Guid id)
        {
            var eventManager = await _context.EventManagers
                .Include(em => em.User)
                .FirstOrDefaultAsync(em => em.EventManagerId == id);

            if (eventManager == null || eventManager.User == null)
            {
                return NotFound(new { message = "Event Manager not found." });
            }

            return Ok(new
            {
                id = eventManager.EventManagerId,
                managerName = eventManager.User.FullName ?? "Unknown",
                email = eventManager.User.Email ?? "Unknown",
                organization = eventManager.Organization ?? "Not Provided",
                experienceYears = eventManager.ExperienceYears,
                certifications = eventManager.Certifications ?? "Not Provided",
                pricePerEvent = eventManager.PricePerEvent,
                eventsCompleted = eventManager.EventsCompleted
            });
        }

        // GET: api/event-manager/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllEventManagers()
        {
            // Fetch all event managers including their user data (if needed)
            var eventManagers = await _context.EventManagers
                .Include(em => em.User) // Include the User (if you need user info like name, email)
                .ToListAsync();

            if (eventManagers == null || eventManagers.Count == 0)
            {
                return NotFound(new { message = "No event managers found." });
            }

            return Ok(eventManagers);
        }
    }
}
