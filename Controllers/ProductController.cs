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
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ProductController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        return await _context.Products.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();
        
        return product;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromForm] ProductCreateDto productDto)
    {
        if (productDto.Image == null || productDto.Image.Length == 0)
            return BadRequest("No image uploaded");

        // Create upload directory if it doesn't exist
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Generate unique filenames
        var originalFileName = Guid.NewGuid().ToString() + "_original" + Path.GetExtension(productDto.Image.FileName);
        var compressedFileName = Guid.NewGuid().ToString() + "_compressed" + Path.GetExtension(productDto.Image.FileName);
        
        var originalPath = Path.Combine(uploadsFolder, originalFileName);
        var compressedPath = Path.Combine(uploadsFolder, compressedFileName);

        // Save original file
        using (var stream = new FileStream(originalPath, FileMode.Create))
        {
            await productDto.Image.CopyToAsync(stream);
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

        // Create image compression record
        var imageCompression = new ImageCompression
        {
            OriginalImage = "/uploads/" + originalFileName,
            ImageSize = originalSize,
            CompressedImage = "/uploads/" + compressedFileName,
            CompressedImageSize = compressedSize
        };

        _context.ImageCompressions.Add(imageCompression);

        // Create product with compressed image
        var product = new Product
        {
            Name = productDto.Name,
            Category = productDto.Category,
            Price = productDto.Price,
            Description = productDto.Description,
            Image = "/uploads/" + compressedFileName // Use the compressed image for the product
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] ProductUpdateDto productDto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        // Update basic properties
        product.Name = productDto.Name;
        product.Category = productDto.Category;
        product.Price = productDto.Price;
        product.Description = productDto.Description;

        // Handle image update if provided
        if (productDto.Image != null && productDto.Image.Length > 0)
        {
            // Delete old image if it exists
            var oldImagePath = Path.Combine(_environment.WebRootPath, product.Image.TrimStart('/'));
            if (System.IO.File.Exists(oldImagePath))
                System.IO.File.Delete(oldImagePath);

            // Generate new filename for compressed image
            var compressedFileName = Guid.NewGuid().ToString() + "_compressed" + Path.GetExtension(productDto.Image.FileName);
            var compressedPath = Path.Combine(_environment.WebRootPath, "uploads", compressedFileName);

            // Compress and save new image
            using (var image = await Image.LoadAsync(productDto.Image.OpenReadStream()))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(image.Width / 2, image.Height / 2),
                    Mode = ResizeMode.Max
                }));

                await image.SaveAsync(compressedPath);
            }

            // Update product image path
            product.Image = "/uploads/" + compressedFileName;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Products.AnyAsync(p => p.Id == id))
                return NotFound();
            throw;
        }
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        // Delete the image file
        var imagePath = Path.Combine(_environment.WebRootPath, product.Image.TrimStart('/'));
        if (System.IO.File.Exists(imagePath))
            System.IO.File.Delete(imagePath);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}

// DTOs for handling form data
public class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public IFormFile Image { get; set; } = null!;
    public string Price { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ProductUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
    public string Price { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
} 