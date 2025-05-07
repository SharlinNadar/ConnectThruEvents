using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ConnectThruEventsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserAndEventManagerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserAndEventManagerController(AppDbContext context)
        {
            _context = context;
        }

        // Endpoint 1: GET /api/userandeventmanager/users
        // Fetches a list of all users (without event manager details)
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.Role,
                    u.ProfilePicture,
                    u.Bio,
                    u.Location,
                    SocialLinks = u.GetSocialLinks() ?? new Dictionary<string, string>() // Return empty dictionary if no social links
                })
                .ToListAsync();

            if (users == null || !users.Any())
            {
                return NotFound("No users found.");
            }

            return Ok(users);
        }

        // Endpoint 2: GET /api/userandeventmanager/eventmanagers
        // Fetches a list of all event managers (with user details included)
        [HttpGet("eventmanagers")]
        public async Task<IActionResult> GetAllEventManagers()
        {
            var eventManagers = await _context.EventManagers
                .Include(em => em.User)  // Include the related User entity
                .ToListAsync();  // Fetch the event managers along with their user details

            var result = eventManagers.Select(em => new
            {
                em.EventManagerId,
                FullName = em.User != null ? em.User.FullName : null,  // Check if User is null
                Email = em.User != null ? em.User.Email : null,
                PhoneNumber = em.User != null ? em.User.PhoneNumber : null,
                Role = em.User != null ? em.User.Role : null,
                ProfilePicture = em.User != null ? em.User.ProfilePicture : null,
                Bio = em.User != null ? em.User.Bio : null,
                Location = em.User != null ? em.User.Location : null,
                SocialLinks = em.User != null ? em.User.GetSocialLinks() : new Dictionary<string, string>(), // Handle null user case
                em.Organization,
                em.ExperienceYears,
                em.Certifications,
                em.EventsCompleted,
                em.PricePerEvent
            }).ToList();

            if (result == null || !result.Any())
            {
                return NotFound("No event managers found.");
            }

            return Ok(result);
        }
        [HttpGet("created-events")]
public async Task<IActionResult> GetAllCreatedEventsWithDetails()
{
    var events = await _context.CreatedEvents
        .Include(e => e.User)  // Include the related User entity
        .Include(e => e.EventManager)  // Include the related EventManager entity
            .ThenInclude(em => em.User) // Include nested User inside EventManager
        .ToListAsync();

    var result = events.Select(e => new
    {
        e.Id,
        e.EventTitle,
        e.EventDate,
        e.Budget,
        e.ServiceChargePercentage,
        e.ServiceCharge,
        e.TotalPrice,
        e.Status,
        e.CreatedAt,
        e.UpdatedAt,

        // Safe check for User and EventManager
        UserName = e.User != null ? e.User.FullName : "No User",  // Check if User is null and handle accordingly
        EventManagerName = e.EventManager != null && e.EventManager.User != null ? e.EventManager.User.FullName : "No Event Manager"  // Check if EventManager or its User is null
    }).ToList();

    if (result == null || !result.Any())
    {
        return NotFound("No created events found.");
    }

    return Ok(result);
}

    }
}
