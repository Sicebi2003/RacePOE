
using Api_proj.Data;
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
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IWebHostEnvironment _environment;

        public EventsController(
            ApplicationDBContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int? GetCurrentUserId()
        {
            string? userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (int.TryParse(userId, out int id))
                return id;

            return null;
        }

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            var query = _context.Events
                .AsNoTracking()
                .Include(e => e.Organiser)
                .AsQueryable();

            if (User.IsInRole("Organiser"))
            {
                int? organiserId = GetCurrentUserId();

                if (organiserId == null)
                    return Unauthorized();

                query = query.Where(e =>
                    e.OrganiserId == organiserId.Value
                );
            }

            return await query.ToListAsync();
        }

        // GET: api/events/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .AsNoTracking()
                .Include(e => e.Organiser)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
                return NotFound();

            return eventItem;
        }

        // POST: api/events
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<Event>> CreateEvent(Event eventItem)
        {
            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
                return Unauthorized();

            bool organiserExists = await _context.Users
                .AnyAsync(u => u.UserId == organiserId.Value);

            if (!organiserExists)
                return BadRequest("Organiser account does not exist.");

            eventItem.EventId = 0;
            eventItem.OrganiserId = organiserId.Value;
            eventItem.Organiser = null;
            eventItem.BannerImage = null;
            eventItem.CreatedDate = DateTime.Now;

            _context.Events.Add(eventItem);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = eventItem.EventId },
                eventItem
            );
        }

        // POST: api/events/5/banner
        [HttpPost("{id}/banner")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UploadEventBanner(
            int id,
            IFormFile banner)
        {
            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
                return Unauthorized();

            if (banner == null || banner.Length == 0)
                return BadRequest("Please select a banner image.");

            if (banner.Length > 5 * 1024 * 1024)
                return BadRequest("The banner image must be smaller than 5 MB.");

            string extension = Path.GetExtension(banner.FileName).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Only JPG, JPEG, PNG and WEBP images are allowed.");

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
                return NotFound("Event not found.");

            if (eventItem.OrganiserId != organiserId.Value)
                return Forbid();

            string webRootPath = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            string folderPath = Path.Combine(
                webRootPath,
                "event-images"
            );

            Directory.CreateDirectory(folderPath);

            string fileName = Guid.NewGuid().ToString() + extension;

            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await banner.CopyToAsync(stream);
            }

            string? oldImage = eventItem.BannerImage;

            eventItem.BannerImage = fileName;

            await _context.SaveChangesAsync();

            // Delete the previous banner after saving the new one.
            if (!string.IsNullOrWhiteSpace(oldImage))
            {
                string oldFileName = Path.GetFileName(oldImage);

                string oldFilePath = Path.Combine(
                    folderPath,
                    oldFileName
                );

                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);
            }

            string imageUrl =
                $"{Request.Scheme}://{Request.Host}/event-images/{fileName}";

            return Ok(new
            {
                message = "Event banner uploaded successfully.",
                bannerImage = fileName,
                imageUrl = imageUrl
            });
        }

        // PUT: api/events/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            Event eventItem)
        {
            if (id != eventItem.EventId)
                return BadRequest("Event ID does not match.");

            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
                return Unauthorized();

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (existingEvent == null)
                return NotFound();

            if (existingEvent.OrganiserId != organiserId.Value)
                return Forbid();

            existingEvent.EventName = eventItem.EventName;
            existingEvent.Description = eventItem.Description;
            existingEvent.EventDate = eventItem.EventDate;
            existingEvent.Location = eventItem.Location;
            existingEvent.Distance = eventItem.Distance;
            existingEvent.EventType = eventItem.EventType;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/events/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            int? organiserId = GetCurrentUserId();

            if (organiserId == null)
                return Unauthorized();

            var eventItem = await _context.Events
                .Include(e => e.Categories)
                .Include(e => e.Enrolments)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
                return NotFound("Event not found.");

            if (eventItem.OrganiserId != organiserId.Value)
                return Forbid();

            if (eventItem.Enrolments.Any())
            {
                return BadRequest(
                    "This event cannot be deleted because participants are enrolled."
                );
            }

            _context.Categories.RemoveRange(eventItem.Categories);

            _context.Events.Remove(eventItem);

            await _context.SaveChangesAsync();

            // Delete the event banner file.
            if (!string.IsNullOrWhiteSpace(eventItem.BannerImage))
            {
                string webRootPath = _environment.WebRootPath
                    ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

                string fileName = Path.GetFileName(eventItem.BannerImage);

                string imagePath = Path.Combine(
                    webRootPath,
                    "event-images",
                    fileName
                );

                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            return Ok(new
            {
                message = "Event deleted successfully."
            });
        }
    }
}
