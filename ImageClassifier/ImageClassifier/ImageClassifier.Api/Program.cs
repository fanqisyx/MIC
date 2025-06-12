var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000") // Frontend URL
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

// Register CategoryService
using ImageClassifier.Api.Services; // Add this using
builder.Services.AddSingleton<CategoryService>();
builder.Services.AddSingleton<ClassificationService>(); // Register ClassificationService
builder.Services.AddScoped<ImageClassifier.Api.Services.LatexReportService>(); // Register LatexReportService

// Ensure Controllers are used
builder.Services.AddControllers();


var app = builder.Build();

// Configure static files for the 'Uploads' folder
using Microsoft.Extensions.FileProviders; // Required for PhysicalFileProvider
using System.IO; // Required for Path
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads" // This makes files accessible via http://localhost:xxxx/Uploads/filename.jpg
});

// Serve React static files from image-classifier-ui/build
var reactAppBuildPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../image-classifier-ui/build"));
if (Directory.Exists(reactAppBuildPath)) // Only configure if the build path exists
{
    // Serve default file (index.html) from the React build folder
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new PhysicalFileProvider(reactAppBuildPath),
        RequestPath = "" // Important: Serve index.html for the root path
    });

    // Serve static files (js, css, images) from the React build folder
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(reactAppBuildPath),
        RequestPath = "" // Serve these files from the root
    });
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection(); // Should be early.

// CORS policy should be applied before endpoints that need it.
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowReactApp"); // Apply CORS policy in development
    app.UseSwagger();
    app.UseSwaggerUI();
}
// In production, CORS might be handled by the hosting environment (e.g., Azure App Service, Nginx)
// or a more restrictive policy could be applied here.


// app.UseAuthentication(); // If you had authentication
// app.UseAuthorization(); // If you had authorization

// Map controller routes
app.MapControllers();

// SPA Fallback: If the React app build path exists, map fallback to its index.html.
// This ensures that client-side routes are handled by the React app.
if (Directory.Exists(reactAppBuildPath)) // Check again, or rely on the check above
{
    app.MapFallbackToFile("index.html"); // This will serve index.html from the path configured in UseDefaultFiles/UseStaticFiles if path is ""
}

app.Run();
