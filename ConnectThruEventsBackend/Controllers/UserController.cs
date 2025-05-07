using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectThruEventsBackend.Data;
using ConnectThruEventsBackend.Models;
using System.Security.Claims;
using System.Text.Json;
using System.IO;

namespace ConnectThruEventsBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Get user profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            Console.WriteLine("📥 [GetUserProfile] Incoming request...");

            // Log all claims (useful for debugging)
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"🔸 Claim: {claim.Type} = {claim.Value}");
            }

            // Try to get user email from various common claim types
            var userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ?? 
                User.FindFirst("email")?.Value ?? 
                User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            Console.WriteLine($"📧 [GetUserProfile] Resolved userEmail: {userEmail}");

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized(new { message = "User not authenticated (email claim missing)." });

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                Console.WriteLine($"❌ [GetUserProfile] No user found for email: {userEmail}");
                return NotFound(new { message = "User not found" });
            }

            var profilePictureUrl = string.IsNullOrEmpty(user.ProfilePicture)
                ? null
                : $"{Request.Scheme}://{Request.Host}{user.ProfilePicture}";

            return Ok(new
            {
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.Role,
                ProfilePictureUrl = profilePictureUrl,
                user.Bio,
                user.Location,
                SocialLinks = user.GetSocialLinks() ?? new Dictionary<string, string>()
            });
        }

        // Update user profile
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUserProfile([FromForm] UpdateProfileRequest request)
        {
            try
            {
                // Log all claims (useful for debugging)
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"🔸 Claim: {claim.Type} = {claim.Value}");
                }

                var userEmail =
                    User.FindFirst(ClaimTypes.Email)?.Value ?? 
                    User.FindFirst("email")?.Value ?? 
                    User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

                Console.WriteLine($"📧 [UpdateProfile] Resolved userEmail: {userEmail}");

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                {
                    Console.WriteLine($"❌ [UpdateProfile] No user found for email: {userEmail}");
                    return NotFound(new { message = "User not found" });
                }

                Console.WriteLine("[UpdateProfile] Existing user found. Starting update...");

                // Handle profile picture
                if (request.ProfilePicture != null)
                {
                    Console.WriteLine($"🖼️ [UpdateProfile] New profile picture: {request.ProfilePicture.FileName}");

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(request.ProfilePicture.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        Console.WriteLine($"❌ [UpdateProfile] Invalid file extension: {fileExtension}");
                        return BadRequest(new { message = "Invalid file type. Only image files are allowed." });
                    }

                    if (request.ProfilePicture.Length > 5 * 1024 * 1024)
                    {
                        Console.WriteLine($"❌ [UpdateProfile] File too large: {request.ProfilePicture.Length} bytes");
                        return BadRequest(new { message = "File size exceeds the maximum limit of 5MB." });
                    }

                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePicture))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                            Console.WriteLine($"🧹 [UpdateProfile] Deleted old profile picture: {oldFilePath}");
                        }
                    }

                    // Save new profile picture
                    var uploads = Path.Combine(_environment.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploads);
                    var fileName = $"{Guid.NewGuid()}_{request.ProfilePicture.FileName}";
                    var filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.ProfilePicture.CopyToAsync(stream);
                    }

                    user.ProfilePicture = $"/uploads/{fileName}";
                    Console.WriteLine($"✅ [UpdateProfile] New profile picture saved: {filePath}");
                }

                // Log updates
                Console.WriteLine($"[UpdateProfile] Updating fields:");
                Console.WriteLine($"  FullName: {request.FullName}");
                Console.WriteLine($"  PhoneNumber: {request.PhoneNumber}");
                Console.WriteLine($"  Bio: {request.Bio}");
                Console.WriteLine($"  Location: {request.Location}");
                Console.WriteLine($"  SocialLinks: {JsonSerializer.Serialize(request.SocialLinks)}");

                // Apply updates
                user.FullName = request.FullName ?? user.FullName;
                user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
                user.Bio = request.Bio ?? user.Bio;
                user.Location = request.Location ?? user.Location;

                if (request.SocialLinks != null && request.SocialLinks.Count > 0)
                {
                    user.SetSocialLinks(request.SocialLinks);
                    Console.WriteLine("[UpdateProfile] ✅ Social links updated.");
                }

                var rowsAffected = await _context.SaveChangesAsync();
                Console.WriteLine($"💾 [UpdateProfile] SaveChangesAsync returned: {rowsAffected}");

                var profilePictureUrl = string.IsNullOrEmpty(user.ProfilePicture)
                    ? null
                    : $"{Request.Scheme}://{Request.Host}{user.ProfilePicture}";

                return Ok(new
                {
                    message = "Profile updated successfully",
                    user.FullName,
                    user.PhoneNumber,
                    user.Bio,
                    user.Location,
                    ProfilePictureUrl = profilePictureUrl
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [UpdateProfile] Exception occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while updating the profile.", error = ex.Message });
            }
        }

        // Get user ID based on email
        [HttpGet("get-user-id-by-email")]
        public async Task<IActionResult> GetUserIdByEmail()
        {
            Console.WriteLine("📥 [GetUserIdByEmail] Incoming request...");

            // Get email from the JWT claims
            var userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ?? 
                User.FindFirst("email")?.Value ?? 
                User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

            Console.WriteLine($"📧 [GetUserIdByEmail] Resolved userEmail: {userEmail}");

            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized(new { message = "User not authenticated (email claim missing)." });

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                Console.WriteLine($"❌ [GetUserIdByEmail] No user found for email: {userEmail}");
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { userId = user.UserId });
        }
    }
}
