using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;
using System.Security.Claims;

namespace ConnectThruEventsBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublishEventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PublishEventsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Create a new event
        [HttpPost("create")]
        public async Task<IActionResult> CreateEvent([FromBody] PublishEvent publishEvent)
        {
            _context.PublishEvents.Add(publishEvent);
            await _context.SaveChangesAsync();
            return Ok(publishEvent);
        }

        // 2. Get all events
        [HttpGet("all")]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.PublishEvents.ToListAsync();
            return Ok(events);
        }

        // 3. Get event by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _context.PublishEvents.FindAsync(id);
            if (ev == null)
                return NotFound("Event not found");
            return Ok(ev);
        }

        // 4. Enroll a user in an event
[HttpPost("enroll")]
public async Task<IActionResult> EnrollUser([FromBody] EventEnrollment enrollment)
{
    // Check if the user exists
    var userExists = await _context.Users.AnyAsync(u => u.UserId == enrollment.UserId);
    if (!userExists)
    {
        return BadRequest("User not found.");
    }

    // Check if the event exists
    var eventExists = await _context.PublishEvents.AnyAsync(e => e.PublishEventId == enrollment.PublishEventId);
    if (!eventExists)
    {
        return BadRequest("Event not found.");
    }

    // Check if already enrolled (optional, to prevent duplicates)
    var alreadyEnrolled = await _context.EventEnrollments
        .AnyAsync(e => e.UserId == enrollment.UserId && e.PublishEventId == enrollment.PublishEventId);

    if (alreadyEnrolled)
    {
        return BadRequest("User already enrolled in this event.");
    }

    _context.EventEnrollments.Add(enrollment);
    await _context.SaveChangesAsync();

    return Ok(enrollment);
}

        // 5. Get users enrolled in an event
        [HttpGet("enrolled-users/{eventId}")]
        public async Task<IActionResult> GetEnrolledUsers(int eventId)
        {
            var users = await _context.EventEnrollments
                .Include(e => e.User)
                .Where(e => e.PublishEventId == eventId)
                .Select(e => new
                {
                    e.User.UserId,
                    e.User.FullName,
                    e.User.Email
                })
                .ToListAsync();

            return Ok(users);
        }

        // 6. Get events a user is enrolled in
        [HttpGet("user-enrollments/{userId}")]
        public async Task<IActionResult> GetUserEnrolledEvents(int userId)
        {
            var events = await _context.EventEnrollments
                .Include(e => e.PublishEvent)
                .Where(e => e.UserId == userId)
                .Select(e => new
                {
                    e.PublishEvent.PublishEventId,
                    e.PublishEvent.EventTitle,
                    e.PublishEvent.EventDate,
                    e.PublishEvent.EventLocation
                })
                .ToListAsync();

            return Ok(events);
        }

        // 7. Add to Favorites
        // 7. Add to Favorites
[HttpPost("favorite")]
public async Task<IActionResult> AddToFavorites([FromBody] FavoriteEvent favorite)
{
    var exists = await _context.FavoriteEvents
        .AnyAsync(f => f.UserId == favorite.UserId && f.PublishEventId == favorite.PublishEventId);

    if (exists)
    {
        return BadRequest("Event already in favorites");
    }

    _context.FavoriteEvents.Add(favorite);
    await _context.SaveChangesAsync();

    return Ok("Event added to favorites");
}


        // 8. Get User's Favorites
        [HttpGet("favorites/{userId}")]
        public async Task<IActionResult> GetUserFavorites(int userId)
        {
            var favorites = await _context.FavoriteEvents
                .Where(f => f.UserId == userId)
                .Include(f => f.PublishEvent)  // Include the event details
                .Select(f => new
                {
                    f.PublishEvent.PublishEventId,
                    f.PublishEvent.EventTitle,
                    f.PublishEvent.EventCategory,
                    f.PublishEvent.EventDate,
                    f.PublishEvent.EventLocation,
                    f.PublishEvent.TicketType,
                    f.PublishEvent.SpeakerName,
                    f.PublishEvent.ExternalUrl,
                    f.PublishEvent.EventImageUrl,
                    f.PublishEvent.Description,
                    f.PublishEvent.CreatedAt
                })
                .ToListAsync();

            return Ok(favorites);
        }

        // 9. Get User ID from JWT Token (New Endpoint)
        [HttpGet("current-user-id")]
        public IActionResult GetUserIdFromToken()
        {
            // Extract the userId or email from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(ClaimTypes.Email);

            if (userIdClaim == null)
            {
                return Unauthorized("User not authenticated");
            }

            string userId = userIdClaim.Value;  // Extracted user ID from the claim

            return Ok(new { userId });
        }
    }
}
