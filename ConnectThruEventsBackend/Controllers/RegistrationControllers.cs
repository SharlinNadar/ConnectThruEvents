using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;
using BCrypt.Net;
using Newtonsoft.Json;


namespace ConnectThruEventsBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RegistrationController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Registration/register
        [HttpPost("register")]
public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationRequest request)
{
    Console.WriteLine(" Incoming Registration Request: " + Newtonsoft.Json.JsonConvert.SerializeObject(request));

    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState); // Return validation errors
    }

    //Check if email is already registered
    if (await _context.Users.AnyAsync(u => u.Email == request.Email))
    {
        return BadRequest(new { message = "Email is already in use." });
    }

    // Create User object
    var newUser = new User
    {
        FullName = request.FullName,
        Email = request.Email,
        PhoneNumber = request.PhoneNumber,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = request.Role
    };

    _context.Users.Add(newUser);
    await _context.SaveChangesAsync(); // Save user first to get UserId

    //If user is an Event Manager, store additional details
    if (request.Role == "event_manager" && request.EventManager != null)
    {
        var eventManager = new EventManager
        {
            UserId = newUser.UserId, // Use UserId from newUser
            Organization = request.EventManager.Organization,
            ExperienceYears = request.EventManager.ExperienceYears,
            Certifications = request.EventManager.Certifications ?? "None"
        };

        _context.EventManagers.Add(eventManager);
        await _context.SaveChangesAsync();
    }

    return Ok(new { message = "User registered successfully!", userId = newUser.UserId });
}

    }
}
