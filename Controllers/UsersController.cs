
using Api_proj.Data;
using Api_proj.DTOs;
using Api_proj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Api_proj.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public UsersController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: api/users/profile
        [HttpGet("profile")]
        public async Task<ActionResult<User>> GetMyProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) ||
                !int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/users/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UserProfileDto updatedUserDto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) ||
                !int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.Username = updatedUserDto.Username;
            user.Email = updatedUserDto.Email;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully!" });
        }

        // POST: api/users/profile/image
        [HttpPost("profile/image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile image)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) ||
                !int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            if (image == null || image.Length == 0)
            {
                return BadRequest("Please select an image.");
            }

            // Maximum image size: 5 MB.
            if (image.Length > 5 * 1024 * 1024)
            {
                return BadRequest("Image size cannot exceed 5 MB.");
            }

            // Only allow these image formats.
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Create a unique filename.
            var fileName = Guid.NewGuid().ToString() + extension;

            // Save inside wwwroot/profile-images.
            var folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "profile-images"
            );

            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // Delete the user's previous profile image.
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                var oldFileName = Path.GetFileName(user.ProfileImage);

                var oldFilePath = Path.Combine(folderPath, oldFileName);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Save the new filename in the database.
            user.ProfileImage = fileName;

            await _context.SaveChangesAsync();

            var imageUrl = $"{Request.Scheme}://{Request.Host}/profile-images/{fileName}";

            return Ok(new
            {
                message = "Profile image uploaded successfully!",
                profileImage = fileName,
                imageUrl = imageUrl
            });
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
