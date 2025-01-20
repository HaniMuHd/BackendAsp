using System.ComponentModel.DataAnnotations;

namespace BackendAsp.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Image { get; set; } = string.Empty;

    [Required]
    public string Price { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;
} 