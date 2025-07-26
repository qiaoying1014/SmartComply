using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

public class StaffController : Controller
{
  private readonly ApplicationDbContext _context;
  private readonly IPasswordHasher<Staff> _passwordHasher;

  public StaffController(ApplicationDbContext context, IPasswordHasher<Staff> passwordHasher)
  {
    _context = context;
    _passwordHasher = passwordHasher;
  }

  // GET: /Staff/Index
  public IActionResult Index(string statusFilter = null, string searchTerm = null)
  {
    var query = _context.Staffs
        .Include(s => s.StaffBranch)
        .AsQueryable();

    // Filter by status
    if (!string.IsNullOrEmpty(statusFilter))
    {
      bool isActive = statusFilter.ToLower() == "active";
      query = query.Where(s => s.StaffIsActive == isActive);
    }

    // Filter by search term (Name or Email)
    if (!string.IsNullOrEmpty(searchTerm))
    {
      string lowerSearchTerm = searchTerm.ToLower();
      query = query.Where(s =>
          s.StaffName.ToLower().Contains(lowerSearchTerm) ||
          s.StaffEmail.ToLower().Contains(lowerSearchTerm));
    }

    query = query.OrderBy(s =>
        s.StaffRole == "Admin" ? 0 :
        s.StaffRole == "Manager" ? 1 :
        s.StaffRole == "User" ? 2 : 3)
      .ThenBy(s => s.StaffName);

    var staffList = query.ToList();
    ViewBag.StatusFilter = statusFilter;
    ViewBag.SearchTerm = searchTerm;

    return View(staffList);
  }

  // GET: /Staff/Add
  public IActionResult Add()
  {
    var activeBranches = _context.Branches
        .Where(b => b.BranchIsActive)
        .ToList();
    ViewBag.Branches = new SelectList(activeBranches, "BranchId", "BranchName");
    return View(new StaffViewModel { StaffRole = "User" });
  }

  // POST: /Staff/Add
  [HttpPost]
  [ValidateAntiForgeryToken]
  public IActionResult Add(StaffViewModel model)
  {
    // Check if the email already exists
    if (!string.IsNullOrWhiteSpace(model.StaffEmail))
    {
      if (_context.Staffs.Any(s => s.StaffEmail.ToLower() == model.StaffEmail.ToLower()))
      {
        ModelState.AddModelError("StaffEmail", "An account with this email already exists.");
      }
    }

    if (ModelState.IsValid)
    {
      var staff = new Staff
      {
        StaffName = model.StaffName,
        StaffEmail = model.StaffEmail,
        StaffRole = model.StaffRole,
        StaffIsActive = model.StaffIsActive,
        StaffBranchId = model.StaffBranchId
      };

      // Hash the password before saving
      if (!string.IsNullOrEmpty(model.NewPassword))
      {
        staff.StaffPassword = _passwordHasher.HashPassword(staff, model.NewPassword);
      }

      _context.Staffs.Add(staff);
      _context.SaveChanges(); // Save the staff first

      // Log the add action for adding new staff
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        var logStaff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value); // Renamed variable to 'logStaff'
        var staffName = logStaff != null ? logStaff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
          ActionType = ActivityLog.ActionTypeEnum.Add, // Use the enum for add action
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) added a new staff member named '{staff.StaffName}'."
        });

        _context.SaveChanges(); // Save the log after adding the staff
      }

      TempData["SuccessMessage"] = $"{model.StaffName} added successfully.";
      return RedirectToAction("Index");
    }

    var activeBranches = _context.Branches
        .Where(b => b.BranchIsActive)
        .ToList();
    ViewBag.Branches = new SelectList(activeBranches, "BranchId", "BranchName", model.StaffBranchId);
    return View(model);
  }

  // GET: /Staff/Edit/{id}
  public IActionResult Edit(int id)
  {
    var staff = _context.Staffs
        .Where(s => s.StaffId == id)
        .Include(s => s.StaffBranch)
        .FirstOrDefault();

    if (staff == null)
    {
      return NotFound();
    }

    var staffViewModel = new StaffViewModel
    {
      StaffId = staff.StaffId,
      StaffName = staff.StaffName,
      StaffRole = staff.StaffRole,
      StaffEmail = staff.StaffEmail,
      StaffIsActive = staff.StaffIsActive,
      StaffBranchId = staff.StaffBranchId,
      ExistingPassword = staff.StaffPassword
    };

    var activeBranches = _context.Branches
        .Where(b => b.BranchIsActive)
        .ToList();
    ViewBag.Branches = new SelectList(activeBranches, "BranchId", "BranchName", staff.StaffBranchId);

    var staffStatusList = new List<SelectListItem>
        {
            new SelectListItem { Text = "Active", Value = "true", Selected = staff.StaffIsActive },
            new SelectListItem { Text = "Inactive", Value = "false", Selected = !staff.StaffIsActive }
        };
    ViewBag.StaffStatus = staffStatusList;

    return View(staffViewModel);
  }

  // POST: /Staff/Edit/{id}
  [HttpPost]
  [ValidateAntiForgeryToken]
  public IActionResult Edit(int id, StaffViewModel model)
  {
    // Check if the email already exists, excluding the current user
    if (!string.IsNullOrWhiteSpace(model.StaffEmail))
    {
      if (_context.Staffs
          .Any(s => s.StaffEmail.ToLower() == model.StaffEmail.ToLower() && s.StaffId != id))
      {
        ModelState.AddModelError("StaffEmail", "An account with this email already exists.");
      }
    }

    if (ModelState.IsValid)
    {
      var staffToUpdate = _context.Staffs.Find(id);  // Renamed the variable to avoid conflict
      if (staffToUpdate == null)
      {
        return NotFound();
      }

      staffToUpdate.StaffName = model.StaffName;
      staffToUpdate.StaffRole = model.StaffRole;
      staffToUpdate.StaffEmail = model.StaffEmail;
      staffToUpdate.StaffIsActive = model.StaffIsActive;
      staffToUpdate.StaffBranchId = model.StaffBranchId;

      if (!string.IsNullOrWhiteSpace(model.NewPassword))
      {
        staffToUpdate.StaffPassword = _passwordHasher.HashPassword(staffToUpdate, model.NewPassword);
      }
      else
      {
        staffToUpdate.StaffPassword = model.ExistingPassword;
      }

      _context.SaveChanges(); // Save the updated staff first

      // Log the update activity
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        var staffWhoUpdated = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value); // Renamed variable to avoid conflict
        var staffName = staffWhoUpdated != null ? staffWhoUpdated.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
          ActionType = ActivityLog.ActionTypeEnum.Update, // Use the enum for update action
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the staff: {staffToUpdate.StaffName}"
        });

        _context.SaveChanges(); // Save the log after editing the staff
      }

      TempData["SuccessMessage"] = $"{model.StaffName} updated successfully.";
      return RedirectToAction("Index");
    }

    var activeBranches = _context.Branches
        .Where(b => b.BranchIsActive)
        .ToList();
    ViewBag.Branches = new SelectList(activeBranches, "BranchId", "BranchName", model.StaffBranchId);
    return View(model);
  }

  // GET: /Staff/ToggleStatus/{id}
  public IActionResult ToggleStatus(int id)
  {
    var staff = _context.Staffs.Find(id);
    if (staff == null)
    {
      return NotFound();
    }

    staff.StaffIsActive = !staff.StaffIsActive;
    _context.SaveChanges(); // Save the status change first

    var staffId = HttpContext.Session.GetInt32("StaffId");
    if (staffId.HasValue)
    {
      // Retrieve the staff that made the update
      var updatingStaff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value); // Renamed to 'updatingStaff'
      var staffName = updatingStaff != null ? updatingStaff.StaffName : "Unknown Staff"; // Fallback if staff is not found

      // Log the update action
      _context.ActivityLogs.Add(new ActivityLog
      {
        StaffId = staffId.Value,
        ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
        ActionType = ActivityLog.ActionTypeEnum.Update, // Enum for update action
        ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the staff member named {staff.StaffName}" // Using staffName
      });

      // Save the log after updating
      _context.SaveChanges();
    }

    TempData["SuccessMessage"] = $"{staff.StaffName} is now " + (staff.StaffIsActive ? "active." : "inactive.");
    return RedirectToAction("Index");
  }
}
