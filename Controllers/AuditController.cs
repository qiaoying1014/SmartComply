using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data; // Your DbContext namespace
using SmartComply.Models; // Your Models namespace
using SmartComply.ViewModels;


namespace SmartComply.Controllers
{
  public class AuditController : Controller
  {
    private readonly ApplicationDbContext _context;

    public AuditController(ApplicationDbContext context)
    {
      _context = context;
    }


    //////////////////////////////////////////////////
    // Show all compliance categories to start audit
    public IActionResult ViewCompliance(string searchTerm)
    {
      // Fetch all categories
      var categories = _context.ComplianceCategories.ToList();

      // Create a list of ViewComplianceViewModel instances, each containing a Category and the count of published forms
      var categoriesWithFormCount = categories.Select(category => new ViewComplianceViewModel
      {
        Category = category,
        FormsCount = _context.Forms.Count(f => f.CategoryId == category.CategoryId && f.Status == "Published")
      }).ToList();

      // Pass the search term and the categories with form count to the view
      ViewBag.SearchTerm = searchTerm;

      return View(categoriesWithFormCount);
    }


    //////////////////////////////////////////////////
    // Show all published forms under a selected compliance category
    public IActionResult ViewComplianceForms(int categoryId)
    {
      var forms = _context.Forms
          .Where(f => f.CategoryId == categoryId && f.Status == "Published")
          .ToList();

      // Get the category name from the ComplianceCategories table
      var category = _context.ComplianceCategories
          .FirstOrDefault(c => c.CategoryId == categoryId);

      // Pass the category name to the view via ViewBag
      ViewBag.CategoryId = categoryId;
      ViewBag.CategoryName = category?.CategoryName; // Assuming CategoryName is the property in ComplianceCategory

      return View(forms); // ViewComplianceForms.cshtml expects List<Form>
    }


    //////////////////////////////////////////////////
    // This method is for displaying the page to create an audit
    public IActionResult AddAudit(int categoryId)
    {
      int? staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId == null)
        return RedirectToAction("Login", "Auth");

      var staff = _context.Staffs
          .Include(s => s.StaffBranch)
          .FirstOrDefault(s => s.StaffId == staffId);

      if (staff == null || staff.StaffBranch == null)
        return Unauthorized();

      var category = _context.ComplianceCategories.Find(categoryId);
      if (category == null)
        return NotFound();

      string datePart = DateTime.UtcNow.ToString("ddMMyyyy");
      string defaultName = $"{category.CategoryName}_{staff.StaffBranch.BranchName}_{datePart}";

      // Convert UTC to Malaysia Time (UTC +8)
      TimeZoneInfo malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuala_Lumpur");
      DateTime malaysiaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);

      var audit = new Audit
      {
        AuditName = defaultName,
        CategoryId = categoryId,
        StaffId = staff.StaffId,
        DueDate = DateTime.SpecifyKind(malaysiaTime.AddDays(7), DateTimeKind.Utc),  // Set due date to 7 days after Malaysia Time
        Status = "Draft"
      };

      _context.Audits.Add(audit);
      _context.SaveChanges();

      // Log the add action
      if (staffId.HasValue)
      {
        var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow,
          ActionType = ActivityLog.ActionTypeEnum.Add,
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) added a new audit named '{audit.AuditName}' under category '{category.CategoryName}'."
        });
        _context.SaveChanges(); // Save the log after adding the audit
      }

      // Redirect to AddAuditDetails to show form selection
      return RedirectToAction("AddAuditDetails", new { auditId = audit.AuditId });
    }


    [HttpGet]
    [Route("Audit/AddAudit/{auditId}", Name = "AddAuditDetailsRoute")]
    public IActionResult AddAuditDetails(int auditId)
    {
      var audit = _context.Audits
          .Include(a => a.Category)
          .FirstOrDefault(a => a.AuditId == auditId);

      if (audit == null)
        return NotFound();

      // Now fetch the available forms after the audit is created
      var forms = _context.Forms
          .Where(f => f.CategoryId == audit.CategoryId && f.Status == "Published")
          .ToList();

      ViewBag.AuditId = audit.AuditId;
      ViewBag.AuditName = audit.AuditName;
      ViewBag.DueDate = audit.DueDate == default ? DateTime.Today : audit.DueDate;

      // Return the view with the list of forms
      return View("AddAudit", forms);
    }



    ///////////////////////////////////////////////////////
    ///Create Audit_Rename Audit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Audit/AddAudit/{auditId}")]
    public IActionResult AddAuditDetails(int AuditId, string AuditName, DateTime DueDate)
    {
      var audit = _context.Audits.Find(AuditId);
      if (audit == null)
        return NotFound();

      audit.AuditName = AuditName;
      audit.DueDate = DateTime.SpecifyKind(DueDate, DateTimeKind.Utc); // Ensure UTC time is saved.

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
          ActionTimestamp = DateTime.UtcNow,
          ActionType = ActivityLog.ActionTypeEnum.Update,
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the audit named '{audit.AuditName}' with due date: '{audit.DueDate}'."
        });
        _context.SaveChanges(); // Save the log after updating the audit
      }

      TempData["SuccessMessage"] = $"Audit {audit.AuditName} updated successfully!";

      return RedirectToAction("AddAuditDetails", new { auditId = AuditId });
    }

    ////////////////////////////////////////////////////////
    ///Verify has existing
    [HttpGet]
    public JsonResult HasExistingAudit(int categoryId)
    {
      int? staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId == null)
        return Json(false);

      bool hasAudit = _context.Audits
          .Any(a => a.CategoryId == categoryId && a.StaffId == staffId);

      return Json(hasAudit);
    }


    ////////////////////////////////////////////////////////
    //My audits summary
    public IActionResult MyAuditSummary(string statusFilter, string searchTerm)
    {
      int? staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId == null)
        return RedirectToAction("Login", "Account");

      // Start with the base query for the audits
      var audits = _context.Audits
          .Where(a => a.StaffId == staffId) // Filter by logged-in staff
          .AsQueryable();

      // Apply the status filter if provided
      if (!string.IsNullOrEmpty(statusFilter))
      {
        audits = audits.Where(a => a.Status.ToLower() == statusFilter.ToLower());
      }

      // Apply the search term filter if provided
      if (!string.IsNullOrEmpty(searchTerm))
      {
        audits = audits.Where(a => a.AuditName.Contains(searchTerm));
      }

      // Order audits by created date in descending order
      audits = audits.OrderByDescending(a => a.CreatedAt);

      // Execute the query and pass the filtered results to the view
      var auditList = audits.ToList();

      // Pass the selected status filter and search term back to the view for retaining the filter state
      ViewBag.StatusFilter = statusFilter;
      ViewBag.SearchTerm = searchTerm;

      return View(auditList);
    }

    //update audit status
    [HttpPost]
    public IActionResult UpdateAuditStatus(int AuditId, string Status)
    {
      var audit = _context.Audits.Find(AuditId);
      if (audit == null)
        return NotFound();

      // Log the status change before updating
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
        var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow,
          ActionType = ActivityLog.ActionTypeEnum.Update,
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the audit status of '{audit.AuditName}' to '{Status}'."
        });
        _context.SaveChanges(); // Save the log before updating the status
      }


      // Update the status
      audit.Status = Status;
      _context.SaveChanges();

      TempData["SuccessMessage"] = $"Audit {audit.AuditName} status updated successfully!";
      return RedirectToAction("MyAuditSummary");
    }


    ///Delete Audit
    [HttpPost]
    public IActionResult DeleteAudit(int AuditId)
    {
      var audit = _context.Audits
    .Include(a => a.FormResponders)
    .Include(a => a.Category) // <-- Add this line
    .FirstOrDefault(a => a.AuditId == AuditId);


      if (audit == null)
        return NotFound();

      // Delete related FormResponders first
      var relatedResponders = _context.FormResponders.Where(fr => fr.AuditId == AuditId);
      _context.FormResponders.RemoveRange(relatedResponders);

      _context.Audits.Remove(audit);
      _context.SaveChanges();

      // Log the delete action
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
        var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow,
          ActionType = ActivityLog.ActionTypeEnum.Delete,
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) deleted the audit '{audit.AuditName}' under category '{audit.Category.CategoryName}'."
        });
        _context.SaveChanges(); // Save the log after deleting the audit
      }


      TempData["SuccessMessage"] = $"Audit {audit.AuditName} deleted successfully!";
      return RedirectToAction("MyAuditSummary");
    }


    /////////////////////////////
    ///View Edit Audit
    [HttpGet]
    [Route("Audit/ViewEditAudit/{auditId}")]
    public IActionResult ViewEditAudit(int auditId)
    {
      var audit = _context.Audits
          .Include(a => a.Category)
          .FirstOrDefault(a => a.AuditId == auditId);

      if (audit == null)
        return NotFound();

      var forms = _context.Forms
          .Where(f => f.CategoryId == audit.CategoryId && f.Status=="Published")
          .ToList();

      ViewBag.AuditId = audit.AuditId;
      ViewBag.AuditName = audit.AuditName;
      ViewBag.DueDate = audit.DueDate == default ? DateTime.Today : audit.DueDate;

      return View("AddAudit", forms); // reuse the AddAudit.cshtml interface
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Audit/ViewEditAudit/{auditId}")]
    public IActionResult ViewEditAudit(int auditId, string auditName, DateTime dueDate)
    {
      var audit = _context.Audits.Find(auditId);
      if (audit == null)
        return NotFound();

      audit.AuditName = auditName;
      audit.DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);

      // <-- Add log here
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
        var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow,
          ActionType = ActivityLog.ActionTypeEnum.Update,
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the audit named '{auditName}' with due date: '{dueDate.ToShortDateString()}'."
        });
        _context.SaveChanges();
      }

      TempData["SuccessMessage"] = $"Audit {audit.AuditName} updated successfully!";

      return RedirectToAction("ViewEditAudit", new { auditId });
    }


    // GET: Show audit for editing
    [HttpGet]
    public IActionResult EditAudit(int auditId)
    {
      var audit = _context.Audits
          .Include(a => a.Category)
          .FirstOrDefault(a => a.AuditId == auditId);

      if (audit == null)
        return NotFound();

      var forms = _context.Forms
          .Where(f => f.CategoryId == audit.CategoryId && f.Status == "Published")
          .ToList();

      ViewBag.AuditId = audit.AuditId;
      ViewBag.AuditName = audit.AuditName;
      ViewBag.DueDate = audit.DueDate == default ? DateTime.Today : audit.DueDate;

      return View(forms); // The view expects List<Form>
    }


    // POST: Save updated audit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditAudit(int auditId, string auditName, DateTime dueDate)
    {
      var audit = _context.Audits.Find(auditId);
      if (audit == null)
        return NotFound();

      audit.AuditName = auditName;
      audit.DueDate = DateTime.SpecifyKind(dueDate, DateTimeKind.Utc);

      // <-- Add log here
      var staffId = HttpContext.Session.GetInt32("StaffId");
      if (staffId.HasValue)
      {
        var staff = _context.Staffs.FirstOrDefault(s => s.StaffId == staffId.Value);
        var staffName = staff != null ? staff.StaffName : "Unknown Staff"; // Fallback in case the staff name is not found

        _context.ActivityLogs.Add(new ActivityLog
        {
          StaffId = staffId.Value,
          ActionTimestamp = DateTime.UtcNow,
          ActionType = ActivityLog.ActionTypeEnum.Update,
          ActionDescription = $"Staff {staffName} (ID: {staffId.Value}) updated the audit named '{auditName}'."
        });
        _context.SaveChanges();
      }


      TempData["SuccessMessage"] = $"Audit {audit.AuditName} status updated.";

      return RedirectToAction("EditAudit", new { auditId });
    }

  }
}
