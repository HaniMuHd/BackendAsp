using Microsoft.EntityFrameworkCore;
using BackendAsp.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BackendAsp API", Version = "v1" });
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add DbContext configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add this configuration for Railway deployment
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Try to get port from environment variable, fallback to 5000 for local development
    var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "5000");
    serverOptions.ListenAnyIP(port);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackendAsp API V1");
    });
}
else
{
    // Enable Swagger in production as well (optional, but useful for testing)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackendAsp API V1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(); // Enable CORS
app.UseAuthorization();
app.MapControllers();

// Add welcome message at root URL
app.MapGet("/", () => "Backend API is running! Go to /swagger to test the APIs");

app.Run();
