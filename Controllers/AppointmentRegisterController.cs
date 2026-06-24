using DCAS.Data;
using DCAS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DCAS.Controllers
{
    public class AppointmentRegisterController : Controller
    {
        private readonly DCASContext _context;

        public AppointmentRegisterController(DCASContext context)
        {
            _context = context;
        }
[HttpGet]
public IActionResult Index()
{
            // Set available days and times
            ViewBag.AvailableDays = _context.TodaySchedule
                .Where(s => s.EventDate.Date >= DateTime.Today)
                .GroupBy(s => s.EventDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvailableCount = g.Count(s => string.IsNullOrEmpty(s.PersonName) || s.PersonName.ToLower() == "available slots")
                })
                .Where(x => x.AvailableCount > 0)
                .Select(x => new SelectListItem
                {
                    Text = x.Date.ToString("MM/dd/yyyy") + " (" +
                           x.AvailableCount + " slot" +
                           (x.AvailableCount > 1 ? "s" : "") +
                           " available)",
                    Value = x.Date.ToString("yyyy-MM-dd")
                }).ToList();

     
            ViewBag.MedicalServices = _context.Services
                .Select(s => new SelectListItem
                {
                    Text = $"{s.MedicalNameService} - ₱{s.Price}",
                    Value = s.Id.ToString()
                })
                .ToList();

            ViewBag.AvailableTimes = new List<SelectListItem>();

            // Initialize model with default values
            var model = new PersonInfo
    {
        Date = DateTime.Today,
        BirthDay = DateTime.Today.AddYears(-30)
    };
    return View(model);
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PersonInfo personInfo)
        {
            // Re-populate AvailableDays
            ViewBag.AvailableDays = _context.TodaySchedule
                .Where(s => s.EventDate.Date >= DateTime.Today)
                .GroupBy(s => s.EventDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    AvailableCount = g.Count(s => string.IsNullOrEmpty(s.PersonName) || s.PersonName.ToLower() == "available slots")
                })
                .Where(x => x.AvailableCount > 0)
                .Select(x => new SelectListItem
                {
                    Text = x.Date.ToString("MM/dd/yyyy") + " (" +
                           x.AvailableCount + " slot" +
                           (x.AvailableCount > 1 ? "s" : "") +
                           " available)",
                    Value = x.Date.ToString("yyyy-MM-dd")
                }).ToList();

            ViewBag.AvailableTimes = new List<SelectListItem>();

            // Calculate Age
            if (personInfo.BirthDay.HasValue)
            {
                var birthDate = personInfo.BirthDay.Value;
                personInfo.Age = DateTime.Today.Year - birthDate.Year;
                if (DateTime.Today.DayOfYear < birthDate.DayOfYear)
                    personInfo.Age--;
            }
            else
            {
                ModelState.AddModelError("BirthDay", "Birth date is required.");
            }


            // Validate date/time
            if (string.IsNullOrEmpty(personInfo.AvailableDay) || string.IsNullOrEmpty(personInfo.AvailableTime))
            {
                ModelState.AddModelError("", "Please select a valid day and time.");
                return View(personInfo);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View(personInfo);
            }

            // Parse date and time properly
            if (DateTime.TryParseExact(personInfo.AvailableDay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date) && TimeSpan.TryParse(personInfo.AvailableTime, out var time))
            {
                // Check slot availability
                var existingSlot = _context.TodaySchedule
                    .FirstOrDefault(s => s.EventDate.Date == date.Date && s.EventTime == time);

                if (existingSlot != null && !string.Equals(existingSlot.PersonName, "Available Slots", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("AvailableTime",
                        $"The slot on {date:MM/dd/yyyy} at {date.Add(time):hh:mm tt} is already taken.");
                    return View(personInfo);
                }

                // Save PersonInfo first to get the ID
                if (personInfo.Id == 0)
                {
                    _context.PersonInfo.Add(personInfo);
                    await _context.SaveChangesAsync(); // Save to generate Id
                }
                else
                {
                    _context.PersonInfo.Update(personInfo);
                    await _context.SaveChangesAsync();
                }

                // Update or add the TodaySchedule slot
                if (existingSlot != null)
                {
                    existingSlot.PersonName = personInfo.Name;
                    _context.TodaySchedule.Update(existingSlot);
                }
                else
                {
                    _context.TodaySchedule.Add(new TodaySchedule
                    {
                        EventDate = date,
                        EventTime = time,
                        PersonName = personInfo.Name
                    });
                }

                await _context.SaveChangesAsync();

                string selectedService = "N/A";
                Services? service = null;

                if (int.TryParse(personInfo.MedicalServices, out int serviceId))
                {
                    service = await _context.Services.FirstOrDefaultAsync(s => s.Id == serviceId);
                    selectedService = service != null ? $"{service.MedicalNameService} - ${service.Price}" : "N/A";
                }
                else
                {
                    selectedService = "N/A";
                }

                // Add Payment linked to personInfo with the newly generated Id
                var payment = new Payment
                {
                    DoctorName = "DR. NINA RICCI ANTIPALA-RIVERA",
                    Cash = 0,
                    Status = "Unpaid",
                    PaymentMethod = "Cash",
                    AmountChanged = 0,
                    AmountPaid = 0,
                    PersonInfoId = personInfo.Id,
                    ServicesId = service?.Id ?? 0
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Appointment for {personInfo.Name} scheduled on {date:MM/dd/yyyy} at {date.Add(time):hh:mm tt}.";

                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Invalid date or time format.");
            return View(personInfo);
        }

        [HttpGet]
        public JsonResult GetAvailableTimes(string selectedDate)
        {
            var options = new List<SelectListItem>();

            if (DateTime.TryParse(selectedDate, out var date))
            {
                // Get all event times for the selected date
                var allSlots = _context.TodaySchedule
                    .Where(s => s.EventDate.Date == date.Date)
                    .Select(s => s.EventTime)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                // Get taken event times
                var takenSlots = _context.TodaySchedule
                    .Where(s => s.EventDate.Date == date.Date && !string.IsNullOrEmpty(s.PersonName) && s.PersonName.ToLower() != "available slots")
                    .Select(s => s.EventTime)
                    .ToList();

                // Filter available slots
                var availableSlots = allSlots.Where(ts => !takenSlots.Contains(ts)).ToList();

                // Populate the dropdown with available time slots
                options = availableSlots.Select(ts => new SelectListItem
                {
                    Text = date.Add(ts).ToString("hh:mm tt"),
                    Value = ts.ToString()
                }).ToList();
            }

            return Json(options.Select(o => new { o.Text, o.Value }));
        }
    }
}
