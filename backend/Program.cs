using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Trellochocero.Api.Extensions;
using Trellochocero.Api.Hubs;
using Trellochocero.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddCorsPolicy();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Run migrations automatically on startup (PostgreSQL in cloud)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Trellochocero.Api.Data.AppDbContext>();
    if (db.Database.IsRelational())
    {
        try
        {
            Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.Migrate(db.Database);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Failed to apply migrations on startup.");
        }
    }
}

// Ensure uploads directory exists and configure static file serving
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trellochocero API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BoardHub>("/hubs/board");

app.Run();

// Make Program accessible for integration tests (WebApplicationFactory<Program>)
public partial class Program { }
