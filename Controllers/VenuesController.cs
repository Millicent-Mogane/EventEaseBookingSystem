using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventEaseBookingSystem.Data;
using EventEaseBookingSystem.Models;

namespace EventEaseBookingSystem.Controllers
{
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;

        public VenuesController(
            ApplicationDbContext context,
            BlobServiceClient blobServiceClient,
            IConfiguration configuration)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
            _configuration = configuration;
        }

        // GET: Venues
        public async Task<IActionResult> Index(
            string searchString,
            int? minCapacity,
            int? maxCapacity,
            bool exactMatch = false)
        {
            var venues = _context.Venues.AsQueryable();

            // Search by venue name or location.
            if (!string.IsNullOrEmpty(searchString))
            {
                venues = venues.Where(v =>
                    v.VenueName.Contains(searchString) ||
                    v.Location.Contains(searchString));
            }

            // Filter by minimum capacity.
            if (minCapacity.HasValue)
            {
                venues = exactMatch
                    ? venues.Where(v => v.Capacity == minCapacity.Value)
                    : venues.Where(v => v.Capacity >= minCapacity.Value);
            }

            // Filter by maximum capacity.
            if (maxCapacity.HasValue)
            {
                venues = venues.Where(v =>
                    v.Capacity <= maxCapacity.Value);
            }

            var resultList = await venues.ToListAsync();

            // Display a message when no venues match the search.
            if (!resultList.Any())
            {
                ViewBag.Message =
                    "No venues found for the specified search criteria.";
            }

            return View(resultList);
        }

        // GET: Venues/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var venue = await _context.Venues
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null)
                return NotFound();

            return View(venue);
        }

        // GET: Venues/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Venues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("VenueId,VenueName,Location,Capacity,HourlyRate,ImageUrl")] Venue venue,
            IFormFile imageFile)
        {
            // ImageUrl is generated from the uploaded image.
            ModelState.Remove(nameof(Venue.ImageUrl));

            // Check that an image was selected.
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError(
                    "ImageUrl",
                    "Please select an image for the venue.");
            }
            else
            {
                // Only allow common image formats.
                var allowedContentTypes = new[]
                {
                    "image/jpeg",
                    "image/png",
                    "image/gif",
                    "image/webp"
                };

                if (!allowedContentTypes.Contains(
                    imageFile.ContentType.ToLowerInvariant()))
                {
                    ModelState.AddModelError(
                        "ImageUrl",
                        "Only JPEG, PNG, GIF, and WebP images are allowed.");
                }

                // Limit uploaded images to 5 MB.
                const long maxFileSize = 5 * 1024 * 1024;

                if (imageFile.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        "ImageUrl",
                        "The image must be smaller than 5 MB.");
                }
            }

            if (ModelState.IsValid)
            {
                var containerName =
                    _configuration["AzureStorage:ContainerName"];

                if (string.IsNullOrWhiteSpace(containerName))
                {
                    ModelState.AddModelError(
                        "",
                        "Azure Blob container name is not configured.");

                    return View(venue);
                }

                // Get the Azure Blob Storage container.
                var containerClient =
                    _blobServiceClient.GetBlobContainerClient(containerName);

                await containerClient.CreateIfNotExistsAsync();

                // Give the uploaded image a unique file name.
                var extension =
                    Path.GetExtension(imageFile!.FileName)
                        .ToLowerInvariant();

                var blobName =
                    $"{Guid.NewGuid()}{extension}";

                var blobClient =
                    containerClient.GetBlobClient(blobName);

                // Upload the image to Azure Blob Storage.
                using (var stream = imageFile.OpenReadStream())
                {
                    var uploadOptions = new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = imageFile.ContentType
                        }
                    };

                    await blobClient.UploadAsync(
                        stream,
                        uploadOptions);
                }

                // Save the Azure Blob URL in the database.
                venue.ImageUrl = blobClient.Uri.ToString();

                _context.Add(venue);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Venue created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(venue);
        }

        // GET: Venues/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var venue = await _context.Venues.FindAsync(id);

            if (venue == null)
                return NotFound();

            return View(venue);
        }

        // POST: Venues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("VenueId,VenueName,Location,Capacity,HourlyRate,ImageUrl")]
            Venue venue,
            IFormFile imageFile)
        {
            if (id != venue.VenueId)
                return NotFound();

            // ImageUrl does not have to be supplied by the form.
            ModelState.Remove(nameof(Venue.ImageUrl));

            if (ModelState.IsValid)
            {
                try
                {
                    // If a new image was uploaded, replace the image URL.
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var allowedContentTypes = new[]
                        {
                            "image/jpeg",
                            "image/png",
                            "image/gif",
                            "image/webp"
                        };

                        if (!allowedContentTypes.Contains(
                            imageFile.ContentType.ToLowerInvariant()))
                        {
                            ModelState.AddModelError(
                                "ImageUrl",
                                "Only JPEG, PNG, GIF, and WebP images are allowed.");

                            return View(venue);
                        }

                        const long maxFileSize =
                            5 * 1024 * 1024;

                        if (imageFile.Length > maxFileSize)
                        {
                            ModelState.AddModelError(
                                "ImageUrl",
                                "The image must be smaller than 5 MB.");

                            return View(venue);
                        }

                        var containerName =
                            _configuration["AzureStorage:ContainerName"];

                        if (string.IsNullOrWhiteSpace(containerName))
                        {
                            ModelState.AddModelError(
                                "",
                                "Azure Blob container name is not configured.");

                            return View(venue);
                        }

                        var containerClient =
                            _blobServiceClient
                                .GetBlobContainerClient(containerName);

                        await containerClient.CreateIfNotExistsAsync();

                        var extension =
                            Path.GetExtension(imageFile.FileName)
                                .ToLowerInvariant();

                        var blobName =
                            $"{Guid.NewGuid()}{extension}";

                        var blobClient =
                            containerClient.GetBlobClient(blobName);

                        // Upload the new image to Azure Blob Storage.
                        using (var stream =
                            imageFile.OpenReadStream())
                        {
                            var uploadOptions =
                                new BlobUploadOptions
                                {
                                    HttpHeaders =
                                        new BlobHttpHeaders
                                        {
                                            ContentType =
                                                imageFile.ContentType
                                        }
                                };

                            await blobClient.UploadAsync(
                                stream,
                                uploadOptions);
                        }

                        venue.ImageUrl =
                            blobClient.Uri.ToString();
                    }
                    else
                    {
                        // Keep the existing image if no new image
                        // was uploaded.
                        var existingVenue =
                            await _context.Venues
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    v => v.VenueId == venue.VenueId);

                        if (existingVenue != null)
                        {
                            venue.ImageUrl =
                                existingVenue.ImageUrl;
                        }
                    }

                    _context.Update(venue);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Venue updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venue.VenueId))
                        return NotFound();

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(venue);
        }

        // GET: Venues/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var venue = await _context.Venues
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null)
                return NotFound();

            return View(venue);
        }

        // POST: Venues/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Check that the venue exists.
            var venue = await _context.Venues
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null)
                return NotFound();

            // Check whether this venue has any booking.
            // Any booking counts, including cancelled bookings,
            // because the rubric requires venues linked to
            // bookings to be protected from deletion.
            bool hasBookings = await _context.Bookings
                .AnyAsync(b => b.VenueId == id);

            if (hasBookings)
            {
                // Display a clear validation message.
                TempData["ErrorMessage"] =
                    $"Cannot delete '{venue.VenueName}' because it has an existing booking.";

                // Return to the venue list.
                // The venue remains safely in the database.
                return RedirectToAction(nameof(Index));
            }

            // Only venues with no bookings can be deleted.
            _context.Venues.Remove(venue);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Venue deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Check whether a venue still exists.
        private bool VenueExists(int id)
        {
            return _context.Venues
                .Any(v => v.VenueId == id);
        }
    }
}