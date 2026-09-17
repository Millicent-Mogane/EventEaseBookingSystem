using EventEaseBookingSystem.Data;
using EventEaseBookingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using PagedList.Core;

namespace EventEaseBookingSystem.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ------------------- SUMMARY -------------------
        public IActionResult Summary(string searchTerm, string sortOrder, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.EventSortParm = string.IsNullOrEmpty(sortOrder) ? "event_desc" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

            var query = _context.BookingSummaryView.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(b =>
                    b.EventName.Contains(searchTerm) ||
                    b.VenueName.Contains(searchTerm) ||
                    b.Status.Contains(searchTerm));
            }

            switch (sortOrder)
            {
                case "event_desc":
                    query = query.OrderByDescending(b => b.EventName);
                    break;
                case "Date":
                    query = query.OrderBy(b => b.StartDate);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(b => b.StartDate);
                    break;
                default:
                    query = query.OrderBy(b => b.EventName);
                    break;
            }

            int pageSize = 10;
            int pageNumber = page ?? 1;
            var pagedList = new PagedList<BookingSummaryView>(query, pageNumber, pageSize);
            return View(pagedList);
        }

        // ------------------- INDEX -------------------
        public async Task<IActionResult> Index(
            string searchString,
            DateTime? startDate,
            DateTime? endDate,
            string statusFilter,
            string sortOrder,
            bool exactMatch = false,
            int page = 1)
        {
            const int pageSize = 10;

            if (page < 1) page = 1;

            ViewBag.DateSortParam = sortOrder == "date_asc" ? "date_desc" : "date_asc";
            ViewBag.StatusSortParam = sortOrder == "status_asc" ? "status_desc" : "status_asc";
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentEndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentStatus = statusFilter;
            ViewBag.CurrentExactMatch = exactMatch;

            var bookings = _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var lowerSearch = searchString.ToLower();
                bookings = bookings.Where(b =>
                    b.BookingId.ToString().Contains(lowerSearch) ||
                    (b.Event != null && b.Event.EventName.ToLower().Contains(lowerSearch)) ||
                    (b.Venue != null && b.Venue.VenueName.ToLower().Contains(lowerSearch)));
            }

            if (startDate.HasValue)
            {
                if (exactMatch)
                {
                    bookings = bookings.Where(b =>
                        b.BookingDate.Date == startDate.Value.Date ||
                        (b.Event != null && b.Event.StartDate.Date == startDate.Value.Date));
                }
                else
                {
                    bookings = bookings.Where(b =>
                        b.BookingDate.Date >= startDate.Value.Date ||
                        (b.Event != null && b.Event.StartDate.Date >= startDate.Value.Date));
                }
            }

            if (endDate.HasValue)
            {
                bookings = bookings.Where(b =>
                    b.BookingDate.Date <= endDate.Value.Date ||
                    (b.Event != null && b.Event.EndDate.Date <= endDate.Value.Date));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                bookings = bookings.Where(b => b.Status == statusFilter);
            }

            ViewBag.TotalBookings = await bookings.CountAsync();
            ViewBag.ConfirmedBookings = await bookings.CountAsync(b => b.Status == "Confirmed");
            ViewBag.PendingBookings = await bookings.CountAsync(b => b.Status == "Pending");
            ViewBag.CancelledBookings = await bookings.CountAsync(b => b.Status == "Cancelled");

            switch (sortOrder)
            {
                case "date_desc":
                    bookings = bookings.OrderByDescending(b => b.BookingDate);
                    break;
                case "date_asc":
                    bookings = bookings.OrderBy(b => b.BookingDate);
                    break;
                case "status_desc":
                    bookings = bookings.OrderByDescending(b => b.Status);
                    break;
                case "status_asc":
                    bookings = bookings.OrderBy(b => b.Status);
                    break;
                default:
                    bookings = bookings.OrderBy(b => b.BookingDate);
                    break;
            }

            var totalPages = (int)Math.Ceiling(ViewBag.TotalBookings / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var resultList = await bookings
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!resultList.Any() && ViewBag.TotalBookings == 0)
            {
                ViewBag.Message = "No bookings found for the selected search criteria.";
            }

            return View(resultList);
        }

        // ------------------- CREATE -------------------
        public IActionResult Create()
        {
            ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName");
            ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingId,EventId,VenueId,Status,BookingDate")] Booking booking)
        {
            // Automatically set the booking date and time to the current date and time.
            booking.BookingDate = DateTime.Now;

            // Check if this venue already has a booking on the same date.
            var existingBooking = await _context.Bookings
                .FirstOrDefaultAsync(b =>
                    b.VenueId == booking.VenueId &&
                    b.BookingDate.Date == booking.BookingDate.Date);

            // Stop the booking if the venue is already booked on this date.
            if (existingBooking != null)
            {
                ModelState.AddModelError(
                    "",
                    "This venue is already booked at the selected date and time.");
            }

            // Only save the booking if there are no validation errors.
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Booking created successfully.";

                return RedirectToAction(nameof(Index));
            }

            // Rebuild the dropdown lists if validation fails.
            ViewData["EventId"] = new SelectList(
                _context.Events,
                "EventId",
                "EventName",
                booking.EventId);

            ViewData["VenueId"] = new SelectList(
                _context.Venues,
                "VenueId",
                "VenueName",
                booking.VenueId);

            return View(booking);
        }


        // ------------------- DETAILS -------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // ------------------- EDIT -------------------
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName", booking.EventId);
            ViewData["VenueId"] = new SelectList(_context.Venues, "VenueId", "VenueName", booking.VenueId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,EventId,VenueId,Status,BookingDate")] Booking booking)
        {
            if (id != booking.BookingId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Booking updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Bookings.Any(e => e.BookingId == booking.BookingId))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(booking);
        }

        // ------------------- DELETE -------------------

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            // Restriction: prevent deletion if booking is linked to an event or venue
            if (booking.EventId != 0 || booking.VenueId != 0)
            {
                TempData["ErrorMessage"] = "Cannot delete booking because it is linked to an event or venue.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Booking deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unable to delete booking: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


    }
}