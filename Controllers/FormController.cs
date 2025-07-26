using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;

namespace SmartComply.Controllers
{
  [Authorize(Roles = "Admin")]
  public class FormController : Controller
  {
    private readonly ApplicationDbContext _context;

    public FormController(ApplicationDbContext context)
    {
      _context = context;
    }

    // See Form with status
    // Old
    //public IActionResult Index(string status = "Published")
    //{
    //  var forms = _context.Forms
    //      .AsNoTracking()
    //      .Include(f => f.Category)
    //      .Include(f => f.FormElements)
    //      .Where(f => f.Status == status)
    //      .OrderByDescending(f => f.CreatedDate)
    //      .ToList();

    //  ViewBag.StatusFilter = status;
    //  return View(forms);
    //}

    // New
    public IActionResult Index(string statusFilter = null, string searchTerm = null)
    {
      var query = _context.Forms
          .AsNoTracking()
          .Include(f => f.Category)
          .Include(f => f.FormElements)
          .OrderByDescending(f => f.CreatedDate)
          .AsQueryable();

      // Apply filter by status if provided
      if (!string.IsNullOrEmpty(statusFilter))
      {
        query = query.Where(f => f.Status.ToLower() == statusFilter.ToLower());
      }

      // Apply search by form name or category name
      if (!string.IsNullOrEmpty(searchTerm))
      {
        string lowerSearchTerm = searchTerm.ToLower();
        query = query.Where(f =>
            f.Category.CategoryName.ToLower().Contains(lowerSearchTerm));
            // f.FormElements.Any(fe => fe.Label.ToLower().Contains(lowerSearchTerm)));
      }

      var forms = query.OrderByDescending(f => f.CreatedDate).ToList();

      // Pass the filter and search term to ViewBag for persistence
      ViewBag.StatusFilter = statusFilter;
      ViewBag.SearchTerm = searchTerm;

      return View(forms);
    }


    public IActionResult Add()
    {
      var model = new FormBuilderViewModel
      {
        Categories = _context.ComplianceCategories
              .Where(c => c.CategoryIsEnabled)
              .Select(c => new SelectListItem
              {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
              })
              .ToList(),
        FormElements = new List<FormElementViewModel>() // initialize empty list if needed
      };

      return View(model);
    }

    // Create new form
    [HttpPost]
    public async Task<IActionResult> Add(FormBuilderViewModel model)
    {
      model.FormElements = model.FormElements?
          .Where(e => !string.IsNullOrWhiteSpace(e.Label))
          .Select(e => new FormElementViewModel
          {
            Label = e.Label.Trim(),
            ElementType = e.ElementType?.Trim(),
            Placeholder = string.IsNullOrWhiteSpace(e.Placeholder) ? null : e.Placeholder.Trim(),
            Options = string.IsNullOrWhiteSpace(e.Options) ? null : e.Options.Trim(),
            IsRequired = e.IsRequired,
            Order = e.Order
          }).ToList();

      if (model.FormElements == null || !model.FormElements.Any())
      {
        ModelState.AddModelError("", "You must add at least one form element.");
        return View(model);
      }

      // Debug log
      foreach (var element in model.FormElements)
      {
        Console.WriteLine($"Creating Element - Label: {element.Label}, ElementType: {element.ElementType}");
      }

      var form = new Form
      {
        FormName = model.FormName,
        CategoryId = model.CategoryId,
        CreatedDate = DateTime.UtcNow,
        Status = "Editing",
        FormElements = model.FormElements.Select(e => new FormElement
        {
          Label = e.Label,
          ElementType = e.ElementType,
          Placeholder = e.Placeholder,
          IsRequired = e.IsRequired,
          Options = e.Options,
          Order = e.Order
        }).ToList()
      };

      _context.Forms.Add(form);
      await _context.SaveChangesAsync();

      // Log the add action for the new form
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
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) added a new form named '{model.FormName}'."
        });

        await _context.SaveChangesAsync(); // Save the activity log
      }

      return RedirectToAction("Preview", new { id = form.FormId });
    }

    // Edit form - GET
    public IActionResult Edit(int id)
    {
      var form = _context.Forms
          .Include(f => f.FormElements)
          .FirstOrDefault(f => f.FormId == id);

      if (form == null)
        return NotFound();

      var model = new FormBuilderViewModel
      {
        FormId = form.FormId,
        CategoryId = form.CategoryId,
        FormName = form.FormName,
        FormElements = form.FormElements
              .OrderBy(e => e.Order)
              .Select(e => new FormElementViewModel
              {
                Label = e.Label,
                ElementType = e.ElementType,
                Placeholder = e.Placeholder,
                Options = e.Options,
                IsRequired = e.IsRequired,
                Order = e.Order
              }).ToList(),

        Categories = _context.ComplianceCategories
              .Where(c => c.CategoryIsEnabled)
              .Select(c => new SelectListItem
              {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
              }).ToList()
      };

      return View(model);
    }

    // Edit form - POST
    [HttpPost]
    public async Task<IActionResult> Edit(FormBuilderViewModel model, string action)
    {
      if (model.FormElements == null || !model.FormElements.Any())
      {
        ModelState.AddModelError("", "You must add at least one form element.");
        return View(model);
      }

      var form = await _context.Forms
          .Include(f => f.FormElements)
          .FirstOrDefaultAsync(f => f.FormId == model.FormId);

      if (form == null)
        return NotFound();

      // Debug log
      foreach (var element in model.FormElements)
      {
        Console.WriteLine($"Editing Element - Label: {element.Label}, ElementType: {element.ElementType}");
      }

      // Remove existing elements
      _context.FormElements.RemoveRange(form.FormElements);

      // Update form data
      form.CategoryId = model.CategoryId;
      form.FormName = model.FormName;
      form.Status = action == "publish" ? "Published" : "Revised";
      form.FormElements = model.FormElements.Select(e => new FormElement
      {
        Label = e.Label,
        ElementType = e.ElementType,
        Placeholder = e.Placeholder,
        Options = e.Options,
        IsRequired = e.IsRequired,
        Order = e.Order
      }).ToList();

      await _context.SaveChangesAsync();

      // Log the update action for the form
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
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the form named '{model.FormName}'."
        });

        await _context.SaveChangesAsync(); // Save the activity log
      }

      TempData["Message"] = action == "publish" ? "Form published successfully!" : "Changes saved!";
      return RedirectToAction("Index");
    }

    // Preview Form
    public IActionResult Preview(int id)
    {
      var form = _context.Forms
          .AsNoTracking()
          .Include(f => f.FormElements)
          .Include(f => f.Category)
          .FirstOrDefault(f => f.FormId == id);

      if (form == null)
        return NotFound();

      form.FormElements = form.FormElements.OrderBy(e => e.Order).ToList();

      return View(form);
    }

    //NEW ADDED to hide form
    [HttpPost]
    public async Task<IActionResult> ToggleVisibility(int id)
    {
      var form = await _context.Forms.FindAsync(id);
      if (form == null)
      {
        return NotFound();
      }

      if (form.Status == "Published")
      {
        form.Status = "Hidden";
      }
      else if (form.Status == "Hidden")
      {
        form.Status = "Published";
      }

      _context.Forms.Update(form);
      await _context.SaveChangesAsync();

      // Log the status update action for the form
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
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the '{form.FormName}' status."
        });

        await _context.SaveChangesAsync(); // Save the activity log
      }

      TempData["SuccessMessage"] = "Form visibility updated successfully!";
      return RedirectToAction("Index");
    }

  }
}
