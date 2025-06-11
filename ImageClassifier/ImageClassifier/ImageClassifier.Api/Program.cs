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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply CORS policy before mapping controllers and other endpoints
app.UseCors("AllowReactApp");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Map controller routes
app.MapControllers();

// Apply CORS policy - re-applying here or ensuring it's before UseEndpoints/MapControllers if not before UseStaticFiles for API calls.
// The current placement of UseCors is after UseStaticFiles. This is usually fine for <img> tags.
// If API calls from JS to /Uploads were a thing and needed CORS, this might be an issue.
// For now, the existing placement is likely fine.
// app.UseCors("AllowReactApp"); // Removed duplicate


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
