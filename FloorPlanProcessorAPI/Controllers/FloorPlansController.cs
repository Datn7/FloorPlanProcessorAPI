
using FloorPlanProcessorAPI.Data;
using FloorPlanProcessorAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FloorPlanProcessor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FloorPlansController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FloorPlansController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // POST: api/FloorPlans/upload
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var svgFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "svgs");
            Directory.CreateDirectory(svgFolder);

            var svgFileName = Path.GetFileNameWithoutExtension(uniqueFileName) + ".svg";
            var svgFilePath = Path.Combine(svgFolder, svgFileName);

            var processor = new FloorPlanProcessorService();
            processor.ProcessAndGenerateSvg(filePath, svgFilePath);

            var floorPlan = new FloorPlan
            {
                UserId = userId,
                FileName = uniqueFileName,
                SvgPath = svgFileName,
                CreatedAt = DateTime.UtcNow
            };

            _context.FloorPlans.Add(floorPlan);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "File uploaded and processed successfully",
                FloorPlanId = floorPlan.Id,
                FileName = floorPlan.FileName,
                SvgPath = floorPlan.SvgPath
            });

        }

        // GET: api/FloorPlans
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FloorPlan>>> GetFloorPlans()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _context.FloorPlans
                .Where(fp => fp.UserId == userId)
                .OrderByDescending(fp => fp.CreatedAt)
                .ToListAsync();
        }

        // GET: api/FloorPlans/{id}/svg
        [HttpGet("{id}/svg")]
        public async Task<IActionResult> GetSvg(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var floorPlan = await _context.FloorPlans.FirstOrDefaultAsync(fp => fp.Id == id && fp.UserId == userId);

            if (floorPlan == null)
                return NotFound();

            if (string.IsNullOrEmpty(floorPlan.SvgPath))
                return BadRequest("SVG not generated yet.");

            var svgPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "svgs", floorPlan.SvgPath);

            if (!System.IO.File.Exists(svgPath))
                return NotFound("SVG file not found.");

            var svgContent = await System.IO.File.ReadAllTextAsync(svgPath);
            return Content(svgContent, "image/svg+xml");
        }
    }
}
