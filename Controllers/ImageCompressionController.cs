using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackendAsp.Models;
using BackendAsp.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace BackendAsp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageCompressionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ImageCompressionController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ImageCompression>>> GetAll()
    {
        return await _context.ImageCompressions.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ImageCompression>> GetById(int id)
    {
        var imageCompression = await _context.ImageCompressions.FindAsync(id);
        if (imageCompression == null)
            return NotFound();
        
        return imageCompression;
    }

    [HttpPost]
    public async Task<ActionResult<ImageCompression>> Create(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        // Create upload directory if it doesn't exist
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Generate unique filenames
        var originalFileName = Guid.NewGuid().ToString() + "_original" + Path.GetExtension(file.FileName);
        var compressedFileName = Guid.NewGuid().ToString() + "_compressed" + Path.GetExtension(file.FileName);
        
        var originalPath = Path.Combine(uploadsFolder, originalFileName);
        var compressedPath = Path.Combine(uploadsFolder, compressedFileName);

        // Save original file
        using (var stream = new FileStream(originalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Compress image
        using (var image = await Image.LoadAsync(originalPath))
        {
            // Resize to 50% of original size while maintaining aspect ratio
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(image.Width / 2, image.Height / 2),
                Mode = ResizeMode.Max
            }));

            // Save compressed image with reduced quality
            await image.SaveAsync(compressedPath);
        }

        // Get file sizes
        var originalSize = new FileInfo(originalPath).Length;
        var compressedSize = new FileInfo(compressedPath).Length;

        // Create database record
        var imageCompression = new ImageCompression
        {
            OriginalImage = "/uploads/" + originalFileName,
            ImageSize = originalSize,
            CompressedImage = "/uploads/" + compressedFileName,
            CompressedImageSize = compressedSize
        };

        _context.ImageCompressions.Add(imageCompression);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = imageCompression.Id }, imageCompression);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ImageCompression imageCompression)
    {
        if (id != imageCompression.Id)
            return BadRequest();

        _context.Entry(imageCompression).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.ImageCompressions.AnyAsync(i => i.Id == id))
                return NotFound();
            throw;
        }
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var imageCompression = await _context.ImageCompressions.FindAsync(id);
        if (imageCompression == null)
            return NotFound();

        // Delete the physical files
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        var originalPath = Path.Combine(uploadsFolder, Path.GetFileName(imageCompression.OriginalImage));
        var compressedPath = Path.Combine(uploadsFolder, Path.GetFileName(imageCompression.CompressedImage));

        if (System.IO.File.Exists(originalPath))
            System.IO.File.Delete(originalPath);
        
        if (System.IO.File.Exists(compressedPath))
            System.IO.File.Delete(compressedPath);

        _context.ImageCompressions.Remove(imageCompression);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
} 