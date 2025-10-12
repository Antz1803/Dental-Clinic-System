using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DCAS.Data;
using DCAS.Models;

namespace DCAS.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly DCASContext _context;

        public PaymentsController(DCASContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Person)
                .Include(p => p.Service)
                .Where(p => p.Status == "Unpaid")
                .ToListAsync();

            return View(payments);
        }
        public async Task<IActionResult> PaymentHistory()
        {
            var paidPayments = await _context.Payments
                .Include(p => p.Person)
                .Include(p => p.Service)
                .Include(p => p.PaymentMedicine) 
                .Where(p => p.Status == "Paid")
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(paidPayments);
        }
        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.PaymentMedicine)
                .Include(p => p.Person)
                .Include(p => p.Service)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
              .Include(p => p.PaymentMedicine)
              .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            ViewData["PersonInfoId"] = new SelectList(_context.PersonInfo, "Id", "Name", payment.PersonInfoId);
            ViewData["ServicesId"] = new SelectList(_context.Services, "Id", "MedicalNameService", payment.ServicesId);
            ViewBag.Services = await _context.Services.ToListAsync();
            ViewBag.MedicineInventory = await _context.MedicineInventory.ToListAsync();

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment payment, int[] MedicineId)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }

            ModelState.Remove("Status");
            ModelState.Remove("AmountPaid");
            ModelState.Remove("AmountChanged");
            ModelState.Remove("PaymentDate");
            ModelState.Remove("Person");
            ModelState.Remove("Service");
            ModelState.Remove("PaymentMedicine");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing payment WITHOUT tracking
                    var existingPayment = await _context.Payments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PaymentId == id);

                    if (existingPayment == null)
                    {
                        return NotFound();
                    }

                    // Update payment properties
                    payment.PaymentDate = DateTime.Today;

                    // Calculate service price
                    var selectedService = await _context.Services
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == payment.ServicesId);
                    decimal servicePrice = selectedService?.Price ?? 0;

                    // Calculate medicine total
                    decimal totalMedicinePrice = 0;
                    if (MedicineId != null && MedicineId.Length > 0)
                    {
                        var medicines = await _context.MedicineInventory
                            .AsNoTracking()
                            .Where(m => MedicineId.Contains(m.Id))
                            .ToListAsync();
                        totalMedicinePrice = medicines.Sum(m => m.Price);
                    }

                    // Set calculated fields
                    decimal totalAmountDue = servicePrice + payment.DentistFee + totalMedicinePrice;
                    payment.AmountPaid = totalAmountDue;
                    payment.AmountChanged = payment.Cash - totalAmountDue;
                    payment.Status = "Paid";

                    // Update the payment
                    _context.Payments.Update(payment);
                    await _context.SaveChangesAsync();

                    // Now handle medicines separately
                    // First, delete old medicine records
                    var oldMedicines = await _context.PaymentMedicine
                        .Where(pm => pm.PaymentId == id)
                        .ToListAsync();

                    if (oldMedicines.Any())
                    {
                        _context.PaymentMedicine.RemoveRange(oldMedicines);
                        await _context.SaveChangesAsync();
                    }

                    // Then add new medicine records
                    if (MedicineId != null && MedicineId.Length > 0)
                    {
                        var medicines = await _context.MedicineInventory
                            .AsNoTracking()
                            .Where(m => MedicineId.Contains(m.Id))
                            .ToListAsync();

                        foreach (var medicine in medicines)
                        {
                            _context.PaymentMedicine.Add(new PaymentMedicine
                            {
                                PaymentId = id,
                                MedicineName = medicine.MedicineName,
                                Price = medicine.Price
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    var innerMsg = ex.InnerException?.Message ?? ex.Message;
                    System.Diagnostics.Debug.WriteLine($"ERROR: {innerMsg}");
                    ModelState.AddModelError("", $"Error saving: {innerMsg}");
                }
            }
            else
            {
                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Field: {error.Key}");
                    foreach (var err in error.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Error: {err.ErrorMessage}");
                    }
                }
            }

            ViewData["PersonInfoId"] = new SelectList(_context.PersonInfo, "Id", "Name", payment.PersonInfoId);
            ViewData["ServicesId"] = new SelectList(_context.Services, "Id", "MedicalNameService", payment.ServicesId);
            ViewBag.Services = await _context.Services.ToListAsync();
            ViewBag.MedicineInventory = await _context.MedicineInventory.ToListAsync();

            var reloadPayment = await _context.Payments
                .Include(p => p.PaymentMedicine)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            return View(reloadPayment ?? payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
              .Include(p => p.PaymentMedicine)
              .Include(p => p.Person)
              .Include(p => p.Service)
              .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }
    }
}