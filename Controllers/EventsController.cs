using Azure.Storage.Blobs;
using EventEaseBookingSystem.Data;
using EventEaseBookingSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EventEaseBookingSystem.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;

        // Azure Blob Storage container for event images.
        private const string EventImagesContainer = "eventimages";

        // Maximum event image size: 5 MB.
        private const long MaxImageSize = 5 * 1024 * 1024;

        // Allowed image file types.
        private static readonly string[] AllowedImageTypes =
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        };

        public EventsController(
            ApplicationDbContext context,
            BlobServiceClient blobServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
        }

        // GET: Events
        public async Task<IActionResult> Index(
            string searchString,
            DateTime? startDate,
            DateTime? endDate,
            bool exactMatch = false)
        {
            var events = _context.Events
                .Include(e => e.Venue)
                .AsQueryable();

            // Search by event name, description, or venue name.
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                events = events.Where(e =>
                    e.EventName.Contains(searchString) ||
                    e.Description.Contains(searchString) ||
                    (e.Venue != null &&
                     e.Venue.VenueName.Contains(searchString)));
            }

            // Filter events by start date.
            if (startDate.HasValue)
            {
                events = exactMatch
                    ? events.Where(e =>
                        e.StartDate.Date == startDate.Value.Date)
                    : events.Where(e =>
                        e.StartDate.Date >= startDate.Value.Date);
            }

            // Filter events by end date.
            if (endDate.HasValue)
            {
                events = events.Where(e =>
                    e.EndDate.Date <= endDate.Value.Date);
            }

            var result = await events.ToListAsync();

            if (!result.Any())
            {
                ViewBag.Message =
                    "No events found for the selected criteria.";
            }

            return View(result);
        }

        // GET: Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // GET: Events/Create
        public IActionResult Create()
        {
            ViewData["VenueId"] = new SelectList(
                _context.Venues,
                "VenueId",
                "VenueName");

            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Event @event,
            IFormFile? imageFile)
        {
            // Make sure the event end date is not before
            // the event start date.
            if (@event.EndDate < @event.StartDate)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "The event end date cannot be before the event start date.");
            }

            // Make sure the selected venue actually exists.
            bool venueExists = await _context.Venues
                .AnyAsync(v => v.VenueId == @event.VenueId);

            if (!venueExists)
            {
                ModelState.AddModelError(
                    "VenueId",
                    "Please select a valid venue.");
            }

            // Validate the uploaded image before saving the event.
            if (imageFile != null)
            {
                ValidateImage(imageFile, ModelState);
            }
            else
            {
                // An event can still be created without an image.
                // This keeps image upload optional while supporting Azure storage.
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Upload the event image to Azure Blob Storage
                    // if the user selected an image.
                    if (imageFile != null)
                    {
                        @event.ImageUrl = await UploadEventImageAsync(imageFile);
                    }

                    _context.Events.Add(@event);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Event created successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to save the event. Please check the information and try again.");
                }
                catch (Exception)
                {
                    ModelState.AddModelError(
                        "",
                        "An unexpected error occurred while creating the event. Please try again.");
                }
            }

            // Reload the venue dropdown if validation fails.
            ViewData["VenueId"] = new SelectList(
                _context.Venues,
                "VenueId",
                "VenueName",
                @event.VenueId);

            return View(@event);
        }

        // GET: Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null)
            {
                return NotFound();
            }

            ViewBag.VenueId = new SelectList(
                _context.Venues,
                "VenueId",
                "VenueName",
                @event.VenueId);

            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Event @event,
            IFormFile? imageFile)
        {
            if (id != @event.EventId)
            {
                return NotFound();
            }

            // Make sure the event end date is not before
            // the event start date.
            if (@event.EndDate < @event.StartDate)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "The event end date cannot be before the event start date.");
            }

            // Make sure the selected venue exists.
            bool venueExists = await _context.Venues
                .AnyAsync(v => v.VenueId == @event.VenueId);

            if (!venueExists)
            {
                ModelState.AddModelError(
                    "VenueId",
                    "Please select a valid venue.");
            }

            // Check whether this event already has bookings.
            var existingBookings = await _context.Bookings
                .Where(b => b.EventId == @event.EventId)
                .ToListAsync();

            // If the event already has bookings, changing its
            // dates could create a conflict with another booking.
            if (ModelState.IsValid && existingBookings.Any())
            {
                foreach (var existingBooking in existingBookings)
                {
                    // Cancelled bookings do not block the venue.
                    if (existingBooking.Status == "Cancelled")
                    {
                        continue;
                    }

                    bool conflictingBooking = await _context.Bookings
                        .Include(b => b.Event)
                        .AnyAsync(b =>
                            b.BookingId != existingBooking.BookingId &&
                            b.VenueId == existingBooking.VenueId &&
                            b.Status != "Cancelled" &&
                            b.Event != null &&
                            @event.StartDate.Date <= b.Event.EndDate.Date &&
                            @event.EndDate.Date >= b.Event.StartDate.Date);

                    if (conflictingBooking)
                    {
                        ModelState.AddModelError(
                            "",
                            "These event dates cannot be changed because the event already has a booking that would conflict with another booking for the same venue.");

                        break;
                    }
                }
            }

            // Validate a replacement image if one was selected.
            if (imageFile != null)
            {
                ValidateImage(imageFile, ModelState);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing database record.
                    var existingEvent = await _context.Events
                        .FirstOrDefaultAsync(e => e.EventId == id);

                    if (existingEvent == null)
                    {
                        return NotFound();
                    }

                    // Update the normal event information.
                    existingEvent.EventName = @event.EventName;
                    existingEvent.StartDate = @event.StartDate;
                    existingEvent.EndDate = @event.EndDate;
                    existingEvent.Description = @event.Description;
                    existingEvent.VenueId = @event.VenueId;

                    // Only replace the image when a new image was uploaded.
                    if (imageFile != null)
                    {
                        string? oldImageUrl = existingEvent.ImageUrl;

                        existingEvent.ImageUrl =
                            await UploadEventImageAsync(imageFile);

                        // Delete the previous Azure image after
                        // the new image has been uploaded successfully.
                        if (!string.IsNullOrWhiteSpace(oldImageUrl))
                        {
                            await DeleteEventImageAsync(oldImageUrl);
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Event updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.EventId))
                    {
                        return NotFound();
                    }

                    ModelState.AddModelError(
                        "",
                        "The event was changed by another user. Please reload the page and try again.");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to save the event. Please check the information and try again.");
                }
                catch (Exception)
                {
                    ModelState.AddModelError(
                        "",
                        "An unexpected error occurred while updating the event. Please try again.");
                }
            }

            // Reload the venue dropdown if validation fails.
            ViewBag.VenueId = new SelectList(
                _context.Venues,
                "VenueId",
                "VenueName",
                @event.VenueId);

            return View(@event);
        }

        // GET: Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Find the event and its bookings.
            var @event = await _context.Events
                .Include(e => e.Bookings)
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (@event == null)
            {
                return NotFound();
            }

            // IMPORTANT:
            // An event that has ANY booking cannot be deleted.
            // This protects the relationship between the Event
            // and Booking records.
            bool hasBookings = await _context.Bookings
                .AnyAsync(b => b.EventId == id);

            if (hasBookings)
            {
                // Store the error message so that the Events
                // Index page can display it to the user.
                TempData["ErrorMessage"] =
                    $"Cannot delete '{@event.EventName}' because it has an existing booking.";

                // Return to the Events list.
                // The event remains safely in the database.
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Delete the event image from Azure Blob Storage
                // before deleting the event from the database.
                if (!string.IsNullOrWhiteSpace(@event.ImageUrl))
                {
                    await DeleteEventImageAsync(@event.ImageUrl);
                }

                // Only events without bookings can be deleted.
                _context.Events.Remove(@event);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Event deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This event cannot be deleted because it is still linked to other records.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    "An unexpected error occurred while deleting the event. Please try again.";

                return RedirectToAction(nameof(Index));
            }
        }

        // Upload an event image to the eventimages Azure Blob container.
        private async Task<string> UploadEventImageAsync(IFormFile imageFile)
        {
            var containerClient =
                _blobServiceClient.GetBlobContainerClient(EventImagesContainer);

            // The container is expected to be created manually in Azure.
            // This call does not create the container automatically.
            await containerClient.ExistsAsync();

            // Generate a unique file name so uploaded images do not overwrite
            // other event images.
            string extension =
                Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            string blobName =
                $"{Guid.NewGuid()}{extension}";

            var blobClient =
                containerClient.GetBlobClient(blobName);

            using var stream = imageFile.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                overwrite: false);

            return blobClient.Uri.ToString();
        }

        // Delete an event image from Azure Blob Storage.
        private async Task DeleteEventImageAsync(string imageUrl)
        {
            try
            {
                // Only delete images that belong to our eventimages container.
                if (!imageUrl.Contains($"/{EventImagesContainer}/"))
                {
                    return;
                }

                Uri imageUri = new Uri(imageUrl);

                string[] segments =
                    imageUri.AbsolutePath
                        .Trim('/')
                        .Split('/');

                // Expected format:
                // storage-account/eventimages/file-name.png
                if (segments.Length < 2)
                {
                    return;
                }

                string blobName =
                    Uri.UnescapeDataString(
                        string.Join("/", segments.Skip(1)));

                var containerClient =
                    _blobServiceClient.GetBlobContainerClient(
                        EventImagesContainer);

                var blobClient =
                    containerClient.GetBlobClient(blobName);

                await blobClient.DeleteIfExistsAsync();
            }
            catch
            {
                // If the old image cannot be deleted, do not prevent
                // the event database operation from completing.
            }
        }

        // Validate the uploaded event image.
        private static void ValidateImage(
            IFormFile imageFile,
            ModelStateDictionary modelState)
        {
            if (imageFile.Length == 0)
            {
                modelState.AddModelError(
                    "imageFile",
                    "Please select a valid image.");
                return;
            }

            if (imageFile.Length > MaxImageSize)
            {
                modelState.AddModelError(
                    "imageFile",
                    "The image must be smaller than 5 MB.");
            }

            if (!AllowedImageTypes.Contains(
                    imageFile.ContentType.ToLowerInvariant()))
            {
                modelState.AddModelError(
                    "imageFile",
                    "Only JPG, PNG, GIF, and WEBP images are allowed.");
            }
        }

        // Check whether an event exists in the database.
        private bool EventExists(int id)
        {
            return _context.Events
                .Any(e => e.EventId == id);
        }
    }
}