using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DCAS.Data;
using DCAS.Models;

namespace DCAS.Controllers
{
    public class TodaySchedulesController : Controller
    {
        private readonly DCASContext _context;

        public TodaySchedulesController(DCASContext context)
        {
            _context = context;
        }

        // GET: TodaySchedules
        public async Task<IActionResult> Index(int page = 0)
        {
            var schedules = await _context.TodaySchedule.ToListAsync();

            if (!schedules.Any())
            {
                // If no schedules, show empty grid with default time slots
                ViewBag.Dates = new List<DateTime>();
                ViewBag.Times = new List<TimeSpan>();
                ViewBag.ScheduleMap = new Dictionary<TimeSpan, Dictionary<DateTime, string>>();
                ViewBag.CurrentPage = 0;
                ViewBag.Message = "No schedules found. Create some entries to see the schedule grid.";
                return View();
            }

            // Get all dates and times from your existing logic
            var today = DateTime.Today;

            var allDates = schedules
                .Select(s => s.EventDate.Date)
                .Where(d => d >= today) 
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var times = schedules
                .Select(s => s.EventTime)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // Pagination logic for dates
            var pageSize = 5;
            var totalPages = (int)Math.Ceiling((double)allDates.Count / pageSize);

            // Validate page number
            if (page < 0) page = 0;
            if (page >= totalPages && totalPages > 0) page = totalPages - 1;

            // Get dates for current page
            var dates = allDates.Skip(page * pageSize).Take(pageSize).ToList();

            // Create the schedule map (only for current page dates)
            var scheduleMap = new Dictionary<TimeSpan, Dictionary<DateTime, string>>();
            foreach (var time in times)
            {
                scheduleMap[time] = new Dictionary<DateTime, string>();
                foreach (var date in dates)
                {
                    var schedule = schedules.FirstOrDefault(s =>
                        s.EventDate.Date == date && s.EventTime == time);
                    scheduleMap[time][date] = schedule?.PersonName ?? "Time Slots";
                }
            }

            // Set ViewBag properties
            ViewBag.Dates = dates; // Only current page dates
            ViewBag.AllDates = allDates; // All dates for summary calculations
            ViewBag.Times = times;
            ViewBag.ScheduleMap = scheduleMap;
            ViewBag.CurrentPage = page;
            ViewBag.Message = TempData["Message"];

            return View();
        }

        // GET: TodaySchedules/Create
        public IActionResult Create()
        {
            var model = new TodaySchedule
            {
                EventDate = DateTime.Today,
                EventTime = new TimeSpan(8, 0, 0), // 8:00 AM default
                PersonName = "Available Slots"
            };

            return View(model);
        }

        // POST: TodaySchedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventDate,EventTime,PersonName")] TodaySchedule todaySchedule)
        {
            if (ModelState.IsValid)
            {
                // Check if this time slot is already taken
                var existingSchedule = await _context.TodaySchedule
                    .FirstOrDefaultAsync(s => s.EventDate.Date == todaySchedule.EventDate.Date
                                           && s.EventTime == todaySchedule.EventTime);

                if (existingSchedule != null)
                {
                    ModelState.AddModelError("", "This Time/Date slot have already created. Please choose a different time.");
                    return View(todaySchedule);
                }

                _context.Add(todaySchedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(todaySchedule);
        }

        // Method to get available time slots
        public async Task<IActionResult> GetAvailableSlots(DateTime date)
        {
            var bookedTimes = await _context.TodaySchedule
                .Where(s => s.EventDate.Date == date.Date)
                .Select(s => s.EventTime)
                .ToListAsync();

            // Define standard business hours (8 AM to 6 PM, 30-minute intervals)
            var allTimeSlots = new List<TimeSpan>();
            for (int hour = 8; hour < 18; hour++)
            {
                allTimeSlots.Add(new TimeSpan(hour, 0, 0));
                allTimeSlots.Add(new TimeSpan(hour, 30, 0));
            }

            var availableSlots = allTimeSlots.Except(bookedTimes).ToList();

            return Json(availableSlots.Select(t => new {
                Value = t.ToString(@"hh\:mm"),
                Text = DateTime.Today.Add(t).ToString("h:mm tt")
            }));
        }
    }
}