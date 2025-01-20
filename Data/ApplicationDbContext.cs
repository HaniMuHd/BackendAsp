using Microsoft.EntityFrameworkCore;
using BackendAsp.Models;

namespace BackendAsp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Todo> Todos { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ImageCompression> ImageCompressions { get; set; }
} 