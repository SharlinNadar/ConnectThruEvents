using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;

namespace ConnectThruEventsBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //  Admin Registration (Hashes Password Before Saving)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AdminRegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.FullName))
                return BadRequest(new { message = "All fields are required." });

            if (await _context.Admins.AnyAsync(a => a.Email == request.Email))
                return BadRequest(new { message = "Admin already exists." });

            // Hash the password before saving
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newAdmin = new Admin
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            _context.Admins.Add(newAdmin);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin registered successfully." });
        }

        // Admin Login
       [HttpPost("login")]
public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
{
    if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        return BadRequest(new { message = "Email and Password are required." });

    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == request.Email);
    if (admin == null) 
        return Unauthorized(new { message = "Invalid credentials. Admin not found." });

    //  Debugging Log
    Console.WriteLine($"Stored Hash: {admin.PasswordHash}");
    Console.WriteLine($"Entered Password: {request.Password}");
    
    if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
    {
        Console.WriteLine(" Password does not match!");
        return Unauthorized(new { message = "Invalid credentials." });
    }

    var token = GenerateJwtToken(admin);
    return Ok(new { token });
}


        // Fetch Admin Profile
        [HttpGet("profile")]
public async Task<IActionResult> GetProfile()
{
    var email = User.FindFirstValue(ClaimTypes.Email);
    if (email == null) return Unauthorized(new { message = "Invalid token." });

    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
    if (admin == null) return NotFound(new { message = "Admin not found." });

    return Ok(new
    {
        admin.FullName,
        admin.Email
    });
}


        // Generate JWT Token
        private string GenerateJwtToken(Admin admin)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
                throw new Exception("JWT Key is missing or too short. Check appsettings.json.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, admin.Email),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Request Model for Admin Login
    public class AdminLoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    //  Request Model for Admin Registration
    public class AdminRegisterRequest
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
