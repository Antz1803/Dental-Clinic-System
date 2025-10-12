using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DCAS.Data;
using DCAS.Models;

namespace DCAS.Controllers
{
    public class MedicineInventoriesController : Controller
    {
        private readonly DCASContext _context;

        public MedicineInventoriesController(DCASContext context)
        {
            _context = context;
        }

        // GET: MedicineInventories
        public async Task<IActionResult> Index()
        {
            return View(await _context.MedicineInventory.ToListAsync());
        }

        public async Task<IActionResult> GetImage(int id)
        {
            var medicineInventory = await _context.MedicineInventory
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicineInventory == null || medicineInventory.Image == null || medicineInventory.Image.Length == 0)
            {
                // Return a default "no image" or 404
                return NotFound();
            }

            // Detect content type from the image bytes
            string contentType = GetImageContentType(medicineInventory.Image);

            return File(medicineInventory.Image, contentType);
        }

        // Helper method to detect image content type
        private string GetImageContentType(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length < 4)
                return "image/jpeg"; // default

            // Check the first few bytes to determine image type
            // JPEG
            if (imageBytes.Length >= 2 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                return "image/jpeg";

            // PNG
            if (imageBytes.Length >= 8 &&
                imageBytes[0] == 0x89 && imageBytes[1] == 0x50 &&
                imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                return "image/png";

            // GIF
            if (imageBytes.Length >= 6 &&
                imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
                return "image/gif";

            // WebP
            if (imageBytes.Length >= 12 &&
                imageBytes[8] == 0x57 && imageBytes[9] == 0x45 &&
                imageBytes[10] == 0x42 && imageBytes[11] == 0x50)
                return "image/webp";

            // Default to JPEG
            return "image/jpeg";
        }

        // GET: MedicineInventories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MedicineInventories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,MedicineName,Price,Quantity,Miligram,Description,ExpiryDate")] MedicineInventory medicineInventory, IFormFile Image)
        {
            // Debug: Log all received data
            System.Diagnostics.Debug.WriteLine($"Received Data:");
            System.Diagnostics.Debug.WriteLine($"MedicineName: {medicineInventory.MedicineName}");
            System.Diagnostics.Debug.WriteLine($"Price: {medicineInventory.Price}");
            System.Diagnostics.Debug.WriteLine($"Quantity: {medicineInventory.Quantity}");
            System.Diagnostics.Debug.WriteLine($"Miligram: {medicineInventory.Miligram}");
            System.Diagnostics.Debug.WriteLine($"Description: {medicineInventory.Description}");
            System.Diagnostics.Debug.WriteLine($"ExpiryDate: {medicineInventory.ExpiryDate}");
            System.Diagnostics.Debug.WriteLine($"Image: {(Image != null ? $"File size: {Image.Length} bytes" : "No image")}");

            // Debug: Check ModelState validity
            System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            // Debug: List all validation errors
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("ModelState Errors:");
                foreach (var modelError in ModelState)
                {
                    var key = modelError.Key;
                    var errors = modelError.Value.Errors;
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {key}: {error.ErrorMessage}");
                    }
                }
            }

            // Check if image is required (based on your current logic)
            if (Image == null || Image.Length == 0)
            {
                ModelState.AddModelError("Image", "The Image field is required.");
                System.Diagnostics.Debug.WriteLine("Image validation failed: No image provided");
                return View(medicineInventory);
            }

            // Only proceed if ModelState is valid
            if (ModelState.IsValid)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Starting database save operation...");

                    // Convert the image to a byte array
                    using (var memoryStream = new MemoryStream())
                    {
                        await Image.CopyToAsync(memoryStream);
                        medicineInventory.Image = memoryStream.ToArray();
                        System.Diagnostics.Debug.WriteLine($"Image converted to byte array: {medicineInventory.Image.Length} bytes");
                    }

                    // Add to context
                    _context.Add(medicineInventory);
                    System.Diagnostics.Debug.WriteLine("Added to context");

                    // Save changes
                    var result = await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"SaveChangesAsync result: {result} rows affected");

                    TempData["SuccessMessage"] = "Medicine inventory created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception occurred: {ex}");
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                    ModelState.AddModelError("", $"An unexpected error occurred while saving the data: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ModelState is invalid, returning to view");
            }

            return View(medicineInventory);
        }


        // Details for modal
        public async Task<IActionResult> GetDetails(int id)
        {
            var medicine = await _context.MedicineInventory.FindAsync(id);
            if (medicine == null) return NotFound();

            var html = $@"
                <div class='row'>
                    <div class='col-md-6 text-center'>
                        {(medicine.Image != null && medicine.Image.Length > 0
                            ? $"<img src='{Url.Action("GetImage", "MedicineInventories", new { id = medicine.Id })}' class='img-fluid rounded shadow-sm mb-3' />"
                            : "<i class='fas fa-image fa-5x text-muted'></i><p class='text-muted'>No image available</p>")}
                    </div>
                    <div class='col-md-6'>
                        <p><strong>💊 Name:</strong> {medicine.MedicineName}</p>   
                        <p><strong>📅 Expiry:</strong> {medicine.ExpiryDate:MMM dd, yyyy}</p>
                        <p><strong>📝 Description:</strong> {medicine.Description}</p>
                    </div>
                </div>";
            return Content(html, "text/html");
        }

        // Edit form for modal
        public async Task<IActionResult> GetEditForm(int id)
        {
            var medicine = await _context.MedicineInventory.FindAsync(id);
            if (medicine == null) return NotFound();

            var html = $@"
<form id='editForm' enctype='multipart/form-data'>
    <input type='hidden' name='Id' value='{medicine.Id}' />

    <div class='mb-3 text-center'>
        {(medicine.Image != null && medicine.Image.Length > 0
                ? $"<img src='{Url.Action("GetImage", "MedicineInventories", new { id = medicine.Id })}' class='img-fluid rounded mb-2' style='max-height:200px;' />"
                : "<div class='text-muted mb-2'><i class='fas fa-image fa-3x'></i><p>No image</p></div>")}
        <input type='file' class='form-control' name='Image' accept='image/*' />
    </div>

    <div class='mb-3'>
        <label class='form-label'>Medicine Name</label>
        <input type='text' class='form-control' name='MedicineName' value='{medicine.MedicineName}' />
    </div>
    <div class='mb-3'>
        <label class='form-label'>Price</label>
        <input type='number' step='0.01' class='form-control' name='Price' value='{medicine.Price}' />
    </div>
    <div class='mb-3'>
        <label class='form-label'>Quantity</label>
        <input type='number' class='form-control' name='Quantity' value='{medicine.Quantity}' />
    </div>
    <div class='mb-3'>
        <label class='form-label'>Strength</label>
        <input type='text' class='form-control' name='Miligram' value='{medicine.Miligram}' />
    </div>
    <div class='mb-3'>
        <label class='form-label'>Description</label>
        <textarea class='form-control' name='Description'>{medicine.Description}</textarea>
    </div>
    <div class='mb-3'>
        <label class='form-label'>Expiry Date</label>
        <input type='date' class='form-control' name='ExpiryDate' value='{medicine.ExpiryDate:yyyy-MM-dd}' />
    </div>

    <button type='submit' class='btn btn-primary'>
        <i class='fas fa-save'></i> Save Changes
    </button>
</form>";

            return Content(html, "text/html");
        }

        // Save edits from modal
        [HttpPost]
        public async Task<IActionResult> EditModal(MedicineInventory medicineInventory, IFormFile Image)
        {
            try
            {
                var existing = await _context.MedicineInventory.FindAsync(medicineInventory.Id);
                if (existing == null)
                    return Json(new { success = false });

                existing.MedicineName = medicineInventory.MedicineName;
                existing.Price = medicineInventory.Price;
                existing.Quantity = medicineInventory.Quantity;
                existing.Miligram = medicineInventory.Miligram;
                existing.Description = medicineInventory.Description;
                existing.ExpiryDate = medicineInventory.ExpiryDate;

                if (Image != null && Image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await Image.CopyToAsync(memoryStream);
                        existing.Image = memoryStream.ToArray();
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // Delete with confirm
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicineInventory = await _context.MedicineInventory.FindAsync(id);
            if (medicineInventory != null)
            {
                _context.MedicineInventory.Remove(medicineInventory);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
