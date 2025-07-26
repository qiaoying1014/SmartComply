using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;
using System.Linq;

namespace SmartComply.Controllers
{
  [Authorize(Roles = "Admin")]
  public class ComplianceCategoryController : Controller
  {
    private readonly ApplicationDbContext _context;

    public ComplianceCategoryController(ApplicationDbContext context)
    {
      _context = context;
    }

    // Read
    public IActionResult Index(string statusFilter = null, string searchTerm = null)
    {
      var query = _context.ComplianceCategories.AsQueryable();

      // Filter by status
      if (!string.IsNullOrEmpty(statusFilter))
      {
        bool isEnabled = statusFilter.ToLower() == "enable";
        query = query.Where(c => c.CategoryIsEnabled == isEnabled);
      }

      // Filter by search term
      if (!string.IsNullOrEmpty(searchTerm))
      {
        string lowerSearchTerm = searchTerm.ToLower();
        query = query.Where(c =>
            c.CategoryName.ToLower().Contains(lowerSearchTerm) ||
            c.CategoryDescription.ToLower().Contains(lowerSearchTerm));
      }

      var categories = query.OrderBy(c => c.CategoryName).ToList();

      ViewBag.StatusFilter = statusFilter;
      ViewBag.SearchTerm = searchTerm;

      return View(categories);
    }

    // Add
    public IActionResult Add()
    {
      var model = new ComplianceCategoryViewModel
      {
        // Set the default value for CategoryIsEnabled to true (Active)
        CategoryIsEnabled = true
      };
      return View(model);
    }

    [HttpPost]
    public IActionResult Add(ComplianceCategoryViewModel model)
    {
      // Check if the CategoryName already exists (case-insensitive)
      if (!string.IsNullOrWhiteSpace(model.CategoryName))
      {
        if (_context.ComplianceCategories
            .Any(c => c.CategoryName.ToLower() == model.CategoryName.ToLower()))
        {
          ModelState.AddModelError("CategoryName", "A category with this name already exists.");
        }
      }

      if (ModelState.IsValid)
      {
        var entity = new ComplianceCategory
        {
          CategoryName = model.CategoryName,
          CategoryDescription = model.CategoryDescription,
          CategoryIsEnabled = model.CategoryIsEnabled
        };

        // Add the new category to the database
        _context.ComplianceCategories.Add(entity);
        _context.SaveChanges();

        // Log the add action
        var staffId = HttpContext.Session.GetInt32("StaffId");
        if (staffId.HasValue)
        {
          var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
          var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

          _context.ActivityLogs.Add(new ActivityLog
          {
            StaffId = staffId.Value,
            ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
            ActionType = ActivityLog.ActionTypeEnum.Add, // Use the enum for add action
            ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) added a new compliance category named '{entity.CategoryName}'."
          });
          _context.SaveChanges();
        }

        TempData["SuccessMessage"] = $"{model.CategoryName} added successfully.";
        return RedirectToAction(nameof(Index)); // Redirect after success
      }

      // If validation fails, return the model to the view to show error messages
      return View(model);
    }


    // Edit (GET)
    public IActionResult Edit(int id)
    {
      var entity = _context.ComplianceCategories.Find(id);
      if (entity == null) return NotFound();

      // Populate ViewBag with Active/Inactive options
      ViewBag.CategoryStatus = new SelectList(new List<SelectListItem>
    {
        new SelectListItem { Text = "Active", Value = "true", Selected = entity.CategoryIsEnabled },
        new SelectListItem { Text = "Inactive", Value = "false", Selected = !entity.CategoryIsEnabled }
    }, "Value", "Text");

      var model = new ComplianceCategoryViewModel
      {
        CategoryId = entity.CategoryId,
        CategoryName = entity.CategoryName,
        CategoryDescription = entity.CategoryDescription,
        CategoryIsEnabled = entity.CategoryIsEnabled // Ensure this value is passed to the view
      };

      return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, ComplianceCategoryViewModel model)
    {
      if (id != model.CategoryId) return BadRequest();

      if (!string.IsNullOrWhiteSpace(model.CategoryName))
      {
        // Check for duplicates, excluding the current record
        if (_context.ComplianceCategories
            .Any(c => c.CategoryName.ToLower() == model.CategoryName.ToLower() && c.CategoryId != id))
        {
          ModelState.AddModelError("CategoryName", "A category with this name already exists.");
        }
      }

      if (ModelState.IsValid)
      {
        var entity = _context.ComplianceCategories.Find(id);
        if (entity == null) return NotFound();

        // Update the category details
        entity.CategoryName = model.CategoryName;
        entity.CategoryDescription = model.CategoryDescription;
        entity.CategoryIsEnabled = model.CategoryIsEnabled;

        _context.SaveChanges();

        // Log the update action
        var staffId = HttpContext.Session.GetInt32("StaffId");
        if (staffId.HasValue)
        {
          var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
          var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

          _context.ActivityLogs.Add(new ActivityLog
          {
            StaffId = staffId.Value,
            ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
            ActionType = ActivityLog.ActionTypeEnum.Update, // Use the enum for update action
            ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the compliance category named '{entity.CategoryName}'."
          });
          _context.SaveChanges();
        }

        TempData["SuccessMessage"] = $"{model.CategoryName} updated successfully.";
        return RedirectToAction(nameof(Index)); // Redirect after success
      }

      // If validation fails, return the model to the view to show error messages
      return View(model);
    }

    // Toggle Status (Enable/Disable)
    public async Task<IActionResult> ToggleStatus(int id)
    {
      var category = await _context.ComplianceCategories.FindAsync(id);
      if (category == null)
      {
        return NotFound();
      }

      // Toggle active status
      category.CategoryIsEnabled = !category.CategoryIsEnabled;

      _context.ComplianceCategories.Update(category);
      await _context.SaveChangesAsync(); // Save the status change first

      // Log the status change action
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
        var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
          ActionType = ActivityLog.ActionTypeEnum.Update, // Use the enum for update action
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the compliance category named '{category.CategoryName}' status."
        });
        await _context.SaveChangesAsync(); // Save the log after updating the compliance category status
      }


      TempData["SuccessMessage"] = $"{category.CategoryName} is now " +
                                   (category.CategoryIsEnabled ? "enabled." : "disabled.");

      return RedirectToAction(nameof(Index));
    }
  }
}
