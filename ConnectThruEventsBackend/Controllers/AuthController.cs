using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ConnectThruEventsBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Login Method (Authenticates User & Generates JWT)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Email and Password are required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials." });

            // Check Password Hash
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials." });

            // Get User Role and Event Manager ID (if applicable)
            string userRole = user.Role ?? "User";
            string eventManagerId = string.Empty;

            var eventManager = await _context.EventManagers.FirstOrDefaultAsync(e => e.UserId == user.UserId);
            if (eventManager != null)
            {
                userRole = "EventManager";
                eventManagerId = eventManager.EventManagerId.ToString();
            }

            // Debugging Log (Check Role and Event Manager ID)
            Console.WriteLine($"User: {user.Email}, Assigned Role: {userRole}, Event Manager ID: {eventManagerId}");

            // Generate JWT Token with Role-Based Access
            var token = GenerateJwtToken(user, userRole, eventManagerId);

            Console.WriteLine($"Returning Token: {token}");
            Console.WriteLine($"Returning Role: {userRole}");

            return Ok(new { token, role = userRole });
        }

        // JWT Token Generation Method
        private string GenerateJwtToken(User user, string role, string eventManagerId)
        {
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(key) || key.Length < 32)
            {
                throw new Exception("JWT Key is missing or too short in appsettings.json!");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role),
                new Claim("EventManagerId", eventManagerId) // Add Event Manager ID to the claims
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            // Debugging Log (Check JWT Claims)
            Console.WriteLine("Generated JWT Token Claims:");
            foreach (var claim in claims)
            {
                Console.WriteLine($"  {claim.Type}: {claim.Value}");
            }

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Login Request Model
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
