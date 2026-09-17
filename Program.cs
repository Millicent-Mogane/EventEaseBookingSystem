using EventEaseBookingSystem.Data; // lets Program.cs see ApplicationDbContext
using Microsoft.EntityFrameworkCore; // needed for UseSqlServer
using Azure.Storage.Blobs; // needed for BlobServiceClient

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection was not found in appsettings.json.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register Azure Blob Storage service
var azureBlobConnectionString =
    builder.Configuration.GetConnectionString("AzureBlobStorage");

if (string.IsNullOrWhiteSpace(azureBlobConnectionString))
{
    throw new InvalidOperationException(
        "AzureBlobStorage connection string was not found in appsettings.json.");
}

builder.Services.AddSingleton<BlobServiceClient>(serviceProvider =>
{
    return new BlobServiceClient(azureBlobConnectionString);
});

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Enforce HTTPS in production
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();