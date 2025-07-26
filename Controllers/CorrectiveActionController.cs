using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;

namespace SmartComply.Controllers
{
  public class CorrectiveActionController : Controller
  {
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IAntiforgery _antiforgery;

    public CorrectiveActionController(ApplicationDbContext db, IWebHostEnvironment env, IAntiforgery antiforgery)
    {
      _db = db;
      _env = env;
      _antiforgery = antiforgery;
    }

    // LIST: show all corrective actions for a given auditId
    public IActionResult Index(int auditId, string searchTerm, string statusFilter)
    {
      // Start with the basic query to get non-deleted items
      var correctiveActions = _db.CorrectiveActions
          .Where(c => c.AuditId == auditId && !c.IsDeleted)
          .AsQueryable(); // Convert to IQueryable for dynamic filtering

      // Apply search filter if searchTerm is provided
      if (!string.IsNullOrEmpty(searchTerm))
      {
        correctiveActions = correctiveActions.Where(c => c.Description.Contains(searchTerm));
      }

      // Apply status filter if statusFilter is provided
      if (!string.IsNullOrEmpty(statusFilter))
      {
        // Convert both the Status enum value and statusFilter to lowercase for case-insensitive comparison
        correctiveActions = correctiveActions.Where(c => c.Status.ToString().ToLower() == statusFilter.ToLower());
      }

      // Get the audit details for the breadcrumb
      var audit = _db.Audits.FirstOrDefault(a => a.AuditId == auditId);
      ViewBag.AuditName = audit?.AuditName ?? $"Audit {auditId}";
      ViewBag.AuditId = auditId;
      ViewBag.SearchTerm = searchTerm; // Store search term in ViewBag
      ViewBag.StatusFilter = statusFilter; // Store status filter in ViewBag

      return View(correctiveActions.ToList());
    }



    // -------------------------------------------------------------------
    // GET: CorrectiveActions/Create
    // Just shows a blank form.
    // -------------------------------------------------------------------
    [HttpGet]
    public IActionResult Add(int auditId)
    {
      // Set AuditId in ViewBag so it can be used in the breadcrumb and elsewhere
      ViewBag.AuditId = auditId;

      // Fetch the Audit details from the database using the auditId
      var audit = _db.Audits.FirstOrDefault(a => a.AuditId == auditId);

      // If the audit is not found, handle it (e.g., redirect or return a not found view)
      if (audit == null)
      {
        // Redirect or return a not found view if the audit is not found
        return NotFound();
      }

      // Create the CorrectiveActionViewModel with necessary default values
      var vm = new CorrectiveActionViewModel
      {
        AuditId = auditId,
        TargetDate = DateTime.Today.AddDays(7), // Default target date
        Status = CorrectiveActionStatus.Pending  // Default status
      };

      // Set the Audit Name in ViewBag for breadcrumb or page title
      ViewBag.AuditName = audit.AuditName ?? $"Audit {auditId}";

      // Return the view with the CorrectiveActionViewModel
      return View(vm);
    }


    // -------------------------------------------------------------------
    // POST: CorrectiveActions/Create
    // Binds the ViewModel, saves files to disk, then saves entity to DB.
    // -------------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Add(CorrectiveActionViewModel vm)
    {
      Console.WriteLine($"[DEBUG] Entered Create POST – AuditId={vm.AuditId}, Description={vm.Description}");

      if (!ModelState.IsValid)
      {
        // For debugging, you can inspect ModelState errors:
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
        // Breakpoint here, or log these errors to see why validation failed.
        return View(vm);
      }

      // 1. Map ViewModel → Entity (except file‐fields)
      var entity = new CorrectiveAction
      {
        AuditId = vm.AuditId,
        Description = vm.Description,
        RootCause = vm.RootCause,
        ProposedAction = vm.ProposedAction,
        ResponsiblePerson = vm.ResponsiblePerson,
        TargetDate = DateTime.SpecifyKind(vm.TargetDate, DateTimeKind.Utc),
        CompletionDate = vm.CompletionDate.HasValue ? DateTime.SpecifyKind(vm.CompletionDate.Value, DateTimeKind.Utc) : (DateTime?)null,
        Status = vm.Status,
        Remarks = vm.Remarks,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      // 2. Handle "BeforePhotoUpload"
      if (vm.BeforePhotoUpload != null && vm.BeforePhotoUpload.Length > 0)
      {
        // a) Choose a folder under wwwroot. E.g. “uploads/correctiveactions”
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "correctiveactions");
        if (!Directory.Exists(uploadFolder))
        {
          Directory.CreateDirectory(uploadFolder);
        }

        // b) Build a unique filename
        var ext = Path.GetExtension(vm.BeforePhotoUpload.FileName).ToLowerInvariant();
        var allowedExts = new[] { ".jpg", ".jpeg", ".png" };
        if (!allowedExts.Contains(ext))
        {
          ModelState.AddModelError(nameof(vm.BeforePhotoUpload), "Only JPG/PNG are allowed for the Before Photo.");
          return View(vm);
        }

        var newFileName = Guid.NewGuid().ToString("N") + ext;
        var filePath = Path.Combine(uploadFolder, newFileName);

        // c) Save to disk
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
          await vm.BeforePhotoUpload.CopyToAsync(fs);
        }

        // d) Store the relative URL for serving later
        entity.BeforeActionPhotoPath = "/uploads/correctiveactions/" + newFileName;
      }

      // 3. Handle "AfterPhotoUpload" (only if Before was set, or you're allowing both)
      if (vm.AfterPhotoUpload != null && vm.AfterPhotoUpload.Length > 0)
      {
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "correctiveactions");
        if (!Directory.Exists(uploadFolder))
        {
          Directory.CreateDirectory(uploadFolder);
        }

        var ext = Path.GetExtension(vm.AfterPhotoUpload.FileName).ToLowerInvariant();
        var allowedExts = new[] { ".jpg", ".jpeg", ".png" };
        if (!allowedExts.Contains(ext))
        {
          ModelState.AddModelError(nameof(vm.AfterPhotoUpload), "Only JPG/PNG are allowed for the After Photo.");
          return View(vm);
        }

        var newFileName = Guid.NewGuid().ToString("N") + ext;
        var filePath = Path.Combine(uploadFolder, newFileName);

        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
          await vm.AfterPhotoUpload.CopyToAsync(fs);
        }

        entity.AfterActionPhotoPath = "/uploads/correctiveactions/" + newFileName;
      }

      // 4. Save the new entity
      Console.WriteLine("[DEBUG] About to add entity and SaveChangesAsync()");
      _db.CorrectiveActions.Add(entity);
      await _db.SaveChangesAsync();

      // Log Activity
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        var staff = _db.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
        var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _db.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
          ActionType = ActivityLog.ActionTypeEnum.Add, // Use the enum for add action
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) added a corrective action for Audit {vm.AuditId}: {vm.Description}"
        });
        await _db.SaveChangesAsync();
      }

      TempData["SuccessMessage"] = $"Corrective action added successfully.";

      // 5. Redirect to List with current AuditId
      return RedirectToAction("Index", new { auditId = vm.AuditId });
    }

    // (You can implement Index, Details, Edit, Delete below as needed.)

    // --------------------------------------------
    // GET: CorrectiveActions/Edit/5?auditId=42
    // --------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Edit(int id, int auditId)
    {
      // Set the AuditId in ViewBag so it can be used in the breadcrumb and elsewhere
      ViewBag.AuditId = auditId;

      // Fetch the CorrectiveAction entity by id
      var entity = await _db.CorrectiveActions.FindAsync(id);
      if (entity == null || entity.AuditId != auditId)
      {
        return NotFound(); // Return NotFound if the entity doesn't exist or the auditId doesn't match
      }

      // Fetch the Audit details from the database using the auditId
      var audit = await _db.Audits.FirstOrDefaultAsync(a => a.AuditId == auditId);

      // If the audit is not found, return NotFound
      if (audit == null)
      {
        return NotFound();
      }

      // Prepare the CorrectiveActionViewModel with data from the entity
      var vm = new CorrectiveActionViewModel
      {
        CorrectiveActionId = entity.CorrectiveActionId,
        AuditId = entity.AuditId,
        Description = entity.Description,
        RootCause = entity.RootCause,
        ProposedAction = entity.ProposedAction,
        ResponsiblePerson = entity.ResponsiblePerson,
        TargetDate = entity.TargetDate,      // Read-only field
        CompletionDate = entity.CompletionDate,  // Editable field
        Status = entity.Status,
        Remarks = entity.Remarks,
        ExistingBeforePhotoPath = entity.BeforeActionPhotoPath,
        ExistingAfterPhotoPath = entity.AfterActionPhotoPath,
        CreatedAt = entity.CreatedAt
      };

      // Set the Audit Name in ViewBag for breadcrumb or page title
      ViewBag.AuditName = audit.AuditName ?? $"Audit {auditId}";

      // Return the view with the CorrectiveActionViewModel
      return View(vm);
    }


    // --------------------------------------------
    // POST: CorrectiveActions/Edit/
    // --------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CorrectiveActionViewModel vm)
    {
      if (id != vm.CorrectiveActionId)
        return BadRequest();

      // If validation fails, re‐populate existing image paths so they show on the form
      if (!ModelState.IsValid)
      {
        var orig = await _db.CorrectiveActions.FindAsync(id);
        if (orig != null)
        {
          vm.ExistingBeforePhotoPath = orig.BeforeActionPhotoPath;
          vm.ExistingAfterPhotoPath = orig.AfterActionPhotoPath;
        }
        return View(vm);
      }

      var entity = await _db.CorrectiveActions.FindAsync(id);
      if (entity == null)
        return NotFound();

      entity.CreatedAt = DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc);

      // 1⃣ Preserve the old TargetDate (do NOT overwrite it)
      entity.TargetDate = entity.TargetDate;

      // 2⃣ Update CompletionDate (convert to UTC kind first)
      if (vm.CompletionDate.HasValue)
        entity.CompletionDate = DateTime.SpecifyKind(vm.CompletionDate.Value, DateTimeKind.Utc);
      else
        entity.CompletionDate = null;

      // 3⃣ Update all other editable fields
      entity.Description = vm.Description;
      entity.RootCause = vm.RootCause;
      entity.ProposedAction = vm.ProposedAction;
      entity.ResponsiblePerson = vm.ResponsiblePerson;
      entity.Status = vm.Status;
      entity.Remarks = vm.Remarks;
      entity.UpdatedAt = DateTime.UtcNow;

      // 4⃣ Handle “Before” photo replacement
      if (vm.BeforePhotoUpload != null && vm.BeforePhotoUpload.Length > 0)
      {
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "correctiveactions");
        if (!Directory.Exists(uploadFolder))
          Directory.CreateDirectory(uploadFolder);

        var ext = Path.GetExtension(vm.BeforePhotoUpload.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        if (!allowed.Contains(ext))
        {
          ModelState.AddModelError(nameof(vm.BeforePhotoUpload), "Only JPG/PNG/GIF allowed.");
          vm.ExistingBeforePhotoPath = entity.BeforeActionPhotoPath;
          vm.ExistingAfterPhotoPath = entity.AfterActionPhotoPath;
          return View(vm);
        }

        // Delete old file if it exists
        if (!string.IsNullOrEmpty(entity.BeforeActionPhotoPath))
        {
          var oldPath = Path.Combine(_env.WebRootPath, entity.BeforeActionPhotoPath.TrimStart('/'));
          if (System.IO.File.Exists(oldPath))
            System.IO.File.Delete(oldPath);
        }

        var newFileName = Guid.NewGuid().ToString("N") + ext;
        var filePath = Path.Combine(uploadFolder, newFileName);
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
          await vm.BeforePhotoUpload.CopyToAsync(fs);

        entity.BeforeActionPhotoPath = "/uploads/correctiveactions/" + newFileName;
      }

      // 5⃣ Handle “After” photo replacement
      if (vm.AfterPhotoUpload != null && vm.AfterPhotoUpload.Length > 0)
      {
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "correctiveactions");
        if (!Directory.Exists(uploadFolder))
          Directory.CreateDirectory(uploadFolder);

        var ext = Path.GetExtension(vm.AfterPhotoUpload.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        if (!allowed.Contains(ext))
        {
          ModelState.AddModelError(nameof(vm.AfterPhotoUpload), "Only JPG/PNG/GIF allowed.");
          vm.ExistingBeforePhotoPath = entity.BeforeActionPhotoPath;
          vm.ExistingAfterPhotoPath = entity.AfterActionPhotoPath;
          return View(vm);
        }

        if (!string.IsNullOrEmpty(entity.AfterActionPhotoPath))
        {
          var oldPath = Path.Combine(_env.WebRootPath, entity.AfterActionPhotoPath.TrimStart('/'));
          if (System.IO.File.Exists(oldPath))
            System.IO.File.Delete(oldPath);
        }

        var newFileName = Guid.NewGuid().ToString("N") + ext;
        var filePath = Path.Combine(uploadFolder, newFileName);
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
          await vm.AfterPhotoUpload.CopyToAsync(fs);

        entity.AfterActionPhotoPath = "/uploads/correctiveactions/" + newFileName;
      }

      // 6⃣ Save changes
      try
      {
        _db.Entry(entity).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Corrective action updated successfully.";

        // Log the update activity
        var staffId = HttpContext.Session.GetInt32("StaffId");
        if (staffId.HasValue)
        {
          var staff = _db.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
          var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

          _db.ActivityLogs.Add(new ActivityLog
          {
            StaffId = staffId.Value,
            ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
            ActionType = ActivityLog.ActionTypeEnum.Update, // Use the enum for update action
            ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the corrective action for Audit {vm.AuditId}: {vm.Description}"
          });
          await _db.SaveChangesAsync(); // Save the log entry after updating the corrective action
        }
      }
      catch (DbUpdateException ex)
      {
        ModelState.AddModelError("", "Unable to save changes: " + ex.Message);
        vm.ExistingBeforePhotoPath = entity.BeforeActionPhotoPath;
        vm.ExistingAfterPhotoPath = entity.AfterActionPhotoPath;
        return View(vm);
      }

      // 7⃣ After saving, redirect back to the List of this audit
      return RedirectToAction("Index", new { auditId = entity.AuditId });
    }



    // <summary>
    // AJAX endpoint that sets IsDeleted = true on the given CorrectiveAction.
    // </summary>
    [HttpPost]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDelete(int id)
    {
      var entity = await _db.CorrectiveActions.FindAsync(id);
      if (entity == null)
        return Json(new { success = false, message = "Not found" });

      // Soft delete
      entity.IsDeleted = true;
      entity.UpdatedAt = DateTime.UtcNow;

      // Prevent changing CreatedAt
      _db.Entry(entity).Property(e => e.CreatedAt).IsModified = false;

      try
      {
        await _db.SaveChangesAsync();

        // Log the soft delete action
        var staffId = HttpContext.Session.GetInt32("StaffId");
        if (staffId.HasValue)
        {
          var staff = _db.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
          var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

          _db.ActivityLogs.Add(new ActivityLog
          {
            StaffId = staffId.Value,
            ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
            ActionType = ActivityLog.ActionTypeEnum.Delete, // Use the enum for delete action (soft delete)
            ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) deleted the corrective action for Audit {entity.AuditId}: {entity.Description}"
          });

          await _db.SaveChangesAsync(); // Save the log after soft-deleting the corrective action
        }


        TempData["SuccessMessage"] = $"Corrective action deleted successfully.";
        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
      }
    }


    // GET: CorrectiveActions/Recover?auditId=42
    public IActionResult Recover(int auditId, string statusFilter, string searchTerm)
    {
      var query = _db.CorrectiveActions
                     .Where(c => c.AuditId == auditId && c.IsDeleted);

      // Apply the status filter
      if (!string.IsNullOrEmpty(statusFilter))
      {
        var status = Enum.TryParse<CorrectiveActionStatus>(statusFilter, true, out var parsedStatus)
                     ? parsedStatus
                     : CorrectiveActionStatus.Pending;  // Default value if parsing fails

        query = query.Where(c => c.Status == status);
      }

      // Apply the search term filter (if provided)
      if (!string.IsNullOrEmpty(searchTerm))
      {
        query = query.Where(c => c.Description.Contains(searchTerm) || c.ResponsiblePerson.Contains(searchTerm));
      }

      // Fetch the filtered data
      var deletedActions = query.ToList();

      var audit = _db.Audits.FirstOrDefault(a => a.AuditId == auditId);
      if (audit == null)
      {
        return NotFound(); // Return NotFound if the audit is not found
      }

      // Set the AuditId and AuditName for the page
      ViewBag.AuditId = auditId;
      ViewBag.AuditName = audit.AuditName ?? $"Audit {auditId}";
      ViewBag.StatusFilter = statusFilter;
      ViewBag.SearchTerm = searchTerm;

      return View(deletedActions);
    }

    // POST: CorrectiveActions/SoftRecover
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftRecover(int id)
    {
      var entity = await _db.CorrectiveActions.FindAsync(id);
      if (entity == null)
        return Json(new { success = false, message = "Not found" });

      entity.IsDeleted = false;
      entity.UpdatedAt = DateTime.UtcNow;
      _db.Entry(entity).Property(e => e.CreatedAt).IsModified = false;

      try
      {
        await _db.SaveChangesAsync();

        // Log the recovery action
        var staffId = HttpContext.Session.GetInt32("StaffId");
        if (staffId.HasValue)
        {
          var staff = _db.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
          var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

          _db.ActivityLogs.Add(new ActivityLog
          {
            StaffId = staffId.Value,
            ActionTimestamp = DateTime.UtcNow, // Store the time in UTC
            ActionType = ActivityLog.ActionTypeEnum.Update, // Use the enum for update action (recovery)
            ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) recovered the corrective action for Audit {entity.AuditId}: {entity.Description}"
          });

          await _db.SaveChangesAsync(); // Save the log after recovering the corrective action
        }


        // Set TempData for success message
        TempData["SuccessMessage"] = "Corrective action recovered successfully.";

        // Send a JSON response indicating success
        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
      }
    }

  }
}

