using System.ComponentModel.DataAnnotations;

namespace BackendAsp.Models;

public class ImageCompression
{
    public int Id { get; set; }

    [Required]
    public string OriginalImage { get; set; } = string.Empty;

    [Required]
    public float ImageSize { get; set; }

    [Required]
    public string CompressedImage { get; set; } = string.Empty;

    [Required]
    public float CompressedImageSize { get; set; }
} 