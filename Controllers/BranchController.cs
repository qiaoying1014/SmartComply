using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;

namespace SmartComply.Controllers
{
  public class BranchController : Controller
  {
    private readonly ApplicationDbContext _context;

    public BranchController(ApplicationDbContext context)
    {
      _context = context;
    }

    // GET: Branch/Index
    public IActionResult Index(string statusFilter = null, string searchTerm = null, string branchName = null)
    {
      var query = _context.Branches.AsQueryable();

      // Filter by status
      if (!string.IsNullOrEmpty(statusFilter))
      {
        bool isActive = statusFilter.ToLower() == "enabled";
        query = query.Where(b => b.BranchIsActive == isActive);
      }

      // Filter by branch address
      if (!string.IsNullOrEmpty(searchTerm))
      {
        string lowerSearchTerm = searchTerm.ToLower();
        query = query.Where(b => b.BranchAddress.ToLower().Contains(lowerSearchTerm));
      }

      // Filter by branch name
      if (!string.IsNullOrEmpty(branchName))
      {
        string lowerBranchName = branchName.ToLower();
        query = query.Where(b => b.BranchName.ToLower().Contains(lowerBranchName));
      }

      // Sort by BranchName ascending
      var branches = query
          .OrderBy(b => b.BranchName)
          .ToList();

      ViewBag.StatusFilter = statusFilter;
      ViewBag.SearchTerm = searchTerm;
      ViewBag.BranchName = branchName;

      return View(branches);
    }

    // GET: Branch/Add
    public IActionResult Add()
    {
      return View();
    }

    // POST: Branch/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(BranchViewModel model)
    {
      // Check if the BranchName or BranchAddress already exists (case-insensitive)
      if (!string.IsNullOrWhiteSpace(model.BranchName))
      {
        if (_context.Branches
            .Any(b => b.BranchName.ToLower() == model.BranchName.ToLower()))
        {
          ModelState.AddModelError("BranchName", "A branch with the same name already exists.");
        }
      }

      if (!string.IsNullOrWhiteSpace(model.BranchAddress))
      {
        if (_context.Branches
            .Any(b => b.BranchAddress.ToLower() == model.BranchAddress.ToLower()))
        {
          ModelState.AddModelError("BranchAddress", "A branch with the same address already exists.");
        }
      }

      if (ModelState.IsValid)
      {
        var branch = new Branch
        {
          BranchName = model.BranchName,
          BranchAddress = model.BranchAddress,
          BranchIsActive = model.BranchIsActive
        };

        _context.Branches.Add(branch);
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
            ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) added a new branch named '{branch.BranchName}'."
          });
          _context.SaveChanges();
        }

        TempData["SuccessMessage"] = $"{model.BranchName} added successfully.";
        return RedirectToAction("Index");
      }

      // If validation fails, return the model to the view to show error messages
      return View(model);
    }


    // GET: Branch/Edit/{id}
    public IActionResult Edit(int id)
    {
      var branch = _context.Branches.Find(id);
      if (branch == null) return NotFound();

      var model = new BranchViewModel
      {
        BranchId = branch.BranchId,
        BranchName = branch.BranchName,
        BranchAddress = branch.BranchAddress,
        BranchIsActive = branch.BranchIsActive
      };

      return View(model);
    }

    // POST: Branch/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, BranchViewModel model)
    {
      if (id != model.BranchId)
      {
        return NotFound();
      }

      // Check if the BranchName or BranchAddress already exists, but exclude the current branch being edited
      if (!string.IsNullOrWhiteSpace(model.BranchName))
      {
        if (_context.Branches
            .Any(b => b.BranchName.ToLower() == model.BranchName.ToLower() && b.BranchId != id))
        {
          ModelState.AddModelError("BranchName", "A branch with the same name already exists.");
        }
      }

      if (!string.IsNullOrWhiteSpace(model.BranchAddress))
      {
        if (_context.Branches
            .Any(b => b.BranchAddress.ToLower() == model.BranchAddress.ToLower() && b.BranchId != id))
        {
          ModelState.AddModelError("BranchAddress", "A branch with the same address already exists.");
        }
      }

      if (ModelState.IsValid)
      {
        var branch = _context.Branches.Find(id);
        if (branch == null) return NotFound();

        // Update branch information
        branch.BranchName = model.BranchName;
        branch.BranchAddress = model.BranchAddress;
        branch.BranchIsActive = model.BranchIsActive;

        _context.SaveChanges(); // Save the updated branch first

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
            ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the branch named '{branch.BranchName}'."
          });
          _context.SaveChanges(); // Save the log after updating the branch
        }

        TempData["SuccessMessage"] = $"{model.BranchName} updated successfully.";
        return RedirectToAction("Index");
      }

      // If validation fails, return the model to the view to show error messages
      return View(model);
    }


    // GET: Branch/ToggleStatus/{id}
    public IActionResult ToggleStatus(int id)
    {
      var branch = _context.Branches.Find(id);
      if (branch == null) return NotFound();

      // Toggle the status
      branch.BranchIsActive = !branch.BranchIsActive;
      _context.SaveChanges(); // Save the status change first

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
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the branch named '{branch.BranchName}' status."
        });
        _context.SaveChanges(); // Save the log after toggling the status
      }

      TempData["SuccessMessage"] = $"{branch.BranchName} is now " +
                                   (branch.BranchIsActive ? "enabled." : "disabled.");
      return RedirectToAction("Index");
    }
  }
}
